# 白背景のキャラクター差分シート（3列×2行など）を1体ずつに切り分け、背景を透過する。
# 使い方: py -3 Tools/Cut-CharacterSheet.py <入力PNG> <出力フォルダ> <名前1,名前2,...> [列数] [行数]
# 太い輪郭線で閉じた絵を前提に、画像の外周からつながる白い部分だけを透明にする（服の白は残る）。
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

MARK = (255, 0, 255)


def remove_background(img, thresh=38):
    rgb = img.convert("RGB")
    w, h = rgb.size
    step = 8
    seeds = [(x, 0) for x in range(0, w, step)] + [(x, h - 1) for x in range(0, w, step)]
    seeds += [(0, y) for y in range(0, h, step)] + [(w - 1, y) for y in range(0, h, step)]
    for s in seeds:
        r, g, b = rgb.getpixel(s)
        if r > 225 and g > 225 and b > 225:
            ImageDraw.floodfill(rgb, s, MARK, thresh=thresh)
    mask = Image.new("L", (w, h), 255)
    px, mp = rgb.load(), mask.load()
    for y in range(h):
        for x in range(w):
            if px[x, y] == MARK:
                mp[x, y] = 0
    # 境界の白いにじみを1px削り、わずかにぼかして縁をなめらかにする
    mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.6))
    out = img.convert("RGBA")
    out.putalpha(mask)
    return out


def split_points(profile, parts):
    n = len(profile)
    cuts = [0]
    for k in range(1, parts):
        center = n * k // parts
        lo, hi = center - n // (parts * 3), center + n // (parts * 3)
        cuts.append(min(range(lo, hi), key=lambda i: profile[i]))
    cuts.append(n)
    return cuts


def main():
    src, out_dir, names = sys.argv[1], Path(sys.argv[2]), sys.argv[3].split(",")
    cols = int(sys.argv[4]) if len(sys.argv) > 4 else 3
    rows = int(sys.argv[5]) if len(sys.argv) > 5 else 2
    out_dir.mkdir(parents=True, exist_ok=True)
    img = remove_background(Image.open(src))
    alpha = img.getchannel("A")
    w, h = img.size
    ap = alpha.load()
    row_profile = [sum(1 for x in range(0, w, 2) if ap[x, y] > 0) for y in range(h)]
    ys = split_points(row_profile, rows)
    i = 0
    for r in range(rows):
        band = img.crop((0, ys[r], w, ys[r + 1]))
        bp = band.getchannel("A").load()
        bh = ys[r + 1] - ys[r]
        col_profile = [sum(1 for y in range(0, bh, 2) if bp[x, y] > 0) for x in range(w)]
        xs = split_points(col_profile, cols)
        for c in range(cols):
            cell = band.crop((xs[c], 0, xs[c + 1], bh))
            box = cell.getchannel("A").getbbox()
            if box and i < len(names):
                pad = 8
                box = (max(0, box[0] - pad), max(0, box[1] - pad), min(cell.width, box[2] + pad), min(cell.height, box[3] + pad))
                cell.crop(box).save(out_dir / f"{names[i]}.png")
                print(names[i], cell.crop(box).size)
            i += 1


if __name__ == "__main__":
    main()
