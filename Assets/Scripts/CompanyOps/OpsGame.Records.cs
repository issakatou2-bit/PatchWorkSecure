using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public void OpenRecords()
        {
            homeVisible=false;StopVoice();NewScreen();StoryBackground();
            StoryCategory(screen,"RecordsCategory","RECORDS",80,36,600,PlanPink);StoryText(screen,"RecordsHeading","記録",80,66,1440,70,42);
            string[] names={"最長の年数","最高の合計点","最高の総合ランク"};
            string[] values={Career.bestDurationMonths==0?"未記録":DurationLabel(Career.bestDurationMonths),Career.bestDurationMonths==0?"未記録":Career.bestTotalScore.ToString("N0")+"<size=22>点</size>",Career.bestOverallRank??"—"};
            for(int i=0;i<3;i++)
            {
                var card=PCard(screen,"RecordBest"+i,150+i*440,168,420,135,Color.white,24);
                StoryText(card,"RecordBestLabel"+i,names[i],20,18,380,25,15,PlanGray,true);
                StoryText(card,"RecordBestValue"+i,values[i],20,48,380,74,i==2?36:32,PlanInk,true);
            }
            StoryText(screen,"RecordTitlesHeading","称号　"+(Career.titles?.Count??0)+" / "+OpsCatalog.TitleIds.Length,150,320,1200,30,20);
            for(int i=0;i<OpsCatalog.TitleIds.Length;i++)
            {
                bool got=Career.HasTitle(OpsCatalog.TitleIds[i]);var card=PCard(screen,"RecordTitle"+i,150+i%3*440,368+i/3*118,420,100,got?Hex("fff6d6"):Hex("eef2f8"),24,false);
                if(got)KitGradient(card.GetComponent<Image>(),Hex("fff6d6"),Hex("ffe7a3"));
                StoryText(card,"RecordTitleName"+i,got?OpsCatalog.TitleNames[i]:"？",14,6,392,50,got?18:20,got?Hex("7a5a00"):PlanGray,true);
                StoryText(card,"RecordTitleHint"+i,OpsCatalog.TitleHints[i],14,53,392,31,13,PlanGray,true,true);
            }
            StoryButton("RecordsClose","タイトルへ",635,782,330,RenderHome);
        }
        public static string DurationLabel(int months)=>months/12+"年"+(months%12==0?"":months%12+"か月");
    }
}
