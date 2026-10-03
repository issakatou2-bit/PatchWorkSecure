#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private sealed class PadFixture:InputTestFixture,IDisposable
        {
            public Gamepad Pad;
            private readonly int? priorSeed=OpsGame.TestRunSeed;
            public PadFixture(){DisconnectInputUI();Setup();Pad=InputSystem.AddDevice<Gamepad>();OpsGame.TestRunSeed=14;}
            public void Dispose(){DisconnectInputUI();OpsGame.TestRunSeed=priorSeed;TearDown();}
            private static void DisconnectInputUI()
            {
                // InputTestFixtureの別InputSystemへ、前の場面のデバイス参照を持ち込まない。
                // 復帰前にもUIを破棄し、仮想デバイスの参照が次のテストへ残らないようにする。
                var assets=new HashSet<InputActionAsset>();
                var modules=Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include);
                foreach(var module in modules){if(module.actionsAsset!=null)assets.Add(module.actionsAsset);module.enabled=false;}
                foreach(var game in Object.FindObjectsByType<OpsGame>(FindObjectsInactive.Include))
                {
                    foreach(string field in new[]{"padOriginalActions","padUiActions"})
                    {var asset=(InputActionAsset)typeof(OpsGame).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);if(asset!=null)assets.Add(asset);}
                    // 専用フォントを解放する前に、それを描くCanvasも閉じる。
                    var canvas=game.Surface!=null?game.Surface.GetComponentInParent<Canvas>():null;
                    if(canvas!=null)Object.DestroyImmediate(canvas.gameObject);
                    Object.DestroyImmediate(game.gameObject);
                }
                foreach(var asset in assets)if(asset!=null)foreach(var map in asset.actionMaps)map.Dispose();
                foreach(var module in modules)if(module!=null)Object.DestroyImmediate(module.gameObject);
            }
            public IEnumerator Tap(ButtonControl button)
            {Press(button,queueEventOnly:true);yield return null;Release(button,queueEventOnly:true);yield return null;}
            public IEnumerator Stick(Vector2 value)
            {Set(Pad.leftStick,value,queueEventOnly:true);yield return null;Set(Pad.leftStick,Vector2.zero,queueEventOnly:true);yield return null;}
        }
        private static readonly Vector2[] PadDirections={Vector2.up,Vector2.down,Vector2.left,Vector2.right};
        // 経路は実際のボタン位置から求める。選択を書き換えず十字キーだけを入力する。
        private static IEnumerator PadTo(OpsGame game,PadFixture input,string name,Func<bool> stillApplicable=null)
        {
            for(int retry=0;retry<90;retry++)
            {
                if(stillApplicable!=null&&!stillApplicable())yield break;
                var choices=(Selectable[])typeof(OpsGame).GetMethod("GamepadChoices",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
                var target=choices.FirstOrDefault(s=>s.name==name);Assert.IsNotNull(target,"操作可能な対象がない: "+name);
                var selected=EventSystem.current.currentSelectedGameObject;
                if(selected==target.gameObject)yield break;
                if(selected==null){yield return input.Tap(input.Pad.dpad.down);continue;}
                var start=selected.GetComponent<Selectable>();var queue=new Queue<Selectable>();queue.Enqueue(start);
                var reached=new HashSet<Selectable>{start};var first=new Dictionary<Selectable,int>();
                while(queue.Count>0&&!reached.Contains(target))
                {
                    var current=queue.Dequeue();Vector2 origin=PadCenter(current);
                    for(int d=0;d<4;d++)
                    {
                        if(current is Slider&&d>=2)continue;
                        var next=choices.Where(s=>s!=current&&Vector2.Dot(PadCenter(s)-origin,PadDirections[d])>1)
                            .OrderBy(s=>{var v=PadCenter(s)-origin;return v.magnitude+2*Mathf.Abs(v.x*PadDirections[d].y-v.y*PadDirections[d].x);}).FirstOrDefault();
                        if(next==null||!reached.Add(next))continue;
                        first[next]=current==start?d:first[current];queue.Enqueue(next);
                    }
                }
                Assert.IsTrue(reached.Contains(target),"方向操作で到達できない: "+start.name+" → "+name);
                int direction=first[target];yield return input.Tap(direction==0?input.Pad.dpad.up:direction==1?input.Pad.dpad.down:direction==2?input.Pad.dpad.left:input.Pad.dpad.right);
            }
            Assert.Fail("選択に時間がかかりすぎた: "+name);
        }
        private static Vector2 PadCenter(Selectable s)=>((RectTransform)s.transform).TransformPoint(((RectTransform)s.transform).rect.center);
        private static IEnumerator PadClick(OpsGame game,PadFixture input,string name,Func<bool> stillApplicable=null)
        {
            while(game.PhasePresentationRunning||Time.unscaledTime<(float)typeof(OpsGame).GetField("presentationInputGuardUntil",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game))
            {if(stillApplicable!=null&&!stillApplicable())yield break;yield return null;}
            yield return PadTo(game,input,name,stillApplicable);
            if(stillApplicable!=null&&!stillApplicable())yield break;
            yield return input.Tap(input.Pad.buttonSouth);
        }
        private static void Next17Shots(string name)
        {foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1920,1080)})Next15Shot(name,size.x,size.y,"Next17");}

        [Category("Capture")]
        [UnityTest] public IEnumerator Next17Menus_仮想ゲームパッドで設定と日記と計画を選ぶ()
        {
            using(var input=new PadFixture())
            {
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(2);var game=Object.FindAnyObjectByType<OpsGame>();
                yield return input.Tap(input.Pad.dpad.up);Assert.AreEqual("NewYear",EventSystem.current.currentSelectedGameObject.name);
                yield return PadTo(game,input,"SingleYear");yield return new WaitForSecondsRealtime(.2f);Next17Shots("title-selected");
                yield return input.Tap(input.Pad.startButton);yield return new WaitForSecondsRealtime(.4f);
                Assert.IsNotNull(Find<Button>("CloseDialog"));yield return PadTo(game,input,"MusicVolume");
                var musicRim=Find<Slider>("MusicVolume").handleRect.GetComponent<Outline>();
                Assert.AreEqual(new Color(.44f,.71f,1f,.85f),musicRim.effectColor,"既存のつまみの枠で選択先を示す");
                yield return PadTo(game,input,"SoundVolume");
                Assert.AreNotEqual(new Color(.44f,.71f,1f,.85f),musicRim.effectColor,"選択を離れたつまみの枠は元に戻る");
                yield return PadTo(game,input,"MusicVolume");
                float before=Find<Slider>("MusicVolume").value;yield return input.Tap(input.Pad.dpad.left);Assert.Less(Find<Slider>("MusicVolume").value,before);
                Next17Shots("settings-selected");yield return input.Tap(input.Pad.buttonEast);yield return new WaitForSecondsRealtime(.4f);
                yield return PadClick(game,input,"HomeDiary");yield return new WaitForSecondsRealtime(.5f);Assert.IsTrue(game.DiaryActive);
                yield return input.Tap(input.Pad.buttonEast);yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(game.DiaryActive);
                yield return PadClick(game,input,"SingleYear");yield return new WaitForSecondsRealtime(.8f);
                if(game.TutorialActive)yield return PadClick(game,input,"SkipTutorial");while(game.PhasePresentationRunning)yield return null;
                yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual(OpsPhase.Planning,game.State.phase);
                Assert.AreEqual("Action_audit",EventSystem.current.currentSelectedGameObject.name);Next17Shots("planning-selected");
                string state=JsonUtility.ToJson(game.State);yield return input.Tap(input.Pad.startButton);yield return new WaitForSecondsRealtime(.3f);
                yield return PadClick(game,input,"CaptionToggle");yield return new WaitForSecondsRealtime(.3f);
                yield return input.Tap(input.Pad.buttonEast);yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(state,JsonUtility.ToJson(game.State));
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
        }
        [UnityTest] public IEnumerator Next17Details_左スティックと日記送りと回転置き直しと持ち替えを確認する()
        {
            // 実デバイスでUIが既に動いた場面からも、仮想入力へ安全に移行できる。
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);
            using(var input=new PadFixture())
            {
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);var game=Object.FindAnyObjectByType<OpsGame>();
                yield return input.Stick(Vector2.down);Assert.IsNotNull(EventSystem.current.currentSelectedGameObject);
                yield return PadTo(game,input,"SingleYear");
                var keyboard=InputSystem.AddDevice<Keyboard>();
                // 仮想デバイスの追加後にUIアクションの接続を1フレーム進める。
                yield return null;
                while(game.PhasePresentationRunning||Time.unscaledTime<(float)typeof(OpsGame).GetField("presentationInputGuardUntil",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game))yield return null;
                Assert.AreEqual("SingleYear",EventSystem.current.currentSelectedGameObject.name);
                yield return input.Tap(keyboard.enterKey);yield return new WaitForSecondsRealtime(.5f);
                Assert.IsNotNull(game.State,"キーボードへの持ち替えでも決定できる");Assert.AreEqual(14,game.State.seed);
                while(game.PhasePresentationRunning)yield return null;yield return new WaitForSecondsRealtime(.4f);Canvas.ForceUpdateCanvases();CheckPointer("Menu");
                var mouse=InputSystem.AddDevice<Mouse>();var menu=Find<Button>("Menu").GetComponent<RectTransform>();
                input.Set(mouse.position,RectTransformUtility.WorldToScreenPoint(null,menu.TransformPoint(menu.rect.center)),queueEventOnly:true);yield return null;
                yield return input.Tap(mouse.leftButton);Assert.IsTrue(PadButtonExists("CloseDialog"),"マウスへの持ち替えも維持");
                yield return PadClick(game,input,"ReplayTutorial");Assert.IsTrue(game.TutorialActive);
                yield return PadClick(game,input,"Stat_0");yield return PadClick(game,input,"CloseDialog");
                yield return PadClick(game,input,"SkipTutorial");Assert.IsFalse(game.TutorialActive);
                var before=JsonUtility.ToJson(game.State);yield return input.Tap(input.Pad.startButton);yield return PadClick(game,input,"SpeedFast");yield return input.Tap(input.Pad.buttonEast);
                Assert.AreEqual(before,JsonUtility.ToJson(game.State));
                Assert.IsTrue(game.BeginDailyPractice("F2",20261003));yield return PadClick(game,input,"MinigameStart");
                var block=(OpsBlockMinigame)game.Minigame;var task=block.Tasks.First(t=>t.Placed==null&&t.Cells.Length>1);
                yield return PadClick(game,input,"BlockTask_"+task.Id);string shape=string.Join(";",task.Cells.Select(c=>c.Row+","+c.Column));
                yield return input.Tap(input.Pad.buttonWest);Assert.AreNotEqual(shape,string.Join(";",task.Cells.Select(c=>c.Row+","+c.Column)));
                for(int i=0;i<3;i++)yield return input.Tap(input.Pad.buttonWest);Assert.AreEqual(shape,string.Join(";",task.Cells.Select(c=>c.Row+","+c.Column)));
                yield return PadClick(game,input,"BlockCell_"+task.Solution.Min(c=>c.Row)+"_"+task.Solution.Min(c=>c.Column));Assert.IsNotNull(task.Placed);
                yield return PadClick(game,input,"BlockTask_"+task.Id);Assert.IsNull(task.Placed,"置いた作業を持ち上げて移せる");
                yield return input.Tap(input.Pad.buttonEast);Assert.AreEqual("BlockTask_"+task.Id,EventSystem.current.currentSelectedGameObject.name);
                yield return PadClick(game,input,"BlockFinish");yield return new WaitForSecondsRealtime(1.3f);yield return PadClick(game,input,"MinigameContinue");
                // 二つの既読ページを用意するのはテストの前提。送りはLB/RBだけで操作。
                game.Career.diary.Add(new OpsDiaryRecord{key=0,content=0,mood=0,recap="確認",thought="確認",rank="B",season="春"});
                game.Career.diary.Add(new OpsDiaryRecord{key=1,content=1,mood=0,recap="確認",thought="確認",rank="B",season="春"});
                yield return PadClick(game,input,"RecordsClose");yield return PadClick(game,input,"HomeDiary");yield return PadClick(game,input,"DiaryPage_0");
                yield return input.Tap(input.Pad.rightShoulder);StringAssert.Contains("5月",Find<TMPro.TextMeshProUGUI>("DiaryDate").text);
                yield return input.Tap(input.Pad.leftShoulder);StringAssert.Contains("4月",Find<TMPro.TextMeshProUGUI>("DiaryDate").text);
                yield return input.Tap(input.Pad.buttonEast);Assert.IsTrue(PadButtonExists("DiaryPage_0"));
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            // 同じPlayMode実行の中で実入力へ戻り、次の場面も参照が混ざらず動く。
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);
            var nativeModule=EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.IsTrue(nativeModule.enabled);Assert.IsTrue(nativeModule.actionsAsset.FindAction("UI/Submit").enabled);
            Click("HomeSettings");yield return new WaitForSecondsRealtime(.3f);CheckPointer("CloseDialog");Click("CloseDialog");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
