# デザインキャンバスの .dc.html を、リポジトリ内で単体表示できるHTMLに書き出す。
# 使い方: py -3 Tools/Export-Mockups.py <キャンバスのprojectフォルダ>
# 画像はキャンバスへアップロードした素材（/_blob/...）を、リポジトリ内の同じ素材への相対パスに置き換える。
import sys
from pathlib import Path

CUT = "../../ArtSource/Hinata/gen-20260929/cut/"
BLOBS = {
    "6027b8c25da0726f66183f0d347603f8": "../../Assets/Art/Office/office-topdown.png",
    "931aef9bdbb433f485581b4dd6d7c99f": "../../Assets/Sprites/Hinata/hinata_normal.png",
    "39415d0e16060bd73847234c0cfc0926": "../../Artifacts/CompanyOps/02-april.png",
    "ea86df44bd211b29f1feef71bded9fd6": "../../Artifacts/CompanyOps/42-compact-planning.png",
    "5fe6ef928f6f1ee956cc58430ce2f457": CUT + "pose_fists.png",
    "252ba48c2435da0e15ab0af66659febf": CUT + "pose_startled.png",
    "c2393ed76f112711d788cae0bb1d1f74": CUT + "pose_jump.png",
    "a48e8acc5a351afff39095e0cbb13fef": CUT + "pose_wave.png",
    "3cf3afa33f9f9399958694dfe872be73": CUT + "pose_point.png",
    "553ac7d2802c35c19a04df1cdb3593c2": CUT + "pose_peace.png",
    "ffb718e7f7376e46a628aa80e882b502": CUT + "base_fullbody.png",
    "a13f4b69db6398d38d0be206ad9b0d05": "../../ArtSource/Hinata/gen-20260929/contact-sheet.png",
    "821782da42bb0ef7c0141c6b60e9b794": "../../Assets/Art/UI/Logo/icon.png",
    "69e55b73f0389d3731fd1b8aa79a350e": "../../Assets/Art/UI/Logo/wordmark.png",
}
NAMES = {
    "Main": "planning-screen", "Before": "current-planning-v0.8", "Hinata": "hinata-outfit-a", "Title": "title-screen",
    "Mission": "mission-brief", "Incident": "incident-choose", "Resolve": "incident-resolve", "Tutorial": "tutorial",
    "Report": "monthly-report", "Annual": "annual-report", "Settings": "settings", "UIKit": "ui-kit", "HinataSheet": "hinata-sheet", "Logo": "logo",
}
src = Path(sys.argv[1])
out = Path(__file__).resolve().parent.parent / "Docs" / "Mockups"
for board, name in NAMES.items():
    lines, keep = (src / f"{board}.dc.html").read_text(encoding="utf-8").splitlines(), []
    for line in lines:
        if line == "</x-dc>":
            break
        if "support.js" in line or line in ("<x-dc>", "<helmet>", "</helmet>"):
            continue
        for blob, path in BLOBS.items():
            line = line.replace("/_blob/" + blob, path)
        keep.append(line)
    keep += ["</body>", "</html>"]
    text = "\n".join(keep) + "\n"
    assert "/_blob/" not in text, board
    (out / f"{name}.html").write_text(text, encoding="utf-8")
    print(name)
