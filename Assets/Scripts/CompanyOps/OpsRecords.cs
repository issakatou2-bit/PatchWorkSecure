using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public static partial class OpsCatalog
    {
        // ⑤の考える6方針×100挑戦の10/40/70/90%点。年間ランクとは独立。
        public const long EndlessRankB=7116,EndlessRankA=8032,EndlessRankS=11707,EndlessRankSS=12329;
        public const int EndlessRetireTitleYears=5;
        public static readonly int[] EndlessGuardYears={3,5,10};
        public static readonly string[] TitleIds={"guard-3","guard-5","guard-10","no-loss","peaks","bosses","overall-ss","retire-5","diary-all","minigames-s"};
        public static readonly string[] TitleNames={"3年の守り","5年の守り","10年の守り","無傷の一年","山場全勝","強敵をすべて倒した年","総合ランクSS","花道の引退","ひなたの日記を全部読んだ","全部のミニゲームでS"};
        public static readonly string[] TitleHints={"3年を運営完了","5年を運営完了","10年を運営完了","運営完了した年の被害0","1年の山場4つを達成","1年の強敵をすべて撃退","終わりなき年度の総合ランクSS","5年以上を運営完了して引退","連載36話と結末3つを読む","今日の一問の6本すべてでS"};
        public static string EndlessRank(long score)=>score>=EndlessRankSS?"SS":score>=EndlessRankS?"S":score>=EndlessRankA?"A":score>=EndlessRankB?"B":"C";
    }
    public sealed partial class OpsCareer
    {
        public int bestDurationMonths;
        public long bestTotalScore;
        public string bestOverallRank;
        public List<string> titles=new List<string>();
        public List<OpsPracticeRecord> practiceRecords=new List<OpsPracticeRecord>();
        public List<string> heardMaxims=new List<string>(),yearMaxims=new List<string>();
        public bool maximYearReady;
        public int maximYearSeed,maximYear;
        public void PrepareMaximYear(int seed,int year,bool newAttempt=false)
        {
            if(newAttempt||!maximYearReady||maximYearSeed!=seed||maximYear!=year)yearMaxims=new List<string>();
            maximYearReady=true;maximYearSeed=seed;maximYear=year;
        }
        public bool HeardMaxim(string id,bool annual)
        {
            id=OpsMaxims.Canonical(id);if(!OpsMaxims.Ids.Contains(id))return false;
            heardMaxims=heardMaxims??new List<string>();yearMaxims=yearMaxims??new List<string>();bool changed=false;
            if(!heardMaxims.Contains(id)){heardMaxims.Add(id);changed=true;}
            if(annual&&!yearMaxims.Contains(id)){yearMaxims.Add(id);changed=true;}return changed;
        }
        private bool ValidRecords()=>bestDurationMonths>=0&&bestTotalScore>=0&&(string.IsNullOrEmpty(bestOverallRank)||OpsStory.RankValue(bestOverallRank)>=0)&&ValidTitles(titles)&&
            OpsMaxims.Valid(heardMaxims)&&OpsMaxims.Valid(yearMaxims)&&(!maximYearReady||maximYear>0)&&
            (practiceRecords==null||practiceRecords.Count<=OpsDailyPractice.Ids.Length&&practiceRecords.All(r=>r!=null&&r.Valid())&&practiceRecords.Select(r=>r.id).Distinct().Count()==practiceRecords.Count);
        public OpsPracticeRecord PracticeRecord(string id)=>practiceRecords?.FirstOrDefault(r=>r.id==id);
        public bool RecordPractice(string id,int day,OpsMinigame result)
        {
            if(result==null||result.Phase!=OpsMinigamePhase.Result||result.Delegated||OpsDailyPractice.Id(result)!=id||!OpsDailyPractice.ValidDay(day))return false;
            practiceRecords=practiceRecords??new List<OpsPracticeRecord>();var r=PracticeRecord(id);
            if(r==null){r=new OpsPracticeRecord{id=id};practiceRecords.Add(r);}
            r.bestScore=Math.Max(r.bestScore,result.Score);r.dailyScore=r.day==day?Math.Max(r.dailyScore,result.Score):result.Score;r.day=day;
            if(OpsDailyPractice.Ids.All(key=>PracticeRecord(key)?.BestGrade=="S"))Award("minigames-s");return true;
        }
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
    public static class OpsMaxims
    {
        // 声の版を更新しても、同じ格言の既読は引き継ぐ。
        public static readonly string[] Ids={"maxim_backup","maxim_hurry","maxim_sender","maxim_link","maxim_account","maxim_mfa","maxim_least","maxim_layers","maxim_segment","maxim_uptime","maxim_baseline","maxim_logs","maxim_priority","maxim_restore","maxim_runbook","maxim_human","maxim_report","maxim_password","maxim_usb","maxim_estimate"};
        public static string Canonical(string id)=>id=="maxim_hurry_v2"?"maxim_hurry":id=="maxim_link_v2"?"maxim_link":id=="maxim_segment_v2"?"maxim_segment":id=="maxim_report_v2"?"maxim_report":id;
        public static bool Valid(List<string> ids)=>ids==null||ids.Count<=Ids.Length&&ids.Distinct().Count()==ids.Count&&ids.All(Ids.Contains);
        public static string[] Candidates(string id)
        {
            id=Canonical(id);
            if(new[]{"maxim_hurry","maxim_sender","maxim_link","maxim_account"}.Contains(id))return new[]{id,"maxim_sender","maxim_link","maxim_account","maxim_hurry","maxim_report"}.Distinct().ToArray();
            if(id=="maxim_mfa")return new[]{id,"maxim_password","maxim_human"};
            if(id=="maxim_logs")return new[]{id,"maxim_baseline","maxim_estimate"};
            if(id=="maxim_restore")return new[]{id,"maxim_backup","maxim_runbook","maxim_uptime"};
            if(id=="maxim_priority")return new[]{id,"maxim_runbook","maxim_human"};
            return new[]{id};
        }
    }
    [Serializable] public sealed class OpsPracticeRecord
    {
        public string id;public int bestScore,day,dailyScore;
        public string BestGrade=>OpsDailyPractice.Grade(id,bestScore);
        public string DailyGrade=>OpsDailyPractice.Grade(id,dailyScore);
        public bool Valid()=>OpsDailyPractice.Ids.Contains(id)&&bestScore>=0&&bestScore<=OpsCatalog.MinigameMaxScore&&dailyScore>=0&&dailyScore<=bestScore&&OpsDailyPractice.ValidDay(day);
    }
}
