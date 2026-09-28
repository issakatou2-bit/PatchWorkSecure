#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using System.Reflection;
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
        [UnityTest] public IEnumerator 基本ポーズは閉眼と閉口を別範囲で同時表示し原画を保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            var earlyMotion=Find<OpsPortraitMotion>("NavigatorPortrait");earlyMotion.ShowEmotion(3);Assert.IsTrue(earlyMotion.GetComponentsInChildren<OpsEmotionMotion>().Any(m=>m.Kind==3),"画面生成直後も感情マークを表示する");yield return new WaitForSecondsRealtime(2.5f);
            var portrait=Find<Image>("NavigatorPortrait");var animator=portrait.GetComponent<OpsPortraitAnimator>();Assert.IsTrue(animator.HasFrames);
            string state=JsonUtility.ToJson(game.State);var random=UnityEngine.Random.state;var sprite=portrait.sprite;
            animator.Blink();float deadline=Time.unscaledTime+.18f;while(animator.EyePhase!=2&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(2,animator.EyePhase);var eyes=portrait.GetComponentsInChildren<Image>().First(i=>i.name=="HinataEyes");var mouth=portrait.GetComponentsInChildren<Image>().First(i=>i.name=="HinataMouth");
            Assert.IsTrue(eyes.enabled&&mouth.enabled);StringAssert.EndsWith("eyes_closed",eyes.sprite.name);StringAssert.EndsWith("mouth_closed",mouth.sprite.name);Assert.AreSame(sprite,portrait.sprite);Capture("100-hinata-blink-closed");
            yield return new WaitForSecondsRealtime(.35f);Assert.AreEqual(0,animator.EyePhase);
            animator.Speak(Find<TextMeshProUGUI>("NavigatorSpeech"));bool open=false,closed=false;
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){open|=animator.MouthIsOpen;closed|=!animator.MouthIsOpen;yield return null;}
            Assert.IsTrue(open&&closed,"文字送り中に開口・閉口の両方が必要");
            var originalSize=portrait.rectTransform.sizeDelta;portrait.rectTransform.sizeDelta=new Vector2(500,430);yield return null;
            var frame=game.Navigator.AnimationFrames.First(f=>f.PoseId=="pose_fists");float width=430*496f/615;var mask=(RectTransform)mouth.transform.parent;
            Assert.AreEqual(-width/2+width*frame.MouthRegion.x-(500-width)/2,mask.anchoredPosition.x,.01f,"横長の枠でも目口を原画と同じ位置へ寄せる");Capture("104-hinata-wide-frame-alignment");portrait.rectTransform.sizeDelta=originalSize;
            Assert.AreEqual(state,JsonUtility.ToJson(game.State));Assert.AreEqual(random,UnityEngine.Random.state);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 省演出は呼吸と移動を止めてもまばたきを残す()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(.6f);
            Click("Menu");Click("ReduceMotion");Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(game.ReducedMotion);
            var portrait=Find<Image>("NavigatorPortrait");var animator=portrait.GetComponent<OpsPortraitAnimator>();var motion=portrait.GetComponent<OpsPortraitMotion>();
            animator.Blink();float deadline=Time.unscaledTime+.18f;while(animator.EyePhase!=2&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(2,animator.EyePhase);Assert.AreEqual(Vector3.one,portrait.transform.localScale);Assert.AreEqual(motion.LayoutPosition,portrait.rectTransform.anchoredPosition);Assert.AreEqual(Quaternion.identity,portrait.transform.localRotation);
            Capture("101-hinata-reduced-blink");yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(motion.LayoutPosition,portrait.rectTransform.anchoredPosition);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ポーズ交換は足元を保ち差分がない絵へ安全に戻る()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(2);
            var portrait=Find<Image>("NavigatorPortrait");var animator=portrait.GetComponent<OpsPortraitAnimator>();var motion=portrait.GetComponent<OpsPortraitMotion>();var position=motion.LayoutPosition;
            animator.ChangePose("pose_point");yield return new WaitForSecondsRealtime(.3f);Assert.IsFalse(animator.HasFrames);Assert.AreSame(game.Navigator.Pose("pose_point"),portrait.sprite);Assert.AreEqual(position,motion.LayoutPosition);
            animator.ChangePose("pose_fists");yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(animator.HasFrames);Assert.AreEqual(position,motion.LayoutPosition);
            game.BeginIncident();yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(Object.FindObjectsByType<Image>().Any(i=>i.name=="HinataDeparting"));CheckPointer("Respond_scope");Capture("102-hinata-incident-motion");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 演出停止と効果音の揺らぎは判定と操作を止めずBGMを戻す()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(1.2f);
            float scale=Time.timeScale;game.HoldPresentation();Assert.IsTrue(game.PresentationHeld);Assert.AreEqual(scale,Time.timeScale);game.ChooseAction("listen");Assert.IsTrue(game.State.listened);yield return new WaitForSecondsRealtime(.15f);Assert.IsFalse(game.PresentationHeld);
            var play=typeof(OpsGame).GetMethod("PlayCue",BindingFlags.NonPublic|BindingFlags.Instance);var pitches=new System.Collections.Generic.List<float>();var random=UnityEngine.Random.state;
            for(int i=0;i<8;i++){play.Invoke(game,new object[]{OpsCue.Click});yield return new WaitForSecondsRealtime(.05f);var source=game.GetComponents<AudioSource>().First(s=>s.clip==game.Sounds.click);Assert.That(source.pitch,Is.InRange(.95f,1.05f));pitches.Add(source.pitch);}
            Assert.Greater(pitches.Distinct().Count(),1);Assert.AreEqual(random,UnityEngine.Random.state);
            var music=game.GetComponents<AudioSource>().First(s=>s.clip==game.Sounds.planningMusic&&s.isPlaying);float volume=music.volume;game.DuckMusic(.3f);yield return new WaitForSecondsRealtime(.12f);Assert.Less(music.volume,volume*.8f);yield return new WaitForSecondsRealtime(.6f);Assert.AreEqual(volume,music.volume,.01f);Assert.AreEqual(scale,Time.timeScale);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 残像ゲージとあと一つ表示は実際の支出と達成条件だけから出す()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return new WaitForSecondsRealtime(.6f);
            int budget=game.State.budget;game.Buy(OpsCatalog.Index("backup"));yield return null;Assert.Less(game.State.budget,budget);Assert.IsNotNull(Find<OpsLossTrail>("BudgetLossTrail"));
            Assert.IsFalse(Find<OpsPortraitMotion>("NavigatorPortrait").Enter,"同じ計画画面の更新で再入場させない");Assert.IsFalse(Object.FindObjectsByType<Image>().Any(i=>i.name=="HinataDeparting"));
            Assert.AreEqual("あと1手",Find<TextMeshProUGUI>("GoalNear").text);Assert.AreEqual(200,Find<TextMeshProUGUI>("GoalLabel").rectTransform.rect.width,"目標名の幅はモック通り残す");Capture("103-budget-loss-trail");yield return new WaitForSecondsRealtime(.8f);Assert.IsFalse(Object.FindObjectsByType<OpsLossTrail>().Any());
            game.Buy(OpsCatalog.Index("drill"));yield return null;Assert.IsFalse(Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.name=="GoalNear"));Assert.IsTrue(game.State.milestones.Contains("戻せることを確かめた"));LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
