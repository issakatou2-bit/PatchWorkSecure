# 声の一覧 その8（10/2）：文学の一節を、大型モデルで作り直す
# その4の一節は、声に渡す文をひらがなと読点だらけにしたため、単語の高低が崩れていた（Docs/Voice/Irodori-Direction-Rules.md）。
# 直し方：①漢字かな交じりのまま、読み違える語だけかな ②文の切れ目で分け、1文を途中で切らない ③朗読の指示と📖
#         ④1区切りにつき2通り作り、「通話のような音」「読み違い」を機械で外して良い方をつなぐ
# 3人とも大型（v4-Large、量子化版）。ひなたは2通り：今の見本（ElevenLabsの声。私的な試しだけ）と、公開できるSA1の声。
# 使い方：.../.venv/Scripts/python.exe Tools/Irodori-Lit8.py
# 出力：Artifacts/VoiceAudition/v8/（gitで追跡しない）。本文は青空文庫（著作権の切れた作品）。
import difflib, html, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v8'
VA = ROOT / 'Artifacts' / 'VoiceAudition'
REFS = ROOT / 'ArtSource/Voice/Irodori-refs'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODEL = 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'
SEEDS = (20261041, 20261042)
GAP = 0.4
READ = '近いマイクで録った、こもりのないクリアな音質。読み上げる文の意味を分かって、文のまとまりごとに自然な抑揚で読む。'
VOICES = {
    'kanon': dict(name='かのん', refs=[REFS / 'secretary-ref.wav'], cap='落ち着いた大人の女性が、しみじみと、物語を読むように朗読する。'),
    'lily': dict(name='りりぃ', refs=[REFS / 'engineer-ref.wav'], cap='若い女性が、静かに、淡々と、語り聞かせるように朗読する。'),
    'hinata-el': dict(name='ひなた（今の見本・私的）', refs=[VA / 'v4' / 'refs' / 'hinata-ref-long.wav'], cap='明るい若い女性が、子どもに読み聞かせるように、表情豊かに、やさしく朗読する。'),
    'hinata-sa1': dict(name='ひなた（SA1・公開可）', refs=[VA / 'v5' / 'audio' / f'SA1-{r}.wav' for r in ('maxim_backup', 'mg_end_good', 'peak_goal_06')],
                       cap='明るい若い女性が、子どもに読み聞かせるように、表情豊かに、やさしく朗読する。'),
}
# （出典, 字幕の原文, 声に渡す区切り）
LIT = {
    'kanon': ('夏目漱石『草枕』',
              '山路を登りながら、こう考えた。智に働けば角が立つ。情に棹させば流される。意地を通せば窮屈だ。とかくに人の世は住みにくい。住みにくさが高じると、安い所へ引き越したくなる。どこへ越しても住みにくいと悟った時、詩が生れて、画が出来る。人の世を作ったものは神でもなければ鬼でもない。やはり向う三軒両隣りにちらちらするただの人である。ただの人が作った人の世が住みにくいからとて、越す国はあるまい。あれば人でなしの国へ行くばかりだ。人でなしの国は人の世よりもなお住みにくかろう。',
              ['山道を登りながら、こう考えた。', 'ちに働けば、かどが立つ。じょうに棹させば流される。', '意地を通せば窮屈だ。とかくに人の世は住みにくい。',
               '住みにくさが高じると、安い所へ引き越したくなる。', 'どこへ越しても住みにくいと悟った時、詩が生まれて、絵が出来る。',
               '人の世を作ったものは、神でもなければ鬼でもない。', 'やはり向こう三軒両隣にちらちらする、ただの人である。',
               'ただの人が作った人の世が住みにくいからとて、越す国はあるまい。', 'あれば、人でなしの国へ行くばかりだ。', '人でなしの国は、人の世よりも、なお住みにくかろう。']),
    'lily': ('芥川龍之介『蜘蛛の糸』',
             'ある日の事でございます。御釈迦様は極楽の蓮池のふちを、独りでぶらぶら御歩きになっていらっしゃいました。池の中に咲いている蓮の花は、みんな玉のようにまっ白で、そのまん中にある金色の蕊からは、何とも云えない好い匂が、絶間なくあたりへ溢れて居ります。極楽は丁度朝なのでございましょう。やがて御釈迦様はその池のふちに御佇みになって、水の面を蔽っている蓮の葉の間から、ふと下の容子を御覧になりました。この極楽の蓮池の下は、丁度地獄の底に当って居りますから、水晶のような水を透き徹して、三途の河や針の山の景色が、丁度覗き眼鏡を見るように、はっきりと見えるのでございます。',
             ['ある日のことでございます。', 'お釈迦様は極楽の蓮池のふちを、独りでぶらぶらお歩きになっていらっしゃいました。',
              '池の中に咲いている蓮の花は、みんな玉のように真っ白で、', 'そのまん中にある金色のずいからは、何とも言えない良い匂いが、絶え間なくあたりへあふれております。',
              '極楽はちょうど朝なのでございましょう。', 'やがてお釈迦様は、その池のふちにおたたずみになって、',
              '水のおもてを覆っている蓮の葉の間から、ふと下の様子をご覧になりました。', 'この極楽の蓮池の下は、ちょうど地獄の底に当たっておりますから、',
              '水晶のような水を透き通して、三途の川や針の山の景色が、', 'ちょうどのぞき眼鏡を見るように、はっきりと見えるのでございます。']),
    'hinata': ('新美南吉『手袋を買いに』',
               '寒い冬が北方から、狐の親子の棲んでいる森へもやって来ました。或朝洞穴から子供の狐が出ようとしましたが、「あっ」と叫んで眼を抑えながら母さん狐のところへころげて来ました。「母ちゃん、眼に何か刺さった、ぬいて頂戴早く早く」と言いました。母さん狐がびっくりして、あわてふためきながら、眼を抑えている子供の手を恐る恐るとりのけて見ましたが、何も刺さってはいませんでした。母さん狐は洞穴の入口から外へ出て始めてわけが解りました。昨夜のうちに、真白な雪がどっさり降ったのです。その雪の上からお陽さまがキラキラと照していたので、雪は眩しいほど反射していたのです。雪を知らなかった子供の狐は、あまり強い反射をうけたので、眼に何か刺さったと思ったのでした。',
               ['寒い冬が北方から、狐の親子の住んでいる森へもやって来ました。', 'ある朝、ほら穴から子供の狐が出ようとしましたが、',
                '「あっ」と叫んで目を押さえながら、かあさんぎつねのところへころげて来ました。', '「かあちゃん、目に何か刺さった、抜いてちょうだい、早く早く」と言いました。',
                'かあさんぎつねがびっくりして、あわてふためきながら、', '目を押さえている子供の手を、恐る恐る取りのけて見ましたが、何も刺さってはいませんでした。',
                'かあさんぎつねは、ほら穴の入口から外へ出て、初めてわけが分かりました。', 'ゆうべのうちに、真っ白な雪がどっさり降ったのです。',
                'その雪の上からお日さまがキラキラと照らしていたので、雪はまぶしいほど反射していたのです。', '雪を知らなかった子供の狐は、あまり強い反射を受けたので、目に何か刺さったと思ったのでした。']),
}
JOBS = [('kanon', 'kanon'), ('lily', 'lily'), ('hinata-el', 'hinata'), ('hinata-sa1', 'hinata')]


