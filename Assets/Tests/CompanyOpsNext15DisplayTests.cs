#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
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
        private static readonly Vector2Int[] Next15Sizes={new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(3440,1440)};
        [Category("Capture")]
        [UnityTest] public IEnumerator Next15Display_画面設定を保存し年度と既存設定を保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();
            Assert.IsTrue(game.FullscreenEnabled);game.StartYear(14);while(game.PhasePresentationRunning)yield return null;
            string state=JsonUtility.ToJson(game.State);bool had=PlayerPrefs.HasKey(OpsGame.FullscreenKey);int previous=PlayerPrefs.GetInt(OpsGame.FullscreenKey);
            var fixture=new TestPreferenceScope();fixture.Snapshot();
            try
            {
                OpsGame.TestMode=false;game.SetFullscreen(false);Assert.AreEqual(0,PlayerPrefs.GetInt(OpsGame.FullscreenKey));
                typeof(OpsGame).GetProperty("FullscreenEnabled").SetValue(game,true);
                typeof(OpsGame).GetMethod("LoadDisplaySettings",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);Assert.IsFalse(game.FullscreenEnabled);
                game.SetFullscreen(true);Assert.AreEqual(1,PlayerPrefs.GetInt(OpsGame.FullscreenKey));
                PlayerPrefs.DeleteKey(OpsGame.FullscreenKey);typeof(OpsGame).GetMethod("LoadDisplaySettings",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);Assert.IsTrue(game.FullscreenEnabled);
            }
            finally{OpsGame.TestMode=true;fixture.Restore();}
            Assert.AreEqual(had,PlayerPrefs.HasKey(OpsGame.FullscreenKey));Assert.AreEqual(previous,PlayerPrefs.GetInt(OpsGame.FullscreenKey));
            Assert.AreEqual(state,JsonUtility.ToJson(game.State));Click("Menu");yield return new WaitForSecondsRealtime(.5f);
            CheckPointer("ScreenFullscreen");CheckPointer("ScreenWindowed");Click("ScreenWindowed");Assert.IsFalse(game.FullscreenEnabled);yield return new WaitForSecondsRealtime(.5f);
            Capture("next15-settings-windowed");Click("ScreenFullscreen");Assert.IsTrue(game.FullscreenEnabled);yield return new WaitForSecondsRealtime(.5f);Capture("next15-settings-fullscreen");
            Assert.AreEqual(state,JsonUtility.ToJson(game.State));Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }

        [Category("Capture")]
        [UnityTest] public IEnumerator Next15DisplayUI_十三画面を五つの実寸で撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(3);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            Next15Shots(game,"01-title");
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(3,6)}));yield return new WaitForSecondsRealtime(3);game.StopVoice();Next15Shots(game,"02-planning");
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(2,8)}));yield return new WaitForSecondsRealtime(2);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            yield return new WaitForSecondsRealtime(.5f);game.StopVoice();Next15Shots(game,"03-incident");
            game.State.Resolve("recover",50,true);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);game.FinishReportCounts();Next15Shots(game,"10-report");
            game.OpenMonthlyDiary();yield return new WaitForSecondsRealtime(3);Next15Shots(game,"12-diary");Click("DiaryClose");yield return null;
            var annual=new OpsState(14,true);FinishStoryTestYear(annual);Assert.IsTrue(game.RestoreProgress(new OpsProgress{single=annual}));yield return new WaitForSecondsRealtime(3);Next15Shots(game,"11-annual");
            string[] names={"04-b-containment","05-c-mail","06-d-mfa","07-e-logs","08-f2-blocks","09-g-restore"};
            for(int i=0;i<names.Length;i++)
            {
                Assert.IsTrue(game.BeginDailyPractice(OpsDailyPractice.Ids[i],20261002));game.StartMinigame();yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual(OpsMinigamePhase.Playing,game.Minigame.Phase);Next15Shots(game,names[i]);
                FinishDailyForTest(game.Minigame);game.TickMinigame(0);game.ConfirmMinigame();yield return null;
            }
            game.BuildPreview();yield return new WaitForSecondsRealtime(1);Click("HomeSettings");yield return new WaitForSecondsRealtime(.5f);Next15Shots(game,"13-settings");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        private static void Next15Shots(OpsGame game,string screen)
        {
            string state=JsonUtility.ToJson(game.State);bool enabled=game.enabled;game.enabled=false;
            try{foreach(var size in Next15Sizes)Next15Shot(screen,size.x,size.y);}
            finally{game.enabled=enabled;}
            Assert.AreEqual(state,JsonUtility.ToJson(game.State),"撮影で年度の数値を変更しない");
        }
        private static void Next15Shot(string screen,int width,int height,string artifactFolder="Next15")
        {
            var canvas=Find<Canvas>("CompanyOpsCanvas");var originalMode=canvas.renderMode;var originalCamera=canvas.worldCamera;float originalDistance=canvas.planeDistance;
            var cameraObject=new GameObject("画面サイズ検証用カメラ",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Camera.main.backgroundColor;
            var target=new RenderTexture(width,height,24);var old=RenderTexture.active;Texture2D texture=null;
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();foreach(var text in canvas.GetComponentsInChildren<TextMeshProUGUI>())text.ForceMeshUpdate();
                camera.Render();Canvas.ForceUpdateCanvases();
                var scaler=canvas.GetComponent<CanvasScaler>();Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand,scaler.screenMatchMode);
                Assert.AreEqual(Mathf.Min(width/1600f,height/900f),canvas.scaleFactor,.002f,"画面全体を余白つきで収める");
                foreach(var button in canvas.GetComponentsInChildren<Selectable>())
                {
                    if(!button.gameObject.activeInHierarchy)continue;var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach(var corner in corners)
                    {
                        var p=camera.WorldToScreenPoint(corner);Assert.That(p.x,Is.InRange(-3f,width+3f),screen+" / "+button.name);Assert.That(p.y,Is.InRange(-3f,height+3f),screen+" / "+button.name);
                    }
                }
                CheckText();foreach(var label in canvas.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.name.StartsWith("ScreenMode")||t.name=="ScreenFullscreenLabel"||t.name=="ScreenWindowedLabel"))Assert.IsFalse(label.isTextOverflowing,label.name);
                RenderTexture.active=target;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                string folder=Path.Combine(Application.dataPath,"../Artifacts/"+artifactFolder+"/"+width+"x"+height);Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,screen+".png"),texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=originalMode;canvas.worldCamera=originalCamera;canvas.planeDistance=originalDistance;camera.targetTexture=null;RenderTexture.active=old;
                target.Release();if(texture!=null)Object.DestroyImmediate(texture);Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);Canvas.ForceUpdateCanvases();
            }
        }
    }
}
#endif
