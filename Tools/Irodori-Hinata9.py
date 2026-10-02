# 声の一覧 その9（10/2）：ひなたの声をゼロから探し直す（こもりの原因を取り除いて）
# その7の「急にくぐもる・最初は良いのにこもる」の原因：見本にしたその5の3本のうち、peak_goal_06が最初から最後まで
# 4kHzより上がほぼ無い（通話のような音）録音だった。見本がこもった音を含むと、作る声もときどきこもる。
# かのん・りりぃの見本はこもりが無く、本編の24本も全部こもりが無い（10/2に確認）。だから「見本から作る」方法は使える。
# 手順：①LD2の系統の説明で、種を変えて16通り作る（大型）→ ②0.5秒ごとにこもりを調べ、1区間でもこもった候補は外す
#       ③残った候補から加藤さんが声を選ぶ → ④選んだ声で、こもりの無い見本を作り直して全行へ（その10で）
# 使い方：.../.venv/Scripts/python.exe Tools/Irodori-Hinata9.py
# 出力：Artifacts/VoiceAudition/v9/（gitで追跡しない）。見本なし・文字の説明だけで作るので、公開に使える。
import html, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v9'
HINATA = ROOT / 'Assets/Audio/CompanyYear/VoiceTest/Hinata'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODEL = 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'
SEEDS = list(range(20261101, 20261117))
# その5のLD2（説明D）と同じ文。1回目は「こもりや電話のような音は無い」と書き足したら、16通り中15通りがこもった
# （打ち消しは伝わらず、「電話」という言葉に引っぱられる）。説明に「電話」「こもり」などの言葉を入れない
CAP = ('人懐っこく、ほがらかな若い女性。やわらかく甘さのある高めの声。少し早口で、うれしそうに話す。'
       '近いマイクで録った、こもりのないクリアな音質。')
LINES = [('greet', 'おはようございます、先輩！　今日も、なにごともない一日にしようね！'),
         ('maxim', 'バックアップは、戻せてこそバックアップ、だよ！')]
ORIG = {'greet': 'tutorial_1', 'maxim': 'maxim_backup'}  # 元の声（ElevenLabs）で近い行。聞き比べだけ


def generate():
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    for i, seed in enumerate(SEEDS):
        for lid, text in LINES:
            out = OUT / 'audio' / f'H{i + 1:02d}-{lid}.wav'
            if out.exists():
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODEL, '--model-precision', 'bf16', '--text', text, '--caption', CAP, '--no-ref',
                        '--seed', str(seed), '--output-wav', str(out)]
            infer.main()


def muffled(path):
    # 0.5秒ごとに、4kHzより上の音の割合を調べる。声のある区間で0.001未満が1つでもあれば「こもり」
    import numpy as np, soundfile as sf
    d, sr = sf.read(path)
    d = d.mean(1) if d.ndim > 1 else d
    w, bad, n = int(sr * .5), 0, 0
    for s in range(0, max(len(d) - w, 1), w):
        x = d[s:s + w]
        if float((x ** 2).mean()) < 1e-4:  # 無音に近い区間は数えない
            continue
        sp = np.abs(np.fft.rfft(x)) ** 2
        f = np.fft.rfftfreq(len(x), 1 / sr)
        n += 1
        bad += float(sp[f > 4000].sum() / sp.sum()) < 0.001
    return bad, n


def page():
    (OUT / 'orig').mkdir(parents=True, exist_ok=True)
    for rid in ORIG.values():
        if (HINATA / f'{rid}.mp3').exists():
            shutil.copy(HINATA / f'{rid}.mp3', OUT / 'orig' / f'{rid}.mp3')
    res, rows, ok = {}, '', 0
    for i in range(len(SEEDS)):
        name = f'H{i + 1:02d}'
        cells, clean = '', True
        for lid, _ in LINES:
            p = OUT / 'audio' / f'{name}-{lid}.wav'
            if not p.exists():
                cells += '<td>未生成</td>'
                continue
            bad, n = muffled(p)
            res[p.name] = [bad, n]
            clean &= bad == 0
            cells += f'<td><button class="sm" onclick="play(this,&quot;audio/{p.name}&quot;)">▶</button>{"<i>こもり</i>" if bad else ""}</td>'
        ok += clean
        rows += f'<tr class="{"" if clean else "ng"}"><td><b>{name}</b>{"" if clean else "<br><small>こもりあり・外す</small>"}</td>{cells}</tr>'
    (OUT / 'checks.json').write_text(json.dumps(res, indent=1), encoding='utf-8')
    orig = '<tr class="o"><td><b>元</b><br><small>ElevenLabs（私的な試しのみ）</small></td>' + ''.join(
        f'<td><button class="sm o" onclick="play(this,&quot;orig/{ORIG[lid]}.mp3&quot;)">▶</button></td>' for lid, _ in LINES) + '</tr>'
    head = ''.join(f'<th>{html.escape(t)}</th>' for _, t in LINES)
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>ひなたの声 その9</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:8px;text-align:left;font-weight:700}} th{{font-size:13px;color:#52607a}}
tr.ng{{opacity:.45}} tr.o td{{background:#f4f6fa}} small{{color:#8a8399;font-weight:500}} i{{font-style:normal;font-size:11px;color:#c9486c;margin-left:6px}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:13px}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>ひなたの声 その9　ゼロから探し直す</h1>
<p>その7がこもった原因は、見本にした3本のうち1本（その5の「今月は最初の山場だよ」）が、最初から最後まで通話のような音だったこと。<br>
LD2の系統の説明（その5と同じ文）で、種を変えて16通り作り、0.5秒ごとにこもりを調べた。こもりが1か所でもある候補は薄く表示（外す）。残り{ok}通りから、ひなたに近い声を選んでください。<br>
選んだ声で、こもりの無い見本を作り直してから全行を作ります（見本がきれいなら安定することは、かのん・りりぃで確認済み）。</p></header>
<section><table><tr><th>候補</th>{head}</tr>{orig}{rows}</table></section>
<script>let a=null;function play(btn,src){{if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html', 'こもり無し', ok)


if __name__ == '__main__':
    if '--page' not in sys.argv:
        generate()
    page()
