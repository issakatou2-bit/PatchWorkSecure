#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator 購入と疲労回復と社員の助力を確定値で演出する()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); yield return null;
            int oldBudget = game.State.budget;
            game.Buy(OpsCatalog.Index("backup")); yield return null;
            StringAssert.Contains("分離バックアップ", Find<TextMeshProUGUI>("InstallationLabel").text);
            StringAssert.Contains("Lv.1", Find<TextMeshProUGUI>("InstallationLabel").text);
            Assert.AreEqual("#70B4FF", "#" + ColorUtility.ToHtmlStringRGB(Find<TextMeshProUGUI>("StatChangeAmount0").color));
            Assert.AreEqual((game.State.budget - oldBudget) + "万円", Find<TextMeshProUGUI>("StatChangeAmount0").text);
            CheckPointer("Action_listen");
            Capture("40-installation-feedback", 1280, 720);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(Vector3.one, Find<UnityEngine.UI.Button>("Pin_backup").transform.localScale);
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t => t.name == "StatChangeEffect0"));
            game.State.fatigue = 50; game.ChooseAction("rest"); yield return null;
            Assert.AreEqual("#47D7A0", "#" + ColorUtility.ToHtmlStringRGB(Find<TextMeshProUGUI>("StatChangeAmount5").color));
            Assert.AreEqual("-18", Find<TextMeshProUGUI>("StatChangeAmount5").text);
            game.State.staffExperience[1] = OpsGrowthCatalog.StaffThresholds[1];
            game.State.supportOrder = "investigate";
            game.BeginIncident(); game.Resolve("scope"); yield return WaitForResolution(game);
            var result = game.State.Latest;
            Assert.Greater(result.power.staff, 0);
            StringAssert.Contains("+" + result.power.staff, Find<TextMeshProUGUI>("OutcomeSupportTitle").text);
            StringAssert.Contains(result.loss + "万円", Find<TextMeshProUGUI>("OfficeOutcomeNumbers").text);
            StringAssert.Contains("今月の記録", Find<TextMeshProUGUI>("OfficeOutcomeNote").text);
            Capture("41-staff-response-feedback", 1280, 720);
            foreach (string name in new[] { "OfficeOutcomeTitle", "OfficeOutcomeNumbers", "OfficeOutcomeNote", "OutcomeSupportTitle", "OutcomeSupportDetail" })
            {
                var text = Find<TextMeshProUGUI>(name); text.ForceMeshUpdate();
                Assert.IsFalse(text.isTextOverflowing, name + " / " + text.text);
            }
            CheckPointer("NextMonth");
            Click("Menu"); yield return null; Click("ReduceMotion"); yield return null; Click("CloseDialog"); yield return null;
            game.Next(); yield return null; game.ChooseAction("listen"); yield return null;
            var effect = Find<RectTransform>("StatChangeEffect3"); var origin = effect.anchoredPosition;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(origin, effect.anchoredPosition);
            game.OpenTab(1); yield return null;
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t => t.name == "StatChangeEffect3"));
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
