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
    }

    /// <summary>Unityに依存しない年間シミュレーション。月内の乱数引き直しはしない。</summary>
    [Serializable] public class OpsState
    {
        public const int SaveVersion = 1;
        public int version = SaveVersion, seed, month, budget = 76, capacity = 4, stability = 92;
        public int culture = 28, trust = 45, fatigue = 24, totalLoss, totalDowntime;
        public int[] levels = new int[11];
        public OpsPhase phase;
        public bool audited, listened, mapped, rested, proposed;
        public string promiseGroup = "", lastMessage = "今月は4工数あるよ。調査や対話に使う？ それとも改善計画から設備を導入する？";
        public int promiseBaseline;
        public List<string> journal = new List<string>();
        public List<string> learned = new List<string>();
        public List<string> milestones = new List<string>();
        public List<int> completedMissions = new List<int>();
        public List<OpsOutcome> history = new List<OpsOutcome>();
        public OpsState() { }
        public OpsState(int yearSeed) { seed = yearSeed; }
        public OpsMonth Current => OpsCatalog.Months[month];
        public int Level(string id) { int i = OpsCatalog.Index(id); return i < 0 ? 0 : levels[i]; }
        public int Upkeep => OpsCatalog.Projects.Select((p, i) => p.upkeep * levels[i]).Sum();
        public int MonthlyGrant => 20 + trust / 20;
        public int MaxCapacity => 4 + Level("automation");
        public int Evidence => (audited ? 1 : 0) + (listened ? 1 : 0) + (mapped ? 1 : 0);
        public int MissionCount => completedMissions == null ? 0 : completedMissions.Count;
        public bool CurrentMissionCompleted => completedMissions != null && completedMissions.Contains(month);
        public bool IsClear => phase == OpsPhase.Ended && history.Count == 12 && budget >= 0 && stability > 0;
        public OpsOutcome Latest => history.Count == 0 ? null : history[history.Count - 1];
        public int Preparedness => Clamp((Level("inventory") + Level("mfa") + Level("patch") + Level("monitor") + Level("segment")) * 10);
        public int Resilience => Clamp((Level("backup") + Level("drill") + Level("redundancy") + Level("runbook")) * 12);
        public int Organization => (culture + trust + 100 - fatigue) / 3;
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
        private int Severity => Current.@base + (int)(Roll() % 13);
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
        public int Cost(int index) => OpsCatalog.Projects[index].cost + levels[index] * (OpsCatalog.Projects[index].cost / 2);
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
            return copy;
        }
        public string UpgradeBlock(int index)
        {
            if (index < 0 || index >= levels.Length) return "不明な設備です";
            var p = OpsCatalog.Projects[index];
            if (phase != OpsPhase.Planning) return "整備できるのは計画中です";
            if (levels[index] >= p.max) return "運用定着済み";
            if (!string.IsNullOrEmpty(p.requires) && Level(p.requires) == 0) return "先に「" + OpsCatalog.Projects[OpsCatalog.Index(p.requires)].name + "」が必要";
            if (capacity < p.time) return "工数が足りません";
            if (budget < Cost(index)) return "予算が足りません";
            return "";
        }
        public bool Upgrade(int index)
        {
            if (UpgradeBlock(index) != "") return false;
            var p = OpsCatalog.Projects[index];
            budget -= Cost(index); capacity -= p.time; levels[index]++;
            if (p.id == "education") culture = Clamp(culture + 9);
            if (p.id == "mfa") fatigue = Clamp(fatigue + 4);
            if (p.id == "runbook") fatigue = Clamp(fatigue - 5);
            trust = Clamp(trust + 2); Learn(p.term);
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
                case "rest": rested = true; fatigue = Clamp(fatigue - 18); Note("当番を調整して休息。疲労 -18。判断の余裕を取り戻した。"); break;
                case "proposal": proposed = true; promiseGroup = group; promiseBaseline = GroupLevels(group);
                    int grant = 12 + Evidence * 3; budget += grant;
                    Note("根拠を示した提案で追加予算 +" + grant + "万円。今月中に約束した分野の整備を1段階進めよう。"); break;
            }
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
            int center = audited ? Severity : Current.@base + 6;
            var low = Calculate(response, Math.Max(0, center - margin), false);
            var high = Calculate(response, center + margin, false);
            string lowLoss = !string.IsNullOrEmpty(Current.calm) ? "0" : low.loss.ToString();
            int lowStop = !string.IsNullOrEmpty(Current.calm) ? Math.Min(low.downtime, Calculate(response, 0, true).downtime) : low.downtime;
            return "被害 " + lowLoss + "～" + high.loss + "万円 / 停止 " + lowStop + "～" + high.downtime + "h";
        }
        public OpsOutcome Preview(string response) => Calculate(response, Severity, Benign);
        private OpsOutcome Calculate(string response, int severity, bool benign)
        {
            if (!new[] { "contain", "scope", "recover" }.Contains(response)) throw new ArgumentException("不明な対応");
            int prevention = Prevention();
            int containment = (Current.kind == "ransom" || Current.kind == "supply" || Current.kind == "vulnerability") ? 6 * Level("segment") : 0;
            int responsePower = response == "contain" ? 23 : response == "scope" ?
                5 + 4 * Level("inventory") + 5 * Level("monitor") + culture / 12 + (audited ? 8 : 0) : 5 + 3 * Level("runbook");
            if (Current.kind == "outage" && response == "contain") responsePower = 7;
            int pressure = benign ? 0 : Math.Max(0, severity - prevention - containment - responsePower + fatigue / 15);
            bool dataLoss = Current.kind == "ransom" || Current.kind == "outage";
            int recovery = dataLoss ? 5 * Level("backup") + 3 * Math.Min(Level("backup"), Level("drill")) : 0;
            int loss = Math.Max(0, (pressure + 1) / 2 - recovery);
            int stop = response == "contain" ? 9 : response == "recover" ? 3 : 1;
            int downtime = Math.Max(0, (pressure + 2) / 3 + stop - 3 * Level("redundancy") - (mapped ? 2 : 0)
                - (response == "recover" ? 2 * Level("drill") + 2 * Level("runbook") : 0));
            if (benign && response != "contain") downtime = 0;
            int cost = response == "contain" ? 6 : response == "scope" ? 3 : 4;
            return new OpsOutcome { month = month, loss = loss, downtime = downtime, pressure = pressure,
                prevention = prevention, containment = containment, recovery = recovery, cost = cost,
                response = response, benign = benign,
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
            budget -= result.loss + result.cost;
            stability = Clamp(stability - result.downtime + 4);
            fatigue = Clamp(fatigue + (response == "contain" ? 9 : 5) + result.downtime / 3);
            trust = Clamp(trust + (result.loss <= 3 && result.downtime <= 4 ? 5 : -Math.Max(1, result.downtime / 3)));
            if (proposed)
            {
                bool met = GroupLevels(promiseGroup) > promiseBaseline;
                trust = Clamp(trust + (met ? 5 : -7));
                result.promise = met ? "約束した整備を実施。経営の信頼 +5。" : "今月の投資の約束は未達。経営の信頼 -7。";
            }
            else result.promise = "今月は追加予算の約束なし。";
            totalLoss += result.loss; totalDowntime += result.downtime; history.Add(result); Learn(Current.lesson);
            phase = OpsPhase.Review;
            Note("対応完了 / 被害 " + result.loss + "万円 / 停止 " + result.downtime + "h。");
            return true;
        }
        public bool NextMonth()
        {
            if (phase != OpsPhase.Review) return false;
            if (month == 11 || budget < 0 || stability == 0) { phase = OpsPhase.Ended; return true; }
            month++;
            int income = MonthlyGrant, upkeep = Upkeep;
            budget += income - upkeep;
            fatigue = Clamp(fatigue + 3 - 3 * Level("automation") - 2 * Level("runbook"));
            culture = Clamp(culture + 2 * Level("education") - 1);
            stability = Clamp(stability + 3);
            capacity = MaxCapacity;
            audited = listened = mapped = rested = proposed = false; promiseGroup = ""; promiseBaseline = 0;
            phase = budget < 0 ? OpsPhase.Ended : OpsPhase.Planning;
            Note("月次予算 +" + income + "万円 / 維持費 -" + upkeep + "万円。今月の工数 " + capacity + "。"); CheckMilestones(); return true;
        }
        public bool Valid()
        {
            if (version != SaveVersion || month < 0 || month > 11 || !Enum.IsDefined(typeof(OpsPhase), phase) ||
                budget < -500 || budget > 5000 || capacity < 0 || capacity > 6 ||
                new[] { stability, culture, trust, fatigue }.Any(n => n < 0 || n > 100) ||
                levels == null || levels.Length != OpsCatalog.Projects.Length || levels.Any(n => n < 0 || n > 2) ||
                history == null || history.Count > 12 || journal == null || journal.Count > 250 || learned == null || learned.Count > OpsCatalog.Terms.Length ||
                milestones == null || milestones.Count > 3 || milestones.Any(t => !new[] { "戻せることを確かめた", "ひとりで抱えない運用", "相談が集まる職場" }.Contains(t))) return false;
            if (completedMissions != null && (completedMissions.Count > 12 || completedMissions.Distinct().Count() != completedMissions.Count ||
                completedMissions.Any(m => m < 0 || m > month))) return false;
            if (learned.Any(t => OpsCatalog.Term(t) == null) || journal.Any(t => t == null || t.Length > 1500)) return false;
            if (history.Any(r => r == null || r.month < 0 || r.month > 11 || r.loss < 0 || r.loss > 200 || r.downtime < 0 || r.downtime > 200)) return false;
            if (phase == OpsPhase.Review && (Latest == null || Latest.month != month)) return false;
            if (phase == OpsPhase.Planning || phase == OpsPhase.Incident) return history.Count == month;
            return true;
        }
    }
}
