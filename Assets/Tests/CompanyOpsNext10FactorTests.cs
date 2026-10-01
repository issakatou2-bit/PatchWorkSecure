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
        [UnityTest] public IEnumerator Next10FactorReveal_順番と光とスキップと既存選択への接続を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartStory(14);for(int y=1;y<=3;y++){FinishStoryTestYear(game.State);if(y<3)Assert.IsTrue(game.NextStoryYear());}
            game.OpenTab(0);yield return new WaitForSecondsRealtime(3);
            var prior=JsonUtility.ToJson(game.ExportProgress());Find<Button>("BackHome").onClick.Invoke();game.FactorRevealPaused=true;
            Assert.IsTrue(game.FactorRevealActive);Assert.AreEqual(prior,JsonUtility.ToJson(game.ExportProgress()));
            Assert.IsFalse(game.Surface.GetComponentsInChildren<Transform>().Any(t=>t.name=="ConfirmFactorDepth"));
            for(int i=0;i<3;i++)Assert.IsTrue(Find<Image>("FactorBack"+i).gameObject.activeInHierarchy);
            yield return new WaitForSecondsRealtime(.8f);CheckPointer("SkipFactorReveal");yield return null;Capture("next10-factor-reveal-start");
            game.TickFactorReveal(1.45f);yield return null;Capture("next10-factor-reveal");
            Assert.IsNotNull(Find<TextMeshProUGUI>("FactorCardName0"));Assert.IsNotNull(Find<TextMeshProUGUI>("FactorCardName1"));Assert.IsNotNull(Find<Image>("FactorBack2"));
            Assert.IsTrue(game.FactorRevealActive);Assert.IsFalse(Find<Button>("ChooseFactor0").interactable);
            foreach(var burst in Object.FindObjectsByType<OpsFactorBurstGraphic>(FindObjectsInactive.Include))Assert.AreEqual(3,burst.Stars);
            Assert.AreEqual(prior,JsonUtility.ToJson(game.ExportProgress()));
            Find<Button>("SkipFactorReveal").onClick.Invoke();yield return new WaitForSecondsRealtime(.8f);Assert.IsFalse(game.FactorRevealActive);CheckPointer("ConfirmFactor");Capture("next10-factor-selection");
            Assert.AreEqual(prior,JsonUtility.ToJson(game.ExportProgress()));Find<Button>("ConfirmFactor").onClick.Invoke();yield return null;
            Assert.IsTrue(game.Story.rewardClaimed);Assert.AreEqual(1,game.Career.factors.Count);
            // 自然終了・軽減設定も実際の入口から。ここではテスト用のClick補助を使わない。
            game.StartStory(9);game.State.stability=0;game.State.phase=OpsPhase.Ended;game.OpenTab(0);yield return new WaitForSecondsRealtime(1);
            typeof(OpsGame).GetProperty("ReducedMotion").SetValue(game,true);Find<Button>("BackHome").onClick.Invoke();game.FactorRevealPaused=true;game.TickFactorReveal(.8f);yield return null;
            Assert.IsTrue(game.FactorRevealActive);Assert.IsTrue(Object.FindObjectsByType<OpsFactorBurstGraphic>().Length==0);game.TickFactorReveal(3);yield return new WaitForSecondsRealtime(.8f);Assert.IsFalse(game.FactorRevealActive);CheckPointer("ConfirmFactor");
            Assert.IsTrue(game.ExportProgress().Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next10FactorLight_星の色だけを変え効果を変えない()
        {
            Assert.AreEqual(new Color(.498f,.769f,1),OpsFactorBurstGraphic.Tone(1));Assert.AreEqual(new Color(1,.843f,.353f),OpsFactorBurstGraphic.Tone(2));
            Assert.AreNotEqual(OpsFactorBurstGraphic.Tone(3,0),OpsFactorBurstGraphic.Tone(3,.5f));
            var story=OpsStory.Begin(14,null);FinishStoryTestYear(story.state);story.RecordYear();var career=new OpsCareer();
            Assert.IsTrue(story.FactorCandidates(career).All(c=>c.stars==1));
        }
    }
}
#endif
