# 切り抜いた差分を、ゲームで切り替えても位置がずれない共通キャンバスにそろえる。
# ポーズ：全身を同じ縮尺のまま、足元を下端・中央にそろえる。表情：胸上の絵を同じ縮尺で下端・中央にそろえる。
from pathlib import Path
from PIL import Image
cut = Path(r"C:\Projects\PatchWorkSecure\ArtSource\Hinata\gen-20260929\cut")
out = cut.parent / "final"
out.mkdir(exist_ok=True)
def fit(files, size, bottom_pad):
    # 拡大すると線がぼけるので、縮尺は原寸のまま。キャンバスは一番大きい絵に合わせる。
    tallest = max(Image.open(f).height for f in files)
    widest = max(Image.open(f).width for f in files)
    W, H = widest + 16, tallest + bottom_pad + 8
    scale = 1.0
    for f in files:
        im = Image.open(f).convert("RGBA")
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
        canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        canvas.paste(im, ((W - im.width) // 2, H - bottom_pad - im.height), im)
        canvas.save(out / f.name)
    print(len(files), (W, H))
fit(sorted(cut.glob("pose_*.png")), (640, 900), 12)
fit(sorted(cut.glob("face_*.png")), (640, 640), 0)
