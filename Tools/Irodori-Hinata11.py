# 声の一覧 その11（10/3）：その9の候補を絞って、声を固定して聞き比べる
# 加藤さんの感想（その9）：H01・H02・H06・H08・H09・H16が良い（H03・H05はあざとい、H07・H15は落ち着きすぎ、H11・H13はイメージと違う）。
# 「2つの台詞で声がぶれる」：その9は見本なし（文字の説明だけ）で1行ずつ作ったので、行ごとに声が少し変わる。
# 直し方：候補ごとに、その9の2本（どちらもこもり無しを確認済み）を見本にして声を固定し、同じ設定で6行を2通りずつ作る。
#         幼く聞こえないよう、説明に「社会人2年目」を入れる。こもり・読み違いは機械で印を付ける（その7と同じ）。
# 使い方：.../.venv/Scripts/python.exe Tools/Irodori-Hinata11.py
# 出力：Artifacts/VoiceAudition/v11/（gitで追跡しない）。見本は文字の説明だけで作った声なので、公開に使える。
import difflib, html, importlib.util, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v11'
V9 = ROOT / 'Artifacts' / 'VoiceAudition' / 'v9' / 'audio'
HINATA = ROOT / 'Assets/Audio/CompanyYear/VoiceTest/Hinata'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODEL = 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'
SEEDS = (20261111, 20261112)
PICKS = ['H01', 'H02', 'H06', 'H08', 'H09', 'H16']
BASE = '社会人2年目の、明るく元気な女性。人懐っこく、ほがらかで、やわらかく甘さのある高めの声。近いマイクで録った、こもりのないクリアな音質。'
TALK = '情シスの後輩が、職場の先輩に話しかけている。読み上げではなく、目の前の相手との会話。'
# （id, 字幕, 声に渡す文, 演技の指示, 元の声のid（無ければ空））
LINES = [
    ('greet', 'おはようございます、先輩！　今日も、なにごともない一日にしようね！', 'おはようございます、先輩！😊　きょうも、なにごともない一日にしようね！',
     '朝、出社してきた先輩に、明るく元気にあいさつする。', 'tutorial_1'),
    ('maxim', 'バックアップは、戻せてこそバックアップ、だよ！', 'バックアップは、戻せてこそバックアップ、だよ！😎',
     '人差し指を立てて、得意げに、でもかわいらしく教える。', 'maxim_backup'),
    ('peak', '今月は最初の山場だよ！　目標、ちゃんと見ておいてね！', 'こんげつは最初の山場だよ！💪　目標、ちゃんと見ておいてね！',
     '気合いを入れて、先輩の背中を押すように言う。', 'peak_goal_06'),
    ('worry', 'えっ、ちょっと待って……このメール、送り主がおかしいかも。', 'えっ😲、ちょっと待って……このメール、送り主がおかしいかも。',
     '画面をのぞきこんで気づき、声を落として心配そうに言う。', ''),
    ('praise', '完璧〜っ！　プロの仕事だね！', 'かんぺき〜っ！😆　プロの仕事だね！',
     '先輩の仕事を見て、心から感心して、弾むようにほめる。', 'mg_end_good'),
    ('diary', '試験に落ちた。悔しくて、帰り道で泣いた。', '試験に落ちた。悔しくて、帰り道で泣いた。',
     '夜、ひとりで日記を書きながら、しんみりと、静かに、少し声を落として言う。', ''),
]


def h9():
    spec = importlib.util.spec_from_file_location('h9', ROOT / 'Tools' / 'Irodori-Hinata9.py')
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


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
        refs = [str(V9 / f'{v}-{x}.wav') for x in ('greet', 'maxim')]
        for lid, _, text, direction, _ in LINES:
            for k, seed in enumerate(SEEDS):
                out = OUT / 'audio' / f'{v}-{lid}-{k + 1}.wav'
                if out.exists():
                    continue
                out.parent.mkdir(parents=True, exist_ok=True)
                sys.argv = ['infer.py', '--hf-checkpoint', MODEL, '--model-precision', 'bf16', '--text', text, '--caption', BASE + TALK + direction,
                            '--ref-wavs', *refs, '--cfg-scale-speaker', '4', '--cfg-scale-caption', '5', '--seed', str(seed), '--output-wav', str(out)]
                infer.main()


