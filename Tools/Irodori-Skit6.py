# 声の一覧 その6（10/2）：掛け合いを「会話」として作り直す
# 加藤さんの指摘：単語ごとのイントネーションがおかしい、会話のトーン・テンションになっていない。
# 原因の見立て：①読みをひらがなと読点だらけにしたので、単語の切れ目と高低（アクセント）の手がかりが消えた
#               ②キャプションが声の説明だけで、「誰に・どんな気持ちで言うか」が無かった
# 直し方：a＝ふつうの漢字かな交じり（読み違える語だけかな）＋1行ごとの演技の指示
#         b＝aに、Irodoriの絵文字による話し方の指定を足したもの（対応するモデルで効く）
# 使い方（Irodori-TTSの仮想環境のPython。モデルごとに別々に実行する）:
#   .../.venv/Scripts/python.exe Tools/Irodori-Skit6.py --model large   … かのん・ひなた
#   .../.venv/Scripts/python.exe Tools/Irodori-Skit6.py --model small   … りりぃ
#   .../.venv/Scripts/python.exe Tools/Irodori-Skit6.py --check         … 書き起こし
# 出力：Artifacts/VoiceAudition/v6/（gitで追跡しない）。ひなたは今はElevenLabsの声を見本にした私的な試し（公開用の声が決まったら差し替え）。
import html, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v6'
V4 = ROOT / 'Artifacts' / 'VoiceAudition' / 'v4'
REFS = ROOT / 'ArtSource/Voice/Irodori-refs'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {'small': 'Aratako/Irodori-TTS-v4.1-Small', 'large': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'}
SEED = 20261013
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
TALK = '同僚どうしの、オフィスでのくだけた会話。読み上げではなく、目の前の相手に話しかけている。'
VOICE = {
    'kanon': dict(model='large', ref=REFS / 'secretary-ref.wav', extra=[], cap='仕事ができる大人の女性。'),
    'lily': dict(model='small', ref=REFS / 'engineer-ref.wav', extra=['--duration-scale', '0.85'], cap='若い女性。眠たげで低めの落ち着いた声。' + CLEAR),
    'hinata': dict(model='large', ref=V4 / 'refs' / 'hinata-ref-long.wav', extra=['--cfg-scale-speaker', '7'], cap='明るく元気な若い女性。' + CLEAR),
}
NAMES = {'kanon': 'かのん', 'lily': 'りりぃ', 'hinata': 'ひなた'}
# （話す人, 字幕, 声に渡す文a, 絵文字つきb, 演技の指示）
SKIT = [
    ('kanon', 'ほら、起きて。会議、五分前よ。', 'ほら、起きて。会議、五分前よ。', 'ほら、起きて🤭。会議、五分前よ。',
     '机で居眠りしている同僚の肩を軽く叩いて、小声で、少しからかうように起こす。'),
    ('lily', '……ん。会議って、今日だっけ。', '……ん。会議って、きょうだっけ。', '……ん😪。会議って、きょうだっけ。',
     '起こされたばかりで、まだ眠そうに、ぼんやりと聞き返す。'),
    ('hinata', '今日だよ！　しかも、りりぃさんの発表の日だよ！', 'きょうだよ！　しかも、りりぃさんの発表の日だよ！', 'きょうだよ！😆　しかも、りりぃさんの発表の日だよ！',
     '横から身を乗り出して、あわてつつも楽しそうに、勢いよく教える。'),
    ('lily', '……あ。資料、まだ半分。', '……あ。資料、まだ半分。', '……あ😮。資料、まだ半分。',
     '今気づいて、小さく驚く。声は大きくならず、ぽつりと言う。'),
    ('kanon', '半分あれば十分よ。数字のところは、わたしが補うわ。', '半分あれば十分よ。数字のところは、わたしが補うわ。', '半分あれば十分よ😌。数字のところは、わたしが補うわ。',
     '慌てる二人を落ち着かせるように、余裕のある笑みで、頼もしく言う。'),
    ('hinata', 'わたし、ログのグラフ、すぐ出せるよ！　毎晩見てるもん！', 'わたし、ログのグラフ、すぐ出せるよ！　毎晩見てるもん！', 'わたし、ログのグラフ、すぐ出せるよ！😊　毎晩見てるもん！',
     '役に立てるのがうれしくて、得意げに、張り切って言う。'),
    ('lily', '……助かる。じゃあ、侵入の経路から話す。', '……助かる。じゃあ、侵入の経路から話す。', '……助かる😌。じゃあ、侵入の経路から話す。',
     '二人に感謝して、少しだけやわらかく。後半は仕事の話になり、淡々と早めに。'),
    ('kanon', '結論から、三行で。社長は、長い話が苦手なの。', '結論から、さんぎょうで。社長は、長い話が苦手なの。', '結論から、さんぎょうで。社長は、長い話が苦手なの🤭。',
     '指を立てて念を押す。後半は内緒話のように声を落として、くすっと笑う。'),
    ('lily', '三行……。入口は保守ID。止めたのは一台だけ。被害は、なし。', 'さんぎょう……。入口は保守アイディー。止めたのは一台だけ。被害は、なし。', 'さんぎょう……🤔。入口は保守アイディー。止めたのは一台だけ。被害は、なし。',
     '少し考えてから、要点だけを短く、正確に、自信をもって言い切る。'),
    ('hinata', 'すごい、ほんとに三行！', 'すごい、ほんとにさんぎょう！', 'すごい😲、ほんとにさんぎょう！',
     '感心して、思わず声が弾む。'),
    ('kanon', 'ふふっ、上出来よ。……次は、遅刻しないでね。', 'ふふっ、上出来よ。……次は、遅刻しないでね。', 'ふふっ🤭、上出来よ。……次は、遅刻しないでね。',
     '満足そうに小さく笑ってほめ、最後はやさしく釘を刺す。'),
    ('lily', '……努力する。たぶん。いや、する。', '……努力する。たぶん。いや、する。', '……努力する。たぶん😅。いや、する。',
     '目をそらして小声で。「たぶん」で自信がなくなり、「いや、する」で少しだけ意地を見せる。'),
]
VARIANTS = ('a', 'b', 'c', 'd')
# c＝bの文で、見本への寄せ方を弱め（話者3）、演技の指示を強める（説明5）。d＝cと同じ設定を、3人とも新しい小型（v4.1-Small）で
LOOSE = ['--cfg-scale-speaker', '3', '--cfg-scale-caption', '5']


def text_of(var, ta, tb):
    return ta if var == 'a' else tb.replace('😅', '🫣')  # 😅は大型の絵文字の一覧に無いので、照れの🫣に


def setting(var, who):
    v = VOICE[who]
    if var in ('a', 'b'):
        return v['model'], v['extra']
    dur = ['--duration-scale', '0.85'] if who == 'lily' else []
    return ('small' if var == 'd' else v['model']), dur + LOOSE


def cap(who, direction):
    return VOICE[who]['cap'] + TALK + direction


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
    for i, (who, _, ta, tb, direction) in enumerate(SKIT):
        v = VOICE[who]
        for var in VARIANTS:
            m, extra = setting(var, who)
            if m != model:
                continue
            text = text_of(var, ta, tb)
            out = OUT / 'audio' / f'{var}-{i + 1:02d}.wav'
            if out.exists():
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[model], '--model-precision', 'bf16', '--text', text, '--caption', cap(who, direction),
                        '--ref-wav', str(v['ref']), '--seed', str(SEED), *extra, '--output-wav', str(out)]
            infer.main()
            done += 1
    print('生成', done, '本')


