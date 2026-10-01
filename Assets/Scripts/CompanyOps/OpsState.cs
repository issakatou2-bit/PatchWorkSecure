using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsPhase { Planning, Incident, Review, Ended }

    // 幅はシナリオ上の見積もりで、確率分布・信頼区間ではない。対応費は被害と分ける。
    [Serializable] public sealed class OpsEstimate
    {
        public int lossMin, lossMax, stopMin, stopMax, cost;
    }

    [Serializable] public class OpsOutcome
    {
        public int month, loss, downtime, pressure, prevention, containment, recovery, cost;
        public int avoidedLoss, avoidedDowntime;
        public bool hasInvestmentComparison;
        public string response, explanation, promise;
        public string eventId, eventTitle, lessonId, ticketId, ticketMode;
        public bool benign;
        public string situationId;
        public bool situationPrepared;
        public int businessLoss, extraFatigue;
        public string recoveryChain;
        public int chainLossReduction, chainDowntimeReduction;
        public List<OpsInvestmentEffect> investmentEffects;
        // 確定後だけ表示する仮定比較。報酬・購入・経験には反映しない。旧記録はnull。
        public List<OpsInvestmentEffect> potentialInvestmentEffects;
        public OpsResponsePower power;
        public OpsGrowthResult growth;
        // 表示専用の月初・対応後スナップショット。旧セーブのnullは未記録のまま。
        public int[] metricsBefore, metricsAfter;
        public OpsEstimate forecast;
        public bool hasClosingState;
        public int closingBudget, closingStability;
        public int missionBonus;
        // 山場の確定結果。旧記録はfalse/0のまま保存する。
        public bool peakGoalRecorded, peakGoalMet;
        public int peakTrustChange, peakBudgetBonus, peakScoreBonus;
        // 旧記録のfalseは点数なし。表示・計算とも50点として扱う。
        public bool minigameRecorded, delegated;
        public int minigameScore;
        public int EffectiveMinigameScore=>minigameRecorded?minigameScore:OpsCatalog.MinigameDelegateScore;
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
        public int auditKnowledgeAdjustment;
        // 0は旧セーブ。新規年度だけ1にし、進行途中のルール変更を避ける。
        public int situationRules;
        // 旧年度は0。受諾は目印のみで、達成判定を後付けで制限しない。
        public int missionBudgetRules, missionBudgetPaid, acceptedMissionMonth=-1;
        public int rankBenefitRules, rankQuarterBonusPaid;
        public int peakGoalRules;
        public bool nextRankVoicePlayed;
        public int ProposalRankBonus=>rankBenefitRules>0&&trust>=50?1:0;
        public int QuarterTrustBonus=>rankBenefitRules>0&&trust>=65?1:0;
        public bool CultureEarlySignal=>rankBenefitRules>0&&culture>=50&&month<11;
        public int ForecastRankPrecision=>rankBenefitRules>0&&culture>=65&&!audited?1:0;
        public int EstimateMargin=>(audited?2:Level("monitor")>0?6:9)-ForecastRankPrecision;
        public const int MissionBudgetReward=1;
        public int MissionBudgetOffer=>missionBudgetRules>0?MissionBudgetReward:0;
        public bool situationPrepared;
        public string promiseGroup = "", lastMessage = "今月は4工数あるよ。調査や対話に使う？ それとも改善計画から設備を導入する？";
        public int promiseBaseline, proposalGrant;
        public List<string> journal = new List<string>();
        public List<string> learned = new List<string>();
        public List<string> milestones = new List<string>();
        public List<int> completedMissions = new List<int>();
        public List<OpsOutcome> history = new List<OpsOutcome>();
        public int[] monthStartMetrics;
        public OpsState() { }
        public OpsState(int yearSeed) { seed = yearSeed; situationRules = 1; growthRules = 1; missionBudgetRules=1;rankBenefitRules=1;peakGoalRules=OpsCatalog.PeakRulesVersion;InitializeDecisionDepth(yearSeed);InitializeBubbles(yearSeed); staffExperience = new int[3]; monthStartMetrics = ReportMetrics; }
        public int[] ReportMetrics => new[] { Preparedness, Resilience, culture, trust, 100-fatigue, Organization };
        public OpsMonth Current => MonthAt(month);
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
        public bool DataRecoveryApplies => CurrentProfile == null ? OpsCatalog.DataRecoveryMonths.Contains(month) : CurrentProfile.dataRecovery;
        public bool RestartApplies => CurrentProfile == null ? OpsCatalog.RestartMonths.Contains(month) : CurrentProfile.restart;
        public int RestoreChain => Math.Min(Level("backup"), Level("drill"));
        public int RestartChain => Math.Min(Level("automation"), Level("runbook"));
        public string RecoveryReadiness => DataRecoveryApplies ? (RestoreChain > 0 ? "復元連携 Lv." + RestoreChain + " / バックアップ＋訓練" : "復元連携は未整備 / バックアップ＋訓練で成立") :
            RestartApplies ? (RestartChain > 0 ? "再開連携 Lv." + RestartChain + " / 自動化＋手順" : "再開連携は未整備 / 自動化＋手順で成立") : "今回はデータ復元・処理再開の連携対象外";
        public string StaffVoice => Level("education") > 0 && culture >= 65 && (CurrentProfile == null ? Current.kind == "social" : CurrentProfile.id == "bec") ?
            "経理の森さん：「急ぎの依頼も、いつもの連絡先で確認してから相談しています」" : Current.staff;
        public int AnnualScore => Math.Max(0, 1000 - totalLoss * 7 - totalDowntime * 4 +
            MissionCount * 45 + milestones.Count * OpsCatalog.GrowthScoreReward + (Preparedness + Resilience + Organization) * 2 + Math.Max(0, Math.Min(200, budget)) + PeakScore);
        public string Rank => "運用ランク " + RankCode;

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
            var mission = CurrentMission;
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
            foreach(var goal in GrowthGoals)Award(goal.name,GrowthSteps(goal).All(done=>done));
        }
        public OpsCatalog.GrowthGoal[] GrowthGoals=>OpsCatalog.GrowthGoals[Math.Max(1,Math.Min(OpsCatalog.StoryYears,storyCalendarYear))-1];
        public bool[] GrowthSteps(OpsCatalog.GrowthGoal goal)
        {
            var steps=new List<bool>{Level(goal.projectA)>=goal.levelA};
            if(!string.IsNullOrEmpty(goal.projectB))steps.Add(Level(goal.projectB)>=goal.levelB);
            if(goal.culture>0)steps.Add(culture>=goal.culture);
            if(goal.staffLevel>0)steps.Add(goal.allStaff?Enumerable.Range(0,3).All(i=>StaffLevel(i)>=goal.staffLevel):Enumerable.Range(0,3).Any(i=>StaffLevel(i)>=goal.staffLevel));
            return steps.ToArray();
        }
        private void Award(string title, bool achieved)
        {
            if (!achieved || milestones.Contains(title)) return;
            milestones.Add(title); trust = Clamp(trust + OpsCatalog.GrowthTrustReward);
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
        public bool Act(string action, string group = "recover", int workScore = OpsCatalog.MinigameDelegateScore)
        {
            if (ActionBlock(action) != "" || workScore<0 || workScore>OpsCatalog.MinigameMaxScore) return false;
            if (action == "proposal" && !new[] { "recover", "protect", "people", "operations" }.Contains(group)) return false;
            capacity--;
            switch (action)
            {
                case "audit": audited = true;
                    auditKnowledgeAdjustment=workScore>=OpsCatalog.AuditHighScore?-OpsCatalog.AuditExtraKnowledge:
                        workScore<OpsCatalog.AuditLowScore?OpsCatalog.UnauditedBlindness-OpsCatalog.AuditLowRecovery:0;
                    Note("現状調査：" + Current.finding + " 今月の見積もり幅が狭まった。"); break;
                case "listen": listened = true; culture = Clamp(culture + 7); fatigue = Clamp(fatigue - 3);
                    Note("社員と対話。責めずに受け止め、相談文化 +7。早い報告が限定対応を支える。"); break;
                case "map": mapped = true; trust = Clamp(trust + (workScore>=OpsCatalog.MapHighScore?OpsCatalog.MapHighTrust:workScore<OpsCatalog.MapLowScore?OpsCatalog.MapLowTrust:OpsCatalog.MapNormalTrust)); Learn("asset");
                    Note("止められない仕事と代替手順を確認。対応時の業務停止を2時間短縮できる。"); break;
                case "rest": rested = true; fatigue = Clamp(fatigue - 18);
                    if (Situation.extraFatigue > 0) situationPrepared = true;
                    Note("当番を調整して休息。疲労 -18。" + (Situation.extraFatigue > 0 ? "今月の少人数対応による追加疲労も防ぐ。" : "判断の余裕を取り戻した。")); break;
                case "prepare": situationPrepared = true;
                    if (Situation.extraFatigue > 0) { rested = true; fatigue = Clamp(fatigue - 18); }
                    Note(Situation.action + " / 今月の" + (Situation.stopLossCap > 0 ? "停止に伴う追加損失" : "少人数対応による追加疲労") + "を防ぐ準備ができた。" +
                        (Situation.extraFatigue > 0 ? "休息も確保し、疲労 -18。" : "")); break;
                case "proposal": proposed = true; promiseGroup = group; promiseBaseline = GroupLevels(group);
                    int grant = 12 + Evidence * 3+ProposalRankBonus; proposalGrant = grant; budget += grant;
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
                missionBudgetPaid=MissionBudgetOffer;budget+=missionBudgetPaid;
                Note("社内依頼「" + CurrentMission.title + "」達成。経営の信頼 +3。");
            }
            phase = OpsPhase.Incident; Note(Current.@event + "。先月までの整備と今月の確認を使って対応しよう。"); return true;
        }
        private int Prevention()
        {
            if (CurrentProfile != null) return CurrentProfile.prevention.Select((weight, i) => weight * levels[i]).Sum() + CulturalPower;
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
        // 公開情報を中心に、調査で幅を絞る。旧保存でも未確認のSeverityを参照しない。
        public string Forecast(string response)
        {
            var estimate = Estimate(response);
            return "被害 " + estimate.lossMin + "～" + estimate.lossMax + "万円 / 停止 " + estimate.stopMin + "～" + estimate.stopMax + "h";
        }
        // 表示側で文章を分解せず、同じ公開情報の見積もりを数値・図・文章へ使う。
        // Previewは未確認の真相も使うため、購入前・対応前の画面には使わない。
        public OpsEstimate Estimate(string response)
        {
            int margin = EstimateMargin;
            int center = Current.@base + SeasonPressure + OpsCatalog.ForecastCenterOffset;
            var low = Calculate(response, Math.Max(0, center - margin), false);
            var high = Calculate(response, center + margin, false);
            int lowLoss = !string.IsNullOrEmpty(Current.calm) ? Math.Min(low.loss, Calculate(response, 0, true).loss) : low.loss;
            int lowStop = !string.IsNullOrEmpty(Current.calm) ? Math.Min(low.downtime, Calculate(response, 0, true).downtime) : low.downtime;
            return new OpsEstimate { lossMin = lowLoss, lossMax = high.loss, stopMin = lowStop, stopMax = high.downtime, cost = high.cost };
        }
        public OpsOutcome Preview(string response) => Calculate(response, Severity, Benign);
        // 対応前の札は公開された見積もり同士で判断し、未確認の真相を漏らさない。
        public OpsEstimate EstimateWithoutProject(int index, string response)
        {
            if (index < 0 || index >= levels.Length) throw new ArgumentOutOfRangeException(nameof(index));
            var copy = CopyForComparison(); copy.levels[index] = 0;
            return copy.Estimate(response);
        }
        private OpsOutcome Calculate(string response, int severity, bool benign)
        {
            if (!new[] { "contain", "scope", "recover" }.Contains(response)) throw new ArgumentException("不明な対応");
            int prevention = Prevention();
            int containment = ContainmentPower;
            var power = ResponsePower(response);
            int responsePower = power.Total - prevention - containment;
            int pressure = benign ? 0 : Math.Max(0, severity - power.Total + fatigue / 15);
            bool dataLoss = DataRecoveryApplies;
            int recovery = dataLoss ? 5 * Level("backup") + 3 * Math.Min(Level("backup"), Level("drill")) : 0;
            int loss = Math.Max(0, (pressure + 1) / 2 - recovery);
            if(!benign&&response=="scope")loss+=ScopeOversight;
            int stop = response == "contain" ? decisionDepthRules==0?OpsCatalog.ContainStop:IncidentTime>0?OpsCatalog.QuietContainStop:Math.Max(OpsCatalog.MinimumContainStop,OpsCatalog.ContainStop-OpsCatalog.SegmentContainStopCut*Level("segment")) : response == "recover" ? OpsCatalog.RecoverStop : OpsCatalog.ScopeStop;
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
            int cost = response == "contain" ? OpsCatalog.ContainCost : response == "scope" ? OpsCatalog.ScopeCost+Blindness*OpsCatalog.ScopeCostPerBlind : decisionDepthRules==0?OpsCatalog.LegacyRecoverCost:OpsCatalog.RecoverCost;
            return new OpsOutcome { month = month, loss = loss, downtime = downtime, pressure = pressure,
                eventId=CurrentEvent?.id, eventTitle=CurrentEvent?.title, lessonId=CurrentEvent==null ? null : Current.lesson,
                ticketId=Ticket?.id, ticketMode=Ticket==null ? null : string.IsNullOrEmpty(ticketResolution) ? "defer" : ticketResolution,
                prevention = prevention, containment = containment, recovery = recovery, cost = cost,
                response = response, benign = benign, power = power,
                situationId = situationRules == 0 ? null : Situation.id, situationPrepared = situationPrepared,
                businessLoss = businessLoss, extraFatigue = SituationFatigue,
                recoveryChain = chain == 0 ? "" : dataLoss ? "復元連携" : "再開連携",
                chainLossReduction = chainLoss, chainDowntimeReduction = chainStop,
                explanation = benign ? Current.calm : (eventRules == 1 ? "事前の備えで影響 -" : "予防で脅威 -") + prevention + " / 影響限定 -" + containment +
                    " / 対応力 " + responsePower + "。" + (dataLoss ? "復旧の備えでデータ被害 -" + recovery + "万円。" : "この出来事はバックアップだけでは防げない。") };
        }
        // 真相を使うのは対応確定後だけ。見積もり自体は変更しない。
        private OpsOutcome ScoredPreview(string response,int score,OpsEstimate publicRange=null)
        {
            var result=Preview(response);
            if(score==OpsCatalog.MinigameDelegateScore)return result;
            var range=publicRange??Estimate(response);
            double factor=1-OpsCatalog.MinigameResultInfluence*(score-OpsCatalog.MinigameDelegateScore)/OpsCatalog.MinigameDelegateScore;
            if(!SupportsRestore)result.loss=BoundMinigameResult(result.loss,range.lossMin,range.lossMax,factor);
            result.downtime=BoundMinigameResult(result.downtime,range.stopMin,range.stopMax,factor);
            result.businessLoss=Math.Min(result.businessLoss,result.loss);
            return result;
        }
        private static int BoundMinigameResult(int original,int low,int high,double factor)=>
            Math.Max(Math.Min(original,low),Math.Min(Math.Max(original,high),(int)Math.Round(original*factor,MidpointRounding.AwayFromZero)));
        public bool SupportsContainment=>EventSpread==OpsCatalog.SpreadHigh;
        public bool SupportsMinigame=>SupportsContainment||SupportsMail||SupportsMfa||SupportsRestore;
        public bool Resolve(string response)=>Resolve(response,OpsCatalog.MinigameDelegateScore,true);
        public bool Resolve(string response,int score,bool delegated)
        {
            if (phase != OpsPhase.Incident || !new[] { "contain", "scope", "recover" }.Contains(response) ||
                score<0 || score>OpsCatalog.MinigameMaxScore || delegated&&score!=OpsCatalog.MinigameDelegateScore || !SupportsMinigame&&score!=OpsCatalog.MinigameDelegateScore) return false;
            var result = ScoredPreview(response,score);
            if(SupportsContainment||(SupportsMail||SupportsMfa||SupportsRestore)&&!delegated){result.minigameRecorded=true;result.minigameScore=score;result.delegated=delegated;}
            result.forecast = Estimate(response);
            result.metricsBefore = monthStartMetrics == null ? null : (int[])monthStartMetrics.Clone();
            // 同じ事件・対応・社員・点数・公開幅で、設備と運用整備の有無だけを比較する。
            var withoutInvestment = CopyForComparison();
            Array.Clear(withoutInvestment.levels, 0, withoutInvestment.levels.Length);
            var baseline = withoutInvestment.ScoredPreview(response,score,result.forecast);
            result.avoidedLoss = baseline.loss - result.loss;
            result.avoidedDowntime = baseline.downtime - result.downtime;
            result.hasInvestmentComparison = true;
            // 各設備を一つだけ外した比較。連携があるため、各行の効果は足し合わせない。
            result.investmentEffects = new List<OpsInvestmentEffect>();
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == 0) continue;
                var withoutOne = CopyForComparison(); withoutOne.levels[i] = 0;
                var comparison = withoutOne.ScoredPreview(response,score,result.forecast);
                result.investmentEffects.Add(new OpsInvestmentEffect { projectId = OpsCatalog.Projects[i].id, level = levels[i],
                    avoidedLoss = comparison.loss - result.loss, avoidedDowntime = comparison.downtime - result.downtime });
            }
            result.potentialInvestmentEffects = new List<OpsInvestmentEffect>();
            for (int i = 0; i < levels.Length; i++)
            {
                var project = OpsCatalog.Projects[i];
                if (levels[i] > 0 || (!string.IsNullOrEmpty(project.requires) && Level(project.requires) == 0)) continue;
                var withOne = CopyForComparison(); withOne.levels[i] = 1;
                var comparison = withOne.ScoredPreview(response,score,result.forecast);
                int loss = result.loss - comparison.loss, stop = result.downtime - comparison.downtime;
                if (loss > 0 || stop > 0) result.potentialInvestmentEffects.Add(new OpsInvestmentEffect {
                    projectId = project.id, level = 1, avoidedLoss = loss, avoidedDowntime = stop });
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
            ApplyPeakGoal(result);
            result.metricsAfter = ReportMetrics;
            result.missionBonus=missionBudgetPaid;
            result.hasClosingState=true;result.closingBudget=budget;result.closingStability=stability;
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
            audited = listened = mapped = rested = proposed = false; auditKnowledgeAdjustment=0; promiseGroup = ""; promiseBaseline = 0; proposalGrant = 0;
            situationPrepared = false;
            ticketResolution = "";
            clueCollected=false;
            missionBudgetPaid=0;
            rankQuarterBonusPaid=0;
            phase = budget < 0 ? OpsPhase.Ended : OpsPhase.Planning;
            Note("月次予算 +" + income + "万円 / 維持費 -" + upkeep + "万円。今月の工数 " + capacity + "。" +
                (situationRules > 0 ? "社内事情「" + Situation.title + "」も確認しよう。" : "")); CheckMilestones(); monthStartMetrics = ReportMetrics; return true;
        }
        public bool Valid()
        {
            if(missionBudgetRules<0||missionBudgetRules>1||missionBudgetPaid<0||missionBudgetPaid>MissionBudgetReward||acceptedMissionMonth < -1||acceptedMissionMonth>month)return false;
            if(rankBenefitRules<0||rankBenefitRules>1||rankQuarterBonusPaid<0||rankQuarterBonusPaid>1||rankQuarterBonusPaid>0&&(!quarterRewardClaimed||!QuarterPeak))return false;
            if (version != SaveVersion || situationRules < 0 || situationRules > 1 || month < 0 || month > 11 || !Enum.IsDefined(typeof(OpsPhase), phase) ||
                budget < -500 || budget > (yearPressure==0?OpsCatalog.LegacySaveBudgetLimit:OpsCatalog.StorySaveBudgetLimit) || capacity < 0 || capacity > 8 ||
                proposalGrant < 0 || proposalGrant > 21+(rankBenefitRules>0?1:0) || (proposalGrant != 0 && !proposed) ||
                new[] { stability, culture, trust, fatigue }.Any(n => n < 0 || n > 100) ||
                levels == null || levels.Length != OpsCatalog.Projects.Length || levels.Any(n => n < 0 || n > 2) ||
                history == null || history.Count > 12 || journal == null || journal.Count > 250 || learned == null || learned.Count > OpsCatalog.Terms.Length ||
                milestones == null || milestones.Count > 3 || milestones.Distinct().Count()!=milestones.Count || milestones.Any(t => !GrowthGoals.Any(g=>g.name==t))) return false;
            if (auditKnowledgeAdjustment < -OpsCatalog.AuditExtraKnowledge || auditKnowledgeAdjustment > OpsCatalog.UnauditedBlindness-OpsCatalog.AuditLowRecovery || !audited&&auditKnowledgeAdjustment!=0) return false;
            if (!ValidGrowth() || !ValidEvents() || !ValidDecisionDepth() || !ValidBubbles() || !ValidPeaks() || capacity > MaxCapacity || !ValidReportMetrics(monthStartMetrics)) return false;
            if (completedMissions != null && (completedMissions.Count > 12 || completedMissions.Distinct().Count() != completedMissions.Count ||
                completedMissions.Any(m => m < 0 || m > month))) return false;
            if (situationPrepared && (situationRules == 0 || string.IsNullOrEmpty(Situation.action))) return false;
            if (learned.Any(t => OpsCatalog.Term(t) == null) || journal.Any(t => t == null || t.Length > 1500)) return false;
            if (history.Any(r => r == null || r.month < 0 || r.month > 11 || r.loss < 0 || r.loss > 200 || r.downtime < 0 || r.downtime > 200 ||
                r.missionBonus<0||r.missionBonus>MissionBudgetReward||
                (r.minigameRecorded && (r.minigameScore<0 || r.minigameScore>OpsCatalog.MinigameMaxScore || r.delegated&&r.minigameScore!=OpsCatalog.MinigameDelegateScore)) ||
                !ValidReportMetrics(r.metricsBefore) || !ValidReportMetrics(r.metricsAfter) ||
                (r.hasClosingState && (r.closingBudget < -500 || r.closingBudget > (yearPressure==0?OpsCatalog.LegacySaveBudgetLimit:OpsCatalog.StorySaveBudgetLimit) || r.closingStability < 0 || r.closingStability > 100)) ||
                (r.forecast != null && (r.forecast.lossMin < 0 || r.forecast.lossMax < r.forecast.lossMin || r.forecast.lossMax > 200 || r.forecast.stopMin < 0 || r.forecast.stopMax < r.forecast.stopMin || r.forecast.stopMax > 200 || r.forecast.cost < 0 || r.forecast.cost > 200)) ||
                r.businessLoss < 0 || r.businessLoss > 6 || r.businessLoss > r.loss || r.extraFatigue < 0 || r.extraFatigue > 8 ||
                r.chainLossReduction < 0 || r.chainLossReduction > 6 || r.chainDowntimeReduction < 0 || r.chainDowntimeReduction > 6 ||
                (!string.IsNullOrEmpty(r.recoveryChain) && r.recoveryChain != "復元連携" && r.recoveryChain != "再開連携") ||
                (r.potentialInvestmentEffects != null && (r.potentialInvestmentEffects.Count > levels.Length ||
                    r.potentialInvestmentEffects.Select(e => e == null ? null : e.projectId).Distinct().Count() != r.potentialInvestmentEffects.Count ||
                    r.potentialInvestmentEffects.Any(e => e == null || OpsCatalog.Index(e.projectId) < 0 || e.level != 1 ||
                        e.avoidedLoss < 0 || e.avoidedLoss > 200 || e.avoidedDowntime < 0 || e.avoidedDowntime > 200 ||
                        (e.avoidedLoss == 0 && e.avoidedDowntime == 0)))) ||
                (r.investmentEffects != null && (r.investmentEffects.Count > levels.Length ||
                    r.investmentEffects.Select(e => e == null ? null : e.projectId).Distinct().Count() != r.investmentEffects.Count ||
                    r.investmentEffects.Any(e => e == null || OpsCatalog.Index(e.projectId) < 0 || e.level < 1 || e.level > 2 ||
                        e.avoidedLoss < 0 || e.avoidedLoss > 200 || e.avoidedDowntime < 0 || e.avoidedDowntime > 200))) ||
                (!string.IsNullOrEmpty(r.situationId) && !OpsCatalog.Situations.Any(s => s.id == r.situationId)))) return false;
            if (phase == OpsPhase.Review && (Latest == null || Latest.month != month)) return false;
            if (phase == OpsPhase.Planning || phase == OpsPhase.Incident) return history.Count == month;
            return true;
        }
        private static bool ValidReportMetrics(int[] values) => values == null || values.Length == 6 && values.All(v=>v>=0 && v<=100);
    }
}