def norm(s):
    return ''.join(c for c in s if c.isalnum())


def check():
    import os, torch
    os.add_dll_directory(os.path.join(os.path.dirname(torch.__file__), 'lib'))
    os.environ['PATH'] = os.path.join(os.path.dirname(torch.__file__), 'lib') + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    m, mf = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16'), h9().muffled
    res = {}
    for p in sorted((OUT / 'audio').glob('*.wav')):
        res[p.name] = {'text': ''.join(s.text for s in m.transcribe(str(p), language='ja', beam_size=5)[0]), 'muffled': mf(p)[0]}
    (OUT / 'checks.json').write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding='utf-8')


def page():
    ck = json.loads((OUT / 'checks.json').read_text(encoding='utf-8')) if (OUT / 'checks.json').exists() else {}
    (OUT / 'orig').mkdir(parents=True, exist_ok=True)
    for *_, rid in LINES:
        if rid and (HINATA / f'{rid}.mp3').exists():
            shutil.copy(HINATA / f'{rid}.mp3', OUT / 'orig' / f'{rid}.mp3')

    def take(v, lid, sub):
        out = ''
        for k in range(len(SEEDS)):
            name = f'{v}-{lid}-{k + 1}.wav'
            c = ck.get(name, {})
            marks = ('<i>こもり？</i>' if c.get('muffled') else '') + ('<i>読み？</i>' if c and difflib.SequenceMatcher(None, norm(c['text']), norm(sub)).ratio() < .6 else '')
            out += f'<span class="take"><button class="sm" title="{html.escape(c.get("text", ""))}" onclick="play(this,&quot;audio/{name}&quot;)">{k + 1}</button>{marks}</span>'
        return f'<td>{out}</td>'
    head = '<th>台詞</th><th>元</th>' + ''.join(f'<th>{v}</th>' for v in PICKS)
    rows = ''
    for lid, sub, _, d, rid in LINES:
        orig = f'<button class="sm o" onclick="play(this,&quot;orig/{rid}.mp3&quot;)">元</button>' if rid else '<small>なし</small>'
        rows += f'<tr><td><b>{html.escape(sub)}</b><div class="dir">演技：{html.escape(d)}</div></td><td>{orig}</td>' + ''.join(take(v, lid, sub) for v in PICKS) + '</tr>'
    allv = ''.join(f'<button class="all" onclick="playAll(this,{html.escape(json.dumps([f"audio/{v}-{l[0]}-1.wav" for l in LINES]))})">{v}を通しで</button>' for v in PICKS)
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>ひなたの声 その11</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:22px 28px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:28px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7;font-size:14px}}
section{{margin:14px 28px;background:#fff;border-radius:22px;padding:14px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:7px 5px;text-align:left;font-weight:700;vertical-align:top}} th{{font-size:13px;color:#52607a}}
.dir{{font-size:12px;color:#6a5200;font-weight:500}} .take{{display:inline-flex;flex-direction:column;align-items:center;margin-right:4px}} i{{font-style:normal;font-size:10px;color:#c9486c}} small{{color:#8a8399}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:32px;height:32px;font-size:13px;font-family:'M PLUS Rounded 1c'}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894;font-size:11px}}
button.all{{height:30px;padding:0 10px;margin:0 6px 6px 0;font-size:12px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>ひなたの声 その11　6つに絞って声を固定</h1>
<p>その9で良かった6つ（H01・H02・H06・H08・H09・H16）を、それぞれその9の2本（こもり無し）を見本にして声を固定した。説明に「社会人2年目」を入れて、幼くなりすぎないように。<br>
6行を2通りずつ（1・2）。「通しで」は各候補の1を6行続けて流す（声がぶれないかを聞く）。機械の印：こもり？・読み？</p></header>
<section>{allv}<table><tr>{head}</tr>{rows}</table></section>
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
