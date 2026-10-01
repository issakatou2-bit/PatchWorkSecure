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