def check():
    import os, torch
    os.add_dll_directory(os.path.join(os.path.dirname(torch.__file__), 'lib'))
    os.environ['PATH'] = os.path.join(os.path.dirname(torch.__file__), 'lib') + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    m = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16')
    res = {}
    for p in sorted((OUT / 'audio').glob('*.wav')):
        res[p.name] = ''.join(s.text for s in m.transcribe(str(p), language='ja', beam_size=5)[0])
    (OUT / 'transcripts.json').write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding='utf-8')
    print('書き起こし', len(res), '本')


def page():
    tr = json.loads((OUT / 'transcripts.json').read_text(encoding='utf-8')) if (OUT / 'transcripts.json').exists() else {}
    (OUT / 'v4').mkdir(parents=True, exist_ok=True)
    for i in range(len(SKIT)):
        src = V4 / 'audio' / f'skit-{i + 1:02d}.wav'
        if src.exists():
            shutil.copy(src, OUT / 'v4' / src.name)

    def btn(src, cls=''):
        return f'<button class="sm {cls}" onclick="play(this,&quot;{src}&quot;)">▶</button>' if (OUT / src).exists() else '<span class="miss">未生成</span>'
    rows = ''
    for i, (who, text, _, _, direction) in enumerate(SKIT):
        n = f'{i + 1:02d}'
        heard = ''.join(f'<div class="heard">{var}の書き起こし：{html.escape(tr[f"{var}-{n}.wav"])}</div>' for var in VARIANTS if f'{var}-{n}.wav' in tr)
        rows += (f'<tr><td><span class="who {who}">{NAMES[who]}</span></td><td>{btn(f"v4/skit-{n}.wav", "o")}</td>' + ''.join(f'<td>{btn(f"audio/{var}-{n}.wav")}</td>' for var in VARIANTS) +
                 f'<td><div>{html.escape(text)}</div><div class="dir">演技：{html.escape(direction)}</div>{heard}</td></tr>')
    lists = {k: json.dumps([f'{d}/{p}-{i + 1:02d}.wav' if d == 'audio' else f'v4/skit-{i + 1:02d}.wav' for i in range(len(SKIT))]) for k, d, p in (('v4', 'v4', ''), *((var, 'audio', var) for var in VARIANTS))}
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>掛け合い その6</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12);overflow-x:auto}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:7px 6px;text-align:left;font-weight:700;vertical-align:top}} th{{font-size:13px;color:#52607a}}
.who{{display:inline-block;width:70px;border-radius:10px;padding:4px 6px;text-align:center;color:#fff;font-family:'M PLUS Rounded 1c';font-size:14px}}
.who.kanon{{background:#5b4b8a}} .who.lily{{background:#2f93dc}} .who.hinata{{background:#ff6f91}}
.dir{{font-size:12px;color:#6a5200;font-weight:500;margin-top:2px}} .heard{{font-size:12px;color:#8a8399;font-weight:500}} .miss{{color:#b9a;font-size:12px}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:13px}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894}}
button.all{{height:34px;padding:0 14px;font-size:13px;font-family:'M PLUS Rounded 1c';margin-right:8px}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>掛け合い その6　会話として作り直し</h1>
<p>前（その4）は、声に渡す文をひらがなと読点だらけにしていたため、単語の切れ目と高低の手がかりが消えていた。<br>
a：ふつうの漢字かな交じり（読み違える語だけかな）＋1行ごとの演技の指示。b：aに、絵文字による話し方の指定を足したもの（絵文字は字幕には出ない）。c：bの文で、見本の声への寄せ方を弱め、演技の指示を強めたもの。d：cと同じ設定を、3人とも新しい小型モデル（v4.1-Small）で。<br>
ひなたは今はElevenLabsの声を見本にした私的な試し（公開用の声が決まったら差し替え）。</p></header>
<section><button class="all" onclick="playAll(this,{html.escape(lists['v4'])})">前（その4）を通しで</button><button class="all" onclick="playAll(this,{html.escape(lists['a'])})">aを通しで</button><button class="all" onclick="playAll(this,{html.escape(lists['b'])})">bを通しで</button><button class="all" onclick="playAll(this,{html.escape(lists['c'])})">cを通しで</button><button class="all" onclick="playAll(this,{html.escape(lists['d'])})">dを通しで</button>
<table><tr><th>話す人</th><th>前</th><th>a</th><th>b</th><th>c</th><th>d</th><th>台詞</th></tr>{rows}</table></section>
<script>let a=null,q=[];function play(btn,src){{q=[];if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}
function playAll(btn,list){{if(a)a.pause();q=list.slice();btn.classList.add('on');step(btn)}}function step(btn){{if(!q.length){{btn.classList.remove('on');return}}a=new Audio(q.shift());a.onerror=a.onended=()=>setTimeout(()=>step(btn),250);a.play().catch(()=>step(btn))}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--check' in sys.argv:
        check()
    elif '--page' in sys.argv:
        pass
    else:
        generate(sys.argv[sys.argv.index('--model') + 1])
    page()
