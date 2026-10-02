#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using System.Reflection;
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
        [Test] public void Next13Maxim_年度と挑戦を分けて保存し声の版は同一扱い()
        {
            var c=new OpsCareer();c.PrepareMaximYear(1,1);Assert.IsTrue(c.HeardMaxim("maxim_hurry",true));Assert.IsFalse(c.HeardMaxim("maxim_hurry_v2",true));
            var copy=JsonUtility.FromJson<OpsCareer>(JsonUtility.ToJson(c));copy.PrepareMaximYear(1,1);Assert.AreEqual(1,copy.yearMaxims.Count);Assert.IsTrue(copy.Valid());
            copy.PrepareMaximYear(1,2);Assert.IsEmpty(copy.yearMaxims);Assert.AreEqual(1,copy.heardMaxims.Count);
            copy.HeardMaxim("maxim_logs",true);copy.PrepareMaximYear(1,2,true);Assert.IsEmpty(copy.yearMaxims);Assert.AreEqual(2,copy.heardMaxims.Count);
            foreach(string id in OpsMaxims.Ids)copy.HeardMaxim(id,false);Assert.AreEqual(20,copy.heardMaxims.Count);Assert.IsTrue(copy.Valid());
            copy.yearMaxims.Add("unknown");Assert.IsFalse(copy.Valid());
        }
        [UnityTest] public IEnumerator Next13MaximUI_同年度の字幕と声を共有し再開でも重複させない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);
            yield return new WaitForSecondsRealtime(.5f);string state=JsonUtility.ToJson(game.State);
            foreach(string id in OpsMaxims.Ids){Assert.IsTrue(game.SpeakSceneLine(id));Assert.AreEqual(id,game.LastReactionId);}
            Assert.AreEqual(20,game.Career.yearMaxims.Count);Assert.AreEqual(20,game.Career.heardMaxims.Count);
            game.SpeakSceneLine("maxim_logs");Assert.AreEqual("think_01",game.LastReactionId);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
            var restored=new OpsProgress{single=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(game.State)),career=JsonUtility.FromJson<OpsCareer>(JsonUtility.ToJson(game.Career))};Assert.IsTrue(game.RestoreProgress(restored));game.SpeakSceneLine("maxim_logs");Assert.AreEqual("think_01",game.LastReactionId);
            game.OpenRecords();yield return new WaitForSecondsRealtime(.6f);Assert.AreEqual("聞いた格言　20 / 20",Find<TextMeshProUGUI>("HeardMaxims").text);Capture("next13-maxims-records");
            game.StartYear(14);game.SpeakSceneLine("maxim_logs");Assert.AreEqual("maxim_logs",game.LastReactionId);Assert.AreEqual(1,game.Career.yearMaxims.Count);Assert.AreEqual(20,game.Career.heardMaxims.Count);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next13MaximUI_月報と年間評価でも同じ年度の記録を使う()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);
            DiaryTestMonth(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(4);
            Assert.Contains("maxim_report",game.Career.yearMaxims);game.SpeakSceneLine("maxim_report");Assert.AreEqual("think_01",game.LastReactionId);
            while(game.State.phase!=OpsPhase.Ended){if(game.State.phase==OpsPhase.Review)game.State.NextMonth();else if(game.State.phase==OpsPhase.Planning)game.State.BeginIncident();else game.State.Resolve("scope");}
            game.OpenTab(0);yield return new WaitForSecondsRealtime(6);Assert.Contains("maxim_human",game.Career.yearMaxims);game.SpeakSceneLine("maxim_human");Assert.AreEqual("think_01",game.LastReactionId);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next13MaximUI_聞こえず見えない声と未再生予約は未読で練習は本編を消費しない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);
            typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,false);typeof(OpsGame).GetProperty("VoiceEnabled").SetValue(game,false);
            game.SpeakSceneLine("maxim_logs",0);yield return new WaitForSecondsRealtime(.15f);Assert.IsEmpty(game.Career.heardMaxims);
            var line=game.ActiveVoiceBank.Find("maxim_logs");var clip=AudioClip.Create("格言試験",4410,1,44100,false);var old=line.clip;line.clip=clip;
            try
            {
                typeof(OpsGame).GetProperty("VoiceEnabled").SetValue(game,true);game.SpeakSceneLine("maxim_logs",5);game.StopVoice();yield return null;Assert.IsEmpty(game.Career.heardMaxims);
                game.SpeakSceneLine("maxim_logs",0);yield return new WaitForSecondsRealtime(.05f);Assert.Contains("maxim_logs",game.Career.heardMaxims);
            }
            finally{line.clip=old;Object.Destroy(clip);}
            typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,true);game.StopVoice();game.OpenRecords();Assert.IsTrue(game.BeginDailyPractice("D",20261002));game.StartMinigame();FinishDailyForTest(game.Minigame);game.TickMinigame(0);
            yield return new WaitForSecondsRealtime(3.8f);Assert.Contains("maxim_mfa",game.Career.heardMaxims);Assert.IsFalse(game.Career.yearMaxims.Contains("maxim_mfa"));Capture("next13-maxims-practice");game.ConfirmMinigame();
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
