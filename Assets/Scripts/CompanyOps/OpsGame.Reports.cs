using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform ReportPanel(string id,float x,float y,float w,float h,float delay=0)
        {
            var p=PCard(screen,id,x,y,w,h,Color.white); Reveal(p,delay); return p;
        }
        private void ReportHeading(Transform p,string en,string title,Color color)
        {
            var ribbon=IncidentShape(p,en+"Ribbon","cutin",22,16,112,32,new Color(color.r,color.g,color.b,.12f));
            PText(ribbon,en+"Category",en,10,2,100,28,11,color);
            PText(p,en+"Heading",title,146,16,240,32,20);
        }
        private void ReportBackground(bool annual)
        {
            var bg=Box(screen,"ReportBackground",0,0,1600,900,Color.white);
            KitGradient(bg.GetComponent<Image>(),annual?Hex("fff4d6"):Hex("cfe9ff"),Hex("ffe3ec"));
            if(!annual)PImage(screen,"ReportOfficeBlur",PlanningArt.officeBlur,-100,-450,1800,1800,new Color(1,1,1,.4f));
        }
        private void ReportNumber(Transform p,string id,int value,string unit,float x,float y,float w,float size,Color color)
        {
            var t=PText(p,id,value.ToString("N0")+unit,x,y,w,size+18,size,color);
            t.enableVertexGradient=true;t.colorGradient=new VertexGradient(Color.Lerp(color,Color.white,.2f),Color.Lerp(color,Color.white,.2f),color,color);
            if(Application.isPlaying)StartCoroutine(ReportCount(t,value,unit));
        }
        private IEnumerator ReportCount(TextMeshProUGUI label,int value,string unit)
        {
            float started=Time.realtimeSinceStartup;
            yield return null;
            if(label==null)yield break;
            if(value!=0&&Time.realtimeSinceStartup-started<.6f)PlayPresentationCue(OpsCue.Count);
            while(Time.realtimeSinceStartup-started<.6f)
            {
                if(label==null)yield break;
                float t=Time.realtimeSinceStartup-started;
                label.text=Mathf.RoundToInt(value*Mathf.SmoothStep(0,1,t/.6f)).ToString("N0")+unit;yield return null;
            }
            if(label==null)yield break;label.text=value.ToString("N0")+unit;
            StartCoroutine(FinishCountSound());
            for(float t=0;t<.18f;t+=Time.unscaledDeltaTime)
            {if(label==null)yield break;label.transform.localScale=Vector3.one*(ReducedMotion?1:1+.1f*Mathf.Sin(t/.18f*Mathf.PI));yield return null;}
            if(label!=null)label.transform.localScale=Vector3.one;
        }
        private void ReportGauge(Transform p,string id,float x,float y,float w,float ratio,Color color)
        {
            PCard(p,id+"Track",x,y,w,10,PlanTrack,12,false);
            if(ratio<=0)return;var fill=PCard(p,id+"Fill",x,y,w*Mathf.Clamp01(ratio),10,color,12,false);KitGradient(fill.GetComponent<Image>(),Color.Lerp(color,Color.white,.3f),color,true);
            PImage(fill,"GaugeLight",null,2,1,Mathf.Max(0,fill.rect.width-4),2,new Color(1,1,1,.5f));
        }
        private void ReportSpeech(string line,float x,float y,float w,string face="face_normal")
        {
            var bubble=PCard(screen,"Navigator",x,y,w,120,Color.white,20);
            SpeechName(bubble);
            PText(bubble,"NavigatorSpeech",line,18,15,w-36,92,18,null,false);
        }
        private void MonthlyScreen()
        {
            var r=State.Latest;var previous=PreviousReport(r);ReportBackground(false);
            PImage(screen,"MonthlyLogoIcon",PlanningArt.logoIcon,40,30,80,80);
            var medal=PCard(screen,"MonthMedal",136,30,120,80,PlanPink,24);KitGradient(medal.GetComponent<Image>(),Hex("ff94ae"),Hex("f45a80"));
            PText(medal,"YearNumber",(Story?.year??1)+"年目",0,6,120,24,13,Color.white,true,true);PText(medal,"Month",State.Current.name,0,30,120,44,36,Color.white,true,true);
            PText(screen,"MonthlyCategory","MONTHLY REPORT",272,26,800,28,12,PlanPink);
            PText(screen,"ReviewTitle","今月のふりかえり",272,52,800,62,40);
            RankProgressStrip(screen,272,109,468);
            PeakMonthlyResult(r);
            var damageStamp=PCard(screen,"MonthlyDamageStamp",1050,53,240,54,r.loss==0?Hex("e3faf3"):Hex("ffe9ee"),16,false);
            PText(damageStamp,"MonthlyDamageStampText",r.loss==0?"金銭被害なし":"金銭被害 "+r.loss+"万円",0,0,240,54,20,r.loss==0?Hex("1a7c63"):Coral,true,true);Reveal(damageStamp,.25f,true);
            PButton(screen,"Menu","設定",1370,38,180,52,Menu,Color.white,PlanInk);
            PButton(screen,"DiaryWritten","日記が書かれた",1290,108,260,38,OpenMonthlyDiary,Hex("fff6d6"),Hex("7a5a00"),16);
            var incident=ReportPanel("MonthlyIncident",40,140,700,330);
            PText(incident,"ReviewEvent","事件："+(r.eventTitle??State.Current.title),26,20,648,36,18,PlanGray);
            string[] names={"被害","業務停止","対応費"},ids={"MonthlyLossValue","MonthlyStopValue","MonthlyCostValue"};int[] values={r.loss,r.downtime,r.cost};
            Color[] bg={Hex("fff1e4"),Hex("ffe9ee"),Hex("eef2f8")},fg={Hex("b35c00"),Hex("c23a60"),Hex("52607a")};
            for(int i=0;i<3;i++)
            {
                var c=PCard(incident,"ResultCard"+i,26+i*220.67f,68,206.67f,146,bg[i],20,false);Reveal(c,.04f*i);
                PText(c,"ResultLabel"+i,names[i],16,12,176,30,15,fg[i]);
                ReportNumber(c,ids[i],values[i],"<size=17>"+(i==1?"時間":"万円")+"</size>",16,44,previous==null?176:118,40,PlanInk);
                if(previous!=null)
                {
                    int last=i==0?previous.loss:i==1?previous.downtime:previous.cost;
                    MonthTrend(c,"MonthlyTrend"+i,values[i]-last,false,138,54);
                    PText(c,"PreviousMonthLabel"+i,"前月比",140,82,52,18,10,PlanGray);
                }
                string hint=i==0?r.hasInvestmentComparison?"備えなしなら"+(r.loss+r.avoidedLoss)+"万円":"備え比較は未記録":i==1?r.forecast==null?"見積もりは未記録":"見積もり "+r.forecast.stopMin+"〜"+r.forecast.stopMax+"時間":"方針："+(r.response=="contain"?"広範囲を停止":r.response=="scope"?"対象を限定":"復旧を優先");
                PText(c,"ResultHint"+i,hint,16,108,176,26,13,i==0?Hex("1a7c63"):PlanGray);
            }
            var effect=PButton(incident,"EffectDetails","",26,236,648,66,()=>InvestmentReport(r),Hex("e3faf3"),PlanInk,16);
            KitGradient(effect.GetComponent<Image>(),Hex("e3faf3"),Hex("e3faf3"));effect.GetComponent<Shadow>().enabled=false;
            var best=r.investmentEffects?.OrderByDescending(e=>e.avoidedLoss).ThenByDescending(e=>e.avoidedDowntime).FirstOrDefault(e=>e.avoidedLoss>0||e.avoidedDowntime>0);
            bool peakMiss=r.peakGoalRecorded&&!r.peakGoalMet;
            PText(effect.transform,"EffectTag",peakMiss?"山場未達 / 次に効きそうな備え":best==null?"次に効きそうな備え":"効いた備え",16,6,peakMiss?616:240,28,14,Hex("1a7c63"));
            var potential=r.potentialInvestmentEffects?.FirstOrDefault();
            string equipment=peakMiss?PeakMissHint(r):best!=null?OpsCatalog.Projects[OpsCatalog.Index(best.projectId)].name+" Lv."+best.level:
                potential!=null?OpsCatalog.Projects[OpsCatalog.Index(potential.projectId)].name+" / 未導入":"次の計画で備えを確認しよう";
            PText(effect.transform,"ImpactNumbers",equipment,16,34,best==null||peakMiss?616:396,24,peakMiss?13:17);
            if(best==null||peakMiss)IncidentShape(effect.transform,"PotentialEquipmentFrame","dashed",4,4,640,58,Hex("1a7c63"));
            else PText(effect.transform,"ImpactSummary",r.hasInvestmentComparison?"被害 −"+r.avoidedLoss+"万円\n停止 −"+r.avoidedDowntime+"時間":"比較未記録",430,6,200,52,15,Hex("1a7c63"));
            MonthlyGrowth(r);
            var mission=ReportPanel("MonthlyMission",770,140,440,330,.04f);ReportHeading(mission,"MISSION","社内依頼",PlanPink);
            PText(mission,"MissionTitle",ReportMissionTitle(State.CurrentMission.title),26,62,225,100,22);
            State.MissionProgress(true,out int a,out int at);State.MissionProgress(false,out int b,out int bt);
            PText(mission,"MissionResult",State.CurrentMissionCompleted?"達成！ 設備 "+a+"/"+at+"・現場 "+b+"/"+bt:"今回は未達成",26,186,388,38,15,PlanGray);
            if(State.CurrentMissionCompleted)
            {
                var stamp=PCard(mission,"MissionStamp",264,70,150,150,new Color(1,.435f,.569f,.12f),28,false);stamp.localEulerAngles=new Vector3(0,0,-8);Reveal(stamp,.12f,true);
                IncidentShape(stamp,"StampStitch","dashed",7,7,136,136,PlanPink);PText(stamp,"StampText","達成",0,0,150,150,42,PlanPink,true,true);
                ReportChip(mission,"MissionReward","信頼 +3",26,267,164,Hex("f0ecfb"),Hex("5a4a9a"));ReportChip(mission,"MissionPoints","年間 +45点",200,267,214,Hex("e4f3ff"),Hex("1f6fb0"));
                if(r.missionBonus>0)PText(mission,"MissionBudgetPaid","臨時予算 +"+r.missionBonus+"万円（受領済み）",26,230,224,26,14,Hex("7a5a00"));
                var conversation=PButton(mission,"MissionConversation","ひなたとふりかえる",264,224,150,34,MissionConversation,Color.white,PlanPink,16);
                conversation.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax=13;
            }
            else PText(mission,"MissionNoReward","報酬なし / 来月の計画に活かそう",26,260,388,48,15,PlanGray);
            var team=ReportPanel("MonthlyTeam",770,494,440,250,.12f);ReportHeading(team,"TEAM","活躍した社員",PlanMint);
            bool support=!r.benign&&r.power!=null&&r.power.staff>0;
            string member=support?r.power.support.Split('：')[0]:"";int index=Array.IndexOf(OpsGrowthCatalog.StaffNames,member);
            if(support && index>=0)
            {
                var face=PCard(team,"SupportFace",26,72,64,64,Hex("1a9c7c"),28,false);PText(face,"SupportInitial",member.Substring(0,1),0,0,64,64,26,Color.white,true,true);
                int level=r.growth?.staffBefore==null?State.StaffLevel(index):r.growth.staffBefore[index];
                PText(team,"SupportName",member+"  Lv."+level,104,62,310,34,19);
                PText(team,"SupportTask",r.power.support.Contains("：")?r.power.support.Split('：')[1]:"対応を助けた",104,100,310,62,15,PlanGray,false);
                PText(team,"SupportPower","抑制力 +"+r.power.staff,104,156,310,28,15,Hex("1f6fb0"));
            }
            else
            {
                for(int i=0;i<3;i++)
                {
                    var face=PCard(team,"NextSupportFace"+i,26+i*116,64,58,58,Hex("edf1f7"),24,false);
                    IncidentShape(face,"NextStaffGlyph"+i,"staff-face",6,6,46,46,new Color(.42f,.47f,.57f,.45f)).GetComponent<OpsIncidentGraphic>().Offset=i;
                }
                PText(team,"SupportNone",r.power==null?"社員の支援は未記録":"次の月は支援を頼める",26,136,388,36,17,PlanGray);
            }
            PButton(team,"ReviewPower","対応力の内訳を見る",26,190,388,44,()=>PowerReport(r.response,r),Color.white,PlanInk,16);
            Portrait(screen,"NavigatorPortrait",1250,300,363,450,r.loss==0?"pose_peace":"pose_think");
            ReportSpeech(best!=null?"備えが効いたね！\n次の計画でも、今回の結果を活かそう。":r.loss==0?"金銭被害はゼロ！\n停止と対応費も確認しよう。":"対応おつかれさま。\n被害と停止を減らす方法を考えよう。",1250,180,320,r.loss==0?"face_sparkle":"face_worried");
            PButton(screen,"ReviewDetails","記録を見る",40,772,384.67f,62,()=>MonthlyRecordDialog(r),Color.white,PlanInk,20);
            PButton(screen,"NextMonth",State.QuarterRewardPending?(State.peakGoalRules>0?"四半期の報酬を選ぶ":"山場クリア / 報酬を選ぶ"):State.month==11||State.budget<0||State.stability==0?"年間評価へ ▶":OpsCatalog.Months[State.month+1].name+"へ ▶",440.67f,772,769.33f,62,OpenMonthlyDiary,PlanInk,Color.white,20);
        }
        public static string ReportMissionTitle(string title)
        {
            // 表示だけの分かち書き。元の依頼名・条件・記録は変更しない。
            string[] lines={
                "納品データを戻す\n手段を示す","新人のログインを\n守る","急ぎの依頼を\n安全に判断する","受注を止めない\n体制を作る",
                "担当者が休める\n運用にする","更新対象を\n絞り込む","取引先からの影響を\n限定する","共有範囲の判断を\n支える",
                "忙しい時も\n報告できる","復元できる状態を\n確かめる","異常なログインを\n見分ける","年度末の納品を\n守る",
                "うちのデータ、\n戻せる？","バックアップも\n同じ権限？","暗号化だけではない\n被害","一台から全部に\n広がる？",
                "委託先の保守IDは\n誰が使う？","正規の更新なら\n安全？","外部サービスが\n侵害されたら","AIに顧客資料を\n入れていい？",
                "AIの回答に\n操作を任せる？","生成した回答を\nそのまま配布？","公開機器の\n緊急更新","使っている\nライブラリが対象？",
                "古い端末を\nすぐ替えられない","会議資料を装う\n添付","便利な連携アプリの\n権限","MFAを通っている\nから安心？",
                "外部情勢で\n攻撃が増えたら","侵害を名乗る\n投稿が出た","退職前の大量\nダウンロード","共有の管理者ID",
                "外注メンバーの\n権限が残る","在宅接続の\n認証情報が流出？","持ち帰った端末が\n見つからない","サポートを名乗る\n電話",
                "サイトだけが重い","回線が埋まっている","取引先の振込先が\n変わった","社長の声なら\n信用する？",
                "いつものメールの\n続き","証明書の更新担当が\nいない","SSOの切替後に\n入れない","更新後に端末が\n起動しない",
                "売上が伸びたら\n処理が詰まる","夜間処理が止まった","保存装置が故障した","共有フォルダを\n誤って削除",
                "使っているSaaSが\n停止","オフィスのネットが\n不安定","共有リンクの範囲が\n広い","メールが勝手に\n転送される"
            };
            foreach(string line in lines)
            {
                string plain=line.Replace("\n","");
                if(title==plain)return line;
                if(title==plain+"への備え")return line+"\nへの備え";
            }
            return (title??"").Replace("、","、\n");
        }
        private void MissionConversation()
        {
            string id=State.CurrentMission.projectA;
            string[] ids={"backup","mfa","education","redundancy","automation","patch","monitor","inventory","runbook","drill","segment"};
            string[] lines={"戻す手段を考える材料がそろったね。実際に復元できるかも確かめていこう！","ログインを守る準備が進んだね。困った社員への案内も一緒に整えよう！","相談しやすい職場に近づいたね。変だと思ったら聞けるって大切！","止まったときの選択肢が増えたね。切り替えの練習もしておこう！","仕事を分ける準備が進んだね。担当者ひとりに抱えさせないのがいいね！","更新する対象を考えられたね。影響の確認と、戻す手順も忘れずに！","気づくための準備が進んだね。見つけた後の連絡先も大切だよ！","何を守るか確認できたね。台帳も実態に合わせて育てていこう！","手順を考えられたね。誰かが休んでも引き継げると安心だね！","復元の準備が進んだね。練習の結果を手順へ戻すところまでやろう！","影響を広げない準備が進んだね。必要な業務の通信は残せるかも確認しよう！"};
            int i=Array.IndexOf(ids,id);Dialog("ひなた / 今月の依頼を達成",i<0?"相談の条件を満たせたね。次に実際の運用でも確かめよう！":lines[i],380);
        }
        private void ReportChip(Transform p,string id,string text,float x,float y,float w,Color bg,Color fg)
        {var c=PCard(p,id,x,y,w,32,bg,16,false);PText(c,id+"Text",text,8,0,w-16,32,14,fg,true,true);}
        private void MonthlyGrowth(OpsOutcome r)
        {
            var p=ReportPanel("MonthlyGrowth",40,494,700,250,.08f);ReportHeading(p,"GROWTH","今月の成長",PlanBlue);
            string[] names={"備え","立て直す力","相談文化","経営の信頼","余力","チームの力"};
            if(r.metricsBefore==null||r.metricsAfter==null) {PText(p,"GrowthUnrecorded","月初の値は未記録",26,68,648,66,22,PlanGray);return;}
            int index=Enumerable.Range(0,6).OrderByDescending(i=>PlanningRank(r.metricsBefore[i])!=PlanningRank(r.metricsAfter[i]) && r.metricsAfter[i]>r.metricsBefore[i]).ThenByDescending(i=>r.metricsAfter[i]-r.metricsBefore[i]).First();
            int before=r.metricsBefore[index],after=r.metricsAfter[index];bool up=after>before&&PlanningRank(before)!=PlanningRank(after);
            var rank=PCard(p,"GrowthRank",26,68,46,46,RankColor(after),16,false);KitGradient(rank.GetComponent<Image>(),Color.Lerp(RankColor(after),Color.white,.3f),RankColor(after));PText(rank,"GrowthRankValue",PlanningRank(after),0,0,46,46,28,Color.white,true,true);if(up)Reveal(rank,.08f,true);
            PText(p,"GrowthName",names[index],86,62,190,34,17);
            if(rankedReports.Add(r.month))RankChangeEffect(rank.Find("GrowthRankValue").GetComponent<TextMeshProUGUI>(),rank.GetComponent<Image>(),before,after);
            ReportChip(p,"GrowthRankTag",(up?"RANK UP  ":after<before?"DOWN  ":"RANK  ")+PlanningRank(before)+"→"+PlanningRank(after),280,66,180,up?PlanPink:PlanTrack,up?Color.white:PlanGray);
            PText(p,"GrowthValues",before+" → "+after,476,64,198,34,17,null,true,true);
            ReportGauge(p,"GrowthGauge",86,105,588,after/100f,PlanPink);LossTrail(p,"GrowthLossTrail",86,105,588,10,before,after,100);
            var lossTrail=p.Find("GrowthLossTrail");if(lossTrail!=null)lossTrail.SetSiblingIndex(p.Find("GrowthGaugeTrack").GetSiblingIndex()+1);
            string[] benefits={"設備の予防力が対応に反映される","復元・再開の備えが停止の軽減に役立つ","相談文化は、詐欺や持ち出しの影響を抑える","信頼が上がると、月次予算の計算に反映される","疲労が下がると、対応の負担を軽くできる","現場の準備と社員の成長が対応を助ける"};
            PText(p,"GrowthBenefit",benefits[index],86,124,588,42,14,Hex("52607a"),false);
            var previous=PreviousReport(r);bool recorded=previous?.metricsAfter!=null;
            PText(p,"GrowthTrendHeading",r.month==0?"初月：前月との比較なし":recorded?"前月末との比較":"前月末の値は未記録",26,164,648,20,12,PlanGray,false);
            // 月初差分（上段）と前月末差分（下段）を混同しない。疲労を必ず含める。
            var changes=Enumerable.Range(0,6).Where(i=>i!=4).OrderByDescending(i=>recorded?System.Math.Abs(r.metricsAfter[i]-previous.metricsAfter[i]):System.Math.Abs(r.metricsAfter[i]-r.metricsBefore[i])).Take(2).Concat(new[]{4}).ToArray();
            for(int j=0;j<changes.Length;j++)
            {
                int i=changes[j],value=i==4?100-r.metricsAfter[i]:r.metricsAfter[i];
                var chip=PCard(p,"GrowthDelta"+i,26+j*216,186,206,32,Hex("eef2f8"),16,false);
                PText(chip,"GrowthDelta"+i+"Text",(i==4?"疲労":names[i])+" "+value,8,0,recorded?128:190,32,14,PlanInk);
                if(recorded){int old=i==4?100-previous.metricsAfter[i]:previous.metricsAfter[i];MonthTrend(chip,"GrowthTrend"+i,value-old,i!=4,136,4);}
            }
        }
        private void MonthlyRecordDialog(OpsOutcome r)
        {
            ReviewDetails(r);var d=modal.Find("Dialog");
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta=new Vector2(748,390);
            PButton(d,"MonthlyLesson","今回の知識",32,536,350,48,()=>Knowledge(string.IsNullOrEmpty(r.lessonId)?State.Current.lesson:r.lessonId),Color.white,PlanInk);
            PButton(d,"ViewHistory","月ごとの記録",410,536,350,48,History,Color.white,PlanInk);
        }
    }
}
