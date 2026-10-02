"""Next-14の実画面写真の実寸・一覧・確認用コンタクトシートを保存する。"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / "Artifacts" / "Store"
NAMES = (
    "01-title", "02-planning-y3-autumn", "03-incident-choice",
    "04-minigame-b-containment", "05-minigame-c-mail", "06-minigame-d-mfa",
    "07-minigame-e-logs", "08-minigame-f2-blocks", "09-minigame-g-restore",
    "10-monthly-report", "11-annual-rating", "12-diary-pair-june",
    "13-opening-y2-rivals", "14-rival-appearance", "15-endless-continue", "16-diary-pair-october",
)

def main():
    rows = []
    sheet = Image.new("RGB", (2560, 1536), "white")
    draw = ImageDraw.Draw(sheet)
    for index, name in enumerate(NAMES):
        path = FOLDER / (name + ".png")
        with Image.open(path) as photo:
            if photo.size != (1920, 1080):
                raise ValueError(f"実寸が違います: {path}: {photo.size}")
            rows.append(dict(file=path.name, width=1920, height=1080,
                             sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
            x, y = index % 4 * 640, index // 4 * 384
            draw.text((x + 8, y + 5), name, fill="black")
            sheet.paste(photo.convert("RGB").resize((640, 360), Image.Resampling.LANCZOS), (x, y + 24))
    review = FOLDER / "Review"
    review.mkdir(exist_ok=True)
    sheet.save(review / "contact-sheet.jpg", quality=95)
    (review / "manifest.json").write_text(json.dumps(dict(
        count=len(rows), capture="Unity Canvasの1920x1080実描画", images=rows,
        note="一覧のラベルは確認用のシートだけ。元の写真にデバッグ表示はない。音声や動画は含まない。"
    ), ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"確認: {len(rows)}枚すべて1920x1080 / {review}")

if __name__ == "__main__":
    main()
