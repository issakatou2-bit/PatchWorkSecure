using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void Next4Rules_山場報酬は一度だけで旧年度を変更しない()
        {
            foreach (int month in OpsCatalog.PeakMonths)
            foreach (bool prepared in new[] { false, true })
            {
                var s = new OpsState(14, true); var old = new OpsState(14, true) { peakGoalRules = 0 };
                // 両年度を同じ操作で山場の月まで進める。
                for (int m=0; m<month; m++) { s.budget=old.budget=1000; s.stability=old.stability=100; s.BeginIncident(); s.Resolve("scope"); s.NextMonth(); old.BeginIncident(); old.Resolve("scope"); old.NextMonth(); }
                // 途中の報酬差を除外し、今回の確定結果の差だけを比較する。
                s.budget=old.budget=1000; s.stability=old.stability=100; s.trust=old.trust=45; s.fatigue=old.fatigue=0;
                if(prepared) { for(int i=0;i<s.levels.Length;i++) s.levels[i]=old.levels[i]=2; s.culture=old.culture=100; }
                s.BeginIncident(); old.BeginIncident(); Assert.IsTrue(s.Resolve("scope")); Assert.IsTrue(old.Resolve("scope"));
                var result=s.Latest; int peak=s.PeakIndex(month);
                Assert.IsTrue(result.peakGoalRecorded); Assert.IsFalse(old.Latest.peakGoalRecorded);
                Assert.AreEqual(result.loss<=OpsCatalog.PeakLossGoals[peak]&&result.downtime<=OpsCatalog.PeakStopGoals[peak],result.peakGoalMet);
                Assert.AreEqual(old.budget+result.peakBudgetBonus,s.budget);
                Assert.AreEqual(Mathf.Clamp(old.trust+result.peakTrustChange,0,100),s.trust);
                Assert.AreEqual(result.peakGoalMet?OpsCatalog.PeakScoreReward:0,result.peakScoreBonus);
                int budget=s.budget,score=s.AnnualScore; Assert.IsFalse(s.Resolve("scope")); Assert.AreEqual(budget,s.budget); Assert.AreEqual(score,s.AnnualScore);
                var loaded=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(loaded.Valid()); Assert.AreEqual(s.PeakScore,loaded.PeakScore);
                loaded.Latest.peakScoreBonus++; Assert.IsFalse(loaded.Valid());
            }
            var legacy=JsonUtility.FromJson<OpsState>("{\"seed\":14,\"levels\":[0,0,0,0,0,0,0,0,0,0,0]}");
            Assert.IsTrue(legacy.Valid()); Assert.AreEqual(0,legacy.peakGoalRules); Assert.AreEqual(-1,legacy.PeakIndex(2));
        }
        [Test] public void Next4Rules_五段の境界と見込みは公開の幅だけを使う()
        {
            var s=new OpsState(14,true);
            foreach(int threshold in new[]{OpsCatalog.AnnualB,OpsCatalog.AnnualA,OpsCatalog.AnnualS,OpsCatalog.AnnualSS})
                Assert.AreNotEqual(s.RankAtScore(threshold-1),s.RankAtScore(threshold));
            CollectionAssert.AreEqual(new[]{"C","B","A","S","SS"},new[]{0,1100,1450,1750,1950}.Select(s.RankAtScore));
            Assert.AreEqual("A",new OpsState(14){peakGoalRules=0}.RankAtScore(1350));
            foreach(int month in OpsCatalog.PeakMonths)
            {
                int i=s.PeakIndex(month),loss=OpsCatalog.PeakLossGoals[i],stop=OpsCatalog.PeakStopGoals[i];
                Assert.AreEqual(2,s.PeakProspect(month,new OpsEstimate{lossMax=loss,stopMax=stop}));
                Assert.AreEqual(1,s.PeakProspect(month,new OpsEstimate{lossMax=loss+1,stopMax=stop}));
                Assert.AreEqual(0,s.PeakProspect(month,new OpsEstimate{lossMin=loss+1,lossMax=loss+1}));
            }
            Assert.IsFalse(s.history.Any()); Assert.IsFalse(s.nextRankVoicePlayed);
        }
    }
}
