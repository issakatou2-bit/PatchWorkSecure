using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PatchWorkSecure.CompanyOps;
using PatchWorkSecure.Tests;

// 3年の本編の試算。方針はCompanyOpsDepthChecksの9種類をそのまま写したもの（元を変えたらここも合わせる）。
public static class CompanyOpsThreeYears
{
    sealed class Profile
    {
        public int role, depth;
        public string name;
        public int RestAt => role==0 ? 65-depth*10 : 45-depth*5;
        public int Reserve => depth==0 ? 8 : role==2 ? 18 : 12;
        public int LossWeight => role==0 ? 7 : role==1 ? 4 : 3;
        public int StopWeight => role==2 ? 6 : role==0 ? 4 : 2;
    }
    static readonly string[][] Names={
        new[]{"ゲーマー/気軽","ゲーマー/戦略","ゲーマー/最適化"},
        new[]{"IT学習者/初学","IT学習者/基礎","IT学習者/FE相当の知識を想定"},
        new[]{"実務視点/担当初期","実務視点/運用経験","実務視点/セキュリティ深掘り"}};
    static string Cell(object v)=>"\""+(v??"").ToString().Replace("\"","\"\"")+"\"";
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static PersonaCommand Command(string kind,string id)=>new PersonaCommand(kind,id);
    static string[] Priorities(Profile p,OpsState s)
    {
        string[] baseOrder=p.role==0 ? new[]{"automation","runbook","education","inventory","backup","drill","patch","mfa","monitor","segment","redundancy"} :
            p.role==1 ? new[]{"backup","education","runbook","inventory","mfa","patch","drill","automation","monitor","segment","redundancy"} :
            new[]{"inventory","runbook","patch","backup","drill","monitor","mfa","automation","education","redundancy","segment"};
        return (p.depth==0 ? baseOrder : new[]{s.CurrentMission.projectA,s.CurrentMission.projectB}.Concat(baseOrder))
            .Where(id=>!string.IsNullOrEmpty(id)).Distinct().ToArray();
    }
    static double UpgradeValue(Profile p,OpsState s,int index)
    {
        // 購入前比較と同じ複製から、公開見積もりだけを読む。確定被害・乱数は選択に使わない。
        var preview=s.PreviewUpgrade(index);if(preview==null)return double.NegativeInfinity;
        double improvement=0;
        foreach(string response in CompanyOpsPersonaPolicy.Responses)
        {
            var a=s.Estimate(response);var b=preview.Estimate(response);
            improvement=Math.Max(improvement,p.LossWeight*(a.lossMax-b.lossMax)+p.StopWeight*(a.stopMax-b.stopMax));
        }
        string id=OpsCatalog.Projects[index].id;
        int mission=(id==s.CurrentMission.projectA||id==s.CurrentMission.projectB)&&s.Level(id)==0?10:0;
        int coverage=s.Level(id)==0?8:0;
        int chain=(id=="drill"&&s.Level("backup")>0||id=="runbook"&&s.Level("automation")>0)?8:0;
        return improvement+mission+coverage+chain-s.Cost(index)*.15-OpsCatalog.Projects[index].upkeep*(12-s.month)*.4;
    }
    static PersonaCommand Next(OpsState s,Profile p,PersonaTurn turn)
    {
        if(s.phase!=OpsPhase.Planning||turn.steps>=24)return null;
        if(p.depth>0||s.month%2==0)for(int i=0;i<4;i++)if(s.BubbleAvailable(i)&&s.BubbleKind(i)==6)return Command("bubble",i.ToString());
        if(string.IsNullOrEmpty(s.supportOrder)&&p.depth>0)
        {
            string order=p.role==0?"routine":s.DataRecoveryApplies||s.RestartApplies?"recover":"investigate";
            if(s.SupportBlock(order)=="")return Command("support",order);
        }
        if(s.fatigue>p.RestAt&&s.ActionBlock("rest")=="")return Command("act","rest");
        if(p.depth>0&&s.TicketBlock(true)=="")return Command("ticket","delegate");
        if(p.depth==2&&s.capacity>=3&&!s.practiced&&s.StaffLevel(s.Ticket.member)<2&&s.PracticeBlock(s.Ticket.member)=="")
            return Command("practice",s.Ticket.member.ToString());
        if(p.role==1&&s.ActionBlock("listen")=="")return Command("act","listen");
        if(p.role==2&&s.ActionBlock("audit")=="")return Command("act","audit");
        if(p.depth>0)
            foreach(string action in new[]{s.CurrentMission.actionA,s.CurrentMission.actionB})
                if(!string.IsNullOrEmpty(action)&&s.ActionBlock(action)=="")return Command("act",action);
        if(p.depth==2&&s.Situation.stopLossCap>0&&s.ActionBlock("prepare")=="")return Command("act","prepare");
        var priorities=Priorities(p,s);
        var eligible=priorities.Select(OpsCatalog.Index).Where(i=>s.Level(OpsCatalog.Projects[i].id)<(p.depth==2?2:1)).ToArray();
        var buyable=eligible.Where(i=>s.UpgradeBlock(i)==""&&s.budget>=s.Cost(i)+p.Reserve);
        if(p.depth==2)buyable=buyable.OrderByDescending(i=>UpgradeValue(p,s,i));
        if(turn.purchases<(p.depth==0?1:2))
        {
            int buy=buyable.DefaultIfEmpty(-1).First();if(buy>=0)return Command("buy",OpsCatalog.Projects[buy].id);
            if(p.depth>0&&s.ActionBlock("proposal")=="")
                foreach(int i in eligible)
                {
                    var project=OpsCatalog.Projects[i];
                    if(s.capacity>=s.WorkCost(i)+1&&s.budget+12+s.Evidence*3>=s.Cost(i)+p.Reserve&&
                        (string.IsNullOrEmpty(project.requires)||s.Level(project.requires)>0))return Command("proposal",project.group);
                }
        }
        if(p.role!=0&&s.TicketBlock(false)=="")return Command("ticket","self");
        foreach(string action in new[]{"audit","map","listen","rest"})
            if(s.ActionBlock(action)==""&&(action!="rest"||s.fatigue>12))return Command("act",action);
        return null;
    }
    static string Response(OpsState s,Profile p,HashSet<string> seen)
    {
        if(p.role==1&&p.depth==0&&!seen.Contains(s.Current.lesson))return s.Current.kind=="outage"?"recover":"contain";
        return CompanyOpsPersonaPolicy.Responses.OrderBy(r=>
        {
            var f=s.Estimate(r);
            // 上限重視／中央値重視の違いも、見えている幅だけを使用する。
            double loss=p.depth==0?f.lossMax:(f.lossMax+f.lossMin)*.5;
            double stop=p.depth==0?f.stopMax:(f.stopMax+f.stopMin)*.5;
            return p.LossWeight*loss+p.StopWeight*stop+f.cost*2;
        }).First();
    }
    // 3年の本編の試算。1年目は現行と同じ。2年目以降は脅威を上乗せし、設備・社員・組織の一部を引き継ぐ。
    // 人間の技能・面白さの測定ではない。数値の置き場所を決めるための仮説。
    sealed class Carry { public int[] levels; public int[] staff; public int culture, trust, budget; }
    static int P2=12, P3=24, Decay=1, BudgetCarryDiv=1, BudgetCarryMax=999;
    static OpsState NewYear(int seed,int year,Carry c,int[] inherit)
    {
        var s=new OpsState(seed,true);
        s.yearPressure=year==1?0:year==2?P2:P3;
        if(c!=null)
        {
            // 設備は古くなる：Lv2はLv1へ、Lv1は残す（Decay=1）。Decay=2なら全部1段下げる。
            for(int i=0;i<s.levels.Length;i++)s.levels[i]=Decay==0?c.levels[i]:Decay==1?Math.Min(1,c.levels[i]):Math.Max(0,c.levels[i]-1);
            s.staffExperience=(int[])c.staff.Clone();
            s.culture=c.culture;s.trust=(c.trust+45)/2;
            s.budget+=Math.Min(BudgetCarryMax,Math.Max(0,c.budget)/BudgetCarryDiv);
        }
        if(inherit!=null)for(int i=0;i<inherit.Length;i++)s.levels[inherit[i]]=Math.Max(s.levels[inherit[i]],1);
        return s;
    }
    static void PlayYear(OpsState s,Profile p)
    {
        var seen=new HashSet<string>();
        while(s.phase!=OpsPhase.Ended)
        {
            var turn=new PersonaTurn();PersonaCommand command;
            while((command=Next(s,p,turn))!=null){Check(CompanyOpsPersonaPolicy.ApplyRule(s,command),p.name+"実行不能");CompanyOpsPersonaPolicy.Applied(turn,command);}
            Check(s.BeginIncident(),"事件開始不能");Check(s.Resolve(Response(s,p,seen)),"対応不能");
            if(p.role!=0||p.depth>0||s.Latest.loss>6)seen.Add(s.Current.lesson);
            if(s.QuarterRewardPending)s.ClaimQuarterReward(p.role==0&&s.budget>30?"capacity":"budget");
            Check(s.NextMonth(),"次月不能");
        }
    }
    // 1回の挑戦（目標で打ち切らず3年まで回す）。年ごとのランク、運営終了は"X"。合計点も返す
    static string[] Run(int seed,Profile p,int[] inherit,out int total)
    {
        Carry c=null;total=0;var ranks=new List<string>();
        for(int y=1;y<=3;y++)
        {
            var s=NewYear(seed+y*7919,y,c,inherit);PlayYear(s,p);total+=s.AnnualScore;
            if(!s.IsClear){ranks.Add("X");break;}
            ranks.Add(s.RankCode);c=new Carry{levels=(int[])s.levels.Clone(),staff=(int[])s.staffExperience.Clone(),culture=s.culture,trust=s.trust,budget=s.budget};
        }
        return ranks.ToArray();
    }
    static readonly string[] Order={"C","B","A","S","SS"};
    static int R(string r)=>r=="X"?-1:Array.IndexOf(Order,r);
    // 目標：年ごとの最低ランク。3年とも満たせばクリア
    static bool Pass(string[] ranks,string[] goal){if(ranks.Length<3)return false;for(int y=0;y<3;y++)if(R(ranks[y])<R(goal[y]))return false;return true;}
    static readonly string[][] Goals={new[]{"C","C","C"},new[]{"B","B","B"},new[]{"B","B","A"},new[]{"B","A","A"},new[]{"A","A","A"},new[]{"B","A","S"}};
    public static void Main(string[] args)
    {
        if(args.Length>=3){P2=int.Parse(args[0]);P3=int.Parse(args[1]);Decay=int.Parse(args[2]);}
        if(args.Length>=5){BudgetCarryDiv=int.Parse(args[3]);BudgetCarryMax=int.Parse(args[4]);}
        Console.WriteLine("脅威 2年目+"+P2+" / 3年目+"+P3+" / 設備の経年="+Decay+" / 予算の繰越 ÷"+BudgetCarryDiv+" 最大"+BudgetCarryMax);
        var first=new int[Goals.Length];var byFour=new int[Goals.Length];var yr=new int[3,6];int n=0;
        var casual=new int[Goals.Length];var casualFour=new int[Goals.Length];
        foreach(int role in Enumerable.Range(0,3))foreach(int depth in Enumerable.Range(0,3))
        {
            var p=new Profile{role=role,depth=depth,name=Names[role][depth]};
            for(int cohort=0;cohort<60;cohort++)
            {
                int seed=14+cohort*997;n++;var tries=new List<string[]>();
                for(int attempt=0;attempt<4;attempt++)
                {
                    int[] inh=Priorities(p,new OpsState(seed,true)).Select(OpsCatalog.Index).Where(i=>string.IsNullOrEmpty(OpsCatalog.Projects[i].requires)).Take(Math.Min(3,attempt)).ToArray();
                    int total;tries.Add(Run(seed+attempt*104729,p,inh,out total));
                }
                for(int y=0;y<3;y++)yr[y,tries[0].Length>y?R(tries[0][y])+1:0]++;
                for(int g=0;g<Goals.Length;g++){bool a=Pass(tries[0],Goals[g]),b=tries.Any(t=>Pass(t,Goals[g]));if(a)first[g]++;if(b)byFour[g]++;if(depth==0){if(a)casual[g]++;if(b)casualFour[g]++;}}
            }
        }
        string[] lab={"届かず","C","B","A","S","SS"};
        for(int y=0;y<3;y++)Console.WriteLine((y+1)+"年目のランク（初回）："+string.Join(" / ",Enumerable.Range(0,6).Select(k=>lab[k]+" "+(100.0*yr[y,k]/n).ToString("F0")+"%")));
        for(int g=0;g<Goals.Length;g++)Console.WriteLine("目標 "+string.Join("→",Goals[g])+"：初回 "+(100.0*first[g]/n).ToString("F0")+"% / 4回目まで "+(100.0*byFour[g]/n).ToString("F0")+"%（気軽な方針だけ 初回 "+(100.0*casual[g]/(n/3)).ToString("F0")+"% / 4回目まで "+(100.0*casualFour[g]/(n/3)).ToString("F0")+"%）");
    }
}
