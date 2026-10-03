#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void FinishEndlessTestYear(OpsState s)
        {
            s.budget=1000;s.culture=s.trust=100;s.fatigue=0;for(int i=0;i<s.levels.Length;i++)s.levels[i]=s.EquipmentAvailable(i)?2:0;
            while(s.phase!=OpsPhase.Ended){s.stability=100;DiaryTestMonth(s);if(s.QuarterRewardPending)s.ClaimQuarterReward("budget");Assert.IsTrue(s.NextMonth());}
            Assert.IsTrue(s.IsClear);Assert.IsTrue(s.Valid());
        }
        [Test] public void Next12Endless_最初の三年は本編と同じ引き継ぎで目標未達でも続く()
        {
            var e=OpsEndless.Begin(14,new[]{"backup"});var s=OpsStory.Begin(14,new[]{"backup"});
            for(int year=1;year<=3;year++)
            {
                Assert.AreEqual(s.state.seed,e.state.seed);Assert.AreEqual(s.state.yearPressure,e.state.yearPressure);CollectionAssert.AreEqual(s.state.eventSchedule,e.state.eventSchedule);CollectionAssert.AreEqual(s.state.levels,e.state.levels);
                FinishStoryTestYear(e.state);FinishStoryTestYear(s.state);Assert.AreEqual(s.state.AnnualScore,e.state.AnnualScore);Assert.AreEqual(s.state.budget,e.state.budget);
                Assert.IsTrue(e.Valid());if(year<3){Assert.IsTrue(e.AdvanceYear());Assert.IsTrue(s.AdvanceYear());}
            }
            Assert.IsTrue(e.AdvanceYear());Assert.AreEqual(4,e.year);Assert.AreEqual(OpsCatalog.EndlessPressure(4),e.state.yearPressure);Assert.IsTrue(e.Valid());
            var weak=OpsEndless.Begin(71,null);FinishStoryTestYear(weak.state);weak.state.totalLoss=1000;Assert.AreEqual("C",weak.state.RankCode);Assert.IsTrue(weak.RecordYear());Assert.IsTrue(weak.CanAdvance);Assert.IsTrue(weak.AdvanceYear());Assert.IsTrue(weak.Valid());
        }
        [Test] public void Next12Endless_全額繰越と四年以降の仲間設備と抽選を保つ()
        {
            var e=OpsEndless.Begin(14,null);
            for(int year=1;year<=5;year++)
            {
                FinishEndlessTestYear(e.state);int budget=e.state.budget;int[] xp=e.state.staffExperience.ToArray();int culture=e.state.culture,trust=e.state.trust;Assert.IsTrue(e.AdvanceYear());
                Assert.AreEqual(budget+OpsCatalog.StoryInitialBudget,e.state.budget);CollectionAssert.AreEqual(xp,e.state.staffExperience.Take(xp.Length));Assert.AreEqual(culture,e.state.culture);Assert.AreEqual((trust+45)/2,e.state.trust);
                Assert.IsTrue(e.Valid());Assert.AreEqual(12,e.state.eventSchedule.Distinct().Count());Assert.AreEqual(OpsCatalog.EndlessPressure(year+1),e.state.yearPressure);
                if(year>=3){Assert.AreEqual(4,e.state.StaffCount);Assert.IsTrue(Enumerable.Range(0,15).All(e.state.EquipmentAvailable));Assert.IsTrue(e.state.levels.All(n=>n==1));Assert.IsTrue(e.state.RequestEngineerResearch());}
            }
            Assert.AreEqual(OpsCatalog.EndlessPressureBase+7*OpsCatalog.EndlessPressureLinear+49*OpsCatalog.EndlessPressureQuadratic,OpsCatalog.EndlessPressure(10));e.state.yearPressure++;Assert.IsFalse(e.Valid());
        }
        [Test] public void Next12Endless_月収の成長は四年目以降だけで実際の入金と保存再開に反映する()
        {
            foreach(int trust in new[]{0,45,100})
            {
                var s=new OpsState(14,true){trust=trust};int oldGrant=20+trust/20;
                for(int year=0;year<=3;year++){s.endlessYear=year;Assert.AreEqual(oldGrant,s.MonthlyGrant);}
                for(int year=4;year<=15;year++){s.endlessYear=year;Assert.AreEqual(oldGrant+(year-3)*OpsCatalog.EndlessMonthlyIncomePerYear,s.MonthlyGrant);}
                s.endlessYear=0;s.storyCalendarYear=3;s.yearPressure=OpsCatalog.GrowthStoryPressures[2];Assert.AreEqual(oldGrant,s.MonthlyGrant,"本編には加算しない");
            }
            var e=OpsEndless.Begin(14,null);for(int y=1;y<4;y++){FinishEndlessTestYear(e.state);e.AdvanceYear();}
            var resumed=JsonUtility.FromJson<OpsEndless>(JsonUtility.ToJson(e));Assert.IsTrue(resumed.Valid());Assert.AreEqual(e.state.MonthlyGrant,resumed.state.MonthlyGrant);
            var state=resumed.state;state.BeginIncident();Assert.IsTrue(state.Resolve("scope"));if(state.QuarterRewardPending)state.ClaimQuarterReward("budget");
            int budget=state.budget,income=state.MonthlyGrant,upkeep=state.Upkeep;Assert.IsTrue(state.NextMonth());Assert.AreEqual(budget+income-upkeep,state.budget,"月替わりで実際に入金される");
        }
        [Test] public void Next12Endless_運営終了年だけ得点を半分にして引退は全額で二重記録しない()
        {
            var e=OpsEndless.Begin(14,null);FinishStoryTestYear(e.state);int first=e.state.AnnualScore;Assert.IsTrue(e.RecordYear());Assert.IsFalse(e.RecordYear());Assert.AreEqual(first,e.TotalScore);Assert.IsTrue(e.AdvanceYear());
            e.state.stability=0;e.state.phase=OpsPhase.Ended;int last=e.state.AnnualScore;Assert.IsTrue(e.RecordYear());Assert.AreEqual(first+last/2,e.TotalScore);Assert.IsTrue(e.finished);Assert.IsFalse(e.retired);Assert.IsFalse(e.AdvanceYear());Assert.IsFalse(e.Retire());Assert.IsTrue(e.Valid());
            var retire=OpsEndless.Begin(14,null);Assert.IsFalse(retire.Retire());FinishStoryTestYear(retire.state);Assert.IsTrue(retire.Retire());Assert.AreEqual(retire.state.AnnualScore,retire.TotalScore);Assert.IsTrue(retire.Valid());Assert.IsFalse(retire.AdvanceYear());Assert.IsFalse(retire.Retire());
        }
        [Test] public void Next12EndlessSave_途中再開と旧保存のバックアップと異常モードを確認する()
        {
            string path=System.IO.Path.GetFullPath("Artifacts/Next12/endless-save-test.json");var old=new OpsState(14,true);Assert.IsTrue(OpsSaveStore.Write(path,old,out var warning),warning);byte[] bytes=System.IO.File.ReadAllBytes(path);
            var e=OpsEndless.Begin(14,null);FinishStoryTestYear(e.state);e.AdvanceYear();Assert.IsTrue(e.state.RequestEngineerResearch());e.state.Act("audit");
            var p=new OpsProgress{endless=e,career=new OpsCareer{endlessUnlocked=true}};Assert.IsTrue(p.Valid());Assert.IsTrue(OpsSaveStore.WriteProgress(path,p,out warning),warning);CollectionAssert.AreEqual(bytes,System.IO.File.ReadAllBytes(path+".bak"));
            var loaded=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(loaded,warning);Assert.IsNull(loaded.single);Assert.IsNull(loaded.story);Assert.AreEqual(JsonUtility.ToJson(e),JsonUtility.ToJson(loaded.endless));Assert.IsTrue(loaded.Current.engineerRequested);Assert.IsTrue(loaded.Valid());
            loaded.career.endlessUnlocked=false;Assert.IsFalse(loaded.Valid());p.story=OpsStory.Begin(1,null);Assert.IsFalse(p.Valid());
            var oldLoaded=OpsSaveStore.ReadProgress(path+".bak",out warning);Assert.IsNotNull(oldLoaded,warning);Assert.IsNull(oldLoaded.endless);Assert.AreEqual(JsonUtility.ToJson(old),JsonUtility.ToJson(oldLoaded.single));
        }
        [Test] public void Next12Endless_五十点の互換を四年目でも保つ()
        {
            var e=OpsEndless.Begin(14,null);for(int y=1;y<4;y++){FinishEndlessTestYear(e.state);e.AdvanceYear();}
            var before=e.state.Preview("scope");e.state.BeginIncident();e.state.Resolve("scope",50,true);Assert.AreEqual(before.loss,e.state.Latest.loss);Assert.AreEqual(before.downtime,e.state.Latest.downtime);Assert.IsTrue(e.Valid());
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next12EndlessUI_解放と年替わりと短い開幕と途中保存を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;Assert.IsFalse(game.StartEndless(14));
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{single=new OpsState(14,true),career=new OpsCareer{endlessUnlocked=true}}));Assert.IsTrue(game.StartEndless(14));Assert.IsNull(game.Story);
            for(int y=1;y<4;y++)
            {
                FinishEndlessTestYear(game.State);Assert.IsTrue(game.NextEndlessYear());Assert.IsTrue(game.YearOpeningActive);
                if(y<3){game.PreviewYearOpening(3);yield return new WaitForSecondsRealtime(2);Assert.IsNotNull(Find<TextMeshProUGUI>("AllyName"));game.SkipYearOpening();game.AdvanceYearOpening();}
            }
            yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual("4年目",Find<TextMeshProUGUI>("OpeningHeading").text);Assert.AreEqual("脅威 +"+OpsCatalog.EndlessPressure(4),Find<TextMeshProUGUI>("OpeningPressure").text);Capture("next12-endless-opening-y4");
            var exported=game.ExportProgress();Assert.IsTrue(exported.Valid());game.AdvanceYearOpening();Assert.IsTrue(game.RestoreProgress(exported));Assert.AreEqual(4,game.RunYear);Assert.IsNull(game.Story);Assert.AreEqual(OpsPhase.Planning,game.State.phase);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
