using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private Color PeakProspectColor(int value)=>value==2?Hex("1a7c63"):value==1?Hex("aa7500"):Hex("b24968");
        private void RankProgressStrip(Transform parent,float x,float y,float width)
        {
            if(State.peakGoalRules==0)return;
            var strip=PCard(parent,"AnnualRankProgress",x,y,width,28,Hex("f0ecfb"),12,false);
            PText(strip,"NextRankPoints",State.RankProgress,10,0,width-20,28,13,Hex("5a4a9a"),true,true);
        }
        private void PeakGoalDialog(int targetMonth)
        {
            string body=State.PeakGoalText(targetMonth)+"\n\n";
            var record=State.history.FirstOrDefault(r=>r.month==targetMonth&&r.peakGoalRecorded);
            if(record!=null)body+=(record.peakGoalMet?"達成・山場の盾を獲得":"目標未達")+" / 確定：被害 "+record.loss+"万円・停止 "+record.downtime+"h\n\n";
            body+="今の備えでの見込み（予測）\n";
            for(int i=0;i<ResponseIds.Length;i++)
            {
                var estimate=State.Estimate(ResponseIds[i]);
                body+=ResponseTitles[i]+"："+OpsState.PeakProspectText(State.PeakProspect(targetMonth,estimate))+"\n"+
                    "被害 "+estimate.lossMin+"〜"+estimate.lossMax+"万円・停止 "+estimate.stopMin+"〜"+estimate.stopMax+"h\n\n";
            }
            body+="現在の公開見積もりを目標に当てはめた参考です。未来の事件と今後の成長は未反映。確率や達成の保証ではありません。";
            Dialog("山場の目標と見込み",body,740);
        }
        private string PeakMissHint(OpsOutcome result)
        {
            var effect=result.potentialInvestmentEffects?.FirstOrDefault();
            if(effect==null)return "単独の未導入設備では改善なし / 調査・運用・支援も見直そう";
            int i=State.PeakIndex(result.month);
            bool reaches=i>=0&&result.loss-effect.avoidedLoss<=OpsCatalog.PeakLossGoals[i]&&result.downtime-effect.avoidedDowntime<=OpsCatalog.PeakStopGoals[i];
            return OpsCatalog.Projects[OpsCatalog.Index(effect.projectId)].name+"："+EffectLine(effect)+" / "+(reaches?"あれば目標内":"単独ではまだ未達");
        }
        private void PeakMonthlyResult(OpsOutcome result)
        {
            if(!result.peakGoalRecorded)return;
            Color tint=result.peakGoalMet?Hex("fff6d6"):Hex("ffe9ee"),text=result.peakGoalMet?Hex("7a5a00"):Hex("b24968");
            var badge=PCard(screen,"PeakResultBadge",1250,784,320,46,tint,16,false);
            PText(badge,"PeakResultText",result.peakGoalMet?"山場の盾 獲得 / 年間 +"+result.peakScoreBonus+"点":"山場の目標は未達 / 信頼 −"+OpsCatalog.PeakTrustPenalty,10,0,300,46,15,text,true,true);
            Reveal(badge,.3f,true);
        }
        private void StyleAnnualRank(RectTransform rank)
        {
            if(State.RankCode=="SS")
            {
                KitGradient(rank.GetComponent<Image>(),Hex("ffe69a"),Hex("d49418"));Shine(rank,360,360);
                for(int i=0;i<4;i++){var spark=PCard(rank,"SSRankSpark"+i,i%2==0?-24:358,i<2?36:282,26,26,Color.white,20,false);Motion(spark,"pulse",2,i*.4f);}
            }
            else if(State.RankCode=="S")
            {
                KitGradient(rank.GetComponent<Image>(),Hex("a486e4"),Hex("6751a4"));
                Color[] colors={PlanPink,Hex("ffbf46"),PlanMint,PlanBlue,Hex("ac83ef"),PlanPink};
                for(int i=0;i<6;i++)PCard(rank,"SRainbowEdge"+i,-9+i*63,-9,63,14,colors[i],12,false);
                for(int i=0;i<6;i++)PCard(rank,"SRainbowFoot"+i,-9+i*63,355,63,14,colors[5-i],12,false);
                PCard(rank,"SRainbowLeft",-9,5,14,350,PlanPink,12,false);PCard(rank,"SRainbowRight",355,5,14,350,PlanBlue,12,false);Shine(rank,360,360);
            }
            else if(State.RankCode=="B")KitGradient(rank.GetComponent<Image>(),Hex("7abfe8"),Hex("3b82b8"));
            else if(State.RankCode=="C")KitGradient(rank.GetComponent<Image>(),Hex("a6afc0"),Hex("65728c"));
        }
        private void PeakAnnualMedals()
        {
            var medals=State.history.Where(r=>r.peakGoalRecorded&&r.peakGoalMet).ToArray();
            if(State.peakGoalRules==0)return;
            PText(screen,"PeakMedalHeading","山場の盾　"+medals.Length+" / "+OpsCatalog.PeakMonths.Length,632,298,890,24,14,Hex("7a5a00"));
            for(int i=0;i<medals.Length;i++)
            {
                var medal=PCard(screen,"PeakMedal"+i,110+i*112,740,104,30,Hex("fff6d6"),12,false);
                PImage(medal,"PeakShield",PlanningArt.logoIcon,6,6,18,18);PText(medal,"PeakMedalMonth",OpsCatalog.Months[medals[i].month].name,30,0,68,30,14,Hex("7a5a00"),true,true);Reveal(medal,i*.1f);
            }
        }
    }
}
