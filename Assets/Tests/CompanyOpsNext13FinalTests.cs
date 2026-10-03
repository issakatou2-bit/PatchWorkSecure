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
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static OpsStory FinalStoryFixture()
        {
            var story=AlliesStory(3);
            for(int m=0;m<11;m++)
            {
                story.state.budget=400;story.state.stability=100;
                DiaryTestMonth(story.state);Assert.IsTrue(story.state.NextMonth());
            }
            story.state.budget=400;story.state.stability=100;Assert.IsTrue(story.state.BeginIncident());
            Assert.AreEqual("y3-final",story.state.CurrentEvent.id);Assert.IsTrue(story.Valid());return story;
        }
        [Test] public void Next13Final_両方五十点の全結果が従来と一致し別の年度に接続しない()
        {
            var story=FinalStoryFixture();string original=JsonUtility.ToJson(story.state);
            foreach(string response in new[]{"contain","scope","recover"})
            {
                var old=JsonUtility.FromJson<OpsState>(original);var current=JsonUtility.FromJson<OpsState>(original);
                Assert.IsTrue(old.Resolve(response));Assert.IsTrue(current.ResolveFinal(response,50,true,50,true));
                Assert.AreEqual(JsonUtility.ToJson(old),JsonUtility.ToJson(current));Assert.IsTrue(current.Valid());
            }
            var s=story.state;s.endlessYear=4;Assert.IsFalse(s.SupportsFinalRecovery);Assert.IsNull(s.CreateFinalRestore());Assert.IsFalse(s.ResolveFinal("scope",50,true,50,true));
            s.endlessYear=0;s.storyCalendarYear=2;Assert.IsFalse(s.SupportsFinalRecovery);
            Assert.IsFalse(new OpsState(14,true){month=11}.SupportsFinalRecovery);
        }
        [Test] public void Next13Final_被害と停止を別々に採点し現行結果を含む公開幅を守る()
        {
            string original=JsonUtility.ToJson(FinalStoryFixture().state);
            foreach(string response in new[]{"contain","scope","recover"})foreach(int b in new[]{0,49,50,51,100})foreach(int g in new[]{0,49,50,51,100})
            {
                var s=JsonUtility.FromJson<OpsState>(original);var old=s.Preview(response);var range=s.Estimate(response);
                int Bound(int value,int low,int high,int score)=>score==50?value:Math.Max(Math.Min(value,low),Math.Min(Math.Max(value,high),(int)Math.Round(value*(1-OpsCatalog.MinigameResultInfluence*(score-50)/50),MidpointRounding.AwayFromZero)));
                Assert.IsTrue(s.ResolveFinal(response,b,b==50,g,g==50));Assert.AreEqual(Bound(old.loss,range.lossMin,range.lossMax,b),s.Latest.loss);
                Assert.AreEqual(Bound(old.downtime,range.stopMin,range.stopMax,g),s.Latest.downtime);Assert.IsTrue(s.Valid());
                Assert.AreEqual(g!=50,s.Latest.recoveryMinigameRecorded);
            }
            var invalid=JsonUtility.FromJson<OpsState>(original);
            Assert.IsFalse(invalid.ResolveFinal("scope",50,true,101,false));Assert.IsFalse(invalid.ResolveFinal("scope",50,true,49,true));Assert.AreEqual(original,JsonUtility.ToJson(invalid));
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next13FinalUI_総決算だけ二本立てで字幕と再暗号化と委任を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var story=FinalStoryFixture();Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));while(game.PhasePresentationRunning)yield return null;
            string before=JsonUtility.ToJson(game.State);game.ChooseResponse("scope");Assert.IsInstanceOf<OpsContainmentMinigame>(game.Minigame);
            Click("MinigameDelegate");yield return null;Capture("next13-final-b-delegate");Click("MinigameContinue");yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.IsInstanceOf<OpsRestoreMinigame>(game.Minigame);Assert.AreEqual("ransom",((OpsRestoreMinigame)game.Minigame).Scenario);
            Assert.AreEqual("eng_found",game.LastReactionId);Capture("next13-final-g-bridge-engineer");
            yield return new WaitForSecondsRealtime(6);Assert.AreEqual("final_restore_bridge",game.LastReactionId);StringAssert.Contains("確かめる",Find<TextMeshProUGUI>("FinalRecoveryVoice").text);Capture("next13-final-g-bridge-hinata");
            Click("MinigameDelegate");yield return null;Click("MinigameContinue");yield return WaitForResolution(game);
            var old=JsonUtility.FromJson<OpsState>(before);old.Resolve("scope");Assert.AreEqual(JsonUtility.ToJson(old),JsonUtility.ToJson(game.State));

            story=FinalStoryFixture();Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));while(game.PhasePresentationRunning)yield return null;
            game.ChooseResponse("scope");Click("MinigameDelegate");Click("MinigameContinue");Click("MinigameStart");yield return new WaitForSecondsRealtime(.35f);
            var restore=(OpsRestoreMinigame)game.Minigame;Capture("next13-final-g-ransom");Assert.IsNotNull(Find<TextMeshProUGUI>("RestoreScenarioText"));Assert.IsFalse(Object.FindObjectsByType<OpsVoiceCircle>().Any(),"開始後に前の顔札を残さない");Assert.AreEqual("pose_magnifier",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);CheckPointer("RestoreNode_fs");Click("RestoreNode_fs");game.TickMinigame(7);yield return null;
            Assert.Greater(restore.Reinfections,0);Assert.IsFalse(restore.Nodes.First(n=>n.Id=="fs").Up);Capture("next13-final-g-reencrypted");
            for(int guard=0;restore.Phase==OpsMinigamePhase.Playing&&guard<100;guard++){AdvanceBestRestore(restore);game.TickMinigame(1);yield return null;}
            Assert.AreEqual(OpsMinigamePhase.Result,restore.Phase);yield return new WaitForSecondsRealtime(3.2f);Capture("next13-final-g-result");Click("MinigameContinue");yield return WaitForResolution(game);Assert.IsTrue(game.State.Valid());
            Assert.IsTrue(game.State.Latest.recoveryMinigameRecorded);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
