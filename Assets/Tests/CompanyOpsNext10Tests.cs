using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next10GrowthUI_年度の目標と年別の季節名を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartStory(14);
            FinishStoryTestYear(game.State);Assert.IsTrue(game.NextStoryYear());game.SkipYearOpening();game.AdvanceYearOpening();yield return new WaitForSecondsRealtime(3);
            Assert.AreEqual("拠点まで見える",Find<UnityEngine.UI.Button>("Goal0").transform.Find("GoalLabel").GetComponent<TextMeshProUGUI>().text);Capture("next10-year2-goals");
            Click("Goal0");yield return new WaitForSecondsRealtime(1);StringAssert.Contains("台帳Lv2",Find<TextMeshProUGUI>("DialogBody").text);Capture("next10-year2-goal-detail");
            game.OpenTab(0);FinishStoryTestYear(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(2);
            Click("StoryAnnualReview");yield return new WaitForSecondsRealtime(3);Capture("next10-year2-annual-goals");
            Assert.IsTrue(game.State.GrowthGoals.All(g=>game.State.milestones.Contains(g.name)||!game.State.GrowthSteps(g).All(done=>done)));
            Assert.IsTrue(game.NextStoryYear());game.SkipYearOpening();game.AdvanceYearOpening();yield return new WaitForSecondsRealtime(3);Capture("next10-year3-goals");
            Assert.AreEqual("戻せることを証明した",Find<UnityEngine.UI.Button>("Goal0").transform.Find("GoalLabel").GetComponent<TextMeshProUGUI>().text);
            CheckText();Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next10Growth_年別の条件と信頼加算は一度だけ()
        {
            for(int year=2;year<=3;year++)
            {
                var story=OpsStory.Begin(14,null);FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());
                if(year==3){FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());}
                var s=story.state;s.culture=100;
                Assert.AreEqual(0,s.milestones.Count);Assert.IsFalse(s.GrowthGoals.Any(g=>s.GrowthSteps(g).All(done=>done)));
                foreach(var goal in s.GrowthGoals)
                {
                    s.levels[OpsCatalog.Index(goal.projectA)]=goal.levelA;
                    if(goal.projectB!=null)s.levels[OpsCatalog.Index(goal.projectB)]=goal.levelB;
                    if(goal.staffLevel>0)for(int i=0;i<(goal.allStaff?s.StaffCount:1);i++)while(s.StaffLevel(i)<goal.staffLevel)s.staffExperience[i]++;
                }
                s.trust=40;s.Act("rest");Assert.AreEqual(3,s.milestones.Count);Assert.AreEqual(40+3*OpsCatalog.GrowthTrustReward,s.trust);
                CollectionAssert.AreEquivalent(s.GrowthGoals.Select(g=>g.name),s.milestones);
                int trust=s.trust;s.Act("listen");Assert.AreEqual(trust,s.trust);Assert.IsTrue(s.Valid());
                s.milestones.Add("戻せることを確かめた");Assert.IsFalse(s.Valid());
            }
            var legacy=new OpsState(14,true){yearPressure=12};Assert.AreEqual("戻せることを確かめた",legacy.GrowthGoals[0].name);
        }
        [Test] public void Next10Events_新しい八件は既存プロファイルと用語を使う()
        {
            Assert.AreEqual(40,OpsEventCatalog.Events.Length);Assert.AreEqual(8,OpsEventCatalog.StoryEvents.Length);
            var appeared=new System.Collections.Generic.HashSet<string>();
            foreach(var e in OpsEventCatalog.StoryEvents)
            {
                var p=OpsEventCatalog.Profile(e.profile);Assert.IsNotNull(p,e.id);Assert.IsNotNull(OpsCatalog.Term(p.lesson),e.id);
                Assert.IsNotEmpty(OpsEventCatalog.SourceUrl(e.source??p.source));
                foreach(string text in new[]{e.title,e.news,e.boss,e.staff,e.symptom,e.finding,e.hint})Assert.IsNotEmpty(text,e.id);
                Assert.AreEqual(e.id=="y3-passkey",e.operational);
            }
            for(int seed=0;seed<250;seed++)
            {
                var first=OpsEventCatalog.StorySchedule(seed,1,null);Assert.IsFalse(first.Any(id=>id.StartsWith("y")));
                var second=OpsEventCatalog.StorySchedule(seed+7919,2,first);var third=OpsEventCatalog.StorySchedule(seed+15838,3,first.Concat(second).ToArray());
                foreach(string id in second.Concat(third))if(id.StartsWith("y"))appeared.Add(id);
            }
            CollectionAssert.AreEquivalent(OpsEventCatalog.StoryEvents.Select(e=>e.id),appeared);
        }
        [Test] public void Next10Calendar_三年の抽選は重複せず固定月と運用月を守る()
        {
            for(int seed=0;seed<500;seed++)
            {
                var used=new string[0];
                for(int year=1;year<=3;year++)
                {
                    int yearSeed=unchecked(seed+year*OpsCatalog.StorySeedStride);
                    var ids=OpsEventCatalog.StorySchedule(yearSeed,year,used);
                    CollectionAssert.AreEqual(ids,OpsEventCatalog.StorySchedule(yearSeed,year,used));
                    Assert.AreEqual(12,ids.Distinct().Count());Assert.IsFalse(ids.Intersect(used).Any());
                    Assert.AreEqual(year==1?"intro-ransom":year==2?"y2-branch":"y3-audit-mail",ids[0]);
                    if(year==3)Assert.AreEqual("y3-final",ids[11]);
                    for(int m=0;m<12;m++)Assert.AreEqual(m==3||m==4||m==7||m==9,OpsEventCatalog.Event(ids[m]).operational);
                    used=used.Concat(ids).ToArray();
                }
                Assert.AreEqual(36,used.Distinct().Count());
            }
        }
        [Test] public void Next10Calendar_年別テーマの重みと初年度の完全一致()
        {
            foreach(var p in OpsEventCatalog.Profiles)
            {
                Assert.AreEqual(new[]{"supply","ai","remote","sharing","session"}.Contains(p.id)?2:1,OpsEventCatalog.StoryWeight(2,p.id));
                Assert.AreEqual(new[]{"ransom","targeted","bec","claim","ddos"}.Contains(p.id)?2:1,OpsEventCatalog.StoryWeight(3,p.id));
                Assert.AreEqual(1,OpsEventCatalog.StoryWeight(1,p.id));
            }
            var story=OpsStory.Begin(14,null);Assert.AreEqual(JsonUtility.ToJson(new OpsState(story.YearSeed,true)),JsonUtility.ToJson(story.state));
            FinishStoryTestYear(story.state);var y1=story.state;Assert.IsTrue(story.AdvanceYear());
            Assert.AreEqual("拠点が増える春",story.state.Current.season);Assert.AreEqual(OpsCatalog.Months[0].@base,story.state.Current.@base);
            CollectionAssert.AreEqual(y1.eventSchedule,story.state.previousStoryEvents);
            var copy=JsonUtility.FromJson<OpsStory>(JsonUtility.ToJson(story));Assert.IsTrue(copy.Valid());
            FinishStoryTestYear(story.state);FinishStoryTestYear(copy.state);Assert.IsTrue(story.AdvanceYear());Assert.IsTrue(copy.AdvanceYear());
            CollectionAssert.AreEqual(story.state.eventSchedule,copy.state.eventSchedule);Assert.AreEqual("3年目の総決算",story.state.MonthAt(11).season);Assert.IsTrue(story.Valid());
        }
    }
}
