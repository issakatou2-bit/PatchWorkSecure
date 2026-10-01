using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public static partial class OpsCatalog
    {
        public const int EndlessPressureBase=24,EndlessPressureLinear=16,EndlessPressureQuadratic=2;
        public const int EndlessMonthlyIncomePerYear=2;
        public const int EndlessFailureScoreDivisor=2,EndlessMonthsPerYear=12;
        public static int EndlessIncome(int year)=>year<=StoryYears?0:checked((year-StoryYears)*EndlessMonthlyIncomePerYear);
        public static int EndlessPressure(int year)
        {
            if(year<1)throw new ArgumentException("年度が不正です");
            if(year<=StoryYears)return StoryPressure(year,year==1?0:StoryThreatVersion);
            long n=(long)year-StoryYears;return checked((int)(EndlessPressureBase+EndlessPressureLinear*n+EndlessPressureQuadratic*n*n));
        }
    }
    public partial class OpsState
    {
        // 0なら旧年度・本編。暦は3年目の部品を再利用し、実際の年は別に保持する。
        public int endlessYear;
        public static OpsState NewEndlessYear(int seed,int year,OpsState previous,IEnumerable<string> factors)
        {
            if(year<1||year==1&&previous!=null||year>1&&(previous==null||!previous.Valid()||!previous.IsClear||previous.endlessYear!=year-1))throw new ArgumentException("引き継げる年度ではありません");
            OpsState s;
            if(year<=OpsCatalog.StoryYears)s=NewStoryYear(seed,year,previous,factors);
            else
            {
                if(!OpsCareer.ValidFactors(factors??Enumerable.Empty<string>()))throw new ArgumentException("因子が不正です");
                s=new OpsState(seed,true){storyCalendarYear=OpsCatalog.StoryYears,yearGrowthRules=OpsCatalog.YearGrowthVersion,yearThreatRules=OpsCatalog.StoryThreatVersion,
                    yearPressure=OpsCatalog.EndlessPressure(year),levels=new int[OpsCatalog.YearEquipmentCount],previousStoryEvents=new string[0]};
                // 3年を一巡した後は重複を許す。同じ年度の12件は重複しない。
                s.eventSchedule=OpsEventCatalog.StorySchedule(seed,s.storyCalendarYear,s.previousStoryEvents,true);s.CarryYear(previous);
            }
            s.endlessYear=year;s.monthStartMetrics=s.ReportMetrics;return s;
        }
        private bool ValidEndlessYear()
        {
            if(endlessYear==0)return true;
            if(endlessYear<1||yearThreatRules!=(endlessYear==1?0:OpsCatalog.StoryThreatVersion)||storyCalendarYear!=(endlessYear==1?0:Math.Min(OpsCatalog.StoryYears,endlessYear)))return false;
            try{return yearPressure==OpsCatalog.EndlessPressure(endlessYear);}catch(OverflowException){return false;}
        }
        private int SaveBudgetLimit=>endlessYear>0?int.MaxValue:yearPressure==0?OpsCatalog.LegacySaveBudgetLimit:OpsCatalog.StorySaveBudgetLimit;
        private long SaveIncidentLimit=>endlessYear>0?(long)200+yearPressure:200;
    }
    [Serializable] public sealed class OpsEndlessYear
    {
        public int year,score,loss,stop,months,wonPeaks,bossCount,defeatedBossCount;
        public string rank;
        public bool operated;
        public bool Valid()=>year>0&&score>=0&&loss>=0&&stop>=0&&months>=0&&months<=OpsCatalog.EndlessMonthsPerYear&&OpsStory.RankValue(rank)>=0&&
            wonPeaks>=0&&wonPeaks<=OpsCatalog.PeakMonths.Length&&bossCount>=0&&bossCount<=OpsCatalog.PeakMonths.Length&&defeatedBossCount>=0&&defeatedBossCount<=bossCount&&(!operated||months==OpsCatalog.EndlessMonthsPerYear);
    }
    [Serializable] public sealed class OpsEndless
    {
        public int version=OpsCatalog.StorySaveVersion,seed,year=1;
        public OpsState state;
        public List<string> factors=new List<string>();
        public List<OpsEndlessYear> records=new List<OpsEndlessYear>();
        public bool finished,retired;
        public bool recordedCareer,bestUpdated;
        public List<string> earnedTitles=new List<string>();
        public int YearSeed=>unchecked(seed+year*OpsCatalog.StorySeedStride);
        public long TotalScore=>records.Sum(r=>(long)r.score/(r.operated?1:OpsCatalog.EndlessFailureScoreDivisor));
        public int CompletedYears=>records.Count(r=>r.operated);
        public int DurationMonths=>(year-1)*OpsCatalog.EndlessMonthsPerYear+(state?.history.Count??0);
        public bool CanAdvance=>!finished&&records.Count==year&&records[year-1].operated;
        public static OpsEndless Begin(int seed,IEnumerable<string> factors)
        {var e=new OpsEndless{seed=seed,factors=(factors??Enumerable.Empty<string>()).ToList()};e.state=OpsState.NewEndlessYear(e.YearSeed,1,null,e.factors);return e;}
        public bool RecordYear()
        {
            if(state==null||state.phase!=OpsPhase.Ended||records.Count!=year-1)return false;
            var bosses=state.history.Where(r=>OpsCatalog.BossFor(state.storyCalendarYear,r.month,r.eventId)!=null).ToArray();
            records.Add(new OpsEndlessYear{year=year,score=state.AnnualScore,loss=state.totalLoss,stop=state.totalDowntime,rank=state.RankCode,months=state.history.Count,operated=state.IsClear,
                wonPeaks=state.history.Count(r=>r.peakGoalRecorded&&r.peakGoalMet),bossCount=bosses.Length,defeatedBossCount=bosses.Count(state.BossDefeated)});
            finished=!state.IsClear;return true;
        }
        public bool AdvanceYear()
        {
            RecordYear();if(!CanAdvance)return false;
            var next=OpsState.NewEndlessYear(unchecked(seed+(year+1)*OpsCatalog.StorySeedStride),year+1,state,factors);year++;state=next;return true;
        }
        public bool Retire(){RecordYear();if(!CanAdvance)return false;retired=finished=true;return true;}
        public bool Valid()
        {
            if(version!=OpsCatalog.StorySaveVersion||year<1||state==null||state.endlessYear!=year||!state.Valid()||state.seed!=YearSeed||!OpsCareer.ValidFactors(factors)||records==null||records.Count<year-1||records.Count>year||records.Any(r=>r==null||!r.Valid())||
                !OpsCareer.ValidTitles(earnedTitles)||recordedCareer&&!finished||bestUpdated&&!recordedCareer)return false;
            for(int i=0;i<records.Count;i++)if(records[i].year!=i+1||i<year-1&&!records[i].operated)return false;
            bool recorded=records.Count==year;
            if(recorded)
            {
                var r=records[year-1];var bosses=state.history.Where(h=>OpsCatalog.BossFor(state.storyCalendarYear,h.month,h.eventId)!=null).ToArray();
                if(state.phase!=OpsPhase.Ended||r.score!=state.AnnualScore||r.rank!=state.RankCode||r.loss!=state.totalLoss||r.stop!=state.totalDowntime||r.months!=state.history.Count||r.operated!=state.IsClear||
                    r.wonPeaks!=state.history.Count(h=>h.peakGoalRecorded&&h.peakGoalMet)||r.bossCount!=bosses.Length||r.defeatedBossCount!=bosses.Count(state.BossDefeated))return false;
            }
            return finished==(recorded&&(!state.IsClear||retired))&&(!retired||recorded&&state.IsClear);
        }
    }
}
