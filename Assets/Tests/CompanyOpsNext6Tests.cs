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
        [Test] public void Next6Common_格言二十行を一般反応と混ぜず台本通り保持する()
        {
            var lines=OpsReactionBank.ScriptV2();Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,lines.Length);Assert.AreEqual(20,lines.Count(l=>l.id.StartsWith("maxim_")));Assert.AreEqual(54,OpsReactionBank.Defaults().Length);
            var csv=System.IO.File.ReadAllLines("Docs/Voice/hinata-script-v2.csv").Where(l=>l.StartsWith("maxim_"));
            foreach(var row in csv)
            {
                var cells=row.Split(',');var line=lines.Single(l=>l.id==cells[0]);Assert.AreEqual(cells[3],line.caption);Assert.AreEqual(cells[4],line.faceId);Assert.AreEqual(cells[5],line.poseId);Assert.IsNull(line.clip);Assert.IsFalse(OpsReactionBank.IsGeneralReaction(line));
            }
            var bank=ScriptableObject.CreateInstance<OpsReactionBank>();bank.lines=lines;var director=new OpsReactionDirector(24);
            for(int i=0;i<100;i++){Assert.IsTrue(director.TryChoose(bank,OpsReaction.Think,i*10,out var line,true));Assert.IsTrue(OpsReactionBank.IsGeneralReaction(line));}
            Object.DestroyImmediate(bank);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next6CommonUI_音声なし消音字幕設定格言と研修の進行を守る()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(14);yield return null;game.StopVoice();string before=JsonUtility.ToJson(game.State);
            game.OpenMailTraining();yield return new WaitForSecondsRealtime(.3f);Capture("next6-c-training-start");Click("MinigameStart");yield return new WaitForSecondsRealtime(.1f);var mail=(OpsMailMinigame)game.Minigame;
            while(mail.Phase==OpsMinigamePhase.Playing)
            {
                if(mail.CanAnswer){bool bad=mail.Current.Suspicious;Click(bad?"MailReport":"MailSafe");if(bad){Assert.AreEqual("mg_mail_catch",game.LastReactionId);Assert.AreEqual(OpsGame.SpeechLines(game.LastReactionCaption),Find<TextMeshProUGUI>("NavigatorSpeech").text);}game.TickMinigame(.5f);}
                yield return null;
            }
            yield return new WaitForSecondsRealtime(3.2f);Assert.IsFalse(game.MinigameCounting);StringAssert.StartsWith("maxim_",game.LastReactionId);Assert.IsFalse(game.PortraitVoicePlaying);Assert.IsFalse(game.ActiveVoiceBank.HasAudio);
            Assert.AreEqual(OpsGame.SpeechLines(game.LastReactionCaption),Find<TextMeshProUGUI>("MinigameMaxim").text);Assert.AreEqual(before,JsonUtility.ToJson(game.State));Capture("next6-c-maxim-result");
            int xp=game.State.staffExperience[0],work=game.State.capacity;Click("MinigameContinue");Assert.IsFalse(game.MinigameActive);Assert.IsFalse(game.PortraitVoicePlaying);
            if(game.VoicePending){var pending=(OpsReactionLine)typeof(OpsGame).GetField("pendingVoice",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);Assert.IsFalse(pending.id.StartsWith("maxim_"));}
            game.StopVoice();Assert.IsFalse(game.VoicePending);Assert.AreEqual(xp+2,game.State.staffExperience[0]);Assert.AreEqual(work-1,game.State.capacity);Assert.IsTrue(game.State.Valid());
            var fixture=DecisionFixture("remote",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);
            // 設定値だけを切り替え、実ユーザーのPlayerPrefsは変更しない。
            typeof(OpsGame).GetField("muted",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game,true);
            game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.1f);var mfa=(OpsMfaMinigame)game.Minigame;
            while(mfa.Current.Legitimate){Click("MfaAllow");game.TickMinigame(.7f);yield return null;}
            Click("MfaAllow");Assert.AreEqual("mg_mfa_breach",game.LastReactionId);Assert.IsFalse(game.PortraitVoicePlaying);game.TickMinigame(31);yield return new WaitForSecondsRealtime(3.2f);
            Assert.AreEqual("maxim_mfa",game.LastReactionId);StringAssert.Contains("心当たり",Find<TextMeshProUGUI>("MinigameMaxim").text);Capture("next6-d-maxim-result");Click("MinigameContinue");yield return WaitForResolution(game);
            typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");game.TickMinigame(31);yield return new WaitForSecondsRealtime(3.2f);
            Assert.IsEmpty(Find<TextMeshProUGUI>("NavigatorSpeech").text);Assert.IsEmpty(Find<TextMeshProUGUI>("MinigameMaxim").text);Capture("next6-d-captions-off");Click("MinigameContinue");yield return WaitForResolution(game);Assert.IsTrue(game.State.Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
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
        private static OpsState LegacyMfaFixture(bool benign,int seed=0)
        {
            for(int n=seed;n<seed+10000;n++)
            {
                var state=new OpsState(n);state.BeginIncident();state.Resolve("scope");state.NextMonth();
                if(state.Preview("scope").benign!=benign)continue;state.BeginIncident();Assert.IsTrue(state.Valid());return state;
            }
            throw new System.InvalidOperationException("旧年度の認証事件が見つからない");
        }
        [Test] public void Next6Mfa_社員照合番号疲労攻撃正常採点と抽選を守る()
        {
            bool spamSeen=false;
            for(int seed=0;seed<45;seed++)foreach(bool benign in new[]{false,true})
            {
                var state=benign?LegacyMfaFixture(true,seed):DecisionFixture("remote",false,seed);state.levels[OpsCatalog.Index("mfa")]=2;
                var snapshot=JsonUtility.ToJson(state);var a=state.CreateMfa();var b=state.CreateMfa();Assert.IsTrue(a.NumberMatch);Assert.IsTrue(a.Start());b.Start();
                float firstDelay=0,lastDelay=0;int attacks=0,legits=0;
                while(a.Phase==OpsMinigamePhase.Playing)
                {
                    if(a.CanAnswer)
                    {
                        var q=a.Current;Assert.AreEqual(q.Number,b.Current.Number);Assert.AreEqual(q.Who,b.Current.Who);
                        Assert.AreEqual(q.Legitimate,a.People[q.Who].LoggingIn);Assert.AreEqual(4,a.People.Count);
                        if(q.Legitimate)legits++;else{attacks++;spamSeen|=q.Spam;}
                        bool answer=a.Count%5==0?!q.Legitimate:q.Legitimate;
                        a.Answer(answer);b.Answer(answer);Assert.AreEqual(OpsMfaAnswer.None,a.Answer(true));lastDelay=a.DelayRemaining;if(firstDelay==0)firstDelay=lastDelay;
                    }
                    a.Tick(.65f);b.Tick(.65f);
                }
                Assert.That(lastDelay,Is.LessThan(firstDelay));Assert.Greater(legits,0);if(benign)Assert.AreEqual(0,attacks);
                Assert.AreEqual(Mathf.Clamp(a.Correct*8-a.Breaches*25-a.Blocks*6,0,100),a.Score);Assert.AreEqual(a.Score,b.Score);Assert.AreEqual(snapshot,JsonUtility.ToJson(state));
            }
            Assert.IsTrue(spamSeen);var timeout=DecisionFixture("session",false).CreateMfa();Assert.IsFalse(timeout.NumberMatch);timeout.Start();timeout.Tick(float.NaN);Assert.AreEqual(30,timeout.Remaining);timeout.Tick(35);Assert.AreEqual(0,timeout.Score);
        }
        [Test] public void Next6Mfa_全対象の委任は既存と完全同一で点数は結果を広げない()
        {
            foreach(string profile in new[]{"session","remote","device"})foreach(string response in new[]{"contain","scope","recover"})
            {
                var source=DecisionFixture(profile,false);Assert.IsTrue(source.SupportsMfa);var snapshot=JsonUtility.ToJson(source);
                var original=source.Preview(response);var estimate=source.Estimate(response);
                foreach(int score in new[]{0,25,49,50,51,75,100})
                {
                    var state=JsonUtility.FromJson<OpsState>(snapshot);Assert.IsTrue(state.Resolve(response,score,score==50));Assert.IsTrue(state.Valid());
                    if(score==50){var normal=JsonUtility.FromJson<OpsState>(snapshot);normal.Resolve(response);Assert.AreEqual(JsonUtility.ToJson(normal),JsonUtility.ToJson(state));}
                    Assert.That(state.Latest.loss,Is.InRange(System.Math.Min(original.loss,estimate.lossMin),System.Math.Max(original.loss,estimate.lossMax)));
                    Assert.That(state.Latest.downtime,Is.InRange(System.Math.Min(original.downtime,estimate.stopMin),System.Math.Max(original.downtime,estimate.stopMax)));
                }
            }
            var legacy=LegacyMfaFixture(true);var before=JsonUtility.ToJson(legacy);var delegated=legacy.CreateMfa();delegated.Delegate();var normalLegacy=JsonUtility.FromJson<OpsState>(before);normalLegacy.Resolve("scope");legacy.Resolve("scope",delegated.Score,delegated.Delegated);Assert.AreEqual(JsonUtility.ToJson(normalLegacy),JsonUtility.ToJson(legacy));
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next6MfaUI_社員番号一致結果委任正常と演出を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var fixture=DecisionFixture("remote",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            game.ChooseResponse("scope");yield return new WaitForSecondsRealtime(.3f);StringAssert.Contains("本人の今の行動",Find<TextMeshProUGUI>("MinigameInstructions").text);StringAssert.DoesNotContain("漏れた",Find<TextMeshProUGUI>("MinigameInstructions").text);Capture("next6-d-start");CheckPointer("MinigameDelegate");var original=game.State.Preview("scope");Click("MinigameDelegate");yield return null;Capture("next6-d-delegate");Click("MinigameContinue");Assert.AreEqual(original.loss,game.State.Latest.loss);Assert.AreEqual(original.downtime,game.State.Latest.downtime);yield return WaitForResolution(game);
            game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.6f);Capture("next6-d-play");CheckPointer("MfaDeny");CheckPointer("MfaAllow");
            var session=(OpsMfaMinigame)game.Minigame;string snapshot=JsonUtility.ToJson(game.State);
            while(session.Phase==OpsMinigamePhase.Playing)
            {
                if(session.CanAnswer)
                {
                    bool answer=session.Current.Legitimate;
                    if(session.Breaches==0&&!answer)answer=true;else if(session.Blocks==0&&answer)answer=false;
                    Click(answer?"MfaAllow":"MfaDeny");game.TickMinigame(.7f);
                }
                if(session.Correct>=9&&session.Breaches==1&&session.Blocks==1)game.TickMinigame(30);
                yield return null;
            }
            Assert.AreEqual(snapshot,JsonUtility.ToJson(game.State));yield return new WaitForSecondsRealtime(2.5f);Capture("next6-d-result");StringAssert.Contains("正解 9／侵入 1（被害）／足止め 1",Find<TextMeshProUGUI>("MinigameResultDetail").text);CheckPointer("MinigameContinue");Click("MinigameContinue");yield return WaitForResolution(game);
            game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.State.levels[OpsCatalog.Index("mfa")]=2;game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.6f);
            session=(OpsMfaMinigame)game.Minigame;bool legit=false,spam=false;
            while(!legit||!spam)
            {
                Assert.AreEqual(OpsMinigamePhase.Playing,session.Phase);var q=session.Current;
                if(q!=null)
                {
                    Assert.AreEqual(q.Legitimate?"画面の番号 <b>"+q.Number+"</b> と一致":"入力された番号がない",Find<TextMeshProUGUI>("MfaNumber").text);
                    if(q.Legitimate){Capture("next6-d-play-num");legit=true;}else if(q.Spam){Capture("next6-d-play-num-attack");spam=true;}
                    Click(q.Legitimate?"MfaAllow":"MfaDeny");if(!q.Legitimate){yield return null;Assert.AreEqual(10,Object.FindObjectsByType<Transform>().Count(t=>t.name=="MinigameCutShard"));Capture("next6-d-burst");}
                    game.TickMinigame(.7f);yield return new WaitForSecondsRealtime(.4f);
                }
                else yield return null;
            }
            game.TickMinigame(session.Remaining-5);yield return null;Assert.IsTrue(Find<RectTransform>("MinigameDanger").gameObject.activeSelf);Capture("next6-d-danger");game.TickMinigame(6);yield return new WaitForSecondsRealtime(2.5f);Click("MinigameContinue");yield return WaitForResolution(game);
            fixture=LegacyMfaFixture(true);game.StartYear(fixture.seed);game.State.eventRules=0;game.State.eventSchedule=null;game.State.ticketSchedule=null;game.State.BeginIncident();game.State.Resolve("scope");game.State.NextMonth();Assert.IsTrue(game.State.Preview("scope").benign);game.OpenTab(0);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.6f);Capture("next6-d-benign-play");
            session=(OpsMfaMinigame)game.Minigame;while(session.Phase==OpsMinigamePhase.Playing){if(session.CanAnswer){Assert.IsTrue(session.Current.Legitimate);Click("MfaDeny");}game.TickMinigame(.7f);yield return null;}
            Assert.AreEqual(0,session.Breaches);Assert.Greater(session.Blocks,0);Assert.AreEqual(0,session.Score);yield return new WaitForSecondsRealtime(2.5f);Capture("next6-d-benign-result");Click("MinigameContinue");yield return WaitForResolution(game);Assert.IsTrue(game.State.Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
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
        [Category("Capture")]
        [UnityTest] public IEnumerator Next6MailUI_開始本編結果委任正常と手がかりを撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<PatchWorkSecure.CompanyOps.OpsGame>();game.UseLocalTestVoices=false;
            var fixture=DecisionFixture("bec",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            game.ChooseResponse("scope");yield return new WaitForSecondsRealtime(.3f);Assert.IsInstanceOf<PatchWorkSecure.CompanyOps.OpsMailMinigame>(game.Minigame);Capture("next6-c-start");CheckPointer("MinigameStart");CheckPointer("MinigameDelegate");
            var original=game.State.Preview("scope");Click("MinigameDelegate");yield return null;Capture("next6-c-delegate");Click("MinigameContinue");Assert.AreEqual(original.loss,game.State.Latest.loss);Assert.AreEqual(original.downtime,game.State.Latest.downtime);yield return WaitForResolution(game);
            game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.State.levels[PatchWorkSecure.CompanyOps.OpsCatalog.Index("education")]=1;game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            game.ChooseResponse("scope");Click("MinigameStart");yield return new WaitForSecondsRealtime(.6f);
            var session=(PatchWorkSecure.CompanyOps.OpsMailMinigame)game.Minigame;string snapshot=JsonUtility.ToJson(game.State);CheckPointer("MailSafe");CheckPointer("MailReport");if(session.Current.Body.Contains("<mark="))StringAssert.Contains("<b><link=\"mail-hint\">",Find<TextMeshProUGUI>("MailBodyText").text);Capture("next6-c-play-hint");
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
