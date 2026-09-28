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
        [UnityTest] public IEnumerator QuickWins2_実際の昇格で文字を切り替え降格と省演出は回さない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");
                game.State.culture=33;game.OpenTab(0);game.ChooseAction("listen");yield return null;
                var effect=Object.FindObjectsByType<OpsRankChange>().First(e=>e.Before=="E"&&e.After=="D");Assert.IsTrue(effect.Up);
                var label=effect.GetComponent<TextMeshProUGUI>();string state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.1f);
                if(reduced)Assert.AreEqual(Vector3.one,label.transform.localScale);else Assert.Less(label.transform.localScale.x,1);
                Capture(reduced?"112-quickwins2-reduced":"112-quickwins2-rank");yield return new WaitForSecondsRealtime(.45f);Assert.AreEqual("D",label.text);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                game.OpenTab(0);yield return null;Assert.IsFalse(Object.FindObjectsByType<OpsRankChange>().Any());
                // 降格は同じ表示部品の単体条件でも確認する。
                var target=Find<TextMeshProUGUI>("RankValue");var drop=target.gameObject.AddComponent<OpsRankChange>();drop.Owner=game;drop.Before="A";drop.After="B";drop.Up=false;drop.Badge=target.transform.parent.GetComponent<Image>();
                yield return new WaitForSecondsRealtime(.12f);Assert.AreEqual(Vector3.one,target.transform.localScale);Assert.AreEqual("B",target.text);yield return new WaitForSecondsRealtime(.4f);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuickWins1_増収だけにコインが出て省演出では動かない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");
                game.ChooseAction("audit");game.ChooseAction("proposal");yield return null;var effect=Find<OpsBudgetGain>("BudgetGainEffect");Assert.IsNotNull(effect);
                var first=effect.Coins[0].anchoredPosition;var state=JsonUtility.ToJson(game.State);
                yield return new WaitForSecondsRealtime(.18f);Capture(reduced?"111-quickwins1-reduced":"111-quickwins1-coins");
                if(reduced){Assert.AreEqual(first,effect.Coins[0].anchoredPosition);Assert.AreEqual(Vector3.one,effect.Target.localScale);}
                else Assert.AreNotEqual(first,effect.Coins[0].anchoredPosition);
                Assert.AreEqual(state,JsonUtility.ToJson(game.State));yield return new WaitForSecondsRealtime(.6f);Assert.IsFalse(Object.FindObjectsByType<OpsBudgetGain>().Any());
                game.OpenTab(0);yield return null;Assert.IsFalse(Object.FindObjectsByType<OpsBudgetGain>().Any());
                game.Buy(OpsCatalog.Index("backup"));yield return null;Assert.IsFalse(Object.FindObjectsByType<OpsBudgetGain>().Any());
            }
            LogAssert.NoUnexpectedReceived();
        }
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
