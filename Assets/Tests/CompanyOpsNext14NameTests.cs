#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
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
        [Test] public void Next14Names_改名は表示だけで旧保存とIDを保つ()
        {
            foreach(int year in new[]{2,3})Assert.AreEqual("りりぃ",OpsCatalog.CompanyYear(year).allyNames[0]);
            foreach(var e in OpsDiaryCatalog.Entries){StringAssert.DoesNotContain("エンジニアさん",e.body+e.intro);}
            var bank=OpsReactionBank.ScriptV2();
            foreach(string id in new[]{"diary_y1_10","diary_y1_10_v2","diary_y3_06"})Assert.IsFalse(bank.Any(l=>l.id==id));
            foreach(int page in new[]{6,26})StringAssert.Contains("りりぃさん",bank.Single(l=>l.id==OpsDiaryCatalog.Entries[page].voiceId).caption);
            // 旧版と同じ形式・11設備・日記のキー。保存ファイル自体を改名しない。
            var progress=new OpsProgress{single=new OpsState(14,true)};progress.single.levels=new int[11];
            progress.career.diary.Add(new OpsDiaryRecord{key=6,content=6,mood=0,recap="10月の記録",thought="備えを確かめた",rank="B",minigame="",season="秋"});
            progress.career.heardMaxims.Add("maxim_link");
            string path=Path.Combine(Application.dataPath,"../Artifacts/Next14/old-progress.json");
            Assert.IsTrue(OpsSaveStore.WriteProgress(path,progress,out var warning),warning);var original=File.ReadAllBytes(path);
            var restored=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(restored,warning);
            CollectionAssert.AreEqual(original,File.ReadAllBytes(path));Assert.AreEqual(11,restored.single.levels.Length);
            Assert.AreEqual(6,restored.career.diary.Single().key);Assert.Contains("maxim_link",restored.career.heardMaxims);
            Assert.IsTrue(OpsSaveStore.WriteProgress(path,restored,out warning),warning);CollectionAssert.AreEqual(original,File.ReadAllBytes(path+".bak"));
            CollectionAssert.AreEqual(new[]{"eng_investigate","eng_react_hmm","eng_found","eng_react_ok"},OpsGame.CompanionSceneLines("investigate"));
        }
        [UnityTest] public IEnumerator Next14NamesUI_顔札と日記字幕と旧保存の画面を確かめる()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();
            game.UseLocalTestVoices=true;Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=AlliesStory(2)}));
            game.PreviewYearOpening(3);yield return new WaitForSecondsRealtime(2);
            Assert.Contains("りりぃ",Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name=="AllyName").Select(t=>t.text).ToArray());Capture("next14-names-allies");
            game.SkipYearOpening();game.AdvanceYearOpening();game.SpeakSceneLine("eng_investigate",0);yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual("りりぃ",game.CurrentSpeaker);Assert.IsNotNull(Find<UnityEngine.UI.RawImage>("CompanionFace").texture);Capture("next14-names-face");
            game.StopVoice();game.Career.diary.Clear();
            foreach(int page in new[]{2,6,26})game.Career.diary.Add(new OpsDiaryRecord{key=page,content=page,mood=0,recap="今月の備えを確認した",thought="みんなで守れた",rank="A",minigame="",season="秋"});
            foreach(int page in new[]{2,6,26})
            {
                game.OpenDiaryBook();Click("DiaryPage_"+page);yield return new WaitForSecondsRealtime(3);
                if(page!=2){Assert.AreEqual(OpsDiaryCatalog.Entries[page].voiceId,game.LastReactionId);Assert.IsFalse(game.VoicePlaying);StringAssert.Contains("りりぃさん",game.LastReactionCaption);}
                var body=Find<TextMeshProUGUI>("DiaryBody");body.ForceMeshUpdate();Assert.IsFalse(body.isTextOverflowing);
                Assert.IsFalse(Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.text.Contains("エンジニアさん")));Capture("next14-names-diary-"+page);
            }
            Next14Names_改名は表示だけで旧保存とIDを保つ();
            var old=OpsSaveStore.ReadProgress(Path.Combine(Application.dataPath,"../Artifacts/Next14/old-progress.json"),out var warning);
            Assert.IsNotNull(old,warning);Assert.IsTrue(game.RestoreProgress(old));game.OpenDiaryBook();Click("DiaryPage_6");yield return new WaitForSecondsRealtime(3);
            StringAssert.Contains("りりぃさん",Find<TextMeshProUGUI>("DiaryBody").text);
            Assert.IsTrue(game.SpeakSceneLine("diary_y1_10_v2",0,"DiaryVoiceCaption"));yield return null;Assert.AreEqual("diary_y1_10_v3",game.LastReactionId);Assert.IsFalse(game.VoicePlaying);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
