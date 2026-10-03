# 声の一覧 その12（10/3）：H02・H09を「おはようございます」を軸に固定して、元の声と同じ台詞で比べる
# 加藤さんの感想（その11）：H02とH09が良い。少しぶれるので、「おはようございます」の声を軸に安定させたい。元との比較をもう一度。
# 直し方：見本をその9の「おはようございます」1本だけにする（2本だと調子の違う「格言」の声が混ざる）。見本への寄せ方は少し強め（5）。
#         台詞はElevenLabsの元の声がある10行と同じ文にして、1行ずつ「元・H02・H09」を並べて聞けるようにする。
#         その9・その11の「元」の行の一部は別の台詞（tutorial_1）だった。今回はすべて同じ台詞。
# 使い方：.../.venv/Scripts/python.exe Tools/Irodori-Hinata12.py
# 出力：Artifacts/VoiceAudition/v12/（gitで追跡しない）
import difflib, html, importlib.util, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v12'
V9 = ROOT / 'Artifacts' / 'VoiceAudition' / 'v9' / 'audio'
HINATA = ROOT / 'Assets/Audio/CompanyYear/VoiceTest/Hinata'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODEL = 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'
SEEDS = (20261121, 20261122, 20261123)
PICKS = ['H02', 'H09']
BASE = '社会人2年目の、明るく元気な女性。人懐っこく、ほがらかで、やわらかく甘さのある高めの声。近いマイクで録った、こもりのないクリアな音質。'
TALK = '情シスの後輩が、職場の先輩に話しかけている。読み上げではなく、目の前の相手との会話。'
# （元の声のid, 字幕, 声に渡す文, 演技の指示）。文は台本`Docs/Voice/hinata-script-v2.csv`と同じ
LINES = [
    ('tutorial_1', 'ようこそ、情シスへ！　上の数字が予算と工数、左が会社の力だよ！', 'ようこそ、情シスへ！😊　上の数字が予算とこうすう、左が会社の力だよ！', '初めて来た先輩を歓迎して、画面を指さしながら明るく案内する。'),
    ('peak_goal_06', '今月は最初の山場だよ！　目標、ちゃんと見ておいてね！', 'こんげつは最初の山場だよ！💪　目標、ちゃんと見ておいてね！', '気合いを入れて、先輩の背中を押すように言う。'),
    ('mg_start_01', 'よーし、いくよっ！', 'よーし、いくよっ！💥', '腕まくりをして、元気よく始める。'),
    ('mg_combo_01', 'その調子！', 'その調子！😆', '横で見ていて、うれしくなって声をかける。'),
    ('mg_miss_01', 'あっ、今のは惜しい！', 'あっ😲、今のは惜しい！', '思わず声が出て、すぐに励ますように言う。'),
    ('mg_end_good', '完璧〜っ！　プロの仕事だね！', 'かんぺき〜っ！😆　プロの仕事だね！', '心から感心して、弾むようにほめる。'),
    ('mg_end_bad', 'う〜ん、次はもっとうまくやろ！', 'う〜ん🤔、次はもっとうまくやろ！', '少し残念そうに笑って、前向きに励ます。'),
    ('maxim_backup', 'バックアップは、戻せてこそバックアップ、だよ！', 'バックアップは、戻せてこそバックアップ、だよ！😎', '人差し指を立てて、得意げに、でもかわいらしく教える。'),
    ('maxim_human', '人は間違えるもの。だから、仕組みで守ろ！', '人は間違えるもの。🫶だから、仕組みで守ろ！', 'やさしく、相手を責めないように言う。'),
    ('diary_y1_04', 'こっそり復元を試したら、本番に上書きしかけた……', 'こっそり復元を試したら、本番に上書きしかけた……🫣', '夜、日記を書きながら、照れ笑いまじりに小声で言う。'),
]


def generate():
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    for v in PICKS:
        ref = str(V9 / f'{v}-greet.wav')
        for rid, _, text, direction in LINES:
            for k, seed in enumerate(SEEDS):
                out = OUT / 'audio' / f'{v}-{rid}-{k + 1}.wav'
                if out.exists():
                    continue
                out.parent.mkdir(parents=True, exist_ok=True)
                sys.argv = ['infer.py', '--hf-checkpoint', MODEL, '--model-precision', 'bf16', '--text', text, '--caption', BASE + TALK + direction,
                            '--ref-wav', ref, '--cfg-scale-speaker', '5', '--cfg-scale-caption', '4', '--seed', str(seed), '--output-wav', str(out)]
                infer.main()


def norm(s):
    return ''.join(c for c in s if c.isalnum())


