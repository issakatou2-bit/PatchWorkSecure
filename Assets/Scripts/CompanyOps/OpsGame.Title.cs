using System;
using UnityEngine;
using UnityEngine.UI;
namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void TitleScreen()
        {
            PImage(screen,"TitleOffice",OfficeArt,350,-380,1400,1400);Motion((RectTransform)screen.Find("TitleOffice"),"drift",18);
            IncidentShape(screen,"TitleVeil","title-veil",0,0,1600,900,Color.white);
            SeasonLayer(Rect(screen,"TitleSeason",0,0,1600,900),1600,900,saved==null?0:saved.month);
            var logo=PImage(screen,"TitleLogoWordmark",PlanningArt.logoWordmark,90,40,640,246);Reveal(logo);if(Application.isPlaying)logo.GetComponent<OpsUIReveal>().Duration=.9f;
            var icon=PImage(screen,"TitleLogoIcon",PlanningArt.logoIcon,16,158,132,132);icon.localEulerAngles=new Vector3(0,0,8);
            PText(screen,"TitlePinkShadow","情シスの一年",116,304,620,60,46,PlanPink);
            PText(screen,"TitleWhiteShadow","情シスの一年",113,301,620,60,46,Color.white);
            PText(screen,"Title","情シスの一年",110,298,620,60,46);
            var ribbon=IncidentShape(screen,"TitleRibbon","cutin",110,374,570,44,PlanPink);
            PText(ribbon,"TitleSubtitle","会社を守る、12か月の育成シミュレーション",18,0,530,44,20,Color.white);
            var start=PButton(screen,"NewYear","ニューゲーム",90,450,460,76,ConfirmNewYear,PlanPink,Color.white);Shine(start.transform,460,76);
            TitleButtonStyle(start,true);
            var resume=PButton(screen,"ContinueYear",saved==null?"つづきの記録はありません":"つづきから",90,540,460,64,
                ()=>{State=saved;statChanges=new int[6];tab=0;Render();},Color.white,PlanInk,20,null,saved!=null);
            TitleButtonStyle(resume,false);
            if(saved!=null)PText(resume.transform,"ContinueMonth",saved.Current.name+" / "+(saved.month+1)+"か月目",258,0,186,64,14,PlanGray,true,true);
            TitleButtonStyle(PButton(screen,"HomeGuide","遊び方",90,618,223,64,Guide,Color.white,PlanInk),false);
            TitleButtonStyle(PButton(screen,"HomeSettings","設定",327,618,223,64,Menu,Color.white,PlanInk),false);
            Portrait(screen,"HomePortrait",1020,190,565,700,"pose_wave");
            var greeting=PCard(screen,"HomeGreeting",700,300,300,120,Color.white,24);
            SpeechName(greeting,"HomeGreetingName");
            PText(greeting,"HomeGreetingLine","今年も一緒に、\n会社を守ろうね！",18,14,264,90,19,null,false);
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
            var label=button.GetComponentInChildren<TMPro.TextMeshProUGUI>();label.fontSizeMax=primary?24:18;label.fontSizeMin=primary?20:14;label.fontSize=label.fontSizeMax;
        }
        private void ConfirmNewYear()
        {
            if(saved==null&&string.IsNullOrEmpty(SaveWarning)){StartYear(Environment.TickCount);return;}
            var d=Dialog("新しい一年を始めますか？","現在の試作の進行を置き換えます。旧版のセーブには影響しません。",340);
            Button(d,"ConfirmNewYear","新しい一年を始める",32,270,420,48,()=>StartYear(Environment.TickCount),Accent);
        }
    }
}
