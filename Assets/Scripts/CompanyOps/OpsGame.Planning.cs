using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public OpsPlanningArt PlanningArt;
        private static readonly Color PlanInk=Hex("1d2a44"), PlanGray=Hex("6b7894"), PlanPink=Hex("ff6f91"),
            PlanBlue=Hex("3fa9f5"), PlanMint=Hex("2ec4a0"), PlanPurple=Hex("8e7cc3"), PlanTrack=Hex("dfe5f0");
        public static string PlanningRank(int v) => v>=95?"S":v>=80?"A":v>=65?"B":v>=50?"C":v>=35?"D":v>=20?"E":v>=10?"F":"G";
        private static Color RankColor(int v) => v>=95?Hex("e0a800"):v>=80?PlanPink:v>=65?Hex("ff9a45"):v>=50?PlanMint:v>=35?PlanBlue:v>=20?PlanPurple:Hex("a6afc0");
        private RectTransform PImage(Transform p,string name,Sprite sprite,float x,float y,float w,float h,Color? c=null,bool sliced=false)
        {
            var r=Rect(p,name,x,y,w,h); var i=r.gameObject.AddComponent<Image>(); i.sprite=sprite; i.color=c??Color.white;
            i.raycastTarget=false; if(sliced) i.type=Image.Type.Sliced; return r;
        }
        private RectTransform PCard(Transform p,string name,float x,float y,float w,float h,Color? color=null,float radius=24,bool shadow=true)
        {
            if(shadow) PImage(p,name+"Shadow",PlanningArt.shadow,x-24,y-16,w+48,h+48,Color.white,true);
            var sprite=radius>=28?PlanningArt.round28:radius>=24?PlanningArt.round24:radius>=20?PlanningArt.round20:radius>=16?PlanningArt.round16:PlanningArt.round12;
            var card=PImage(p,name,sprite,x,y,w,h,color??new Color(1,1,1,.94f),true);
            var c=color??Color.white;
            if(w>200 && h>100 && c.a>.85f && c.r>.9f && c.g>.9f && c.b>.9f) KitPanel(card,c);
            return card;
        }
        private TextMeshProUGUI PText(Transform p,string name,string value,float x,float y,float w,float h,float size=20,Color? c=null,bool heading=true,bool center=false)
        {
            var t=Text(p,name,value,x,y,w,h,size,c??PlanInk); t.font=heading&&HeadingFont!=null?HeadingFont:Font;
            t.alignment=center?TextAlignmentOptions.Midline:TextAlignmentOptions.MidlineLeft;
            if(heading) t.fontStyle=FontStyles.Bold;
            if(name=="NavigatorSpeech" || name=="TutorialLine" || name=="ResolutionReaction")
            {
                t.text=SpeechLines(value);t.textWrappingMode=TextWrappingModes.NoWrap;t.fontSizeMin=12;PortraitSpeech(t);
            }
            return t;
        }
        public static string SpeechLines(string value)=>string.Join("\n",(value??"").Replace("\r","").Replace("、","、\n").Replace("！","！\n").Replace("。","。\n").Replace("？","？\n")
            .Split('\n').Select(line=>line.TrimStart(' ','　','\t')).Where(line=>line.Length>0));
        private Button PButton(Transform p,string id,string label,float x,float y,float w,float h,Action action,Color bg,Color fg,float radius=20,Color? shadow=null,bool enabled=true)
        {
            var b=Button(p,id,label,x,y,w,h,action,bg,enabled); var i=b.GetComponent<Image>();
            i.sprite=radius>=20?PlanningArt.round20:PlanningArt.round16; i.pixelsPerUnitMultiplier=1; i.type=Image.Type.Sliced;
            var t=b.GetComponentInChildren<TextMeshProUGUI>(); t.color=fg; t.alignment=TextAlignmentOptions.Midline; t.margin=Vector4.zero;
            t.rectTransform.offsetMin=Vector2.zero;t.rectTransform.offsetMax=Vector2.zero;
            if(shadow.HasValue) { var s=b.GetComponent<Shadow>(); s.effectDistance=new Vector2(0,-6); s.effectColor=shadow.Value; s.useGraphicAlpha=true; }
            var feedback=b.GetComponent<OpsButtonFeedback>(); feedback.PressDepth=4; return b;
        }
        private void Motion(RectTransform r,string kind,float period,float delay=0)
        {
            var m=r.gameObject.AddComponent<OpsPlanningMotion>();m.Owner=this;m.Kind=kind;m.Period=period;m.Delay=delay;
        }
        private void Shine(Transform p,float w,float h)
        {
            var clip=Rect(p,"ShineClip",0,0,w,h);clip.gameObject.AddComponent<RectMask2D>();
            var shine=PImage(clip,"ButtonShine",PlanningArt.shine,-w*.4f,0,w*.3f,h);Motion(shine,"shine",2.8f);
        }
        private void SpeechName(RectTransform bubble,string label="NavigatorName")
        {
            // 名札全体を吹き出しの上に置く。枠の縁・マスクに文字を重ねない。
            var p=bubble.anchoredPosition;
            var tag=PCard(bubble.parent,bubble.name+"NameTag",p.x+16,-p.y-36,90,28,PlanPink,12,false);
            PText(tag,label,Navigator!=null?Navigator.DisplayName:"ひなた",0,0,90,28,14,Color.white,true,true);
        }
        private void PlanningScreen()
        {
            if(PlanningArt==null) throw new InvalidOperationException("計画画面の承認済みUI素材を設定してください。");
            PImage(screen,"PlanningBackground",PlanningArt.gradient,0,0,1600,900);
            PImage(screen,"OfficeBlur",PlanningArt.officeBlur,-100,-450,1800,1800,new Color(1,1,1,.45f));
            PlanningStage(); PlanningHeader(); PlanningCompany(); PlanningConsultation(); PlanningNavigator(); PlanningActions();
            WorkCompleteEffect();
            RankBenefitBand();
            if(tab!=0) PlanningOverlay();
            if(SaveWarning!="") PText(screen,"SaveWarning",SaveWarning,24,866,600,28,15,Hex("c23a60"),false);
        }
        private void PlanningStage()
        {
            PImage(screen,"OfficeStageShadow",PlanningArt.shadow,322,88,960,844,new Color(1,1,1,1),true);
            PImage(screen,"StageWhiteBorder",PlanningArt.stageTop,346,102,912,804,Color.white,true);
            var stage=PImage(screen,"OfficeStage",PlanningArt.stageTop,352,108,900,792,Color.white,true);
            stage.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            PImage(stage,"OfficeArt",OfficeArt,0,0,900,900);
            PImage(stage,"StageShade",PlanningArt.stageShade,0,492,900,300);
            SeasonLayer(stage,900,792,State.month);
            PlanningRooms(stage);
            if(State.decisionDepthRules>0)
            {
                var time=PCard(stage,"PlanningTimeBadge",250,16,146,34,State.IncidentTime>0?Hex("5a4a9a"):PlanBlue,12,false);
                PText(time,"PlanningTimeLabel",State.IncidentTimeLabel,0,0,146,34,14,Color.white,true,true);
            }
            if(State.CultureEarlySignal)
            {
                var upcoming=State.EventAt(State.month+1);string category=upcoming==null?"システムの運用":OpsEventCatalog.Profile(upcoming.profile).category;
                var signal=PButton(stage,"CultureEarlySignal","?",220,172,40,40,()=>Dialog("社員の兆候報告 / 次月への備え",OpsCatalog.Months[State.month+1].name+"は、"+category+"の周りを点検しておきたいね。\n\n社員の報告から得た点検のヒント。攻撃や侵害が確定したという意味ではありません。",420),Color.white,PlanPink,20);Hover(signal,"相談文化 C以上 / 次月の点検ヒント");
            }
            for(int j=0;j<3;j++)
            {
                bool installed=State.Level(j==0?"backup":j==1?"monitor":"redundancy")>0;
                var lamp=PCard(stage,"ServerLamp"+j,372+j*18,146,8,8,installed?(j<2?Hex("5dff9c"):Hex("ffcf4a")):Hex("8b93a3"),12,false);
                if(installed) Motion(lamp,"blink",1.1f,j*.4f);
            }
            PlanningPin(stage,"Pin_backup","復旧基盤",430,190,52,State.Level("backup")+State.Level("drill"),()=>OfficePinDialog("復旧基盤",State.Level("backup")+State.Level("drill"),"backup","recover"));
            PlanningPin(stage,"Pin_culture","相談できる現場",650,365,42,State.Level("education"),()=>OfficePinDialog("相談できる現場",State.Level("education"),"culture","people"));
            PlanningPin(stage,"Pin_change","運用のしくみ",330,380,42,State.Level("automation")+State.Level("runbook"),()=>OfficePinDialog("運用のしくみ",State.Level("automation")+State.Level("runbook"),"change","operations"));
            var consultation=PButton(stage,"OfficeConsultation","!",700,36,56,56,PlanningBriefDialog,PlanPink,Color.white,20,Hex("c1536c"));
            var mark=consultation.GetComponentInChildren<TextMeshProUGUI>();mark.fontSize=mark.fontSizeMax=34;mark.fontSizeMin=34;
            consultation.GetComponent<Image>().sprite=PlanningArt.markerBubble;consultation.GetComponent<Image>().type=Image.Type.Simple;
            Border(consultation,Color.white,4);Motion((RectTransform)consultation.transform,"pop",1.6f);
            Hover(consultation,"今月の相談 / "+State.Current.person);
            if(State.Ticket!=null&&string.IsNullOrEmpty(State.ticketResolution))
            {
                var ticket=PButton(stage,"OfficeTicket","?",150,250,44,44,TicketDialog,Color.white,PlanBlue,20);
                var question=ticket.GetComponentInChildren<TextMeshProUGUI>();question.fontSize=question.fontSizeMax=26;question.fontSizeMin=26;
                ticket.GetComponent<Image>().sprite=PlanningArt.markerBubble;ticket.GetComponent<Image>().type=Image.Type.Simple;
                Border(ticket,PlanBlue,3); Motion((RectTransform)ticket.transform,"pop",1.6f,.7f);Hover(ticket,State.Ticket.title);
            }
            PlanningBubbles(stage);
        }
        private string bubbleMonthKey="";
        private float bubbleArrivalAt,lastBubblePop=-10;
        private int bubbleStreak;
        private AudioSource bubbleAudio;
        private void PlanningBubbles(RectTransform stage)
        {
            if(State.bubbleRules==0)return;
            string key=State.seed+":"+State.month;
            if(bubbleMonthKey!=key){bubbleMonthKey=key;bubbleArrivalAt=Time.unscaledTime;bubbleStreak=0;}
            var counter=PCard(stage,"BubbleCounter",16,16,220,34,Color.white,16,false);
            PText(counter,"BubbleDone","困りごと "+State.BubbleDone+" / 4",12,0,196,34,16);
            // 時間帯はカウンターの右へ。どちらもマップの公開情報。
            var clock=stage.Find("PlanningTimeBadge") as RectTransform;if(clock!=null)clock.anchoredPosition=new Vector2(250,-16);
            for(int i=0;i<4;i++)if(State.BubbleAvailable(i))
            {
                int index=i,kind=State.BubbleKind(i);Color tint=kind==7?Hex("e0a100"):kind==6?PlanPink:PlanBlue;
                Vector2 position=BubblePosition(kind,index);
                var b=PButton(stage,"OfficeBubble"+i,"",position.x,position.y,70,70,()=>PopOfficeBubble(index),new Color(1,1,1,.9f),tint,28);
                b.GetComponent<Image>().sprite=PlanningArt.round28;b.GetComponent<Image>().color=new Color(1,1,1,.01f);
                // 素材200px中の泡は直径140px。100pxで表示して操作範囲70pxと揃える。
                PImage(b.transform,"BubbleGlass",kind==7?PlanningArt.bubbleRare:kind==6?PlanningArt.bubbleConsult:PlanningArt.bubbleNormal,-15,-15,100,100);
                IncidentShape(b.transform,"BubbleIcon","bubble-icon",20,20,30,30,tint).GetComponent<OpsIncidentGraphic>().Offset=kind;
                var tag=PCard(b.transform,"BubbleTag",-28,74,126,22,tint,12,false);PText(tag,"BubbleName",OpsCatalog.BubbleNames[kind],0,0,126,22,12,Color.white,true,true);
                if(kind==7){var ring=PImage(b.transform,"BubbleRareRing",PlanningArt.bubbleRareRing,-15,-15,100,100);ring.pivot=new Vector2(.5f,.5f);ring.anchoredPosition+=new Vector2(50,-50);Motion(ring,"rotate",6);}
                var motion=b.gameObject.AddComponent<OpsBubbleMotion>();motion.Owner=this;motion.ArrivalAt=bubbleArrivalAt+i*.6f;
            }
            if(State.clueCollected)BubbleClue(stage,false);
        }
        private Vector2 BubblePosition(int kind,int index)
        {
            Vector2[] rooms={new Vector2(95,110),new Vector2(245,190),new Vector2(280,290),new Vector2(505,300),new Vector2(705,200),new Vector2(760,420),new Vector2(600,235)};
            if(kind<7)return rooms[kind];
            var occupied=Enumerable.Range(0,4).Where(i=>State.BubbleKind(i)<7).Select(i=>rooms[State.BubbleKind(i)]).ToList();
            int rareIndex=Enumerable.Range(0,index).Count(i=>State.BubbleKind(i)==7);
            return rooms.Where(p=>!occupied.Contains(p)).Reverse().ElementAt(rareIndex);
        }
        public bool PopOfficeBubble(int index)
        {
            if(State==null||!State.BubbleAvailable(index))return false;
            var button=screen.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="OfficeBubble"+index);if(button==null)return false;
            int kind=State.BubbleKind(index);if(!State.PopBubble(index))return false;
            button.interactable=false;button.GetComponent<OpsBubbleMotion>().PopAt=Time.unscaledTime;
            float now=Time.unscaledTime;bubbleStreak=now-lastBubblePop<2.5f?bubbleStreak+1:1;lastBubblePop=now;
            BubbleSound(kind==7,Mathf.Pow(1.122f,Mathf.Max(0,bubbleStreak-2)));
            var stage=button.transform.parent;var p=BubblePosition(kind,index);Color tint=kind==7?Hex("e0a100"):kind==6?PlanPink:PlanBlue;
            for(int j=0,n=kind==7?14:8;j<n;j++)
            {
                float angle=j*Mathf.PI*2/n;var drop=PCard(stage,"BubbleDrop"+j,p.x+30,p.y+30,10,10,tint,12,false);
                var fx=drop.gameObject.AddComponent<OpsBubbleMotion>();fx.Owner=this;fx.Kind="drop";fx.Direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*54;
            }
            var reward=PText(stage,"BubbleReward",OpsCatalog.BubbleRewards[kind],p.x-70,p.y-30,210,34,22,Color.white,true,true);
            var outline=reward.gameObject.AddComponent<Outline>();outline.effectColor=tint;outline.effectDistance=new Vector2(2,-2);
            var rewardMotion=reward.gameObject.AddComponent<OpsBubbleMotion>();rewardMotion.Owner=this;rewardMotion.Kind="reward";
            screen.GetComponentsInChildren<TextMeshProUGUI>().First(t=>t.name=="BubbleDone").text="困りごと "+State.BubbleDone+" / 4";
            if(kind==6)BubbleClue(stage,true);
            if(kind==7)
            {
                var prior=stage.Find("BubbleThanks");if(prior!=null){prior.gameObject.SetActive(false);Destroy(prior.gameObject);}
                var clue=stage.Find("BubbleClue");if(clue!=null)clue.gameObject.SetActive(false);
                var thanks=PCard(stage,"BubbleThanks",186,58,420,104,Color.white,20);PText(thanks,"BubbleThanksText","社員からのお礼\n助かった！ いつもありがとう。",20,12,380,76,18,PlanInk,false);
                Reveal(thanks);StartCoroutine(FinishBubbleThanks(thanks,clue));SpeakSceneLine(LastReactionId=="extra_embarrassed"?"extra_doya":"extra_embarrassed",.25f);
            }
            // HUDだけ更新し、他の泡の出現や弾ける演出を再起動しない。
            var company=screen.Find("CompanyGrowth");if(company!=null){company.gameObject.SetActive(false);Destroy(company.gameObject);}
            var companyShadow=screen.Find("CompanyGrowthShadow");if(companyShadow!=null){companyShadow.gameObject.SetActive(false);Destroy(companyShadow.gameObject);}
            PlanningCompany();
            var goal=screen.Find("YearGoals/Goal2/GoalProgress");if(goal!=null)goal.GetComponent<TextMeshProUGUI>().text=State.culture+"/65";
            Save();return true;
        }
        private void BubbleClue(Transform stage,bool reveal)
        {
            if(stage.Find("BubbleClue")!=null)return;
            var card=PCard(stage,"BubbleClue",186,58,420,104,Color.white,20);
            PText(card,"BubbleClueTitle","今月の手がかり（事件のときに効く）",16,8,388,24,13,PlanPink);
            PText(card,"BubbleClueText",State.Current.staff,16,32,388,64,16,PlanInk,false);if(reveal)Reveal(card);
        }
        private void BubbleSound(bool rare,float pitch)
        {
            if(!Application.isPlaying||muted||soundVolume<=0)return;
            if(bubbleAudio==null)bubbleAudio=NewAudioSource();PlayCue(OpsCue.Click);bubbleAudio.clip=buttonAudio.clip;bubbleAudio.pitch=pitch;bubbleAudio.volume=soundVolume*.65f;bubbleAudio.Play();buttonAudio.Stop();
            if(rare)StartCoroutine(BubbleChime(pitch));
        }
        private System.Collections.IEnumerator FinishBubbleThanks(RectTransform thanks,Transform clue)
        {yield return new WaitForSecondsRealtime(3);if(thanks!=null)Destroy(thanks.gameObject);if(clue!=null)clue.gameObject.SetActive(true);}
        private System.Collections.IEnumerator BubbleChime(float pitch)
        {
            for(int i=0;i<2;i++){yield return new WaitForSecondsRealtime(.09f);if(muted||soundVolume<=0)yield break;bubbleAudio.pitch=pitch*(i==0?1.5f:2);bubbleAudio.Play();}
        }
        private void Border(Button b,Color color,float size)
        { var edge=b.gameObject.AddComponent<Outline>();edge.effectColor=color;edge.effectDistance=new Vector2(size,-size); }
        private void Hover(Button b,string caption)
        {
            var r=PCard(b.transform,"HoverLabel",-64,-46,240,34,PlanInk,12,false);
            PText(r,"HoverCaption",caption,8,0,224,34,13,Color.white,false,true);r.gameObject.SetActive(false);
            b.gameObject.AddComponent<OpsPlanningHover>().Tooltip=r.gameObject;
        }
        private void PlanningPin(Transform p,string id,string title,float x,float y,float size,int level,Action action)
        {
            var b=PButton(p,id,"",x,y,size,size,action,level>0?PlanMint:new Color(.35f,.39f,.47f,.85f),Color.white,24);
            b.GetComponent<Image>().sprite=PlanningArt.round28; Border(b,Color.white,3);
            if(level==0) PImage(b.transform,"ToolIcon",PlanningArt.tool,(size-26)/2,(size-26)/2,26,26);
            else PCard(b.transform,"InstalledLamp",size/2-6,size/2-6,12,12,Hex("5dff9c"),12,false);
            Hover(b,title+" / "+(level>0?"整備 Lv."+level:"未整備"));
        }
        private void PlanningHeader()
        {
            PImage(screen,"PlanningLogoIcon",PlanningArt.logoIcon,24,18,76,76);
            var medal=PCard(screen,"MonthMedal",116,16,120,80,PlanPink,24,false);
            KitGradient(medal.GetComponent<Image>(),Hex("ff9ab3"),Hex("f2557c"));
            IncidentShape(medal,"MonthStitch","dashed",5,5,110,70,new Color(1,1,1,.65f));
            var shadow=medal.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-5);shadow.effectColor=Hex("d94a70");
            PText(medal,"YearLabel",Story==null?"1年だけ":Story.year+"年目・目標 "+Story.Goal,0,9,120,22,13,Color.white,true,true);
            PText(medal,"Month",State.Current.name,0,30,120,45,38,Color.white,true,true);
            int slots=Math.Max(State.MaxCapacity,State.capacity);
            float workWidth=Math.Max(192,72+slots*30), workX=1496-workWidth, budgetWidth=Math.Max(170,126+State.budget.ToString().Length*24), budgetX=workX-16-budgetWidth;
            var timeline=PCard(screen,"YearTimeline",252,24,budgetX-268,64,null,20,false);
            PText(timeline,"TimelineTitle","一年の歩み",20,0,84,64,14,PlanGray);
            float x=118;
            for(int j=0;j<12;j++)
            {
                bool current=j==State.month,peak=j%3==2;float size=j==11?30:peak||current?26:18;
                if(current) PCard(timeline,"CurrentMonthRing",x-4,32-size/2-4,size+8,size+8,Hex("ffd3de"),20,false);
                var dot=PCard(timeline,"Month"+j,x,32-size/2,size,size,current?PlanPink:j==11?PlanBlue:peak?Hex("ffd23f"):j<State.month?Hex("b8dbea"):PlanTrack,12,false);
                if(peak) PText(dot,"PeakMark",j==11?"決":"山",0,0,size,size,13,current||j==11?Color.white:Hex("7a5a00"),true,true);
                if(peak&&State.peakGoalRules>0)
                {
                    int target=j;var hit=PButton(dot,"PeakGoal_"+j,"",0,0,size,size,()=>PeakGoalDialog(target),Color.clear,Color.clear,12);
                    Hover(hit,OpsCatalog.Months[j].name+"の目標と見込み");
                }
                x+=size+8;
            }
            int next=Math.Min(11,State.month+(2-State.month%3));
            string hint=State.growthRules==0?"旧年度の記録":State.month==next?(next==11?"年度末の総力対応":"今月は山場！"):"次の山場 "+OpsCatalog.Months[next].name+"まで あと"+(next-State.month)+"か月";
            PText(timeline,"PeakLegend",hint,x+14,0,Math.Max(120,timeline.rect.width-x-34),64,16,Hex("d94a70"));
            var budget=PButton(screen,"Stat_0","",budgetX,24,budgetWidth,64,()=>StatusDetail(0),new Color(1,1,1,.92f),PlanInk,20);
            var coin=PCard(budget.transform,"BudgetCoin",22,17,30,30,Hex("ffd23f"),20,false);
            PText(coin,"CoinLabel","円",0,0,30,30,16,Hex("7a5a00"),true,true);
            PText(budget.transform,"予算Value",State.budget.ToString(),62,2,budgetWidth-126,48,32);
            PText(budget.transform,"BudgetUnit","万円",budgetWidth-58,2,46,48,16,PlanGray);
            BudgetGainEffect((RectTransform)budget.transform);
            if(statChanges[0]<0)
            {
                int previous=State.budget-statChanges[0];
                PCard(budget.transform,"BudgetFlowTrack",16,56,budgetWidth-32,4,PlanTrack,12,false);
                LossTrail(budget.transform,"BudgetLossTrail",16,56,budgetWidth-32,4,previous,State.budget,previous);
                PCard(budget.transform,"BudgetFlowFill",16,56,(budgetWidth-32)*Mathf.Clamp01(State.budget/(float)Math.Max(1,previous)),4,PlanBlue,12,false);
                Hover(budget,"今回の支出前 "+previous+"万円 → "+State.budget+"万円");
            }
            var work=PButton(screen,"Stat_1","",workX,24,workWidth,64,()=>StatusDetail(1),new Color(1,1,1,.92f),PlanInk,20);
            // ラベルと最初のコマの間を12px空ける。残数・回復量は押したときの詳細へ。
            PText(work.transform,"CapacityTitle","工数",16,0,36,64,14,PlanGray);
            for(int j=0;j<slots;j++)
            {
                var token=PCard(work.transform,"WorkToken"+j,64+j*30,15,22,34,j<State.capacity?PlanBlue:PlanTrack,12,false);
                token.GetComponent<Image>().pixelsPerUnitMultiplier=12f/7;
                token.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                PImage(token,"TokenBase",null,0,29,22,5,j<State.capacity?Hex("2381c9"):Hex("c7d0e0"));
            }
            var menu=PButton(screen,"Menu","",1512,24,64,64,Menu,new Color(1,1,1,.92f),PlanInk,20);
            PImage(menu.transform,"MenuIcon",PlanningArt.menu,18,18,28,28);
        }
        private void PlanningCompany()
        {
            var left=PCard(screen,"CompanyGrowth",24,108,312,612);
            PText(left,"CompanyTitle","会社の力",18,18,178,30,20);
            int[] values={State.stability,State.Organization,State.trust,State.culture,State.Preparedness,State.Resilience};
            string[] labels={"業務の安定","チームの力","経営の信頼","相談文化","備え","立て直す力"};
            Color[] colors={PlanMint,PlanBlue,PlanPurple,PlanPink,PlanBlue,PlanBlue};
            var total=PCard(left,"CompanyRankPill",224,22,70,24,PlanInk,12,false);
            PText(total,"CompanyRank","総合 "+PlanningRank(values.Sum()/6),0,0,70,24,13,Color.white,true,true);
            for(int j=0;j<6;j++)
            {
                int metric=j;string id=j==0?"Stat_2":j==2?"Stat_4":j==3?"Stat_3":"CompanyMetric"+j;
                var row=PButton(left,id,"",18,57.6f+j*68,276,58,()=>PlanningMetric(metric),Hex("f3f6fb"),PlanInk,16);
                var rank=PCard(row.transform,"RankBadge",12,9,40,40,RankColor(values[j]),12,false);KitBadge(rank,RankColor(values[j]));
                RankChangeEffect(PText(rank,"RankValue",PlanningRank(values[j]),0,0,40,40,26,Color.white,true,true),rank.GetComponent<Image>(),rankBefore==null?values[j]:rankBefore[j],rankAfter==null?values[j]:rankAfter[j]);
                PText(row.transform,"MetricTitle",labels[j],62,7,150,28,16);
                PCard(row.transform,"Track",62,37,150,6,PlanTrack,12,false);
                if(j==0)LossTrail(row.transform,"StabilityLossTrail",62,37,150,6,State.stability-statChanges[2],State.stability,100);
                if(values[j]>0)
                {
                    var fill=PCard(row.transform,"Fill",62,37,150*values[j]/100f,6,colors[j],12,false);
                    int delta=j==2?statChanges[4]:j==3?statChanges[3]:0;
                    if(statEffectPending&&delta>0&&Application.isPlaying){var grow=fill.gameObject.AddComponent<OpsStatGaugeGrow>();grow.Owner=this;grow.From=150*Mathf.Max(0,values[j]-delta)/100f;grow.To=150*values[j]/100f;}
                }
                if(j==0)DangerGauge(row.transform,"StabilityDanger",62,37,150*values[j]/100f,6);
                PText(row.transform,labels[j]+"Value",values[j].ToString(),217,0,47,58,24,null,true,true);
            }
            rankBefore=rankAfter=null;
            RankProgressStrip(left,18,475,276);
            int spare=Mathf.Clamp(100-State.fatigue,0,100); Color tone=spare>=70?PlanMint:spare>=40?PlanBlue:Hex("c23a60");
            var rest=PButton(left,"Stat_5","",18,520,276,74,()=>StatusDetail(5),Hex("e9fbf5"),PlanInk,16);
            PImage(rest.transform,"MoraleIcon",PlanningArt.morale,12,11,30,30);
            PText(rest.transform,"SpareTitle","チームの余力",52,10,140,30,15);
            PText(rest.transform,"StatHint5",spare>=70?"好調":spare>=40?"ふつう":"要休息",201,10,63,30,15,tone);
            PCard(rest.transform,"SpareTrack",12,50,252,12,Hex("c8ece4"),12,false);
            if(spare>0)PCard(rest.transform,"SpareFill",12,50,252*spare/100f,12,tone,12,false);
            Hover(rest,"余力 "+spare+" / 100（疲労 "+State.fatigue+"）");
        }
        private void PlanningMetric(int metric)
        {
            if(metric==0){StatusDetail(2);return;} if(metric==2){StatusDetail(4);return;} if(metric==3){StatusDetail(3);return;}
            Dialog(metric==1?"チームの力":metric==4?"備え":"立て直す力",metric==1?"相談文化・経営の信頼・チームの余力（100−疲労）の平均。\n\n運用チームの習熟・支援方針はメニューから確認できます。":metric==4?"資産台帳・多要素認証・更新運用・監視・分離の導入段階で増えます。\n\n今月の出来事に対する効果は、導入計画の比較で確認できます。":"分離バックアップ・復元訓練・冗長化・引継ぎ手順で増えます。\n\n対応効果は事件の種類と設備の連携によって変わります。",420);
        }
        private void PlanningConsultation()
        {
            var card=PCard(screen,"ConsultationCard",1268,108,308,237.2f);
            PImage(card,"ConsultationRibbon",PlanningArt.ribbon,0,0,225,38,PlanPink);
            PText(card,"ConsultationHeading","今月の相談",20,0,190,38,15,Color.white);
            var caller=PCard(card,"CallerBadge",20,66,50,50,PlanInk,28,false);
            string person=State.Current.person.Split('・')[0].Trim(); PText(caller,"CallerRole",person,2,0,46,50,17,Hex("ffd23f"),true,true);
            PText(card,"CaseTitle",State.Current.title.Replace("、","、\n"),82,58,206,68,24);
            string category=State.CurrentProfile==null?"今月の相談":State.CurrentProfile.category;
            var chip=PCard(card,"CategoryChip",20,142,114,25,Hex("ffe3ec"),12,false);
            PText(chip,"CategoryText",category,5,0,104,25,13,Hex("c23a60"),true,true);
            var reward=PCard(card,"RewardChip",140,142,148,25,Hex("e3f2ff"),12,false);
            PText(reward,"RewardText","達成で 信頼+3",4,0,140,25,13,Hex("1f6fb0"),true,true);
            string near=MissionNearText();if(near!="")PText(card,"MissionNear",near,20,126,268,16,12,Hex("1a7c63"));
            var ring=PCard(card,"ConsultationPulse",20,177.2f,268,52,new Color(1,.435f,.569f,.4f),16,false);ring.pivot=new Vector2(.5f,.5f);ring.anchoredPosition+=new Vector2(134,-26);Motion(ring,"pulse",1.8f);
            var talk=PButton(card,"ConsultationDetails","話を聞く",20,177.2f,268,52,PlanningBriefDialog,PlanPink,Color.white,16,Hex("d94a70"));Shine(talk.transform,268,52);
            var goals=PCard(screen,"YearGoals",1268,359.2f,308,122.4f,null,20);
            PText(goals,"GoalsTitle","今年の目標",18,10,260,24,15,PlanGray);
            string[] names={"戻せることを確かめる","相談が集まる職場"}; int[] ids={0,2};
            string[] progress={(State.Level("backup")>0?1:0)+(State.Level("drill")>0?1:0)+"/2",State.culture+"/65"};
            for(int j=0;j<2;j++)
            {
                int goal=ids[j];var b=PButton(goals,"Goal"+goal,"",14,40+j*36,280,30,()=>GrowthPlan(goal),new Color(1,1,1,0),PlanInk,16);
                PImage(b.transform,"GoalStar",j==0||State.milestones.Contains("相談が集まる職場")?PlanningArt.star:PlanningArt.starMuted,4,2,26,26);
                PText(b.transform,"GoalLabel",names[j],40,0,200,30,16,j==0?PlanInk:PlanGray);
                PText(b.transform,"GoalProgress",progress[j],231,0,49,30,15,Hex("7c879c"),true,true);
                bool last=j==0?(State.Level("backup")>0?1:0)+(State.Level("drill")>0?1:0)==1:State.Level("education")>0&&State.culture<65&&State.culture+7>=65&&State.ActionBlock("listen")=="";
                if(last){b.transform.Find("GoalProgress").GetComponent<RectTransform>().sizeDelta=new Vector2(49,20);PText(b.transform,"GoalNear","あと1手",216,20,64,14,11,Hex("1a7c63"),true,true);}
            }
            PButton(screen,"AdvanceMonth","月を進める ▶",1268,660,308,60,AdvancePlanning,PlanInk,Color.white,20,Hex("0c1226"));
        }
        private void AdvancePlanning()
        {
            if(State.capacity==0){BeginIncident();return;}
            var d=Dialog("工数を残して進みますか？","残り "+State.capacity+" 工数は翌月に繰り越せません。\n調査・対話・改善・休息に使うこともできます。",360);
            Button(d,"ConfirmAdvance","この計画で進む",32,290,420,48,BeginIncident,Accent);
        }
        private void PlanningNavigator()
        {
            var stage=screen.Find("OfficeStage");
            var character=Rect(stage,"PlanningCharacter",-64,363,347,430);
            Portrait(character,"NavigatorPortrait",0,0,347,430,State.fatigue>=70?"pose_exhausted":"pose_fists");
            var speech=PCard(screen,"Navigator",600,580,420,129.2f,Color.white,24);
            PImage(speech,"SpeechTail",PlanningArt.tail,-13,36,14,20);
            SpeechName(speech);
            var line=PText(speech,"NavigatorSpeech",State.Current.person.Split('・')[0].Trim()+"から相談が来てるよ！\n今月の備え、一緒に確認しよう。",20,22,380,85,19,null,false);
            line.fontStyle=FontStyles.Bold;
        }
        private void PlanningActions()
        {
            string[] ids={"audit","listen","map","rest"},titles={"調べる","話す","優先順位","休む"},effects={"見積もり精度 UP","相談文化 +7","信頼 +4","余力 +18"};
            Sprite[] icons={PlanningArt.audit,PlanningArt.listen,PlanningArt.map,PlanningArt.rest};Color[] colors={Hex("1f6fb0"),Hex("c23a60"),Hex("6f5fb0"),Hex("1a7c63")};
            for(int j=0;j<5;j++)
            {
                bool upgrade=j==4;string id=upgrade?"OpenProjects":"Action_"+ids[j], action=upgrade?"":ids[j];float w=upgrade?204.4f:165.9f;
                string block=upgrade?"":State.ActionBlock(action);
                var b=PButton(screen,id,"",660+j*177.9f,740,w,144,upgrade?(Action)(()=>OpenTab(1)):(()=>ChooseAction(action)),upgrade?Hex("ffc02e"):Color.white,PlanInk,20,upgrade?Hex("d18a00"):Hex("c7d0e0"),block=="");
                b.GetComponent<OpsButtonFeedback>().LiftOnFocus=true;
                if(upgrade)Shine(b.transform,w,144);
                PImage(b.transform,"ActionIcon",upgrade?PlanningArt.upgrade:icons[j],(w-44)/2,22,44,44);
                PText(b.transform,"ActionTitle",upgrade?"設備を導入":titles[j],8,71,w-16,30,20,upgrade?Hex("4a3200"):PlanInk,true,true);
                int count=Enumerable.Range(0,OpsCatalog.Projects.Length).Count(i=>State.UpgradeBlock(i)=="");
                PText(b.transform,"ActionEffect",upgrade?"導入できる "+count+"件":block!=""?block:effects[j],6,107,w-12,22,13,upgrade?Hex("5c4000"):colors[j],true,true);
                // 計画行動に社員の追加効果はないため、経験獲得を「支援」として表示しない。
                if(!upgrade) Hover(b,titles[j]+" / 1工数");
            }
        }
        private void PlanningOverlay()
        {
            screen.Find("AdvanceMonth").name="AdvanceFromDashboard";
            modal=Box(screen,"ModalBlocker",0,0,1600,900,new Color(.02f,.025f,.035f,.72f));
            var p=Box(modal,"PlanningDetails",501,28,598,844,Panel,true); Reveal(p);
            WindowHeader(p,tab==1?"設備・運用の導入":"運用ノート","PLANNING",598);
            var close=p.Find("KitHeader/HeaderClose").GetComponent<Button>(); close.name="ClosePlanner";
            close.onClick.RemoveAllListeners(); close.onClick.AddListener(()=>OpenTab(0));
            var content=Rect(p,"PlanningContent",0,84,598,744);
            Planning(content); // 既存の導入比較・知識・操作条件をそのまま利用する。
        }
        private void PlanningBriefDialog()
        {
            CloseDialog();
            modal=Box(screen,"ModalBlocker",0,0,1600,900,new Color(.106f,.137f,.251f,.45f));
            PImage(modal,"MissionBackground",PlanningArt.gradient,0,0,1600,900);PImage(modal,"MissionOfficeBlur",PlanningArt.officeBlur,-100,-450,1800,1800,new Color(1,1,1,.45f));
            Box(modal,"MissionShade",0,0,1600,900,new Color(.106f,.137f,.251f,.45f));
            var d=PCard(modal,"MissionBrief",330,60,940,780,Color.white,28);Reveal(d);
            var header=PCard(d,"MissionHeader",0,0,940,64,PlanPink,28,false);KitGradient(header.GetComponent<Image>(),Hex("ff94ae"),PlanPink);
            PImage(header,"MissionLogo",PlanningArt.logoIcon,28,10,44,44);PText(header,"MissionHeading","社内依頼  "+State.Current.name,84,0,650,64,22,Color.white);
            var deadline=PCard(header,"MissionDeadline",764,16,148,32,Color.white,12,false);PText(deadline,"MissionDeadlineText","期限：今月中",0,0,148,32,16,Coral,true,true);
            var caller=PCard(d,"MissionCaller",32,88,72,72,PlanInk,28,false);PText(caller,"MissionRole",State.Current.person.Split('・')[0].Trim(),0,0,72,72,22,Hex("ffd23f"),true,true);
            var quote=PCard(d,"MissionQuote",122,88,786,90,Hex("f3f6fb"),20,false);PText(quote,"MissionPerson",State.Current.person,18,8,750,22,14,PlanGray,false);
            PText(quote,"MissionBoss","「"+State.Current.boss+"」",18,32,750,50,19,PlanInk,false);
            PText(d,"MissionTitle",State.CurrentMission.title,32,196,876,72,34);
            State.MissionProgress(true,out int equipDone,out int equipTotal);State.MissionProgress(false,out int fieldDone,out int fieldTotal);
            PText(d,"MissionProgress","どちらかの道で達成  / 設備 "+equipDone+"/"+equipTotal+"  または現場 "+fieldDone+"/"+fieldTotal,32,280,440,28,15,PlanGray);
            MissionRoute(d,true,32,322);MissionRoute(d,false,478,322);
            PText(d,"MissionRewardsHeading","達成すると",32,548,876,26,15,PlanGray);
            Color[] colors={Hex("fff6d6"),Hex("f0ecfb"),Hex("ffe3ec"),Hex("e3f2ff")};
            string[] rewards={State.MissionBudgetOffer>0?"臨時予算 +"+State.MissionBudgetOffer+"万円":"旧年度：予算加算なし","経営の信頼 +3","ひなたの会話","年間評価 +45"};
            Sprite[] icons={PlanningArt.upgrade,PlanningArt.morale,PlanningArt.listen,PlanningArt.star};
            for(int i=0;i<4;i++)
            {
                var reward=PCard(d,"BriefReward"+i,32+i*222,586,210,94,colors[i],20,false);
                if(i==0){var coin=PCard(reward,"RewardCoin",87,12,36,36,Hex("ffd23f"),28,false);PText(coin,"RewardYen","円",0,0,36,36,16,Hex("7a5a00"),true,true);}
                else if(i==1)IncidentShape(reward,"RewardHeart","heart",87,12,36,36,PlanPurple);
                else PImage(reward,"RewardIcon",icons[i],87,12,36,36);
                PText(reward,"RewardCaption"+i,rewards[i],4,52,202,34,18,null,true,true);
            }
            PButton(d,"OpenEventBrief","題材・根拠",500,280,200,26,EventBriefDialog,new Color(1,1,1,0),PlanGray,16);
            PButton(d,"EmployeeConsultation","社員の声",710,280,170,26,ConsultationDetails,new Color(1,1,1,0),PlanGray,16);
            PButton(d,"CloseDialog","閉じる",32,696,286,60,CloseDialog,Hex("eef2f8"),PlanInk,20);
            var accept=PButton(d,"AcceptMission",State.acceptedMissionMonth==State.month?"引き受け済み":"引き受ける",332,696,576,60,()=>{State.acceptedMissionMonth=State.month;Save();CloseDialog();Toast("依頼を確認 / 条件を満たして今月を進めよう",true,OpsCue.Action);SpeakSceneLine(MissionVoiceId(State));},PlanPink,Color.white);Shine(accept.transform,576,60);
            Portrait(modal,"MissionPortrait",1255,455,320,440,"pose_point");
            var hint=PCard(modal,"MissionHint",1290,330,290,104,Color.white,20);SpeechName(hint,"MissionHintName");
            State.MissionProgress(true,out int a,out int at);State.MissionProgress(false,out int b,out int bt);
            string near=State.MissionReady?"もう条件を満たしてるよ！\n今月を進めると達成だね。":at-a<=bt-b?"Aの道は、あと"+(at-a)+"つ。\n費用と工数も確認しよう！":"Bの道は、あと"+(bt-b)+"つ。\n今月の工数を使って確かめよう！";
            PText(hint,"MissionHintLine",near,16,12,258,80,17,null,false);
        }
        private void MissionRoute(Transform parent,bool equipment,float x,float y)
        {
            var mission=State.CurrentMission;Color tone=equipment?PlanBlue:PlanMint;
            var route=PCard(parent,equipment?"MissionRouteA":"MissionRouteB",x,y,430,202,Color.white,20,false);var outline=route.gameObject.AddComponent<Outline>();outline.effectColor=tone;outline.effectDistance=new Vector2(3,-3);
            var tag=PCard(route,"RouteLetter",14,14,32,26,tone,12,false);PText(tag,"RouteLetterText",equipment?"A":"B",0,0,32,26,14,Color.white,true,true);
            PText(route,"RouteHeading",equipment?"設備で示す":"現場で確かめる",54,12,250,30,19);
            State.MissionProgress(equipment,out int done,out int total);PText(route,equipment?"MissionProgressA":"MissionProgressB",done+" / "+total,322,12,94,30,15,tone,true,true);
            string[] ids=equipment?new[]{mission.projectA,mission.projectB}:new[]{mission.actionA,mission.actionB};int row=0;
            foreach(string id in ids.Where(s=>!string.IsNullOrEmpty(s)))
            {
                int index=equipment?OpsCatalog.Index(id):-1;bool complete=equipment?State.Level(id)>0:id=="audit"?State.audited:id=="listen"?State.listened:id=="map"?State.mapped:State.rested;
                var task=PCard(route,"MissionTask"+(equipment?"A":"B")+row,14,54+row*68,402,58,complete?Hex("e3faf3"):Hex("f3f6fb"),16,false);
                var check=PCard(task,"TaskCheck",14,16,26,26,complete?tone:Color.white,12,false);var edge=check.gameObject.AddComponent<Outline>();edge.effectColor=complete?tone:Hex("b8c1d3");edge.effectDistance=new Vector2(3,-3);
                if(complete)IncidentShape(check,"TaskTick","check",5,5,16,16,Color.white);
                string title=equipment?OpsCatalog.Projects[index].name+"を導入":OpsEventCatalog.ActionName(id);
                string cost=complete?"済み":equipment?State.Cost(index)+"万円・"+State.WorkCost(index)+"工数"+(string.IsNullOrEmpty(OpsCatalog.Projects[index].requires)?"":" / 前提設備あり"):"1工数";
                PText(task,"TaskTitle",title,52,4,336,28,16);PText(task,"TaskCost",cost,52,32,336,22,13,complete?tone:PlanGray,false);row++;
            }
        }
        private void PlanningMenuLinks(RectTransform d)
        {
            var nav=PCard(d,"PlanningMenuLinks",-274,0,250,760);
            PText(nav,"LinksTitle","会社の情報",20,25,210,38,24);
            string[] ids={"OpenTeam","OpenTicket","Tab2","Goal1","OpenSituation","OpenGuide","MailTraining"},labels={"運用チーム・育成","日常チケット","運用ノート","仕事を分担する目標","今月の社内事情","遊び方","メール研修 / 1工数"};
            Action[] actions={TeamDialog,TicketDialog,()=>OpenTab(2),()=>GrowthPlan(1),SituationDialog,Guide,OpenMailTraining};
            for(int j=0;j<ids.Length;j++)PButton(nav,ids[j],labels[j],16,92+j*72,218,58,actions[j],Hex("f3f6fb"),PlanInk,16,null,(j!=1||State.Ticket!=null)&&(j!=6||State.MailTrainingBlock==""));
            PText(nav,"SavingNotice",SaveWarning==""?"行動ごとに自動保存":"保存について確認が必要",20,654,210,65,16,PlanGray,false);
        }
    }
}
