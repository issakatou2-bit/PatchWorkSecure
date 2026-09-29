using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static int PolishBubbleSeed=>Enumerable.Range(0,10000).First(s=>{var a=new OpsState(s,true).bubbleSchedule.Take(4);return a.Contains(6)&&a.Contains(7);});
        private static void PolishCapture(int item,string suffix="after")
        {
            string name="polish4-"+item.ToString("00")+"-"+suffix;Capture(name);
            Directory.CreateDirectory("Artifacts/Polish4");File.Copy("Artifacts/CompanyOps/"+name+".png","Artifacts/Polish4/"+item.ToString("00")+"-"+suffix+"-unity.png",true);
        }
        private IEnumerator PolishPlanning()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(PolishBubbleSeed);yield return new WaitForSecondsRealtime(3);
        }
        [UnityTest] public IEnumerator Polish4_01_泡は透過素材と輪を重ね操作範囲を保つ()
        {
            yield return PolishPlanning();var game=Object.FindAnyObjectByType<OpsGame>();
            for(int i=0;i<4;i++)
            {
                var b=Find<RectTransform>("OfficeBubble"+i);int kind=game.State.BubbleKind(i);
                var glass=b.Find("BubbleGlass").GetComponent<Image>();Assert.AreSame(kind==7?game.PlanningArt.bubbleRare:kind==6?game.PlanningArt.bubbleConsult:game.PlanningArt.bubbleNormal,glass.sprite);
                Assert.AreEqual(new Vector2(200,200),glass.sprite.rect.size);Assert.AreEqual(new Vector2(100,100),glass.rectTransform.sizeDelta);
                Assert.IsFalse(glass.raycastTarget);Assert.AreEqual(70,b.rect.width);CheckPointer(b.name);
                Assert.IsNull(glass.GetComponent<OpsIncidentGraphic>());Assert.IsNotNull(b.Find("BubbleIcon"));
            }
            var ring=Find<Image>("BubbleRareRing");Assert.AreSame(game.PlanningArt.bubbleRareRing,ring.sprite);Assert.AreEqual("rotate",ring.GetComponent<OpsPlanningMotion>().Kind);
            string before=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(before,JsonUtility.ToJson(game.State));PolishCapture(1);LogAssert.NoUnexpectedReceived();
        }
    }
}
