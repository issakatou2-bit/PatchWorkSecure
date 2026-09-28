# まばたき・口パク用の差分を作る。2×2のシート（左上＝基本、他＝目や口だけ違う絵）から、
# 基本の絵に「変わった顔の部分だけ」を位置合わせして重ね、体や髪が揺れない差分にする。
# 使い方: py -3 Tools/Make-FaceFrames.py <シートPNG> <出力フォルダ> <接頭辞> <名前1,名前2,名前3,名前4>
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
sys.path.insert(0, str(Path(__file__).parent))
from importlib import import_module
cut = import_module("Cut-CharacterSheet")

def cells(sheet):
    img = cut.remove_background(Image.open(sheet))
    w, h = img.size
    out = []
    for r in range(2):
        for c in range(2):
            cell = img.crop((c * w // 2, r * h // 2, (c + 1) * w // 2, (r + 1) * h // 2))
            out.append(cell.crop(cell.getchannel("A").getbbox()))
    return out

def align(base, other, search=24):
    b = np.asarray(base.convert("L"), dtype=np.float32)
    o = np.asarray(other.convert("L"), dtype=np.float32)
    H, W = min(b.shape[0], o.shape[0]) - 2 * search, min(b.shape[1], o.shape[1]) - 2 * search
    ref = b[search:search + H // 2, search:search + W]  # 頭側の半分で合わせる
    best = (1e18, 0, 0)
    for dy in range(-search, search + 1, 2):
        for dx in range(-search, search + 1, 2):
            win = o[search + dy:search + dy + H // 2, search + dx:search + dx + W]
            if win.shape != ref.shape:
                continue
            d = float(np.abs(win - ref).mean())
            if d < best[0]:
                best = (d, dx, dy)
    return best[1], best[2]

def main():
    sheet, out_dir, prefix, names = sys.argv[1], Path(sys.argv[2]), sys.argv[3], sys.argv[4].split(",")
    out_dir.mkdir(parents=True, exist_ok=True)
    cs = cells(sheet)
    base = cs[0].convert("RGBA")
    base.save(out_dir / f"{prefix}_{names[0]}.png")
    bw, bh = base.size
    # 瞳の赤で目の位置を見つけ、目から口までを「顔の範囲」にする（髪や体は基本の絵のまま）
    px = np.asarray(base, dtype=np.int16)
    # 瞳の濃い赤だけ（髪の影のピンクを拾わない）
    red = (px[..., 0] > 150) & (px[..., 1] < 70) & (px[..., 2] < 80) & (px[..., 0] - px[..., 1] > 100) & (px[..., 3] > 200)
    # 3頭身の目は全身の高さの15〜38%あたり。リボンタイ・名札の赤を拾わないよう範囲を絞る
    red[:int(bh * 0.15), :] = False
    red[int(bh * 0.38):, :] = False
    red[:, :int(bw * 0.2)] = False
    red[:, int(bw * 0.8):] = False
    ys, xs = np.nonzero(red)
    ex0, ex1 = np.percentile(xs, 2), np.percentile(xs, 98)
    ey0, ey1 = np.percentile(ys, 2), np.percentile(ys, 98)
    eh, ew = ey1 - ey0, ex1 - ex0
    # 開いた口の中も濃い赤なので、赤の範囲＝両目＋口。少しだけ広げる
    box = (int(ex0 - ew * 0.1), int(ey0 - eh * 0.25), int(ex1 + ew * 0.1), int(ey1 + eh * 0.25))
    print("赤の範囲", (int(ex0), int(ey0), int(ex1), int(ey1)), "全身", (bw, bh))
    region = Image.new("L", base.size, 0)
    region.paste(255, (box[0] + 6, box[1] + 6, box[2] - 6, box[3] - 6))
    region = region.filter(ImageFilter.GaussianBlur(5))
    for cell, name in zip(cs[1:], names[1:]):
        # 顔の範囲の周りだけで位置を合わせる
        pad = 14
        ref = np.asarray(base.convert("L"), dtype=np.float32)[box[1]:box[3], box[0]:box[2]]
        oth = np.asarray(cell.convert("L"), dtype=np.float32)
        best = (1e18, 0, 0)
        for dy in range(-pad, pad + 1):
            for dx in range(-pad, pad + 1):
                y0, x0 = box[1] + dy, box[0] + dx
                win = oth[y0:y0 + ref.shape[0], x0:x0 + ref.shape[1]]
                if y0 < 0 or x0 < 0 or win.shape != ref.shape:
                    continue
                # 目と口は変わるので、範囲の外周（髪・輪郭）の一致で比べる
                d = float(np.abs(win - ref)[[0, 1, 2, -3, -2, -1], :].mean() + np.abs(win - ref)[:, [0, 1, 2, -3, -2, -1]].mean())
                if d < best[0]:
                    best = (d, dx, dy)
        dx, dy = best[1], best[2]
        moved = Image.new("RGBA", base.size, (0, 0, 0, 0))
        moved.paste(cell, (-dx, -dy), cell)
        frame = base.copy()
        frame.paste(moved, (0, 0), Image.composite(region, Image.new("L", base.size, 0), moved.getchannel("A")))
        frame.save(out_dir / f"{prefix}_{name}.png")
        print(name, "ずれ", (dx, dy), "顔の範囲", box)

if __name__ == "__main__":
    main()
