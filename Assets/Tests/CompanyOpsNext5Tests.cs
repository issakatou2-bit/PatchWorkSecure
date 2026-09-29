using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
        private static OpsState ContainmentFixture(bool benign,int seed=0,bool monitor=false,bool segment=false)
        {
            while(new OpsState(seed,true).Preview("scope").benign!=benign)seed++;
            var state=new OpsState(seed,true);state.levels[OpsCatalog.Index("inventory")]=1;
            state.levels[OpsCatalog.Index("monitor")]=monitor?1:0;state.levels[OpsCatalog.Index("segment")]=segment?1:0;state.BeginIncident();return state;
        }
        [Test] public void Next5Containment_観測と調査の三秒二回と部屋選択を守る()
        {
            var state=ContainmentFixture(false);var game=state.CreateContainment();Assert.IsNotNull(game);
            Assert.AreEqual("",game.Finding);Assert.AreEqual(0,game.TotalInfected);Assert.IsFalse(game.Scan());Assert.AreEqual(OpsTerminalCut.None,game.Cut(0));
            game.Start();Assert.AreEqual(0,game.VisibleCount);game.Tick(.1f);Assert.AreEqual(0,game.VisibleCount);
            int suspect=Enumerable.Range(0,game.PCCount).First(i=>game.Suspect(i));Assert.IsTrue(game.SelectRoom(suspect/5));Assert.IsTrue(game.Scan());Assert.IsTrue(game.Visible(suspect));Assert.AreEqual(1,game.ScansLeft);
            Assert.IsTrue(game.Scan());Assert.IsFalse(game.Scan());Assert.IsFalse(game.SelectRoom(-1));Assert.IsFalse(game.SelectRoom(4));
            game.Tick(3.1f);Assert.IsFalse(game.Scanning(game.SelectedRoom));Assert.IsFalse(game.Scan());
            var visible=ContainmentFixture(false,0,true).CreateContainment();visible.Start();Assert.AreEqual(2,visible.VisibleCount);
            int cut=Enumerable.Range(0,visible.PCCount).First(i=>visible.Visible(i));Assert.AreEqual(OpsTerminalCut.Infected,visible.Cut(cut));
            float before=visible.Elapsed;visible.Tick(.06f);Assert.AreEqual(before,visible.Elapsed);Assert.Greater(visible.HitStopRemaining,0);Assert.AreEqual(1,visible.Streak);
            Assert.AreEqual(OpsTerminalCut.None,visible.Cut(cut));Assert.AreEqual(1,visible.StoppedCount);
        }
        [Test] public void Next5Containment_分離は部屋越えを止め正常事件は感染せず止めすぎを減点する()
        {
            int cross=0;
            for(int seed=0;seed<30;seed++)
            {
                var game=ContainmentFixture(false,seed,false,true).CreateContainment();int links=0;
                game.Spread+=(a,b)=>{links++;Assert.AreEqual(a/5,b/5);};game.Start();game.Tick(25);Assert.AreEqual(OpsMinigamePhase.Result,game.Phase);
                Assert.AreEqual(Mathf.Clamp(100-game.Uncontained*12-game.TotalInfected*3-game.NormalStopped*4,0,100),game.Score);
                var open=ContainmentFixture(false,seed).CreateContainment();open.Spread+=(a,b)=>{if(a/5!=b/5)cross++;};open.Start();open.Tick(25);
            }
            Assert.Greater(cross,0);
            foreach(int stops in new[]{0,1,5,20})
            {
                var game=ContainmentFixture(true).CreateContainment();game.Start();game.Tick(1.2f);Assert.IsTrue(Enumerable.Range(0,20).Any(game.Suspect));Assert.AreEqual("",game.Finding);
                for(int i=0;i<stops;i++)Assert.AreEqual(OpsTerminalCut.Normal,game.Cut(i));
                Assert.AreEqual(0,game.VisibleCount);game.Tick(25);Assert.AreEqual(0,game.TotalInfected);Assert.AreEqual(0,game.Uncontained);Assert.AreEqual(100-stops*4,game.Score);
                StringAssert.Contains("感染ではありません",game.Finding);Assert.AreEqual(stops,game.NormalStopped);
            }
        }
        [Test] public void Next5Containment_年の状態と乱数に触れず同じ種なら同じ進行になる()
        {
            var state=ContainmentFixture(false);string before=JsonUtility.ToJson(state);var a=state.CreateContainment();var b=state.CreateContainment();a.Start();b.Start();
            for(int i=0;i<200;i++){a.Tick(.1f);b.Tick(.1f);Assert.AreEqual(a.VisibleCount,b.VisibleCount);Assert.AreEqual(a.Phase,b.Phase);}
            Assert.AreEqual(a.Score,b.Score);Assert.AreEqual(before,JsonUtility.ToJson(state));
            var c=ContainmentFixture(false,0,true,true).CreateContainment();c.Start();c.SelectRoom(1);Assert.AreEqual(5,c.StopRoom());Assert.AreEqual(0,c.StopRoom());Assert.AreEqual(5,c.StoppedCount);
            var noGame=new OpsState(14);Assert.IsNull(noGame.CreateContainment());noGame.month=3;noGame.BeginIncident();Assert.IsNull(noGame.CreateContainment());
        }
        [UnityTest] public IEnumerator Next5ContainmentUI_開始本編結果任せる正常事件と演出を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var fixture=ContainmentFixture(false,0,true,true);game.StartYear(fixture.seed);game.State.levels[OpsCatalog.Index("inventory")]=1;game.State.levels[OpsCatalog.Index("monitor")]=1;game.State.levels[OpsCatalog.Index("segment")]=1;
            game.BeginIncident();while(game.PhasePresentationRunning)yield return null;Find<Button>("Respond_scope").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(game.MinigameActive);Assert.AreEqual(OpsPhase.Incident,game.State.phase);CheckPointer("MinigameStart");CheckPointer("MinigameDelegate");Capture("150-minigame-start");
            var expected=game.State.Preview("scope");Click("MinigameDelegate");yield return null;Capture("153-minigame-delegated");CheckPointer("MinigameContinue");Click("MinigameContinue");
            Assert.AreEqual(expected.loss,game.State.Latest.loss);Assert.AreEqual(expected.downtime,game.State.Latest.downtime);Assert.IsTrue(game.State.Latest.delegated);yield return WaitForResolution(game);
            game.StartYear(fixture.seed);game.State.levels[OpsCatalog.Index("inventory")]=1;game.State.levels[OpsCatalog.Index("monitor")]=1;game.State.levels[OpsCatalog.Index("segment")]=1;
            game.BeginIncident();while(game.PhasePresentationRunning)yield return null;Find<Button>("Respond_scope").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Click("MinigameStart");yield return null;
            var session=(OpsContainmentMinigame)game.Minigame;Assert.AreEqual(20,session.PCCount);CheckPointer("MinigamePC_0");CheckPointer("MinigameRoom_1",new Vector2(102.75f,-18));CheckPointer("MinigameScan");CheckPointer("MinigameWide");
            Assert.AreEqual(18,Object.FindObjectsByType<OpsMinigameVisual>().Count(v=>v.Kind=="mote"));
            Canvas.ForceUpdateCanvases();
            foreach(var icon in Object.FindObjectsByType<OpsMinigameGraphic>().Where(g=>g.Kind=="pc"))
            {
                Assert.IsNotNull(icon.GetComponent<CanvasRenderer>());var mesh=icon.GetComponent<CanvasRenderer>().GetMesh();
                Assert.IsNotNull(mesh);Assert.Greater(mesh.vertexCount,0,"端末のアイコンが描画されていない");
            }
            string before=JsonUtility.ToJson(game.State);float deadline=Time.unscaledTime+9;
            while(!Object.FindObjectsByType<Transform>().Any(t=>t.name=="MinigameSpreadLine")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Object.FindObjectsByType<Transform>().Any(t=>t.name=="MinigameSpreadLine"));Capture("151-minigame-playing");
            foreach(int index in Enumerable.Range(0,20).Where(session.Visible).ToArray())Click("MinigamePC_"+index);
            Assert.Greater(session.HitStopRemaining,0);yield return null;
            Assert.AreEqual(10*session.StoppedCount,Object.FindObjectsByType<Transform>().Count(t=>t.name=="MinigameCutShard"));
            Capture("155-minigame-cut-burst");Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            deadline=Time.unscaledTime+23;while(session.Phase==OpsMinigamePhase.Playing&&Time.unscaledTime<deadline){foreach(int i in Enumerable.Range(0,20).Where(session.Visible).ToArray())Click("MinigamePC_"+i);yield return null;}
            Assert.AreEqual(OpsMinigamePhase.Result,session.Phase);yield return new WaitForSecondsRealtime(1.2f);Assert.IsTrue(game.MinigameCounting);Capture("156-minigame-result-counting");
            while(game.MinigameCounting)yield return null;yield return new WaitForSecondsRealtime(.55f);Capture("152-minigame-result");Assert.AreEqual(session.Score+"点",Find<TextMeshProUGUI>("MinigameScore").text);CheckPointer("MinigameContinue");
            Click("MinigameContinue");Assert.AreEqual(session.Score,game.State.Latest.minigameScore);Assert.IsFalse(game.State.Latest.delegated);Assert.IsTrue(game.State.Valid());yield return WaitForResolution(game);
            fixture=ContainmentFixture(true);game.StartYear(fixture.seed);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            Find<Button>("Respond_scope").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Click("MinigameStart");session=(OpsContainmentMinigame)game.Minigame;
            yield return new WaitForSecondsRealtime(1.3f);Assert.AreEqual("",session.Finding);Assert.AreEqual(0,session.VisibleCount);Capture("157-minigame-benign-suspect");
            Click("MinigamePC_0");game.TickMinigame(13);yield return null;Assert.IsTrue(Find<RectTransform>("MinigameDanger").gameObject.activeInHierarchy);Capture("158-minigame-danger");
            game.TickMinigame(10);yield return new WaitForSecondsRealtime(2.3f);Assert.AreEqual(Vector3.one*(900f/760f),Find<RectTransform>("MinigameCanvas").localScale,"揺れた後も画面の拡大率を保つ");Assert.AreEqual(96,session.Score);Assert.AreEqual(0,session.TotalInfected);StringAssert.Contains("感染ではありません",Find<TextMeshProUGUI>("MinigameResultDetail").text);Capture("154-minigame-benign-result");
            Click("MinigameContinue");yield return WaitForResolution(game);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
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
            Click("MinigameStart");game.TickMinigame(20);yield return new WaitForSecondsRealtime(2.3f);
            Assert.AreEqual("90点",Find<TextMeshProUGUI>("MinigameScore").text);Click("MinigameContinue");yield return null;
            Assert.AreEqual(90,received.Score);Assert.IsFalse(received.Delegated);Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
