#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static OpsState Next11State(int year,string profile)
        {
            var previous=new OpsState(14,true);FinishStoryTestYear(previous);
            var s=OpsState.NewStoryYear(14+OpsCatalog.StorySeedStride,2,previous,null);
            if(year==3){FinishStoryTestYear(s);s=OpsState.NewStoryYear(14+2*OpsCatalog.StorySeedStride,3,s,null);}
            s.month=2;s.history.Clear();s.history.Add(new OpsOutcome());s.history.Add(new OpsOutcome());
            s.eventSchedule[s.month]=OpsEventCatalog.Events.Concat(OpsEventCatalog.StoryEvents).First(e=>e.profile==profile).id;
            s.budget=400;s.capacity=4;s.fatigue=24;return s;
        }
        [Test] public void Next11Equipment_初年度と旧保存の十一個を保ち年別の十五個だけ導入する()
        {
            for(int seed=0;seed<30;seed++)Assert.AreEqual(JsonUtility.ToJson(new OpsState(seed,true)),JsonUtility.ToJson(OpsState.NewStoryYear(seed,1,null,null)));
            Assert.AreEqual(11,OpsCatalog.Projects.Length);Assert.AreEqual(15,OpsCatalog.AllProjects.Length);
            Assert.IsTrue(OpsCatalog.Projects.SequenceEqual(OpsCatalog.AllProjects.Take(11)),"従来の設備と並びをそのまま保つ");
            var old=new OpsState(14,true);Assert.IsTrue(old.Valid());Assert.AreEqual(0,old.Level("edr"));Assert.IsFalse(old.Upgrade(12));
            FinishStoryTestYear(old);var s=OpsState.NewStoryYear(8000,2,old,null);Assert.AreEqual(15,s.levels.Length);Assert.IsTrue(s.Valid());
            Assert.AreEqual(0,s.Level("threatSharing"));Assert.IsFalse(s.Upgrade(13));s.levels[3]=s.levels[4]=0;
            StringAssert.Contains("先に",s.UpgradeBlock(11));StringAssert.Contains("先に",s.UpgradeBlock(12));
            s.levels[3]=s.levels[4]=1;s.budget=200;s.capacity=4;Assert.IsTrue(s.Upgrade(11));Assert.IsTrue(s.Upgrade(12));Assert.IsTrue(s.Valid());
            var copy=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s));Assert.IsTrue(copy.Valid());Assert.AreEqual(1,copy.Level("zeroTrust"));
            copy.yearGrowthRules=0;copy.levels=copy.levels.Take(11).ToArray();Assert.IsTrue(copy.Valid());Assert.AreEqual(0,copy.Level("zeroTrust"));
            Assert.IsFalse(OpsCareer.ValidFactors(new[]{"edr"}));
        }
        [Test] public void Next11Equipment_効く題材と公開見積もりと五十点互換と正常事件を守る()
        {
            foreach(var pair in new[]{Tuple.Create("zeroTrust","supply"),Tuple.Create("edr","ransom"),Tuple.Create("threatSharing","targeted"),Tuple.Create("csirt","ransom")})
            {
                var s=Next11State(3,pair.Item2);int index=OpsCatalog.Index(pair.Item1);s.levels[index]=0;var before=s.Estimate("scope");int knowledge=s.SituationKnowledge,margin=s.EstimateMargin;
                s.levels[index]=1;var after=s.Estimate("scope");Assert.LessOrEqual(after.lossMax,before.lossMax);Assert.LessOrEqual(after.stopMax,before.stopMax);
                if(pair.Item1=="threatSharing"){Assert.GreaterOrEqual(s.SituationKnowledge,knowledge);Assert.Less(s.EstimateMargin,margin);}
                s.BeginIncident();var expected=s.Preview("scope");Assert.IsTrue(s.Resolve("scope",50,true));Assert.AreEqual(expected.loss,s.Latest.loss);Assert.AreEqual(expected.downtime,s.Latest.downtime);
            }
            var unrelated=Next11State(3,"service");var estimate=JsonUtility.ToJson(unrelated.Estimate("scope"));unrelated.levels[11]=unrelated.levels[12]=unrelated.levels[13]=2;Assert.AreEqual(estimate,JsonUtility.ToJson(unrelated.Estimate("scope")));
            var one=new OpsState(14,true);one.BeginIncident();var normal=one.CreateContainment();Assert.IsNotNull(normal);
        }
        [Test] public void Next11Equipment_EDRは観測できた感染だけ通知し正常な事件では通知しない()
        {
            foreach(bool benign in new[]{false,true})
            {
                OpsState state=null;
                for(int seed=0;seed<200;seed++)
                {
                    var s=Next11State(3,"ransom");s.seed=seed;
                    if(s.Preview("scope").benign!=benign)continue;state=s;break;
                }
                Assert.IsNotNull(state);state.levels[12]=1;state.BeginIncident();
                var session=state.CreateContainment();Assert.IsTrue(session.Start());
                Assert.AreEqual(session.VisibleCount,Enumerable.Range(0,OpsCatalog.ContainmentRooms).Sum(session.EdrRoomAlerts));
                if(benign){session.Tick(3);Assert.AreEqual(0,session.VisibleCount);Assert.AreEqual(0,Enumerable.Range(0,OpsCatalog.ContainmentRooms).Sum(session.EdrRoomAlerts));}
                else {Assert.Greater(session.VisibleCount,0);Assert.Greater(Enumerable.Range(0,OpsCatalog.ContainmentRooms).Sum(session.EdrRoomAlerts),0);}
                Assert.AreEqual(0,session.EdrRoomAlerts(-1));Assert.AreEqual(0,session.EdrRoomAlerts(OpsCatalog.ContainmentRooms));
            }
        }
        [UnityTest] public IEnumerator Next11EquipmentAlertUI_三年目の実際の事件で通知と五十点を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var story=OpsStory.Begin(14,null);FinishStoryTestYear(story.state);story.AdvanceYear();FinishStoryTestYear(story.state);story.AdvanceYear();
            var state=story.state;for(int i=0;i<state.levels.Length;i++)state.levels[i]=2;
            state.culture=state.trust=100;state.fatigue=0;state.budget=400;
            while(state.month<11){DiaryTestMonth(state);if(state.QuarterRewardPending)state.ClaimQuarterReward("budget");Assert.IsTrue(state.NextMonth());}
            Assert.IsTrue(state.BeginIncident());Assert.IsTrue(state.Valid());var expected=state.Preview("scope");
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));game.ChooseResponse("scope");
            yield return new WaitForSecondsRealtime(.3f);Capture("next11-edr-brief");
            game.StartMinigame();yield return new WaitForSecondsRealtime(.25f);
            var session=game.Minigame as OpsContainmentMinigame;Assert.IsNotNull(session);Assert.IsTrue(session.Edr);Assert.IsTrue(session.Monitor);
            Assert.Greater(session.VisibleCount,0);Assert.IsTrue(UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>().Any(t=>t.name.StartsWith("MinigameSelected_")&&t.text.StartsWith("EDR 検知")));
            Capture("next11-edr-play");
            // 50点の互換は本編の事件でも確認。別の新しいセッションを社員に任せる。
            game.StartYear(1);Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));game.ChooseResponse("scope");game.DelegateMinigame();
            yield return null;game.ConfirmMinigame();Assert.AreEqual(expected.loss,state.Latest.loss);Assert.AreEqual(expected.downtime,state.Latest.downtime);Assert.IsTrue(game.ExportProgress().Valid());
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next11EquipmentUI_年別設備と四画面の十五設備の参照を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            typeof(OpsGame).GetProperty("ReducedMotion").SetValue(game,true);
            foreach(int year in new[]{2,3})
            {
                var story=OpsStory.Begin(14,null);
                for(int y=1;y<year;y++){FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());}
                var state=story.state;state.budget=400;
                for(int i=0;i<state.levels.Length;i++)state.levels[i]=state.EquipmentAvailable(i)?1:0;
                Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));
                yield return new WaitForSecondsRealtime(3);Capture("next11-equipment-y"+year+"-planning");
                Assert.IsNotNull(Find<Button>("Room_branch"));Assert.IsNotNull(Find<UnityEngine.UI.Image>("RoomDevice_zeroTrust"));
                if(year==3){Assert.IsNotNull(Find<Button>("Room_partners"));Assert.IsNotNull(Find<UnityEngine.UI.Image>("RoomDevice_csirt"));}
                game.OpenTab(1);yield return new WaitForSecondsRealtime(3);
                Assert.IsNotNull(Find<Button>("Details_edr"));
                Assert.AreEqual(year==3,UnityEngine.Object.FindObjectsByType<Button>().Any(b=>b.name=="Details_csirt"));
                Find<Button>("Details_edr").onClick.Invoke();yield return new WaitForSecondsRealtime(1);Capture("next11-equipment-y"+year+"-details");
                Assert.IsNotNull(Find<Button>("Buy_edr"));
                Assert.IsTrue(state.BeginIncident());game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next11-equipment-y"+year+"-incident");
                var expected=state.Preview("scope");Assert.IsTrue(state.Resolve("scope",50,true));Assert.AreEqual(expected.loss,state.Latest.loss);Assert.AreEqual(expected.downtime,state.Latest.downtime);
                game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next11-equipment-y"+year+"-report");
                game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(3);Capture("next11-equipment-y"+year+"-diary");
                Assert.IsTrue(state.NextMonth());FinishStoryTestYear(state);game.OpenTab(0);
                typeof(OpsGame).GetField("storyAnnualDetails",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game,true);
                game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next11-equipment-y"+year+"-annual");Assert.IsTrue(game.ExportProgress().Valid());
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
