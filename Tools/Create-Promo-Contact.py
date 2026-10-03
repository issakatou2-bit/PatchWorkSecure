"""宣伝素材の連番・音一覧を検査し、最初/中央/最後の確認画像を作る。"""
import argparse
import json
import struct
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

EXPECTED = {
    "01-office": 150, "02-planning": 180, "03-incident": 180,
    "04-mini-b": 120, "05-mini-c": 120, "06-mini-e": 120,
    "07-rivals": 150, "08-pairs": 180, "09-title": 180,
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("Artifacts/Promo"))
    args = parser.parse_args()
    root = args.root.resolve()
    project = Path(__file__).resolve().parents[1]
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 22)
    sheet = Image.new("RGB", (1920, 80 + len(EXPECTED) * 400), "#f5f7fa")
    draw = ImageDraw.Draw(sheet)
    draw.text((16, 12), "PatchWorkSecure | 1920 x 1080 | 30 fps | First / Middle / Last", fill="#1d2a44", font=font)
    summary = {"fps": 30, "width": 1920, "height": 1080, "shots": [],
               "notice": "Hinata voice: private only; BGM: pending license confirmation. No video mix authorized."}
    for row, (name, count) in enumerate(EXPECTED.items()):
        folder = root / "shots" / name
        files = sorted(folder.glob("frame_*.png"))
        expected = [f"frame_{i:05d}.png" for i in range(1, count + 1)]
        if [f.name for f in files] != expected:
            raise ValueError(f"連番の欠落/余分: {name} {len(files)} / {count}")
        for file in files:
            with file.open("rb") as handle:
                header = handle.read(24)
            if header[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", header[16:24]) != (1920, 1080):
                raise ValueError(f"PNGの実寸が違う: {file}")
        sounds = json.loads((folder / "sounds.json").read_text(encoding="utf-8-sig"))
        if sounds["fps"] != 30 or sounds["frames"] != count:
            raise ValueError(f"音一覧の時間情報が違う: {name}")
        for sound in sounds["sounds"]:
            if not (1 <= sound["frame"] <= count):
                raise ValueError(f"音のコマが範囲外: {name} {sound}")
            if not (project / sound["file"]).is_file():
                raise ValueError(f"音源が見つからない: {sound['file']}")
        y = 80 + row * 400
        for column, index in enumerate((1, count // 2, count)):
            with Image.open(folder / f"frame_{index:05d}.png") as frame:
                sheet.paste(frame.convert("RGB").resize((640, 360), Image.Resampling.LANCZOS), (column * 640, y + 34))
            draw.text((column * 640 + 8, y + 3), f"{name} | frame {index:05d}", fill="#1d2a44", font=font)
        summary["shots"].append({"shot": name, "frames": count, "seconds": count / 30,
                                  "sounds": len(sounds["sounds"]), "bytes": sum(f.stat().st_size for f in files)})
    sheet.save(root / "contact-sheet.png")
    summary["frames"] = sum(EXPECTED.values())
    (root / "manifest.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"shots": len(EXPECTED), "frames": summary["frames"], "contact": str(root / "contact-sheet.png")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
