using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    // 集計は確定済みの記録だけから作る。設備間の連携を二重に総効果へ足さない。
    public sealed class OpsAnnualSummary
    {
        public sealed class Project
        {
            public string id;
            public int activations, loss, downtime;
        }
        public readonly List<Project> projects = new List<Project>();
        public int[] staffSupport = new int[OpsCatalog.OriginalStaffCount];
        public readonly List<string> encountered = new List<string>();
        public Project Mvp => projects.OrderByDescending(p=>p.loss).ThenByDescending(p=>p.downtime).ThenByDescending(p=>p.activations).FirstOrDefault();
        public int StaffMvp => staffSupport.Max()==0 ? -1 : Array.IndexOf(staffSupport,staffSupport.Max());
        public static OpsAnnualSummary From(OpsState state)
        {
            var summary = new OpsAnnualSummary{staffSupport=new int[state.StaffCount]};
            foreach (var record in state.history)
            {
                if (OpsEventCatalog.Event(record.eventId) != null && !summary.encountered.Contains(record.eventId)) summary.encountered.Add(record.eventId);
                if (record.investmentEffects != null) foreach (var effect in record.investmentEffects)
                {
                    if (OpsCatalog.Index(effect.projectId)<0 || effect.avoidedLoss<=0 && effect.avoidedDowntime<=0) continue;
                    var project=summary.projects.FirstOrDefault(p=>p.id==effect.projectId);
                    if(project==null) { project=new Project{id=effect.projectId};summary.projects.Add(project); }
                    project.activations++; project.loss+=effect.avoidedLoss; project.downtime+=effect.avoidedDowntime;
                }
                if (record.benign || record.power==null || record.power.staff<=0 || string.IsNullOrEmpty(record.power.support)) continue;
                for(int i=0;i<summary.staffSupport.Length;i++)if(record.power.support.StartsWith(OpsGrowthCatalog.StaffNames[i]+"：",StringComparison.Ordinal)) summary.staffSupport[i]++;
            }
            return summary;
        }
        // これは見せ方の分類であり、攻撃・成績の数値ルールには影響しない。
        public static string MonthLabel(OpsState state, OpsOutcome record)
        {
            if(record==null)return "未到達";
            if(record.loss>=10)return "被害大";
            if(record.downtime>=12)return "停止長";
            if(record.hasClosingState && (record.closingBudget<0 || record.closingStability==0))return "運営終了";
            if(record.peakGoalRecorded)return record.peakGoalMet?"山場突破":"目標未達";
            if(state.growthRules>0 && record.month%3==2 && record.hasClosingState && record.closingBudget>=0 && record.closingStability>0)return "山場突破";
            if(state.completedMissions!=null && state.completedMissions.Contains(record.month))return "達成";
            return record.loss==0 && record.downtime==0 ? "無事" : "対応済";
        }
    }
}
