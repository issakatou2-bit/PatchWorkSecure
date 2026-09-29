using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private bool resolutionActive;
        private int resolutionCount;
        private OpsEstimate resolutionEstimate;
        private string resolutionLevelUp;
        public bool ResolutionActive => resolutionActive && State != null && State.phase == OpsPhase.Review;
        public bool CanSkipResolution => ResolutionActive && resolutionCount > 1;
        public void SkipResolution() {if(CanSkipResolution){StopVoice();FinishResolution();}}
        private sealed class ResolutionStep { public string name,detail,member; public Color color; public bool equipment,staff,missing; }
        private static string EffectLine(OpsInvestmentEffect effect) =>
            (effect.avoidedLoss>0?"被害 −"+effect.avoidedLoss+"万円":"")+
            (effect.avoidedLoss>0&&effect.avoidedDowntime>0?" / ":"")+
            (effect.avoidedDowntime>0?"停止 −"+effect.avoidedDowntime+"時間":"");
        private List<ResolutionStep> ResolutionSteps(OpsOutcome result)
        {
            int response=Array.IndexOf(ResponseIds,result.response);
            var steps=new List<ResolutionStep>{new ResolutionStep{name="方針："+ResponseTitles[response],detail=State.CurrentProfile==null?ResponseLines[response]:State.ResponseName(result.response),color=PlanInk}};
            if(result.investmentEffects!=null)
                foreach(var effect in result.investmentEffects.Where(e=>e.avoidedLoss>0||e.avoidedDowntime>0))
                    steps.Add(new ResolutionStep{name=OpsCatalog.Projects[OpsCatalog.Index(effect.projectId)].name+" Lv."+effect.level+" 発動",detail=EffectLine(effect),color=PlanMint,equipment=true});
            if(!result.benign&&result.power!=null&&result.power.staff>0)
            {
                string[] parts=result.power.support.Split('：');
                string member=parts.Length>1?parts[0]:"社員";
                string task=parts.Length>1?parts[1].Split(new[]{" / "},StringSplitOptions.None)[0]:"対応を助力";
                steps.Add(new ResolutionStep{name=member+"の支援",detail="抑制力 +"+result.power.staff+" / "+task,color=PlanBlue,staff=true,member=member});
            }
            var missing=result.potentialInvestmentEffects?.Where(e=>e.avoidedLoss>0||e.avoidedDowntime>0)
                .OrderByDescending(e=>e.avoidedLoss).ThenByDescending(e=>e.avoidedDowntime).FirstOrDefault();
            if(missing!=null)steps.Add(new ResolutionStep{name=OpsCatalog.Projects[OpsCatalog.Index(missing.projectId)].name+" 未導入",detail="あれば、"+EffectLine(missing),color=PlanGray,missing=true});
            return steps;
        }
        private void ResolutionScreen()
        {
            IncidentBackdrop(true);var r=State.Latest;var steps=ResolutionSteps(r);
            var rays=IncidentShape(screen,"ResolutionRays","rays",0,-550,1400,1400,new Color(1,1,1,.005f));
            rays.pivot=new Vector2(.5f,.5f);rays.anchoredPosition=new Vector2(700,-150);Motion(rays,"rotate",30);
            PText(screen,"ResolutionHeading","対応の流れ",24,24,480,33,22,Color.white);
            var flow=Rect(screen,"ResolutionFlow",24,69,480,300);
            // 方針は残し、発動が多い時は直近3件を送る。全件を一度ずつ表示する。
            var cutin=IncidentShape(screen,"ResolutionCutin","cutin",380,300,1100,190,PlanPink);
            var cast=cutin.gameObject.AddComponent<Shadow>();cast.effectColor=Hex("d94a70");cast.effectDistance=new Vector2(0,-10);
            var lines=IncidentShape(cutin,"CutinSpeedLines","rays",-50,-500,1200,1200,new Color(1,1,1,.18f));
            cutin.gameObject.AddComponent<RectMask2D>();Motion(lines,"rotate",5);
            var shineMask=IncidentShape(cutin,"CutinShineMask","cutin",0,0,1100,190,Color.white);
            shineMask.gameObject.AddComponent<Mask>().showMaskGraphic=false;Shine(shineMask,1100,190);
            PText(cutin,"CutinCaption","備えが効いた！",60,21,980,30,20,Color.white);
            PText(cutin,"CutinTitle","",60,56,980,77,64,Color.white);
            cutin.Find("CutinTitle").GetComponent<TextMeshProUGUI>().fontSizeMin=32;
            var titleShadow=cutin.Find("CutinTitle").gameObject.AddComponent<Shadow>();titleShadow.effectDistance=new Vector2(0,-5);titleShadow.effectColor=Hex("c23a60");
            PText(cutin,"CutinEffect","",60,139,980,32,20,Hex("7a1f3a"));
            cutin.gameObject.SetActive(false);
            var resources=PCard(screen,"ResolutionResources",560,568,1016,32,new Color(1,1,1,.15f),12,false);
            PText(resources,"ResolutionBudget","予算 "+(State.budget-statChanges[0])+" → "+State.budget+"万円",16,0,232,32,15,Color.white);
            PCard(resources,"BudgetResourceTrack",258,12,214,7,new Color(1,1,1,.2f),12,false);
            int budgetBefore=State.budget-statChanges[0];LossTrail(resources,"ResolutionBudgetLossTrail",258,12,214,7,budgetBefore,State.budget,budgetBefore);
            PCard(resources,"BudgetResourceFill",258,12,214*Mathf.Clamp01(State.budget/(float)Math.Max(1,budgetBefore)),7,PlanBlue,12,false);
            PText(resources,"ResolutionStability","業務の安定 "+(State.stability-statChanges[2])+" → "+State.stability,520,0,238,32,15,Color.white);
            PCard(resources,"StabilityResourceTrack",770,12,230,7,new Color(1,1,1,.2f),12,false);
            LossTrail(resources,"ResolutionStabilityLossTrail",770,12,230,7,State.stability-statChanges[2],State.stability,100);
            PCard(resources,"StabilityResourceFill",770,12,230*State.stability/100f,7,PlanMint,12,false);
            DangerGauge(resources,"ResolutionDanger",770,12,230*State.stability/100f,7);
            var result=PCard(screen,"ResolutionResults",560,610,1016,180,new Color(1,1,1,.96f),24,false);
            var lossNeedle=ResolutionMeter(result,"Loss","被害",r.loss,Math.Max(r.loss,resolutionEstimate.lossMax),22,Hex("ff9f43"),"万円");
            var stopNeedle=ResolutionMeter(result,"Stop","業務停止",r.downtime,Math.Max(r.downtime,resolutionEstimate.stopMax),76,PlanPink,"時間");
            PText(result,"ResolutionForecast","見積もり "+resolutionEstimate.lossMin+"～"+resolutionEstimate.lossMax+"万円・"+resolutionEstimate.stopMin+"～"+resolutionEstimate.stopMax+"時間の中で確定",28,129,574,29,15,PlanGray,false);
            if(r.hasInvestmentComparison)
            {
                var baseline=PCard(result,"ResolutionBaseline",607,127,381,32,Hex("e3faf3"),12,false);
                PText(baseline,"ResolutionBaselineValue","備えなしなら 被害 "+(r.loss+r.avoidedLoss)+"万円",8,0,365,32,15,Hex("1a7c63"),true,true);
            }
            bool good=r.loss==0&&r.downtime<=4;
            var hinata=Rect(screen,"ResolutionHinata",20,500,307,380);Portrait(hinata,"NavigatorPortrait",0,0,307,380,good?"pose_jump":"pose_exhausted");
            var speech=PCard(screen,"ResolutionSpeech",330,520,230,85,Color.white,20,false);
            PImage(speech,"SpeechTail",PlanningArt.tail,-22,20,28,38);
            PText(speech,"ResolutionReaction",good?"やった、\n守れたよ！":r.benign?"正常な操作だったね。":"対応できたね。\n次の備えを考えよう！",16,10,198,65,20,Hex("d94a70"));
            if(CanSkipResolution)PButton(screen,"SkipResolution","演出をスキップ",1320,822,256,48,SkipResolution,Color.white,PlanInk,16);
            PText(screen,"ResolutionComparisonNote","各設備を一つ外した場合との比較。連携があるため、削減値は足し合わせません。",560,798,1016,25,14,Color.white,false);
            if(Application.isPlaying)StartCoroutine(ResolutionRoutine(steps,flow,cutin,lossNeedle,stopNeedle,hinata,good,r));
        }
        private RectTransform ResolutionMeter(Transform parent,string id,string title,int actual,int scale,float y,Color color,string unit)
        {
            PText(parent,"ResolvedLabel_"+id,title,28,y,110,36,18);
            var track=PCard(parent,"ResolvedTrack_"+id,158,y+10,680,16,Hex("e6eaf2"),12,false);
            var fill=PCard(track,"ResolvedGradientClip_"+id,0,0,680,16,Color.white,12,false);fill.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            IncidentShape(fill,"ResolvedGradient_"+id,id=="Loss"?"lossGradient":"stopGradient",0,0,680,16,color);
            var needle=PCard(track,"ResolvedNeedle_"+id,680f*actual/Math.Max(1,scale)-3,-10,6,36,PlanInk,12,false);
            PText(parent,"ResolvedValue_"+id,actual.ToString(),858,y,72,40,30,PlanInk,true,true);
            PText(parent,"ResolvedUnit_"+id,unit,940,y+9,48,29,16,PlanGray);
            return needle;
        }
        private void ResolutionFlow(Transform flow,List<ResolutionStep> steps,int current)
        {
            var supportMap=screen.Find("SupportRoom");if(supportMap!=null)supportMap.gameObject.SetActive(false);
            Clear(flow);int rowIndex=0;
            for(int index=0;index<=current;index++)
            {
                if(index>0&&index<current-2)continue;
                var step=steps[index];float y=rowIndex++*72;
                var row=PCard(flow,"ResolutionStep_"+index,0,y,480,60,step.missing?new Color(1,1,1,.12f):new Color(1,1,1,.95f),16,false);
                if(step.missing)IncidentShape(row,"MissingDashedFrame","dashed",0,0,480,60,new Color(1,1,1,.9f));
                var number=PCard(row,step.staff?"StaffFace_"+step.member:"StepNumber",16,12,36,36,step.color,20,false);
                if(step.staff)
                {
                    PImage(number,"StaffFaceGlyph",PlanningArt.morale,5,3,26,26,Color.white);
                    PText(number,"StaffInitial",step.member.Substring(0,1),20,21,16,15,11,Color.white,true,true);
                    if(index==current){var bounce=number.gameObject.AddComponent<OpsStaffBounce>();bounce.Owner=this;bounce.Member=step.member;RoomSupportMarker(step.member);}
                }
                else PText(number,"StepIndex",step.missing?"?":(index+1).ToString(),0,0,36,36,16,Color.white,true,true);
                PText(row,"StepName",step.name,64,7,378,24,17,step.missing?Color.white:PlanInk);
                PText(row,"StepEffect",step.detail,64,31,395,23,14,step.missing?Hex("e0e6f0"):step.color,false);
                if(index==0)IncidentShape(row,"ChosenCheck","check",438,17,26,26,PlanMint);
            }
        }
        private IEnumerator ResolutionRoutine(List<ResolutionStep> steps,RectTransform flow,RectTransform cutin,RectTransform loss,RectTransform stop,RectTransform hinata,bool good,OpsOutcome outcome)
        {
            bool equipmentSpoken=false;
            float stepDuration=(resolutionCount>1?(ShortenInterruptions?1.15f:2.3f):3.2f)/steps.Count;
            for(int index=0;index<steps.Count;index++)
            {
                var step=steps[index];ResolutionFlow(flow,steps,index);
                cutin.gameObject.SetActive(step.equipment||step.staff);
                if(step.equipment||step.staff)
                {
                    DuckMusic(stepDuration+.25f);
                    cutin.Find("CutinCaption").GetComponent<TextMeshProUGUI>().text=step.staff?"社員が助けてくれた！":"備えが効いた！";
                    cutin.Find("CutinTitle").GetComponent<TextMeshProUGUI>().text=step.name;
                    cutin.Find("CutinEffect").GetComponent<TextMeshProUGUI>().text=step.detail;
                    PlayCue(step.staff?OpsCue.StaffHelp:OpsCue.Prepared);
                    if(step.equipment&&!equipmentSpoken){equipmentSpoken=true;ResolutionVoice("incident_activate");}
                    for(int j=0;j<3;j++)
                    {
                        var spark=PCard(cutin,"CutinSpark"+j,j==0?1000:j==1?1060:40,j==0?-20:j==1?170:200,18,18,j==2?Color.white:IncidentYellow,20,false);
                        Motion(spark,"pulse",stepDuration,j*.08f);
                    }
                }
                else if(index>0&&!step.missing)PlayCue(OpsCue.Action);
                if(step.missing)ResolutionVoice("incident_missing");
                float elapsed=0;bool impact=false;
                while(elapsed<stepDuration)
                {
                    elapsed+=PresentationDeltaTime;
                    float progress=elapsed/stepDuration;
                    float slide=progress<.2f?Mathf.Lerp(-1500,0,progress/.2f):progress>.8f?Mathf.Lerp(0,1700,(progress-.8f)/.2f):0;
                    cutin.anchoredPosition=new Vector2(380+(ReducedMotion?0:slide), -300);
                    if(!impact&&progress>=.2f&&(step.equipment||step.staff)){impact=true;HoldPresentation();hinata.GetComponentInChildren<OpsPortraitMotion>()?.Celebrate();}
                    yield return null;
                }
                for(int j=cutin.childCount-1;j>=0;j--)if(cutin.GetChild(j).name.StartsWith("CutinSpark"))Destroy(cutin.GetChild(j).gameObject);
            }
            cutin.gameObject.SetActive(false);
            var lossFinal=loss.anchoredPosition;var stopFinal=stop.anchoredPosition;
            for(float elapsed=0;elapsed<1.05f;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/.85f);
                // 演出の針だけが動く。値は再抽選せず最初から確定している。
                float sweep=ReducedMotion?0:Mathf.Sin(t*Mathf.PI*5)*(1-t)*180;
                loss.anchoredPosition=new Vector2(Mathf.Clamp(lossFinal.x+sweep,-3,677),lossFinal.y);
                stop.anchoredPosition=new Vector2(Mathf.Clamp(stopFinal.x-sweep*.8f,-3,677),stopFinal.y);
                yield return null;
            }
            loss.anchoredPosition=lossFinal;stop.anchoredPosition=stopFinal;
            Feedback(good?OpsCue.Success:outcome.loss>0?OpsCue.Damage:OpsCue.Action);
            if(good)hinata.GetComponentInChildren<OpsPortraitMotion>()?.Celebrate();
            yield return new WaitForSecondsRealtime(.75f);
            FinishResolution();
        }
        private void FinishResolution()
        {
            if(!ResolutionActive)return;
            // 自動で月報に移っただけでは全文を切らない。次の操作なら即時停止する。
            carryResolutionVoice=speakingPriority==5&&(VoicePending||PortraitVoicePlaying||Time.unscaledTime<voiceBusyUntil||followingVoice.Count>0)&&LastReactionId.StartsWith("incident_");
            resolutionActive=false;Render();
            if(Application.isPlaying)StartCoroutine(ReviewGrowthFeedback(State.Latest,resolutionLevelUp));
        }
    }
}
