# 台本CSVから、ひなたの声をElevenLabsのAPIで1行ずつ生成する。
# 使い方: py -3 Tools/Generate-HinataVoice.py [台本CSV] [出力フォルダ] [--only id1,id2] [--force]
# APIキーは環境変数 ELEVENLABS_API_KEY から読む（ファイルやログには書かない）。
# 無料プランの音声は試遊専用。出力先はgitで追跡しないフォルダにする。
import csv, json, os, sys, time, urllib.request, winreg
from pathlib import Path

VOICE_ID = "ADwaA3FlFcOVWmII6hQe"   # Hinata V9-2（2026-09-29に決定）
MODEL_ID = "eleven_v4"
SETTINGS = {"stability": 0.5, "similarity_boost": 0.9}

def api_key():
    key = os.environ.get("ELEVENLABS_API_KEY")
    if not key:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as k:
            key = winreg.QueryValueEx(k, "ELEVENLABS_API_KEY")[0]
    return key

def speak(key, text, path):
    body = json.dumps({"text": text, "model_id": MODEL_ID, "language_code": "ja", "voice_settings": SETTINGS}).encode("utf-8")
    req = urllib.request.Request(
        f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}?output_format=mp3_44100_128",
        data=body, headers={"xi-api-key": key, "Content-Type": "application/json", "Accept": "audio/mpeg"})
    with urllib.request.urlopen(req, timeout=120) as r:
        path.write_bytes(r.read())

def main():
    argv = sys.argv[1:]
    if "--only" in argv:
        i = argv.index("--only")
        argv = argv[:i] + argv[i + 2:]
    args = [a for a in argv if not a.startswith("--")]
    root = Path(__file__).resolve().parent.parent
    script = Path(args[0]) if args else root / "Docs/Voice/hinata-script-v2.csv"
    out = Path(args[1]) if len(args) > 1 else root / "Assets/Audio/CompanyYear/VoiceTest/Hinata"
    only = None
    if "--only" in sys.argv:
        only = set(sys.argv[sys.argv.index("--only") + 1].split(","))
    force = "--force" in sys.argv
    out.mkdir(parents=True, exist_ok=True)
    key = api_key()
    rows = list(csv.DictReader(script.open(encoding="utf-8")))
    done = chars = 0
    for row in rows:
        rid = row["id"]
        if only and rid not in only:
            continue
        path = out / f"{rid}.mp3"
        if path.exists() and not force:
            continue
        text = (row["演技の指示"] + " " + row["セリフ"]).strip()
        for attempt in range(3):
            try:
                speak(key, text, path)
                break
            except Exception as e:
                print(rid, "失敗", attempt + 1, e)
                time.sleep(3 * (attempt + 1))
        else:
            continue
        done += 1
        chars += len(row["セリフ"])
        print(rid, path.stat().st_size, "bytes")
        time.sleep(0.4)
    print(f"生成 {done} 本 / 約{chars}文字")

if __name__ == "__main__":
    main()
