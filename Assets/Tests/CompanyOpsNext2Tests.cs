using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next3Title_承認済み一枚絵と縮尺と字幕と低減設定を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1.5f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var kv=Find<Image>("TitleKeyVisual");Assert.AreSame(game.PlanningArt.titleKeyVisual,kv.sprite);Assert.AreEqual(new Vector2(1672,941),kv.sprite.rect.size);Assert.AreEqual(new Vector2(0,1),kv.rectTransform.pivot);
            Assert.IsNull(kv.GetComponent<OpsPlanningMotion>());Assert.AreEqual(Vector3.one,kv.rectTransform.localScale);Assert.AreEqual(1,Find<RectTransform>("TitleBrand").localScale.x);
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="HomePortrait"||t.name=="HomeGreeting"));CheckPointer("NewYear");CheckPointer("HomeGuide");CheckPointer("HomeSettings");Capture("134-title-kv");
            game.SpeakSceneLine("think_01",0);yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual(game.LastReactionCaption,Find<TextMeshProUGUI>("TitleCaption").text);Assert.IsFalse(game.PortraitVoicePlaying);
            game.SpeakSceneLine(game.ActiveVoiceBank.lines.OrderByDescending(l=>l.caption.Length).First().id,0);yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(TextWrappingModes.NoWrap,Find<TextMeshProUGUI>("TitleCaption").textWrappingMode);Assert.IsFalse(Find<TextMeshProUGUI>("TitleCaption").text.Contains("\n"));CheckText();
            Click("HomeSettings");yield return new WaitForSecondsRealtime(.5f);Click("CaptionToggle");Click("ReduceMotion");Click("CloseDialog");yield return new WaitForSecondsRealtime(.5f);
            game.SpeakSceneLine("think_02",0);yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual("",Find<TextMeshProUGUI>("TitleCaption").text);Assert.AreEqual(1,Find<RectTransform>("TitleKeyVisual").localScale.x);CheckText();Capture("134-title-kv-reduced");LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next3Bubbles_固定抽選と一度だけの報酬と旧保存を検証する()
        {
            int rare=0,consult=0,total=0;
            for(int seed=0;seed<200;seed++)
            {
                var state=new OpsState(seed,true);var same=new OpsState(seed,true);CollectionAssert.AreEqual(state.bubbleSchedule,same.bubbleSchedule);
                rare+=state.bubbleSchedule.Count(k=>k==7);consult+=state.bubbleSchedule.Count(k=>k==6);total+=48;
                for(int i=0;i<4;i++)
                {
                    int kind=state.BubbleKind(i),work=state.capacity,knowledge=state.SituationKnowledge;var metrics=state.ReportMetrics;
                    Assert.IsTrue(state.PopBubble(i));Assert.IsFalse(state.PopBubble(i));Assert.AreEqual(work,state.capacity);
                    if(kind==7)CollectionAssert.AreEqual(metrics,state.ReportMetrics);
                    if(kind==6)Assert.AreEqual(knowledge+1,state.SituationKnowledge);
                    Assert.IsTrue(state.Valid());
                }
                var copy=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));Assert.IsTrue(copy.Valid());Assert.AreEqual(4,copy.BubbleDone);Assert.AreEqual(state.clueCollected,copy.clueCollected);
                state.BeginIncident();Assert.IsFalse(state.PopBubble(0));state.Resolve("contain");state.NextMonth();if(state.phase==OpsPhase.Planning){Assert.IsFalse(state.clueCollected);Assert.AreEqual(0,state.BubbleDone);}
            }
            Assert.That(rare/(float)total,Is.InRange(.02f,.04f));Assert.That(consult/(200f*12),Is.InRange(.44f,.54f));
            var legacy=new OpsState(14){bubbleRules=0,bubbleSchedule=null,poppedBubbles=null};Assert.IsTrue(legacy.Valid());Assert.AreEqual(-1,legacy.BubbleKind(0));Assert.IsFalse(legacy.PopBubble(0));
            var invalid=new OpsState(14);invalid.poppedBubbles[11]=1;Assert.IsFalse(invalid.Valid());invalid.poppedBubbles[11]=0;invalid.clueCollected=true;Assert.IsFalse(invalid.Valid());
        }
        [UnityTest] public IEnumerator Next3Bubbles_出現と弾ける瞬間と手がかりとレアを撮影する()
        {
            int seed=Enumerable.Range(0,10000).First(s=>{var a=new OpsState(s,true).bubbleSchedule.Take(4);return a.Contains(6)&&a.Contains(7);});
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(seed);yield return new WaitForSecondsRealtime(3);
            Assert.AreEqual(4,Find<RectTransform>("OfficeStage").GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("OfficeBubble")));Capture("133-bubbles-appear");
            for(int i=0;i<4;i++){CheckPointer("OfficeBubble"+i);Assert.AreEqual(70,Find<RectTransform>("OfficeBubble"+i).rect.width);}
            int normal=Enumerable.Range(0,4).First(i=>game.State.BubbleKind(i)<6);int work=game.State.capacity;Click("OfficeBubble"+normal);yield return new WaitForSecondsRealtime(.12f);Assert.AreEqual(8,Object.FindObjectsByType<RectTransform>().Count(t=>t.name.StartsWith("BubbleDrop")));Capture("133-bubbles-pop");yield return new WaitForSecondsRealtime(.5f);
            int clue=Enumerable.Range(0,4).First(i=>game.State.BubbleKind(i)==6);int before=game.State.SituationKnowledge;Click("OfficeBubble"+clue);yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(before+1,game.State.SituationKnowledge);Assert.AreEqual(game.State.Current.staff,Find<TextMeshProUGUI>("BubbleClueText").text);Capture("133-bubbles-clue");
            int gold=Enumerable.Range(0,4).First(i=>game.State.BubbleKind(i)==7);CheckPointer("OfficeBubble"+gold);Click("OfficeBubble"+gold);yield return new WaitForSecondsRealtime(.12f);Capture("133-bubbles-rare");yield return new WaitForSecondsRealtime(.5f);Assert.IsNotNull(Find<TextMeshProUGUI>("BubbleThanksText"));StringAssert.StartsWith("extra_",game.LastReactionId);Assert.AreEqual(work,game.State.capacity);Assert.IsTrue(game.State.Valid());CheckText();
            game.OpenTab(0);yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual("困りごと 3 / 4",Find<TextMeshProUGUI>("BubbleDone").text);Assert.IsNotNull(Find<TextMeshProUGUI>("BubbleClueText"));game.BeginIncident();yield return new WaitForSecondsRealtime(1.4f);Assert.IsFalse(Object.FindObjectsByType<Button>().Any(b=>b.name.StartsWith("OfficeBubble")));Assert.AreEqual(game.State.SituationKnowledge+" / 4",Find<TextMeshProUGUI>("KnowledgeValue").text);Capture("133-bubbles-incident-clue");LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next3Depth_五規則と旧年度と保存の境界を確認する()
        {
            var state=new OpsState(14,true);SetEvent(state,"ransom-extortion");Assert.AreEqual(4,state.Blindness);Assert.AreEqual(11,state.Estimate("scope").cost);Assert.AreEqual(24,state.ScopeOversight);Assert.AreEqual(2,state.Preview("recover").cost);
            var legacy=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));legacy.decisionDepthRules=0;legacy.incidentTimes=null;
            Assert.AreEqual(3,legacy.Estimate("scope").cost);Assert.AreEqual(4,legacy.Preview("recover").cost);Assert.AreEqual(24,state.Preview("scope").loss-legacy.Preview("scope").loss);
            state.levels[OpsCatalog.Index("segment")]=1;Assert.AreEqual(22,state.ScopeOversight);state.audited=true;Assert.AreEqual(2,state.Blindness);state.levels[OpsCatalog.Index("monitor")]=1;state.levels[OpsCatalog.Index("inventory")]=1;Assert.AreEqual(0,state.Blindness);
            Assert.AreEqual(4,state.incidentTimes.Count(t=>t>0));var saved=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));CollectionAssert.AreEqual(state.incidentTimes,saved.incidentTimes);Assert.IsTrue(saved.Valid());Assert.AreEqual(0,legacy.IncidentTime);
            foreach(var p in OpsEventCatalog.Profiles)Assert.AreEqual(new[]{"ransom","supply","vulnerability","targeted","remote"}.Contains(p.id)?2:new[]{"change","storage","service","ddos"}.Contains(p.id)?0:1,p.spread);
        }
        [Test] public void Next3Depth_把握と見積もりに未確認の真相を漏らさない()
        {
            foreach(int rules in new[]{0,1})foreach(bool audit in new[]{false,true})
            {
                var first=new OpsState(14,true){audited=audit,decisionDepthRules=rules};
                for(int seed=15;seed<45;seed++)
                {
                    var other=new OpsState(seed,true){audited=audit,decisionDepthRules=rules,eventSchedule=first.eventSchedule,incidentTimes=first.incidentTimes};
                    Assert.AreEqual(first.SituationKnowledge,other.SituationKnowledge);
                    foreach(string response in new[]{"contain","scope","recover"})Assert.AreEqual(JsonUtility.ToJson(first.Estimate(response)),JsonUtility.ToJson(other.Estimate(response)));
                }
            }
        }
        [UnityTest] public IEnumerator Next3Depth_時間帯と把握ゲージは公開状態に一致する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);game.BeginIncident();yield return new WaitForSecondsRealtime(1.4f);
            Assert.AreEqual(game.State.IncidentTimeLabel,Find<TextMeshProUGUI>("IncidentTimeLabel").text);Assert.AreEqual(game.State.SituationKnowledge+" / 4",Find<TextMeshProUGUI>("KnowledgeValue").text);CheckPointer("KnowledgeCard");Capture("132-depth-incident");CheckText();LogAssert.NoUnexpectedReceived();
            int quietSeed=Enumerable.Range(0,100).First(s=>new OpsState(s,true).IncidentTime>0);game.StartYear(quietSeed);game.BeginIncident();yield return new WaitForSecondsRealtime(1.4f);Assert.Greater(game.State.IncidentTime,0);Assert.AreEqual(game.State.IncidentTimeLabel,Find<TextMeshProUGUI>("IncidentTimeLabel").text);Capture("132-depth-incident-quiet");CheckText();LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next3Polish_月報と計画の八件を同条件で撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);game.State.budget=59;game.OpenTab(0);game.StopVoice();game.SpeakSceneLine("growth_01",0);yield return new WaitForSecondsRealtime(1.4f);Capture("130-polish-planning");
            var budget=Find<RectTransform>("予算Value");var unit=Find<RectTransform>("BudgetUnit");Assert.Greater(unit.anchoredPosition.x,budget.anchoredPosition.x+budget.rect.width+4);Assert.Less(budget.rect.height-budget.anchoredPosition.y,56);
            foreach(int value in new[]{0,59,100,108,999,5000}){game.State.budget=value;game.OpenTab(0);CheckText();budget=Find<RectTransform>("予算Value");unit=Find<RectTransform>("BudgetUnit");Assert.Greater(unit.anchoredPosition.x,budget.anchoredPosition.x+budget.rect.width+4);}
            game.State.budget=59;game.OpenTab(0);game.SpeakSceneLine("rankup",0);yield return new WaitForSecondsRealtime(.2f);CheckText();Capture("130-polish-planning-rankup");
            foreach(var room in Find<RectTransform>("OfficeStage").GetComponentsInChildren<RectTransform>().Where(r=>r.name.StartsWith("Room_")))Assert.LessOrEqual(room.GetComponentsInChildren<OpsIncidentGraphic>().Count(g=>g.name=="UninstalledFrame"),3);
            Assert.Greater(Find<RectTransform>("Pin_change").anchoredPosition.x,283);Assert.AreEqual(TextWrappingModes.NoWrap,Find<TextMeshProUGUI>("NavigatorSpeech").textWrappingMode);
            ChooseDelegatedWork(game,"audit");game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);game.StopVoice();yield return new WaitForSecondsRealtime(.8f);Capture("130-polish-monthly-empty");
            Assert.IsFalse(Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.name=="ImpactSummary"));Assert.IsNotNull(Find<RectTransform>("PotentialEquipmentFrame"));Assert.IsFalse(Object.FindObjectsByType<OpsPortraitIdentity>().Any(i=>i.FaceIcon));Assert.AreEqual(3,Find<RectTransform>("MonthlyTeam").GetComponentsInChildren<RectTransform>().Count(t=>t.name.StartsWith("NextSupportFace")));CheckText();
            Assert.AreEqual(3,Find<RectTransform>("MonthlyTeam").GetComponentsInChildren<OpsIncidentGraphic>().Count(g=>g.Kind=="staff-face"));
            game.StartYear(14);ChooseDelegatedWork(game,game.State.CurrentMission.actionA);ChooseDelegatedWork(game,game.State.CurrentMission.actionB);game.Buy(OpsCatalog.Index(game.State.CurrentMission.projectA));game.Buy(OpsCatalog.Index(game.State.CurrentMission.projectB));game.BeginIncident();game.Resolve("recover");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(.8f);Capture("130-polish-monthly-mission");Assert.IsTrue(game.State.CurrentMissionCompleted);CheckPointer("MissionConversation");Assert.GreaterOrEqual(Find<Button>("MissionConversation").GetComponent<RectTransform>().rect.height,32);CheckText();LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator VoiceV2_届いた音源の全IDと全文再生を確認し素材なしでも成功する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(2);
            var bank=game.ActiveVoiceBank;var expected=OpsReactionBank.ScriptV2();Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,bank.lines.Length);Assert.IsFalse(game.Navigator.Reactions.HasAudio);
            foreach(var row in expected)
            {
                var line=bank.Find(row.id);Assert.AreEqual(row.caption,line.caption);Assert.AreEqual(row.faceId,line.faceId);Assert.AreEqual(row.poseId,line.poseId);
                if(line.clip!=null){Assert.AreEqual(line.id,line.clip.name);Assert.Greater(line.clip.samples,0);Assert.Greater(line.clip.length,0);}
            }
            string before=JsonUtility.ToJson(game.State);
            foreach(var line in bank.lines.Where(l=>l.fullSpeech))
            {
                game.SpeakSceneLine(line.id,0);yield return new WaitForSecondsRealtime(.08f);Assert.AreEqual(OpsGame.SpeechLines(line.caption),Find<TextMeshProUGUI>("NavigatorSpeech").text);
                if(line.clip!=null)Assert.IsTrue(game.PortraitVoicePlaying,line.id);else Assert.IsFalse(game.PortraitVoicePlaying);
                game.StopVoice();Assert.IsFalse(game.PortraitVoicePlaying);
            }
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
        }
        [Test] public void VoiceV2_台本は九場面六本と追加十一と全文五十二で重複しない()
        {
            var lines=OpsReactionBank.ScriptV2();Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,lines.Length);Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,lines.Select(l=>l.id).Distinct().Count());Assert.AreEqual(52,lines.Count(l=>l.fullSpeech&&!l.id.StartsWith("diary_")));Assert.AreEqual(38,lines.Count(l=>l.fullSpeech&&l.id.StartsWith("diary_")));Assert.AreEqual(11,lines.Count(l=>l.extra));
            foreach(var l in lines){Assert.IsNotEmpty(l.caption);Assert.IsNotEmpty(l.faceId);Assert.IsNotEmpty(l.poseId);Assert.IsNull(l.clip);}
            foreach(OpsReaction r in System.Enum.GetValues(typeof(OpsReaction)))Assert.AreEqual(6,lines.Count(l=>OpsReactionBank.IsGeneralReaction(l)&&l.reaction==r));
            for(int m=0;m<12;m++){var s=new OpsState(1){month=m};Assert.AreEqual("mission_accept_"+(m+1).ToString("00"),OpsGame.MissionVoiceId(s));}
            var random=new OpsState(14,true);SetEvent(random,"vuln-web");Assert.AreEqual("mission_accept_06",OpsGame.MissionVoiceId(random));
        }
        [UnityTest] public IEnumerator VoiceV2_素材なしでも全文字幕と台本のポーズで進み操作が予約を取り消す()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);
            Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,game.ActiveVoiceBank.lines.Length);Assert.IsFalse(game.ActiveVoiceBank.HasAudio);Assert.AreEqual("season_04",game.LastReactionId);yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual(OpsGame.SpeechLines(game.ActiveVoiceBank.Find("season_04").caption),Find<TextMeshProUGUI>("NavigatorSpeech").text);Assert.AreEqual("pose_wave",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);Assert.IsFalse(game.PortraitVoicePlaying);
            game.StartYear(14);Assert.IsTrue(game.VoicePending,"新年度を始め直したら同じ季節でも読み直す");yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual(OpsGame.SpeechLines(game.ActiveVoiceBank.Find("season_04").caption),Find<TextMeshProUGUI>("NavigatorSpeech").text);
            string before=JsonUtility.ToJson(game.State);game.SpeakSceneLine("incident_unconfirmed",2);Assert.IsTrue(game.VoicePending);Click("Stat_0");yield return null;Assert.IsFalse(game.VoicePending);Assert.AreEqual(before,JsonUtility.ToJson(game.State));Click("CloseDialog");
            Assert.IsTrue(game.StartTutorial());yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual("tutorial_1",game.LastReactionId);Assert.AreEqual(OpsGame.SpeechLines(game.ActiveVoiceBank.Find("tutorial_1").caption),Find<TextMeshProUGUI>("TutorialLine").text);
            Click("Stat_0");Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual("tutorial_2",game.LastReactionId);Assert.AreEqual(OpsGame.SpeechLines(game.ActiveVoiceBank.Find("tutorial_2").caption),Find<TextMeshProUGUI>("TutorialLine").text);Capture("127-voice-tutorial-no-audio");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator VoiceV2_全文再生とBGM減衰と設定と操作キャンセルがゲーム数値に触れない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);yield return new WaitForSecondsRealtime(2);
            var original=game.Navigator;var persona=Object.Instantiate(original);var bank=ScriptableObject.CreateInstance<OpsReactionBank>();bank.lines=OpsReactionBank.ScriptV2();var clip=AudioClip.Create("全文再生の検証用無音",144000,1,24000,false);bank.Find("rankup").clip=clip;bank.Find("tutorial_1").clip=clip;persona.Reactions=bank;game.Navigator=persona;
            try
            {
                string before=JsonUtility.ToJson(game.State);var music=game.GetComponents<AudioSource>().First(s=>s.clip==game.Sounds.planningMusic);float volume=music.volume;
                Assert.IsTrue(game.SpeakSceneLine("rankup",.2f));yield return new WaitForSecondsRealtime(.4f);Assert.IsTrue(game.PortraitVoicePlaying);Assert.Less(music.volume,volume*.7f);Assert.AreEqual("sparkle",Find<OpsPortraitAnimator>("NavigatorPortrait").ExpressionId);
                game.StopVoice();Assert.IsTrue(game.SpeakSceneLine("rankup",0),"別の昇格でも同じ全文を再生する");yield return new WaitForSecondsRealtime(.15f);Assert.IsTrue(game.PortraitVoicePlaying);
                var source=game.GetComponents<AudioSource>().Single(s=>s.clip==clip);Click("Menu");yield return null;Assert.IsFalse(source.isPlaying);Assert.IsFalse(game.VoicePending);
                Find<Slider>("VoiceVolume").value=.36f;Assert.IsTrue(game.SpeakSceneLine("tutorial_1",0));yield return new WaitForSecondsRealtime(.15f);Assert.AreEqual(.36f,source.volume,.001f);Assert.IsTrue(source.isPlaying);
                Click("CaptionToggle");yield return null;Assert.IsFalse(game.CaptionsEnabled);Assert.AreEqual("",Find<TextMeshProUGUI>("NavigatorSpeech").text);Assert.IsFalse(source.isPlaying);
                Click("CaptionToggle");Find<Slider>("VoiceVolume").value=0;Assert.IsTrue(game.SpeakSceneLine("incident_unconfirmed",0));yield return new WaitForSecondsRealtime(.15f);Assert.IsFalse(game.PortraitVoicePlaying);Assert.AreEqual(OpsGame.SpeechLines(bank.Find("incident_unconfirmed").caption),Find<TextMeshProUGUI>("NavigatorSpeech").text);
                Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
            }
            finally{game.StopVoice();game.Navigator=original;Object.Destroy(persona);Object.Destroy(bank);Object.Destroy(clip);}
        }
        [UnityTest] public IEnumerator VoiceV2_依頼と事件と発動と月報は実際の場面に連動し音声なしで完走する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);yield return new WaitForSecondsRealtime(2);
            game.Buy(OpsCatalog.Index("backup"));ChooseDelegatedWork(game,"audit");ChooseDelegatedWork(game,"map");Click("ConsultationDetails");yield return new WaitForSecondsRealtime(.4f);Click("AcceptMission");Assert.AreEqual(OpsGame.MissionVoiceId(game.State),game.LastReactionId);
            game.BeginIncident();Assert.AreEqual("incident_start",game.LastReactionId);yield return new WaitForSecondsRealtime(3.7f);Assert.AreEqual("incident_unconfirmed",game.LastReactionId);yield return new WaitForSecondsRealtime(1.4f);Capture("128-voice-unconfirmed");
            game.Resolve("recover");float limit=Time.realtimeSinceStartup+4;while(game.LastReactionId!="incident_activate"&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.AreEqual("incident_activate",game.LastReactionId);Assert.IsTrue(game.State.Latest.investmentEffects.Any(e=>e.avoidedLoss>0||e.avoidedDowntime>0));
            limit=Time.realtimeSinceStartup+10;
            while(game.ResolutionActive&&Time.realtimeSinceStartup<limit)
            {
                if(game.LastReactionId=="incident_activate"||game.LastReactionId=="incident_missing")Assert.AreEqual(OpsGame.SpeechLines(game.LastReactionCaption),Find<TextMeshProUGUI>("ResolutionReaction").text);
                yield return null;
            }
            Assert.IsFalse(game.ResolutionActive);
            // 月報では依頼の結果のあとに未読の格言が続く。固定秒数後の最後の台詞ではなく順番を確認する。
            limit=Time.realtimeSinceStartup+8;while(game.LastReactionId!="mission_done"&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.AreEqual("mission_done",game.LastReactionId);Assert.IsTrue(game.State.CurrentMissionCompleted);
            limit=Time.realtimeSinceStartup+2;string doneCaption=OpsGame.SpeechLines(game.ActiveVoiceBank.Find("mission_done").caption);
            while(Find<TextMeshProUGUI>("NavigatorSpeech").text!=doneCaption&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.AreEqual(doneCaption,Find<TextMeshProUGUI>("NavigatorSpeech").text);Capture("129-voice-mission-done");
            limit=Time.realtimeSinceStartup+5;while(game.LastReactionId!="maxim_report_v2"&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.AreEqual("maxim_report_v2",game.LastReactionId);Assert.Contains("maxim_report",game.Career.yearMaxims);
            Click("NextMonth");yield return null;Assert.AreEqual("season_05",game.LastReactionId);Assert.AreEqual(1,game.State.month);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator UIRepair_四件の修正前後を同じ条件で撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1.4f);var game=Object.FindAnyObjectByType<OpsGame>();Capture("126-ui-title");
            foreach(string id in new[]{"NewYear","ContinueYear","HomeGuide","HomeSettings"})
            {
                var b=Find<Button>(id);Assert.AreEqual(Color.white,b.colors.disabledColor);Assert.AreEqual(Color.white,b.GetComponent<Image>().color);
                var g=b.GetComponent<OpsKitGradient>();Assert.Greater(g.Top.r,.95f);if(id!="NewYear")Assert.AreEqual(Color.white,g.Top);else Assert.Less(g.Bottom.g,.4f);
            }
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="HomeGreeting"));Assert.IsNotNull(Find<Image>("TitleKeyVisual").sprite);
            game.StartYear(14);yield return new WaitForSecondsRealtime(2);Capture("126-ui-planning");
            Click("ConsultationDetails");yield return new WaitForSecondsRealtime(.6f);Capture("126-ui-brief");AssertSpeechName("MissionHint","MissionHintName");
            Assert.AreEqual(-280,Find<RectTransform>("OpenEventBrief").anchoredPosition.y);Assert.AreEqual(-280,Find<RectTransform>("EmployeeConsultation").anchoredPosition.y);CheckPointer("OpenEventBrief");CheckPointer("EmployeeConsultation");Click("CloseDialog");yield return null;
            SetEvent(game.State,"vuln-web");game.BeginIncident();yield return new WaitForSecondsRealtime(1.6f);Capture("126-ui-incident");AssertSpeechName("Navigator","NavigatorName");game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(1);Capture("126-ui-monthly");
            var title=Find<TextMeshProUGUI>("MissionTitle");Assert.AreEqual("使っている\nライブラリが対象？\nへの備え",title.text);title.ForceMeshUpdate();
            int start=title.text.IndexOf("ライブラリ");int lineNumber=title.textInfo.characterInfo[start].lineNumber;
            for(int i=start;i<start+5;i++)Assert.AreEqual(lineNumber,title.textInfo.characterInfo[i].lineNumber);Assert.IsFalse(title.isTextOverflowing);AssertSpeechName("Navigator","NavigatorName");LogAssert.NoUnexpectedReceived();
        }
        private static void AssertSpeechName(string bubbleName,string textName)
        {
            var bubble=Find<RectTransform>(bubbleName);var name=Find<TextMeshProUGUI>(textName);var tag=(RectTransform)name.transform.parent;
            Assert.AreEqual(bubble.parent,tag.parent);Assert.GreaterOrEqual(tag.anchoredPosition.y-tag.rect.height,bubble.anchoredPosition.y+6);name.ForceMeshUpdate();Assert.IsFalse(name.isTextOverflowing);
        }
        [Test] public void UIRepair_全依頼の語の区切りは元の依頼名を保つ()
        {
            foreach(var m in OpsCatalog.Missions)Assert.AreEqual(m.title,OpsGame.ReportMissionTitle(m.title).Replace("\n",""));
            foreach(var e in OpsEventCatalog.Events)Assert.AreEqual(e.title+"への備え",OpsGame.ReportMissionTitle(e.title+"への備え").Replace("\n",""));
        }
        [UnityTest] public IEnumerator NextScreens4_ランクの恩恵と行動の粒は実状態に連動し省演出でも動く()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");game.State.culture=49;game.OpenTab(0);
                Assert.IsFalse(game.State.CultureEarlySignal);game.ChooseAction("listen");
                // 粒の寿命は実時間0.6秒。重い描画フレームでも消滅後を検査しないよう、生成直後に確認する。
                Assert.IsTrue(game.State.CultureEarlySignal);Assert.IsNotEmpty(UnityEngine.Object.FindObjectsByType<OpsStatSpark>());StringAssert.Contains("次月の兆候",Find<TextMeshProUGUI>("RankBenefitBandText").text);yield return new WaitForSecondsRealtime(.15f);Capture(reduced?"125-next-rank-reduced":"125-next-rank-up");
                yield return new WaitForSecondsRealtime(.9f);CheckPointer("CultureEarlySignal");Click("CultureEarlySignal");yield return new WaitForSecondsRealtime(.4f);StringAssert.Contains("確定",Find<TextMeshProUGUI>("DialogBody").text);Click("CloseDialog");
                game.State.culture=65;game.State.audited=false;Assert.AreEqual(8,game.State.EstimateMargin);game.OpenTab(0);Click("Stat_3");yield return new WaitForSecondsRealtime(.5f);StringAssert.Contains("1狭める",Find<TextMeshProUGUI>("CurrentRankBenefit").text);Capture("125-next-rank-details");Click("CloseDialog");
                game.State.trust=50;game.State.capacity=4;game.State.audited=true;game.State.proposed=false;int money=game.State.budget;int grant=12+game.State.Evidence*3+1;game.ChooseAction("proposal");Assert.AreEqual(money+grant,game.State.budget);
            }
            var old=new OpsState(14){culture=100,trust=100,rankBenefitRules=0};Assert.IsFalse(old.CultureEarlySignal);Assert.AreEqual(0,old.ProposalRankBonus);Assert.IsTrue(old.Valid());LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NextScreens3_部屋から関連改善を導入し段階と社員の支援先が一致する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(2);
            foreach(string room in new[]{"server","office","meeting"})
            {
                var button=Find<UnityEngine.UI.Button>("Room_"+room);var rect=button.GetComponent<RectTransform>();Canvas.ForceUpdateCanvases();
                // 動く泡の操作範囲を避け、固定の部屋名の位置で部屋ボタンを確認する。
                var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+100,rect.rect.yMax-16)))};
                var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>());
                Click("Room_"+room);yield return new WaitForSecondsRealtime(.5f);
                var cards=UnityEngine.Object.FindObjectsByType<RectTransform>().Where(t=>t.name.StartsWith("Project_")).ToArray();Assert.IsNotEmpty(cards);foreach(var c in cards)Assert.AreEqual(room,OpsGame.ProjectRoom(c.name.Substring(8)));
                Click("ClosePlanner");yield return null;
            }
            game.Buy(OpsCatalog.Index("mfa"));yield return new WaitForSecondsRealtime(1.6f);Assert.IsNotNull(Find<UnityEngine.UI.Image>("RoomDevice_mfa"));Assert.IsFalse(game.Surface.GetComponentsInChildren<OpsIncidentGraphic>().Any(g=>g.name=="UninstalledFrame"));Capture("124-next-office-rooms");
            Click("Room_office");yield return new WaitForSecondsRealtime(.5f);Assert.IsNotNull(Find<RectTransform>("RoomDevice_inventory").Find("UninstalledFrame"));Click("Details_inventory");yield return new WaitForSecondsRealtime(.5f);CheckPointer("Buy_inventory");Click("Buy_inventory");yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(1,game.State.Level("inventory"));
            game.OpenTab(0);Click("Room_reception");yield return new WaitForSecondsRealtime(.5f);CheckPointer("ReceptionBrief");Click("CloseDialog");
            game.OpenTab(0);SetEvent(game.State,"ransom-backup");game.State.staffExperience[1]=3;game.State.supportOrder="investigate";game.BeginIncident();game.Resolve("scope");
            float limit=Time.realtimeSinceStartup+5;while(!UnityEngine.Object.FindObjectsByType<OpsRoomHelperMotion>().Any()&&Time.realtimeSinceStartup<limit)yield return null;
            Assert.AreEqual("server",UnityEngine.Object.FindAnyObjectByType<OpsRoomHelperMotion>().TargetRoom);Capture("124-next-staff-room");yield return WaitForResolution(game);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NextScreens2_依頼書の二経路と臨時予算は実数で一度だけ働く()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);ChooseDelegatedWork(game,game.State.CurrentMission.actionA);Click("ConsultationDetails");yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(new Vector2(330,-60),Find<RectTransform>("MissionBrief").anchoredPosition);Assert.AreEqual(new Vector2(940,780),Find<RectTransform>("MissionBrief").sizeDelta);
            game.State.MissionProgress(false,out int b,out int bt);Assert.AreEqual(b+" / "+bt,Find<TextMeshProUGUI>("MissionProgressB").text);CheckPointer("AcceptMission");Capture("123-next-mission-brief");Click("AcceptMission");Assert.AreEqual(game.State.month,game.State.acceptedMissionMonth);
            ChooseDelegatedWork(game,game.State.CurrentMission.actionB);int money=game.State.budget;game.BeginIncident();Assert.IsTrue(game.State.CurrentMissionCompleted);Assert.AreEqual(money+OpsState.MissionBudgetReward,game.State.budget);game.BeginIncident();Assert.AreEqual(money+OpsState.MissionBudgetReward,game.State.budget);
            game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(.8f);Assert.AreEqual(1,game.State.Latest.missionBonus);Capture("123-next-mission-reward");Click("MissionConversation");yield return new WaitForSecondsRealtime(.5f);CheckPointer("CloseDialog");
            var old=new OpsState(14);old.missionBudgetRules=0;old.Act(old.CurrentMission.actionA);old.Act(old.CurrentMission.actionB);money=old.budget;old.BeginIncident();Assert.AreEqual(money,old.budget);Assert.IsTrue(old.Valid());LogAssert.NoUnexpectedReceived();
        }
        private static void PressPresentationEnter(System.Action assertion)
        {
            var settings=InputSystem.settings;var focus=settings.editorInputBehaviorInPlayMode;var background=settings.backgroundBehavior;
            Keyboard keyboard=null;
            try
            {
                // Gameビューのフォーカスに検証を依存させない。実際の入力イベントを処理し、押下を確認してから操作する。
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.EnableDevice(keyboard);
                Assert.IsTrue(keyboard.enabled);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
                Assert.IsTrue(keyboard.enterKey.wasPressedThisFrame);assertion();
            }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
                settings.editorInputBehaviorInPlayMode=focus;settings.backgroundBehavior=background;
            }
        }
        [UnityTest] public IEnumerator NextScreens1_タイトルと月替わり事件入口はルールを変えない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1.2f);var game=Object.FindAnyObjectByType<OpsGame>();
            Assert.AreEqual("story-title-veil",Find<OpsIncidentGraphic>("TitleVeil").Kind);Assert.AreEqual(.9f,Find<OpsUIReveal>("TitleLogoWordmark").Duration);Capture("122-next-title");
            game.StartYear(14);string state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(game.State.Current.name,Find<TextMeshProUGUI>("CalendarMonth").text);Assert.IsFalse(game.PhasePresentationCanSkip);Capture("122-next-calendar");
            // スキップに使った入力を、背後の行動ボタンへ通さない。初回は短縮しない。
            PressPresentationEnter(()=>{Click("Action_listen");Assert.AreEqual(state,JsonUtility.ToJson(game.State));});
            yield return new WaitForSecondsRealtime(1.6f);Assert.AreEqual(state,JsonUtility.ToJson(game.State));game.BeginIncident();state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual("緊急",Find<TextMeshProUGUI>("IncidentEntryTitle").text);Capture("122-next-incident-entry");yield return new WaitForSecondsRealtime(.9f);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
            game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(.8f);
            StringAssert.Contains(game.State.Latest.loss==0?"金銭被害なし":game.State.Latest.loss.ToString(),Find<TextMeshProUGUI>("MonthlyDamageStampText").text);
            game.Next();yield return new WaitForSecondsRealtime(.6f);Assert.IsTrue(game.PhasePresentationCanSkip);state=JsonUtility.ToJson(game.State);
            PressPresentationEnter(()=>{Click("Action_listen");Assert.AreEqual(state,JsonUtility.ToJson(game.State));});
            yield return null;yield return null;
            Assert.IsFalse(Object.FindObjectsByType<RectTransform>().Any(t=>t.name=="PhasePresentation"));CheckPointer("Action_listen");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator チュートリアルは実操作だけで六段階を完了する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);Assert.IsTrue(game.StartTutorial());yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(0,game.TutorialStep);CheckPointer("Stat_0");Click("Stat_0");yield return new WaitForSecondsRealtime(.5f);Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1,game.TutorialStep);Assert.IsFalse(game.Surface.GetComponentsInChildren<Transform>(true).First(t=>t.name=="PlanningCharacter").gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(3);CheckPointer("Action_audit");Capture("73-tutorial-audit");Click("Action_audit");DelegateWork(game);yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(2,game.TutorialStep);CheckPointer("ConsultationDetails");Click("ConsultationDetails");yield return new WaitForSecondsRealtime(.5f);CheckPointer("CloseDialog");Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(3,game.TutorialStep);CheckPointer("OpenProjects");Click("OpenProjects");yield return new WaitForSecondsRealtime(.5f);CheckPointer("Details_inventory");Click("Details_inventory");yield return new WaitForSecondsRealtime(.5f);CheckPointer("Buy_inventory");Click("Buy_inventory");yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(4,game.TutorialStep);CheckPointer("AdvanceMonth");Click("AdvanceMonth");yield return new WaitForSecondsRealtime(.5f);CheckPointer("ConfirmAdvance");Click("ConfirmAdvance");yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(5,game.TutorialStep);yield return new WaitForSecondsRealtime(3);CheckPointer("Respond_scope");Capture("74-tutorial-incident");Click("Respond_scope");yield return WaitForResolution(game);
            yield return null;Assert.IsFalse(game.TutorialActive);Assert.AreEqual(1,game.State.history.Count);Assert.AreEqual(OpsPhase.Review,game.State.phase);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator チュートリアルは飛ばしてもゲーム数値を変えない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(18);
            string before=JsonUtility.ToJson(game.State);Assert.IsTrue(game.StartTutorial());yield return null;Click("SkipTutorial");yield return null;
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.IsFalse(game.TutorialActive);Assert.IsTrue(game.Surface.GetComponentsInChildren<Transform>(true).First(t=>t.name=="PlanningCharacter").gameObject.activeSelf);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 月報は三つの数値と実際の支援をモック配置で表示する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            game.Buy(OpsCatalog.Index("backup"));ChooseDelegatedWork(game,"audit");ChooseDelegatedWork(game,"map");game.BeginIncident();game.Resolve("recover");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(1);
            var r=game.State.Latest;Assert.AreEqual(r.loss+"<size=17>万円</size>",Find<TextMeshProUGUI>("MonthlyLossValue").text);
            Assert.AreEqual(r.downtime+"<size=17>時間</size>",Find<TextMeshProUGUI>("MonthlyStopValue").text);
            Assert.AreEqual(new Vector2(40,-140),Find<RectTransform>("MonthlyIncident").anchoredPosition);
            Assert.IsNotNull(r.metricsBefore);Assert.IsNotNull(r.forecast);Assert.AreEqual("次の月は支援を頼める",Find<TextMeshProUGUI>("SupportNone").text);
            CheckPointer("EffectDetails");CheckPointer("ReviewDetails");CheckPointer("NextMonth");Capture("75-monthly-report");CheckText();
            Click("ReviewDetails");yield return new WaitForSecondsRealtime(.5f);CheckPointer("MonthlyLesson");CheckPointer("ViewHistory");CheckText();Click("CloseDialog");yield return new WaitForSecondsRealtime(.25f);
            game.Next();game.State.staffExperience[1]=OpsGrowthCatalog.StaffThresholds[1];game.State.supportOrder="investigate";game.OpenTab(0);game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(1);
            if(game.State.Latest.power.staff>0)Assert.IsNotNull(Find<TextMeshProUGUI>("SupportName"));Capture("76-monthly-team");LogAssert.NoUnexpectedReceived();
        }
        [Test] public void 年間集計は連携効果を二重計上せず実際の支援と遭遇を数える()
        {
            var state=new OpsState(2);
            for(int i=0;i<2;i++)state.history.Add(new OpsOutcome{month=i,eventId=OpsEventCatalog.Events[0].id,investmentEffects=new System.Collections.Generic.List<OpsInvestmentEffect>{new OpsInvestmentEffect{projectId="backup",avoidedLoss=5},new OpsInvestmentEffect{projectId="drill",avoidedLoss=5}},power=new OpsResponsePower{staff=4,support="佐伯：記録を整理"}});
            state.history.Add(new OpsOutcome{month=2,benign=true,power=new OpsResponsePower{staff=4,support="森：通常業務"}});
            var summary=OpsAnnualSummary.From(state);Assert.AreEqual(2,summary.projects.Count);Assert.AreEqual(10,summary.Mvp.loss);Assert.AreEqual(2,summary.Mvp.activations);Assert.AreEqual(1,summary.encountered.Count);Assert.AreEqual(2,summary.staffSupport[1]);Assert.AreEqual(0,summary.staffSupport[2]);
            var legacy=new OpsState(1);legacy.history.Add(new OpsOutcome{month=0});Assert.IsNull(OpsAnnualSummary.From(legacy).Mvp);Assert.AreEqual(-1,OpsAnnualSummary.From(legacy).StaffMvp);
        }
        [UnityTest] public IEnumerator 年間評価は十二か月とMVPを表示し再挑戦できる()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            game.Buy(OpsCatalog.Index("backup"));
            for(int i=0;i<12;i++)
            {
                if(game.State.phase==OpsPhase.Ended)break;
                Plan(game.State);game.OpenTab(0);game.BeginIncident();game.Resolve(PublicTestResponse(game.State));game.Next();yield return null;
            }
            yield return new WaitForSecondsRealtime(1.6f);Assert.AreEqual(OpsPhase.Ended,game.State.phase);Assert.AreEqual(12,game.State.history.Count);Assert.IsTrue(game.State.IsClear);
            Assert.AreEqual(12,game.Surface.GetComponentsInChildren<RectTransform>().Count(t=>t.name.StartsWith("AnnualMonth")&&!t.name.StartsWith("AnnualMonthName")&&!t.name.StartsWith("AnnualMonthResult")));
            var summary=OpsAnnualSummary.From(game.State);Assert.AreEqual(summary.encountered.Count+" / 40",Find<TextMeshProUGUI>("CollectionValue").text);
            var portraitPosition=Find<RectTransform>("NavigatorPortrait").GetComponent<OpsPortraitMotion>().LayoutPosition;
            Assert.AreEqual(1275,portraitPosition.x);Assert.AreEqual(-630,portraitPosition.y,"成長目標の下に置き、足元を画面内に収める");
            CheckPointer("AnnualDetails");CheckPointer("EndingHistory");CheckPointer("BackHome");CheckPointer("ReplayYear");CheckText();Capture("77-annual-report");
            Click("ReplayYear");yield return null;Assert.AreEqual(0,game.State.month);Assert.AreEqual(OpsPhase.Planning,game.State.phase);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 設定はスライダーと表示変更を即時反映し削除前に確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);Click("Menu");yield return new WaitForSecondsRealtime(.6f);
            var s=Find<Slider>("MusicVolume");s.value=.73f;yield return null;Assert.AreEqual("73",Find<TextMeshProUGUI>("MusicVolumePercent").text);Assert.Less(Find<RectTransform>("MusicVolumeFill").rect.width,612);
            Find<Slider>("SoundVolume").value=0;Find<Slider>("VoiceVolume").value=.9f;
            Click("TextSize2");yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(2,game.TextSize);Assert.Greater(Find<TextMeshProUGUI>("TextSizePreview").fontSize,16);
            Click("CaptionToggle");yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(game.CaptionsEnabled);Click("ShortenInterruptions");yield return new WaitForSecondsRealtime(.5f);Assert.IsTrue(game.ShortenInterruptions);
            CheckPointer("ReduceMotion");CheckPointer("DeleteRecords");CheckPointer("CloseDialog");Capture("78-settings");CheckText();
            string before=JsonUtility.ToJson(game.State);Click("DeleteRecords");yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(before,JsonUtility.ToJson(game.State));CheckPointer("ConfirmDeleteRecords");CheckPointer("CloseDialog");Click("CloseDialog");yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ひなたは差分未設定なら一枚絵を保ちゲーム乱数に触れない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return null;
            var portrait=Find<Image>("NavigatorPortrait");var animator=portrait.GetComponent<OpsPortraitAnimator>();Assert.IsNotNull(animator);animator.ChangePose("pose_laptop");yield return new WaitForSecondsRealtime(.4f);
            Assert.IsFalse(animator.HasFrames);string before=JsonUtility.ToJson(game.State);var original=portrait.sprite;
            yield return new WaitForSecondsRealtime(5.2f);Assert.AreSame(original,portrait.sprite);Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.IsFalse(portrait.GetComponentsInChildren<Image>().First(i=>i.name=="HinataEyes").enabled);Assert.IsFalse(portrait.GetComponentsInChildren<Image>().First(i=>i.name=="HinataMouth").enabled);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void 月報の見積もりと月初値は保存でき旧記録には捏造しない()
        {
            var state=new OpsState(14,true);state.Act("listen");state.BeginIncident();var forecast=state.Estimate("scope");state.Resolve("scope");
            var copy=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));Assert.IsTrue(copy.Valid());CollectionAssert.AreEqual(state.Latest.metricsBefore,copy.Latest.metricsBefore);Assert.AreEqual(forecast.stopMax,copy.Latest.forecast.stopMax);Assert.IsTrue(copy.Latest.hasClosingState);Assert.AreEqual(copy.budget,copy.Latest.closingBudget);
            var legacy=new OpsState(14){monthStartMetrics=null};legacy.BeginIncident();legacy.Resolve("scope");Assert.IsNull(legacy.Latest.metricsBefore);Assert.AreEqual(0,OpsAnnualSummary.From(legacy).encountered.Count);
            var failed=new OpsOutcome{month=2,hasClosingState=true,closingBudget=-1,closingStability=50};Assert.AreEqual("運営終了",OpsAnnualSummary.MonthLabel(state,failed));
        }
    }
}
