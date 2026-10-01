using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public static partial class OpsEventCatalog
    {
        // 通常年度の40件と既存の8件は変えない。年別の山場にだけ固定する。
        public static readonly OpsEvent[] BossEvents=OpsCatalog.StoryCompanies.Skip(1).SelectMany(y=>y.rivals)
            .Where(r=>r.id!="y3-final").Select(r=>new OpsEvent{id=r.id,profile=r.profile,title=r.name,news=r.hint,
                boss="この依頼や兆候、本物か確認して対応できる？",person="社長・加藤",staff="エンジニアさん：記録と依頼の経路を照合しよう。",
                symptom=r.identity,finding="関係する端末・接続先・承認の記録を照合。",hint=r.hint,
                threats=r.profile=="ransom"?new[]{1}:r.profile=="supply"?new[]{2}:r.profile=="bec"?new[]{10,3}:new[]{5},
                calm="記録と既知の経路を確かめた結果、正当な活動でした。感染や侵害は確認されませんでした。",source="ipa"}).ToArray();
    }
    public static partial class OpsCatalog
    {
        public static OpsYearRival BossFor(int year,int month,string eventId)
        {
            if(year<2||year>StoryYears||!PeakMonths.Contains(month))return null;
            var fixedBoss=CompanyYear(year).rivals.FirstOrDefault(r=>r.month==month&&r.id==eventId);
            if(fixedBoss!=null)return fixedBoss;
            var e=OpsEventCatalog.Event(eventId);
            // 2年目の3月だけは抽選された題材を正として札を作る。
            if(year!=2||month!=MarchPeak||e==null)return null;
            var profile=OpsEventCatalog.Profile(e.profile);
            return new OpsYearRival{id=eventId,month=month,name=e.title,stars=3,shape=2,hint=e.hint,identity=e.symptom,profile=e.profile,equipment=new[]{profile.projectA,profile.projectB}};
        }
    }
    public partial class OpsState
    {
        public OpsYearRival CurrentBoss=>yearGrowthRules>0?OpsCatalog.BossFor(storyCalendarYear,month,CurrentEvent?.id):null;
        public bool BossDefeated(OpsOutcome result)=>result!=null&&yearGrowthRules>0&&OpsCatalog.BossFor(storyCalendarYear,result.month,result.eventId)!=null&&
            !result.benign&&result.forecast!=null&&2*result.loss<=result.forecast.lossMin+result.forecast.lossMax&&2*result.downtime<=result.forecast.stopMin+result.forecast.stopMax;
    }
    [Serializable] public sealed class OpsBossDefeat
    {
        public int year,month;
        public string eventId;
        public string Key=>year+":"+eventId;
        public OpsYearRival Boss=>OpsCatalog.BossFor(year,month,eventId);
        public bool Valid()=>Boss!=null;
    }
    public sealed partial class OpsCareer
    {
        public List<OpsBossDefeat> defeatedBosses=new List<OpsBossDefeat>();
        public bool RecordBoss(OpsState state,OpsOutcome result)
        {
            if(state==null||state.history==null||!state.history.Contains(result)||!state.BossDefeated(result))return false;
            var item=new OpsBossDefeat{year=state.storyCalendarYear,month=result.month,eventId=result.eventId};
            defeatedBosses=defeatedBosses??new List<OpsBossDefeat>();
            if(defeatedBosses.Any(b=>b.Key==item.Key))return false;defeatedBosses.Add(item);return true;
        }
        private bool ValidBosses()=>defeatedBosses==null||defeatedBosses.Count<=2*(OpsEventCatalog.Events.Length+OpsEventCatalog.StoryEvents.Length+OpsEventCatalog.BossEvents.Length)&&
            defeatedBosses.All(b=>b!=null&&b.Valid())&&defeatedBosses.Select(b=>b.Key).Distinct().Count()==defeatedBosses.Count;
    }
}
