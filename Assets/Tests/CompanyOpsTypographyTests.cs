#if UNITY_INCLUDE_TESTS
using System.Collections;
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
        [UnityTest] public IEnumerator 本文と見出しの書体と角丸を分けても日本語と操作が保たれる()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>();
            Assert.AreEqual(AtlasPopulationMode.Static, game.Font.atlasPopulationMode);
            Assert.AreEqual(AtlasPopulationMode.Static, game.HeadingFont.atlasPopulationMode);
            Assert.AreNotEqual(game.Font, game.HeadingFont);
            foreach (var font in new[] { game.Font, game.HeadingFont })
            {
                Assert.AreEqual(1, font.faceInfo.scale);
                Assert.IsTrue(font.HasCharacters("予算 ￥2,450,000 工数 人望 社員 経営 復旧 調査 0/100 ＋ → ～", out uint[] missing, true, true),
                    missing == null ? "" : string.Join(",", missing));
                Assert.IsNotEmpty(font.fallbackFontAssetTable);
                Assert.AreEqual(AtlasPopulationMode.Dynamic, font.fallbackFontAssetTable[0].atlasPopulationMode);
            }
            game.StartYear(14); yield return null;
            Assert.AreEqual(game.Font, Find<TextMeshProUGUI>("NavigatorSpeech").font);
            Assert.AreEqual(game.HeadingFont, Find<TextMeshProUGUI>("予算Value").font);
            Assert.AreEqual(game.HeadingFont, Find<TextMeshProUGUI>("CaseTitle").font);
            var panel = Find<UnityEngine.UI.Image>("Navigator");
            var button = Find<UnityEngine.UI.Button>("Action_audit").GetComponent<UnityEngine.UI.Image>();
            Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, panel.type);
            Assert.Greater(button.pixelsPerUnitMultiplier, panel.pixelsPerUnitMultiplier);
            CheckText(); CheckPointer("Action_audit"); CheckPointer("OpenTeam");
            Capture("33-typography-rounded", 1280, 720);
            Click("OpenTeam"); yield return null; CheckTeamText(); CheckPointer("CloseDialog");
            Capture("34-typography-team");
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
