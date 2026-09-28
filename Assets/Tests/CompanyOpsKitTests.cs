#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator 共通ウィンドウと四種のボタンは承認キットで動作する()
        {
            SceneManager.LoadScene("CompanyYear"); yield return new WaitForSecondsRealtime(.6f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(.4f);
            Click("Stat_0");yield return new WaitForSecondsRealtime(.5f);
            var dialog=Find<RectTransform>("Dialog"); Assert.IsNotNull(dialog.Find("KitHeader/WindowCategory"));
            Assert.AreEqual("予算 / 76万円",Find<TextMeshProUGUI>("DialogHeading").text);
            Assert.Greater(dialog.GetComponent<Image>().color.r,.9f);
            Assert.IsNotNull(dialog.GetComponent<OpsKitGradient>());Assert.IsNotNull(Find<Button>("HeaderClose"));
            CheckPointer("HeaderClose");CheckPointer("CloseDialog");Capture("70-kit-standard-window");
            Click("CloseDialog");yield return new WaitForSecondsRealtime(.25f);
            foreach(var name in new[]{"Action_audit","OpenProjects","AdvanceMonth"})
            {var button=Find<Button>(name);Assert.IsNotNull(button.GetComponent<OpsKitGradient>());Assert.AreEqual(4,button.GetComponent<OpsButtonFeedback>().PressDepth);CheckPointer(name);}
            Capture("71-kit-planning");Click("OpenProjects");yield return new WaitForSecondsRealtime(.5f);
            Assert.IsNotNull(Find<RectTransform>("PlanningDetails").GetComponent<OpsKitGradient>());Capture("72-kit-project-list");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
