# Sim-DecisionDepth.py の出力（years.csv・turns.csv）から、年間得点・月別の無事率・山場の目標の達成率を集計する。
# 使い方: 先頭の D を作業フォルダの Artifacts/CompanyOps/Depths に合わせて py -3 Tools/Analyze-DepthRuns.py
import csv, statistics as st, collections
D = r'C:\Users\issak\AppData\Local\Temp\claude\C--Projects-PatchWorkSecure\815089ba-76f2-45e4-9170-3aad182f08df\scratchpad\depth2\Artifacts\CompanyOps\Depths'
years = list(csv.DictReader(open(D + r'\years.csv', encoding='utf-8-sig')))
turns = list(csv.DictReader(open(D + r'\turns.csv', encoding='utf-8-sig')))
pol = []
for r in years:
    if r['方針'] not in pol: pol.append(r['方針'])
print('== 年間得点（方針別：平均 / 10% / 50% / 90%）')
allsc = []
for p in pol:
    sc = sorted(int(r['得点']) for r in years if r['方針'] == p)
    allsc += sc
    q = lambda f: sc[int(f * (len(sc) - 1))]
    print(f'{p[:14]:14} 平均{st.mean(sc):6.0f}  {q(.1):5} {q(.5):5} {q(.9):5}  A(>=1350) {sum(s>=1350 for s in sc)}%')
allsc.sort()
print('全体の分位', [allsc[int(f*(len(allsc)-1))] for f in (.1,.25,.5,.75,.9,.97)])
print('== 月別（全方針）：被害の平均 / 停止の平均 / 被害0かつ停止<=2の割合')
by = collections.defaultdict(list)
for r in turns: by[int(r['月'])].append((int(r['被害']), int(r['停止'])))
names = {1:'4月',2:'5月',3:'6月',4:'7月',5:'8月',6:'9月',7:'10月',8:'11月',9:'12月',10:'1月',11:'2月',12:'3月'}
for m in sorted(by):
    v = by[m]
    print(f'{names[m]:4} 被害{st.mean(a for a,b in v):5.1f} 停止{st.mean(b for a,b in v):5.1f}  無事{100*sum(a==0 and b<=2 for a,b in v)/len(v):4.0f}%  件数{len(v)}')
print('== 山場（6・9・12・3月）で「被害<=X かつ 停止<=Y」を満たす割合（方針別）')
for X, Y in [(3, 4), (5, 6), (8, 8)]:
    row = []
    for p in pol:
        v = [(int(r['被害']), int(r['停止'])) for r in turns if r['方針'] == p and int(r['月']) in (3, 6, 9, 12)]
        row.append(100 * sum(a <= X and b <= Y for a, b in v) // max(1, len(v)))
    print(f'X={X} Y={Y}:', row)
print('== peak month x goal: pass% for depth0 policies / depth1-2 policies')
d0 = [pol[0], pol[3], pol[6]]
for m in (3, 6, 9, 12):
    for X, Y in [(8, 8), (5, 6), (3, 4), (1, 3)]:
        a = [(int(r['被害']), int(r['停止'])) for r in turns if int(r['月']) == m and r['方針'] in d0]
        b = [(int(r['被害']), int(r['停止'])) for r in turns if int(r['月']) == m and r['方針'] not in d0]
        f = lambda v: 100 * sum(x <= X and y <= Y for x, y in v) // max(1, len(v))
        print(f'm{m} X{X} Y{Y}: casual {f(a)}  engaged {f(b)}')
