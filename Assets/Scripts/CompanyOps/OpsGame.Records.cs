using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private string dailyPracticeId="";
        private int dailyPracticeDay;
        private bool recordsActive;
        public bool DailyPracticeActive=>dailyPracticeId!="";
        public bool BeginDailyPractice(string id,int day)
        {
            if(MinigameActive||ResolutionActive)return false;var session=OpsDailyPractice.Create(id,day);if(session==null)return false;
            dailyPracticeId=id;dailyPracticeDay=day;
            if(OpenMinigame(session,"",result=>
            {
                if(!result.Delegated){Career.RecordPractice(id,day,result);SaveCareer();}
                dailyPracticeId="";OpenRecords();
            }))return true;
            dailyPracticeId="";return false;
        }
        public void OpenRecords()
        {
            if(MinigameActive)return;
            homeVisible=false;StopVoice();NewScreen();StoryBackground();
            recordsActive=true;
            StoryCategory(screen,"RecordsCategory","RECORDS",80,36,600,PlanPink);StoryText(screen,"RecordsHeading","記録",80,66,1440,70,42);
            string[] names={"最長の年数","最高の合計点","最高の総合ランク"};
            string[] values={Career.bestDurationMonths==0?"未記録":DurationLabel(Career.bestDurationMonths),Career.bestDurationMonths==0?"未記録":Career.bestTotalScore.ToString("N0")+"<size=22>点</size>",Career.bestOverallRank??"—"};
            for(int i=0;i<3;i++)
            {
                var card=PCard(screen,"RecordBest"+i,150+i*440,168,420,135,Color.white,24);
                StoryText(card,"RecordBestLabel"+i,names[i],20,18,380,25,15,PlanGray,true);
                StoryText(card,"RecordBestValue"+i,values[i],20,48,380,74,i==2?36:32,PlanInk,true);
            }
            var list=DecisionScroll(screen,"RecordsList",150,320,1300,436);
            var scroll=list.GetComponentInParent<ScrollRect>();scroll.viewport.sizeDelta=new Vector2(1290,436);scroll.scrollSensitivity=32;
            var rail=PCard(scroll.transform,"RecordsRail",1292,0,8,436,PlanTrack,8,false);
            var handle=PCard(rail,"RecordsHandle",0,0,8,90,PlanPink,8,false);handle.GetComponent<Image>().raycastTarget=true;
            var bar=rail.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<Image>();bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
            var heading=RecordListRow(list,"PracticeHeadingRow",46);
            int day=OpsDailyPractice.Day(DateTime.Today);
            StoryText(heading,"PracticeHeading","今日の一問　"+DateTime.Today.ToString("yyyy/MM/dd"),0,0,600,40,22);
            StoryText(heading,"PracticeHint","同じ日は同じ問題／本編の進行には影響しません",620,0,670,40,15,PlanGray,false,true);
            for(int row=0;row<2;row++)
            {
                var line=RecordListRow(list,"PracticeRow"+row,140);
                for(int col=0;col<3;col++)
                {
                    int index=row*3+col;string id=OpsDailyPractice.Ids[index];var r=Career.PracticeRecord(id);
                    var card=PCard(line,"PracticeCard"+id,col*434,0,418,132,Color.white,24,false);
                    StoryText(card,"PracticeName"+id,OpsDailyPractice.Names[index],18,10,382,30,20);
                    StoryText(card,"PracticeBest"+id,"自己ベスト "+(r==null?"未記録":r.bestScore+"点 / "+r.BestGrade),18,45,250,26,15,PlanInk);
                    StoryText(card,"PracticeDaily"+id,"今日 "+(r==null||r.day!=day?"未挑戦":r.dailyScore+"点 / "+r.DailyGrade),18,79,244,26,15,PlanGray);
                    PButton(card,"PracticePlay"+id,"遊ぶ",288,73,110,42,()=>BeginDailyPractice(id,day),PlanPink,Color.white,18);
                }
            }
            var titlesHeading=RecordListRow(list,"RecordTitlesRow",42);
            StoryText(titlesHeading,"RecordTitlesHeading","称号　"+(Career.titles?.Count??0)+" / "+OpsCatalog.TitleIds.Length,0,0,1200,36,20);
            RectTransform titleRow=null;
            for(int i=0;i<OpsCatalog.TitleIds.Length;i++)
            {
                if(i%3==0)titleRow=RecordListRow(list,"TitleRow"+(i/3),110);
                bool got=Career.HasTitle(OpsCatalog.TitleIds[i]);var card=PCard(titleRow,"RecordTitle"+i,i%3*434,0,418,100,got?Hex("fff6d6"):Hex("eef2f8"),24,false);
                if(got)KitGradient(card.GetComponent<Image>(),Hex("fff6d6"),Hex("ffe7a3"));
                StoryText(card,"RecordTitleName"+i,got?OpsCatalog.TitleNames[i]:"？",14,6,392,50,got?18:20,got?Hex("7a5a00"):PlanGray,true);
                StoryText(card,"RecordTitleHint"+i,OpsCatalog.TitleHints[i],14,53,392,31,13,PlanGray,true,true);
            }
            StoryButton("RecordsClose","タイトルへ",635,782,330,RenderHome);
        }
        private RectTransform RecordListRow(RectTransform parent,string name,float height)
        {
            var row=Rect(parent,name,0,0,1300,height);var layout=row.gameObject.AddComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=height;return row;
        }
        public static string DurationLabel(int months)=>months/12+"年"+(months%12==0?"":months%12+"か月");
    }
}
