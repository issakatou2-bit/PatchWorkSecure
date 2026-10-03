using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public bool YearOpeningActive{get;private set;}
        public bool YearOpeningPaused{get;private set;}
        public int YearOpeningStage{get;private set;}
        private float openingElapsed;
        private bool openingRepeated,buildingYearOpening;
        private OpsYearDefinition OpeningYear=>OpsCatalog.CompanyYear(RunYear);
        private RectTransform OpeningShape(Transform p,string id,string kind,float x,float y,float w,float h,Color? color=null,float radius=30,int shape=0)
        {var r=Rect(p,id,x,y,w,h);var g=r.gameObject.AddComponent<OpsOpeningGraphic>();g.Kind=kind;g.color=color??Color.white;g.Radius=radius;g.Shape=shape;g.raycastTarget=false;return r;}
        private void OpeningMotion(RectTransform r,string kind,float seconds=.6f,float delay=0)
        {if(!Application.isPlaying)return;var m=r.gameObject.AddComponent<OpsOpeningMotion>();m.Owner=this;m.Kind=kind;m.Duration=seconds;m.Delay=delay;}
        private void OpeningHeader(string en,string title,Color tone,bool dark=false)
        {var e=StoryText(screen,"OpeningCategory",en,0,54,1600,23,15,tone,true);e.characterSpacing=32;StoryText(screen,"OpeningHeading",title,0,81,1600,68,46,dark?Color.white:PlanInk,true);}
        private RectTransform OpeningCard(Transform p,string id,float x,float y,float w,float h,Color background,Color? border=null,float radius=26)
        {
            bool dark=background.r<.5f;
            var shadow=OpeningShape(p,id+"Shadow","shadow",x-22,y-8,w+44,h+62,new Color(.1f,.14f,.25f,.35f),radius+20);
            RectTransform outer=null;
            if(border.HasValue&&!dark)outer=OpeningShape(p,id+"Outer","rounded",x-8,y-8,w+16,h+16,border.Value,radius+8);
            var edge=OpeningShape(p,id+"Edge","rounded",x-(dark?3:4),y-(dark?3:4),w+(dark?6:8),h+(dark?6:8),dark?Hex("b83754"):Color.white,radius+4);
            var card=OpeningShape(p,id,"rounded",x,y,w,h,background,radius);
            foreach(var frame in new[]{shadow,outer,edge}.Where(f=>f!=null))frame.gameObject.AddComponent<OpsOpeningFrameFollow>().Initialize(card,new Vector2(x+w/2,-y-h/2));
            return card;
        }
        private Material OfficeHue(int year)
        {var shader=Resources.Load<Shader>("OpsOfficeHue");if(shader==null)return null;var m=new Material(shader){hideFlags=HideFlags.DontSave};m.SetFloat("_Hue",year==3?250:160);m.SetFloat("_Saturation",year==3?.9f:.8f);return m;}
        private void OpeningMap(Transform p,string id,float x,float y,float size,bool satellite)
        {
            var card=OpeningCard(p,id,x,y,size,size,Color.white,satellite?new Color(.247f,.663f,.961f,.45f):(Color?)null,30);
            card.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var image=PImage(card,id+"Art",OfficeArt,0,0,size,size).GetComponent<Image>();
            if(satellite){image.material=OfficeHue(RunYear);var r=image.rectTransform;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(size/2,-size/2);r.localScale=new Vector3(-1,1,1);card.gameObject.AddComponent<OpsTransientMaterial>().Material=image.material;}
            var text=satellite?"NEW  "+OpeningYear.location:"本社";float width=text.Length*18+36;
            var tag=OpeningShape(card,id+"Tag","rounded",18,16,width,36,satellite?PlanBlue:PlanInk,12);StoryText(tag,id+"Label",text,14,0,width-28,36,18,Color.white).textWrappingMode=TextWrappingModes.NoWrap;
            OpeningMotion(card,satellite?"right":"zoom",satellite?.8f:1.4f,satellite?.5f:0);
        }
        private void OpeningPill(Transform p,string id,string text,float x,float y,float w,Color tone,Color fg)
        {var r=PCard(p,id,x,y,w,32,tone,20,false);KitGradient(r.GetComponent<Image>(),Color.Lerp(tone,Color.white,.55f),tone);var o=r.gameObject.AddComponent<Outline>();o.effectColor=Color.white;o.effectDistance=new Vector2(3,-3);StoryText(r,id+"Text",text,5,0,w-10,32,15,fg,true);}
        public void PreviewYearOpening(int stage,bool paused=true)
        {if(Story==null&&Endless==null||RunYear<2||State.phase!=OpsPhase.Planning)return;YearOpeningPaused=paused;YearOpeningStage=RunYear>3?4:Mathf.Clamp(stage,0,4);openingElapsed=0;if(RunYear>3)EndlessShortOpening();else OpeningScreen();}
        private void BeginYearOpening()
        {
            Career.seenOpeningYears=Career.seenOpeningYears??new System.Collections.Generic.List<int>();openingRepeated=RepeatedChallenge||Career.seenOpeningYears.Contains(RunYear);
            if(!openingRepeated&&RunYear<=3)Career.seenOpeningYears.Add(RunYear);Save();PreviewYearOpening(0,false);
        }
        public void AdvanceYearOpening(){if(!YearOpeningActive)return;if(YearOpeningStage==4){YearOpeningActive=false;YearOpeningPaused=false;Render();return;}PreviewYearOpening(YearOpeningStage+1,YearOpeningPaused);}
        public void SkipYearOpening(){if(YearOpeningActive)PreviewYearOpening(4,false);}
        private void TickYearOpening(float dt)
        {
            if(openingTiming?.Skipped==true){StopVoice();YearOpeningStage=4;AdvanceYearOpening();return;}
            if(!YearOpeningPaused){openingTiming?.Advance(dt,FastPresentation);openingElapsed=openingTiming?.Elapsed??openingElapsed+dt*PresentationRate;if(openingElapsed>=OpsPresentationTiming.Opening[YearOpeningStage]){carryCompanionVoice=true;try{AdvanceYearOpening();}finally{carryCompanionVoice=false;}return;}}
            var bar=screen.Find("OpeningProgress") as RectTransform;if(bar!=null)bar.sizeDelta=new Vector2(1600*(YearOpeningStage+(YearOpeningPaused?1:Mathf.Clamp01(openingElapsed/OpsCatalog.YearOpeningSeconds[YearOpeningStage])))/5,5);
            var k=UnityEngine.InputSystem.Keyboard.current;if(k!=null&&k.escapeKey.wasPressedThisFrame)TrySkipPresentation();
        }
        private void OpeningScreen()
        {
            buildingYearOpening=true;NewScreen();buildingYearOpening=false;YearOpeningActive=true;PhasePresentationRunning=false;if(!carryCompanionVoice)StopVoice();
            openingTiming=BeginPresentation("year_opening_"+YearOpeningStage,OpsPresentationTiming.Opening[YearOpeningStage]);
            string[] stages={"growth","unlock","rivals","allies","title"};string kind=stages[YearOpeningStage];
            OpeningShape(screen,"OpeningBackground",kind=="title"?"rounded":kind,0,0,1600,900,PlanInk,0);
            PButton(screen,"OpeningAdvance","",0,0,1600,900,AdvanceYearOpening,Color.clear,Color.clear);
            if(YearOpeningStage==0||YearOpeningStage==1||YearOpeningStage==3)
            {var rays=IncidentShape(screen,"OpeningRays","rays",-150,-480,1900,1900,new Color(YearOpeningStage==3?.247f:1,YearOpeningStage==1?.753f:YearOpeningStage==3?.663f:.58f,YearOpeningStage==1?.18f:YearOpeningStage==3?.961f:.68f,YearOpeningStage==1?.16f:.1f));rays.pivot=new Vector2(.5f,.5f);rays.anchoredPosition=new Vector2(800,-470);Motion(rays,"rotate",YearOpeningStage==1?24:YearOpeningStage==3?36:40);}
            if(YearOpeningStage==0)OpeningGrowth();else if(YearOpeningStage==1)OpeningUnlock();else if(YearOpeningStage==2)OpeningRivals();else if(YearOpeningStage==3)OpeningAllies();else OpeningTitle();
            bool largeSkip=openingRepeated||RepeatedChallenge;
            var skip=PButton(screen,"OpeningSkip","スキップ",largeSkip?1374:1438,largeSkip?818:830,largeSkip?200:136,largeSkip?60:48,SkipYearOpening,new Color(1,1,1,largeSkip?.32f:.18f),Color.white,largeSkip?20:16);skip.GetComponentInChildren<TextMeshProUGUI>().fontSize=largeSkip?20:15;
            foreach(var effect in skip.GetComponents<BaseMeshEffect>())DestroyImmediate(effect);
            var topLight=skip.transform.Find("KitTopLight");if(topLight!=null)DestroyImmediate(topLight.gameObject);
            skip.GetComponent<Image>().color=new Color(1,1,1,largeSkip?.32f:.18f);
            for(int i=0;i<2;i++)IncidentShape(skip.transform,"SkipTriangle"+i,"play",(largeSkip?169:107)+i*10,largeSkip?24:18,8,11,Color.white);
            PImage(screen,"OpeningProgress",null,0,895,1600*(YearOpeningStage+1)/5,5,PlanPink);
            if(YearOpeningStage==3)QueueCompanionScene("opening");
            else if(carryCompanionVoice&&CurrentSpeaker!=""){voiceCaptionTarget="CompanionCaption";ApplyVoiceCaption();}
        }
        private void OpeningGrowth()
        {
            OpeningHeader("THE COMPANY GROWS",OpeningYear.growthHeading,PlanPink);OpeningMap(screen,"OpeningHeadOffice",150,190,560,false);OpeningMap(screen,"OpeningLocation",930,250,440,true);
            var link=OpeningShape(screen,"OpeningLink","link",700,430,240,80);OpeningMotion(link,"link",1);
            var prev=OpsCatalog.CompanyYear(RunYear-1);string[] names={"社員","守る端末",RunYear==3?"つながる会社":"拠点"};int[] before={prev.employees,prev.devices,RunYear==3?prev.partners:prev.branches},after={OpeningYear.employees,OpeningYear.devices,RunYear==3?OpeningYear.partners:OpeningYear.branches};
            for(int i=0;i<3;i++){float x=570+i*180;StoryText(screen,"OpeningCountLabel"+i,names[i],x,770,150,22,15,PlanInk,true);StoryText(screen,"OpeningCountBefore"+i,before[i]+" →",x,806,62,46,22,PlanGray,true);StoryText(screen,"OpeningCountAfter"+i,after[i].ToString(),x+63,782,100,90,54,PlanInk,true).textWrappingMode=TextWrappingModes.NoWrap;}
        }
        private void OpeningUnlock()
        {
            OpeningHeader("NEW EQUIPMENT","新しい設備が使えるようになった",Hex("e0a500"));
            for(int i=0;i<2;i++)
            {var data=OpeningYear.equipment[i];var card=OpeningCard(screen,"OpeningEquipment"+i,i==0?300:920,250,380,300,Color.white,new Color(1,.824f,.247f,.65f));card.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                var shade=PImage(card,"EquipmentGradient",PlanningArt.round24,0,0,380,300,Color.white,true);KitGradient(shade.GetComponent<Image>(),Color.white,Hex("f3f7ff"));
                var icon=PCard(card,"EquipmentIcon"+i,28,26,86,86,i==0?PlanBlue:PlanPink,24);KitGradient(icon.GetComponent<Image>(),i==0?Hex("7fc4ff"):Hex("ff94ae"),i==0?Hex("2f93dc"):Hex("f45a80"));StoryText(icon,"EquipmentInitial"+i,data.icon,0,0,86,86,34,Color.white,true);
                bool twoLines=data.name.Contains("（");StoryText(card,"OpeningEquipmentName"+i,data.name.Replace("（","\n（"),28,128,340,twoLines?84:44,28);StoryText(card,"EquipmentDescription"+i,data.description,28,twoLines?224:176,324,70,16,Hex("52607a"),false,true);
                var ribbon=PCard(card,"NewRibbon"+i,250,17,180,30,PlanPink,12,false);ribbon.pivot=new Vector2(.5f,.5f);ribbon.anchoredPosition=new Vector2(337,-53);ribbon.localRotation=Quaternion.Euler(0,0,-38);StoryText(ribbon,"NewRibbonText"+i,"NEW",0,0,180,30,15,Color.white,true);Shine(card,380,300);OpeningMotion(card,"pop",.6f,.15f+i*.3f);
            }
            string[] chips=new[]{RunYear+"年目の成長目標"}.Concat(State.GrowthGoals.Select(g=>g.name)).ToArray();float[] widths=chips.Select(t=>t.Length*15f+28).ToArray();float x=(1600-widths.Sum()-48)/2;
            for(int i=0;i<chips.Length;i++){OpeningPill(screen,"OpeningGoal"+i,chips[i],x,600,widths[i],i==0?Hex("eef2f8"):Hex("ffc02e"),i==0?Hex("52607a"):Hex("7a5a00"));x+=widths[i]+16;}StoryText(screen,"OpeningUnlockNote",OpeningYear.unlockNote,0,680,1600,36,20,Hex("52607a"),true);
        }
        private void OpeningRivals()
        {
            OpeningShape(screen,"OpeningScan","scan",0,0,1600,900);OpeningHeader("NEW THREATS","まだ見ぬ強敵",PlanPink,true);
            for(int i=0;i<3;i++)
            {var rival=OpeningYear.rivals[i];var c=OpeningCard(screen,"OpeningRival"+i,190+i*445,200,330,480,Hex("130810"),new Color(1,.314f,.431f,.6f),24);c.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                var bg=PImage(c,"RivalGradient",PlanningArt.round24,0,0,330,480);KitGradient(bg.GetComponent<Image>(),Hex("2a0f1d"),Hex("130810"));var tag=PCard(c,"RivalMonth",16,16,114,27,Hex("e0405f"),12,false);StoryText(tag,"RivalMonthText",((rival.month+3)%12+1)+"月の山場",0,0,114,27,14,Color.white,true);
                var art=OpeningShape(c,"RivalSilhouette","rival",55,40,220,300,null,0,rival.shape);OpeningMotion(art,"rival",1);
                StoryText(c,"RivalStars",new string('★',rival.stars),0,360,330,32,20,PlanPink,true).characterSpacing=3;StoryText(c,"RivalName",rival.name,0,394,330,38,24,Hex("ffd0da"),true);StoryText(c,"RivalHint",rival.hint,0,436,330,22,14,new Color(1,.816f,.855f,.75f),true,true);OpeningMotion(c,"rise",.5f,.2f+i*.3f);
            }StoryText(screen,"OpeningRivalNote",OpeningYear.rivalNote,0,720,1600,36,20,Hex("ffd0da"),true);
        }
        private void OpeningAllies()
        {
            OpeningHeader("ALLIES","仲間も強くなった",PlanBlue);Sprite[] images={PlanningArt.focusEngineer,PlanningArt.focusHinata,PlanningArt.focusSecretary};
            for(int i=0;i<3;i++)
            {var c=OpeningCard(screen,"OpeningAlly"+i,110+i*480,i==1?170:190,420,560,Color.white,null,26);c.gameObject.AddComponent<Mask>().showMaskGraphic=false;c.pivot=new Vector2(.5f,.5f);c.anchoredPosition+=new Vector2(210,-280);c.localRotation=Quaternion.Euler(0,0,i==0?3:i==2?-3:0);
                var image=PImage(c,"AllyArt",images[i],0,0,420,560);image.gameObject.AddComponent<OpsOpeningCover>().Sprite=images[i];
                OpeningShape(c,"AllyCaptionShade","ally-veil",0,420,420,140);StoryText(c,"AllyName",OpeningYear.allyNames[i],20,446,380,36,24,Color.white);StoryText(c,"AllyDescription",OpeningYear.allyDescriptions[i],20,486,380,56,15,Color.white,false,true);OpeningMotion(c,i==0?"left":i==1?"pop":"right",.6f,.1f+i*.25f);
            }
            var chips=Enumerable.Range(0,State.staffExperience.Length).Select(i=>OpsGrowthCatalog.StaffNames[i]+" Lv"+State.StaffLevel(i)).Concat(new[]{"相談文化 "+State.culture}).ToArray();float[] widths=chips.Select(t=>t.Length*15f+28).ToArray();float x=(1600-widths.Sum()-(chips.Length-1)*14)/2;for(int i=0;i<chips.Length;i++){OpeningPill(screen,"OpeningStaff"+i,chips[i],x,790,widths[i],i==chips.Length-1?Hex("7fc4ff"):Hex("ffc02e"),i==chips.Length-1?Hex("1f5f99"):Hex("7a5a00"));x+=widths[i]+14;}
        }
        private void OpeningTitle()
        {
            var art=PImage(screen,"OpeningKeyVisual",PlanningArt.titleKeyVisual,0,0,1600,900,new Color(1,1,1,.55f));OpeningMotion(art,"zoom",3);OpeningShape(screen,"OpeningTitleVeil","title-veil",0,0,1600,900);
            var ch=StoryText(screen,"OpeningChapter","CHAPTER "+RunYear,0,200,1600,30,18,Hex("ffd23f"),true);ch.characterSpacing=32;OpeningMotion(ch.rectTransform,"rise",.5f,.1f);
            StoryText(screen,"OpeningYearShadow",RunYear+"年目",0,202,1600,280,200,new Color(1,.435f,.569f,.85f),true);var year=StoryText(screen,"OpeningYear",RunYear+"年目",0,192,1600,280,200,Color.white,true);OpeningMotion(year.rectTransform,"big",.9f,.2f);
            float width=OpeningYear.theme.Length*36+70;var theme=PCard(screen,"OpeningTheme",(1600-width)/2,450,width,66,PlanPink,16,false);KitGradient(theme.GetComponent<Image>(),Hex("ff94ae"),PlanPink,true);StoryText(theme,"OpeningThemeText",OpeningYear.theme,30,0,width-60,66,36,Color.white,true).textWrappingMode=TextWrappingModes.NoWrap;OpeningMotion(theme,"pop",.5f,.9f);
            var cry=StoryText(screen,"OpeningCry",OpeningYear.cry,400,600,800,74,44,Hex("ffd23f"),true);cry.characterSpacing=20;OpeningMotion(cry.rectTransform,"stamp",.6f,1.3f);
            var flash=PImage(screen,"OpeningFlash",null,0,0,1600,900,new Color(1,1,1,.95f));OpeningMotion(flash,"flash",.5f,.15f);
        }
        private void EndlessShortOpening()
        {
            buildingYearOpening=true;NewScreen();buildingYearOpening=false;YearOpeningActive=true;PhasePresentationRunning=false;StopVoice();
            openingTiming=BeginPresentation("endless_opening",OpsPresentationTiming.Opening[4]);
            OpeningShape(screen,"OpeningBackground","rivals",0,0,1600,900,PlanInk,0);OpeningShape(screen,"OpeningScan","scan",0,0,1600,900);
            OpeningHeader("NEXT YEAR",RunYear+"年目",PlanPink,true);
            OpeningShape(screen,"RivalSilhouette","rival",600,170,400,450,null,0,OpeningYear.rivals[2].shape);
            StoryText(screen,"OpeningPressure","脅威 +"+State.yearPressure,0,620,1600,65,40,Color.white,true);
            PButton(screen,"OpeningAdvance","迎え撃つ",630,755,340,62,AdvanceYearOpening,PlanPink,Color.white,23);
        }
    }
    public sealed class OpsOpeningCover : MonoBehaviour
    {
        public Sprite Sprite;
        private void Start(){if(Sprite==null)return;float h=Mathf.Max(560,420*Sprite.rect.height/Sprite.rect.width),w=h*Sprite.rect.width/Sprite.rect.height;var r=(RectTransform)transform;r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2((420-w)/2,-(h-560)*.16f);}
    }
    public sealed class OpsTransientMaterial : MonoBehaviour
    {public Material Material;private void OnDestroy(){if(Material!=null)Destroy(Material);}}
    public sealed class OpsOpeningFrameFollow : MonoBehaviour
    {
        private RectTransform target,rect;private Vector2 offset;
        public void Initialize(RectTransform next,Vector2 center)
        {target=next;rect=(RectTransform)transform;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition+=new Vector2(rect.rect.width/2,-rect.rect.height/2);offset=rect.anchoredPosition-center;}
        private void LateUpdate()
        {if(target==null)return;Vector2 center=target.anchoredPosition+new Vector2((.5f-target.pivot.x)*target.rect.width,(target.pivot.y-.5f)*-target.rect.height);rect.anchoredPosition=center+(Vector2)(target.localRotation*(Vector3)(offset*target.localScale.x));rect.localRotation=target.localRotation;rect.localScale=target.localScale;}
    }
}
