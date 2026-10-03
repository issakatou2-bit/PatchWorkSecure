#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private bool next23ClockOwned;
        [TearDown] public void Next23_撮影時計の所有分を失敗時も戻す()
        {if(next23ClockOwned){OpsPromoClock.End();next23ClockOwned=false;}}
        [Test]
        public void Next23_共通時計は初回を飛ばさず再訪は終端へ速いは半分()
        {
            var first=new OpsPresentationWait("factor",OpsPresentationTiming.Factor,false);
            Assert.IsFalse(first.Skip());Assert.AreEqual(0,first.Elapsed);
            var repeat=new OpsPresentationWait("factor",OpsPresentationTiming.Factor,true);
            Assert.IsTrue(repeat.Skip());Assert.IsTrue(repeat.Done);Assert.AreEqual(first.Duration,repeat.Elapsed);
            var fast=new OpsPresentationWait("factor",OpsPresentationTiming.Factor,false);
            first.Advance(first.Duration,false);fast.Advance(fast.Duration/2,true);
            Assert.AreEqual(first.Elapsed,fast.Elapsed);Assert.IsTrue(fast.Done);
        }

        [UnityTest]
        public IEnumerator Next23_クリック決定Aは初回を守り再訪の声と待ちだけを飛ばす()
        {
            using(var input=new PadFixture())
            {
                var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
                var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);
                game.StartYear(14);game.SkipTutorial();yield return input.Tap(input.Pad.buttonSouth);
                Assert.IsTrue(game.PhasePresentationRunning,"初回はAで飛ばない");
                while(game.PhasePresentationRunning)yield return null;
                foreach(var button in new UnityEngine.InputSystem.Controls.ButtonControl[]{mouse.leftButton,keyboard.enterKey,input.Pad.buttonSouth})
                {
                    game.StartYear(14);game.SkipTutorial();yield return null;
                    game.SpeakSceneLine("mg_start",1);string state=JsonUtility.ToJson(game.State);
                    yield return input.Tap(button);yield return null;
                    Assert.IsFalse(game.PhasePresentationRunning,button.name);Assert.IsFalse(game.VoicePending);Assert.IsFalse(game.PortraitVoicePlaying);
                    Assert.AreEqual(state,JsonUtility.ToJson(game.State),"スキップ入力を計画の行動へ渡さない");
                }
                LogAssert.NoUnexpectedReceived();
            }
        }

        private static IEnumerator TimingSettle(OpsGame game,bool skip,int max=900)
        {
            for(int frame=0;frame<max;frame++)
            {
                OpsPromoClock.Advance();if(skip&&frame>=3)game.TrySkipPresentation();yield return null;
                if(frame>3&&!game.PresentationWaiting&&!game.PhasePresentationRunning&&!game.ResolutionActive&&!game.FactorRevealActive&&!game.ReportCounting&&!game.MinigameCounting)yield break;
            }
            var waits=(System.Collections.Generic.List<OpsPresentationWait>)typeof(OpsGame).GetField("presentationWaits",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            Assert.Fail("演出の待ちが終了しない: "+string.Join(" / ",waits.Where(w=>!w.Done).Select(w=>w.Kind+" "+w.Elapsed+"/"+w.Duration)));
        }

        [UnityTest]
        public IEnumerator Next23_遊び中の成功表示は次の入力を演出スキップへ渡さない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);
            var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);game.StartYear(9);game.SkipTutorial();yield return null;
            var portrait=game.Surface.GetComponentsInChildren<OpsPortraitAnimator>().First();
            portrait.ChangePose("pose_magnifier");portrait.ChangePose("pose_laptop");
            yield return new WaitForSecondsRealtime(2);
            Assert.IsFalse(game.PresentationWaiting,"上書きされたポーズの待ちを残さない");
            Assert.IsTrue(game.BeginDailyPractice("C",20261003));game.StartMinigame();yield return null;
            AuditSuccess(game);yield return null;
            Assert.AreEqual(OpsMinigamePhase.Playing,game.Minigame.Phase);
            Assert.IsFalse(game.TrySkipPresentation(),"成功表示の間もクリック・決定・Aは遊びの操作として扱う");
            Assert.AreEqual(0,(float)typeof(OpsGame).GetField("presentationInputGuardUntil",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Next23_月替わり事件発動月報は三経路で状態と数字が一致する()
        {
            next23ClockOwned=true;OpsPromoClock.Begin();
            try
            {
                string baseline=null;
                for(int mode=0;mode<3;mode++)
                {
                    SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);game.UseLocalTestVoices=false;
                    game.SetPresentationSpeed(mode==2);
                    int repeats=mode==1?2:1;
                    for(int attempt=0;attempt<repeats;attempt++)
                    {
                        game.StartYear(14);game.SkipTutorial();yield return TimingSettle(game,mode==1&&attempt==1);
                        string start=JsonUtility.ToJson(game.State);game.BeginIncident();yield return TimingSettle(game,mode==1&&attempt==1);
                        game.Resolve("scope");string resolved=JsonUtility.ToJson(game.State);yield return TimingSettle(game,mode==1&&attempt==1);
                        Assert.AreEqual(resolved,JsonUtility.ToJson(game.State));
                        var values=Object.FindObjectsByType<TMPro.TextMeshProUGUI>().Where(t=>t.name=="MonthlyLossValue"||t.name=="MonthlyStopValue").OrderBy(t=>t.name).Select(t=>t.text);
                        string final=resolved+"|"+string.Join("|",values);
                        if(baseline==null)baseline=final;else Assert.AreEqual(baseline,final,"経路 "+mode+" / "+attempt);
                        Assert.IsNotEmpty(start);Assert.IsFalse(game.ReportCounting);
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();}
        }

        [UnityTest]
        public IEnumerator Next23_日記と年間と因子の最終表示は三経路で一致する()
        {
            next23ClockOwned=true;OpsPromoClock.Begin();
            try
            {
                string diary=null,annual=null,factor=null;
                for(int mode=0;mode<3;mode++)
                {
                    SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);game.SetPresentationSpeed(mode==2);
                    for(int attempt=0;attempt<(mode==1?2:1);attempt++)
                    {
                        bool skip=mode==1&&attempt==1;
                        game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)});
                        game.Career.diary.Clear();game.Career.diary.Add(new OpsDiaryRecord{key=2,content=2,mood=0,recap="今月の備え",thought="みんなで守れた",rank="A",season="秋",minigame=""});
                        game.OpenDiaryBook();AuditClick("DiaryPage_2");yield return TimingSettle(game,skip);
                        var body=Find<TMPro.TextMeshProUGUI>("DiaryBody");Assert.AreEqual(int.MaxValue,body.maxVisibleCharacters);Assert.AreEqual(Vector3.one,Find<Transform>("DiaryNotebook").localScale);
                        if(diary==null)diary=body.text;else Assert.AreEqual(diary,body.text);
                        game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)});FinishStoryTestYear(game.State);game.OpenTab(0);yield return TimingSettle(game,skip);
                        AuditClick("StoryAnnualReview");yield return TimingSettle(game,skip);
                        string value=JsonUtility.ToJson(game.State)+"|"+Find<Transform>("RankBadge").localScale;
                        if(annual==null)annual=value;else Assert.AreEqual(annual,value);
                        game.StartStory(14);for(int y=1;y<=3;y++){FinishStoryTestYear(game.State);if(y<3)game.NextStoryYear();}
                        game.OpenTab(0);yield return TimingSettle(game,skip);AuditClick("BackHome");yield return TimingSettle(game,skip);
                        Assert.IsFalse(game.FactorRevealActive);Assert.IsTrue(Find<UnityEngine.UI.Button>("ChooseFactor0").interactable);
                        string cards=string.Join("|",Enumerable.Range(0,3).Select(i=>Find<Transform>("ChooseFactor"+i).GetComponentInChildren<TMPro.TextMeshProUGUI>().text));
                        if(factor==null)factor=cards;else Assert.AreEqual(factor,cards);
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();}
        }

        [UnityTest]
        public IEnumerator Next23_年度の開幕も三経路で同じ計画へ進む()
        {
            next23ClockOwned=true;OpsPromoClock.Begin();
            try
            {
                string baseline=null;
                for(int mode=0;mode<3;mode++)
                {
                    SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);game.SetPresentationSpeed(mode==2);
                    for(int attempt=0;attempt<(mode==1?2:1);attempt++)
                    {
                        Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(2,0)}));game.PreviewYearOpening(0,false);
                        for(int frame=0;game.YearOpeningActive&&frame<900;frame++)yield return PromoWait(1,()=>{if(mode==1&&attempt==1)game.TrySkipPresentation();});
                        Assert.IsFalse(game.YearOpeningActive);yield return TimingSettle(game,false);
                        string state=JsonUtility.ToJson(game.State);if(baseline==null)baseline=state;else Assert.AreEqual(baseline,state);
                        Assert.IsNotNull(Find<UnityEngine.UI.Button>("Action_listen"));
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();}
        }

        [UnityTest]
        public IEnumerator Next23_六本の遊びの時計と終了点数と判子は三経路で一致する()
        {
            next23ClockOwned=true;OpsPromoClock.Begin();
            try
            {
                foreach(string id in OpsDailyPractice.Ids)
                {
                    string baseline=null;
                    for(int mode=0;mode<3;mode++)
                    {
                        SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);game.SetPresentationSpeed(mode==2);
                        for(int attempt=0;attempt<(mode==1?2:1);attempt++)
                        {
                            game.StartYear(9);game.BeginDailyPractice(id,20261003);game.StartMinigame();
                            var model=OpsDailyPractice.Create(id,20261003);model.Start();
                            // 画面の速さを変えてもモデルには同じ固定刻みを与える。
                            for(int n=0;n<90&&game.Minigame.Phase==OpsMinigamePhase.Playing;n++)
                            {OpsPromoClock.Advance();yield return null;model.Tick(1f/30);}
                            Assert.AreEqual(model.Elapsed,game.Minigame.Elapsed,.001f,id+"の遊びの時計");Assert.AreEqual(model.Duration,game.Minigame.Duration);
                            if(game.Minigame is OpsBlockMinigame){for(int i=0;i<12;i++)AuditSuccess(game);game.FinishBlocks();}
                            else for(int guard=0;game.Minigame.Phase==OpsMinigamePhase.Playing&&guard<6000;guard++)yield return PromoWait(1,()=>{if(game.Minigame is OpsRestoreMinigame)AuditSuccess(game);});
                            Assert.AreEqual(OpsMinigamePhase.Result,game.Minigame.Phase,id+" / 経路 "+mode+" / "+attempt);
                            int score=game.Minigame.Score;string grade=game.Minigame.Grade;
                            yield return TimingSettle(game,mode==1&&attempt==1);
                            Assert.AreEqual(score,game.Minigame.Score);Assert.AreEqual(grade,game.Minigame.Grade);
                            Assert.IsTrue(Find<Transform>("MinigameRankStamp").gameObject.activeInHierarchy);Assert.AreEqual(score+"点",Find<TMPro.TextMeshProUGUI>("MinigameScore").text);
                            string final=score+"|"+grade+"|"+Find<Transform>("MinigameRankStamp").localScale;
                            if(baseline==null)baseline=final;else Assert.AreEqual(baseline,final,id);
                            game.ConfirmMinigame();
                        }
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();}
        }
        [UnityTest,Explicit("Next-23の変更前後だけ撮影する"),Category("Capture")]
        public IEnumerator Next23_待ちの変更前後の最終画面を撮る()
        {
            string phase=typeof(OpsGame).Assembly.GetType("PatchWorkSecure.CompanyOps.OpsPresentationTiming")==null?"before":"after";
            next23ClockOwned=true;OpsPromoClock.Begin();
            try
            {
                SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);
                var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);
                using(var recorder=new PromoRecorder("Artifacts/Next23/"+phase))
                {
                    game.StartYear(14);game.SkipTutorial();yield return PromoWait(120);
                    yield return AuditShot(recorder,game,"month",null,3);
                    game.BeginIncident();yield return PromoWait(90);
                    yield return AuditShot(recorder,game,"incident",null,3);
                    game.Resolve("scope");for(int i=0;game.ResolutionActive&&i<600;i++)yield return PromoWait(1);
                    yield return PromoWait(90);yield return AuditShot(recorder,game,"report",null,3);
                    game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)});
                    game.Career.diary.Clear();game.Career.diary.Add(new OpsDiaryRecord{key=2,content=2,mood=0,recap="今月の備えを確認した",thought="みんなで守れた",rank="A",season="秋",minigame=""});
                    game.OpenDiaryBook();AuditClick("DiaryPage_2");yield return PromoWait(150);
                    yield return AuditShot(recorder,game,"diary",null,3);
                    game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)});FinishStoryTestYear(game.State);game.OpenTab(0);yield return PromoWait(90);
                    AuditClick("StoryAnnualReview");yield return PromoWait(120);
                    yield return AuditShot(recorder,game,"annual",null,3);
                    game.StartStory(14);for(int y=1;y<=3;y++){FinishStoryTestYear(game.State);if(y<3)game.NextStoryYear();}
                    game.OpenTab(0);yield return PromoWait(90);AuditClick("BackHome");yield return PromoWait(150);
                    yield return AuditShot(recorder,game,"factor",null,3);
                    game.StartYear(9);Assert.IsTrue(game.BeginDailyPractice("F2",20261003));game.StartMinigame();
                    for(int i=0;i<12;i++)AuditSuccess(game);game.FinishBlocks();yield return PromoWait(150);
                    yield return AuditShot(recorder,game,"blocks",null,3);
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();OpsPromoClock.ResumeFrom="";}
        }
    }
}
#endif
