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
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator QuickWins6_行動と方針カードはマウスとキーボードで四ピクセル浮く()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");yield return new WaitForSecondsRealtime(.4f);
                var action=Find<Button>("Action_listen");var pos=((RectTransform)action.transform).anchoredPosition;
                ExecuteEvents.Execute(action.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(pos+Vector2.up*(reduced?0:4),((RectTransform)action.transform).anchoredPosition);CheckPointer("Action_listen");Capture(reduced?"116-quickwins6-action-reduced":"116-quickwins6-action");
                ExecuteEvents.Execute(action.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(pos,((RectTransform)action.transform).anchoredPosition);
                EventSystem.current.SetSelectedGameObject(action.gameObject);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(pos+Vector2.up*(reduced?0:4),((RectTransform)action.transform).anchoredPosition);
                game.BeginIncident();yield return new WaitForSecondsRealtime(.5f);var card=Find<RectTransform>("ResponseCard_scope");pos=card.anchoredPosition;var shadow=card.GetComponents<Shadow>().Last();float alpha=shadow.effectColor.a;
                ExecuteEvents.Execute(card.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(pos+Vector2.up*(reduced?0:4),card.anchoredPosition);Assert.Greater(shadow.effectColor.a,alpha);CheckPointer("Respond_scope");
                Capture(reduced?"116-quickwins6-card-reduced":"116-quickwins6-card");ExecuteEvents.Execute(card.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.2f);
                EventSystem.current.SetSelectedGameObject(Find<Button>("Respond_scope").gameObject);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(pos+Vector2.up*(reduced?0:4),card.anchoredPosition);EventSystem.current.SetSelectedGameObject(null);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(pos,card.anchoredPosition);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuickWins5_無効ボタンは理由だけ示し操作や資源を変えない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");
                game.State.capacity=0;game.OpenTab(0);yield return null;var button=Find<Button>("Action_listen");CheckPointer("Action_listen");Assert.IsFalse(button.interactable);
                string state=JsonUtility.ToJson(game.State);var pos=((RectTransform)button.transform).anchoredPosition;int count=game.RejectedPressCount;
                ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(.08f);Assert.AreEqual(count+1,game.RejectedPressCount);StringAssert.Contains("工数",Find<TextMeshProUGUI>("BlockedReasonText").text);
                if(reduced)Assert.AreEqual(pos,((RectTransform)button.transform).anchoredPosition);
                Capture(reduced?"115-quickwins5-reduced":"115-quickwins5-disabled");yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(pos,((RectTransform)button.transform).anchoredPosition);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);Assert.AreEqual(count+2,game.RejectedPressCount);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                yield return new WaitForSecondsRealtime(1.5f);Assert.IsFalse(Object.FindObjectsByType<OpsBlockedTag>().Any());
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuickWins4_危険域の最初と悪化だけ心音を鳴らし点滅は動かない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");
                game.State.stability=35;game.OpenTab(0);Assert.AreEqual(0,game.DangerPulseCount);
                game.State.stability=34;game.OpenTab(0);yield return null;Assert.AreEqual(1,game.DangerPulseCount);
                var pulse=Find<Image>("StabilityDanger");var pos=pulse.rectTransform.anchoredPosition;float alpha=pulse.color.a;
                string state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.5f);Assert.AreNotEqual(alpha,pulse.color.a);Assert.AreEqual(pos,pulse.rectTransform.anchoredPosition);Assert.AreEqual(Vector3.one,pulse.transform.localScale);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                Capture(reduced?"114-quickwins4-reduced":"114-quickwins4-danger");game.OpenTab(0);Assert.AreEqual(1,game.DangerPulseCount);
                game.State.stability=33;game.OpenTab(0);Assert.AreEqual(2,game.DangerPulseCount);game.State.stability=34;game.OpenTab(0);Assert.AreEqual(2,game.DangerPulseCount);
                yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.damage&&s.isPlaying));
                game.State.stability=36;game.OpenTab(0);Assert.IsFalse(Object.FindObjectsByType<OpsDangerPulse>().Any());
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuickWins3_工数を使い切った月だけ一度光り省演出で動かない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");
                foreach(string action in new[]{"audit","listen","map"})game.ChooseAction(action);
                Assert.IsFalse(Object.FindObjectsByType<OpsWorkComplete>().Any());game.ChooseAction("rest");yield return null;
                Assert.AreEqual(0,game.State.capacity);var effect=Find<OpsWorkComplete>("Stat_1");Assert.AreEqual(4,effect.Tokens.Length);
                string state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.16f);Capture(reduced?"113-quickwins3-reduced":"113-quickwins3-work");
                if(reduced){Assert.AreEqual(effect.Portrait.LayoutPosition,effect.Portrait.GetComponent<RectTransform>().anchoredPosition);Assert.AreEqual(Vector3.one,effect.Portrait.transform.localScale);}
                yield return new WaitForSecondsRealtime(.6f);Assert.IsFalse(Object.FindObjectsByType<OpsWorkComplete>().Any());Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                game.OpenTab(0);yield return null;Assert.IsFalse(Object.FindObjectsByType<OpsWorkComplete>().Any());
                game.State.capacity=1;game.State.audited=false;game.ChooseAction("audit");yield return null;Assert.IsFalse(Object.FindObjectsByType<OpsWorkComplete>().Any(),"同月で回復して使い切っても再生しない");
            }
            LogAssert.NoUnexpectedReceived();
        }
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
