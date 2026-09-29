using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsState
    {
        public int PeakIndex(int targetMonth) => peakGoalRules > 0 ? Array.IndexOf(OpsCatalog.PeakMonths, targetMonth) : -1;
        public bool HasPeakGoal => PeakIndex(month) >= 0;
        public int PeakScore => peakGoalRules > 0 && history != null ? history.Sum(r => r.peakScoreBonus) : 0;
        public int PeakMedals => history == null ? 0 : history.Count(r => r.peakGoalRecorded && r.peakGoalMet);
        public string RankCode => RankAtScore(AnnualScore);
        public string RankAtScore(int score)
        {
            if (peakGoalRules == 0) return score >= OpsCatalog.LegacyAnnualA ? "A" : score >= OpsCatalog.LegacyAnnualB ? "B" : "C";
            return score >= OpsCatalog.AnnualSS ? "SS" : score >= OpsCatalog.AnnualS ? "S" : score >= OpsCatalog.AnnualA ? "A" : score >= OpsCatalog.AnnualB ? "B" : "C";
        }
        public int NextRankPoints
        {
            get
            {
                var thresholds = peakGoalRules > 0 ? new[] { OpsCatalog.AnnualB, OpsCatalog.AnnualA, OpsCatalog.AnnualS, OpsCatalog.AnnualSS } : new[] { OpsCatalog.LegacyAnnualB, OpsCatalog.LegacyAnnualA };
                return thresholds.Where(n => n > AnnualScore).Select(n => n - AnnualScore).DefaultIfEmpty(0).First();
            }
        }
        public string RankProgress => NextRankPoints > 0 ? "次のランクまであと " + NextRankPoints + " 点" : "最高ランク到達";
        public string PeakGoalText(int targetMonth)
        {
            int i = PeakIndex(targetMonth);
            return i < 0 ? "この年度は山場の目標なし" : OpsCatalog.Months[targetMonth].name + "の山場　目標 被害" + OpsCatalog.PeakLossGoals[i] + "万円以下・停止" + OpsCatalog.PeakStopGoals[i] + "h以下";
        }
        // 公開見積もりの幅だけで判定。未来の抽選済み事件・確定被害は参照しない。
        // 未来の目標にも現在の公開見積もりを当てはめる。事件や今後の成長は未反映。
        public int PeakProspect(int targetMonth, OpsEstimate estimate)
        {
            int i = PeakIndex(targetMonth);
            if (i < 0 || estimate == null) return -1;
            if (estimate.lossMax <= OpsCatalog.PeakLossGoals[i] && estimate.stopMax <= OpsCatalog.PeakStopGoals[i]) return 2;
            return estimate.lossMin <= OpsCatalog.PeakLossGoals[i] && estimate.stopMin <= OpsCatalog.PeakStopGoals[i] ? 1 : 0;
        }
        public static string PeakProspectText(int prospect) => prospect == 2 ? "目標内の見込み" : prospect == 1 ? "届く可能性あり" : "届きにくい見込み";
        void ApplyPeakGoal(OpsOutcome result)
        {
            int i = PeakIndex(result.month);
            if (i < 0) return;
            result.peakGoalRecorded = true;
            result.peakGoalMet = result.loss <= OpsCatalog.PeakLossGoals[i] && result.downtime <= OpsCatalog.PeakStopGoals[i];
            result.peakTrustChange = result.peakGoalMet ? OpsCatalog.PeakTrustReward : -OpsCatalog.PeakTrustPenalty;
            result.peakBudgetBonus = result.peakGoalMet ? OpsCatalog.PeakBudgetReward : 0;
            result.peakScoreBonus = result.peakGoalMet ? OpsCatalog.PeakScoreReward : 0;
            trust = Clamp(trust + result.peakTrustChange);
            budget += result.peakBudgetBonus;
        }
        bool ValidPeaks()
        {
            if (peakGoalRules < 0 || peakGoalRules > OpsCatalog.PeakRulesVersion || history == null) return false;
            if (peakGoalRules == 0 && nextRankVoicePlayed) return false;
            foreach (var r in history)
            {
                if (r == null) return false;
                int i = PeakIndex(r.month);
                if (r.peakGoalRecorded != (i >= 0)) return false;
                if (i < 0)
                {
                    if (r.peakGoalMet || r.peakTrustChange != 0 || r.peakBudgetBonus != 0 || r.peakScoreBonus != 0) return false;
                    continue;
                }
                bool met = r.loss <= OpsCatalog.PeakLossGoals[i] && r.downtime <= OpsCatalog.PeakStopGoals[i];
                if (r.peakGoalMet != met || r.peakTrustChange != (met ? OpsCatalog.PeakTrustReward : -OpsCatalog.PeakTrustPenalty) ||
                    r.peakBudgetBonus != (met ? OpsCatalog.PeakBudgetReward : 0) || r.peakScoreBonus != (met ? OpsCatalog.PeakScoreReward : 0)) return false;
            }
            return history.Where(r => r.peakGoalRecorded).Select(r => r.month).Distinct().Count() == history.Count(r => r.peakGoalRecorded);
        }
    }
}
