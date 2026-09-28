"""ひなたの小さな感情マーク。既存UIの紺の輪郭と色で図形を描き、原画には触れない。"""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[1] / "Assets/Art/UI/HinataEmotions"
OUT.mkdir(parents=True, exist_ok=True)
NAVY = "#1d2a44"

for kind in ["alert", "sweat", "joy", "anger", "thinking", "dust"]:
    im = Image.new("RGBA", (256, 256))
    d = ImageDraw.Draw(im)
    if kind == "alert":
        d.rounded_rectangle((102, 20, 154, 165), 22, fill="#ffd23f", outline=NAVY, width=12)
        d.ellipse((104, 188, 152, 236), fill="#ffd23f", outline=NAVY, width=10)
    elif kind == "sweat":
        d.polygon([(128, 22), (182, 110), (202, 166), (178, 216), (128, 234), (78, 216), (54, 166), (74, 110)], fill="#75caff", outline=NAVY, width=10)
        d.arc((88, 146, 142, 206), 90, 200, fill="white", width=10)
    elif kind == "joy":
        d.polygon([(128, 20), (157, 95), (234, 128), (157, 157), (128, 236), (99, 157), (20, 128), (99, 95)], fill="#ffd85c", outline=NAVY, width=10)
        d.line([(128, 72), (128, 111), (151, 126)], fill="white", width=9)
    elif kind == "anger":
        for pts in [[(42, 92), (91, 92), (91, 43)], [(163, 43), (163, 92), (212, 92)], [(42, 163), (91, 163), (91, 212)], [(163, 212), (163, 163), (212, 163)]]:
            d.line(pts, fill=NAVY, width=24, joint="curve")
            d.line(pts, fill="#ff6f91", width=10, joint="curve")
    else:
        for box in [(28, 100, 135, 210), (80, 65, 193, 210), (145, 110, 232, 210)]:
            d.ellipse(box, fill="#eef2f8", outline=NAVY, width=9)
        d.rounded_rectangle((62, 126, 202, 204), 16, fill="#eef2f8")
        if kind == "thinking":
            d.arc((78, 97, 171, 180), 20, 320, fill="#8e7cc3", width=11)
            d.ellipse((179, 216, 199, 236), fill="#eef2f8", outline=NAVY, width=5)
    im.resize((64, 64), Image.Resampling.LANCZOS).save(OUT / (kind + ".png"))
print("感情マーク6種を生成しました。")
