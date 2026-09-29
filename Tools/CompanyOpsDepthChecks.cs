using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PatchWorkSecure.CompanyOps;
using PatchWorkSecure.Tests;

// 人間・資格保有者の再現ではなく、公開情報だけで選ぶ9種類の方針仮説。
public static class CompanyOpsDepthChecks
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
    // 同じ公開情報方針と種で、①前（山場ルールなし）をその場で再計測する。
    static int BaselineClear(Profile p)
    {
        int clear=0;
        for(int cohort=0;cohort<100;cohort++)
        {
            var state=new OpsState(14+cohort*997,true){peakGoalRules=0};var seen=new HashSet<string>();
            while(state.phase!=OpsPhase.Ended)
            {
                var turn=new PersonaTurn();PersonaCommand command;
                while((command=Next(state,p,turn))!=null){Check(CompanyOpsPersonaPolicy.ApplyRule(state,command),"比較年度の計画不能");CompanyOpsPersonaPolicy.Applied(turn,command);Check(state.Valid(),"比較年度が不正");}
                Check(turn.steps<24,"比較年度の計画が停止上限到達");state.BeginIncident();state.Resolve(Response(state,p,seen));
                if(p.role!=0||p.depth>0||state.Latest.loss>6)seen.Add(state.Current.lesson);
                if(state.QuarterRewardPending)state.ClaimQuarterReward(p.role==0&&state.budget>30?"capacity":"budget");
                state.NextMonth();Check(state.Valid(),"比較年度の進行が不正");
            }
            if(state.IsClear)clear++;
        }
        return clear;
    }
    public static void Main()
    {
        Directory.CreateDirectory("Artifacts/CompanyOps/Depths");
        var years=new StringBuilder("方針,種,完走,月数,得点,被害万円,停止h,残予算,未使用工数,依頼,社員加算月,日常委任,工数支援月,Lv2設備,維持費,停止選択,限定選択,復旧選択,年間ランク,山場の盾\n");
        var turns=new StringBuilder("方針,種,月,出来事,計画,対応,被害,停止,社員加算,未使用工数,成長前社員Lv,成長後社員Lv,優越されない選択数,山場の目標,達成\n");
        string[] ranks={"SS","S","A","B","C"};var allRanks=new int[ranks.Length];
        var goalAttempts=new int[2,4];var goalWins=new int[2,4];
        double[,] guide={{41,45,46,29},{64,70,73,56}};
        int allMonths=0,allScopes=0,allCandidates=0;var differences=new List<int>();
        foreach(int role in Enumerable.Range(0,3))foreach(int depth in Enumerable.Range(0,3))
        {
            var p=new Profile{role=role,depth=depth,name=Names[role][depth]};int clear=0,months=0,score=0,loss=0,stop=0,unusedTotal=0,help=0;
            var responses=new int[3];var eventIds=new HashSet<string>();int nondominatedTotal=0;
            int before=BaselineClear(p);var peakAttempts=new int[4];var peakWins=new int[4];var rankCounts=new int[ranks.Length];
            for(int cohort=0;cohort<100;cohort++)
            {
                // 全9方針で同じ100種。再挑戦記憶を混ぜず、各年度は独立。
                int seed=14+cohort*997;var s=new OpsState(seed,true);var seen=new HashSet<string>();
                int unusedYear=0,staffMonths=0,delegations=0,routine=0;var counts=new int[3];
                while(s.phase!=OpsPhase.Ended)
                {
                    var turn=new PersonaTurn();PersonaCommand command;
                    while((command=Next(s,p,turn))!=null)
                    {
                        Check(CompanyOpsPersonaPolicy.ApplyRule(s,command),p.name+"実行不能: "+command);
                        CompanyOpsPersonaPolicy.Applied(turn,command);Check(s.Valid(),p.name+"計画で不正状態");
                    }
                    Check(turn.steps<24,p.name+"計画停止上限到達");int unused=s.capacity;unusedYear+=unused;
                    if(s.supportOrder=="routine")routine++;
                    Check(s.BeginIncident(),"事件開始不能");string response=Response(s,p,seen);
                    // 損失・停止・対応費の3軸で他の選択に厳密に優越されない候補数。楽しさとは別の指標。
                    var estimates=CompanyOpsPersonaPolicy.Responses.Select(s.Estimate).ToArray();
                    int candidates=estimates.Count(a=>!estimates.Any(b=>b.lossMax<=a.lossMax&&b.stopMax<=a.stopMax&&b.cost<=a.cost&&
                        (b.lossMax<a.lossMax||b.stopMax<a.stopMax||b.cost<a.cost)));
                    nondominatedTotal+=candidates;eventIds.Add(s.CurrentEvent.id);
                    int chosen=Array.IndexOf(CompanyOpsPersonaPolicy.Responses,response);counts[chosen]++;responses[chosen]++;
                    Check(s.Resolve(response),"対応不能");Check(s.Valid(),p.name+"結果で不正状態");
                    if(s.Latest.peakGoalRecorded){int peak=s.PeakIndex(s.month);peakAttempts[peak]++;if(s.Latest.peakGoalMet)peakWins[peak]++;}
                    if(s.Latest.power.staff>0)staffMonths++;if(s.Latest.ticketMode=="delegate")delegations++;
                    if(p.role!=0||depth>0||s.Latest.loss>6)seen.Add(s.Current.lesson);
                    turns.AppendLine(string.Join(",",new object[]{p.name,seed,s.month+1,s.CurrentEvent.id,string.Join(" > ",turn.actions),response,
                        s.Latest.loss,s.Latest.downtime,s.Latest.power.staff,unused,string.Join("/",s.Latest.growth.staffBefore),
                        string.Join("/",s.Latest.growth.staffAfter),candidates,s.Latest.peakGoalRecorded,s.Latest.peakGoalMet}.Select(Cell)));
                    if(s.QuarterRewardPending)Check(s.ClaimQuarterReward(role==0&&s.budget>30?"capacity":"budget"),"報酬不能");
                    Check(s.NextMonth(),"次月不能");Check(s.Valid(),p.name+"進行で不正状態");
                }
                Check(s.history.Count>0&&s.history.Count<=12,"年度の月数が不正");
                if(s.IsClear)clear++;months+=s.history.Count;score+=s.AnnualScore;loss+=s.totalLoss;stop+=s.totalDowntime;unusedTotal+=unusedYear;help+=staffMonths;
                years.AppendLine(string.Join(",",new object[]{p.name,seed,s.IsClear,s.history.Count,s.AnnualScore,s.totalLoss,s.totalDowntime,s.budget,
                    unusedYear,s.MissionCount,staffMonths,delegations,routine,s.levels.Count(n=>n==2),s.Upkeep,counts[0],counts[1],counts[2],s.RankCode,s.PeakMedals}.Select(Cell)));
                rankCounts[Array.IndexOf(ranks,s.RankCode)]++;
            }
            allMonths+=months;allScopes+=responses[1];allCandidates+=nondominatedTotal;differences.Add(clear-before);
            int group=depth==0?0:1;
            for(int i=0;i<4;i++){goalAttempts[group,i]+=peakAttempts[i];goalWins[group,i]+=peakWins[i];}
            for(int i=0;i<ranks.Length;i++)allRanks[i]+=rankCounts[i];
            Console.WriteLine(p.name+" / 完走"+clear+"/100 / 平均点"+(score/100.0).ToString("F1")+" / 被害"+(loss/100.0).ToString("F1")+
                " / 停止"+(stop/100.0).ToString("F1")+" / 未使用工数"+(unusedTotal/100.0).ToString("F1")+" / 社員加算"+help+"/"+months+
                "月 / 停止・限定・復旧="+string.Join("/",responses)+" / 出来事"+eventIds.Count+"種 / 非優越候補平均"+(nondominatedTotal/(double)months).ToString("F2"));
            Console.WriteLine("  ①前の完走="+before+"/100 / 差="+(clear-before)+"pt / ランク="+string.Join(" / ",ranks.Select((r,i)=>r+":"+rankCounts[i]+"%")));
            Console.WriteLine("  山場（達成/到達、達成率、年度開始100回に対する率）："+string.Join(" / ",Enumerable.Range(0,4).Select(i=>OpsCatalog.Months[OpsCatalog.PeakMonths[i]].name+" "+peakWins[i]+"/"+peakAttempts[i]+" ("+(100.0*peakWins[i]/Math.Max(1,peakAttempts[i])).ToString("F1")+"%、開始比"+peakWins[i]+"%)")));
        }
        File.WriteAllText("Artifacts/CompanyOps/Depths/years.csv",years.ToString(),new UTF8Encoding(true));
        File.WriteAllText("Artifacts/CompanyOps/Depths/turns.csv",turns.ToString(),new UTF8Encoding(true));
        double scopePercent=100.0*allScopes/allMonths,candidateMean=allCandidates/(double)allMonths;
        Console.WriteLine("全体：限定="+scopePercent.ToString("F2")+"% / 非優越候補="+candidateMean.ToString("F3")+" / ①前からの完走率差(pt)="+string.Join("/",differences));
        Console.WriteLine("年間ランク分布（全900年度・運営終了も含む）："+string.Join(" / ",ranks.Select((r,i)=>r+":"+allRanks[i]+"/900="+(allRanks[i]/9.0).ToString("F2")+"%")));
        Console.WriteLine("ランクの目安との差(pt)：SS "+(allRanks[0]/9.0-10).ToString("F2")+" / S "+(allRanks[1]/9.0-30).ToString("F2")+" / A "+(allRanks[2]/9.0-30).ToString("F2"));
        for(int group=0;group<2;group++)for(int i=0;i<4;i++)
        {
            double rate=100.0*goalWins[group,i]/Math.Max(1,goalAttempts[group,i]),delta=rate-guide[group,i];
            Console.WriteLine((group==0?"気軽（深度0）":"考える（深度1・2）")+" / "+OpsCatalog.Months[OpsCatalog.PeakMonths[i]].name+" 山場="+rate.ToString("F2")+"% / 目安="+guide[group,i]+"% / 差="+delta.ToString("F2")+"pt / "+(Math.Abs(delta)<=10?"目安±10以内":"目安外"));
        }
        Check(scopePercent>=40&&scopePercent<=55,"限定40〜55%の基準を満たさない");
        Check(candidateMean>=1.9,"非優越候補の平均1.9を満たさない");
        Check(differences.All(d=>Math.Abs(d)<=5),"いずれかの方針の完走率差が±5ポイントを超えた");
        Console.WriteLine("9方針×100年度=900年度。経験深度は方針差の仮説であり、人間の技能・学習・面白さの測定ではない。");
    }
}
