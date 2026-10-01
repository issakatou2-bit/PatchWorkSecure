using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private bool storyAnnualDetails;
        private TextMeshProUGUI StoryText(Transform parent,string id,string value,float x,float y,float w,float h,float size,Color? color=null,bool center=false,bool body=false)
        {var t=PText(parent,id,value,x,y,w,h,size,color,!body,center);t.fontStyle=FontStyles.Bold;t.enableAutoSizing=false;t.overflowMode=TextOverflowModes.Overflow;return t;}
        private void StoryCategory(Transform parent,string id,string value,float x,float y,float width,Color color)
        {var t=StoryText(parent,id,value,x,y,width,24,12,color);t.characterSpacing=3.8f;}
        private Button StoryButton(string id,string label,float x,float y,float w,Action action,bool primary=false,bool blue=false)
        {
            var b=PButton(screen,id,label,x,y,w,62,action,primary?PlanPink:blue?PlanBlue:Color.white,primary||blue?Color.white:PlanInk);
            TitleButtonStyle(b,primary);var t=b.GetComponentInChildren<TextMeshProUGUI>();t.fontSize=t.fontSizeMax=20;t.fontSizeMin=16;t.alignment=TextAlignmentOptions.Midline;
            if(blue)KitGradient(b.GetComponent<Image>(),Hex("7fc4ff"),Hex("2f93dc"));
            if(primary||blue)Shine(b.transform,w,62);return b;
        }
        private void StoryBackground(bool failed=false,bool dark=false)
        {
            IncidentShape(screen,"StoryBackground",dark?"story-factor-background":failed?"story-fail-background":"story-clear-background",0,0,1600,900,Color.white);
            if(failed)return;
            // Linear色空間で暗い背景へ重ねるとCSSより明るくなるため、因子画面だけ濃度を合わせる。
            var rays=IncidentShape(screen,"StoryRays","rays",0,0,dark?1800:1600,dark?1800:1600,new Color(1,.824f,.247f,dark?.02f:.16f));
            rays.pivot=new Vector2(.5f,.5f);rays.anchoredPosition=new Vector2(dark?800:300,dark?-430:-250);Motion(rays,"rotate",dark?60:40);
            if(!dark)StoryConfetti();
        }
        private void StoryConfetti()
        {
            Color[] colors={PlanPink,PlanBlue,Hex("ffd23f"),PlanMint};float[] x={160,430,980,1380},period={7,9,8,10},delay={0,1,2,.5f};
            for(int i=0;i<4;i++){var r=PCard(screen,"StoryConfetti"+i,x[i],0,12,18,colors[i],12,false);Motion(r,"petal",period[i],delay[i]);}
        }
        private void StoryRoad(float x,float y,bool fail=false)
        {
            for(int i=0;i<OpsCatalog.StoryYears;i++)
            {
                var record=Story.records.FirstOrDefault(r=>r.year==i+1);bool done=record!=null&&record.goalMet,bad=record!=null&&!record.goalMet;
                bool next=!Story.finished&&i+1==Story.year+1;
                Color bg=done?Hex("fff6d6"):bad?Hex("ffe9ee"):next?Hex("e3f2ff"):Hex("eef2f8"),fg=done?Hex("7a5a00"):bad?Hex("c23a60"):next?Hex("1f5f99"):Hex("9aa3b5");
                var card=PCard(screen,"StoryYear"+(i+1),x+i*196,y,150,92,bg,20);
                KitGradient(card.GetComponent<Image>(),bg,done?Hex("ffe7a3"):bad?Hex("ffc9d6"):next?Hex("bfe2ff"):bg);
                var outline=card.gameObject.AddComponent<Outline>();outline.effectColor=Color.white;outline.effectDistance=new Vector2(3,-3);
                if(next){var pulse=PCard(card,"NextYearPulse",-3,-3,156,98,new Color(.247f,.663f,.961f,.3f),20,false);pulse.SetAsFirstSibling();Motion(pulse,"pulse",1.6f);}
                StoryText(card,"StoryYearName"+i,(i+1)+"年目",0,8,150,24,13,fg,true);
                StoryText(card,"StoryYearRank"+i,record?.rank??(fail?"—":OpsCatalog.StoryGoals[i]+"<size=16>以上</size>"),0,31,150,32,30,fg,true);
                StoryText(card,"StoryYearGoal"+i,done?"目標"+OpsCatalog.StoryGoals[i]+" 達成":bad?"目標"+OpsCatalog.StoryGoals[i]+" に届かず":fail?"次の挑戦で":next?"今年の目標":"最終目標",0,65,150,20,13,fg,true);
                if(i<2){var bar=PCard(screen,"StoryRoadBar"+i,x+150+i*196,y+42,46,8,done?Hex("ffd23f"):Hex("dfe5ef"),12,false);if(done)KitGradient(bar.GetComponent<Image>(),Hex("ffd23f"),Hex("ff9f43"),true);}
            }
        }
        private static int StoryGoalPoints(string goal)=>goal=="B"?OpsCatalog.AnnualB:OpsCatalog.AnnualA;
        private void StorySpeech(string line,float x,float y,float w,float h)
        {
            var bubble=PCard(screen,"StorySpeech",x,y,w,h,Color.white,20);
            var tag=PCard(screen,"StorySpeechTag",x+16,y-13,60,24,PlanPink,12,false);StoryText(tag,"StorySpeechName","ひなた",0,0,60,24,13,Color.white,true);
            StoryText(bubble,"StorySpeechText",CaptionsEnabled?line:"",18,14,w-36,h-28,18,null,false,true);
        }
        private void ReviewStoryAnnual(){storyAnnualDetails=true;Render();}
        private void ReturnStoryOutcome(){storyAnnualDetails=false;Render();}
        private void StoryRenewScreen()
        {
            StoryBackground();int year=Story.year,next=year+1;
            StoryCategory(screen,"StoryCategory","YEAR "+year+" CLEAR",60,32,450,PlanPink);
            StoryText(screen,"EndingTitle",year+"年目　目標達成！",60,52,560,66,42);StoryRoad(640,36);
            var rank=PCard(screen,"RankBadge",120,170,300,300,PlanPink,28);rank.localEulerAngles=new Vector3(0,0,6);
            rank.pivot=new Vector2(.5f,.5f);rank.anchoredPosition+=new Vector2(150,-150);
            KitGradient(rank.GetComponent<Image>(),Hex("ff94ae"),Hex("f45a80"));rank.GetComponent<Image>().pixelsPerUnitMultiplier=PlanningArt.round28.border.x/56;
            var edge=rank.gameObject.AddComponent<Outline>();edge.effectColor=Color.white;edge.effectDistance=new Vector2(8,-8);
            IncidentShape(rank,"RankStitch","rounded-dashed",14,14,272,272,new Color(1,1,1,.7f)).GetComponent<OpsIncidentGraphic>().StrokeWidth=5;Reveal(rank,0,true);
            StoryText(rank,"RankHeading","運用ランク",0,45,300,40,22,Color.white,true);
            StoryText(rank,"CompanyRank",State.RankCode,0,83,300,205,State.RankCode=="SS"?130:170,Color.white,true);
            StoryText(screen,"ScoreHeading","年間得点",80,500,380,26,15,PlanGray,true);
            var number=PText(screen,"AnnualScoreValue",State.AnnualScore.ToString("N0")+"<size=22>点</size>",80,528,380,70,60,PlanInk,true,true);number.enableAutoSizing=false;number.overflowMode=TextOverflowModes.Overflow;
            int threshold=StoryGoalPoints(Story.Goal);var achieved=PCard(screen,"StoryGoalBand",80,602,390,30,Hex("e3faf3"),16,false);
            StoryText(achieved,"StoryGoalResult","目標 "+Story.Goal+"以上（"+threshold.ToString("N0")+"点）を "+(State.AnnualScore-threshold).ToString("N0")+"点 上回った",8,0,374,30,13,Hex("1a7c63"),true);
            var panel=PCard(screen,"StoryCarry",520,170,1020,470,Color.white,24);
            StoryCategory(panel,"CarryCategory","NEXT YEAR",24,20,114,PlanBlue);StoryText(panel,"CarryHeading",next+"年目へ持っていくもの",138,17,450,34,21);
            StoryText(panel,"CarryThreat",next+"年目は攻撃が手強くなる（脅威 +"+OpsCatalog.StoryPressures[next-1]+"）",652,22,344,24,14,PlanGray,false,true);
            StoryCarryMetrics(panel);
            StoryText(panel,"CarryEquipmentHeading","設備",24,220,42,27,16);var note=PCard(panel,"CarryEquipmentNote",66,220,690,27,Hex("e6f3ff"),12,false);
            StoryText(note,"CarryEquipmentNoteText","新しい手口が出たので、Lv2の設備は見直してLv1から。更新すればLv2へ戻せる",10,0,670,27,13,Hex("1f5f99"),false,true);
            StoryEquipmentGrid(panel);
            Portrait(screen,"StoryPortrait",1300,600,260,300,"pose_peace");StorySpeech(year+"年目、おつかれさま！\n守り方も、毎年見直すものだよ。",930,668,360,88);
            StoryButton("StoryAnnualReview",year+"年目を振り返る",120,780,310,ReviewStoryAnnual);
            StoryButton("NextStoryYear",next+"年目をはじめる",446,780,434,()=>NextStoryYear(),true);
        }
        private void StoryCarryMetrics(RectTransform panel)
        {
            string[] names={"予算","社員の経験","相談文化","経営の信頼"};int budget=OpsCatalog.StoryInitialBudget+Math.Max(0,State.budget),trust=(State.trust+OpsCatalog.StoryTrustBaseline)/OpsCatalog.StoryTrustDivisor;
            string[] values={budget+"<size=15>万円</size>",OpsGrowthCatalog.StaffNames[0]+" Lv"+State.StaffLevel(0)+"　"+OpsGrowthCatalog.StaffNames[1]+" Lv"+State.StaffLevel(1)+"\n"+OpsGrowthCatalog.StaffNames[2]+" Lv"+State.StaffLevel(2),State.culture.ToString(),trust.ToString()};
            string[] hints={"毎年の"+OpsCatalog.StoryInitialBudget+"万円＋残りの"+Math.Max(0,State.budget)+"万円を全額持ち越し","そのまま","そのまま","新しい経営計画で、改めて築く"};
            for(int i=0;i<4;i++)
            {
                var box=PCard(panel,"CarryMetric"+i,24+i*246,65,234,140,Color.white,24,false);foreach(var outline in box.GetComponents<Outline>())DestroyImmediate(outline);var border=box.gameObject.AddComponent<Outline>();border.effectColor=Hex("e6ebf3");border.effectDistance=new Vector2(2,-2);
                StoryText(box,"CarryLabel"+i,names[i],14,12,206,22,13,PlanGray);StoryText(box,"CarryValue"+i,values[i],14,37,206,i==1?50:42,i==1?15:28);
                StoryText(box,"CarryHint"+i,hints[i],14,i==1?92:82,206,48,12,i==3?PlanGray:Hex("1a7c63"),false,true);
            }
        }
        private void StoryEquipmentGrid(RectTransform panel)
        {
            string[] order={"backup","drill","inventory","mfa","runbook","automation","education","monitor","segment","patch","redundancy","zeroTrust","edr","threatSharing","csirt"};
            var available=order.Where(id=>State.EquipmentAvailable(OpsCatalog.Index(id))).ToArray();
            var installed=available.Select(id=>OpsCatalog.AllProjects[OpsCatalog.Index(id)]).Where(p=>State.Level(p.id)>0).ToArray();int count=installed.Length+(installed.Length<available.Length?1:0);Transform grid=panel;
            if(count>8)
            {
                var scroll=Rect(panel,"CarryEquipmentScroll",24,244,972,190);var view=PImage(scroll,"Viewport",null,0,0,972,190);view.GetComponent<Image>().raycastTarget=true;view.gameObject.AddComponent<Mask>().showMaskGraphic=false;
                var content=Rect(view,"Content",0,0,972,Mathf.Ceil(count/4f)*76+10);var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=layout.childControlHeight=layout.childForceExpandWidth=layout.childForceExpandHeight=false;layout.spacing=10;layout.padding=new RectOffset(0,0,10,0);
                var sr=scroll.gameObject.AddComponent<ScrollRect>();sr.viewport=view;sr.content=content;sr.horizontal=false;sr.vertical=true;sr.movementType=ScrollRect.MovementType.Clamped;
                for(int r=0;r<Mathf.CeilToInt(count/4f);r++)Rect(content,"EquipmentRow"+r,0,0,972,66);grid=content;
            }
            for(int i=0;i<count;i++)
            {
                bool missing=i>=installed.Length;var project=missing?null:installed[i];bool renew=!missing&&State.Level(project.id)>1;
                Transform parent=count>8?grid.GetChild(i/4):grid;float x=count>8?i%4*246:24+i%4*246,y=count>8?0:254+i/4*76;
                var eq=PCard(parent,"CarryEquipment"+i,x,y,236,66,renew?Hex("e6f3ff"):Color.white,16,false);var border=eq.gameObject.AddComponent<Outline>();border.effectColor=renew?Hex("7fc4ff"):Hex("e6ebf3");border.effectDistance=new Vector2(2,-2);
                if(missing){eq.gameObject.AddComponent<CanvasGroup>().alpha=.45f;StoryText(eq,"MissingEquipment","未導入 "+(available.Length-installed.Length)+"つ",64,10,150,46,14);StoryText(eq,"MissingIcon","＋",12,14,38,38,20,PlanGray,true);continue;}
                string[] glyph={"戻","練","台","認","手","自","教","監","分","更","代","ゼ","E","情","C"};string[] colors={"2ec4a0","ffb020","8b7cf6","3fa9f5","ff6f91","f0803c","2f93dc","3fa9f5","3fa9f5","3fa9f5","2ec4a0","3fa9f5","3fa9f5","3fa9f5","ff6f91"};int oi=Array.IndexOf(order,project.id);
                var icon=PCard(eq,"EquipmentIcon"+i,12,14,38,38,Hex(colors[oi]),12,false);StoryText(icon,"EquipmentGlyph"+i,glyph[oi],0,0,38,38,15,Color.white,true);
                // Lv1は短い札なので、設備名へ幅を返す。モックのflexと同じ並び。
                StoryText(eq,"EquipmentName"+i,project.name,60,8,renew?113:128,50,14,null,false,true);StoryText(eq,"EquipmentLevel"+i,renew?"Lv2→1":"Lv1",renew?172:190,14,renew?60:34,38,15,renew?Hex("1f75b8"):PlanInk,true);
                if(renew){var badge=PCard(eq,"EquipmentReview"+i,177,-10,51,20,PlanBlue,12,false);StoryText(badge,"EquipmentReviewLabel"+i,"見直し",0,0,51,20,11,Color.white,true);}Reveal(eq,i*.05f);
            }
        }
        private void StoryFailScreen()
        {
            StoryBackground(true);StoryCategory(screen,"StoryCategory","CHALLENGE RECORD",60,32,450,PlanGray);
            StoryText(screen,"EndingTitle","挑戦の記録　"+Story.year+"年目で終了",60,52,590,66,42);StoryRoad(640,36,true);
            var panel=PCard(screen,"StoryFailure",80,170,700,470,Color.white,24);foreach(var outline in panel.GetComponents<Outline>())DestroyImmediate(outline);var whiteEdge=panel.gameObject.AddComponent<Outline>();whiteEdge.effectColor=Color.white;whiteEdge.effectDistance=new Vector2(3,-3);StoryCategory(panel,"FailureCategory","WHAT HAPPENED",26,22,300,PlanPink);
            StoryText(panel,"FailureHeading",Story.year+"年目"+(State.IsClear?"あと少しだった":"運営を続けられなかった"),26,46,648,40,21);
            var score=StoryText(panel,"FailureScore",State.AnnualScore.ToString("N0"),26,93,180,64,56,Hex("c23a60"));score.textWrappingMode=TextWrappingModes.NoWrap;
            int threshold=StoryGoalPoints(Story.Goal);float marker=432*.897f,fill=marker*Mathf.Clamp01(State.AnnualScore/(float)threshold);
            PCard(panel,"StoryGoalTrack",210,103,432,16,Hex("eef2f8"),12,false);if(fill>0){var f=PCard(panel,"StoryGoalFill",210,103,fill,16,PlanPink,12,false);KitGradient(f.GetComponent<Image>(),Hex("ffb3c4"),PlanPink,true);}
            PImage(panel,"StoryGoalMarker",null,210+marker,99,3,24,PlanInk);
            StoryText(panel,"StoryGoalResult",!State.IsClear&&State.AnnualScore>=threshold?"得点は目標内 / 会社の運営継続も必要だった":"目標 "+Story.Goal+"（"+threshold.ToString("N0")+"点）まで あと"+Math.Max(0,threshold-State.AnnualScore).ToString("N0")+"点",210,125,446,30,13,PlanGray);
            var worst=State.history.OrderByDescending(r=>r.loss+r.downtime).FirstOrDefault();
            string painful=worst==null?"事件の対応記録なし":OpsCatalog.Months[worst.month].name+" "+(worst.eventTitle??OpsCatalog.Months[worst.month].@event)+"　被害 "+worst.loss+"万円・停止 "+worst.downtime+"時間";
            var potential=State.history.Where(r=>r.potentialInvestmentEffects!=null).SelectMany(r=>r.potentialInvestmentEffects.Select(e=>new{record=r,effect=e})).OrderByDescending(p=>p.effect.avoidedLoss*7+p.effect.avoidedDowntime*4).FirstOrDefault();
            string next=potential==null?"個別比較の記録なし / 次の計画で備えを確認":OpsCatalog.AllProjects[OpsCatalog.Index(potential.effect.projectId)].name+"（"+OpsCatalog.Months[potential.record.month].name+"の個別比較：停止 −"+potential.effect.avoidedDowntime+"時間・被害 −"+potential.effect.avoidedLoss+"万円）";
            var best=State.history.Where(r=>r.minigameRecorded&&!r.delegated).GroupBy(r=>StoryMinigameName(r)).OrderByDescending(g=>g.Average(r=>r.minigameScore)).FirstOrDefault();
            string good="山場 "+State.history.Count(r=>r.peakGoalRecorded)+"つ中 "+State.PeakMedals+"つ達成"+(best==null?"・自分で遊んだミニゲームは未記録":"・"+best.Key+" 平均"+Mathf.RoundToInt((float)best.Average(r=>r.minigameScore))+"点");
            StoryFailureRow(panel,"WorstMonth","一番痛かった月",painful,166,Hex("ffe9ee"),Hex("c23a60"));
            StoryFailureRow(panel,"PotentialEquipment","次に効きそうな備え",next,226,Hex("e6f3ff"),Hex("1f75b8"));
            StoryFailureRow(panel,"GoodWork","よくできた",good,286,Hex("fff6d6"),Hex("7a5a00"));
            var reward=PCard(screen,"StoryFactorReward",820,170,700,470,PlanInk,24);KitGradient(reward.GetComponent<Image>(),PlanInk,Hex("2b3a5e"));
            var rewardEdge=reward.gameObject.AddComponent<Outline>();rewardEdge.effectColor=Color.white;rewardEdge.effectDistance=new Vector2(3,-3);
            StoryCategory(reward,"FactorRewardCategory","FACTOR GET",26,22,260,Hex("ffd23f"));StoryText(reward,"FactorRewardHeading","因子を1つ持ち帰れる",26,46,648,40,21,Color.white);
            StoryText(reward,"FactorRewardHint","次の挑戦は、選んだ設備を最初からLv1で始められる。\n届いた年が進むほど、候補の星が増える。",26,86,648,58,14,new Color(1,1,1,.85f),false,true);
            var candidates=Story.FactorCandidates(Career);
            for(int i=0;i<candidates.Length;i++)
            {
                var c=candidates[i];var card=PCard(reward,"FactorPreview"+i,26+i*220,153,206,116,new Color(1,1,1,.1f),20,false);
                if(i==0)IncidentShape(card,"FactorPreviewBorder","story-outline",0,0,206,116,new Color(1,.824f,.247f,.6f));
                StoryStars(card,"FactorPreviewStars"+i,c.stars,103-c.stars*12,18);
                StoryText(card,"FactorPreviewName"+i,OpsCatalog.AllProjects[OpsCatalog.Index(c.id)].name,12,48,182,32,18,Color.white,true);
                StoryText(card,"FactorPreviewReason"+i,c.reason,12,84,182,24,12,new Color(1,1,1,.8f),true);
            }
            var ownedHeading=StoryText(reward,"OwnedFactorsHeading","持っている因子",26,292,104,56,13,new Color(1,1,1,.8f));ownedHeading.textWrappingMode=TextWrappingModes.NoWrap;
            for(int i=0;i<OpsCatalog.StoryFactorSlots;i++)StoryFactorSlot(reward,"OwnedFactor"+i,i<Career.factors.Count?TitleFactorName(Career.factors[i]):"空き",128+i*164,292,154,56,i<Career.factors.Count);
            Portrait(screen,"StoryPortrait",1310,610,250,290,"pose_fists");StorySpeech("ここまで来られたのは本物だよ。\n次は山場に備えて、もう一回！",920,676,380,88);
            StoryButton("BackHome","タイトルへ",80,780,326,StoryTitleOrFactor);
            StoryButton("StoryRecord","因子を選んで次の挑戦へ",422,780,458,OpenStoryFactors,true);
        }
        private void StoryFailureRow(Transform parent,string id,string title,string value,float y,Color bg,Color fg)
        {
            var row=PCard(parent,id,26,y,648,50,bg,16,false);var chip=PCard(row,id+"Chip",14,9,160,32,Color.white,16,false);
            StoryText(chip,id+"Label",title,0,0,160,32,14,fg,true);StoryText(row,id+"Value",value,186,0,448,50,14,null,false,true);
        }
        private static string StoryMinigameName(OpsOutcome r)
        {var e=OpsEventCatalog.Event(r.eventId);var p=e==null?null:OpsEventCatalog.Profile(e.profile);string kind=p?.kind??OpsCatalog.Months[r.month].kind;return OpsCatalog.Months[r.month].kind=="identity"||p?.id=="session"||p?.id=="remote"?"多要素認証の関所":p?.id=="bec"||p?.id=="targeted"?"メールの仕分け":kind=="outage"||p?.id=="change"||p?.id=="storage"||p?.id=="service"?"復旧の順番":"感染の封じ込め";}
        private void StoryStars(Transform parent,string id,int count,float x,float y)
        {for(int i=0;i<count;i++)IncidentShape(parent,id+i,"solid-star",x+i*24,y,22,22,Hex("ffd23f"));}
        private RectTransform StoryFactorSlot(Transform parent,string id,string value,float x,float y,float w,float h,bool full)
        {
            var slot=PCard(parent,id,x,y,w,h,full?Hex("fff6d6"):Color.clear,16,false);
            if(full){KitGradient(slot.GetComponent<Image>(),Hex("fff6d6"),Hex("ffe7a3"));var outline=slot.gameObject.AddComponent<Outline>();outline.effectColor=Color.white;outline.effectDistance=new Vector2(3,-3);}
            else {var d=IncidentShape(slot,id+"Dashes","rounded-dashed",2,2,w-4,h-4,new Color(1,1,1,.45f));d.GetComponent<OpsIncidentGraphic>().StrokeWidth=3;}
            StoryText(slot,id+"Text",value,8,0,w-16,h,15,full?Hex("7a5a00"):new Color(1,1,1,.7f),true);return slot;
        }
        private void OpenStoryFactors(){ShowStoryFactors(()=>StartStory(Environment.TickCount));}
        private void StoryTitleOrFactor()
        {if(Story!=null&&Story.finished&&!Story.rewardClaimed){ShowStoryFactors(RenderHome);return;}storyAnnualDetails=false;RenderHome();}
    }
}
