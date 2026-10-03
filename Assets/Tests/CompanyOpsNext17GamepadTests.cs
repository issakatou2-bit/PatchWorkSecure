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
            public PadFixture(){Setup();Pad=InputSystem.AddDevice<Gamepad>();}
            public void Dispose(){TearDown();}
            public IEnumerator Tap(ButtonControl button)
            {Press(button,queueEventOnly:true);yield return null;Release(button,queueEventOnly:true);yield return null;}
        }
        private static readonly Vector2[] PadDirections={Vector2.up,Vector2.down,Vector2.left,Vector2.right};
        // 経路は実際のボタン位置から求める。選択を書き換えず十字キーだけを入力する。
        private static IEnumerator PadTo(OpsGame game,PadFixture input,string name)
        {
            for(int retry=0;retry<90;retry++)
            {
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
        private static IEnumerator PadClick(OpsGame game,PadFixture input,string name)
        {yield return PadTo(game,input,name);yield return input.Tap(input.Pad.buttonSouth);}
        private static void Next17Shots(string name)
        {foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1920,1080)})Next15Shot(name,size.x,size.y,"Next17");}

        [UnityTest] public IEnumerator Next17Menus_仮想ゲームパッドで設定と日記と計画を選ぶ()
        {
            using(var input=new PadFixture())
            {
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(2);var game=Object.FindAnyObjectByType<OpsGame>();
                yield return input.Tap(input.Pad.dpad.up);Assert.AreEqual("NewYear",EventSystem.current.currentSelectedGameObject.name);
                yield return PadTo(game,input,"SingleYear");yield return new WaitForSecondsRealtime(.2f);Next17Shots("title-selected");
                yield return input.Tap(input.Pad.startButton);yield return new WaitForSecondsRealtime(.4f);
                Assert.IsNotNull(Find<Button>("CloseDialog"));yield return PadTo(game,input,"MusicVolume");
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
    }
}
#endif
