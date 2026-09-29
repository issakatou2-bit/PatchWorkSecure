# 判断の深さの仮ルールを、本体のルールの「写し」に差し込んで自動プレイ（9方針×100年度）で測る。本体は変更しない。
# 使い方: py -3 Tools/Sim-DecisionDepth.py <作業フォルダ> "Blind SegStop Reinfect ContainBusy ClueCut Quiet QuietStop ScopeCost RecoverCut" ["..."]
# 例（推奨案）: py -3 Tools/Sim-DecisionDepth.py $env:TEMP\depth "3 2 0 0 0 1 2 2 2"
# 設計と結果は Docs/Decision-Depth-Design-2026-09-29.md。
import os, shutil, subprocess, sys
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WORK = sys.argv[1]
os.makedirs(os.path.join(WORK, 'Artifacts', 'CompanyOps'), exist_ok=True)
for f in ['Assets/Scripts/CompanyOps/OpsCatalog.cs', 'Assets/Scripts/CompanyOps/OpsEventCatalog.cs', 'Assets/Scripts/CompanyOps/OpsState.cs',
          'Assets/Scripts/CompanyOps/OpsState.Events.cs', 'Assets/Scripts/CompanyOps/OpsState.Growth.cs',
          'Assets/Tests/CompanyOpsPersonaPolicy.cs', 'Tools/CompanyOpsDepthChecks.cs']:
    shutil.copy(os.path.join(ROOT, f), WORK)
os.chdir(WORK)
p = 'OpsState.cs'
s = open(p, encoding='utf-8-sig').read()
knobs = '''
    public static class DepthKnobs
    {
        // Blind: 状況を知らずに「限定」したときの見落とし（圧力）の係数
        // SegStop: 分離Lvごとに「広く止める」の停止時間を減らす量
        // ReinfectRecover: 広がる型の事件で、封じ込めずに「復旧」したときの再侵入（圧力）
        // ContainBusy: 繁忙期に「広く止める」と増える業務損失の上限
        public static int RecoverCut = 0, ScopeCost = 0, Quiet = 0, QuietStop = 2, Blind = 0, SegStop = 0, ReinfectRecover = 0, ContainBusy = 0, ClueCut = 0;
    }
'''
s = s.replace('namespace PatchWorkSecure.CompanyOps\n{', 'namespace PatchWorkSecure.CompanyOps\n{' + knobs, 1)
old = 'int pressure = benign ? 0 : Math.Max(0, severity - power.Total + fatigue / 15);'
assert old in s
new = old + '''
            int spread = CurrentProfile == null ? 1 : CurrentProfile.containment > 0 ? 2 : CurrentProfile.stopPower <= 7 ? 0 : 1;
            int blindness = Math.Max(0, (audited ? 0 : 2) + (Level("monitor") == 0 ? 1 : 0) + (Level("inventory") == 0 ? 1 : 0) - DepthKnobs.ClueCut);
            if (!benign && response == "recover" && spread == 2) pressure += DepthKnobs.ReinfectRecover * Math.Max(0, 2 - Level("segment"));'''
s = s.replace(old, new, 1)
old2 = 'int stop = response == "contain" ? 9 : response == "recover" ? 3 : 1;'
assert old2 in s
s = s.replace(old2, 'bool quiet = DepthKnobs.Quiet > 0 && CurrentEvent != null && ((month * 7 + CurrentEvent.id.Length) % 3 == 0);\n            int stop = response == "contain" ? (quiet ? DepthKnobs.QuietStop : Math.Max(3, 9 - DepthKnobs.SegStop * Level("segment"))) : response == "recover" ? 3 : 1;', 1)
old3 = 'int businessLoss = situationPrepared ? 0 : Math.Min(Situation.stopLossCap, downtime);'
assert old3 in s
s = s.replace(old3, old3 + '\n            if (!situationPrepared && response == "contain" && Situation.stopLossCap > 0) businessLoss += DepthKnobs.ContainBusy;', 1)
old4 = 'int loss = Math.Max(0, (pressure + 1) / 2 - recovery);'
assert old4 in s
s = s.replace(old4, old4 + '\n            if (!benign && response == "scope") loss += Math.Max(0, DepthKnobs.Blind * blindness * spread - containment / 3);', 1)
old5 = 'int cost = response == "contain" ? 6 : response == "scope" ? 3 : 4;'
assert old5 in s
s = s.replace(old5, old5 + '\n            if (response == "scope") cost += DepthKnobs.ScopeCost * blindness;\n            if (response == "recover") cost -= DepthKnobs.RecoverCut;', 1)
open(p, 'w', encoding='utf-8').write(s)

d = 'CompanyOpsDepthChecks.cs'
t = open(d, encoding='utf-8-sig').read()
t = t.replace('public static void Main()', 'public static void Main(string[] args)', 1)
t = t.replace('Directory.CreateDirectory("Artifacts/CompanyOps/Depths");', '''Directory.CreateDirectory("Artifacts/CompanyOps/Depths");
        if(args.Length>=8)DepthKnobs.ScopeCost=int.Parse(args[7]);
        if(args.Length>=9)DepthKnobs.RecoverCut=int.Parse(args[8]);
        if(args.Length>=7){DepthKnobs.Quiet=int.Parse(args[5]);DepthKnobs.QuietStop=int.Parse(args[6]);}
        if(args.Length>=5){DepthKnobs.Blind=int.Parse(args[0]);DepthKnobs.SegStop=int.Parse(args[1]);DepthKnobs.ReinfectRecover=int.Parse(args[2]);DepthKnobs.ContainBusy=int.Parse(args[3]);DepthKnobs.ClueCut=int.Parse(args[4]);}
        var all=new int[3];int allNd=0,allMonths=0;var clears=new List<int>();var losses=new List<int>();var scopes=new List<int>();''', 1)
# 各方針の集計を全体へ足す
t = t.replace('Console.WriteLine(p.name+" / 完走"', 'all[0]+=responses[0];all[1]+=responses[1];all[2]+=responses[2];allNd+=nondominatedTotal;allMonths+=months;clears.Add(clear);losses.Add(loss/100);scopes.Add(100*responses[1]/months);\n            if(false)Console.WriteLine(p.name+" / 完走"', 1)
t = t.replace('Console.WriteLine("9方針×100年度', '''int tot=all.Sum();Console.WriteLine("knobs "+string.Join(",",args)+" | contain/scope/recover % = "+(100*all[0]/tot)+"/"+(100*all[1]/tot)+"/"+(100*all[2]/tot)+
            " | nondom "+(allNd/(double)allMonths).ToString("F2")+" | clear "+string.Join(" ",clears)+" | loss "+string.Join(" ",losses)+" | scope% "+string.Join(" ",scopes));
        if(false)Console.WriteLine("9方針×100年度''', 1)
open(d, 'w', encoding='utf-8').write(t)
MONO = r'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
cs = [f for f in os.listdir('.') if f.endswith('.cs')]
subprocess.run([MONO + '/bin/mono.exe', MONO + '/lib/mono/4.5/csc.exe', '-nologo', '-nowarn:162', '-out:Depth.exe'] + cs, check=True)
for knobs in sys.argv[2:]:
    subprocess.run([MONO + '/bin/mono.exe', 'Depth.exe'] + knobs.split(), check=True)
