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
            var veil=Box(screen,"TitleVeil",0,0,1600,900,Color.white);
            KitGradient(veil.GetComponent<Image>(),new Color(.92f,.96f,1,.97f),new Color(1,.89f,.93f,0),true);
            SeasonLayer(Rect(screen,"TitleSeason",0,0,1600,900),1600,900,saved==null?0:saved.month);
            var logo=PImage(screen,"TitleLogoWordmark",PlanningArt.logoWordmark,90,40,640,246);Reveal(logo,0,true);
            var icon=PImage(screen,"TitleLogoIcon",PlanningArt.logoIcon,16,158,132,132);icon.localEulerAngles=new Vector3(0,0,8);
            PText(screen,"Title","情シスの一年",110,298,620,60,46);
            var ribbon=IncidentShape(screen,"TitleRibbon","cutin",110,374,570,44,PlanPink);
            PText(ribbon,"TitleSubtitle","会社を守る、12か月の育成シミュレーション",18,0,530,44,20,Color.white);
            var start=PButton(screen,"NewYear","ニューゲーム",90,450,460,76,ConfirmNewYear,PlanPink,Color.white);Shine(start.transform,460,76);
            PButton(screen,"ContinueYear",saved==null?"つづきの記録はありません":"つづきから  /  "+saved.Current.name,90,540,460,64,
                ()=>{State=saved;statChanges=new int[6];tab=0;Render();},Color.white,PlanInk,20,null,saved!=null);
            PButton(screen,"HomeGuide","遊び方",90,618,223,64,Guide,Color.white,PlanInk);
            PButton(screen,"HomeSettings","設定",327,618,223,64,Menu,Color.white,PlanInk);
            Portrait(screen,"HomePortrait",1020,190,565,700,"pose_wave");
            var greeting=PCard(screen,"HomeGreeting",700,300,300,120,Color.white,24);
            PText(greeting,"HomeGreetingName","ひなた",16,-12,100,26,13,PlanPink);
            PText(greeting,"HomeGreetingLine","今年も一緒に、\n会社を守ろうね！",18,14,264,90,19,null,false);
            PText(screen,"HomeFooter","ねっとわーく商事・社員45人 / 公表事例を参考にした架空の会社と数値です",90,828,1320,38,14,PlanGray,false);
            if(SaveWarning!="")PText(screen,"SaveWarning",SaveWarning,90,864,1420,28,15,Coral,false);
        }
        private void ConfirmNewYear()
        {
            if(saved==null&&string.IsNullOrEmpty(SaveWarning)){StartYear(Environment.TickCount);return;}
            var d=Dialog("新しい一年を始めますか？","現在の試作の進行を置き換えます。旧版のセーブには影響しません。",340);
            Button(d,"ConfirmNewYear","新しい一年を始める",32,270,420,48,()=>StartYear(Environment.TickCount),Accent);
        }
    }
}
