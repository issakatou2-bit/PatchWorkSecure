using System.Collections;
using System.Linq;
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
        private static void AdvanceBestRestore(OpsRestoreMinigame game)
        {
            foreach(string id in game.BestOrder)
            {
                var n=game.Nodes.First(x=>x.Id==id);
                if(!n.Up&&!n.Busy&&game.Dependencies(n).All(d=>game.Nodes.First(x=>x.Id==d).Up))game.Boot(id);
            }
        }
        [Test] public void Next7Restore_五百事故の最善手順が目標内で百点になる()
        {
            for(int seed=0;seed<500;seed++)
            {
                var state=new OpsState(seed,true);string snapshot=JsonUtility.ToJson(state);
                string scenario=new[]{"power","change","storage"}[seed%3];var game=new OpsRestoreMinigame(state,scenario);var simulation=game.Simulate(game.BestOrder);
                Assert.AreEqual(game.BestLoss,simulation.Loss);
                foreach(var n in game.Nodes.Where(n=>n.Rto>=0))Assert.LessOrEqual(simulation.Finished[n.Id],n.Rto,"seed="+seed+" node="+n.Id);
                game.Start();int guard=0;
                while(game.Phase==OpsMinigamePhase.Playing&&guard++<100){AdvanceBestRestore(game);game.Tick(1);}
                Assert.AreEqual(OpsMinigamePhase.Result,game.Phase,"seed="+seed);Assert.AreEqual(100,game.Score,"seed="+seed);
                Assert.AreEqual(simulation.Loss,game.Loss);Assert.AreEqual(0,game.Overdue);Assert.AreEqual(snapshot,JsonUtility.ToJson(state));
            }
        }
        [Test] public void Next7Restore_順番失敗正常事件と安全確認を守る()
        {
            var game=new OpsRestoreMinigame(new OpsState(4,true),"power");game.Start();Assert.IsEmpty(game.Revealed);
            Assert.AreEqual(OpsRestoreBoot.Failed,game.Boot("order"));Assert.AreEqual(.5,game.Hour);Assert.AreEqual(1,game.Failures);Assert.Contains("db-order",game.Revealed.ToArray());
            Assert.AreEqual(OpsRestoreBoot.None,game.Boot("unknown"));game.Tick(float.NaN);Assert.AreEqual(.5,game.Hour);
            for(int seed=0;seed<40;seed++)
            {
                var normal=new OpsRestoreMinigame(new OpsState(seed,true),"power",true);int down=normal.Nodes.Count(n=>!n.Up);Assert.That(down,Is.InRange(1,2));normal.Start();
                for(int guard=0;normal.Phase==OpsMinigamePhase.Playing&&guard<100;guard++){AdvanceBestRestore(normal);normal.Tick(1);}
                Assert.AreEqual(0,normal.Loss);Assert.AreEqual(100-down*2,normal.Score);Assert.AreEqual(0,normal.Nodes.Count(n=>n.Rto>=0));
            }
            var ransom=new OpsRestoreMinigame(new OpsState(7,true),"ransom");ransom.Start();Assert.AreEqual(OpsRestoreBoot.Started,ransom.Boot("fs"));
            ransom.Tick(7);Assert.Greater(ransom.Reinfections,0);Assert.IsFalse(ransom.Nodes.First(n=>n.Id=="fs").Up);Assert.Contains("check-fs",ransom.Revealed.ToArray());
            Assert.AreEqual(OpsRestoreBoot.Started,ransom.Boot("check"));ransom.Tick(7);Assert.IsTrue(ransom.Nodes.First(n=>n.Id=="check").Up);
            Assert.AreEqual(OpsRestoreBoot.Started,ransom.Boot("fs"));ransom.Tick(7);Assert.IsTrue(ransom.Nodes.First(n=>n.Id=="fs").Up);
            Assert.IsFalse(DecisionFixture("ransom",false).SupportsRestore,"ランサム事件はBのまま");
        }
        [Test] public void Next7Restore_委任は完全互換で被害を変えず停止だけ幅内に収める()
        {
            foreach(string profile in new[]{"change","storage","service"})foreach(string response in new[]{"contain","scope","recover"})foreach(int score in new[]{0,49,50,51,100})
            {
                var state=DecisionFixture(profile,false);string snapshot=JsonUtility.ToJson(state);var normal=JsonUtility.FromJson<OpsState>(snapshot);normal.Resolve(response);
                var estimate=state.Estimate(response);var before=state.Preview(response);Assert.IsTrue(state.Resolve(response,score,score==50));
                Assert.AreEqual(before.loss,state.Latest.loss);Assert.That(state.Latest.downtime,Is.InRange(System.Math.Min(before.downtime,estimate.stopMin),System.Math.Max(before.downtime,estimate.stopMax)));
                if(score==50)Assert.AreEqual(JsonUtility.ToJson(normal),JsonUtility.ToJson(state));Assert.IsTrue(state.Valid());
            }
        }
        [UnityTest] public IEnumerator Next7RestoreUI_開始手順書結果委任正常を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var fixture=DecisionFixture("service",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            string before=JsonUtility.ToJson(game.State);game.ChooseResponse("scope");yield return new WaitForSecondsRealtime(.4f);Assert.IsInstanceOf<OpsRestoreMinigame>(game.Minigame);Capture("next7-g-start");CheckPointer("MinigameStart");CheckPointer("MinigameDelegate");
            Click("MinigameDelegate");yield return null;Capture("next7-g-delegate");Click("MinigameContinue");yield return WaitForResolution(game);
            var normal=JsonUtility.FromJson<OpsState>(before);normal.Resolve("scope");Assert.AreEqual(JsonUtility.ToJson(normal),JsonUtility.ToJson(game.State));
            foreach(bool runbook in new[]{false,true})
            {
                game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);if(runbook)game.State.levels[OpsCatalog.Index("runbook")]=1;game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
                game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.4f);var restore=(OpsRestoreMinigame)game.Minigame;
                Capture(runbook?"next7-g-play-runbook":"next7-g-play");Assert.AreEqual("pose_typing",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);
                CheckPointer("RestoreNode_net");Click("RestoreNode_net");Assert.IsTrue(restore.Nodes.First(n=>n.Id=="net").Busy);yield return null;
                int guard=0;while(restore.Phase==OpsMinigamePhase.Playing&&guard++<100){AdvanceBestRestore(restore);game.TickMinigame(1);yield return null;}
                Assert.AreEqual(100,restore.Score);yield return new WaitForSecondsRealtime(3.2f);Capture(runbook?"next7-g-result-runbook":"next7-g-result");
                Assert.IsNotNull(Find<TextMeshProUGUI>("RestoreResultRto_order"));CheckPointer("MinigameContinue");Click("MinigameContinue");yield return WaitForResolution(game);Assert.IsTrue(game.State.Valid());
            }
            // 現行の障害イベントにはcalmが無い。ルールは変えず、共通ホストで正常ケースの表示を検証。
            game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            string benignState=JsonUtility.ToJson(game.State);Assert.IsTrue(game.OpenMinigame(new OpsRestoreMinigame(game.State,"storage",true),"scope",s=>game.OpenTab(0)));
            Click("MinigameStart");yield return new WaitForSecondsRealtime(.3f);var benign=(OpsRestoreMinigame)game.Minigame;Assert.IsTrue(benign.Benign);Capture("next7-g-benign-play");
            for(int guard=0;benign.Phase==OpsMinigamePhase.Playing&&guard<100;guard++){AdvanceBestRestore(benign);game.TickMinigame(1);yield return null;}
            yield return new WaitForSecondsRealtime(3.2f);Capture("next7-g-benign-result");Assert.AreEqual(0,benign.Loss);Click("MinigameContinue");Assert.AreEqual(benignState,JsonUtility.ToJson(game.State));
            typeof(OpsGame).GetField("muted",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game,true);
            typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,false);
            Assert.IsTrue(game.OpenMinigame(new OpsRestoreMinigame(game.State,"storage",true),"scope",s=>game.OpenTab(0)));Click("MinigameStart");yield return null;
            Assert.IsEmpty(Find<TextMeshProUGUI>("NavigatorSpeech").text);var quiet=(OpsRestoreMinigame)game.Minigame;
            for(int guard=0;quiet.Phase==OpsMinigamePhase.Playing&&guard<100;guard++){AdvanceBestRestore(quiet);game.TickMinigame(1);yield return null;}
            yield return new WaitForSecondsRealtime(3.2f);Assert.IsEmpty(Find<TextMeshProUGUI>("MinigameMaxim").text);Assert.IsFalse(game.PortraitVoicePlaying);Click("MinigameContinue");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
