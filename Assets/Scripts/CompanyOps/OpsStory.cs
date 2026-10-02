using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsState
    {
        // 年の種は呼び出し元が決める。事件を引き直さず、1年目の通常年度との比較にも使う。
        public static OpsState NewStoryYear(int yearSeed,int year,OpsState previous,IEnumerable<string> factors)
        {
            if(year<1||year>OpsCatalog.StoryYears||year==1&&previous!=null||year>1&&(previous==null||!previous.Valid()||!previous.IsClear))throw new ArgumentException("引き継げる年度ではありません");
            var ids=(factors??Enumerable.Empty<string>()).ToArray();
            if(!OpsCareer.ValidFactors(ids))throw new ArgumentException("因子が不正です");
            int threatRules=year>1?OpsCatalog.StoryThreatVersion:0;
            var s=new OpsState(yearSeed,true){yearPressure=OpsCatalog.StoryPressure(year,threatRules),yearThreatRules=threatRules};
            if(previous!=null)
            {
                s.storyCalendarYear=year;
                s.yearGrowthRules=OpsCatalog.YearGrowthVersion;s.levels=new int[OpsCatalog.YearEquipmentCount];
                // 旧2年目の保存にも対応。初年度の既存抽選を種から復元し、今の年度は引き直さない。
                var earlier=previous.previousStoryEvents??(year==3?OpsEventCatalog.Schedule(unchecked(yearSeed-(year-1)*OpsCatalog.StorySeedStride),false):new string[0]);
                s.previousStoryEvents=earlier.Concat(previous.eventSchedule??new string[0]).Distinct().ToArray();
                s.eventSchedule=OpsEventCatalog.StorySchedule(yearSeed,year,s.previousStoryEvents,true);
                s.CarryYear(previous);
            }
            else foreach(string id in ids)s.levels[OpsCatalog.Index(id)]=OpsCatalog.StoryEquipmentLevel;
            s.monthStartMetrics=s.ReportMetrics;
            return s;
        }
        private void CarryYear(OpsState previous)
        {
            for(int i=0;i<previous.levels.Length;i++)levels[i]=Math.Min(OpsCatalog.StoryEquipmentLevel,previous.levels[i]);
            staffExperience=new int[StaffCount];Array.Copy(previous.staffExperience,staffExperience,previous.staffExperience.Length);culture=previous.culture;
            trust=(previous.trust+OpsCatalog.StoryTrustBaseline)/OpsCatalog.StoryTrustDivisor;
            // 全額繰越。試算用のMax=999を本番の上限にしない。
            budget=checked(OpsCatalog.StoryInitialBudget+Math.Max(0,previous.budget));
        }
    }
    [Serializable] public sealed class OpsStoryYear
    {
        public int year,score,loss,stop;
        public string rank;
        public bool operated,goalMet;
        // 表示用の個別比較の記録。古い保存のnullは未記録。総効果や報酬には足さない。
        public List<OpsInvestmentEffect> presentationEffects;
        public bool Valid()=>year>=1&&year<=OpsCatalog.StoryYears&&score>=0&&loss>=0&&stop>=0&&OpsStory.RankValue(rank)>=0&&goalMet==(operated&&OpsStory.RankValue(rank)>=OpsStory.RankValue(OpsCatalog.StoryGoals[year-1]))&&
            (presentationEffects==null||presentationEffects.Count<=OpsCatalog.YearEquipmentCount&&presentationEffects.All(e=>e!=null&&OpsCatalog.Index(e.projectId)>=0&&e.avoidedLoss>=0&&e.avoidedDowntime>=0));
    }
    [Serializable] public sealed class OpsStory
    {
        public int version=OpsCatalog.StorySaveVersion,seed,year=1;
        public OpsState state;
        public List<string> factors=new List<string>();
        public List<OpsStoryYear> records=new List<OpsStoryYear>();
        public bool finished,cleared,rewardClaimed;
        public string earnedFactor="";
        public string Goal=>OpsCatalog.StoryGoals[year-1];
        public bool CanAdvance=>!finished&&records.Count==year&&records[year-1].goalMet;
        public static int RankValue(string rank)
        {switch(rank){case "C":return 0;case "B":return 1;case "A":return 2;case "S":return 3;case "SS":return 4;default:return -1;}}
        public static OpsStory Begin(int seed,IEnumerable<string> factors)
        {
            var story=new OpsStory{seed=seed,factors=(factors??Enumerable.Empty<string>()).ToList()};
            story.state=OpsState.NewStoryYear(story.YearSeed,1,null,story.factors);return story;
        }
        public int YearSeed=>unchecked(seed+year*OpsCatalog.StorySeedStride);
        public bool RecordYear()
        {
            if(state==null||state.phase!=OpsPhase.Ended||records.Count!=year-1)return false;
            bool met=state.IsClear&&RankValue(state.RankCode)>=RankValue(Goal);
            records.Add(new OpsStoryYear{year=year,rank=state.RankCode,score=state.AnnualScore,loss=state.totalLoss,stop=state.totalDowntime,operated=state.IsClear,goalMet=met,
                presentationEffects=state.history.Where(r=>r.investmentEffects!=null).SelectMany(r=>r.investmentEffects).Where(e=>e.avoidedLoss>0||e.avoidedDowntime>0).GroupBy(e=>e.projectId).Select(g=>new OpsInvestmentEffect{projectId=g.Key,level=1,avoidedLoss=g.Sum(e=>e.avoidedLoss),avoidedDowntime=g.Sum(e=>e.avoidedDowntime)}).ToList()});
            finished=!met||year==OpsCatalog.StoryYears;cleared=met&&year==OpsCatalog.StoryYears;return true;
        }
        public bool AdvanceYear()
        {
            RecordYear();if(!CanAdvance)return false;
            var previous=state;year++;state=OpsState.NewStoryYear(YearSeed,year,previous,factors);return true;
        }
        public OpsFactorCandidate[] FactorCandidates(OpsCareer career)
        {
            var result=new List<OpsFactorCandidate>();
            Action<string,string,int> add=(id,reason,source)=>
            {
                if(OpsCatalog.Index(id)<0)return;
                var p=OpsCatalog.AllProjects[OpsCatalog.Index(id)];
                while(!string.IsNullOrEmpty(p.requires))p=OpsCatalog.Projects[OpsCatalog.Index(p.requires)];
                if(career.factors.Contains(p.id)||result.Any(c=>c.id==p.id)||result.Count>=OpsCatalog.StoryFactorSlots)return;
                result.Add(new OpsFactorCandidate{id=p.id,reason=reason,sourceYear=source,stars=year});
            };
            var potential=state.history.Where(r=>r.potentialInvestmentEffects!=null).SelectMany(r=>r.potentialInvestmentEffects).OrderByDescending(e=>e.avoidedLoss*7+e.avoidedDowntime*4);
            foreach(var e in potential){add(e.projectId,"いちばん効きそう",year);if(result.Count>0)break;}
            add(state.Level("inventory")==0?"inventory":state.culture<65?"education":"runbook","把握と判断を助ける",year);
            var effects=records.Where(r=>r.presentationEffects!=null).SelectMany(r=>r.presentationEffects).GroupBy(e=>e.projectId).OrderByDescending(g=>g.Sum(e=>e.avoidedLoss*7+e.avoidedDowntime*4));
            foreach(var g in effects){int source=records.First(r=>r.presentationEffects!=null&&r.presentationEffects.Any(e=>e.projectId==g.Key)).year;add(g.Key,"挑戦のMVP",source);if(result.Count>=3)break;}
            foreach(var p in OpsCatalog.Projects.Where(p=>string.IsNullOrEmpty(p.requires)).OrderByDescending(p=>state.Level(p.id)))add(p.id,"次の挑戦の備え",year);
            return result.ToArray();
        }
        public bool Valid()
        {
            if(version!=OpsCatalog.StorySaveVersion||year<1||year>OpsCatalog.StoryYears||state==null||state.endlessYear!=0||!state.Valid()||state.seed!=YearSeed||state.yearPressure!=OpsCatalog.StoryPressure(year,state.yearThreatRules)||!OpsCareer.ValidFactors(factors))return false;
            if(records==null||records.Count<year-1||records.Count>year||records.Any(r=>r==null||!r.Valid()))return false;
            for(int i=0;i<records.Count;i++)if(records[i].year!=i+1||i<year-1&&!records[i].goalMet)return false;
            bool recorded=records.Count==year;
            if(recorded&&(state.phase!=OpsPhase.Ended||records[year-1].rank!=state.RankCode||records[year-1].score!=state.AnnualScore||records[year-1].loss!=state.totalLoss||records[year-1].stop!=state.totalDowntime||records[year-1].operated!=state.IsClear))return false;
            bool end=recorded&&(!records[year-1].goalMet||year==OpsCatalog.StoryYears);
            if(finished!=end||cleared!=(end&&year==OpsCatalog.StoryYears&&records[year-1].goalMet)||rewardClaimed&&!finished)return false;
            return rewardClaimed?OpsCatalog.Index(earnedFactor)>=0:string.IsNullOrEmpty(earnedFactor);
        }
    }
    public sealed class OpsFactorCandidate { public string id,reason; public int stars,sourceYear; }
    [Serializable] public sealed partial class OpsCareer
    {
        public int version=OpsCatalog.StorySaveVersion,finishedAttempts;
        public int startedStoryAttempts;
        public bool endlessUnlocked;
        public List<string> factors=new List<string>();
        public static bool ValidFactors(IEnumerable<string> factors)
        {if(factors==null)return false;var ids=factors.ToArray();return ids.Length<=OpsCatalog.StoryFactorSlots&&ids.Distinct().Count()==ids.Length&&ids.All(id=>OpsCatalog.Index(id)>=0&&OpsCatalog.Index(id)<OpsCatalog.BaseEquipmentCount);}
        public List<OpsDiaryRecord> diary=new List<OpsDiaryRecord>();
        public List<int> seenOpeningYears=new List<int>();
        public bool Valid()=>version==OpsCatalog.StorySaveVersion&&finishedAttempts>=0&&startedStoryAttempts>=0&&ValidFactors(factors)&&ValidBosses()&&ValidRecords()&&
            (diary==null||diary.Count<=OpsDiaryCatalog.Entries.Length&&diary.All(p=>p!=null&&p.Valid())&&diary.Select(p=>p.key).Distinct().Count()==diary.Count)&&
            (seenOpeningYears==null||seenOpeningYears.Count<=2&&seenOpeningYears.Distinct().Count()==seenOpeningYears.Count&&seenOpeningYears.TrueForAll(y=>y==2||y==3));
        // 選択画面はNext-9。満杯のときは指定枠を置換できる。二重受取はしない。
        public bool Claim(OpsStory story,string id,int replaceSlot=-1)
        {
            if(story==null||!story.Valid()||!story.finished||story.rewardClaimed||!Valid()||!ValidFactors(new[]{id}))return false;
            if(!factors.Contains(id))
            {
                if(factors.Count<OpsCatalog.StoryFactorSlots)factors.Add(id);
                else if(replaceSlot>=0&&replaceSlot<factors.Count)factors[replaceSlot]=id;
                else return false;
            }
            story.earnedFactor=id;story.rewardClaimed=true;finishedAttempts++;endlessUnlocked|=story.cleared;return true;
        }
    }
    [Serializable] public sealed class OpsProgress
    {
        public int format=OpsCatalog.ProgressSaveVersion;
        public bool storyMode;
        public bool endlessMode;
        public OpsState single;
        public OpsStory story;
        public OpsEndless endless;
        public OpsCareer career=new OpsCareer();
        public OpsState Current=>endless!=null?endless.state:story==null?single:story.state;
        public bool Valid()=>format==OpsCatalog.ProgressSaveVersion&&career!=null&&career.Valid()&&
            (endless!=null?story==null&&single==null&&career.endlessUnlocked&&endless.Valid():story==null?single!=null&&single.endlessYear==0&&single.Valid()&&single.yearPressure==0:single==null&&story.Valid())&&
            (!((story?.cleared??false)&&(story?.rewardClaimed??false))||career.endlessUnlocked);
    }
}
