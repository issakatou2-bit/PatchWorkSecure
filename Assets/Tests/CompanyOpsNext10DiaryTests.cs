#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void DiaryTestMonth(OpsState s)
        {
            Assert.IsTrue(s.BeginIncident());
            string response=CompanyOpsPersonaPolicy.Responses.OrderBy(r=>s.Estimate(r).lossMax*7+s.Estimate(r).stopMax*4+s.Estimate(r).cost).First();Assert.IsTrue(s.Resolve(response));
        }
        [UnityTest] public IEnumerator Next10DiaryUI_四つの見開きと字幕なし音声なしと再読を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartStory(14);FinishStoryTestYear(game.State);Assert.IsTrue(game.NextStoryYear());
            for(int i=0;i<game.State.levels.Length;i++)game.State.levels[i]=2;game.State.budget=400;
            for(int m=0;m<3;m++){DiaryTestMonth(game.State);if(game.State.QuarterRewardPending)game.State.ClaimQuarterReward("budget");game.State.NextMonth();}
            DiaryTestMonth(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next10-diary-month-good-before");
            string before=JsonUtility.ToJson(game.State);game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(3);Capture("next10-diary-month-good");
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.AreEqual(OpsDiaryCatalog.Entries[15].body,Find<TextMeshProUGUI>("DiaryBody").text);Assert.IsNotNull(Find<Button>("DiaryReplay"));
            Assert.AreEqual("KleeOneDiary",Find<TextMeshProUGUI>("DiaryBody").font.name.Split(new[]{"_Test_"},System.StringSplitOptions.None)[0]);
            Assert.IsTrue(game.Career.Valid(),"日記のキャリア");Assert.IsTrue(game.State.Valid(),"年度の状態");Assert.IsTrue(game.Story.Valid(),"本編の状態");
            string path=System.IO.Path.Combine(Application.dataPath,"../Artifacts/Next10/diary-save-test.json");Assert.IsTrue(OpsSaveStore.WriteProgress(path,game.ExportProgress(),out var warning),warning);var saved=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(saved,warning);Assert.IsTrue(saved.Valid());Assert.AreEqual(1,saved.career.diary.Count);
            game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(.8f);Assert.IsNotNull(Find<Button>("DiaryExpand"));Find<Button>("DiaryExpand").onClick.Invoke();Assert.AreEqual(OpsDiaryCatalog.Entries[15].body,Find<TextMeshProUGUI>("DiaryBody").text);
            game.OpenDiaryBook();yield return new WaitForSecondsRealtime(.8f);Assert.IsFalse(Find<Button>("DiaryPage_16").interactable);Assert.IsTrue(Find<Button>("DiaryPage_15").interactable);Capture("next10-diary-book");
            // 悪い月の実記録。連載の試験結果とゲームの成績は独立した欄。
            game.StartYear(2);game.State.budget=400;for(int m=0;m<9;m++){DiaryTestMonth(game.State);if(game.State.QuarterRewardPending)game.State.ClaimQuarterReward("budget");game.State.NextMonth();}DiaryTestMonth(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next10-diary-month-bad-before");game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(3);Capture("next10-diary-month-bad");
            StringAssert.Contains(game.State.Latest.loss+"万円",Find<TextMeshProUGUI>("DiaryActualRecord").text);Assert.AreEqual("diary_y1_01",game.LastReactionId);
            game.StartStory(14);for(int i=0;i<game.State.levels.Length;i++)game.State.levels[i]=2;game.State.culture=game.State.trust=100;game.State.budget=400;
            for(int m=0;m<11;m++){DiaryTestMonth(game.State);if(game.State.QuarterRewardPending)game.State.ClaimQuarterReward("budget");game.State.NextMonth();}DiaryTestMonth(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next10-diary-year-end-before");game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(3);Capture("next10-diary-year-end");Assert.AreEqual("1年目のおわりに",Find<TextMeshProUGUI>("DiaryDate").text);Assert.AreEqual(12,game.Career.diary.Single(p=>p.key==11).monthNotes.Length);
            Find<Button>("DiaryClose").onClick.Invoke();yield return null;Assert.AreEqual(OpsPhase.Ended,game.State.phase);Assert.IsNotNull(Find<Button>("NextStoryYear"));
            game.OpenDiaryBook();Find<Button>("DiaryPage_15").onClick.Invoke();yield return null;Find<Button>("DiaryReplay").onClick.Invoke();yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual("diary_y2_07",game.LastReactionId);Assert.IsFalse(game.PortraitVoicePlaying);Assert.IsNotEmpty(Find<TextMeshProUGUI>("DiaryBody").text);Assert.AreEqual(OpsDiaryCatalog.Entries[15].intro,Find<TextMeshProUGUI>("DiaryVoiceCaption").text);
            typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,false);Find<Button>("DiaryReplay").onClick.Invoke();Assert.IsEmpty(Find<TextMeshProUGUI>("DiaryVoiceCaption").text);Assert.IsNotEmpty(Find<TextMeshProUGUI>("DiaryBody").text);
            foreach(var text in Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>new[]{"DiaryBody","DiaryThought","DiaryActualRecord","DiaryMemoText"}.Contains(t.name))){text.ForceMeshUpdate();Assert.IsFalse(text.isTextOverflowing,text.name);}
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next10DiaryData_三十九ページと三十八音声と旧保存を保持する()
        {
            Assert.AreEqual(39,OpsDiaryCatalog.Entries.Length);Assert.AreEqual(38,OpsDiaryCatalog.Entries.Select(e=>e.voiceId).Where(id=>id!="").Distinct().Count());
            for(int y=1;y<=3;y++)for(int m=0;m<12;m++){var e=OpsDiaryCatalog.Entries[OpsDiaryCatalog.Page(y,m)];Assert.AreEqual(y,e.year);Assert.AreEqual(m,e.month);Assert.IsNotEmpty(e.body);Assert.IsNotEmpty(e.memo);}
            StringAssert.Contains("DNS",OpsDiaryCatalog.Entries[15].intro);StringAssert.Contains("TTL",OpsDiaryCatalog.Entries[15].intro);
            Assert.IsTrue(new OpsCareer{diary=null}.Valid());var career=new OpsCareer();career.diary.Add(new OpsDiaryRecord{key=0,content=0,recap="実記録",thought="ひとこと",rank="B",minigame=""});Assert.IsTrue(career.Valid());career.diary.Add(career.diary[0]);Assert.IsFalse(career.Valid());
        }
    }
}
#endif
