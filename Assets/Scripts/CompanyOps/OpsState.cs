using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsPhase { Planning, Incident, Review, Ended }

    [Serializable] public class OpsOutcome
    {
        public int month, loss, downtime, pressure, prevention, containment, recovery, cost;
        public int avoidedLoss, avoidedDowntime;
        public bool hasInvestmentComparison;
        public string response, explanation, promise;
        public bool benign;
        public string situationId;
        public bool situationPrepared;
        public int businessLoss, extraFatigue;
        public string recoveryChain;
        public int chainLossReduction, chainDowntimeReduction;
        public List<OpsInvestmentEffect> investmentEffects;
        public OpsResponsePower power;
        public OpsGrowthResult growth;
    }

    [Serializable] public class OpsInvestmentEffect
    {
        public string projectId;
        public int level, avoidedLoss, avoidedDowntime;
    }

    /// <summary>Unityに依存しない年間シミュレーション。月内の乱数引き直しはしない。</summary>
    [Serializable] public partial class OpsState
    {
        public const int SaveVersion = 1;
        public int version = SaveVersion, seed, month, budget = 76, capacity = 4, stability = 92;
        public int culture = 28, trust = 45, fatigue = 24, totalLoss, totalDowntime;
        public int[] levels = new int[11];
        public OpsPhase phase;
        public bool audited, listened, mapped, rested, proposed;
        // 0は旧セーブ。新規年度だけ1にし、進行途中のルール変更を避ける。
        public int situationRules;
        public bool situationPrepared;
        public string promiseGroup = "", lastMessage = "今月は4工数あるよ。調査や対話に使う？ それとも改善計画から設備を導入する？";
        public int promiseBaseline, proposalGrant;
        public List<string> journal = new List<string>();
        public List<string> learned = new List<string>();
        public List<string> milestones = new List<string>();
        public List<int> completedMissions = new List<int>();
        public List<OpsOutcome> history = new List<OpsOutcome>();
        public OpsState() { }
        public OpsState(int yearSeed) { seed = yearSeed; situationRules = 1; growthRules = 1; staffExperience = new int[3]; }
        public OpsMonth Current => OpsCatalog.Months[month];
        public OpsSituation Situation => SituationAt(month);
        public OpsSituation SituationAt(int targetMonth)
        {
            if (situationRules == 0 || targetMonth == 0) return OpsCatalog.Situations[0];
            // 攻撃判定と別の乱数系列。4種類を一巡するため同じ事情が連続しない。
            unchecked
            {
                uint n = (uint)seed ^ 0xA341316Cu;
                n ^= n >> 16; n *= 2246822519u; n ^= n >> 13;
                int offset = (int)(n % 4), step = (n & 4) == 0 ? 1 : 3;
                return OpsCatalog.Situations[1 + (offset + (targetMonth - 1) * step) % 4];
            }
        }
        public int SituationFatigue => situationPrepared ? 0 : Math.Max(0, Situation.extraFatigue - 4 * Level("runbook"));
        public int Level(string id) { int i = OpsCatalog.Index(id); return i < 0 ? 0 : levels[i]; }
        public int Upkeep => OpsCatalog.Projects.Select((p, i) => p.upkeep * levels[i]).Sum();
        public int MonthlyGrant => 20 + trust / 20;
        public int MaxCapacity => 4 + Level("automation") + (growthRules > 0 ? monthExtraCapacity + (supportOrder == "routine" ? 1 : 0) : 0);
        public int Evidence => (audited ? 1 : 0) + (listened ? 1 : 0) + (mapped ? 1 : 0);
        public int MissionCount => completedMissions == null ? 0 : completedMissions.Count;
        public bool CurrentMissionCompleted => completedMissions != null && completedMissions.Contains(month);
        public bool IsClear => phase == OpsPhase.Ended && history.Count == 12 && budget >= 0 && stability > 0;
        public OpsOutcome Latest => history.Count == 0 ? null : history[history.Count - 1];
        public int Preparedness => Clamp((Level("inventory") + Level("mfa") + Level("patch") + Level("monitor") + Level("segment")) * 10);
        public int Resilience => Clamp((Level("backup") + Level("drill") + Level("redundancy") + Level("runbook")) * 12);
        public int Organization => (culture + trust + 100 - fatigue) / 3;
        // 保存データの喪失と、性能・処理の停止を区別する。バックアップは混雑の解決には使わない。
        public bool DataRecoveryApplies => OpsCatalog.DataRecoveryMonths.Contains(month);
        public bool RestartApplies => OpsCatalog.RestartMonths.Contains(month);
        public int RestoreChain => Math.Min(Level("backup"), Level("drill"));
        public int RestartChain => Math.Min(Level("automation"), Level("runbook"));
        public string RecoveryReadiness => DataRecoveryApplies ? (RestoreChain > 0 ? "復元連携 Lv." + RestoreChain + " / バックアップ＋訓練" : "復元連携は未整備 / バックアップ＋訓練で成立") :
            RestartApplies ? (RestartChain > 0 ? "再開連携 Lv." + RestartChain + " / 自動化＋手順" : "再開連携は未整備 / 自動化＋手順で成立") : "今回はデータ復元・処理再開の連携対象外";
        public string StaffVoice => Level("education") > 0 && culture >= 65 && (month == 2 || month == 8) ?
            "経理の森さん：「急ぎの依頼も、いつもの連絡先で確認してから相談しています」" : Current.staff;
        public int AnnualScore => Math.Max(0, 1000 - totalLoss * 7 - totalDowntime * 4 +
            MissionCount * 45 + milestones.Count * 30 + (Preparedness + Resilience + Organization) * 2 + Math.Max(0, Math.Min(200, budget)));
        public string Rank => AnnualScore >= 1350 ? "運用ランク A" : AnnualScore >= 750 ? "運用ランク B" : "運用ランク C";

        private bool ActionDone(string action)
        {
            switch (action)
            {
                case "audit": return audited;
                case "listen": return listened;
                case "map": return mapped;
                case "rest": return rested;
                default: return false;
            }
        }
        public void MissionProgress(bool equipment, out int done, out int total)
        {
            var mission = OpsCatalog.Missions[month];
            done = 0; total = 0;
            if (equipment)
            {
                if (!string.IsNullOrEmpty(mission.projectA)) { total++; if (Level(mission.projectA) > 0) done++; }
                if (!string.IsNullOrEmpty(mission.projectB)) { total++; if (Level(mission.projectB) > 0) done++; }
            }
            else
            {
                if (!string.IsNullOrEmpty(mission.actionA)) { total++; if (ActionDone(mission.actionA)) done++; }
                if (!string.IsNullOrEmpty(mission.actionB)) { total++; if (ActionDone(mission.actionB)) done++; }
            }
        }
        public bool MissionReady
        {
            get
            {
                MissionProgress(true, out int equipmentDone, out int equipmentTotal);
                MissionProgress(false, out int fieldDone, out int fieldTotal);
                return (equipmentTotal > 0 && equipmentDone == equipmentTotal) || (fieldTotal > 0 && fieldDone == fieldTotal);
            }
        }

        private static int Clamp(int n) => Math.Max(0, Math.Min(100, n));
        private uint Roll()
        {
            unchecked { uint n = (uint)seed + (uint)(month + 1) * 2654435761u; n ^= n >> 16; n *= 2246822519u; n ^= n >> 13; return n; }
        }
        private int Severity => Current.@base + SeasonPressure + (int)(Roll() % 13);
        private bool Benign => !string.IsNullOrEmpty(Current.calm) && Roll() % 5 == 0;
        private void Learn(string term) { if (!learned.Contains(term)) learned.Add(term); }
        private void Note(string message) { lastMessage = message; journal.Add(Current.name + " / " + message); }
        private void CheckMilestones()
        {
            Award("戻せることを確かめた", Level("backup") > 0 && Level("drill") > 0);
            Award("ひとりで抱えない運用", Level("automation") > 0 && Level("runbook") > 0);
            Award("相談が集まる職場", Level("education") > 0 && culture >= 65);
        }
        private void Award(string title, bool achieved)
        {
            if (!achieved || milestones.Contains(title)) return;
            milestones.Add(title); trust = Clamp(trust + 4);
            Note("会社の成長「" + title + "」を達成。経営の信頼 +4。");
        }
        public int BaseCost(int index) => OpsCatalog.Projects[index].cost + levels[index] * (OpsCatalog.Projects[index].cost / 2);
        public int Cost(int index) => Math.Max(1, BaseCost(index) - (OpsCatalog.Projects[index].group == Situation.group ? Situation.costDiscount : 0));
        public int WorkCost(int index) => Math.Max(1, OpsCatalog.Projects[index].time - (OpsCatalog.Projects[index].group == Situation.group ? Situation.timeDiscount : 0));
        // 購入前の比較専用。予算・履歴・知識・報酬を実際の進行に反映しない。
        public OpsState PreviewUpgrade(int index)
        {
            if (UpgradeBlock(index) != "") return null;
            var copy = CopyForComparison();
            copy.Upgrade(index);
            return copy;
        }
        private OpsState CopyForComparison()
        {
            var copy = (OpsState)MemberwiseClone();
            copy.levels = (int[])levels.Clone();
            copy.journal = new List<string>(journal);
            copy.learned = new List<string>(learned);
            copy.milestones = new List<string>(milestones);
            copy.completedMissions = completedMissions == null ? new List<int>() : new List<int>(completedMissions);
            copy.history = new List<OpsOutcome>(history);
            copy.staffExperience = staffExperience == null ? null : (int[])staffExperience.Clone();
            return copy;
        }
        public string UpgradeBlock(int index)
        {
            if (index < 0 || index >= levels.Length) return "不明な設備です";
            var p = OpsCatalog.Projects[index];
            if (phase != OpsPhase.Planning) return "整備できるのは計画中です";
            if (levels[index] >= p.max) return "運用定着済み";
            if (!string.IsNullOrEmpty(p.requires) && Level(p.requires) == 0) return "先に「" + OpsCatalog.Projects[OpsCatalog.Index(p.requires)].name + "」が必要";
            if (capacity < WorkCost(index)) return "工数が足りません";
            if (budget < Cost(index)) return "予算が足りません";
            return "";
        }
        public bool Upgrade(int index)
        {
            if (UpgradeBlock(index) != "") return false;
            var p = OpsCatalog.Projects[index];
            budget -= Cost(index); capacity -= WorkCost(index); levels[index]++;
            if (p.id == "education") culture = Clamp(culture + 9);
            if (p.id == "mfa") fatigue = Clamp(fatigue + 4);
            if (p.id == "runbook") fatigue = Clamp(fatigue - 5);
            trust = Clamp(trust + 2); Learn(p.term);
            GainFromWork("upgrade", p.id);
            Note(p.name + " Lv." + levels[index] + " / " + p.effect);
            CheckMilestones();
            return true;
        }
        public string ActionBlock(string action)
        {
            if (phase != OpsPhase.Planning) return "計画フェーズで選べます";
            if (capacity < 1) return "今月の工数を使い切りました";
            if (action == "audit") return audited ? "今月は調査済み" : "";
            if (action == "listen") return listened ? "今月は対話済み" : "";
            if (action == "map") return mapped ? "今月は確認済み" : "";
            if (action == "rest") return rested ? "今月は休息済み" : "";
            if (action == "prepare") return string.IsNullOrEmpty(Situation.action) ? "今月は特別な事前調整は不要です" :
                situationPrepared ? "今月は事前調整済み" : Situation.extraFatigue > 0 && SituationFatigue == 0 ? "引継ぎ手順で追加疲労を解消済み" : "";
            if (action == "proposal") return proposed ? "今月は提案済み" : Evidence == 0 ? "調査・対話・業務確認のいずれかが必要" : "";
            return "不明な行動です";
        }
        public bool Act(string action, string group = "recover")
        {
            if (ActionBlock(action) != "") return false;
            if (action == "proposal" && !new[] { "recover", "protect", "people", "operations" }.Contains(group)) return false;
            capacity--;
            switch (action)
            {
                case "audit": audited = true; Note("現状調査：" + Current.finding + " 今月の見積もり幅が狭まった。"); break;
                case "listen": listened = true; culture = Clamp(culture + 7); fatigue = Clamp(fatigue - 3);
                    Note("社員と対話。責めずに受け止め、相談文化 +7。早い報告が限定対応を支える。"); break;
                case "map": mapped = true; trust = Clamp(trust + 4); Learn("asset");
                    Note("止められない仕事と代替手順を確認。対応時の業務停止を2時間短縮できる。"); break;
                case "rest": rested = true; fatigue = Clamp(fatigue - 18);
                    if (Situation.extraFatigue > 0) situationPrepared = true;
                    Note("当番を調整して休息。疲労 -18。" + (Situation.extraFatigue > 0 ? "今月の少人数対応による追加疲労も防ぐ。" : "判断の余裕を取り戻した。")); break;
                case "prepare": situationPrepared = true;
                    if (Situation.extraFatigue > 0) { rested = true; fatigue = Clamp(fatigue - 18); }
                    Note(Situation.action + " / 今月の" + (Situation.stopLossCap > 0 ? "停止に伴う追加損失" : "少人数対応による追加疲労") + "を防ぐ準備ができた。" +
                        (Situation.extraFatigue > 0 ? "休息も確保し、疲労 -18。" : "")); break;
                case "proposal": proposed = true; promiseGroup = group; promiseBaseline = GroupLevels(group);
                    int grant = 12 + Evidence * 3; proposalGrant = grant; budget += grant;
                    Note("根拠を示した提案で追加予算 +" + grant + "万円。今月中に約束した分野の整備を1段階進めよう。"); break;
            }
            GainFromWork(action);
            CheckMilestones(); return true;
        }
        private int GroupLevels(string group) => OpsCatalog.Projects.Select((p, i) => p.group == group ? levels[i] : 0).Sum();
        public bool BeginIncident()
        {
            if (phase != OpsPhase.Planning) return false;
            if (MissionReady && !CurrentMissionCompleted)
            {
                if (completedMissions == null) completedMissions = new List<int>();
                completedMissions.Add(month);
                trust = Clamp(trust + 3);
                Note("社内依頼「" + OpsCatalog.Missions[month].title + "」達成。経営の信頼 +3。");
            }
            phase = OpsPhase.Incident; Note(Current.@event + "。先月までの整備と今月の確認を使って対応しよう。"); return true;
        }
        private int Prevention()
        {
            switch (Current.kind)
            {
                case "identity": return 11 * Level("mfa") + 3 * Level("education");
                case "ransom": return 6 * Level("patch") + 4 * Level("mfa");
                case "vulnerability": return 12 * Level("patch") + 2 * Level("inventory");
                case "social": return 7 * Level("education") + culture / 8 + 2 * Level("runbook");
                case "leak": return 7 * Level("inventory") + 5 * Level("education") + culture / 12;
                case "outage": return 6 * Level("patch") + 5 * Level("automation") + 4 * Level("runbook");
                default: return 4 * Level("monitor") + 3 * Level("inventory");
            }
        }
        // 未調査の見積もりは公開情報のみ。調査後は把握した深刻度の周辺へ幅を絞る。
        public string Forecast(string response)
        {
            int margin = audited ? 2 : Level("monitor") > 0 ? 6 : 9;
            int center = audited ? Severity : Current.@base + SeasonPressure + 6;
            var low = Calculate(response, Math.Max(0, center - margin), false);
            var high = Calculate(response, center + margin, false);
            string lowLoss = (!string.IsNullOrEmpty(Current.calm) ? Math.Min(low.loss, Calculate(response, 0, true).loss) : low.loss).ToString();
            int lowStop = !string.IsNullOrEmpty(Current.calm) ? Math.Min(low.downtime, Calculate(response, 0, true).downtime) : low.downtime;
            return "被害 " + lowLoss + "～" + high.loss + "万円 / 停止 " + lowStop + "～" + high.downtime + "h";
        }
        public OpsOutcome Preview(string response) => Calculate(response, Severity, Benign);
        private OpsOutcome Calculate(string response, int severity, bool benign)
        {
            if (!new[] { "contain", "scope", "recover" }.Contains(response)) throw new ArgumentException("不明な対応");
            int prevention = Prevention();
            int containment = (Current.kind == "ransom" || Current.kind == "supply" || Current.kind == "vulnerability") ? 6 * Level("segment") : 0;
            var power = ResponsePower(response);
            int responsePower = power.Total - prevention - containment;
            int pressure = benign ? 0 : Math.Max(0, severity - power.Total + fatigue / 15);
            bool dataLoss = DataRecoveryApplies;
            int recovery = dataLoss ? 5 * Level("backup") + 3 * Math.Min(Level("backup"), Level("drill")) : 0;
            int loss = Math.Max(0, (pressure + 1) / 2 - recovery);
            int stop = response == "contain" ? 9 : response == "recover" ? 3 : 1;
            int downtime = Math.Max(0, (pressure + 2) / 3 + stop - 3 * Level("redundancy") - (mapped ? 2 : 0)
                - (response == "recover" ? (dataLoss ? 2 * Level("drill") : 0) + 2 * Level("runbook") : 0));
            if (benign && response != "contain") downtime = 0;
            // 基本の封じ込めを省く選択ではない。復旧に人を割ける備えがある場合だけ連携する。
            int chain = response != "recover" || benign ? 0 : dataLoss ? RestoreChain : RestartApplies ? RestartChain : 0;
            int chainLoss = dataLoss ? Math.Min(loss, OpsCatalog.ChainLossPerLevel * chain) : 0;
            int chainStop = Math.Min(downtime, OpsCatalog.ChainStopPerLevel * chain);
            loss -= chainLoss; downtime -= chainStop;
            int businessLoss = situationPrepared ? 0 : Math.Min(Situation.stopLossCap, downtime);
            loss += businessLoss;
            int cost = response == "contain" ? 6 : response == "scope" ? 3 : 4;
            return new OpsOutcome { month = month, loss = loss, downtime = downtime, pressure = pressure,
                prevention = prevention, containment = containment, recovery = recovery, cost = cost,
                response = response, benign = benign, power = power,
                situationId = situationRules == 0 ? null : Situation.id, situationPrepared = situationPrepared,
                businessLoss = businessLoss, extraFatigue = SituationFatigue,
                recoveryChain = chain == 0 ? "" : dataLoss ? "復元連携" : "再開連携",
                chainLossReduction = chainLoss, chainDowntimeReduction = chainStop,
                explanation = benign ? Current.calm : "予防で脅威 -" + prevention + " / 影響限定 -" + containment +
                    " / 対応力 " + responsePower + "。" + (dataLoss ? "復旧の備えでデータ被害 -" + recovery + "万円。" : "この出来事はバックアップだけでは防げない。") };
        }
        public bool Resolve(string response)
        {
            if (phase != OpsPhase.Incident || !new[] { "contain", "scope", "recover" }.Contains(response)) return false;
            var result = Preview(response);
            // 同じ事件・対応・社員の状態で、設備と運用整備の有無だけを比較する。
            var withoutInvestment = CopyForComparison();
            Array.Clear(withoutInvestment.levels, 0, withoutInvestment.levels.Length);
            var baseline = withoutInvestment.Preview(response);
            result.avoidedLoss = baseline.loss - result.loss;
            result.avoidedDowntime = baseline.downtime - result.downtime;
            result.hasInvestmentComparison = true;
            // 各設備を一つだけ外した比較。連携があるため、各行の効果は足し合わせない。
            result.investmentEffects = new List<OpsInvestmentEffect>();
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == 0) continue;
                var withoutOne = CopyForComparison(); withoutOne.levels[i] = 0;
                var comparison = withoutOne.Preview(response);
                result.investmentEffects.Add(new OpsInvestmentEffect { projectId = OpsCatalog.Projects[i].id, level = levels[i],
                    avoidedLoss = comparison.loss - result.loss, avoidedDowntime = comparison.downtime - result.downtime });
            }
            budget -= result.loss + result.cost;
            stability = Clamp(stability - result.downtime + 4);
            fatigue = Clamp(fatigue + (response == "contain" ? 9 : 5) + result.downtime / 3 + result.extraFatigue);
            trust = Clamp(trust + (result.loss <= 3 && result.downtime <= 4 ? 5 : -Math.Max(1, result.downtime / 3)));
            if (proposed)
            {
                bool met = GroupLevels(promiseGroup) > promiseBaseline;
                trust = Clamp(trust + (met ? 5 : -7));
                if (!met) budget -= proposalGrant;
                result.promise = met ? "約束した整備を実施。経営の信頼 +5。" : "整備の約束は未達。信頼 -7。" +
                    (proposalGrant > 0 ? "追加予算 " + proposalGrant + "万円を返却。" : "以前の記録には交付額の記録がありません。");
            }
            else result.promise = "今月は追加予算の約束なし。";
            totalLoss += result.loss; totalDowntime += result.downtime; history.Add(result); Learn(Current.lesson);
            CompleteGrowth(result);
            phase = OpsPhase.Review;
            Note("対応完了 / 被害 " + result.loss + "万円 / 停止 " + result.downtime + "h。");
            return true;
        }
        public bool NextMonth()
        {
            if (phase != OpsPhase.Review) return false;
            if (month == 11 || budget < 0 || stability == 0) { phase = OpsPhase.Ended; return true; }
            // UIでは選択画面を出す。旧操作・自動進行は表示済みの既定値「改善予算」を受け取る。
            if (QuarterRewardPending) ClaimQuarterReward("budget");
            month++;
            int income = MonthlyGrant, upkeep = Upkeep;
            budget += income - upkeep;
            fatigue = Clamp(fatigue + 3 - 3 * Level("automation") - 2 * Level("runbook"));
            culture = Clamp(culture + 2 * Level("education") - 1);
            stability = Clamp(stability + 3);
            supportOrder = ""; practiced = false;
            monthExtraCapacity = nextMonthExtraCapacity; nextMonthExtraCapacity = 0; quarterRewardClaimed = false;
            capacity = MaxCapacity;
            audited = listened = mapped = rested = proposed = false; promiseGroup = ""; promiseBaseline = 0; proposalGrant = 0;
            situationPrepared = false;
            phase = budget < 0 ? OpsPhase.Ended : OpsPhase.Planning;
            Note("月次予算 +" + income + "万円 / 維持費 -" + upkeep + "万円。今月の工数 " + capacity + "。" +
                (situationRules > 0 ? "社内事情「" + Situation.title + "」も確認しよう。" : "")); CheckMilestones(); return true;
        }
        public bool Valid()
        {
            if (version != SaveVersion || situationRules < 0 || situationRules > 1 || month < 0 || month > 11 || !Enum.IsDefined(typeof(OpsPhase), phase) ||
                budget < -500 || budget > 5000 || capacity < 0 || capacity > 8 ||
                proposalGrant < 0 || proposalGrant > 21 || (proposalGrant != 0 && !proposed) ||
                new[] { stability, culture, trust, fatigue }.Any(n => n < 0 || n > 100) ||
                levels == null || levels.Length != OpsCatalog.Projects.Length || levels.Any(n => n < 0 || n > 2) ||
                history == null || history.Count > 12 || journal == null || journal.Count > 250 || learned == null || learned.Count > OpsCatalog.Terms.Length ||
                milestones == null || milestones.Count > 3 || milestones.Any(t => !new[] { "戻せることを確かめた", "ひとりで抱えない運用", "相談が集まる職場" }.Contains(t))) return false;
            if (!ValidGrowth() || capacity > MaxCapacity) return false;
            if (completedMissions != null && (completedMissions.Count > 12 || completedMissions.Distinct().Count() != completedMissions.Count ||
                completedMissions.Any(m => m < 0 || m > month))) return false;
            if (situationPrepared && (situationRules == 0 || string.IsNullOrEmpty(Situation.action))) return false;
            if (learned.Any(t => OpsCatalog.Term(t) == null) || journal.Any(t => t == null || t.Length > 1500)) return false;
            if (history.Any(r => r == null || r.month < 0 || r.month > 11 || r.loss < 0 || r.loss > 200 || r.downtime < 0 || r.downtime > 200 ||
                r.businessLoss < 0 || r.businessLoss > 6 || r.businessLoss > r.loss || r.extraFatigue < 0 || r.extraFatigue > 8 ||
                r.chainLossReduction < 0 || r.chainLossReduction > 6 || r.chainDowntimeReduction < 0 || r.chainDowntimeReduction > 6 ||
                (!string.IsNullOrEmpty(r.recoveryChain) && r.recoveryChain != "復元連携" && r.recoveryChain != "再開連携") ||
                (r.investmentEffects != null && (r.investmentEffects.Count > levels.Length ||
                    r.investmentEffects.Select(e => e == null ? null : e.projectId).Distinct().Count() != r.investmentEffects.Count ||
                    r.investmentEffects.Any(e => e == null || OpsCatalog.Index(e.projectId) < 0 || e.level < 1 || e.level > 2 ||
                        e.avoidedLoss < 0 || e.avoidedLoss > 200 || e.avoidedDowntime < 0 || e.avoidedDowntime > 200))) ||
                (!string.IsNullOrEmpty(r.situationId) && !OpsCatalog.Situations.Any(s => s.id == r.situationId)))) return false;
            if (phase == OpsPhase.Review && (Latest == null || Latest.month != month)) return false;
            if (phase == OpsPhase.Planning || phase == OpsPhase.Incident) return history.Count == month;
            return true;
        }
    }
}
