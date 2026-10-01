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
        var unlocked=Production&&s.yearGrowthRules>0?OpsCatalog.AdvancedProjects.Where(project=>s.EquipmentAvailable(OpsCatalog.Index(project.id))).Select(project=>project.id):Enumerable.Empty<string>();
        return (p.depth==0 ? baseOrder.Concat(unlocked) : new[]{s.CurrentMission.projectA,s.CurrentMission.projectB}.Concat(unlocked).Concat(baseOrder))
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
        string id=OpsCatalog.AllProjects[index].id;
        int mission=(id==s.CurrentMission.projectA||id==s.CurrentMission.projectB)&&s.Level(id)==0?10:0;
        int coverage=s.Level(id)==0?8:0;
        int chain=(id=="drill"&&s.Level("backup")>0||id=="runbook"&&s.Level("automation")>0)?8:0;
        return improvement+mission+coverage+chain-s.Cost(index)*.15-OpsCatalog.AllProjects[index].upkeep*(12-s.month)*.4;
    }
    static PersonaCommand Next(OpsState s,Profile p,PersonaTurn turn)
    {
        if(s.phase!=OpsPhase.Planning||turn.steps>=24)return null;
        if(Production&&s.EngineerResearchBlock=="")return Command("engineer","");
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
        var eligible=priorities.Select(OpsCatalog.Index).Where(i=>s.Level(OpsCatalog.AllProjects[i].id)<(p.depth==2?2:1)).ToArray();
        var buyable=eligible.Where(i=>s.UpgradeBlock(i)==""&&s.budget>=s.Cost(i)+p.Reserve);
        if(p.depth==2)buyable=buyable.OrderByDescending(i=>UpgradeValue(p,s,i));
        if(turn.purchases<(p.depth==0?1:2))
        {
            int buy=buyable.DefaultIfEmpty(-1).First();if(buy>=0)return Command("buy",OpsCatalog.AllProjects[buy].id);
            if(p.depth>0&&s.ActionBlock("proposal")=="")
                foreach(int i in eligible)
                {
                    var project=OpsCatalog.AllProjects[i];
                    if(s.capacity>=s.WorkCost(i)+1&&s.budget+(Production?s.ProposalOffer:12+s.Evidence*3)>=s.Cost(i)+p.Reserve&&
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
    static int P2=6, P3=12, Decay=1, BudgetCarryDiv=2, BudgetCarryMax=30;
    static OpsState NewYear(int seed,int year,Carry c,int[] inherit)
    {
        var s=new OpsState(seed,true);
        s.yearPressure=year==1?0:year==2?P2:P3;
        if(c!=null)
        {
            if(Production){s.storyCalendarYear=year;s.yearGrowthRules=OpsCatalog.YearGrowthVersion;s.yearThreatRules=OpsCatalog.StoryThreatVersion;s.levels=new int[OpsCatalog.YearEquipmentCount];}
            // 設備は古くなる：Lv2はLv1へ、Lv1は残す（Decay=1）。Decay=2なら全部1段下げる。
            for(int i=0;i<Math.Min(s.levels.Length,c.levels.Length);i++)s.levels[i]=Decay==0?c.levels[i]:Decay==1?Math.Min(1,c.levels[i]):Math.Max(0,c.levels[i]-1);
            if(Production){s.staffExperience=new int[s.StaffCount];Array.Copy(c.staff,s.staffExperience,c.staff.Length);}
            else s.staffExperience=(int[])c.staff.Clone();
            s.culture=c.culture;s.trust=(c.trust+45)/2;
            s.budget+=Math.Min(BudgetCarryMax,Math.Max(0,c.budget)/BudgetCarryDiv);
        }
        if(inherit!=null)for(int i=0;i<inherit.Length;i++)s.levels[inherit[i]]=Math.Max(s.levels[inherit[i]],1);
        return s;
    }
    static void PlayYear(OpsState s,Profile p,Action<OpsState> afterMonth=null)
    {
        var seen=new HashSet<string>();
        while(s.phase!=OpsPhase.Ended)
        {
            var turn=new PersonaTurn();PersonaCommand command;
            while((command=Next(s,p,turn))!=null){Check(command.kind=="engineer"?s.RequestEngineerResearch():CompanyOpsPersonaPolicy.ApplyRule(s,command),p.name+"実行不能");CompanyOpsPersonaPolicy.Applied(turn,command);}
            Check(s.BeginIncident(),"事件開始不能");Check(s.Resolve(Response(s,p,seen)),"対応不能");
            if(p.role!=0||p.depth>0||s.Latest.loss>6)seen.Add(s.Current.lesson);
            if(s.QuarterRewardPending)s.ClaimQuarterReward(p.role==0&&s.budget>30?"capacity":"budget");
            Check(s.NextMonth(),"次月不能");afterMonth?.Invoke(s);
        }
    }
    // 2年目以降の成長目標を年ごとに替える案（Milestones=1）。1年目の3つを外し、年末の状態で新しい3つを判定した近似。
    static int Milestones=0;
    static bool Production;
    static int ComparedYears,ComparedChallenges,LargestCarry;
    static int Adjusted(OpsState s,int y)
    {
        if(Milestones==0||y==1)return s.AnnualScore;
        int L(string id)=>s.Level(id);int staff3=Enumerable.Range(0,3).Count(i=>s.StaffLevel(i)>=3),staff2=Enumerable.Range(0,3).Count(i=>s.StaffLevel(i)>=2);
        bool[] got=y==2?new[]{L("inventory")>=2&&L("segment")>=1, L("mfa")>=2&&L("monitor")>=1, L("runbook")>=2&&staff3>=1}
                       :new[]{L("backup")>=2&&L("drill")>=2, L("monitor")>=2&&staff2>=3, L("education")>=2&&s.culture>=85};
        return Math.Max(0,s.AnnualScore-s.milestones.Count*30+got.Count(g=>g)*30);
    }
    // 1回の挑戦（目標で打ち切らず3年まで回す）。年ごとのランク、運営終了は"X"。合計点も返す
    static string[] Run(int seed,Profile p,int[] inherit,out int total)
    {
        Carry c=null;total=0;var ranks=new List<string>();OpsState previous=null;
        var story=Production?OpsStory.Begin(seed,inherit==null?null:inherit.Select(i=>OpsCatalog.Projects[i].id)):null;
        for(int y=1;y<=3;y++)
        {
            var s=NewYear(seed+y*7919,y,c,inherit);
            if(Production)
            {
                var actual=OpsState.NewStoryYear(seed+y*OpsCatalog.StorySeedStride,y,previous,inherit==null?null:inherit.Select(i=>OpsCatalog.Projects[i].id));
                Check(actual.Valid()&&actual.seed==s.seed&&actual.yearThreatRules==s.yearThreatRules&&actual.yearPressure==s.yearPressure&&actual.budget==s.budget&&actual.culture==s.culture&&actual.trust==s.trust&&actual.fatigue==s.fatigue&&actual.capacity==s.capacity&&actual.levels.SequenceEqual(s.levels)&&actual.staffExperience.SequenceEqual(s.staffExperience),"試算と本実装の引き継ぎが違う");
                if(previous!=null)LargestCarry=Math.Max(LargestCarry,previous.budget);ComparedYears++;
                if(!story.finished){Check(story.state.seed==actual.seed&&story.state.budget==actual.budget&&story.state.levels.SequenceEqual(actual.levels)&&story.state.staffExperience.SequenceEqual(actual.staffExperience),"本編進行と年度生成が違う");actual=story.state;}
                s=actual;
            }
            PlayYear(s,p);int score=Adjusted(s,y);total+=score;
            if(Production){previous=s;if(!story.finished){Check(story.RecordYear()&&story.Valid(),"年度目標の記録不能");if(story.CanAdvance)Check(story.AdvanceYear()&&story.Valid(),"年度を進められない");}}
            if(!s.IsClear){ranks.Add("X");break;}
            ranks.Add(s.RankAtScore(score));c=new Carry{levels=(int[])s.levels.Clone(),staff=(int[])s.staffExperience.Clone(),culture=s.culture,trust=s.trust,budget=s.budget};
        }
        if(Production){Check(story.finished&&story.cleared==Pass(ranks.ToArray(),OpsCatalog.StoryGoals),"本編の終了と目標結果が違う");ComparedChallenges++;}
        return ranks.ToArray();
    }
    static readonly string[] Order={"C","B","A","S","SS"};
    static int R(string r)=>r=="X"?-1:Array.IndexOf(Order,r);
    // 目標：年ごとの最低ランク。3年とも満たせばクリア
    static bool Pass(string[] ranks,string[] goal){if(ranks.Length<3)return false;for(int y=0;y<3;y++)if(R(ranks[y])<R(goal[y]))return false;return true;}
    static readonly string[][] Goals={new[]{"C","C","C"},new[]{"B","B","B"},new[]{"B","B","A"},new[]{"B","A","A"},new[]{"A","A","A"},new[]{"B","A","S"}};
    public static void Main(string[] args)
    {
        if(args.Length>0&&args[0]=="endless"){RunEndless(args);return;}
        if(args.Length>0&&args[0]=="diagnose-endless"){DiagnoseEndless(args);return;}
        bool catalogRun=args.Length==1&&args[0]=="production";
        if(catalogRun){P2=OpsCatalog.GrowthStoryPressures[1];P3=OpsCatalog.GrowthStoryPressures[2];Decay=1;BudgetCarryDiv=1;BudgetCarryMax=999;}
        if(args.Length>=3){P2=int.Parse(args[0]);P3=int.Parse(args[1]);Decay=int.Parse(args[2]);}
        if(args.Length>=5){BudgetCarryDiv=int.Parse(args[3]);BudgetCarryMax=int.Parse(args[4]);}
        bool probe=args.Length>=6&&args[5]=="production-probe";
        Production=catalogRun||args.Length>=6&&(args[5]=="production"||probe);
        // 候補はこの試算プロセスだけ。ファイルの数値や保存は書き換えない。
        if(probe){OpsCatalog.GrowthStoryPressures[1]=P2;OpsCatalog.GrowthStoryPressures[2]=P3;Console.WriteLine("調整候補（ゲームへ未採用）");}
        if(args.Length>=6&&!Production)Milestones=int.Parse(args[5]);
        if(Production)Check(P2==OpsCatalog.GrowthStoryPressures[1]&&P3==OpsCatalog.GrowthStoryPressures[2]&&Decay==1&&BudgetCarryDiv==1&&Milestones==0,"本実装との比較は採用値のみ");
        Console.WriteLine("脅威 2年目+"+P2+" / 3年目+"+P3+" / 設備の経年="+Decay+" / 予算の繰越 ÷"+BudgetCarryDiv+" 最大"+BudgetCarryMax+" / 成長目標の入れ替え="+Milestones);
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
        if(Production)
        {
            Check(n==540&&ComparedChallenges==2160,"本実装の検証数が不足しています");
            Console.WriteLine("本実装の年度生成・目標・引き継ぎ一致："+ComparedChallenges+"挑戦 / "+ComparedYears+"年度 / 最大繰越 "+LargestCarry+"万円（試算の999上限には到達しない）");
            Console.WriteLine("実数：初回 "+first[3]+"/"+n+"、4回目まで "+byFour[3]+"/"+n);
            if(!probe)Check(100.0*first[3]/n>=35&&100.0*first[3]/n<=45&&100.0*byFour[3]/n>=60&&100.0*byFour[3]/n<=70,"Next-11の目安（初回35〜45%、4回目まで60〜70%）の外です");
        }
    }
    static double Quantile(IEnumerable<double> values,double fraction)
    {var a=values.OrderBy(x=>x).ToArray();return a[(int)Math.Ceiling((a.Length-1)*fraction)];}
    static void DiagnoseEndless(string[] args)
    {
        Production=true;int role=int.Parse(args[1]),depth=int.Parse(args[2]);string dir=args[3];Directory.CreateDirectory(dir);
        var p=new Profile{role=role,depth=depth,name=Names[role][depth]};var rows=new List<string>{"seed,year,month,phase,budget,upkeep,grant,stability,levels"};int budgetEnds=0,stabilityEnds=0;
        for(int cohort=0;cohort<100;cohort++)
        {
            int seed=14+cohort*997;var factor=Priorities(p,new OpsState(seed,true)).First(id=>string.IsNullOrEmpty(OpsCatalog.AllProjects[OpsCatalog.Index(id)].requires));var run=OpsEndless.Begin(seed,new[]{factor});
            while(!run.finished)
            {
                Check(run.year<=40,"診断で40年を超えた");PlayYear(run.state,p,s=>rows.Add(string.Join(",",new object[]{seed,run.year,s.Latest?.month??s.month,s.phase,s.budget,s.Upkeep,s.MonthlyGrant,s.stability,Cell(string.Join(";",s.levels))})));
                Check(run.RecordYear()&&run.Valid(),"診断の年度記録不能");if(run.CanAdvance)run.AdvanceYear();
            }
            if(run.state.budget<0)budgetEnds++;if(run.state.stability==0)stabilityEnds++;
        }
        File.WriteAllLines(Path.Combine(dir,"months.csv"),rows,new UTF8Encoding(true));Console.WriteLine(p.name+" 100挑戦の運営終了：予算不足 "+budgetEnds+" / 安定0 "+stabilityEnds+"（重複あり）");
    }
    static void RunEndless(string[] args)
    {
        Production=true;int factorCount=args.Length>2?int.Parse(args[2]):1;
        Check(factorCount>=0&&factorCount<=OpsCatalog.StoryFactorSlots,"因子数が不正");
        string dir=args.Length>1?args[1]:"Artifacts/Next12/Endless";Directory.CreateDirectory(dir);
        var rows=new List<string>{"role,depth,policy,cohort,seed,duration_months,completed_years,total_score,overall_rank,end_year"};
        var summaries=new List<string>{"role,depth,policy,p25_years,median_years,p90_years,max_years,median_score,ss_percent,target_met"};
        var deepScores=new List<double>();bool targets=true;double maximum=0;
        Console.WriteLine("終わりなき年度：9方針×100挑戦 / 本編の引き継ぎと同じ / 社員に任せる50点");
        Console.WriteLine("脅威：1〜3年は確定した本編の値 / 4年以降 "+OpsCatalog.EndlessPressureBase+" + "+OpsCatalog.EndlessPressureLinear+"n + "+OpsCatalog.EndlessPressureQuadratic+"n²");
        Console.WriteLine("入力仮説：本編クリアで解放済み、因子 "+factorCount+"枠を方針の優先設備から選択。人間の試遊・学習効果ではない。");
        foreach(int role in Enumerable.Range(0,3))foreach(int depth in Enumerable.Range(0,3))
        {
            var p=new Profile{role=role,depth=depth,name=Names[role][depth]};var years=new List<double>();var scores=new List<double>();int ss=0;
            for(int cohort=0;cohort<100;cohort++)
            {
                int seed=14+cohort*997;var factors=Priorities(p,new OpsState(seed,true)).Where(id=>string.IsNullOrEmpty(OpsCatalog.AllProjects[OpsCatalog.Index(id)].requires)).Take(factorCount).ToArray();
                var run=OpsEndless.Begin(seed,factors);
                while(!run.finished)
                {
                    Check(run.year<=40,"40年を超えた生存例。自動の引退や強制終了で目安に合わせないこと");
                    PlayYear(run.state,p);Check(run.state.Valid(),p.name+" / "+run.year+"年目の状態が不正");Check(run.RecordYear()&&run.Valid(),"エンドレスの年度記録不能");
                    if(run.CanAdvance)Check(run.AdvanceYear()&&run.Valid(),"エンドレスの年度引き継ぎ不能");
                }
                double duration=run.DurationMonths/(double)OpsCatalog.EndlessMonthsPerYear;years.Add(duration);scores.Add(run.TotalScore);if(depth>0)deepScores.Add(run.TotalScore);
                string rank=OpsCatalog.EndlessRank(run.TotalScore);if(rank=="SS")ss++;
                rows.Add(string.Join(",",new object[]{role,depth,Cell(p.name),cohort,seed,run.DurationMonths,run.CompletedYears,run.TotalScore,rank,run.year}));
            }
            double q25=Quantile(years,.25),median=Quantile(years,.5),q90=Quantile(years,.9),max=years.Max(),score=Quantile(scores,.5);bool met=median>=(depth==0?2:6)&&median<=(depth==0?4:9);targets&=met;maximum=Math.Max(maximum,max);
            summaries.Add(string.Join(",",new object[]{role,depth,Cell(p.name),q25.ToString("F2"),median.ToString("F2"),q90.ToString("F2"),max.ToString("F2"),score,ss,met}));
            Console.WriteLine(p.name+"：25% "+q25.ToString("F2")+"年 / 中央 "+median.ToString("F2")+"年 / 90% "+q90.ToString("F2")+"年 / 最長 "+max.ToString("F2")+"年 / 合計点中央 "+score+" / SS "+ss+"% / 中央の目安 "+(met?"内":"外"));
        }
        File.WriteAllLines(Path.Combine(dir,"runs.csv"),rows,new UTF8Encoding(true));File.WriteAllLines(Path.Combine(dir,"summary.csv"),summaries,new UTF8Encoding(true));
        Console.WriteLine("深度1・2の得点分位：10% "+Quantile(deepScores,.1)+" / 40% "+Quantile(deepScores,.4)+" / 70% "+Quantile(deepScores,.7)+" / 90% "+Quantile(deepScores,.9));
        Console.WriteLine("中央・最長の目安（15年程度まで、判定上限17年）："+(targets&&maximum<=17?"内":"外"));
        if(args.Length>3&&args[3]=="check")Check(targets&&maximum<=17,"Next-12の継続年数の目安の外。成功扱いにしないこと");
    }
}
