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
        [Test] public void Next13Colleague_同じ会社の同僚という文に統一し旧日記音声を外す()
        {
            Assert.AreEqual("開発部から、情シスに異動してきた。\n月に1回、調査を頼める",OpsCatalog.CompanyYear(2).allyDescriptions[0]);
            Assert.AreEqual("情シスのチームに、すっかりなじんだ。\n強敵の正体を、真っ先に見抜く",OpsCatalog.CompanyYear(3).allyDescriptions[0]);
            Assert.AreEqual("エンジニアさんが情シスに来るのは2年目から",new OpsState(14,true).EngineerResearchBlock);
            var entry=OpsDiaryCatalog.Entries[6];Assert.AreEqual("diary_y1_10_v2",entry.voiceId);StringAssert.StartsWith("開発部のエンジニアさんが情シスを手伝いに来て、初めて一緒に仕事をした。",entry.body);StringAssert.Contains("弟子はいらない。一緒にやればいい",entry.body);
            var bank=OpsReactionBank.ScriptV2();Assert.IsFalse(bank.Any(l=>l.id=="diary_y1_10"));Assert.AreEqual(entry.intro,bank.Single(l=>l.id==entry.voiceId).caption);
        }
        [UnityTest] public IEnumerator Next13ColleagueUI_開幕と日記の新しい文を表示する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            foreach(int year in new[]{2,3})
            {
                var story=AlliesStory(year);Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));string snapshot=JsonUtility.ToJson(story.state);
                game.PreviewYearOpening(3);yield return new WaitForSecondsRealtime(2);
                Assert.IsTrue(Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.text==OpsCatalog.CompanyYear(year).allyDescriptions[0]));
                Assert.AreEqual(snapshot,JsonUtility.ToJson(story.state));Capture("next13-colleague-opening-y"+year);
            }
            game.Career.diary.Add(new OpsDiaryRecord{key=6,content=6,mood=0,recap="10月の記録",thought="戻せる備えを確かめた。",rank="B",minigame="",season="秋"});
            game.OpenDiaryBook();Click("DiaryPage_6");yield return new WaitForSecondsRealtime(3);
            Assert.AreEqual("diary_y1_10_v2",game.LastReactionId);Assert.IsFalse(game.VoicePlaying);Assert.IsNull(game.ActiveVoiceBank.Find("diary_y1_10"));
            var body=Find<TextMeshProUGUI>("DiaryBody");StringAssert.Contains("弟子はいらない。一緒にやればいい",body.text);body.ForceMeshUpdate();Assert.IsFalse(body.isTextOverflowing);Capture("next13-colleague-diary-october");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
