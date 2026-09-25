using System;

namespace PatchWorkSecure
{
    /// <summary>判断材料と振り返りの文章。ルールの数値はGameStateから取得する。</summary>
    public static class GamePresentation
    {
        public static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();
        public static string Rate(float value) => $"{Math.Round(value * 100):0}%";
        public static string Points(float value) => $"{value * 100:+0.#;-0.#;0}pt";

        public static string DefensePurpose(string key)
        {
            switch (key)
            {
                case "mfa": return "盗まれた認証情報だけでは入れない";
                case "firewall": return "外からの不審な通信を制限する";
                case "training": return "怪しい兆候を相談できる職場へ";
                case "waf": return "Webへの攻撃を入り口で止める";
                case "idsIps": return "不審な動きを検知・遮断する";
                case "backup": return "侵入されても、復旧で被害を減らす";
                case "vpn": return "社外からの接続経路を守る";
                default: return "パスワードの使い回しを減らす";
            }
        }

        public static string DefenseDetail(GameState state, string key)
        {
            var def = GameData.Defenses[key];
            int level = state.DefenseLevels.TryGetValue(key, out int v) ? v : 0;
            if (level >= def.Levels.Count) return DefensePurpose(key) + "\n最大レベルまで整備済み";
            var next = def.Levels[level];
            var preview = new GameState(0) { Day = state.Day, Stress = state.Stress };
            foreach (var item in state.DefenseLevels) preview.DefenseLevels[item.Key] = item.Value;
            // 所持金に関係なく「次のレベル」の効果を試算する。
            preview.DefenseLevels[key] = level + 1;
            preview.Stress = Math.Clamp(state.Stress + next.StressCost, 0, 100);
            float reduction = state.EstimateExpectedDamage() - preview.EstimateExpectedDamage();
            string change = (reduction >= 0 ? "-" : "+") + Money.EstimatedYen(Math.Abs(reduction));
            return DefensePurpose(key) + $"\nストレス {Signed(next.StressCost)} / 予測被害 {change}";
        }

        public static string Agenda(GameState state)
        {
            string advice = state.Stress > 60 ? "みんな疲れ気味。教育や休息で負担を下げよう。"
                : state.Trust < 25 ? "相談に向き合って、声をかけやすい職場に。"
                : state.DefenseLevels.Count == 0 ? "最初の備えを選ぼう。社員教育は負担も軽くする。"
                : "守りを重ねよう。ひとつの対策だけでは防げない。";
            return $"{advice}\n\n今期の攻撃発生率  {Rate(state.AttackOccurrenceChance())}\n残り {GameState.TotalPeriods - state.Day + 1} 期を守り抜こう";
        }

        public static string Record(GameState state) =>
            $"相談解決 {state.HelpedColleagues}件   防御成功 {state.DefendedIncidents}件\n防いだ想定損失 {Money.Yen(state.PreventedLoss)}";

        public static string ResultBreakdown(AttackResult result) =>
            $"今回の防御率  {Rate(result.FinalDefenseRate)}  （上限95% / 下限2%）\n" +
            $"設備 {Rate(result.EquipmentRate)}   対応 {Points(result.ResponseBonus)}   タイミング {Points(result.ParryBonus)}\n" +
            $"人望 {Points(result.TrustBonus)}   疲労 {Points(-result.StressPenalty)}\n\n" +
            (result.Defended ? "備えと判断で、この職場の日常を守った。" :
                $"復旧で抑えた損失 {Money.Yen(result.RecoverySavings)}。失敗を責めず、次の備えへ。") +
            $"\n対応費 {Money.Yen(result.ResponseCost)}  / 防御率は成功の保証ではありません";
    }
}
