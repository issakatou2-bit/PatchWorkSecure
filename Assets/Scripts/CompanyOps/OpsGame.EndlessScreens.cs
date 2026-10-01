using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void EndlessRoad(float x,float y)
        {
            int first=Math.Max(1,Endless.year-1);
            for(int i=0;i<3;i++)
            {
                int year=first+i;var record=Endless.records.FirstOrDefault(r=>r.year==year);bool done=record!=null&&record.operated,bad=record!=null&&!record.operated,next=!Endless.finished&&year==Endless.year+1;
                Color bg=done?Hex("fff6d6"):bad?Hex("ffe9ee"):next?Hex("e3f2ff"):Hex("eef2f8"),fg=done?Hex("7a5a00"):bad?Hex("c23a60"):next?Hex("1f5f99"):PlanGray;
                var card=PCard(screen,"EndlessRoad"+i,x+i*196,y,150,92,bg,20);KitGradient(card.GetComponent<Image>(),bg,done?Hex("ffe7a3"):bad?Hex("ffc9d6"):next?Hex("bfe2ff"):bg);
                var border=card.gameObject.AddComponent<Outline>();border.effectColor=Color.white;border.effectDistance=new Vector2(3,-3);
                StoryText(card,"EndlessRoadYear"+i,year+"年目",0,8,150,24,13,fg,true);StoryText(card,"EndlessRoadRank"+i,record?.rank??"—",0,27,150,38,22,fg,true);
                StoryText(card,"EndlessRoadStatus"+i,done?"運営完了":bad?"運営終了":next?"次の年度":"未記録",0,64,150,26,12,fg,true);
                if(i<2)PCard(screen,"EndlessRoadBar"+i,x+150+i*196,y+42,46,8,done?Hex("ffd23f"):Hex("dfe5ef"),12,false);
            }
        }
        private void ConfirmRetireEndless()
        {
            var d=Dialog("ここで引退しますか？","累計 "+Endless.TotalScore.ToString("N0")+"点を確定します。この挑戦には戻れません。記録・称号・因子は残ります。",340);
            Button(d,"ConfirmRetireEndless","引退して得点を確定する",32,270,420,48,()=>RetireEndless(),Accent);
        }
        private void EndlessEndScreen()
        {
            StoryBackground(true);StoryCategory(screen,"StoryCategory","CHALLENGE RECORD",60,32,450,PlanGray);
            StoryText(screen,"EndingTitle",Endless.retired?"挑戦の記録　ここで引退":"挑戦の記録　"+RunYear+"年目で運営終了",60,52,590,66,36);EndlessRoad(640,36);
            var panel=PCard(screen,"StoryFailure",80,170,700,470,Color.white,24);
            StoryCategory(panel,"FailureCategory","ENDLESS YEARS",26,22,648,PlanPink);StoryText(panel,"FailureHeading",Endless.retired?"ここまでの得点を確定した":"次の挑戦に記録を残す",26,56,648,46,26);
            StoryFailureRow(panel,"EndlessDuration","続いた年数",DurationLabel(Endless.DurationMonths),122,Hex("eef2f8"),PlanInk);
            StoryFailureRow(panel,"EndlessScore","合計点",Endless.TotalScore.ToString("N0")+"点",190,Hex("fff6d6"),Hex("7a5a00"));
            StoryFailureRow(panel,"EndlessRank","総合ランク",OpsCatalog.EndlessRank(Endless.TotalScore),258,Hex("e6f3ff"),Hex("1f5f99"));
            StoryText(panel,"EndlessScoreHint",Endless.retired?"引退した年も年間得点を全額合算":RunYear+"年目の得点は半分（"+State.AnnualScore.ToString("N0")+" → "+(State.AnnualScore/OpsCatalog.EndlessFailureScoreDivisor).ToString("N0")+"点）",26,334,648,45,16,PlanGray,false,true);
            if(Endless.bestUpdated){var badge=PCard(panel,"EndlessBestUpdated",26,408,270,34,PlanBlue,16,false);StoryText(badge,"EndlessBestUpdatedLabel","自己ベスト更新",0,0,270,34,17,Color.white,true);Reveal(badge,.4f,true);}
            var reward=PCard(screen,"EndlessAwards",820,170,700,470,Color.white,24);
            StoryCategory(reward,"AwardCategory","NEW TITLES",26,22,648,PlanPink);StoryText(reward,"AwardHeading","今回の挑戦で獲得した称号",26,56,648,46,24);
            var ids=Endless.earnedTitles??new System.Collections.Generic.List<string>();
            for(int i=0;i<ids.Count;i++){int index=Array.IndexOf(OpsCatalog.TitleIds,ids[i]);var card=PCard(reward,"EndlessAward"+i,26+i%2*326,126+i/2*58,310,48,Hex("fff6d6"),16,false);StoryText(card,"EndlessAwardName"+i,OpsCatalog.TitleNames[index],8,0,294,48,16,Hex("7a5a00"),true);}
            if(ids.Count==0)StoryText(reward,"EndlessNoAwards","新しい称号はなし\nこれまでの称号と自己ベストは残る",26,126,648,100,22,PlanGray,false,true);
            StoryButton("EndlessAnnualReview",RunYear+"年目を振り返る",80,780,326,ReviewStoryAnnual);
            StoryButton("EndlessBackHome","タイトルへ",422,780,330,RenderHome,true);StoryButton("EndlessRecords","記録を見る",768,780,310,OpenRecords);
        }
    }
}
