# 声の一覧 その7（10/2）：ひなたの声を安定させる
# 加藤さんの感想（その5）：下の3つ（LD2・SA1・SB1）がまだ良いが、急に変になる・通話のような音質になる・安定しない。
# 安定させる方法：①気に入った候補の3本をつないで見本（--ref-wavs）にし、声を固定する（かのん・りりぃと同じ）
#                 ②会話の掛け合いで評判の良かった設定（演技の指示＋絵文字、見本への寄せ方を弱める、小型v4.1）
#                 ③1行につき3通り作り、機械で「読み違い」「通話のような音質」に印を付ける（最後は人の耳で選ぶ）
# 使い方：.../.venv/Scripts/python.exe Tools/Irodori-Hinata7.py   … 生成→書き起こし→一覧
# 出力：Artifacts/VoiceAudition/v7/（gitで追跡しない）。見本は文字の説明だけで作った声なので、公開に使える。
import difflib, html, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v7'
V5 = ROOT / 'Artifacts' / 'VoiceAudition' / 'v5' / 'audio'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODEL = 'Aratako/Irodori-TTS-v4.1-Small'
SEEDS = (20261031, 20261032, 20261033)
REF_IDS = ('maxim_backup', 'mg_end_good', 'peak_goal_06')
VOICES = ['LD2', 'SA1', 'SB1']
BASE = '明るく元気な若い女性。少し高めでかわいらしい声。近いマイクで録った、こもりのないクリアな音質。'
TALK = '情シスの後輩が、職場の先輩に話しかけている。読み上げではなく、目の前の相手との会話。'
# （id, 字幕, 声に渡す文, 演技の指示）。声に渡す文は漢字かな交じりのまま。読み違える語だけかなにする
LINES = [
    ('greet', 'おはようございます、先輩！　今日も、なにごともない一日にしようね！', 'おはようございます、先輩！😊　きょうも、なにごともない一日にしようね！',
     '朝、出社してきた先輩に、明るく元気にあいさつする。'),
    ('meeting', '今日だよ！　しかも、りりぃさんの発表の日だよ！', 'きょうだよ！💥　しかも、りりぃさんの発表の日だよ！',
     '横から身を乗り出して、あわてつつも楽しそうに教える。'),
    ('maxim', 'バックアップは、戻せてこそバックアップ、だよ！', 'バックアップは、戻せてこそバックアップ、だよ！😎',
     '人差し指を立てて、得意げに、でもかわいらしく教える。'),
    ('worry', 'えっ、ちょっと待って……このメール、送り主がおかしいかも。', 'えっ😲、ちょっと待って……😟このメール、送り主がおかしいかも。',
     '画面をのぞきこんで気づき、小声になって心配そうに言う。'),
    ('diary', '試験に落ちた。悔しくて、帰り道で泣いた。', '📖試験に落ちた。悔しくて、帰り道で泣いた。',
     '夜、日記を読み返すように、しんみりと静かに独り言を言う。'),
    ('praise', '完璧〜っ！　プロの仕事だね！', 'かんぺき〜っ！😆　プロの仕事だね！',
     '先輩の仕事を見て、心から感心して、弾むようにほめる。'),
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
    done = 0
    for v in VOICES:
        refs = [str(V5 / f'{v}-{r}.wav') for r in REF_IDS]
        for lid, _, text, direction in LINES:
            for k, seed in enumerate(SEEDS):
                out = OUT / 'audio' / f'{v}-{lid}-{k + 1}.wav'
                if out.exists():
                    continue
                out.parent.mkdir(parents=True, exist_ok=True)
                sys.argv = ['infer.py', '--hf-checkpoint', MODEL, '--model-precision', 'bf16', '--text', text, '--caption', BASE + TALK + direction,
                            '--ref-wavs', *refs, '--cfg-scale-speaker', '3', '--cfg-scale-caption', '5', '--seed', str(seed), '--output-wav', str(out)]
                infer.main()
                done += 1
    print('生成', done, '本')


def band(path):
    # 4kHzより上の音の割合（通話の音質は3.4kHzあたりで切れるので、この割合がとても小さくなる）
    import numpy as np, soundfile as sf
    d, sr = sf.read(path)
    d = d.mean(1) if d.ndim > 1 else d
    spec = np.abs(np.fft.rfft(d)) ** 2
    f = np.fft.rfftfreq(len(d), 1 / sr)
    return float(spec[f > 4000].sum() / max(spec.sum(), 1e-9))


def check():
    import os, torch
    os.add_dll_directory(os.path.join(os.path.dirname(torch.__file__), 'lib'))
    os.environ['PATH'] = os.path.join(os.path.dirname(torch.__file__), 'lib') + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    m = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16')
    res = {}
    for p in sorted((OUT / 'audio').glob('*.wav')):
        res[p.name] = {'text': ''.join(s.text for s in m.transcribe(str(p), language='ja', beam_size=5)[0]), 'band': band(p)}
    (OUT / 'checks.json').write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding='utf-8')
    print('確認', len(res), '本')


