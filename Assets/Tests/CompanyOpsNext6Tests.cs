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
        private static OpsState DecisionFixture(string profile,bool benign,int seed=0)
        {
            for(int n=seed;n<seed+10000;n++)
            {
                var state=new PatchWorkSecure.CompanyOps.OpsState(n,true);
                var e=PatchWorkSecure.CompanyOps.OpsEventCatalog.Events.First(ev=>ev.profile==profile&&(!benign||!string.IsNullOrEmpty(ev.calm)));
                SetEvent(state,e.id);if(state.Preview("scope").benign!=benign)continue;
                state.BeginIncident();return state;
            }
            throw new System.InvalidOperationException("指定の事件状態が見つからない");
        }
        [Test] public void Next6Mail_十通の配分と急ぐ本物と採点と年の乱数を守る()
        {
            for(int seed=0;seed<45;seed++)foreach(bool benign in new[]{false,true})
            {
                var state=DecisionFixture("bec",benign,seed);string snapshot=JsonUtility.ToJson(state);
                var a=state.CreateMail();var b=state.CreateMail();Assert.AreEqual(40,a.Duration);Assert.IsTrue(a.Start());b.Start();
                int bad=0,urgent=0,total=0;
                while(a.Phase==PatchWorkSecure.CompanyOps.OpsMinigamePhase.Playing)
                {
                    Assert.AreEqual(a.Current.Id,b.Current.Id);if(a.Current.Suspicious)bad++;if(a.Current.Urgent)urgent++;total++;
                    Assert.AreEqual(PatchWorkSecure.CompanyOps.OpsMailAnswer.Correct,a.Answer(a.Current.Suspicious));b.Answer(b.Current.Suspicious);
                    Assert.AreEqual(PatchWorkSecure.CompanyOps.OpsMailAnswer.None,a.Answer(true));a.Tick(.5f);b.Tick(.5f);
                }
                Assert.AreEqual(10,total);Assert.That(bad,benign?Is.EqualTo(1):Is.InRange(5,6));Assert.GreaterOrEqual(urgent,1);
                Assert.AreEqual(100,a.Score);Assert.AreEqual(a.Score,b.Score);Assert.AreEqual(snapshot,JsonUtility.ToJson(state));
            }
            var source=DecisionFixture("targeted",false);var session=source.CreateMail();session.Start();int correct=0,miss=0,fp=0;
            for(int i=0;i<10;i++)
            {
                var q=session.Current;bool wrong=i<3;var answer=session.Answer(wrong?!q.Suspicious:q.Suspicious);
                if(answer==PatchWorkSecure.CompanyOps.OpsMailAnswer.Correct)correct++;else if(q.Suspicious)miss++;else fp++;
                if(wrong){Assert.IsNotEmpty(session.Feedback);session.Tick(1);Assert.IsFalse(session.CanAnswer);session.Tick(.7f);}else session.Tick(.5f);
            }
            int expected=Mathf.Clamp(correct*10-miss*15-fp*5+Mathf.RoundToInt(session.Remaining),0,100);Assert.AreEqual(expected,session.Score);
            var timeout=source.CreateMail();timeout.Start();timeout.Tick(float.NaN);Assert.AreEqual(40,timeout.Remaining);timeout.Tick(45);Assert.AreEqual(0,timeout.Score);
        }
        [Test] public void Next6Mail_委任は既存結果と同一で研修だけ経験二を一度反映する()
        {
            foreach(string profile in new[]{"bec","targeted"})foreach(string response in new[]{"contain","scope","recover"})foreach(bool benign in new[]{true,false})
            {
                if(benign&&!OpsEventCatalog.Events.Any(e=>e.profile==profile&&!string.IsNullOrEmpty(e.calm)))continue;
                if(benign&&!OpsEventCatalog.Events.Any(e=>e.profile==profile&&!string.IsNullOrEmpty(e.calm)))continue;
                var state=DecisionFixture(profile,benign);string snapshot=JsonUtility.ToJson(state);var original=state.Preview(response);var session=state.CreateMail();session.Delegate();
                var normal=JsonUtility.FromJson<PatchWorkSecure.CompanyOps.OpsState>(snapshot);normal.Resolve(response);
                Assert.IsTrue(state.Resolve(response,session.Score,session.Delegated));Assert.AreEqual(JsonUtility.ToJson(normal),JsonUtility.ToJson(state));
                Assert.AreEqual(original.loss,state.Latest.loss);Assert.AreEqual(original.downtime,state.Latest.downtime);
            }
            var training=new PatchWorkSecure.CompanyOps.OpsState(14,true);string before=JsonUtility.ToJson(training);var skip=training.CreateMailPractice();skip.Delegate();Assert.IsFalse(training.CompleteMailTraining(skip));Assert.AreEqual(before,JsonUtility.ToJson(training));
            var practice=training.CreateMailPractice();practice.Start();practice.Tick(45);int xp=training.staffExperience[0],capacity=training.capacity;
            Assert.IsTrue(training.CompleteMailTraining(practice));Assert.AreEqual(xp+2,training.staffExperience[0]);Assert.AreEqual(capacity-1,training.capacity);
            before=JsonUtility.ToJson(training);Assert.IsFalse(training.CompleteMailTraining(practice));Assert.IsNull(training.CreateMailPractice());Assert.AreEqual(before,JsonUtility.ToJson(training));Assert.IsTrue(training.Valid());
        }
        [UnityTest] public IEnumerator Next6MailUI_開始本編結果委任正常と手がかりを撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<PatchWorkSecure.CompanyOps.OpsGame>();game.UseLocalTestVoices=false;
            var fixture=DecisionFixture("bec",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            game.ChooseResponse("scope");yield return new WaitForSecondsRealtime(.3f);Assert.IsInstanceOf<PatchWorkSecure.CompanyOps.OpsMailMinigame>(game.Minigame);Capture("next6-c-start");CheckPointer("MinigameStart");CheckPointer("MinigameDelegate");
            var original=game.State.Preview("scope");Click("MinigameDelegate");yield return null;Capture("next6-c-delegate");Click("MinigameContinue");Assert.AreEqual(original.loss,game.State.Latest.loss);Assert.AreEqual(original.downtime,game.State.Latest.downtime);yield return WaitForResolution(game);
            game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.State.levels[PatchWorkSecure.CompanyOps.OpsCatalog.Index("education")]=1;game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.6f);
            var session=(PatchWorkSecure.CompanyOps.OpsMailMinigame)game.Minigame;string snapshot=JsonUtility.ToJson(game.State);CheckPointer("MailSafe");CheckPointer("MailReport");Capture("next6-c-play-hint");
            if(session.Current.Link!=""){game.ShowMailLink(session.Current.Link);StringAssert.Contains(session.Current.Link,Find<TextMeshProUGUI>("MailStatus").text);Capture("next6-c-link");game.ShowMailLink("");}
            while(session.Phase==PatchWorkSecure.CompanyOps.OpsMinigamePhase.Playing)
            {
                if(session.CanAnswer)
                {
                    if(session.Current.Suspicious&&session.Learned.Count==0){Click("MailReport");yield return null;Assert.AreEqual(10,Object.FindObjectsByType<Transform>().Count(t=>t.name=="MinigameCutShard"));Capture("next6-c-burst");}
                    else Click(session.Current.Suspicious?"MailReport":"MailSafe");
                }
                yield return null;
            }
            Assert.AreEqual(snapshot,JsonUtility.ToJson(game.State));yield return new WaitForSecondsRealtime(2.5f);Capture("next6-c-result");StringAssert.Contains("正解 10／見逃し 0／止めすぎ 0",Find<TextMeshProUGUI>("MinigameResultDetail").text.Replace("（被害につながる）","").Replace("（業務が遅れる）",""));
            Assert.AreEqual("100点",Find<TextMeshProUGUI>("MinigameScore").text);CheckPointer("MinigameContinue");Click("MinigameContinue");yield return WaitForResolution(game);
            fixture=DecisionFixture("bec",true);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.6f);Capture("next6-c-benign-play");
            session=(PatchWorkSecure.CompanyOps.OpsMailMinigame)game.Minigame;
            while(session.Phase==PatchWorkSecure.CompanyOps.OpsMinigamePhase.Playing){if(session.CanAnswer){Click("MailReport");game.TickMinigame(1.7f);}yield return null;}
            yield return new WaitForSecondsRealtime(2.5f);Assert.AreEqual(9,session.FalseAlarms);Capture("next6-c-benign-result");Click("MinigameContinue");yield return WaitForResolution(game);Assert.IsTrue(game.State.Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
