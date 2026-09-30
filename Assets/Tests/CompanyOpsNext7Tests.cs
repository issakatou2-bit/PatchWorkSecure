using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next7Detail_帯は語句単位で道具のポーズを保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(9);game.State.levels[OpsCatalog.Index("education")]=1;
            game.OpenMailTraining();Click("MinigameStart");
            var mail=(OpsMailMinigame)game.Minigame;
            for(int i=0;i<10&&mail.Current.Id!="account";i++){game.AnswerMail(mail.Current.Suspicious);game.TickMinigame(.6f);}
            Assert.AreEqual("account",mail.Current.Id);yield return new WaitForSecondsRealtime(.4f);
            var label=Find<TextMeshProUGUI>("MailBodyText");StringAssert.DoesNotContain("<mark",label.text);
            var marker=Find<OpsPhraseMarker>("MailBodyTextMarker");marker.RebuildBands();
            Assert.AreEqual(label.textInfo.linkCount,marker.Bands.Count);Assert.Greater(marker.Bands.Count,0);
            foreach(var band in marker.Bands){Assert.Greater(band.width,label.fontSize);Assert.Greater(band.height,0);}
            Assert.IsFalse(marker.raycastTarget);Assert.Less(marker.transform.GetSiblingIndex(),label.transform.GetSiblingIndex());
            Assert.AreEqual("pose_magnifier",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);
            yield return null;Canvas.ForceUpdateCanvases();
            var mesh=marker.canvasRenderer.GetMesh();Assert.IsNotNull(mesh);Assert.Greater(mesh.vertexCount,0,"帯の描画メッシュ");
            Capture("next7-c-play-hint");
            label.text="<b><link=\"mail-hint\">振込先口座が変更されました</link></b>";
            label.rectTransform.sizeDelta=new Vector2(110,100);yield return null;marker.RebuildBands();
            Assert.Greater(marker.Bands.Count,1);Assert.AreEqual(label.textInfo.lineCount,marker.Bands.Count);
            var fixture=DecisionFixture("remote",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);
            game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");
            yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual("pose_laptop",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);
            var mfa=(OpsMfaMinigame)game.Minigame;game.AnswerMfa(!mfa.Current.Legitimate);yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual("pose_laptop",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);Capture("next7-d-play");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
