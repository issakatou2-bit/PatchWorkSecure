using System;
using UnityEngine;
using UnityEngine.UI;
namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void TitleScreen()
        {
            if(PlanningArt.titleKeyVisual==null)throw new InvalidOperationException("承認済みタイトル一枚絵を設定してください。");
            var kv=PImage(screen,"TitleKeyVisual",PlanningArt.titleKeyVisual,80,0,1600,900);
            kv.pivot=new Vector2(.7f,.6f);kv.anchoredPosition+=new Vector2(1120,-360);Motion(kv,"kv",20);
            IncidentShape(screen,"TitleVeil","title-kv-veil",0,0,1600,900,Color.white);
            IncidentShape(screen,"TitleBottomVeil","title-bottom-veil",0,780,1600,120,Color.white);
            SeasonLayer(Rect(screen,"TitleSeason",0,0,1600,900),1600,900,0);
            var brand=Rect(screen,"TitleBrand",40,40,700,400);brand.localScale=Vector3.one*.84f;
            brand.pivot=new Vector2(0,.5f);brand.anchoredPosition+=new Vector2(0,-168);
            // HTMLは下向きYなので CSS rotate(-2deg) はUnityの +2度。
            brand.localEulerAngles=new Vector3(0,0,2);
            var logo=PImage(brand,"TitleLogoWordmark",PlanningArt.logoWordmark,50,0,640,246);Reveal(logo);if(Application.isPlaying)logo.GetComponent<OpsUIReveal>().Duration=.9f;
            var icon=PImage(brand,"TitleLogoIcon",PlanningArt.logoIcon,-24,118,132,132);icon.pivot=new Vector2(.5f,.5f);icon.anchoredPosition+=new Vector2(66,-66);icon.localEulerAngles=new Vector3(0,0,8);
            PText(brand,"TitlePinkShadow","情シスの一年",76,264,620,60,46,PlanPink);
            PText(brand,"TitleWhiteShadow","情シスの一年",73,261,620,60,46,Color.white);
            PText(brand,"Title","情シスの一年",70,258,620,60,46);
            var ribbon=PImage(brand,"TitleRibbon",PlanningArt.ribbonSlant,70,326,570,44,PlanPink);
            PText(ribbon,"TitleSubtitle","会社を守る、12か月の育成シミュレーション",18,0,530,44,20,Color.white);
            var start=PButton(screen,"NewYear","ニューゲーム",90,450,460,76,ConfirmNewYear,PlanPink,Color.white);Shine(start.transform,460,76);
            TitleButtonStyle(start,true);
            var resume=PButton(screen,"ContinueYear",saved==null?"つづきの記録はありません":"つづきから",90,540,460,64,
                ()=>{State=saved;statChanges=new int[6];tab=0;Render();},Color.white,PlanInk,20,null,saved!=null);
            TitleButtonStyle(resume,false);
            if(saved!=null)PText(resume.transform,"ContinueMonth",saved.Current.name+" / "+(saved.month+1)+"か月目",258,0,186,64,14,PlanGray,true,true);
            TitleButtonStyle(PButton(screen,"HomeGuide","遊び方",90,618,223,64,Guide,Color.white,PlanInk),false);
            TitleButtonStyle(PButton(screen,"HomeSettings","設定",327,618,223,64,Menu,Color.white,PlanInk),false);
            var caption=PText(screen,"TitleCaption","",620,836,890,30,16,PlanInk,false,true);caption.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
            PText(screen,"HomeFooter","ねっとわーく商事・社員45人 / 公表事例を参考にした架空の会社と数値です",90,828,1320,38,14,PlanGray,false);
            if(SaveWarning!="")PText(screen,"SaveWarning",SaveWarning,90,864,1420,28,15,Coral,false);
        }
        private void TitleButtonStyle(Button button,bool primary)
        {
            // 保存が無い場合も白いメニューを保つ。操作不可と理由は変えない。
            KitGradient(button.GetComponent<Image>(),primary?Hex("ff9ab3"):Color.white,primary?Hex("f2557c"):Hex("edf1f7"));
            var states=button.colors;states.normalColor=states.highlightedColor=states.selectedColor=states.disabledColor=Color.white;
            states.pressedColor=new Color(.96f,.96f,.96f);states.colorMultiplier=1;button.colors=states;
            button.GetComponent<Shadow>().effectColor=primary?Hex("d94a70"):Hex("c7d0e0");
            button.GetComponent<Shadow>().effectDistance=new Vector2(0,-6);
            var label=button.GetComponentInChildren<TMPro.TextMeshProUGUI>();label.fontSizeMax=primary?24:button.name=="ContinueYear"?21:18;label.fontSizeMin=primary?20:14;label.fontSize=label.fontSizeMax;
            bool large=primary||button.name=="ContinueYear";float padding=large?58:26;var rect=button.GetComponent<RectTransform>();
            label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(0,1);
            label.rectTransform.anchoredPosition=new Vector2(padding,0);label.rectTransform.sizeDelta=new Vector2(button.name=="ContinueYear"&&saved!=null?186:rect.rect.width-padding-16,rect.rect.height);
            if(large)IncidentShape(button.transform,"TitleMenuIcon",primary?"play":"arrow",26,(rect.rect.height-26)/2,26,26,primary?Color.white:PlanBlue);
        }
        private void ConfirmNewYear()
        {
            if(saved==null&&string.IsNullOrEmpty(SaveWarning)){StartYear(Environment.TickCount);return;}
            var d=Dialog("新しい一年を始めますか？","現在の試作の進行を置き換えます。旧版のセーブには影響しません。",340);
            Button(d,"ConfirmNewYear","新しい一年を始める",32,270,420,48,()=>StartYear(Environment.TickCount),Accent);
        }
    }
}