def check():
    import os, torch
    os.add_dll_directory(os.path.join(os.path.dirname(torch.__file__), 'lib'))
    os.environ['PATH'] = os.path.join(os.path.dirname(torch.__file__), 'lib') + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    spec = importlib.util.spec_from_file_location('h9', ROOT / 'Tools' / 'Irodori-Hinata9.py')
    h9 = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(h9)
    m = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16')
    res = {p.name: {'text': ''.join(s.text for s in m.transcribe(str(p), language='ja', beam_size=5)[0]), 'muffled': h9.muffled(p)[0]}
           for p in sorted((OUT / 'audio').glob('*.wav'))}
    (OUT / 'checks.json').write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding='utf-8')


def page():
    ck = json.loads((OUT / 'checks.json').read_text(encoding='utf-8')) if (OUT / 'checks.json').exists() else {}
    (OUT / 'orig').mkdir(parents=True, exist_ok=True)
    (OUT / 'anchor').mkdir(parents=True, exist_ok=True)
    for rid, *_ in LINES:
        shutil.copy(HINATA / f'{rid}.mp3', OUT / 'orig' / f'{rid}.mp3')
    for v in PICKS:
        shutil.copy(V9 / f'{v}-greet.wav', OUT / 'anchor' / f'{v}.wav')

    def cell(v, rid, sub):
        out = ''
        for k in range(len(SEEDS)):
            name = f'{v}-{rid}-{k + 1}.wav'
            c = ck.get(name, {})
            marks = ('<i>こもり？</i>' if c.get('muffled') else '') + ('<i>読み？</i>' if c and difflib.SequenceMatcher(None, norm(c['text']), norm(sub)).ratio() < .6 else '')
            out += f'<span class="take"><button class="sm" title="{html.escape(c.get("text", ""))}" onclick="play(this,&quot;audio/{name}&quot;)">{k + 1}</button>{marks}</span>'
        return f'<td>{out}</td>'
    rows = ''.join(f'<tr><td><b>{html.escape(sub)}</b><div class="dir">演技：{html.escape(d)}</div></td><td><button class="sm o" onclick="play(this,&quot;orig/{rid}.mp3&quot;)">元</button></td>'
                   + ''.join(cell(v, rid, sub) for v in PICKS) + '</tr>' for rid, sub, _, d in LINES)
    anchors = ''.join(f'<button class="all" onclick="play(this,&quot;anchor/{v}.wav&quot;)">{v}の軸（おはようございます）</button>' for v in PICKS)
    alls = ''.join(f'<button class="all" onclick="playAll(this,{html.escape(json.dumps([f"audio/{v}-{l[0]}-1.wav" for l in LINES]))})">{v}を通しで</button>' for v in PICKS)
    orig_all = f'<button class="all o2" onclick="playAll(this,{html.escape(json.dumps([f"orig/{l[0]}.mp3" for l in LINES]))})">元を通しで</button>'
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>ひなたの声 その12</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:22px 28px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:28px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7;font-size:14px}}
section{{margin:14px 28px;background:#fff;border-radius:22px;padding:14px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:7px 6px;text-align:left;font-weight:700;vertical-align:top}} th{{font-size:13px;color:#52607a}}
.dir{{font-size:12px;color:#6a5200;font-weight:500}} .take{{display:inline-flex;flex-direction:column;align-items:center;margin-right:4px}} i{{font-style:normal;font-size:10px;color:#c9486c}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:32px;height:32px;font-size:13px;font-family:'M PLUS Rounded 1c'}} button.o,button.o2{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894}} button.o{{font-size:11px}}
button.all{{height:30px;padding:0 10px;margin:0 6px 6px 0;font-size:12px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>ひなたの声 その12　「おはようございます」を軸に固定して、元と同じ台詞で比べる</h1>
<p>見本はその9の「おはようございます」1本だけ（H02・H09それぞれ）。見本への寄せ方は少し強め。台詞はElevenLabsの元の声がある10行と同じ文。1行に3通り（1〜3）。<br>
「通しで」は1を10行続けて流す（元も同じ順で流せる）。機械の印：こもり？・読み？ ／ その9・その11の「元」の一部は別の台詞だったので、今回から同じ台詞にそろえた。</p></header>
<section>{anchors}<br>{orig_all}{alls}<table><tr><th>台詞</th><th>元</th>{''.join(f'<th>{v}</th>' for v in PICKS)}</tr>{rows}</table></section>
<script>let a=null,q=[];function play(btn,src){{q=[];if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}
function playAll(btn,list){{if(a)a.pause();q=list.slice();btn.classList.add('on');step(btn)}}function step(btn){{if(!q.length){{btn.classList.remove('on');return}}a=new Audio(q.shift());a.onended=()=>setTimeout(()=>step(btn),300);a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--page' not in sys.argv:
        generate()
        check()
    page()