def generate():
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    for vid, lit in JOBS:
        v = VOICES[vid]
        for j, chunk in enumerate(LIT[lit][2]):
            for k, seed in enumerate(SEEDS):
                out = OUT / 'tmp' / f'{vid}-{j:02d}-{k}.wav'
                if out.exists():
                    continue
                out.parent.mkdir(parents=True, exist_ok=True)
                sys.argv = ['infer.py', '--hf-checkpoint', MODEL, '--model-precision', 'bf16', '--text', '📖' + chunk, '--caption', v['cap'] + READ,
                            '--ref-wavs', *map(str, v['refs']), '--cfg-scale-speaker', '3', '--cfg-scale-caption', '5', '--seed', str(seed), '--output-wav', str(out)]
                infer.main()


def band(d, sr):
    import numpy as np
    spec = np.abs(np.fft.rfft(d)) ** 2
    f = np.fft.rfftfreq(len(d), 1 / sr)
    return float(spec[f > 4000].sum() / max(spec.sum(), 1e-9))


def norm(s):
    return ''.join(c for c in s if c.isalnum())


def assemble():
    # 区切りごとに、こもっていない方・書き起こしが台詞に近い方を選んでつなぐ
    import os, numpy as np, soundfile as sf, torch
    os.add_dll_directory(os.path.join(os.path.dirname(torch.__file__), 'lib'))
    os.environ['PATH'] = os.path.join(os.path.dirname(torch.__file__), 'lib') + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    m = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16')
    report = {}
    for vid, lit in JOBS:
        pieces, sr, picks = [], None, []
        for j, chunk in enumerate(LIT[lit][2]):
            best = None
            for k in range(len(SEEDS)):
                p = OUT / 'tmp' / f'{vid}-{j:02d}-{k}.wav'
                d, s = sf.read(p)
                d = d.mean(1) if d.ndim > 1 else d
                text = ''.join(x.text for x in m.transcribe(str(p), language='ja', beam_size=5)[0])
                score = difflib.SequenceMatcher(None, norm(text), norm(chunk)).ratio() + (0 if band(d, s) > 0.004 else -1)
                if best is None or score > best[0]:
                    best = (score, d, s, k, text)
            _, d, s, k, text = best
            sr = sr or s
            pieces += [d, np.zeros(int(sr * GAP))]
            picks.append({'chunk': chunk, 'take': k + 1, 'heard': text})
        out = OUT / 'audio' / f'lit-{vid}.wav'
        out.parent.mkdir(parents=True, exist_ok=True)
        sf.write(out, np.concatenate(pieces[:-1]), sr)
        report[vid] = picks
    (OUT / 'picks.json').write_text(json.dumps(report, ensure_ascii=False, indent=1), encoding='utf-8')


