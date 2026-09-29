using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly string[] ResponseIds = { "contain", "scope", "recover" };
        private static readonly string[] ResponseTitles = { "広く止める", "範囲を絞る", "復旧を急ぐ" };
        private static readonly string[] ResponseLines = { "関連する環境を停止・隔離", "対象を特定して隔離・調査", "安全確認と復旧に人を配分" };
        private static readonly Color IncidentRed = Hex("ff3b5c"), IncidentYellow = Hex("ffd23f");
        private RectTransform IncidentShape(Transform parent,string name,string kind,float x,float y,float w,float h,Color color)
        {
            var rect=Rect(parent,name,x,y,w,h);
            var graphic=rect.gameObject.AddComponent<OpsIncidentGraphic>();graphic.Kind=kind;graphic.color=color;
            graphic.raycastTarget=false;return rect;
        }
        private void IncidentBackdrop(bool resolving)
        {
            var background=screen.Find("SharedBackground");if(background!=null)background.gameObject.SetActive(false);
            screen.GetComponent<Image>().color=resolving?Hex("1b2340"):Hex("2a1020");
            PImage(screen,"OfficeBlur",PlanningArt.officeBlur,-100,-450,1800,1800,
                resolving?new Color(.55f,.55f,.55f,.8f):new Color(.5f,.5f,.5f,.7f));
            if(!resolving) IncidentShape(screen,"IncidentVignette","vignette",0,0,1600,900,new Color(.47f,.04f,.16f,.55f));
        }
        private bool EquipmentHelps(int index,string response)
        {
            if(State.levels[index]==0) return false;
            var actual=State.Estimate(response);var absent=State.EstimateWithoutProject(index,response);
            return absent.lossMin>actual.lossMin||absent.lossMax>actual.lossMax||absent.stopMin>actual.stopMin||absent.stopMax>actual.stopMax;
        }
        private void IncidentWorkspace()
        {
            IncidentBackdrop(false);
            var tape=IncidentShape(screen,"WarningTape","tape",0,0,1600,14,IncidentYellow);
            tape.gameObject.AddComponent<OpsIncidentTape>().Owner=this;
            PImage(screen,"IncidentLogoIcon",PlanningArt.logoIcon,24,32,68,68);
            var emergency=PCard(screen,"EmergencyBadge",108,34,250,64,IncidentRed,20,false);
            var shadow=emergency.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-5);shadow.effectColor=Hex("b3203d");
            IncidentShape(emergency,"WarningIcon","warning",26,17,30,30,Color.white);
            PText(emergency,"EmergencyTitle","緊急対応",68,0,170,64,30,Color.white);Motion(emergency,"shake",1.8f);
            var header=PCard(screen,"IncidentHeader",374,34,980,64,Color.white,20,false);
            var month=PCard(header,"MonthBadge",22,19,64,26,PlanInk,12,false);
            PText(month,"Month",State.Current.name,0,0,64,26,14,Color.white,true,true);
            var title=PText(header,"IncidentTitle",State.Current.title,100,0,506,64,26);
            float topicX=100+Mathf.Min(506,title.GetPreferredValues(State.Current.title).x)+16;
            string topic=State.CurrentProfile==null?"今月の出来事":State.CurrentProfile.category;
            var category=PButton(header,"IncidentTopic",topic,topicX,18,Mathf.Min(958-topicX,32+topic.Length*14),28,EventBriefDialog,Hex("ffe3ec"),Hex("c23a60"),16);
            KitGradient(category.GetComponent<Image>(),Hex("ffe3ec"),Hex("ffe3ec"));category.transform.Find("KitTopLight").gameObject.SetActive(false);
            category.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax=14;
            var budget=PButton(screen,"Menu","",1370,34,206,64,Menu,Color.white,PlanInk,20);
            var coin=PCard(budget.transform,"BudgetCoin",22,18,28,28,IncidentYellow,28,false);
            PText(coin,"CoinMark","円",0,0,28,28,12,Hex("7a5a00"),true,true);
            PText(budget.transform,"BudgetValue",State.budget.ToString(),62,0,80,64,30);
            PText(budget.transform,"BudgetUnit","万円",142,0,58,64,15,PlanGray);
            PCard(screen,"StageWhiteBorder",19,115,610,780,Color.white,28,false);
            var map=PCard(screen,"OfficeStage",24,120,600,770,Color.white,28,false);
            map.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            // 確認対象の場所であり、侵害や犯人を確定した描写ではない。
            bool desks=CurrentRoom=="office";
            bool reception=CurrentRoom=="reception";
            bool meeting=CurrentRoom=="meeting";
            var room=PImage(map,"OfficeArt",OfficeArt,desks?-40:reception?-450:meeting?-840:-440,desks||meeting?-140:reception?-560:-20,1500,1500,new Color(.55f,.55f,.55f,1));
            if(Application.isPlaying)room.gameObject.AddComponent<OpsRoomZoom>().Owner=this;
            // 把握カードの下に確認対象の枠を収める。下端と対象の横幅は保ち、上端の飾りだけを下げる。
            float alarmTop=State.decisionDepthRules>0?128:16;
            var alarm=IncidentShape(map,"AffectedRoom","alarm",110,alarmTop,380,406-alarmTop,IncidentRed);Motion(alarm,"alarm",1.2f);
            var mark=PButton(map,"IncidentLocation","!",270,150,64,64,IncidentEvidence,IncidentRed,Color.white,28);
            mark.GetComponent<Image>().sprite=PlanningArt.markerBubble;Border(mark,Color.white,4);
            mark.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax=38;Motion((RectTransform)mark.transform,"pop",1.6f);
            var symptom=PCard(map,"IncidentSymptomCard",110,320,380,105,new Color(.11f,.16f,.27f,.9f),16,false);
            PText(symptom,"LocationName",desks?"社員の席・情報の扱い":reception?"受付・社外とのやりとり":meeting?"会議室・情報の確認":"サーバー室・システム",16,8,348,25,15,Hex("ff9fb5"));
            PText(symptom,"IncidentSymptom",State.Current.symptom,16,33,348,44,15,Color.white,false);
            PText(symptom,"UnknownScope","影響範囲は未確認",16,80,348,20,13,Color.white,false);
            var portrait=Rect(map,"IncidentHinata",10,440,266,330);Portrait(portrait,"NavigatorPortrait",0,0,266,330,"pose_startled");
            var navigator=PCard(map,"Navigator",250,560,320,126,Color.white,20,false);
            PImage(navigator,"SpeechTail",PlanningArt.tail,-20,20,26,36);
            SpeechName(navigator);
            PText(navigator,"NavigatorSpeech",State.audited?"調査できたね！\n止める範囲も比べよう！":"まだ確認が必要だね！\nまずは止める範囲を決めよう！",18,17,284,76,18,PlanInk,false);
            PButton(map,"IncidentHelp","",250,560,320,126,IncidentEvidence,Color.clear,Color.clear,20);
            var right=Rect(screen,"DecisionPanel",648,120,928,770);IncidentComparison(right);
            if(State.decisionDepthRules>0)
            {
                var time=PCard(map,"IncidentTimeBadge",270,18,148,32,State.IncidentTime>0?Hex("5a4a9a"):PlanBlue,12,false);
                PText(time,"IncidentTimeLabel",State.IncidentTimeLabel,0,0,148,32,14,Color.white,true,true);
                var knowButton=PButton(map,"KnowledgeCard","",18,18,238,92,IncidentEvidence,Color.white,PlanInk,16);var know=(RectTransform)knowButton.transform;
                PText(know,"KnowledgeTitle","状況の把握",12,8,116,26,15);
                PText(know,"KnowledgeValue",State.SituationKnowledge+" / "+OpsCatalog.KnowledgeMax,146,8,80,26,15,PlanInk,true,true);
                for(int i=0;i<OpsCatalog.KnowledgeMax;i++)PCard(know,"KnowledgeGauge"+i,12+i*54,42,46,12,i<State.SituationKnowledge?PlanMint:PlanTrack,12,false);
                var how=PText(know,"KnowledgeHow","調べる・監視・台帳・相談の泡",12,65,214,20,11,PlanGray,false,true);how.textWrappingMode=TextWrappingModes.NoWrap;
                Hover(knowButton,"調査・監視・台帳・相談の手がかりで把握。未確認の真相は表示しません。");
            }
        }
        private void IncidentComparison(RectTransform p)
        {
            if(State.HasPeakGoal)
            {
                var goal=PCard(p,"IncidentPeakGoal",0,-38,928,29,Hex("fff6d6"),12,false);
                PText(goal,"IncidentPeakGoalText",State.PeakGoalText(State.month),16,0,896,29,16,Hex("7a5a00"));
            }
            PText(p,"DecisionHeading","対応方針を選ぶ",0,0,235,39,26,Color.white);
            var evidence=PButton(p,"IncidentEvidence","未確認：正常な操作の可能性もある",247,5,340,29,IncidentEvidence,IncidentYellow,PlanInk,16);
            KitGradient(evidence.GetComponent<Image>(),IncidentYellow,IncidentYellow);evidence.transform.Find("KitTopLight").gameObject.SetActive(false);
            var evidenceText=evidence.GetComponentInChildren<TextMeshProUGUI>();evidenceText.fontSizeMax=14;evidenceText.transform.SetAsLastSibling();
            var ready=PCard(p,"Readiness",0,53,928,62,new Color(1,1,1,.1f),20,false);
            var readyTitle=PText(ready,"ReadinessTitle","あなたの備え",16,0,112,62,15,Color.white);
            var relevant=Enumerable.Range(0,State.levels.Length).Where(i=>ResponseIds.Any(r=>EquipmentHelps(i,r))).Take(3).ToList();
            string[] missing=State.DataRecoveryApplies?new[]{"backup","drill","monitor"}:new[]{"monitor","education","runbook"};float x=134;
            bool support=ResponseIds.Any(id=>State.ResponsePower(id).staff>0);
            string member=State.SupportSummary.Split('：')[0],staffCaption=support?member+"が支援できる":"社員の育成・支援";
            // 字形の実幅＋左右12px。支援札の場所を先に確保し、すべて同じ列を左から詰める。
            Func<string,float> tagWidth=value=>Mathf.Ceil(readyTitle.GetPreferredValues("<size=14>"+value+"</size>").x)+24;
            float staffWidth=tagWidth(staffCaption)+(support?22:0),gearRight=928-16-staffWidth-10;
            foreach(int i in relevant)
            {
                var item=OpsCatalog.Projects[i];string caption=item.name+" Lv."+State.levels[i];float w=tagWidth(caption);if(x+w>gearRight) break;
                var tag=PCard(ready,"Ready_"+item.id,x,15,w,32,PlanMint,12,false);
                PText(tag,"ReadyLabel_"+item.id,caption,12,0,w-24,32,14,Color.white);x+=w+10;
            }
            foreach(string id in missing.Where(id=>State.Level(id)==0).Take(2))
            {
                string caption=OpsCatalog.Projects[OpsCatalog.Index(id)].name+" 未導入";float w=tagWidth(caption);if(x+w>gearRight) break;
                var tag=PCard(ready,"Missing_"+id,x,15,w,32,Color.white,12,false);
                IncidentShape(tag,"MissingFrame_"+id,"round-dashed",0,0,w,32,PlanGray).GetComponent<OpsIncidentGraphic>().StrokeWidth=1;
                PText(tag,"MissingLabel_"+id,caption,12,0,w-24,32,14,PlanInk);x+=w+10;
            }
            var staff=PButton(ready,"OpenTeam",staffCaption,x,15,staffWidth,32,TeamDialog,PlanBlue,Color.white,12);
            KitGradient(staff.GetComponent<Image>(),PlanBlue,PlanBlue);staff.transform.Find("KitTopLight").gameObject.SetActive(false);
            var staffText=staff.GetComponentInChildren<TextMeshProUGUI>();staffText.fontSize=staffText.fontSizeMin=staffText.fontSizeMax=14;staffText.fontStyle=FontStyles.Bold;
            if(support)
            {
                PImage(staff.transform,"SupportFace",PlanningArt.morale,10,5,22,22,Color.white);
                staffText.margin=new Vector4(34,0,8,0);
            }
            var estimates=ResponseIds.Select(State.Estimate).ToArray();int lossScale=Math.Max(1,estimates.Max(e=>e.lossMax)),stopScale=Math.Max(1,estimates.Max(e=>e.stopMax));
            string[] caution={"正常な業務も止め、\n広がりを抑える",State.ScopeOversight>0?"見落としの恐れを含む\n範囲を絞る備えが重要":"業務を続けやすいが、\n範囲を絞る備えが重要","安全確認の後に再開。\n戻せる備えが重要"};
            for(int i=0;i<3;i++)
            {
                string id=ResponseIds[i];var e=estimates[i];float w=896f/3,xCard=i*(w+16);
                var working=Enumerable.Range(0,State.levels.Length).Where(j=>EquipmentHelps(j,id)).ToArray();
                bool prepared=working.Length>0;
                if(prepared) PCard(p,"PreparedOutline_"+id,xCard-4,125,w+8,584,PlanMint,28,false);
                var card=PCard(p,"ResponseCard_"+id,xCard,129,w,576,Color.white,24,false);
                card.GetComponent<Image>().raycastTarget=true;card.gameObject.AddComponent<OpsCardLift>().Owner=this;
                if(prepared)
                {
                    // 公開見積もりに効く設備だけを表示。未確定の結果は参照しない。
                    int best=working.OrderByDescending(j=>State.EstimateWithoutProject(j,id).lossMax-e.lossMax)
                        .ThenByDescending(j=>State.EstimateWithoutProject(j,id).stopMax-e.stopMax).First();
                    string label=OpsCatalog.Projects[best].name+"が効く";
                    float badgeWidth=Mathf.Min(w-32,24+label.Length*13);
                    var badge=PCard(card,"PreparedBadge_"+id,w-badgeWidth-16,-14,badgeWidth,27,PlanMint,12,false);
                    PText(badge,"PreparedText_"+id,label,8,0,badgeWidth-16,27,13,Color.white,true,true);
                }
                var icon=PCard(card,"ResponseIcon_"+id,20,20,56,56,Hex(i==0?"ffe3ec":i==1?"e3f2ff":"e3faf3"),20,false);
                if(i==1) PImage(icon,"ResponseGlyph_"+id,PlanningArt.audit,12,12,32,32);
                else IncidentShape(icon,"ResponseGlyph_"+id,i==0?"stop":"restore",12,12,32,32,i==0?IncidentRed:Hex("1a9c7c"));
                PText(card,"ResponseName_"+id,ResponseTitles[i],88,18,w-108,32,22);
                PText(card,"ResponseType_"+id,State.CurrentProfile==null?ResponseLines[i]:State.ResponseName(id),88,51,w-108,37,14,PlanGray,false);
                PText(card,"CostLabel_"+id,"対応費",20,105,98,38,15,PlanGray);
                var cost=PText(card,"ResponseCost_"+id,e.cost.ToString(),112,105,w-174,38,26,State.budget<e.cost?IncidentRed:PlanInk,true,true);cost.alignment=TextAlignmentOptions.MidlineRight;
                var costUnit=PText(card,"CostUnit_"+id,"万円",w-57,105,37,38,14,PlanInk);costUnit.alignment=TextAlignmentOptions.MidlineLeft;
                InlineMetric(cost,costUnit,w-20);
                EstimateMetric(card,"Stop_"+id,"業務停止",e.stopMin,e.stopMax,stopScale,159,PlanPink,w);
                EstimateMetric(card,"Loss_"+id,"被害",e.lossMin,e.lossMax,lossScale,227,Hex("ff9f43"),w);
                PText(card,"ResponseCaution_"+id,State.budget<e.cost?"手元予算が対応費に不足\n"+caution[i]:caution[i],20,305,w-40,State.HasPeakGoal?44:70,14,PlanInk,false);
                if(State.HasPeakGoal)
                {
                    int prospect=State.PeakProspect(State.month,e);Color tone=PeakProspectColor(prospect);
                    var forecast=PCard(card,"PeakForecast_"+id,20,350,w-40,26,Color.clear,12,false);
                    IncidentShape(forecast,"PeakForecastFrame","dashed",0,0,w-40,26,tone);
                    PText(forecast,"PeakForecastText_"+id,"予測："+OpsState.PeakProspectText(prospect),6,0,w-52,26,13,tone,true,true);
                }
                WorkingEquipment(card,id,working,w);
                // 詳細はアイコンから開く。モックにない説明列は常設しない。
                var details=PButton(card,"Power_"+id,"",20,20,56,56,()=>PowerReport(id),Color.clear,Color.clear,20);Hover(details,"抑制力の内訳を見る");
                PButton(card,"Respond_"+id,"この方針で対応",20,500,w-40,56,()=>ChooseResponse(id),PlanInk,Color.white,16,Hex("0c1226"));
            }
            PText(p,"NoTimer","見積もりは目安の幅で、確率ではありません。札と一覧は、この方針に効く導入済みの備えです。",0,724,928,43,14,Hex("e8c9d3"),false);
        }
        private void WorkingEquipment(Transform card,string response,int[] working,float width)
        {
            if(working.Length==0)return;
            PText(card,"WorkingHeading_"+response,"この方針で働く備え",20,380,width-40,24,13,PlanGray);
            int count=Math.Min(working.Length,6);float chipWidth=(width-48)/2;
            for(int j=0;j<count;j++)
            {
                bool more=j==5&&working.Length>6;
                string text=more?"ほか "+(working.Length-5)+"件":OpsCatalog.Projects[working[j]].name;
                string key=more?"WorkingMore_"+response:"Working_"+response+"_"+OpsCatalog.Projects[working[j]].id;
                var chip=PButton(card,key,text,20+(j%2)*(chipWidth+8),410+(j/2)*27,chipWidth,23,
                    ()=>WorkingEquipmentDetails(response,working),Hex("e3faf3"),Hex("1a7c63"),12);
                chip.GetComponentInChildren<TextMeshProUGUI>().fontSizeMin=10;
            }
        }
        private void WorkingEquipmentDetails(string response,int[] working)
        {
            var actual=State.Estimate(response);
            string body=string.Join("\n\n",working.Select(j=>
            {
                var absent=State.EstimateWithoutProject(j,response);
                return OpsCatalog.Projects[j].name+" Lv."+State.levels[j]+"\n外すと見積もり上限：被害 +"+(absent.lossMax-actual.lossMax)+"万円 / 停止 +"+(absent.stopMax-actual.stopMax)+"時間";
            }));
            var dialog=Dialog("この方針で働く備え",body+"\n\n設備を一つずつ外した比較です。連携があるため、差は足し合わせません。",Mathf.Min(820,300+working.Length*64));
            var text=dialog.Find("DialogBody").GetComponent<TextMeshProUGUI>();text.fontSize=15;text.fontSizeMin=12;text.fontSizeMax=15;
            text.GetComponent<OpsTextPreference>()?.Initialize(text,TextScale);
        }
        private void EstimateMetric(Transform parent,string id,string title,int min,int max,int scale,float y,Color color,float width)
        {
            float w=width-40;
            PText(parent,"EstimateLabel_"+id,title,20,y,95,30,15,PlanGray);
            var value=PText(parent,"EstimateValue_"+id,min==max?min.ToString():min+"～"+max,112,y,width-174,30,18,PlanInk,true,true);value.alignment=TextAlignmentOptions.MidlineRight;
            var unit=PText(parent,"EstimateUnit_"+id,id.StartsWith("Stop")?"時間":"万円",width-57,y,37,30,13,PlanInk);unit.alignment=TextAlignmentOptions.MidlineLeft;
            InlineMetric(value,unit,width-20);
            var track=PCard(parent,"EstimateTrack_"+id,20,y+44,w,10,Hex("e6eaf2"),12,false);
            // 帯は下限から上限まで。3方針の尺度は同一で、確率に見える塗り分けはしない。
            PCard(track,"EstimateBand_"+id,w*min/scale,0,w*(max-min)/scale,10,color,12,false);
            if(min==max) PCard(track,"EstimateFloor_"+id,Mathf.Min(w-3,w*min/scale),0,3,10,color,12,false);
        }
        private void InlineMetric(TextMeshProUGUI number,TextMeshProUGUI unit,float right)
        {
            number.textWrappingMode=unit.textWrappingMode=TextWrappingModes.NoWrap;
            var flow=number.gameObject.AddComponent<OpsInlineMetric>();flow.Number=number;flow.Unit=unit;flow.Right=right;
        }
        private void Glyph(Transform parent,string name,string key,float x,float y,float size)
        {
            var r=Rect(parent,name,x,y,size,size);var icon=r.gameObject.AddComponent<DefenseGlyph>();icon.SetKey(key);icon.raycastTarget=false;
        }
        private void IncidentEvidence()
        {
            var d=Dialog("確認できたこと",State.Current.symptom+"\n\n"+(State.audited?State.Current.finding:
                "調査前のため、深刻度は未確認です。正常な活動の可能性も残ります。")+
                "\n\n見積もりは公開情報に基づく幅です。発生確率や保証ではありません。\n抑制力の内訳は各対応案から確認できます。",640);
            Button(d,"IncidentKnowledge","関連する知識を読む",32,554,420,48,()=>Knowledge(State.Current.lesson),Accent);
        }
    }
}
