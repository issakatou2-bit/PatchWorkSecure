using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure
{
    /// <summary>
    /// ゲームのコアロジック。UnityのMonoBehaviourに依存しない純粋なC#クラスなので、
    /// エディタ上のシーンとは切り離してテスト・バランス調整ができる。
    /// UI側(MonoBehaviour)はこのクラスの状態を読み取って描画するだけにする。
    /// </summary>
    public class GameState
    {
        // ---- 定数 ----
        public const int DailyIncome = 16;       // 検証済み：対策すれば報われるバランスになる水準
        public const int TotalPeriods = 36;      // 4月上旬〜3月下旬（12ヶ月 × 上旬/中旬/下旬）
        private static readonly string[] Months = { "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "1月", "2月", "3月" };
        private static readonly string[] Parts = { "上旬", "中旬", "下旬" };

        // ---- 状態 ----
        public int Day = 1;
        public int Budget = 100;
        public int Trust = 30;
        public int Stress = 20;
        public Dictionary<string, int> DefenseLevels = new Dictionary<string, int>();
        public List<string> Log = new List<string>();
        public bool IsGameOver;
        public string GameOverReason = "";
        public bool IsCleared;

        private readonly Random _rng;

        public GameState(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>「4月上旬」のような表示ラベルを返す。</summary>
        public string PeriodLabel()
        {
            int idx = Math.Clamp(Day - 1, 0, TotalPeriods - 1);
            return Months[idx / 3] + Parts[idx % 3];
        }

        private static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
        private static float Clamp01(float v, float min, float max) => Math.Max(min, Math.Min(max, v));

        // ---- 防御率計算 ----

        /// <summary>
        /// 導入済み対策から、指定された攻撃に対する基礎防御率を算出する。
        /// 攻撃ごとの特殊ギミック（複合防御必須、技術対策無効など）もここで適用される。
        /// </summary>
        public float CalcDefenseRate(string attackKey)
        {
            var attack = GameData.Attacks[attackKey];
            int installedCount = DefenseLevels.Count(kv => kv.Value > 0);
            float rate = 0f;

            foreach (var kv in DefenseLevels)
            {
                if (kv.Value <= 0) continue;
                if (attack.ExcludeDefenses.Contains(kv.Key)) continue; // 迂回される対策は無効
                if (!GameData.Defenses.TryGetValue(kv.Key, out var def)) continue;
                if (def.BaseEffect.TryGetValue(attackKey, out float eff))
                    rate += eff * kv.Value;
            }

            switch (attack.Special)
            {
                case AttackSpecial.MultiDefenseRequired:
                    if (installedCount <= 1) rate *= 0.4f;
                    else if (installedCount == 2) rate *= 0.75f;
                    break;
                case AttackSpecial.TechIgnored:
                    rate *= 0.25f;
                    break;
                case AttackSpecial.DefenseDampen:
                    rate *= 0.55f;
                    break;
                case AttackSpecial.VpnCritical:
                    if (!DefenseLevels.TryGetValue("vpn", out int vpnLvl) || vpnLvl == 0) rate *= 0.5f;
                    break;
                case AttackSpecial.Composite:
                    if (installedCount <= 2) rate *= 0.5f;
                    else if (installedCount <= 4) rate *= 0.8f;
                    break;
            }
            // 設備だけで無敵にはならない（多層防御でも「絶対安全」は存在しないという思想の実装）。
            // 残りは人望・ストレス・対応判断・パリィで埋めることになる。
            return Clamp01(rate, 0f, 0.70f);
        }

        /// <summary>
        /// 最終防御率。基礎防御率に加え、対応選択・パリィ・人望ボーナス・ストレスペナルティを合算する。
        /// 人望が高いと社員が早く通報し、ストレスが高いと現場が対策を迂回する、という性弱説の実装。
        /// </summary>
        public float CalcFinalDefenseRate(string attackKey, AttackChoice choice, float parryBonus)
        {
            float baseRate = CalcDefenseRate(attackKey);
            float trustBonus = (Trust - 50f) / 400f;
            float stressPenalty = Stress > 60 ? (Stress - 60f) / 300f : 0f;
            return Clamp01(baseRate + choice.DefenseBonus + parryBonus + trustBonus - stressPenalty, 0.02f, 0.95f);
        }

        // ---- 日常フェーズ ----

        /// <summary>ヘルプデスク対応。誠実に対応すれば人望が上がり、後回しにすると下がる。</summary>
        public void ResolveChore(bool solved, int trustGain)
        {
            Trust = Clamp(Trust + (solved ? trustGain : -2), 0, 100);
            Budget = Clamp(Budget + DailyIncome, 0, 999);
            AddLog(solved ? $"✅ 雑務を解決した（人望+{trustGain}）" : "⚠️ 対応を後回しにした（人望-2）");
            AddLog($"💰 本日の売上 +¥{DailyIncome}");

            if (Trust <= 0)
            {
                IsGameOver = true;
                GameOverReason = "人望を完全に失い、取引先・顧客が離れていった…";
            }
        }

        /// <summary>対策の導入・強化。ストレスも変動する（社員教育のみストレスが下がる）。</summary>
        public bool UpgradeDefense(string key)
        {
            if (!GameData.Defenses.TryGetValue(key, out var def)) return false;
            int currentLvl = DefenseLevels.TryGetValue(key, out int lv) ? lv : 0;
            if (currentLvl >= def.Levels.Count) return false;

            var next = def.Levels[currentLvl];
            if (Budget < next.Cost) return false;

            Budget -= next.Cost;
            Stress = Clamp(Stress + next.StressCost, 0, 100);
            DefenseLevels[key] = currentLvl + 1;
            AddLog($"🔧 {def.DisplayName} を Lv.{currentLvl + 1} に強化した（予算-{next.Cost}）");
            return true;
        }

        // ---- 攻撃フェーズ ----

        /// <summary>
        /// その日に攻撃が発生するか判定する。年度末に向けて発生率が上昇していく。
        /// </summary>
        public bool RollAttackOccurrence()
        {
            float chance = Clamp01(0.35f + Day * (0.4f / TotalPeriods), 0.35f, 0.75f);
            return _rng.NextDouble() < chance;
        }

        /// <summary>
        /// 発生する攻撃の種類を抽選する。
        /// ストレスが高いほど内部不正が、終盤ほどランサム・AI悪用型の比率が上がる。
        /// </summary>
        public string RollAttackType()
        {
            float escalation = (float)Day / TotalPeriods;
            var keys = GameData.Attacks.Keys.ToList();
            var weights = new List<float>();

            foreach (var k in keys)
            {
                float w = GameData.Attacks[k].BaseChance;
                if (k == "insider") w += Stress / 300f;
                if (k == "ransomware" || k == "aiRisk") w *= (1f + escalation * 1.5f);
                weights.Add(w);
            }

            float total = weights.Sum();
            double r = _rng.NextDouble() * total;
            for (int i = 0; i < keys.Count; i++)
            {
                if (r < weights[i]) return keys[i];
                r -= weights[i];
            }
            return keys[0];
        }

        /// <summary>攻撃への対応を確定し、被害または防御成功を反映する。</summary>
        public AttackResult ResolveAttack(string attackKey, AttackChoice choice, float parryBonus)
        {
            var attack = GameData.Attacks[attackKey];
            float finalRate = CalcFinalDefenseRate(attackKey, choice, parryBonus);
            double roll = _rng.NextDouble();
            bool defended = roll < finalRate;
            double margin = Math.Abs(roll - finalRate);

            Budget = Clamp(Budget - choice.BudgetCost, 0, 999);
            Stress = Clamp(Stress + choice.StressCost, 0, 100);

            var result = new AttackResult
            {
                AttackKey = attackKey,
                Defended = defended,
                FinalDefenseRate = finalRate,
                CharacterLine = defended ? attack.LineWin : attack.LineLose
            };

            if (defended)
            {
                result.Flavor = margin < 0.08 ? "紙一重で防いだ！" : "危なげなく防いだ。";
                AddLog($"🛡️ {attack.DisplayName}を{result.Flavor}（防御率{Math.Round(finalRate * 100)}%）");
            }
            else
            {
                // バックアップのレベルに応じて被害額を軽減（防御ではなく復旧手段としての性質）
                int backupLvl = DefenseLevels.TryGetValue("backup", out int bl) ? bl : 0;
                float mitigation = backupLvl * 0.15f;
                int damage = (int)Math.Round(attack.DamageBudget * (1f - mitigation));

                Budget = Clamp(Budget - damage, 0, 999);
                Trust = Clamp(Trust - attack.DamageTrust, 0, 100);
                result.Flavor = margin < 0.08 ? "僅かの差で防げなかった…" : "呆気なく突破された…";
                result.BudgetDamage = damage;
                result.TrustDamage = attack.DamageTrust;
                AddLog($"💥 {attack.DisplayName}に{result.Flavor}（予算-{damage}, 人望-{attack.DamageTrust}）");
            }

            if (Budget <= 0)
            {
                IsGameOver = true;
                GameOverReason = "予算が尽きて経営破綻した…";
            }
            else if (Trust <= 0)
            {
                IsGameOver = true;
                GameOverReason = "人望を完全に失い、取引先・顧客が離れていった…";
            }

            return result;
        }

        /// <summary>次の期間へ進む。最終期間を超えたらクリア。</summary>
        public void AdvanceDay()
        {
            if (Day >= TotalPeriods)
            {
                IsCleared = true;
                return;
            }
            Day++;
        }

        private void AddLog(string message)
        {
            Log.Insert(0, message);
            if (Log.Count > 8) Log.RemoveAt(Log.Count - 1);
        }
    }

    [System.Serializable]
    public class AttackResult
    {
        public string AttackKey;
        public bool Defended;
        public float FinalDefenseRate;
        public string Flavor;
        public string CharacterLine;
        public int BudgetDamage;
        public int TrustDamage;
    }
}
