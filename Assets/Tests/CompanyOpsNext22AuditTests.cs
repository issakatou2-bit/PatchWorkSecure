#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        // 棚卸しだけ。通常のテストの組とゲーム本体には接続しない。
        private static void AuditClick(string name)
        {
            var button=Find<Button>(name);Assert.IsTrue(button.IsInteractable(),name);
            button.onClick.Invoke(); // 通年テストの自動委任・日記閉じ・因子スキップは使わない。
        }
        private static void AuditDefaults(OpsGame game)
        {
            game.SetPresentationSpeed(false);
            typeof(OpsGame).GetProperty("ReducedMotion").SetValue(game,false);
        }
        private static IEnumerator AuditShot(PromoRecorder recorder,OpsGame game,string name,Action<int> action=null,int frames=75)
        {
            int before=UnityEngine.Time.frameCount;
            recorder.Begin(name,frames,game);
            for(int n=1;n<=frames;n++)
            {
                recorder.Frame=n;OpsPromoClock.Advance();action?.Invoke(n);yield return null;
                PromoCheckText();recorder.Snapshot();
            }
            recorder.End();Assert.Greater(UnityEngine.Time.frameCount,before,"実際の描画フレームが進む");
        }

        [UnityTest,Explicit("Next-22の棚卸し撮影。名前指定時だけ実行し普段の件数を変えない")]
        [Category("Capture")]
        public IEnumerator Next22Audit_通常画面の操作前後を30Hzで撮る()
        {
            int old=UnityEngine.Time.captureFramerate;OpsPromoClock.Begin();
            try
            {
                SceneManager.LoadScene("CompanyYear");yield return PromoWait(1);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);
                using(var recorder=new PromoRecorder("Artifacts/Next22/shots"))
                {
                    yield return AuditShot(recorder,game,"01-title-entry");
                    game.StartYear(14);game.SkipTutorial();yield return PromoWait(70);
                    yield return AuditShot(recorder,game,"02-tutorial",n=>{if(n==12)Assert.IsTrue(game.StartTutorial());});game.SkipTutorial();yield return PromoWait(15);
                    yield return AuditShot(recorder,game,"03-planning-card",n=>{if(n==34)AuditClick("Action_listen");});
                    game.StartYear(14);game.SkipTutorial();yield return PromoWait(60);
                    yield return AuditShot(recorder,game,"04-install",n=>{if(n==34)game.Buy(OpsCatalog.Index("backup"));});
                    // 既存の撮影入力と同じ成長直前の状態から実際の行動を行う。
                    game.State.playerExperience=OpsGrowthCatalog.PlayerThresholds[1]-1;
                    yield return AuditShot(recorder,game,"05-rank-up",n=>{if(n==34)game.ChooseAction("listen");});
                    yield return AuditShot(recorder,game,"06-incident-entry",n=>{if(n==12)game.BeginIncident();});
                    yield return AuditShot(recorder,game,"07-incident-decision",n=>{if(n==34)game.Resolve("scope");});
                    for(int guard=0;game.ResolutionActive&&guard<450;guard++)yield return PromoWait(1);
                    Assert.IsFalse(game.ResolutionActive);
                    yield return AuditShot(recorder,game,"08-monthly-count",n=>{if(n==12)game.OpenTab(0);});
                    yield return AuditShot(recorder,game,"09-month-change",n=>{if(n==12)game.Next();});
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)}));FinishStoryTestYear(game.State);
                    // これは年度の目標結果。年間評価の詳細は補足撮影で開く。
                    yield return AuditShot(recorder,game,"10-annual",n=>{if(n==12)game.OpenTab(0);});
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{careerOnly=true,career=game.Career}));yield return PromoWait(60);
                    yield return AuditShot(recorder,game,"11-settings",n=>{if(n==34)AuditClick("HomeSettings");});
                    yield return AuditShot(recorder,game,"12-settings-fast",n=>{if(n==34)AuditClick("SpeedFast");});
                    AuditClick("CloseDialog");yield return PromoWait(30);
                    game.StartYear(14);game.SkipTutorial();yield return PromoWait(60);
                    yield return AuditShot(recorder,game,"13-incident-fast",n=>{if(n==12)game.BeginIncident();});
                    yield return AuditShot(recorder,game,"14-resolution-fast",n=>{if(n==12)game.Resolve("scope");});
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();OpsPromoClock.ResumeFrom="";}
            Assert.AreEqual(old,UnityEngine.Time.captureFramerate);
        }

        private static void AuditSuccess(OpsGame game)
        {
            var session=game.Minigame;
            if(session is OpsMailMinigame mail&&mail.CanAnswer)game.AnswerMail(mail.Current.Suspicious);
            else if(session is OpsMfaMinigame mfa&&mfa.CanAnswer)game.AnswerMfa(mfa.Current.Legitimate);
            else if(session is OpsContainmentMinigame b)
            {int pc=Enumerable.Range(0,b.PCCount).Where(b.Visible).DefaultIfEmpty(-1).First();if(pc>=0)typeof(OpsGame).GetMethod("CutMinigamePC",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,new object[]{pc});}
            else if(session is OpsLogMinigame logs)
            {var row=logs.Visible.FirstOrDefault(r=>r.Suspicious&&!r.Hit);if(row!=null)game.HitLog(row.Id);}
            else if(session is OpsBlockMinigame blocks)
            {var task=blocks.Tasks.FirstOrDefault(t=>t.Placed==null);if(task!=null){game.SelectBlock(task.Id);game.PlaceSelectedBlock(task.Solution.Min(c=>c.Row),task.Solution.Min(c=>c.Column));}}
            else if(session is OpsRestoreMinigame restore)
            {var node=restore.Nodes.FirstOrDefault(n=>!n.Up&&!n.Busy&&n.Need.All(d=>restore.Nodes.First(x=>x.Id==d).Up));if(node!=null)game.BootRestore(node.Id);}
        }
        private static void AuditFailure(OpsGame game)
        {
            var session=game.Minigame;
            if(session is OpsMailMinigame mail&&mail.CanAnswer)game.AnswerMail(!mail.Current.Suspicious);
            else if(session is OpsMfaMinigame mfa&&mfa.CanAnswer)game.AnswerMfa(!mfa.Current.Legitimate);
            else if(session is OpsContainmentMinigame b)
            {int pc=Enumerable.Range(0,b.PCCount).Where(i=>!b.Visible(i)).DefaultIfEmpty(-1).First();if(pc>=0)typeof(OpsGame).GetMethod("CutMinigamePC",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,new object[]{pc});}
            else if(session is OpsLogMinigame logs)
            {var row=logs.Visible.FirstOrDefault(r=>!r.Suspicious&&!r.Hit);if(row!=null)game.HitLog(row.Id);}
            else if(session is OpsBlockMinigame blocks)
            {var task=blocks.Tasks.FirstOrDefault(t=>t.Placed==null);if(task!=null){game.SelectBlock(task.Id);game.PlaceSelectedBlock(99,99);}}
            else if(session is OpsRestoreMinigame restore)
            {var node=restore.Nodes.FirstOrDefault(n=>!n.Up&&!n.Busy&&n.Need.Any(d=>!restore.Nodes.First(x=>x.Id==d).Up));if(node!=null)game.BootRestore(node.Id);}
        }
        [UnityTest,Explicit("Next-22のミニゲーム棚卸し撮影だけ")]
        [Category("Capture")]
        public IEnumerator Next22Audit_六本の開始と成功失敗と結果を30Hzで撮る()
        {
            int old=UnityEngine.Time.captureFramerate;OpsPromoClock.Begin();
            try
            {
                SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);
                using(var recorder=new PromoRecorder("Artifacts/Next22/shots"))
                foreach(string id in OpsDailyPractice.Ids)
                {
                    game.StartYear(9);Assert.IsTrue(game.BeginDailyPractice(id,20261003));
                    yield return AuditShot(recorder,game,"20-"+id+"-start-success",n=>{if(n==12)game.StartMinigame();if(n==34||n==64)AuditSuccess(game);});
                    yield return AuditShot(recorder,game,"21-"+id+"-failure",n=>{if(n==34)AuditFailure(game);});
                    // 本編の数値に触れない練習。撮影しない待ちだけを進め、結果の演出は30Hzで撮る。
                    if(game.Minigame is OpsBlockMinigame)
                    {for(int i=0;i<12;i++)AuditSuccess(game);yield return AuditShot(recorder,game,"22-"+id+"-result",n=>{if(n==1)game.FinishBlocks();},90);}
                    else
                    {
                        for(int guard=0;game.Minigame.Phase==OpsMinigamePhase.Playing&&guard<5000;guard++)yield return PromoWait(1,()=>AuditSuccess(game));
                        Assert.AreEqual(OpsMinigamePhase.Result,game.Minigame.Phase,id);
                        yield return AuditShot(recorder,game,"22-"+id+"-result",null,90);
                    }
                    Assert.IsNotNull(Find<Transform>("MinigameRankStamp"));game.ConfirmMinigame();yield return PromoWait(10);
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();OpsPromoClock.ResumeFrom="";}
            Assert.AreEqual(old,UnityEngine.Time.captureFramerate);
        }

        [UnityTest,Explicit("Next-22の日記と年度の棚卸し撮影だけ")]
        [Category("Capture")]
        public IEnumerator Next22Audit_日記と開幕と因子とエンドレスを30Hzで撮る()
        {
            int old=UnityEngine.Time.captureFramerate;OpsPromoClock.Begin();
            try
            {
                SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);
                using(var recorder=new PromoRecorder("Artifacts/Next22/shots"))
                {
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)}));
                    game.Career.diary.Clear();foreach(int page in new[]{2,6})game.Career.diary.Add(new OpsDiaryRecord{key=page,content=page,mood=0,recap="今月の備えを確認した",thought="みんなで守れた",rank="A",season="秋",minigame=""});
                    yield return AuditShot(recorder,game,"30-diary-entry",n=>{if(n==12){game.OpenDiaryBook();AuditClick("DiaryPage_2");}},90);
                    yield return AuditShot(recorder,game,"31-diary-next",n=>{if(n==34){game.OpenDiaryBook();AuditClick("DiaryPage_6");}},90);
                    foreach(int year in new[]{2,3})
                    {
                        Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(year,0)}));
                        for(int stage=0;stage<5;stage++)
                        {game.PreviewYearOpening(stage,false);yield return AuditShot(recorder,game,"32-y"+year+"-opening-"+stage);}
                    }
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(2,8)}));yield return PromoWait(60);
                    yield return AuditShot(recorder,game,"33-boss-entry",n=>{if(n==12)game.BeginIncident();});
                    game.StartStory(14);for(int y=1;y<=3;y++){FinishStoryTestYear(game.State);if(y<3)game.NextStoryYear();}
                    game.OpenTab(0);yield return PromoWait(100);
                    yield return AuditShot(recorder,game,"34-factor-flip",n=>{if(n==1)AuditClick("BackHome");},90);
                    game.SkipFactorReveal();
                    game.RestoreProgress(new OpsProgress{single=new OpsState(14,true),career=new OpsCareer{endlessUnlocked=true}});game.StartEndless(14);FinishEndlessTestYear(game.State);game.OpenTab(0);yield return PromoWait(90);
                    yield return AuditShot(recorder,game,"35-endless-continue",n=>{if(n==34)AuditClick("NextEndlessYear");});
                    game.SkipYearOpening();game.AdvanceYearOpening();FinishEndlessTestYear(game.State);game.OpenTab(0);yield return PromoWait(75);
                    yield return AuditShot(recorder,game,"36-endless-confirm",n=>{if(n==34)AuditClick("RetireEndless");});
                    yield return AuditShot(recorder,game,"37-endless-retire",n=>{if(n==12)AuditClick("ConfirmRetireEndless");});
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();OpsPromoClock.ResumeFrom="";}
            Assert.AreEqual(old,UnityEngine.Time.captureFramerate);
        }

        [UnityTest,Explicit("一覧で対象を確かめて不足した場面だけ補足する")]
        [Category("Capture")]
        public IEnumerator Next22Audit_補足の年間詳細と封じ込め失敗と警告を30Hzで撮る()
        {
            int old=UnityEngine.Time.captureFramerate;OpsPromoClock.Begin();
            try
            {
                SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();AuditDefaults(game);
                using(var recorder=new PromoRecorder("Artifacts/Next22/shots"))
                {
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{careerOnly=true,career=game.Career}));yield return PromoWait(60);
                    yield return AuditShot(recorder,game,"15-button-press",n=>
                    {
                        var button=Find<Button>("HomeGuide");var feedback=button.GetComponent<OpsButtonFeedback>();
                        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                        if(n==25)feedback.OnPointerEnter(pointer);
                        if(n==34)feedback.OnPointerDown(pointer);
                        if(n==45){feedback.OnPointerUp(pointer);feedback.OnPointerExit(pointer);AuditClick("HomeGuide");}
                    });
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(1,0)}));FinishStoryTestYear(game.State);game.OpenTab(0);yield return PromoWait(90);
                    yield return AuditShot(recorder,game,"10a-annual-detail",n=>{if(n==12)AuditClick("StoryAnnualReview");},90);
                    Assert.IsNotNull(Find<Transform>("RankBadge"));
                    game.StartYear(9);Assert.IsTrue(game.BeginDailyPractice("B",20261003));game.StartMinigame();yield return PromoWait(5);
                    var b=(OpsContainmentMinigame)game.Minigame;int stopped=b.StoppedCount;
                    yield return AuditShot(recorder,game,"21a-B-real-failure",n=>{if(n==34)AuditFailure(game);});
                    Assert.AreEqual(stopped+1,b.StoppedCount,"正常端末を実際に切り離した");Assert.AreEqual(OpsMinigamePhase.Playing,b.Phase);
                    for(int guard=0;b.Phase==OpsMinigamePhase.Playing&&guard<1200;guard++)yield return PromoWait(1,()=>AuditSuccess(game));
                    Assert.AreEqual(OpsMinigamePhase.Result,b.Phase);
                    yield return AuditShot(recorder,game,"22a-B-result-count",null,90);game.ConfirmMinigame();
                    // 攻撃のケースは感染拡大で早期終了し得る。時間警告は正常想定の日で撮る。
                    game.StartYear(9);Assert.IsTrue(game.BeginDailyPractice("B",20261005));game.StartMinigame();
                    for(int guard=0;game.Minigame.Remaining>6.2f&&game.Minigame.Phase==OpsMinigamePhase.Playing&&guard<800;guard++)yield return PromoWait(1);
                    Assert.AreEqual(OpsMinigamePhase.Playing,game.Minigame.Phase);
                    Assert.LessOrEqual(game.Minigame.Remaining,6.2f);
                    yield return AuditShot(recorder,game,"23-B-danger");
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();OpsPromoClock.ResumeFrom="";}
            Assert.AreEqual(old,UnityEngine.Time.captureFramerate);
        }
    }
}
#endif
