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
        [Test] public void Next12Names_呼び方と録り直しの字幕とエンジニアの設定を保つ()
        {
            Assert.AreEqual("かのん",OpsDiaryRecord.PageTitles[2]);
            foreach(var e in OpsDiaryCatalog.Entries){StringAssert.DoesNotContain("秘書さん",e.body);StringAssert.DoesNotContain("秘書さん",e.intro);}
            foreach(int page in new[]{2,14,21})
            {
                var e=OpsDiaryCatalog.Entries[page];StringAssert.Contains("かのんさん",e.intro);StringAssert.EndsWith("_kanon",e.voiceId);
                Assert.AreEqual(e.intro,OpsReactionBank.ScriptV2().Single(l=>l.id==e.voiceId).caption);
            }
            foreach(int year in new[]{2,3}){Assert.AreEqual("かのん",OpsCatalog.CompanyYear(year).allyNames[2]);Assert.AreEqual("りりぃ",OpsCatalog.CompanyYear(year).allyNames[0]);}
            StringAssert.Contains("試験は一回で受かったけど、会議は三回すっぽかした",OpsDiaryCatalog.Entries[9].body);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next12NamesUI_日記と開幕と因子の表示を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();WithoutPublishedVoices(game);game.StartStory(14);
            for(int m=0;m<3;m++){DiaryTestMonth(game.State);if(m<2)game.State.NextMonth();}
            game.OpenTab(0);game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(2);
            StringAssert.Contains("かのん",Find<TextMeshProUGUI>("DiaryBody").text);Assert.AreEqual("diary_y1_06_kanon",game.LastReactionId);Assert.IsFalse(game.PortraitVoicePlaying);Capture("next12-names-diary");
            Find<Button>("DiaryClose").onClick.Invoke();game.OpenDiaryBook();yield return new WaitForSecondsRealtime(1);Capture("next12-names-book");
            game.OpenTab(0);game.State.NextMonth();FinishStoryTestYear(game.State);game.NextStoryYear();game.PreviewYearOpening(3);yield return new WaitForSecondsRealtime(2);Capture("next12-names-allies");
            Assert.Contains("かのん",Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name=="AllyName").Select(t=>t.text).ToArray());
            game.SkipYearOpening();game.AdvanceYearOpening();FinishStoryTestYear(game.State);game.NextStoryYear();game.SkipYearOpening();game.AdvanceYearOpening();FinishStoryTestYear(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);
            Find<Button>("BackHome").onClick.Invoke();game.SkipFactorReveal();yield return new WaitForSecondsRealtime(1);Capture("next12-names-factor");
            StringAssert.StartsWith("かのんの推薦：",Find<TextMeshProUGUI>("FactorRecommendation2").text);StringAssert.StartsWith("りりぃの推薦：",Find<TextMeshProUGUI>("FactorRecommendation0").text);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