def norm(s):
    return ''.join(c for c in s if c.isalnum())


def page():
    ck = json.loads((OUT / 'checks.json').read_text(encoding='utf-8')) if (OUT / 'checks.json').exists() else {}
    bands = sorted(c['band'] for c in ck.values()) or [0]
    low = bands[len(bands) // 2] * 0.35  # 中央値の35%未満なら「こもり・通話の音」の疑い

    def cell(v, lid, sub):
        out = ''
        for k in range(len(SEEDS)):
            name = f'{v}-{lid}-{k + 1}.wav'
            c = ck.get(name)
            flags = []
            if c:
                if difflib.SequenceMatcher(None, norm(c['text']), norm(sub)).ratio() < 0.6:
                    flags.append('読み？')
                if c['band'] < low:
                    flags.append('こもり？')
            title = html.escape(c['text']) if c else ''
            mark = ''.join(f'<i>{f}</i>' for f in flags)
            out += f'<span class="take"><button class="sm" title="{title}" onclick="play(this,&quot;audio/{name}&quot;)">{k + 1}</button>{mark}</span>'
        return f'<td>{out}</td>'
    rows = ''.join(f'<tr><td><b>{html.escape(sub)}</b><div class="dir">演技：{html.escape(d)}</div></td>' + ''.join(cell(v, lid, sub) for v in VOICES) + '</tr>'
                   for lid, sub, _, d in LINES)
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>ひなたの声 その7</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:8px;text-align:left;font-weight:700;vertical-align:top}} th{{font-size:14px;color:#52607a}}
.dir{{font-size:12px;color:#6a5200;font-weight:500;margin-top:2px}} .take{{display:inline-flex;flex-direction:column;align-items:center;margin-right:6px}}
i{{font-style:normal;font-size:11px;color:#c9486c;font-weight:700}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:14px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>ひなたの声 その7　声を固定して安定させる</h1>
<p>その5の下の3つ（LD2・SA1・SB1）を、それぞれ3本つないで見本にし、声を固定した。設定は掛け合いで良かったもの（演技の指示＋絵文字、見本への寄せ方を弱める、小型v4.1）。<br>
1行につき3通り（1〜3）。機械の印：「読み？」＝書き起こしが台詞と大きく違う、「こもり？」＝高い音が少なく通話のような音の疑い。印が無くても、最後は耳で選ぶ。ボタンに指を置くと書き起こしが出る。</p></header>
<section><table><tr><th>台詞</th>{''.join(f'<th>{v}の声</th>' for v in VOICES)}</tr>{rows}</table></section>
<script>let a=null;function play(btn,src){{if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--page' not in sys.argv:
        if '--check' not in sys.argv:
            generate()
        check()
    page()
