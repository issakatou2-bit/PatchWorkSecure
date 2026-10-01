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
        [UnityTest] public IEnumerator Next9Renew_全額繰越と設備見直しと年度の道のりを撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartStory(14);
            FinishStoryTestYear(game.State);game.State.budget=38;game.State.culture=68;game.State.trust=71;
            foreach(var p in OpsCatalog.Projects)game.State.levels[OpsCatalog.Index(p.id)]=0;
            foreach(string id in new[]{"backup","drill","runbook"})game.State.levels[OpsCatalog.Index(id)]=2;
            foreach(string id in new[]{"inventory","mfa","automation","education"})game.State.levels[OpsCatalog.Index(id)]=1;
            game.State.capacity=0;Assert.IsTrue(game.State.Valid(),"撮影状態も有効な年度である");game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next9-year-renew");
            Assert.AreEqual("114<size=15>万円</size>",Find<TextMeshProUGUI>("CarryValue0").text);Assert.AreEqual("58",Find<TextMeshProUGUI>("CarryValue3").text);
            Assert.AreEqual(3,Object.FindObjectsByType<TextMeshProUGUI>().Count(t=>t.name.StartsWith("EquipmentReviewLabel")&&t.text=="見直し"));
            var prior=JsonUtility.ToJson(game.State);CheckPointer("StoryAnnualReview");Click("StoryAnnualReview");yield return new WaitForSecondsRealtime(3);CheckPointer("BackHome");Click("BackHome");yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual(prior,JsonUtility.ToJson(game.State));CheckPointer("NextStoryYear");Click("NextStoryYear");yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual(2,game.Story.year);Assert.AreEqual(114,game.State.budget);Assert.AreEqual(12,game.State.yearPressure);Assert.IsTrue(game.ExportProgress().Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next9Title_承認済み三つのモードと因子札を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1.2f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            Assert.AreEqual(new Vector2(470,92),Find<Button>("NewYear").GetComponent<RectTransform>().sizeDelta);
            Assert.AreEqual(new Vector2(90,-292),Find<Button>("NewYear").GetComponent<RectTransform>().anchoredPosition);
            Assert.IsFalse(Find<Button>("EndlessYear").interactable);Assert.IsNotNull(Find<TextMeshProUGUI>("EndlessLockText"));
            Capture("next9-title-empty");
            yield return null;CheckPointer("NewYear");CheckPointer("SingleYear");CheckPointer("HomeGuide");CheckPointer("HomeSettings");
            game.Career.factors.Add("backup");game.Career.factors.Add("inventory");game.BuildPreview();
            yield return new WaitForSecondsRealtime(1.2f);Capture("next9-title-mode");
            var label=Find<Button>("NewYear").GetComponentInChildren<TextMeshProUGUI>();label.ForceMeshUpdate();Assert.IsTrue(label.textInfo.characterInfo.Any(c=>c.isVisible),"本編のボタン名が描画されている");
            Assert.AreEqual("因子 2/3",Find<TextMeshProUGUI>("StoryFactors").text);CheckText();
            game.Career.endlessUnlocked=true;game.BuildPreview();yield return null;
            CheckPointer("EndlessYear");Click("EndlessYear");StringAssert.Contains("準備中",Find<TextMeshProUGUI>("DialogBody").text);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