def page():
    rep = json.loads((OUT / 'picks.json').read_text(encoding='utf-8')) if (OUT / 'picks.json').exists() else {}
    (OUT / 'v4').mkdir(parents=True, exist_ok=True)
    old = {'kanon': 'lit-kanon.wav', 'lily': 'lit-eng.wav', 'hinata': 'lit-hinata.wav'}
    for f in old.values():
        if (VA / 'v4' / 'audio' / f).exists():
            shutil.copy(VA / 'v4' / 'audio' / f, OUT / 'v4' / f)

    def btn(src, label='▶', cls=''):
        return f'<button class="sm {cls}" onclick="play(this,&quot;{src}&quot;)">{label}</button>'
    body = ''
    for lit, (src, text, _) in LIT.items():
        rows = f'<div class="row"><span class="tag o">前（その4）</span>{btn("v4/" + old[lit], cls="o")}</div>'
        for vid, l2 in JOBS:
            if l2 != lit:
                continue
            heard = ''.join(f'<div class="heard">{i + 1}（{p["take"]}通り目）：{html.escape(p["heard"])}</div>' for i, p in enumerate(rep.get(vid, [])))
            rows += f'<div class="row"><span class="tag">{html.escape(VOICES[vid]["name"])}</span>{btn(f"audio/lit-{vid}.wav")}</div><details><summary>区切りごとの書き起こし</summary>{heard}</details>'
        body += f'<section><h2>{html.escape(src)}</h2><div class="txt">{html.escape(text)}</div>{rows}</section>'
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>文学の一節 その8</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12)}}
h2{{font-family:'M PLUS Rounded 1c';margin:0 0 6px;font-size:22px}} .txt{{font-size:14px;line-height:1.8;margin-bottom:8px}}
.row{{display:flex;align-items:center;gap:10px;padding:6px 0;border-top:1px solid #eef1f6;font-weight:700}}
.tag{{display:inline-block;min-width:190px;border-radius:10px;padding:4px 10px;color:#fff;background:#ff6f91;font-family:'M PLUS Rounded 1c';font-size:14px}} .tag.o{{background:#8a97b0}}
details{{font-size:12px;color:#8a8399;margin:0 0 6px}} .heard{{line-height:1.6}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:13px}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>文学の一節 その8　大型モデルで作り直し</h1>
<p>3人とも大型（v4-Large・量子化版）。声に渡す文は漢字かな交じりのまま（読み違える語だけかな）、1文を途中で切らずに区切った。朗読の指示と📖つき。<br>
区切りごとに2通り作り、通話のような音と読み違いを機械で外して、良い方をつないだ。「前」はその4（ひらがなだらけで作った版）。</p></header>
{body}
<script>let a=null;function play(btn,src){{if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--page' not in sys.argv:
        generate()
        assemble()
    page()
