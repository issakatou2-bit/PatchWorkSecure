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
using UnityEngine.UI;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void BossTestMonth(OpsState s,int month)
        {
            s.budget=400;s.fatigue=0;s.trust=s.culture=100;for(int i=0;i<s.levels.Length;i++)s.levels[i]=s.EquipmentAvailable(i)?2:0;
            while(s.month<month){DiaryTestMonth(s);if(s.QuarterRewardPending)s.ClaimQuarterReward("budget");Assert.IsTrue(s.NextMonth());}
        }
        private static OpsStory BossArchiveFixture(int year)
        {
            // 撃退済みの札も撮るための入力選び。実ゲームの選択には真相を使わない。
            for(int seed=14;seed<100;seed++)
            {
                var story=OpsStory.Begin(seed,null);
                for(int y=1;y<year;y++){FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());}
                BossTestMonth(story.state,OpsCatalog.JunePeak);
                Assert.IsTrue(story.state.Act("audit"));
                var preview=story.state.Preview("scope");preview.forecast=story.state.Estimate("scope");
                if(story.state.BossDefeated(preview))return story;
            }
            Assert.Fail("撃退済みの撮影入力がありません");return null;
        }
        [Test] public void Next11Boss_新年度の山場を固定して初年度と旧年度の抽選は保つ()
        {
            Assert.AreEqual(40,OpsEventCatalog.Events.Length);Assert.AreEqual(8,OpsEventCatalog.StoryEvents.Length);
            foreach(int year in new[]{2,3})
            {
                var story=AlliesStory(year);var s=story.state;
                foreach(var rival in OpsCatalog.CompanyYear(year).rivals){Assert.AreEqual(rival.id,s.eventSchedule[rival.month]);Assert.AreEqual(rival.profile,OpsEventCatalog.Event(rival.id).profile);}
                Assert.IsTrue(story.Valid());Assert.AreEqual(12,s.eventSchedule.Distinct().Count());Assert.IsFalse(s.eventSchedule.Any(s.previousStoryEvents.Contains));
                s.month=OpsCatalog.JunePeak;Assert.IsNotNull(s.CurrentBoss);s.yearGrowthRules=0;s.levels=s.levels.Take(11).ToArray();if(year==3)s.staffExperience=s.staffExperience.Take(3).ToArray();Assert.IsNull(s.CurrentBoss);
            }
            for(int seed=0;seed<30;seed++)CollectionAssert.AreEqual(OpsEventCatalog.Schedule(seed,false),new OpsState(seed,true).eventSchedule);
            Assert.AreEqual("bec",OpsEventCatalog.Event("y2-ai").profile);Assert.AreEqual("bec",OpsEventCatalog.Event("y3-ai").profile,"声の偽装は支払・承認の確認で対応する");
        }
        [Test] public void Next11Boss_撃退は確定記録と公開幅だけで判定して正常事件を数えない()
        {
            var story=AlliesStory(2);var s=story.state;BossTestMonth(s,OpsCatalog.JunePeak);s.BeginIncident();var expected=s.Preview("scope");s.Resolve("scope",50,true);
            Assert.AreEqual(expected.loss,s.Latest.loss);Assert.AreEqual(expected.downtime,s.Latest.downtime);Assert.IsTrue(story.Valid());
            var record=s.Latest;record.benign=false;record.loss=3;record.downtime=4;record.forecast=new OpsEstimate{lossMin=2,lossMax=4,stopMin=3,stopMax=5};
            Assert.IsTrue(s.BossDefeated(record));var career=new OpsCareer();Assert.IsTrue(career.RecordBoss(s,record));Assert.IsFalse(career.RecordBoss(s,record));Assert.IsTrue(career.Valid());
            var restored=JsonUtility.FromJson<OpsCareer>(JsonUtility.ToJson(career));Assert.IsTrue(restored.Valid());Assert.AreEqual(record.eventId,restored.defeatedBosses.Single().eventId);
            record.loss=4;Assert.IsFalse(s.BossDefeated(record));record.loss=3;record.downtime=5;Assert.IsFalse(s.BossDefeated(record));record.downtime=4;
            record.benign=true;Assert.IsFalse(s.BossDefeated(record));record.benign=false;record.forecast=null;Assert.IsFalse(s.BossDefeated(record));
            restored.defeatedBosses.Add(restored.defeatedBosses[0]);Assert.IsFalse(restored.Valid());
            var april=AlliesStory(2).state;Assert.IsNull(april.CurrentBoss);Assert.IsFalse(career.RecordBoss(april,record));
        }
        [UnityTest] public IEnumerator Next11BossUI_強敵の札と図鑑と再読み込みを撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            foreach(int year in new[]{2,3})
            {
                var story=BossArchiveFixture(year);
                Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));yield return new WaitForSecondsRealtime(2);Capture("next11-boss-y"+year+"-before");
                game.BeginIncident();float deadline=Time.realtimeSinceStartup+3;while(!UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.name=="BossEntryTitle")&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNotNull(Find<TextMeshProUGUI>("BossEntryTitle"));yield return new WaitForSecondsRealtime(.65f);Capture("next11-boss-y"+year+"-appear");
                Assert.AreEqual(story.state.CurrentBoss.name,Find<TextMeshProUGUI>("BossName").text);
                foreach(var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name.StartsWith("Boss"))){t.ForceMeshUpdate();Assert.IsFalse(t.isTextOverflowing,t.name+" / "+t.text);}
                yield return new WaitForSecondsRealtime(1);Assert.IsFalse(game.PhasePresentationRunning);Assert.IsNotNull(Find<Button>("Respond_scope"));
                game.State.Resolve("scope",50,true);game.OpenTab(2);yield return new WaitForSecondsRealtime(1);
                Assert.AreEqual(game.State.BossDefeated(game.State.Latest)?1:0,game.Career.defeatedBosses.Count);
                Assert.AreEqual(1,game.Career.defeatedBosses.Count,"撃退済みの札も確認する");
                if(game.State.QuarterRewardPending)Assert.IsTrue(game.State.ClaimQuarterReward("budget"));
                Assert.IsTrue(game.State.NextMonth());game.OpenTab(2);yield return new WaitForSecondsRealtime(1);
                CheckPointer("BossArchive");Click("BossArchive");yield return new WaitForSecondsRealtime(1);Capture("next11-boss-y"+year+"-archive");
                Assert.IsTrue(game.Career.Valid(),"図鑑の保存");
                Assert.IsTrue(game.State.Valid(),string.Join(" / ",typeof(OpsState).GetMethods(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Where(m=>m.Name.StartsWith("Valid")&&m.GetParameters().Length==0).Select(m=>m.Name+":"+m.Invoke(game.State,null))));
                Assert.IsTrue(game.Story.Valid(),"本編の保存");
                Assert.IsTrue(game.ExportProgress().Valid());
                string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Artifacts/Next11/boss-save-y"+year+".json"));
                Assert.IsTrue(OpsSaveStore.WriteProgress(path,game.ExportProgress(),out string warning),warning);
                var saved=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(saved,warning);Assert.IsTrue(saved.Valid());
                Assert.IsTrue(game.RestoreProgress(saved));
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
