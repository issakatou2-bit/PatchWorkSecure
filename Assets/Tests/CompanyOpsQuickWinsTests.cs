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
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator QuickWins0_全事件の説明札はひなたと重ならず読める()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);
            var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");
                foreach(var entry in OpsEventCatalog.Events)
                {
                    game.StartYear(14);SetEvent(game.State,entry.id);game.BeginIncident();yield return null;
                    var card=Find<RectTransform>("IncidentSymptomCard");var character=Find<RectTransform>("IncidentHinata");
                    Assert.Less(-card.anchoredPosition.y+card.rect.height,-character.anchoredPosition.y-12,entry.id);
                    var line=Find<TextMeshProUGUI>("IncidentSymptom");line.ForceMeshUpdate();Assert.IsFalse(line.isTextOverflowing,entry.id);
                    CheckPointer("Respond_scope");
                }
                yield return new WaitForSecondsRealtime(.6f);Capture(reduced?"110-quickwins0-reduced":"110-quickwins0-incident");
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
