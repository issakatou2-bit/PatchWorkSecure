using System;
using System.IO;
using System.Linq;
using System.Text;
using PatchWorkSecure.CompanyOps;
using PatchWorkSecure.Tests;

public static class CompanyOpsPersonaChecks
{
    static string Cell(object value) => "\""+(value??"").ToString().Replace("\"","\"\"")+"\"";
    static void Check(bool ok,string message) { if (!ok) throw new Exception(message); }
    public static void Main()
    {
        Directory.CreateDirectory("Artifacts/CompanyOps/Persona");
        var years=new StringBuilder("役,試行群,周回,種,クリア,到達月,得点,被害万円,停止h,残予算,未使用工数,依頼達成,日常対応,社員委任,知識画面数,残疲労,担当者Lv,停止選択,限定選択,復旧選択,買物回数,維持費,導入なし月\n");
        var turns=new StringBuilder("役,試行群,周回,種,月,出来事ID,計画行動,対応,予測損失下限,予測損失上限,予測停止下限,予測停止上限,被害万円,停止h,予算,疲労,知識画面,未使用工数,依頼達成,社員委任\n");
        for(int role=0;role<3;role++)
        {
            int totalClear=0;
            for(int cohort=0;cohort<30;cohort++)
            {
                var memory=new PersonaMemory{role=role};
                for(int cycle=0;cycle<5;cycle++)
                {
                    memory.cycle=cycle; int seed=CompanyOpsPersonaPolicy.Seeds[cycle]+cohort*997;
                    var s=new OpsState(seed,true); int spare=0,tickets=0,delegations=0,reads=0,buys=0,idle=0; var counts=new int[3];
                    while(s.phase!=OpsPhase.Ended)
                    {
                        var turn=new PersonaTurn(); PersonaCommand command;
                        while((command=CompanyOpsPersonaPolicy.Next(s,memory,turn))!=null)
                        { Check(CompanyOpsPersonaPolicy.ApplyRule(s,command),"実行不能: "+command); CompanyOpsPersonaPolicy.Applied(turn,command); Check(s.Valid(),"計画で不正状態"); }
                        Check(turn.steps<=24,"計画が終了しない");
                        int unused=s.capacity; spare+=unused; buys+=turn.purchases; if(turn.purchases==0) idle++;
                        s.BeginIncident(); string response=CompanyOpsPersonaPolicy.Response(s,memory); var f=CompanyOpsPersonaPolicy.PublicForecast(s,response);
                        counts[Array.IndexOf(CompanyOpsPersonaPolicy.Responses,response)]++;
                        Check(s.Resolve(response),"対応不能"); Check(s.Valid(),"月報で不正状態");
                        bool read=CompanyOpsPersonaPolicy.ReadLesson(s,memory); if(read) { memory.readTerms.Add(s.Current.lesson); reads++; }
                        if(s.Latest.ticketMode!="defer") tickets++; if(s.Latest.ticketMode=="delegate") delegations++;
                        turns.AppendLine(string.Join(",",new object[]{memory.Label,cohort,cycle+1,seed,s.month+1,s.CurrentEvent.id,string.Join(" > ",turn.actions),response,
                            f[0],f[1],f[2],f[3],s.Latest.loss,s.Latest.downtime,s.budget,s.fatigue,read,unused,s.CurrentMissionCompleted,s.Latest.ticketMode=="delegate"}.Select(Cell)));
                        if(s.QuarterRewardPending) s.ClaimQuarterReward(CompanyOpsPersonaPolicy.Reward(s,memory));
                        Check(s.NextMonth(),"次月へ進めない"); Check(s.Valid(),"進行で不正状態");
                    }
                    years.AppendLine(string.Join(",",new object[]{memory.Label,cohort,cycle+1,seed,s.IsClear,s.history.Count,s.AnnualScore,s.totalLoss,s.totalDowntime,
                        s.budget,spare,s.MissionCount,tickets,delegations,reads,s.fatigue,s.PlayerLevel,counts[0],counts[1],counts[2],buys,s.Upkeep,idle}.Select(Cell)));
                    if(s.IsClear) totalClear++;
                    if(cohort==0) Console.WriteLine(memory.Label+" / "+(cycle+1)+"周目 / 種"+seed+" / "+(s.IsClear?"クリア":"終了")+" / "+s.history.Count+
                        "月 / "+s.AnnualScore+"点 / 被害"+s.totalLoss+"・停止"+s.totalDowntime+" / 依頼"+s.MissionCount+" / 日常"+tickets+"（委任"+delegations+"） / 知識画面"+reads+" / 未使用工数"+spare);
                }
            }
            Console.WriteLine(new PersonaMemory{role=role}.Label+" / 30群×5周 / 完走 "+totalClear+"/150");
        }
        File.WriteAllText("Artifacts/CompanyOps/Persona/rule-years.csv",years.ToString(),new UTF8Encoding(true));
        File.WriteAllText("Artifacts/CompanyOps/Persona/rule-turns.csv",turns.ToString(),new UTF8Encoding(true));
        Console.WriteLine("仮想方針450年度 / 条件・全行動・結果を記録。学習と楽しさの測定ではない。");
    }
}
