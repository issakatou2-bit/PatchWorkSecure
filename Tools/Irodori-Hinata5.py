# 声の一覧 その5（10/2）：公開できるひなたの声を、Irodori-TTSで一から作る候補
# ElevenLabsの無料枠の声は商用に使えず、それを見本（--ref-wav）にした声も公開には使わない。
# そこで見本なし・文字の説明（キャプション）だけで声を作り、気に入った1本を本番の見本にして全行を作る（かのん・りりぃと同じ手順）。
# 使い方（Irodori-TTSの仮想環境のPython。モデルごとに別々に実行する）:
#   .../.venv/Scripts/python.exe Tools/Irodori-Hinata5.py --model large
#   .../.venv/Scripts/python.exe Tools/Irodori-Hinata5.py --model small
#   .../.venv/Scripts/python.exe Tools/Irodori-Hinata5.py --page
# 出力：Artifacts/VoiceAudition/v5/（gitで追跡しない）。「元」はElevenLabsの元の声（聞き比べだけ。この一覧の外へ出さない）。
import html, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v5'
HINATA = ROOT / 'Assets/Audio/CompanyYear/VoiceTest/Hinata'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {'small': 'Aratako/Irodori-TTS-v4.1-Small', 'large': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'}
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
CAPS = {
    'A': '明るく元気な二十代前半の女性。高めで澄んだ声。語尾が弾むように、はきはきと話す。笑顔が伝わる声。',
    'B': '元気いっぱいの若い女性の後輩。少し高めでかわいらしい声だが、幼すぎない。勢いよく、楽しそうに話す。',
    'C': 'アニメの明るいヒロインのような、張りのある高めの声の若い女性。表情豊かで、元気に話す。',
    'D': '人懐っこく、ほがらかな若い女性。やわらかく甘さのある高めの声。少し早口で、うれしそうに話す。',
}
# 候補（名前, モデル, キャプション, 種）
CANDS = [(f'{m[0].upper()}{k}{s}', m, k, seed) for m, keys, seeds in (('large', 'ABCD', (1, 2)), ('small', 'AB', (1,)))
         for k in keys for s, seed in zip((1, 2), (20261021, 20261022)[:len(seeds)])]
# 聞き比べる台詞（ElevenLabsの元の声があるもの）：id, 字幕, 読み
LINES = [('maxim_backup', 'バックアップは、戻せてこそバックアップ、だよ！', 'バックアップは、もどせてこそ、バックアップ、だよ！'),
         ('mg_end_good', '完璧〜っ！　プロの仕事だね！', 'かんぺき〜っ！　プロのしごとだね！'),
         ('peak_goal_06', '今月は最初の山場だよ！　目標、ちゃんと見ておいてね！', 'こんげつは、さいしょのやまばだよ！　もくひょう、ちゃんと見ておいてね！')]


def generate(model):
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    done = 0
    for name, m, key, seed in CANDS:
        if m != model:
            continue
        for rid, _, read in LINES:
            out = OUT / 'audio' / f'{name}-{rid}.wav'
            if out.exists():
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[m], '--model-precision', 'bf16', '--text', read,
                        '--caption', CAPS[key] + CLEAR, '--no-ref', '--seed', str(seed), '--output-wav', str(out)]
            infer.main()
            done += 1
    print('生成', done, '本')


def page():
    (OUT / 'orig').mkdir(parents=True, exist_ok=True)
    for rid, _, _ in LINES:
        shutil.copy(HINATA / f'{rid}.mp3', OUT / 'orig' / f'{rid}.mp3')
    art = ROOT / 'Artifacts' / 'VoiceAudition' / 'art'
    face = next((f'../art/{p.name}' for p in sorted(art.glob('*hinata*'))), '') if art.exists() else ''

    def btn(src, cls=''):
        return f'<button class="sm {cls}" onclick="play(this,&quot;{src}&quot;)">▶</button>'
    head = ''.join(f'<th>{html.escape(t)}</th>' for _, t, _ in LINES)
    orig = '<tr class="o"><td><b>元</b><br><small>ElevenLabs（私的な試しのみ）</small></td>' + ''.join(f'<td>{btn(f"orig/{rid}.mp3", "o")}</td>' for rid, _, _ in LINES) + '</tr>'
    rows = ''
    for name, m, key, seed in CANDS:
        files = [f'audio/{name}-{rid}.wav' for rid, _, _ in LINES]
        cells = ''.join(f'<td>{btn(f) if (OUT / f).exists() else "<span class=miss>未生成</span>"}</td>' for f in files)
        rows += (f'<tr><td><b>{name}</b> <button class="all" onclick="playAll(this,{html.escape(json.dumps(files))})">3本続けて</button>'
                 f'<br><small>{"大型" if m == "large" else "小型"}・説明{key}・種{seed % 100}</small></td>{cells}</tr>')
    caps = ''.join(f'<li><b>説明{k}</b>：{html.escape(v)}</li>' for k, v in CAPS.items())
    img = f'<img src="{face}" alt="">' if face else ''
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>ひなたの声 その5</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px;display:flex;gap:18px;align-items:center}} header img{{height:120px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}}
header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.6}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} th,td{{border-top:1px solid #eef1f6;padding:8px;text-align:left;font-weight:700;vertical-align:middle}} th{{font-size:13px;color:#52607a}}
small{{color:#8a8399;font-weight:500}} tr.o td{{background:#f4f6fa}} ul{{margin:0;padding-left:20px;line-height:1.8;font-size:14px}} .miss{{color:#b9a;font-size:12px}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:13px}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894}}
button.all{{height:28px;padding:0 10px;font-size:12px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header>{img}<div><h1>ひなたの声 その5（公開できる声の候補）</h1>
<p>見本の声を使わず、文字の説明だけで作った候補。気に入った候補を本番の見本にして、191行を作り直す。<br>「元」はElevenLabsの今の声（聞き比べだけ。公開には使えない）。</p></div></header>
<section><ul>{caps}</ul></section>
<section><table><tr><th>候補</th>{head}</tr>{orig}{rows}</table></section>
<script>let a=null,q=[];function play(btn,src){{q=[];if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}
function playAll(btn,list){{if(a)a.pause();q=list.slice();btn.classList.add('on');step(btn)}}function step(btn){{if(!q.length){{btn.classList.remove('on');return}}a=new Audio(q.shift());a.onended=()=>setTimeout(()=>step(btn),300);a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--page' in sys.argv:
        page()
    else:
        generate(sys.argv[sys.argv.index('--model') + 1])
        page()
