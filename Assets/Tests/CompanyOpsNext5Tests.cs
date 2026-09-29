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
        [Test] public void Next5Score_五十点の互換と幅外をさらに広げない係数を三方針で確認する()
        {
            int outside=0,cases=0;
            for(int seed=0;seed<35;seed++)for(int month=0;month<12;month++)foreach(string response in new[]{"contain","scope","recover"})
            {
                var state=new OpsState(seed){month=month,audited=seed%2==0};if(!state.SupportsContainment)continue;
                for(int i=0;i<state.levels.Length;i++)state.levels[i]=(seed+month)%3;
                state.BeginIncident();var original=state.Preview(response);var range=state.Estimate(response);string snapshot=JsonUtility.ToJson(state);
                if(original.loss<range.lossMin||original.loss>range.lossMax||original.downtime<range.stopMin||original.downtime>range.stopMax)outside++;
                int previousLoss=200,previousStop=200;
                foreach(int score in new[]{0,1,25,49,50,51,75,99,100})
                {
                    var copy=JsonUtility.FromJson<OpsState>(snapshot);Assert.IsTrue(copy.Resolve(response,score,false));var actual=copy.Latest;
                    if(score==50){Assert.AreEqual(original.loss,actual.loss);Assert.AreEqual(original.downtime,actual.downtime);Assert.AreEqual(original.businessLoss,actual.businessLoss);}
                    else
                    {
                        double f=1-OpsCatalog.MinigameResultInfluence*(score-50)/50;
                        int expectedLoss=System.Math.Max(System.Math.Min(original.loss,range.lossMin),System.Math.Min(System.Math.Max(original.loss,range.lossMax),(int)System.Math.Round(original.loss*f,System.MidpointRounding.AwayFromZero)));
                        int expectedStop=System.Math.Max(System.Math.Min(original.downtime,range.stopMin),System.Math.Min(System.Math.Max(original.downtime,range.stopMax),(int)System.Math.Round(original.downtime*f,System.MidpointRounding.AwayFromZero)));
                        Assert.AreEqual(expectedLoss,actual.loss);Assert.AreEqual(expectedStop,actual.downtime);
                    }
                    Assert.LessOrEqual(actual.loss,previousLoss);Assert.LessOrEqual(actual.downtime,previousStop);previousLoss=actual.loss;previousStop=actual.downtime;
                    Assert.AreEqual(JsonUtility.ToJson(range),JsonUtility.ToJson(actual.forecast));Assert.AreEqual(original.cost,actual.cost);Assert.AreEqual(original.recovery,actual.recovery);
                    Assert.IsTrue(actual.minigameRecorded);Assert.AreEqual(score,actual.EffectiveMinigameScore);Assert.IsFalse(actual.delegated);
                    Assert.IsTrue(actual.investmentEffects.All(e=>e.avoidedLoss>=0&&e.avoidedDowntime>=0));
                    Assert.AreEqual(state.budget-actual.loss-actual.cost+actual.peakBudgetBonus,copy.budget);
                }
                var auto=JsonUtility.FromJson<OpsState>(snapshot);var delegated=JsonUtility.FromJson<OpsState>(snapshot);
                auto.Resolve(response);delegated.Resolve(response,50,true);Assert.AreEqual(JsonUtility.ToJson(auto),JsonUtility.ToJson(delegated));cases++;
            }
            Assert.Greater(outside,0);Assert.Greater(cases,100);
        }
        [Test] public void Next5Score_対象外と不正点数と二重反映を拒否し保存して戻せる()
        {
            var state=new OpsState(14);state.BeginIncident();string before=JsonUtility.ToJson(state);
            Assert.IsFalse(state.Resolve("scope",-1,false));Assert.IsFalse(state.Resolve("scope",101,false));Assert.IsFalse(state.Resolve("scope",80,true));Assert.AreEqual(before,JsonUtility.ToJson(state));
            Assert.IsTrue(state.Resolve("scope",100,false));Assert.IsTrue(state.Valid());var copy=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));Assert.IsTrue(copy.Valid());Assert.AreEqual(100,copy.Latest.EffectiveMinigameScore);
            before=JsonUtility.ToJson(state);Assert.IsFalse(state.Resolve("scope",0,false));Assert.AreEqual(before,JsonUtility.ToJson(state));
            state=new OpsState(14){month=3};Assert.IsFalse(state.SupportsContainment);state.BeginIncident();before=JsonUtility.ToJson(state);
            Assert.IsFalse(state.Resolve("scope",100,false));Assert.AreEqual(before,JsonUtility.ToJson(state));Assert.IsTrue(state.Resolve("scope"));Assert.IsFalse(state.Latest.minigameRecorded);
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
