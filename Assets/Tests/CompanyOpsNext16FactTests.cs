#if UNITY_INCLUDE_TESTS
using System.Collections;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void Next16Fact_二つの新IDと二十格言と旧既読保存を維持する()
        {
            var lines=OpsReactionBank.ScriptV2();Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,lines.Length);Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,lines.Select(l=>l.id).Distinct().Count());
            Assert.IsFalse(lines.Any(l=>l.id=="maxim_report"||l.id=="diary_y3_07"));
            var report=lines.Single(l=>l.id=="maxim_report_v2");Assert.AreEqual("迷ったら相談！　早い報告ほど、打てる手が増えるよ！",report.caption);Assert.IsNull(report.clip);
            var july=OpsDiaryCatalog.Entries.Single(e=>e.year==3&&e.month==3);Assert.AreEqual("diary_y3_07_v2",july.voiceId);Assert.AreEqual("支援士の科目B、長い……夜が足りない",july.intro);StringAssert.Contains("科目Bの問題は",july.body);
            StringAssert.Contains("科目Bの問題の図",OpsDiaryCatalog.Entries.Single(e=>e.year==2&&e.month==5).body);
            Assert.IsFalse(OpsDiaryCatalog.Entries.Any(e=>e.body.Contains("午後の問題")||e.intro.Contains("午後問題")));
            var career=new OpsCareer();career.PrepareMaximYear(14,1);Assert.IsTrue(career.HeardMaxim("maxim_report",true));
            var saved=JsonUtility.FromJson<OpsCareer>(JsonUtility.ToJson(career));Assert.IsTrue(saved.Valid());Assert.IsFalse(saved.HeardMaxim("maxim_report_v2",true));
            CollectionAssert.AreEqual(new[]{"maxim_report"},saved.heardMaxims);CollectionAssert.AreEqual(new[]{"maxim_report"},saved.yearMaxims);Assert.AreEqual(20,OpsMaxims.Ids.Length);
            CollectionAssert.AreEqual(new[]{"maxim_report"},OpsMaxims.Candidates(report.id));
            CollectionAssert.AreEquivalent(new[]{"onboard","offboard","transfer","contractor"},OpsEventCatalog.Tickets.Where(t=>t.source=="identity").Select(t=>t.id));
            Assert.IsFalse(OpsEventCatalog.Events.Concat(OpsEventCatalog.StoryEvents).Any(e=>e.news.Contains("事例。")));
            var bank=Resources.Load<OpsReactionBank>("CompanionVoices");Assert.AreEqual(24,bank.lines.Length);
            Assert.AreEqual("数字のことは、わたしに任せて。あなたたちは、守ることに集中して。",bank.Find("kanon_opening").caption);
            if(bank.Find("kanon_opening").clip!=null)Assert.AreEqual("kanon_opening",bank.Find("kanon_opening").clip.name);
            StringAssert.Contains("……たぶん。いや、止められる",bank.Find("eng_factor").caption);
        }
        [UnityTest] public IEnumerator Next16FactUI_旧IDでも新版の字幕だけを使い年度の数値を変えない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(2);game.StopVoice();string state=JsonUtility.ToJson(game.State);
            foreach(bool local in new[]{false,true})
            {
                game.UseLocalTestVoices=local;
                foreach(string old in new[]{"maxim_report","diary_y3_07"})
                {
                    game.Career.PrepareMaximYear(game.State.seed,game.RunYear,true);
                    Assert.IsNull(game.ActiveVoiceBank.Find(old));Assert.IsNull(game.ActiveVoiceBank.Find(old+"_v2").clip);
                    Assert.IsTrue(game.SpeakSceneLine(old,0));yield return new WaitForSecondsRealtime(.3f);
                    Assert.AreEqual(old+"_v2",game.LastReactionId);Assert.IsFalse(game.VoicePlaying);Assert.AreEqual(state,JsonUtility.ToJson(game.State));game.StopVoice();
                }
            }
            Assert.IsTrue(game.SpeakSceneLine("kanon_opening",0));yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual("数字のことは、わたしに任せて。あなたたちは、守ることに集中して。",game.LastReactionCaption);
            Assert.AreEqual("かのん",game.CurrentSpeaker);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next16ReferencesUI_注意書きと四チケットの参考を五解像度で確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);game.BeginIncident();
            while(game.PhasePresentationRunning)yield return null;game.StopVoice();
            typeof(OpsGame).GetMethod("EventBriefDialog",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            yield return new WaitForSecondsRealtime(.8f);game.enabled=false;
            string before=JsonUtility.ToJson(game.State);
            try
            {
                var note=Find<TextMeshProUGUI>("EventFictionNote");Assert.AreEqual("この出来事は架空の想定です。資料は考え方の参考です。",note.text);
                foreach(var size in Next15Sizes)
                {
                    Next15Shot("event-reference",size.x,size.y,"Next16/After");Assert.IsFalse(note.isTextOverflowing);
                    foreach(string id in new[]{"EventKnowledge","EventSource","CloseDialog"})Next16Separated(note,Find<Button>(id));
                }
                Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            }
            finally{game.enabled=true;}
            Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);game.StartYear(14);while(game.PhasePresentationRunning)yield return null;yield return new WaitForSecondsRealtime(.5f);game.StopVoice();
            foreach(string id in new[]{"onboard","offboard","transfer","contractor"})
            {
                var schedule=game.State.ticketSchedule;int existing=Array.IndexOf(schedule,id);if(existing>=0){string first=schedule[0];schedule[0]=id;schedule[existing]=first;}else schedule[0]=id;
                string state=JsonUtility.ToJson(game.State);typeof(OpsGame).GetMethod("TicketDialog",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);yield return new WaitForSecondsRealtime(.5f);
                var reference=Find<TextMeshProUGUI>("TicketReference");Assert.AreEqual("参考：IdP（社員の認証をまとめる仕組み）の説明",reference.text);
                game.enabled=false;try{foreach(var size in Next15Sizes){Next15Shot("ticket-"+id,size.x,size.y,"Next16/After");Assert.IsFalse(reference.isTextOverflowing);Next16Separated(reference,Find<Button>("CloseDialog"));Next16Separated(reference,Find<Button>("TicketKnowledge"));}}
                finally{game.enabled=true;}
                Assert.AreEqual(state,JsonUtility.ToJson(game.State));Assert.AreEqual("identity",game.State.Ticket.source);Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        private static void Next16Separated(TextMeshProUGUI note,Button button)
        {
            Assert.AreSame(note.transform.parent,button.transform.parent);
            var noteBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(note.transform.parent,note.rectTransform);
            var buttonBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(button.transform.parent,button.transform);
            var a=new Rect(noteBounds.min.x,noteBounds.min.y,noteBounds.size.x,noteBounds.size.y);var b=new Rect(buttonBounds.min.x,buttonBounds.min.y,buttonBounds.size.x,buttonBounds.size.y);
            Assert.IsFalse(a.Overlaps(b),note.name+" / "+button.name);
        }
    }
}
#endif
