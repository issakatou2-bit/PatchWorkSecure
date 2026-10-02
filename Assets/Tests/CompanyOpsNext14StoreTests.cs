#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static OpsStory StoreStory(int year,int month)
        {
            var story=AlliesStory(year);var s=story.state;
            // 素材撮影用の入力。ルールや採点を変えず、履歴は実際の事件計算から作る。
            s.budget=400;s.culture=90;s.trust=90;s.fatigue=12;
            for(int i=0;i<s.levels.Length;i++)s.levels[i]=s.EquipmentAvailable(i)?2:0;
            for(int m=0;m<month;m++){DiaryTestMonth(s);if(s.QuarterRewardPending)s.ClaimQuarterReward("budget");Assert.IsTrue(s.NextMonth());}
            Assert.IsTrue(story.Valid());return story;
        }
        private static void StoreShot(string name)
        {
            foreach(var label in Object.FindObjectsByType<TextMeshProUGUI>())
            {
                Assert.IsFalse(label.name.IndexOf("Debug",StringComparison.OrdinalIgnoreCase)>=0||label.name.IndexOf("FPS",StringComparison.OrdinalIgnoreCase)>=0);
                StringAssert.DoesNotContain("エンジニアさん",label.text);
            }
            // 1920の描画ターゲットへCanvasを実描画。1600の写真の拡大ではない。
            Capture("store-"+name,1920,1080);
            string folder=Path.Combine(Application.dataPath,"../Artifacts/Store");Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,name+".png");File.Copy(Path.Combine(Application.dataPath,"../Artifacts/CompanyOps/store-"+name+".png"),path,true);
            var png=File.ReadAllBytes(path);Assert.AreEqual(1920,(png[16]<<24)|(png[17]<<16)|(png[18]<<8)|png[19]);Assert.AreEqual(1080,(png[20]<<24)|(png[21]<<16)|(png[22]<<8)|png[23]);
        }
        [UnityTest] public IEnumerator Next14StoreUI_本編と二人絵と強敵と続行を実寸撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(4);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            StoreShot("01-title");
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(3,6)}));yield return new WaitForSecondsRealtime(4);game.StopVoice();StoreShot("02-planning-y3-autumn");
            Assert.AreEqual(3,game.RunYear);Assert.AreEqual(6,game.State.month);
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(2,8)}));yield return new WaitForSecondsRealtime(2);
            game.BeginIncident();yield return new WaitForSecondsRealtime(.6f);Assert.IsNotNull(Find<TextMeshProUGUI>("BossEntryTitle"));StoreShot("14-rival-appearance");
            yield return new WaitForSecondsRealtime(5);Assert.IsFalse(game.PhasePresentationRunning);StoreShot("03-incident-choice");
            Assert.IsTrue(game.State.Resolve("recover",50,true));game.OpenTab(0);yield return new WaitForSecondsRealtime(4);game.FinishReportCounts();StoreShot("10-monthly-report");
            var annual=new OpsState(14,true);FinishStoryTestYear(annual);Assert.IsTrue(game.RestoreProgress(new OpsProgress{single=annual}));yield return new WaitForSecondsRealtime(4);
            Assert.GreaterOrEqual(OpsStory.RankValue(annual.RankCode),OpsStory.RankValue("A"));StoreShot("11-annual-rating");
            foreach(int page in new[]{2,6})
            {
                var story=StoreStory(1,page);Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));DiaryTestMonth(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);
                game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(3);CheckPairPhoto("DiaryPair",page==2?"hinata-kanon-1":"hinata-engineer-3",page==2?"予算のひみつ":"はじめての夜");
                StoreShot(page==2?"12-diary-pair-june":"16-diary-pair-october");
            }
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(2,0)}));game.PreviewYearOpening(2);yield return new WaitForSecondsRealtime(2);StoreShot("13-opening-y2-rivals");
            var endless=OpsEndless.Begin(14,null);for(int y=1;y<4;y++){FinishEndlessTestYear(endless.state);Assert.IsTrue(endless.AdvanceYear());}FinishEndlessTestYear(endless.state);
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{endless=endless,career=new OpsCareer{endlessUnlocked=true}}));yield return new WaitForSecondsRealtime(4);
            Assert.IsNotNull(Find<UnityEngine.UI.Button>("NextEndlessYear"));StoreShot("15-endless-continue");Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next14StoreUI_六本のミニゲームの本編を実寸撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            string[] files={"04-minigame-b-containment","05-minigame-c-mail","06-minigame-d-mfa","07-minigame-e-logs","08-minigame-f2-blocks","09-minigame-g-restore"};
            for(int i=0;i<OpsDailyPractice.Ids.Length;i++)
            {
                Assert.IsTrue(game.BeginDailyPractice(OpsDailyPractice.Ids[i],20261002));game.StartMinigame();yield return new WaitForSecondsRealtime(2);
                if(game.Minigame is OpsBlockMinigame blocks)
                {
                    foreach(var task in blocks.Tasks.Take(2))Assert.IsTrue(blocks.Place(task.Id,task.Solution.Min(c=>c.Row),task.Solution.Min(c=>c.Column)));
                    game.TickMinigame(0);yield return new WaitForSecondsRealtime(.3f);
                }
                Assert.AreEqual(OpsMinigamePhase.Playing,game.Minigame.Phase);StoreShot(files[i]);
                FinishDailyForTest(game.Minigame);game.TickMinigame(0);game.ConfirmMinigame();yield return null;
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
