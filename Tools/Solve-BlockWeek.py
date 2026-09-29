# 試作F2（作業のはめ込み）の盤面が解けるか、解がいくつあるかを全探索で確かめる。
# 盤面：縦4（午前1・午前2・午後1・午後2）×横5（月〜金）。作業は回転できる。期限・避ける日の条件も守る。
# 使い方: py -3 Tools/Solve-BlockWeek.py
ROWS, COLS = 4, 5
LOCKS = {(0, 0): '朝会', (2, 2): '給与の計算', (3, 2): '給与の計算', (0, 3): '来客対応', (3, 4): '定例会議'}
TASKS = [  # 名前, 形（行,列）, 期限（この列まで）, 避ける列
    ('VPN機器の緊急修正', [(0, 0), (1, 0)], 1, ()),
    ('基幹サーバーの更新', [(0, 0), (0, 1), (1, 0), (1, 1)], 3, ()),
    ('メール移行（準備→本番）', [(0, 0), (1, 0), (1, 1)], 4, ()),
    ('社員PCの更新（毎日少しずつ）', [(0, 0), (0, 1), (0, 2)], 4, ()),
    ('研修の準備', [(0, 0), (0, 1)], 4, (2,)),
    ('ファイルサーバーの再起動', [(0, 0)], 4, ()),
]

def norm(c):
    mr, mc = min(a for a, b in c), min(b for a, b in c)
    return tuple(sorted((a - mr, b - mc) for a, b in c))

def rotations(c):
    out, cur = set(), c
    for _ in range(4):
        cur = [(b, -a) for a, b in cur]
        out.add(norm(cur))
    return sorted(out)

def placements(task):
    name, shape, dl, avoid = task
    res = []
    for sh in rotations(shape):
        for r in range(ROWS):
            for c in range(COLS):
                cells = [(r + a, c + b) for a, b in sh]
                if any(not (0 <= x < ROWS and 0 <= y < COLS) or (x, y) in LOCKS for x, y in cells):
                    continue
                if max(y for x, y in cells) > dl or any(y in avoid for x, y in cells):
                    continue
                res.append(frozenset(cells))
    return res

def main():
    free = ROWS * COLS - len(LOCKS)
    need = sum(len(t[1]) for t in TASKS)
    print('空きマス', free, '作業のマス', need)
    opts = [placements(t) for t in TASKS]
    order = sorted(range(len(TASKS)), key=lambda i: len(opts[i]))
    sols = []
    def go(k, used, pick):
        if k == len(order):
            sols.append(dict(pick)); return
        i = order[k]
        for p in opts[i]:
            if used & p:
                continue
            pick[i] = p; go(k + 1, used | p, pick); del pick[i]
    go(0, frozenset(), {})
    print('解の数', len(sols))
    if sols:
        grid = [['××' if (r, c) in LOCKS else '　' for c in range(COLS)] for r in range(ROWS)]
        for i, p in sols[0].items():
            for r, c in p:
                grid[r][c] = TASKS[i][0][:2]
        print('月  火  水  木  金'); [print(' '.join(row)) for row in grid]

if __name__ == '__main__':
    main()
