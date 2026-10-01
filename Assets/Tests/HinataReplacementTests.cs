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
        [UnityTest] public IEnumerator 新ひなたの全表情と全ポーズは明示割当で共通の足元を保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);
            var p=Object.FindAnyObjectByType<OpsGame>().Navigator;
            Assert.AreEqual(18,p.Faces.Length);Assert.AreEqual(18,p.Poses.Length);
            foreach(var entry in p.Poses){Assert.IsNotNull(entry.Sprite,entry.Id);Assert.AreEqual(new Vector2(496,615),entry.Sprite.rect.size);Assert.AreEqual(new Vector2(248,0),entry.Sprite.pivot);}
            foreach(var entry in p.Faces){Assert.IsNotNull(entry.Sprite,entry.Id);Assert.AreEqual(new Vector2(546,548),entry.Sprite.rect.size);}
            Assert.AreSame(p.FaceNormal,p.Face("face_normal"));Assert.AreSame(p.FaceNormal,p.Face("未設定"));Assert.AreSame(p.Pose("pose_fists"),p.Pose("未設定"));
            foreach(var line in p.Reactions.lines){Assert.IsTrue(p.Poses.Any(s=>s.Id==line.poseId),line.id);Assert.IsTrue(p.Faces.Any(s=>s.Id==line.faceId),line.id);}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 実装済み全画面の新ひなたとロゴを撮影し操作と数値を保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);
            var game=Object.FindAnyObjectByType<OpsGame>();Assert.AreSame(game.PlanningArt.titleKeyVisual,Find<Image>("TitleKeyVisual").sprite);
            CheckRect("TitleLogoWordmark",50,0,640,246);Assert.AreEqual(1,Find<RectTransform>("TitleBrand").localScale.x);Capture("90-v2-title");yield return null;CheckPointer("NewYear");CheckPointer("HomeSettings");
            game.StartYear(14);yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreSame(game.Navigator.Pose(game.ActiveVoiceBank.Find("season_04").poseId),Find<Image>("NavigatorPortrait").sprite);Assert.IsNotNull(Find<Image>("PlanningLogoIcon").sprite);
            Assert.AreEqual("OfficeStage",Find<Transform>("PlanningCharacter").parent.name);CheckNewPortraits();CheckText();Capture("91-v2-planning");
            string state=JsonUtility.ToJson(game.State);Assert.IsTrue(game.StartTutorial());yield return new WaitForSecondsRealtime(2);
            Assert.AreSame(game.Navigator.Pose(game.ActiveVoiceBank.Find("tutorial_1").poseId),Find<Image>("TutorialPortrait").sprite);CheckRect("TutorialSpeech",660,380,560,280);Capture("92-v2-tutorial");game.SkipTutorial();Assert.AreEqual(state,JsonUtility.ToJson(game.State));
            Click("Menu");yield return new WaitForSecondsRealtime(.6f);Assert.IsNotNull(Find<Image>("WindowLogoIcon").sprite);Capture("97-v2-settings");Click("CloseDialog");yield return new WaitForSecondsRealtime(.25f);
            game.BeginIncident();yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreSame(game.Navigator.Pose("pose_startled"),Find<Image>("NavigatorPortrait").sprite);CheckNewPortraits();CheckText();Capture("93-v2-incident");
            game.Resolve("scope");yield return new WaitForSecondsRealtime(.5f);CheckNewPortraits();Capture("94-v2-resolution");yield return WaitForResolution(game);
            yield return new WaitForSecondsRealtime(1);CheckNewPortraits();CheckText();Assert.IsNotNull(Find<Image>("MonthlyLogoIcon").sprite);Capture("95-v2-monthly");
            for(int i=1;i<12;i++){game.Next();if(game.State.phase==OpsPhase.Ended)break;Plan(game.State);game.OpenTab(0);game.BeginIncident();game.Resolve("scope");yield return null;}
            game.Next();yield return new WaitForSecondsRealtime(1.6f);Assert.AreEqual(OpsPhase.Ended,game.State.phase);
            CheckNewPortraits();CheckText();Assert.IsNotNull(Find<Image>("AnnualLogoWordmark").sprite);Capture("96-v2-annual");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 旧版のタイトルと通常プレイにも新ひなただけを使う()
        {
            SceneManager.LoadScene("SampleScene");yield return new WaitForSecondsRealtime(.8f);
            var image=Find<Image>("NavigatorPortrait");Assert.IsNotNull(image.sprite);StringAssert.StartsWith("pose_",image.sprite.name);Capture("98-v2-legacy-title",1600,900,"Canvas");
            Click("QuickStartButton");yield return new WaitForSecondsRealtime(.8f);StringAssert.StartsWith("pose_",image.sprite.name);Capture("99-v2-legacy-play",1600,900,"Canvas");
            Assert.IsFalse(Object.FindObjectsByType<Image>().Any(i=>i.sprite!=null&&i.sprite.name=="hinata_normal"));LogAssert.NoUnexpectedReceived();
        }
        private static void CheckNewPortraits()
        {
            foreach(var identity in Object.FindObjectsByType<OpsPortraitIdentity>())
            {
                var sprite=identity.GetComponent<Image>().sprite;Assert.IsNotNull(sprite,identity.name);
                StringAssert.StartsWith(identity.FaceIcon?"face_":"pose_",sprite.name,identity.name);
            }
        }
    }
}
#endif
