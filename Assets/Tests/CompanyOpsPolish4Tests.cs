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
        private IEnumerator PolishIncident()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);game.BeginIncident();yield return new WaitForSecondsRealtime(1.4f);
        }
        private IEnumerator PolishTitle()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1.5f);Object.FindAnyObjectByType<OpsGame>().UseLocalTestVoices=false;
        }
        [UnityTest] public IEnumerator Polish4_16_四つのタイトルボタンは六ピクセルの厚みがある()
        {
            yield return PolishTitle();
            foreach(string name in new[]{"NewYear","ContinueYear","HomeGuide","HomeSettings"})
            {
                var body=Find<RectTransform>(name);var depth=Find<Image>(name+"Depth");Assert.AreEqual(body.sizeDelta,depth.rectTransform.sizeDelta);
                Assert.AreEqual(body.anchoredPosition+Vector2.down*6,depth.rectTransform.anchoredPosition);Assert.Less(depth.transform.GetSiblingIndex(),body.GetSiblingIndex());Assert.IsFalse(depth.raycastTarget);
                ColorUtility.TryParseHtmlString(name=="NewYear"?"#d94a70":"#c7d0e0",out var color);Assert.AreEqual(color,depth.color);
            }
            CheckPointer("NewYear");CheckPointer("HomeGuide");CheckPointer("HomeSettings");PolishCapture(16);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_15_タイトル文字は白三ピクセルと薄ピンク六ピクセルの輪郭になる()
        {
            yield return PolishTitle();var title=Find<RectTransform>("Title");
            foreach(string layer in new[]{"TitleWhiteShadow","TitlePinkShadow"})
            {
                int offset=layer=="TitleWhiteShadow"?3:6;var edge=Find<TextMeshProUGUI>(layer);
                Assert.AreEqual(new Vector2(offset,-offset),edge.rectTransform.anchoredPosition-title.anchoredPosition);Assert.AreEqual("情シスの一年",edge.text);Assert.IsFalse(edge.raycastTarget);
            }
            ColorUtility.TryParseHtmlString("#ffc4d3",out var pink);Assert.AreEqual(pink,Find<TextMeshProUGUI>("TitlePinkShadow").color);PolishCapture(15);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_14_副題帯は単色ピンクの斜め切り素材になる()
        {
            yield return PolishTitle();var game=Object.FindAnyObjectByType<OpsGame>();var ribbon=Find<Image>("TitleRibbon");Assert.AreSame(game.PlanningArt.ribbonSlant,ribbon.sprite);Assert.IsNotNull(ribbon.sprite);
            Assert.IsNull(ribbon.GetComponent<OpsIncidentGraphic>());ColorUtility.TryParseHtmlString("#ff6f91",out var pink);Assert.AreEqual(pink,ribbon.color);Assert.AreEqual(new Vector2(570,44),ribbon.rectTransform.sizeDelta);
            Assert.IsFalse(Find<TextMeshProUGUI>("TitleSubtitle").isTextOverflowing);PolishCapture(14);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_13_ロゴの最終角度はCSSのマイナス二度と一致する()
        {
            yield return PolishTitle();var brand=Find<RectTransform>("TitleBrand");Assert.AreEqual(2,Mathf.DeltaAngle(0,brand.localEulerAngles.z),.01f);
            Assert.AreEqual(new Vector2(0,.5f),brand.pivot);Assert.AreEqual(.84f,brand.localScale.x);Assert.IsFalse(Find<OpsUIReveal>("TitleLogoWordmark").enabled);
            PolishCapture(13);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_12_把握と時間帯は左上の赤枠外にまとまる()
        {
            yield return PolishIncident();var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(int timeValue in new[]{0,1})
            {
                game.State.incidentTimes[game.State.month]=timeValue;game.OpenTab(0);yield return new WaitForSecondsRealtime(.3f);
                var alarm=Find<RectTransform>("AffectedRoom");var time=Find<RectTransform>("IncidentTimeBadge");var know=Find<RectTransform>("KnowledgeCard");
                foreach(var r in new[]{time,know})Assert.Less(r.anchoredPosition.x+r.rect.width,alarm.anchoredPosition.x);
                Assert.AreEqual(time.anchoredPosition.x,know.anchoredPosition.x);Assert.AreEqual(time.rect.width,know.rect.width);
                Assert.AreEqual(game.State.IncidentTimeLabel,Find<TextMeshProUGUI>("IncidentTimeLabel").text);CheckPointer("KnowledgeCard");CheckText();
                PolishCapture(12,timeValue==0?"after":"quiet");
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_11_備えなしの二行だけ消しカードの高さを保つ()
        {
            yield return PolishIncident();var game=Object.FindAnyObjectByType<OpsGame>();
            Assert.IsFalse(game.Surface.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name.StartsWith("WorkingHeading_")||t.name.StartsWith("WorkingNone_")));
            foreach(string id in new[]{"contain","scope","recover"})Assert.AreEqual(576,Find<RectTransform>("ResponseCard_"+id).rect.height);
            PolishCapture(11);game.State.levels[OpsCatalog.Index("backup")]=1;game.OpenTab(0);yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(game.Surface.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name.StartsWith("WorkingHeading_")));
            Assert.IsFalse(game.Surface.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name.StartsWith("WorkingNone_")));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_10_数字と小さい単位は同じ行で隙間なくつながる()
        {
            yield return PolishIncident();var game=Object.FindAnyObjectByType<OpsGame>();string before=JsonUtility.ToJson(game.State);
            foreach(int size in new[]{0,2})
            {
                Click("Menu");Click("TextSize"+size);Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
                var metrics=game.Surface.GetComponentsInChildren<OpsInlineMetric>();Assert.AreEqual(9,metrics.Length);
                foreach(var flow in metrics)
                {
                    Assert.AreEqual(flow.Number.rectTransform.anchoredPosition.y,flow.Unit.rectTransform.anchoredPosition.y);
                    Assert.AreEqual(flow.Number.rectTransform.anchoredPosition.x+flow.Number.rectTransform.rect.width,flow.Unit.rectTransform.anchoredPosition.x,.01f);
                    Assert.Less(flow.Unit.fontSize,flow.Number.fontSize);Assert.IsFalse(flow.Unit.isTextOverflowing);
                }
                if(size==0)PolishCapture(10);
            }
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_09_備えは未導入点線と導入済み緑と社員支援青を区別する()
        {
            yield return PolishIncident();var game=Object.FindAnyObjectByType<OpsGame>();
            Assert.IsNotEmpty(game.Surface.GetComponentsInChildren<OpsIncidentGraphic>().Where(g=>g.name.StartsWith("MissingFrame_")));
            Assert.AreEqual(Color.white,Find<Button>("OpenTeam").GetComponentInChildren<TextMeshProUGUI>().color);
            ColorUtility.TryParseHtmlString("#3fa9f5",out var blue);Assert.AreEqual(blue,Find<Button>("OpenTeam").GetComponent<OpsKitGradient>().Bottom);PolishCapture(9);
            game.State.levels[OpsCatalog.Index("backup")]=1;game.State.staffExperience[1]=OpsGrowthCatalog.StaffThresholds[1];game.State.supportOrder="investigate";game.OpenTab(0);yield return new WaitForSecondsRealtime(.5f);
            var installed=game.Surface.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Ready_")).ToArray();Assert.IsNotEmpty(installed);
            foreach(var tag in installed){ColorUtility.TryParseHtmlString("#2ec4a0",out var green);Assert.AreEqual(green,tag.color);Assert.AreEqual(Color.white,tag.GetComponentInChildren<TextMeshProUGUI>().color);}
            Assert.IsNotNull(Find<Image>("SupportFace"));PolishCapture(9,"equipped");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_08_未確認札は黄色地に紺文字で表示する()
        {
            yield return PolishIncident();var button=Find<Button>("IncidentEvidence");var text=button.GetComponentInChildren<TextMeshProUGUI>();
            ColorUtility.TryParseHtmlString("#1d2a44",out var ink);Assert.AreEqual(ink,text.color);Assert.IsFalse(button.transform.Find("KitTopLight").gameObject.activeSelf);
            var gradient=button.GetComponent<OpsKitGradient>();Assert.AreEqual(gradient.Top,gradient.Bottom);CheckPointer("IncidentEvidence");PolishCapture(8);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_07_攻撃種別は題名の直後のピンク札になる()
        {
            yield return PolishIncident();var topic=Find<RectTransform>("IncidentTopic");var title=Find<TextMeshProUGUI>("IncidentTitle");
            Assert.AreEqual(100+Mathf.Min(506,title.GetPreferredValues(title.text).x)+16,topic.anchoredPosition.x,.1f);
            var gradient=topic.GetComponent<OpsKitGradient>();Assert.AreEqual(gradient.Top,gradient.Bottom);Assert.AreEqual(ColorUtility.TryParseHtmlString("#ffe3ec",out var pink)?pink:Color.clear,gradient.Top);
            CheckPointer("IncidentTopic");PolishCapture(7);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_06_工具マークはひなたの名札より上に離れる()
        {
            yield return PolishPlanning();Canvas.ForceUpdateCanvases();var pin=Find<RectTransform>("Pin_change");var tag=(RectTransform)Find<TextMeshProUGUI>("NavigatorName").transform.parent;
            var a=new Vector3[4];var b=new Vector3[4];pin.GetWorldCorners(a);tag.GetWorldCorners(b);
            var surface=Object.FindAnyObjectByType<OpsGame>().Surface;Assert.Greater(surface.InverseTransformPoint(a[0]).y,surface.InverseTransformPoint(b[1]).y+10);
            CheckPointer("Pin_change");PolishCapture(6);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_05_時間帯は困りごとと同じ高さでマップ内に収まる()
        {
            yield return PolishPlanning();var time=Find<RectTransform>("PlanningTimeBadge");var counter=Find<RectTransform>("BubbleCounter");
            Assert.AreEqual(counter.anchoredPosition.y,time.anchoredPosition.y);Assert.AreEqual(counter.rect.height,time.rect.height);
            Assert.Greater(time.anchoredPosition.x,counter.anchoredPosition.x+counter.rect.width);Assert.GreaterOrEqual(-time.anchoredPosition.y,0);
            Assert.Less(time.anchoredPosition.x+time.rect.width,Find<RectTransform>("OfficeStage").rect.width);PolishCapture(5);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_04_未導入枠は一ピクセル半透明で部屋の隅に収まる()
        {
            yield return PolishPlanning();var game=Object.FindAnyObjectByType<OpsGame>();game.OpenTab(1);yield return new WaitForSecondsRealtime(.3f);
            Assert.IsNotEmpty(Find<RectTransform>("OfficeStage").GetComponentsInChildren<OpsIncidentGraphic>().Where(g=>g.name=="UninstalledFrame"));
            foreach(var frame in Find<RectTransform>("OfficeStage").GetComponentsInChildren<OpsIncidentGraphic>().Where(g=>g.name=="UninstalledFrame"))
            {
                Assert.AreEqual(1,frame.StrokeWidth);Assert.AreEqual(.5f,frame.color.a);
                var device=(RectTransform)frame.transform.parent;var room=(RectTransform)device.parent;
                Assert.That(device.anchoredPosition.x,Is.InRange(room.rect.width-110,room.rect.width-26));
                Assert.LessOrEqual(room.GetComponentsInChildren<OpsIncidentGraphic>().Count(g=>g.name=="UninstalledFrame"),3);
            }
            PolishCapture(4);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_17_未導入の置き場所は導入を開いた間だけ表示する()
        {
            yield return PolishPlanning();var game=Object.FindAnyObjectByType<OpsGame>();string before=JsonUtility.ToJson(game.State);
            Assert.IsFalse(game.Surface.GetComponentsInChildren<OpsIncidentGraphic>().Any(g=>g.name=="UninstalledFrame"));PolishCapture(17);
            game.OpenTab(1);yield return new WaitForSecondsRealtime(.3f);
            Assert.IsNotEmpty(game.Surface.GetComponentsInChildren<OpsIncidentGraphic>().Where(g=>g.name=="UninstalledFrame"));PolishCapture(17,"install");
            Click("ClosePlanner");yield return null;
            Assert.IsFalse(game.Surface.GetComponentsInChildren<OpsIncidentGraphic>().Any(g=>g.name=="UninstalledFrame"));
            Click("Room_office");yield return null;Assert.IsNotNull(Find<RectTransform>("RoomDevice_inventory").Find("UninstalledFrame"));Click("ClosePlanner");yield return null;
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_03_ランプはサーバー室の名札の下に収まる()
        {
            yield return PolishPlanning();var room=Find<RectTransform>("Room_server");var label=room.Find("RoomLabel").GetComponent<RectTransform>();
            for(int i=0;i<3;i++)
            {
                var lamp=Find<RectTransform>("ServerLamp"+i);Assert.AreEqual(-146,lamp.anchoredPosition.y);
                Assert.Less(lamp.anchoredPosition.y,room.anchoredPosition.y+label.anchoredPosition.y-label.rect.height);
            }
            PolishCapture(3);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Polish4_02_泡は半秒で弾み三秒二で漂い省演出では静止する()
        {
            Assert.AreEqual(0,OpsBubbleMotion.EntranceScale(0));Assert.AreEqual(1.12f,OpsBubbleMotion.EntranceScale(.35f),.001f);Assert.AreEqual(1,OpsBubbleMotion.EntranceScale(.5f));
            Assert.AreEqual(new Vector2(3,7),OpsBubbleMotion.FloatOffset(3.2f*.33f));Assert.AreEqual(new Vector2(-3,3),OpsBubbleMotion.FloatOffset(3.2f*.66f));Assert.AreEqual(Vector2.zero,OpsBubbleMotion.FloatOffset(3.2f));
            yield return PolishPlanning();var game=Object.FindAnyObjectByType<OpsGame>();string before=JsonUtility.ToJson(game.State);
            PolishCapture(2);Click("Menu");if(!game.ReducedMotion)Click("ReduceMotion");Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            var rect=Find<RectTransform>("OfficeBubble0");var position=rect.anchoredPosition;yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(position,rect.anchoredPosition);Assert.AreEqual(Vector3.one,rect.localScale);Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
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
