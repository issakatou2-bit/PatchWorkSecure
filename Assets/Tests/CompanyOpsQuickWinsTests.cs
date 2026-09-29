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
        [UnityTest] public IEnumerator QuickWins8_前月末の実数と比較し疲労減少を緑で示す()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(.7f);
            Assert.IsFalse(Object.FindObjectsByType<RectTransform>().Any(t=>t.name.StartsWith("MonthlyTrend")));StringAssert.Contains("初月",Find<TextMeshProUGUI>("GrowthTrendHeading").text);
            var first=game.State.Latest;game.Next();game.ChooseAction("rest");game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(.8f);
            var second=game.State.Latest;var values=new[]{second.loss-first.loss,second.downtime-first.downtime,second.cost-first.cost};string state=JsonUtility.ToJson(game.State);
            for(int i=0;i<3;i++)
            {
                Assert.AreEqual(System.Math.Abs(values[i]).ToString(),Find<TextMeshProUGUI>("MonthlyTrend"+i+"Amount").text);
                Assert.AreEqual(values[i]>0?"trend-up":values[i]<0?"trend-down":"trend-flat",Find<OpsIncidentGraphic>("MonthlyTrend"+i+"Arrow").Kind);
                var number=Find<TextMeshProUGUI>(i==0?"MonthlyLossValue":i==1?"MonthlyStopValue":"MonthlyCostValue");number.ForceMeshUpdate();Assert.IsFalse(number.isTextOverflowing);
            }
            int fatigueDelta=first.metricsAfter[4]-second.metricsAfter[4];Assert.AreEqual(System.Math.Abs(fatigueDelta).ToString(),Find<TextMeshProUGUI>("GrowthTrend4Amount").text);
            Assert.IsTrue(OpsGame.IsGoodMonthlyChange(-5,false));Assert.IsFalse(OpsGame.IsGoodMonthlyChange(5,false));Assert.IsTrue(OpsGame.IsGoodMonthlyChange(5,true));
            var color=Find<TextMeshProUGUI>("GrowthTrend4Amount").color;if(fatigueDelta<0)Assert.Greater(color.g,color.r);
            Capture("118-quickwins8-monthly");Assert.AreEqual(state,JsonUtility.ToJson(game.State));Click("Menu");if(!game.ReducedMotion)Click("ReduceMotion");Click("CloseDialog");game.OpenTab(0);yield return new WaitForSecondsRealtime(.8f);Capture("118-quickwins8-reduced");Assert.AreEqual(state,JsonUtility.ToJson(game.State));
            Assert.IsFalse(Object.FindObjectsByType<OpsRankChange>().Any(),"月報の再表示では昇格演出を繰り返さない");
            first.metricsAfter=null;game.OpenTab(0);yield return null;StringAssert.Contains("未記録",Find<TextMeshProUGUI>("GrowthTrendHeading").text);Assert.IsFalse(Object.FindObjectsByType<RectTransform>().Any(t=>t.name=="GrowthTrend4"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuickWins7_実際に働いた社員だけ顔マークが跳ねる()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(bool reduced in new[]{false,true})
            {
                game.StartYear(14);Click("Menu");if(game.ReducedMotion!=reduced)Click("ReduceMotion");Click("CloseDialog");SetEvent(game.State,"ransom-backup");
                game.State.staffExperience[1]=3;game.State.supportOrder="investigate";game.BeginIncident();game.Resolve("scope");Assert.Greater(game.State.Latest.power.staff,0);Assert.IsFalse(game.State.Latest.benign);
                string state=JsonUtility.ToJson(game.State),member=game.State.Latest.power.support.Split('：')[0];float deadline=Time.realtimeSinceStartup+5;
                while(!Object.FindObjectsByType<OpsStaffBounce>().Any()&&Time.realtimeSinceStartup<deadline)yield return null;
                var bounce=Object.FindObjectsByType<OpsStaffBounce>().First();Assert.AreEqual(member,bounce.Member);yield return new WaitForSecondsRealtime(.08f);
                var rect=bounce.GetComponent<RectTransform>();if(reduced)Assert.AreEqual(new Vector2(16,-12),rect.anchoredPosition);else Assert.Greater(rect.anchoredPosition.y,-12);
                Assert.AreEqual(Vector3.one,rect.localScale);Capture(reduced?"117-quickwins7-reduced":"117-quickwins7-support");yield return new WaitForSecondsRealtime(.35f);Assert.IsFalse(Object.FindObjectsByType<OpsStaffBounce>().Any());Assert.AreEqual(state,JsonUtility.ToJson(game.State));yield return WaitForResolution(game);
            }
            game.StartYear(14);game.BeginIncident();game.Resolve("scope");Assert.AreEqual(0,game.State.Latest.power.staff);
            while(game.ResolutionActive){Assert.IsFalse(Object.FindObjectsByType<OpsStaffBounce>().Any());Assert.IsFalse(Object.FindObjectsByType<RectTransform>().Any(t=>t.name.StartsWith("StaffFace_")));yield return null;}
            LogAssert.NoUnexpectedReceived();
        }
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
                Click("Menu");Find<Slider>("SoundVolume").value=.4f;
                Assert.IsTrue(game.GetComponents<AudioSource>().Where(s=>s.clip==game.Sounds.damage).All(s=>s.volume<=.4f*.25f+.001f));
                Find<Slider>("SoundVolume").value=0;yield return new WaitForSecondsRealtime(.3f);Assert.IsFalse(game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.damage&&s.isPlaying));
                Find<Slider>("SoundVolume").value=.6f;Click("CloseDialog");
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
                Click("Menu");Find<Slider>("SoundVolume").value=.4f;
                Assert.IsTrue(game.GetComponents<AudioSource>().Where(s=>s.clip==game.Sounds.damage).All(s=>s.volume<=.4f*.12f+.001f));
                Find<Slider>("SoundVolume").value=0;yield return new WaitForSecondsRealtime(.3f);Assert.IsFalse(game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.damage&&s.isPlaying));
                Find<Slider>("SoundVolume").value=.6f;Click("CloseDialog");
                string state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.5f);Assert.AreNotEqual(alpha,pulse.color.a);Assert.AreEqual(pos,pulse.rectTransform.anchoredPosition);Assert.AreEqual(Vector3.one,pulse.transform.localScale);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                Capture(reduced?"114-quickwins4-reduced":"114-quickwins4-danger");game.OpenTab(0);Assert.AreEqual(1,game.DangerPulseCount);
                game.State.stability=33;game.OpenTab(0);Assert.AreEqual(2,game.DangerPulseCount);game.State.stability=34;game.OpenTab(0);Assert.AreEqual(2,game.DangerPulseCount);
                game.State.stability=33;game.OpenTab(0);Assert.AreEqual(3,game.DangerPulseCount,"一度回復して再び悪化した場合も一打だけ知らせる");
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
                var label=effect.GetComponent<TextMeshProUGUI>();var badgeColor=effect.Badge.color;string state=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.1f);
                if(reduced){Assert.AreEqual(Vector3.one,label.transform.localScale);Assert.AreEqual(badgeColor,effect.Badge.color,"省演出は透明度のみ");}else Assert.Less(label.transform.localScale.x,1);
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
        [UnityTest] public IEnumerator QuickWins1_四半期報酬を含む実増収を既存ルールのまま演出する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            for(int month=0;month<3;month++)
            {
                game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);
                if(month<2)game.Next();
            }
            Assert.IsTrue(game.State.QuarterRewardPending);
            // 報酬なしの月次収支は赤字だが、報酬を含む実際の予算は増える表示条件。
            game.State.budget=100;game.State.trust=0;
            for(int i=0;i<game.State.levels.Length&&game.State.Upkeep<=game.State.MonthlyGrant;i++)game.State.levels[i]=2;
            Assert.Greater(game.State.Upkeep,game.State.MonthlyGrant);
            Assert.Less(game.State.Upkeep,game.State.MonthlyGrant+OpsGrowthCatalog.QuarterBudget);
            int before=game.State.budget;var expected=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(game.State));
            Assert.IsTrue(expected.ClaimQuarterReward("budget"));Assert.IsTrue(expected.NextMonth());
            game.OpenTab(0);Click("NextMonth");yield return null;CheckPointer("Reward_budget");Click("Reward_budget");yield return null;
            Assert.AreEqual(JsonUtility.ToJson(expected),JsonUtility.ToJson(game.State),"状態・報酬額は既存ルールと一致する");
            Assert.Greater(game.State.budget,before);Assert.IsNotNull(Find<OpsBudgetGain>("BudgetGainEffect"));Capture("119-quickwins1-quarter-reward");
            yield return new WaitForSecondsRealtime(.7f);Assert.IsFalse(Object.FindObjectsByType<OpsBudgetGain>().Any());LogAssert.NoUnexpectedReceived();
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
