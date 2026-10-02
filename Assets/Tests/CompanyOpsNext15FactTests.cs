#if UNITY_INCLUDE_TESTS
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
        [Test] public void Next15Fact_分離は保証せず旧格言の既読と数値を保つ()
        {
            var lines=OpsReactionBank.ScriptV2();Assert.AreEqual(191,lines.Length);Assert.AreEqual(191,lines.Select(l=>l.id).Distinct().Count());
            Assert.IsFalse(lines.Any(l=>l.id=="maxim_segment"));var updated=lines.Single(l=>l.id=="maxim_segment_v2");
            Assert.AreEqual("区切っておけば、広がりを抑えられる！",updated.caption);Assert.IsNull(updated.clip);
            var career=new OpsCareer();career.PrepareMaximYear(14,1);Assert.IsTrue(career.HeardMaxim("maxim_segment",true));
            Assert.IsFalse(career.HeardMaxim("maxim_segment_v2",true));Assert.IsTrue(career.Valid());
            CollectionAssert.AreEqual(new[]{"maxim_segment"},career.heardMaxims);CollectionAssert.AreEqual(new[]{"maxim_segment"},OpsMaxims.Candidates(updated.id));
        }
        [UnityTest] public IEnumerator Next15FactUI_旧IDの呼び出しでも新しい字幕だけを使う()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=true;
            game.StartYear(14);yield return new WaitForSecondsRealtime(2);game.StopVoice();string before=JsonUtility.ToJson(game.State);
            // 旧台本文を同じ画面で再現する。旧バイナリの撮影と偽らず、音源も差し戻さない。
            var line=game.ActiveVoiceBank.Find("maxim_segment_v2");Assert.IsNotNull(line);string revised=line.caption;
            try{line.caption="区切っておけば、広がらない！";game.SpeakSceneLine("maxim_segment",0);yield return new WaitForSecondsRealtime(2);Capture("next15-fact-segment-before");}
            finally{line.caption=revised;game.StopVoice();}
            game.Career.PrepareMaximYear(game.State.seed,game.RunYear,true);
            Assert.IsTrue(game.SpeakSceneLine("maxim_segment",0));yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual("maxim_segment_v2",game.LastReactionId);Assert.IsFalse(game.VoicePlaying);Assert.IsNull(game.ActiveVoiceBank.Find("maxim_segment"));
            Assert.AreEqual("区切っておけば、広がりを抑えられる！",game.LastReactionCaption);Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            var label=Find<TextMeshProUGUI>("NavigatorSpeech");label.ForceMeshUpdate();Assert.IsFalse(label.isTextOverflowing);Capture("next15-fact-segment");
            Assert.Contains("maxim_segment",game.Career.yearMaxims);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
