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
        private class ScoredMinigame:OpsMinigame
        {
            private readonly int score;
            public ScoredMinigame(int value,OpsState state):base("感染の封じ込め",state){score=value;}
            protected override void OnTimeUp()=>Complete(score);
        }
        [Test] public void Next5Foundation_進行設備の固定点数範囲と旧保存の五十点を守る()
        {
            var state=new OpsState(14);state.levels[OpsCatalog.Index("monitor")]=1;
            var game=new OpsMinigame("感染の封じ込め",state);state.levels[OpsCatalog.Index("monitor")]=0;
            Assert.IsTrue(game.Monitor);Assert.IsFalse(game.Backup);Assert.IsFalse(game.Segment);
            game.Tick(25);Assert.AreEqual(OpsMinigamePhase.Brief,game.Phase);
            Assert.IsTrue(game.Delegate());Assert.IsFalse(game.Delegate());Assert.IsFalse(game.Start());
            Assert.IsTrue(game.Delegated);Assert.AreEqual(50,game.Score);Assert.AreEqual("B",game.Grade);
            Assert.AreEqual(50,JsonUtility.FromJson<OpsOutcome>("{}").EffectiveMinigameScore);
            foreach(int score in new[]{-5,0,49,50,69,70,79,80,84,85,100,105})
            {
                var timed=new ScoredMinigame(score,state);Assert.IsTrue(timed.Start());Assert.IsFalse(timed.Start());Assert.IsFalse(timed.Delegate());
                timed.Tick(float.NaN);timed.Tick(float.PositiveInfinity);timed.Tick(-1);Assert.AreEqual(20,timed.Remaining);
                timed.Tick(21);Assert.AreEqual(OpsMinigamePhase.Result,timed.Phase);Assert.AreEqual(Mathf.Clamp(score,0,100),timed.Score);Assert.AreEqual(0,timed.Remaining);
                Assert.AreEqual(score>=85?"S":score>=70?"A":score>=50?"B":"C",timed.Grade);
                Assert.AreEqual(score>=80?"mg_end_good":score>=50?"mg_end_ok":"mg_end_bad",timed.EndVoice);
            }
            state.BeginIncident();state.Resolve("scope");state.Latest.minigameRecorded=true;state.Latest.minigameScore=50;state.Latest.delegated=true;Assert.IsTrue(state.Valid());
            state.Latest.minigameScore=51;Assert.IsFalse(state.Valid());state.Latest.delegated=false;Assert.IsTrue(state.Valid());state.Latest.minigameScore=101;Assert.IsFalse(state.Valid());
        }
        [UnityTest] public IEnumerator Next5Foundation_共通開始結果と十六行は音源なしでも進む()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();Assert.IsNotNull(game.GetComponent<OpsMinigameHost>());
            game.UseLocalTestVoices=false;game.StartYear(14);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            string before=JsonUtility.ToJson(game.State);OpsMinigame received=null;
            Assert.IsTrue(game.OpenMinigame(new OpsMinigame("感染の封じ込め",game.State),"scope",s=>received=s));
            yield return new WaitForSecondsRealtime(.3f);CheckPointer("MinigameStart");CheckPointer("MinigameDelegate");
            Assert.AreEqual(16,game.ActiveVoiceBank.lines.Count(l=>l.id.StartsWith("mg_")));Assert.IsFalse(game.ActiveVoiceBank.HasAudio);
            foreach(var line in OpsReactionBank.ScriptV2().Where(l=>l.id.StartsWith("mg_")))
            {
                var actual=game.ActiveVoiceBank.Find(line.id);Assert.AreEqual(line.caption,actual.caption);Assert.AreEqual(line.faceId,actual.faceId);Assert.AreEqual(line.poseId,actual.poseId);
                game.SpeakSceneLine(line.id,0);yield return new WaitForSecondsRealtime(.12f);Assert.IsFalse(game.PortraitVoicePlaying);
                Assert.AreEqual(OpsGame.SpeechLines(line.caption),Find<TextMeshProUGUI>("NavigatorSpeech").text);
                Assert.AreEqual(line.poseId,Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);game.StopVoice();
            }
            Click("MinigameDelegate");yield return null;Assert.AreEqual("50点",Find<TextMeshProUGUI>("MinigameScore").text);CheckPointer("MinigameContinue");
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));Click("MinigameContinue");yield return null;
            Assert.IsNotNull(received);Assert.IsTrue(received.Delegated);Assert.IsFalse(game.MinigameActive);
            // 共通タイマーはプレイ中だけ進み、結果確認でだけ年間側へ返す。
            game.OpenTab(0);Assert.IsTrue(game.OpenMinigame(new ScoredMinigame(90,game.State),"scope",s=>received=s));
            Click("MinigameStart");game.TickMinigame(20);yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual("90点",Find<TextMeshProUGUI>("MinigameScore").text);Click("MinigameContinue");yield return null;
            Assert.AreEqual(90,received.Score);Assert.IsFalse(received.Delegated);Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
