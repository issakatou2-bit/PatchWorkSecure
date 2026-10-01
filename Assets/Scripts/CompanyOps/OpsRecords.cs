using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public static partial class OpsCatalog
    {
        // ⑤の900挑戦の分布で確定する。年間ランクの数値とは独立。
        public const long EndlessRankB=1500,EndlessRankA=3000,EndlessRankS=4500,EndlessRankSS=6000;
        public const int EndlessRetireTitleYears=5;
        public static readonly int[] EndlessGuardYears={3,5,10};
        public static readonly string[] TitleIds={"guard-3","guard-5","guard-10","no-loss","peaks","bosses","overall-ss","retire-5","diary-all"};
        public static readonly string[] TitleNames={"3年の守り","5年の守り","10年の守り","無傷の一年","山場全勝","強敵をすべて倒した年","総合ランクSS","花道の引退","ひなたの日記を全部読んだ"};
        public static readonly string[] TitleHints={"3年を運営完了","5年を運営完了","10年を運営完了","運営完了した年の被害0","1年の山場4つを達成","1年の強敵をすべて撃退","終わりなき年度の総合ランクSS","5年以上を運営完了して引退","連載36話と結末3つを読む"};
        public static string EndlessRank(long score)=>score>=EndlessRankSS?"SS":score>=EndlessRankS?"S":score>=EndlessRankA?"A":score>=EndlessRankB?"B":"C";
    }
    public sealed partial class OpsCareer
    {
        public int bestDurationMonths;
        public long bestTotalScore;
        public string bestOverallRank;
        public List<string> titles=new List<string>();
        private bool ValidRecords()=>bestDurationMonths>=0&&bestTotalScore>=0&&(string.IsNullOrEmpty(bestOverallRank)||OpsStory.RankValue(bestOverallRank)>=0)&&ValidTitles(titles);
        public static bool ValidTitles(IEnumerable<string> ids)=>ids==null||ids.Count()<=OpsCatalog.TitleIds.Length&&ids.Distinct().Count()==ids.Count()&&ids.All(OpsCatalog.TitleIds.Contains);
        public bool HasTitle(string id)=>titles!=null&&titles.Contains(id);
        private bool Award(string id,OpsEndless run=null)
        {
            if(HasTitle(id))return false;titles=titles??new List<string>();titles.Add(id);
            if(run!=null){run.earnedTitles=run.earnedTitles??new List<string>();run.earnedTitles.Add(id);}return true;
        }
        private bool AwardYears(int years,OpsEndless run=null)
        {bool changed=false;foreach(int n in OpsCatalog.EndlessGuardYears)if(years>=n)changed|=Award("guard-"+n,run);return changed;}
        private bool AwardAnnual(bool operated,int loss,int peaks,int bosses,int defeated,OpsEndless run=null)
        {
            if(!operated)return false;bool changed=false;
            if(loss==0)changed|=Award("no-loss",run);
            if(peaks==OpsCatalog.PeakMonths.Length)changed|=Award("peaks",run);
            if(bosses>0&&defeated==bosses)changed|=Award("bosses",run);return changed;
        }
        public bool RecordDiaryTitle()=>diary!=null&&diary.Count==OpsDiaryCatalog.Entries.Length&&diary.Select(d=>d.key).Distinct().Count()==OpsDiaryCatalog.Entries.Length&&Award("diary-all");
        public bool RecordStoryTitles(OpsStory story)
        {
            if(story==null)return false;bool changed=AwardYears(story.records.Count(r=>r.operated));var s=story.state;
            if(s.phase==OpsPhase.Ended){var bosses=s.history.Where(r=>OpsCatalog.BossFor(s.storyCalendarYear,r.month,r.eventId)!=null).ToArray();changed|=AwardAnnual(s.IsClear,s.totalLoss,s.history.Count(r=>r.peakGoalRecorded&&r.peakGoalMet),bosses.Length,bosses.Count(s.BossDefeated));}
            return changed|RecordDiaryTitle();
        }
        public bool RecordEndless(OpsEndless run)
        {
            if(run==null||!run.Valid())return false;bool changed=AwardYears(run.CompletedYears,run);
            foreach(var r in run.records)changed|=AwardAnnual(r.operated,r.loss,r.wonPeaks,r.bossCount,r.defeatedBossCount,run);
            changed|=RecordDiaryTitle();if(!run.finished||run.recordedCareer)return changed;
            string rank=OpsCatalog.EndlessRank(run.TotalScore);bool best=run.DurationMonths>bestDurationMonths||run.TotalScore>bestTotalScore||OpsStory.RankValue(rank)>OpsStory.RankValue(bestOverallRank??"C");
            bestDurationMonths=Math.Max(bestDurationMonths,run.DurationMonths);bestTotalScore=Math.Max(bestTotalScore,run.TotalScore);
            if(string.IsNullOrEmpty(bestOverallRank)||OpsStory.RankValue(rank)>OpsStory.RankValue(bestOverallRank))bestOverallRank=rank;
            if(rank=="SS")changed|=Award("overall-ss",run);
            if(run.retired&&run.CompletedYears>=OpsCatalog.EndlessRetireTitleYears)changed|=Award("retire-5",run);
            run.bestUpdated=best;run.recordedCareer=true;return true;
        }
    }
}
