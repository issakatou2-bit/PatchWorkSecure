# 声の試しを「一覧のファイル」から作る（10/3）。Tools/Irodori-*.py を試しのたびに書き直さず、作る声と台詞だけを
# Docs/Voice/jobs/<名前>.json に書く。決まりは Docs/Voice/Irodori-Direction-Rules.md（.claude/skills/voice-direction）。
# 使い方（Irodori-TTSの仮想環境のPython）：
#   .../.venv/Scripts/python.exe Tools/Irodori-Make.py Docs/Voice/jobs/<名前>.json          … 生成（モデルごとに別の処理で）→確かめ→一覧
#   .../.venv/Scripts/python.exe Tools/Irodori-Make.py Docs/Voice/jobs/<名前>.json --page   … 一覧だけ作り直す
# 一覧のファイルの形：
#   {"title": "...", "out": "v14", "note": "ページの説明", "seed": 20261201, "takes": 3, "talk": "全員に足す一文（空でよい）",
#    "voices": {"key": {"name": "表示名", "model": "large|small|full", "caption": "声と話し方", "refs": ["見本.wav", ...], "extra": ["--cfg-scale-speaker", "3"]}},
#    "lines": [{"id": "...", "text": "字幕", "read": "声に渡す文（空なら字幕）", "direction": "演技の指示", "orig": "聞き比べる元の音（空でよい）"}]}
# 出力：Artifacts/VoiceAudition/<out>/（gitで追跡しない）。こもり・読み違いには印を付ける（最後は人が耳で選ぶ）。
import html, json, shutil, subprocess, sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import irodori_kit as kit

ROOT = kit.ROOT


def load(path):
    job = json.loads(Path(path).read_text(encoding='utf-8'))
    job.setdefault('takes', 3)
    job.setdefault('talk', '')
    return job, ROOT / 'Artifacts' / 'VoiceAudition' / job['out']


def generate(job, out, model):
    for vk, v in job['voices'].items():
        if v['model'] != model:
            continue
        refs = [ROOT / r for r in v.get('refs', [])]
        for ln in job['lines']:
            for k in range(job['takes']):
                f = out / 'audio' / f"{vk}-{ln['id']}-{k + 1}.wav"
                if not f.exists():
                    kit.synth(model, ln.get('read') or ln['text'], v['caption'] + job['talk'] + ln.get('direction', ''), f, job['seed'] + k, refs, v.get('extra', []))


def check(job, out):
    hear = kit.whisper()
    res = {}
    for f in sorted((out / 'audio').glob('*.wav')):
        res[f.name] = {'text': hear(f), 'muffled': kit.muffled(f)[0]}
    (out / 'checks.json').write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding='utf-8')


def page(job, out):
    ck = json.loads((out / 'checks.json').read_text(encoding='utf-8')) if (out / 'checks.json').exists() else {}
    voices = list(job['voices'].items())
    has_orig = any(ln.get('orig') for ln in job['lines'])
    if has_orig:
        (out / 'orig').mkdir(parents=True, exist_ok=True)

    def take(vk, ln):
        s = ''
        for k in range(job['takes']):
            name = f"{vk}-{ln['id']}-{k + 1}.wav"
            c = ck.get(name, {})
            mark = ('<i>こもり？</i>' if c.get('muffled') else '') + ('<i>読み？</i>' if c and kit.similarity(c['text'], ln['text']) < .6 else '')
            s += f'<span class="take"><button class="sm" title="{html.escape(c.get("text", ""))}" onclick="play(this,&quot;audio/{name}&quot;)">{k + 1}</button>{mark}</span>'
        return f'<td>{s}</td>'
    rows = ''
    for ln in job['lines']:
        orig = ''
        if has_orig:
            if ln.get('orig'):
                src = ROOT / ln['orig']
                shutil.copy(src, out / 'orig' / src.name)
                orig = f'<td><button class="sm o" onclick="play(this,&quot;orig/{src.name}&quot;)">元</button></td>'
            else:
                orig = '<td></td>'
        rows += (f'<tr><td><b>{html.escape(ln["text"])}</b><div class="dir">{html.escape(ln.get("direction", ""))}</div></td>{orig}'
                 + ''.join(take(vk, ln) for vk, _ in voices) + '</tr>')
    def first_takes(vk):
        return html.escape(json.dumps([f"audio/{vk}-{ln['id']}-1.wav" for ln in job['lines']]))
    alls = ''.join(f'<button class="all" onclick="playAll(this,{first_takes(vk)})">{html.escape(v["name"])}を通しで</button>' for vk, v in voices)
    head = '<th>台詞</th>' + ('<th>元</th>' if has_orig else '') + ''.join(f'<th>{html.escape(v["name"])}</th>' for _, v in voices)
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>{html.escape(job["title"])}</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:22px 28px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:28px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7;font-size:14px}}
section{{margin:14px 28px;background:#fff;border-radius:22px;padding:14px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:7px 6px;text-align:left;font-weight:700;vertical-align:top}} th{{font-size:13px;color:#52607a}}
.dir{{font-size:12px;color:#6a5200;font-weight:500}} .take{{display:inline-flex;flex-direction:column;align-items:center;margin-right:4px}} i{{font-style:normal;font-size:10px;color:#c9486c}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:32px;height:32px;font-size:13px;font-family:'M PLUS Rounded 1c'}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894;font-size:11px}}
button.all{{height:30px;padding:0 10px;margin:0 6px 6px 0;font-size:12px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>{html.escape(job["title"])}</h1><p>{html.escape(job.get("note", ""))}<br>1行に{job["takes"]}通り。機械の印：こもり？＝通話のような音、読み？＝書き起こしが台詞と大きく違う。ボタンに指を置くと書き起こしが出る。</p></header>
<section>{alls}<table><tr>{head}</tr>{rows}</table></section>
<script>let a=null,q=[];function play(btn,src){{q=[];if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}
function playAll(btn,list){{if(a)a.pause();q=list.slice();btn.classList.add('on');step(btn)}}function step(btn){{if(!q.length){{btn.classList.remove('on');return}}a=new Audio(q.shift());a.onended=()=>setTimeout(()=>step(btn),300);a.play()}}</script>
</body></html>'''
    (out / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', out / 'index.html')


if __name__ == '__main__':
    job, out = load(sys.argv[1])
    if '--model' in sys.argv:
        generate(job, out, sys.argv[sys.argv.index('--model') + 1])
    elif '--check' in sys.argv:
        check(job, out)
    elif '--page' in sys.argv:
        page(job, out)
    else:
        # モデルごとに別の処理で作る（同じ処理で切り替えると落ちる）。そのあと確かめて一覧を作る
        for m in sorted({v['model'] for v in job['voices'].values()}):
            subprocess.run([sys.executable, __file__, sys.argv[1], '--model', m], check=True)
        subprocess.run([sys.executable, __file__, sys.argv[1], '--check'], check=True)
        page(job, out)
