# 凝った表現用のUI素材を作る（Docs/Visual-Craft-2026-09-29.md）。白や薄い色で描き、Unity側で色を付けて使う。
# 使い方: py -3 Tools/Make-CraftTextures.py  → Assets/Art/UI/Craft/ に書き出す
import os, random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Assets', 'Art', 'UI', 'Craft')
os.makedirs(OUT, exist_ok=True)
S = 4  # 4倍で描いて縮め、縁をなめらかにする

def save(im, name, size):
    im.resize(size, Image.LANCZOS).save(os.path.join(OUT, name))
    print(name, size)

# 1. 縫い目の枠（9スライス。枠の幅32px）。白い破線の角丸。ツギハギのモチーフ
w, h, r, inset = 128, 128, 22, 9
im = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
x0, y0, x1, y1 = inset * S, inset * S, (w - inset) * S, (h - inset) * S
dash, gap, lw = 9 * S, 6 * S, 3 * S
def dashed(a, b):
    (ax, ay), (bx, by) = a, b
    import math
    L = math.hypot(bx - ax, by - ay); t = 0
    while t < L:
        e = min(t + dash, L)
        d.line([(ax + (bx - ax) * t / L, ay + (by - ay) * t / L), (ax + (bx - ax) * e / L, ay + (by - ay) * e / L)], fill=(255, 255, 255, 235), width=lw)
        t = e + gap
rr = r * S
dashed((x0 + rr, y0), (x1 - rr, y0)); dashed((x0 + rr, y1), (x1 - rr, y1))
dashed((x0, y0 + rr), (x0, y1 - rr)); dashed((x1, y0 + rr), (x1, y1 - rr))
for (cx, cy, st) in [(x0 + rr, y0 + rr, 180), (x1 - rr, y0 + rr, 270), (x1 - rr, y1 - rr, 0), (x0 + rr, y1 - rr, 90)]:
    for a in range(st, st + 90, 30):
        d.arc([cx - rr, cy - rr, cx + rr, cy + rr], a + 4, a + 22, fill=(255, 255, 255, 235), width=lw)
save(im, 'stitch-frame.png', (w, h))

# 2. 上からの光沢（パネル・ボタンの上に重ねる。上端が白45%→下で0）
w, h = 256, 128
im = Image.new('RGBA', (w, h), (255, 255, 255, 0)); px = im.load()
for y in range(h):
    a = int(115 * (1 - y / h) ** 1.6)
    for x in range(w):
        px[x, y] = (255, 255, 255, a)
save(im, 'gloss-top.png', (w, h))

# 3. 箔押しの光の帯（左右が透明、中央が白。斜めにしてマスクの中を横切らせる）
w, h = 256, 64
im = Image.new('RGBA', (w * S, h * S), (255, 255, 255, 0)); d = ImageDraw.Draw(im)
for i in range(w * S):
    t = abs(i - w * S / 2) / (w * S / 2)
    a = int(230 * max(0, 1 - t / .35) ** 2)
    d.line([(i + h * S * .5, 0), (i - h * S * .5, h * S)], fill=(255, 255, 255, a), width=2)
save(im.filter(ImageFilter.GaussianBlur(3 * S)), 'foil-sheen.png', (w, h))

# 4. 斜めの帯（右端を斜めに切った白い帯。色はUnityで付ける）。左側は9スライス用に角丸
w, h = 512, 64
im = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
d.rounded_rectangle([0, 0, w * S - 40 * S, h * S], radius=10 * S, fill=(255, 255, 255, 255))
d.polygon([(w * S - 60 * S, 0), (w * S, 0), (w * S - 40 * S, h * S), (w * S - 60 * S, h * S)], fill=(255, 255, 255, 255))
save(im, 'ribbon-slant.png', (w, h))

# 5. 紙の地紋（繰り返し。ごく薄い繊維と粒）
w = h = 256
random.seed(7)
im = Image.new('RGBA', (w, h), (255, 255, 255, 0)); d = ImageDraw.Draw(im)
for _ in range(1800):
    x, y = random.randrange(w), random.randrange(h); a = random.randint(6, 18)
    d.point((x, y), fill=(120, 110, 100, a))
for _ in range(90):
    x, y = random.randrange(w), random.randrange(h); L = random.randint(6, 18); dx = random.choice([-1, 1])
    for k in range(L):
        d.point(((x + k) % w, (y + dx * k // 3) % h), fill=(120, 110, 100, 10))
save(im, 'paper-grain.png', (w, h))

# 6. 網点（繰り返し。背景の装飾、影の代わり）
w = h = 32
im = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
for cx, cy in [(8, 8), (24, 24)]:
    d.ellipse([(cx - 3) * S, (cy - 3) * S, (cx + 3) * S, (cy + 3) * S], fill=(255, 255, 255, 255))
save(im, 'halftone.png', (w, h))

# 7. 走査線（繰り返し。監視・ログの画面に重ねる）
im = Image.new('RGBA', (4, 4), (255, 255, 255, 0))
for x in range(4):
    im.putpixel((x, 0), (255, 255, 255, 12))
im.save(os.path.join(OUT, 'scanline.png')); print('scanline.png', (4, 4))

# 8. 集中線（中心から外へ。発動カットイン・山場の突破の背景）
w, h = 1024, 576
im = Image.new('RGBA', (w * 2, h * 2), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
import math
cx, cy = w, h; random.seed(3)
for i in range(140):
    a = i / 140 * math.tau + random.uniform(-.01, .01); inner = random.uniform(.32, .5) * h * 2
    wid = random.uniform(.004, .011)
    p = [(cx + math.cos(a - wid) * inner, cy + math.sin(a - wid) * inner), (cx + math.cos(a) * w * 3, cy + math.sin(a) * w * 3), (cx + math.cos(a + wid) * inner, cy + math.sin(a + wid) * inner)]
    d.polygon(p, fill=(255, 255, 255, random.randint(120, 220)))
save(im, 'speed-lines.png', (w, h))
