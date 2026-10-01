# Irodori-TTS（ローカル・無料）で、秘書さんとエンジニアさんの声の候補を作り、キャラの1枚絵つきの試聴ページを書き出す。
# 使い方（Irodori-TTSの仮想環境のPythonで実行）:
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-Audition.py [--model small|large] [--only-page]
#   （小さいモデルと大きいモデルは別々に実行する）
# 出力：Artifacts/VoiceAudition/（gitでは追跡しない）。index.html をブラウザで開くと聞き比べられる。
# 声の性格は文字（キャプション）だけで作る。実在の人の声を手本にしない（Irodoriの利用条件）。
import html, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {
    'small': ('v4.1-Small（MIT）', 'Aratako/Irodori-TTS-v4.1-Small'),
    'large': ('v4-Large int8（Gemmaの利用規約）', 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'),
}
CHARS = [
    {'id': 'secretary', 'name': '秘書さん', 'art': 'focus-secretary.png',
     'about': '黒髪のお姉さん系。落ち着いて面倒見がいいが、たまに辛口。いたずらっぽい',
     'captions': [
         '落ち着いた大人の女性。少し低めで艶のある声。親しい後輩に、余裕のある笑みを浮かべながら、いたずらっぽく話している。',
         '上品で柔らかい大人の女性の声。面倒見がよく、ゆったりとした丁寧な話し方。ときどき、からかうように笑う。',
         'はっきりした発声の、仕事ができる女性秘書。少し早口で、さばさばと辛口に話すが、根は優しい。',
         '近い距離感で話す、余裕のある大人の女性。ゆっくりと、楽しそうに相手をからかう。',
     ],
     'lines': [
         '数字は、入れたら何が減るかで語るのよ。止まる時間、失う売上。',
         '全部禁止すると、みんな隠れて使うわ。使っていい道を作るの。',
         'ふふっ、社長にはわたしから話しておくわ。……貸し、ひとつね？',
     ]},
    {'id': 'engineer', 'name': 'エンジニアさん', 'art': 'focus-engineer.png',
     'about': '金髪でクール、でも少し抜けている。眠たげ。ポテトが好き',
     'captions': [
         '若い女性。眠たげで低めの落ち着いた声。淡々と、抑揚少なめに話すが、どこか優しい。',
         'クールで中性的な若い女性の声。ぼそっと短く話し、ときどき気の抜けた間がある。',
         '少しかすれた、けだるい雰囲気の女性。マイペースでゆっくり、ぼんやりした話し方。',
         '落ち着いた知的な若い女性。静かではっきりと、専門的なことを淡々と説明する。',
     ],
     'lines': [
         '……ん、見つけた。同じところから、ログイン失敗が四十回。',
         '私も、一回落ちた。……だから、大丈夫。',
         'いつもを知らないと、いつもと違うは分からない。……ポテト、食べる？',
     ]},
]
SEED = 20261001


def jobs():
    for c in CHARS:
        for ci, cap in enumerate(c['captions']):
            for li, line in enumerate(c['lines']):
                yield 'small', c, ci, li, cap, line
            # 話題の大きいモデルは、各候補の1行目だけで聞き比べる
            yield 'large', c, ci, 0, cap, c['lines'][0]


def wav_name(model, c, ci, li):
    return f"{c['id']}-{model}-c{ci + 1}-l{li + 1}.wav"


def generate():
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached  # 同じモデルを1回だけ読み込む
    done = 0
    # 同じ処理の中でモデルを切り替えると落ちる（Windows、v4-Large int8）。--model でモデルごとに分けて実行する
    want = sys.argv[sys.argv.index('--model') + 1] if '--model' in sys.argv else 'small'
    for model, c, ci, li, cap, line in jobs():
        if model != want:
            continue
        out = OUT / 'audio' / wav_name(model, c, ci, li)
        if out.exists():
            continue
        sys.argv = ['infer.py', '--hf-checkpoint', MODELS[model][1], '--model-precision', 'bf16',
                    '--text', line, '--caption', cap, '--no-ref', '--seed', str(SEED + ci), '--output-wav', str(out)]
        infer.main()
        done += 1
    print('生成', done, '本')


def page():
    import shutil
    (OUT / 'art').mkdir(exist_ok=True)
    for c in CHARS:
        shutil.copy(ROOT / 'Assets' / 'Art' / 'KeyVisual' / c['art'], OUT / 'art' / c['art'])
    def player(model, c, ci, li):
        f = wav_name(model, c, ci, li)
        if not (OUT / 'audio' / f).exists():
            return '<span class="miss">未生成</span>'
        return f'<button onclick="play(this,\'audio/{f}\')">▶</button>'
    cards = []
    for c in CHARS:
        rows = []
        for ci, cap in enumerate(c['captions']):
            cells = ''.join(f'<td>{player("small", c, ci, li)}</td>' for li in range(len(c['lines'])))
            rows.append(f'<tr><th>候補{ci + 1}</th><td class="cap">{html.escape(cap)}</td>{cells}<td class="lg">{player("large", c, ci, 0)}</td>'
                        f'<td><label><input type="radio" name="{c["id"]}" value="{ci + 1}"> これ</label></td></tr>')
        heads = ''.join(f'<th class="ln">{html.escape(l)}</th>' for l in c['lines'])
        cards.append(f'''<section class="char"><img src="art/{c['art']}" alt="">
<div class="body"><h2>{c['name']}</h2><p class="about">{html.escape(c['about'])}</p>
<table><tr><th></th><th>声の性格（キャプション）</th>{heads}<th class="lg">話題の大型モデルで1行目</th><th></th></tr>{''.join(rows)}</table></div></section>''')
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>声の試聴</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:28px 40px 6px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:34px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700}}
.char{{display:flex;min-width:1180px;gap:24px;margin:22px 40px;background:#fff;border-radius:24px;box-shadow:0 0 0 3px #fff,0 14px 34px rgba(27,35,64,.14);overflow:hidden}}
.char img{{width:300px;object-fit:cover;object-position:50% 15%}}
.body{{padding:20px 24px 22px 0;flex:1}} h2{{font-family:'M PLUS Rounded 1c';margin:0;font-size:28px}} .about{{margin:4px 0 12px;color:#6b7894;font-weight:700}}
table{{border-collapse:collapse;width:100%;font-size:14px}} th,td{{padding:8px 6px;border-bottom:1px solid #eef1f6;text-align:center;vertical-align:middle}}
th{{color:#6b7894;font-weight:700}} .cap{{text-align:left;max-width:330px;line-height:1.5}} .ln{{font-size:12px;max-width:170px;line-height:1.4}} .lg{{background:#fff8e6}}
button{{width:46px;height:46px;border:0;border-radius:14px;cursor:pointer;font-size:18px;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c}}
button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}} .miss{{color:#b5bccb;font-size:12px}}
</style></head><body><header><h1>秘書さん・エンジニアさんの声の試聴</h1>
<p>Irodori-TTS（ローカル・無料）。声は文字の説明だけで作った候補です。気に入った候補に「これ」を付けてください（この画面の中だけの印です）。</p></header>
{''.join(cards)}
<script>let a=null,b=null;function play(btn,src){{if(a){{a.pause();b&&b.classList.remove('on')}}a=new Audio(src);b=btn;btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('試聴ページ', OUT / 'index.html')


if __name__ == '__main__':
    (OUT / 'audio').mkdir(parents=True, exist_ok=True)
    if '--only-page' not in sys.argv:
        generate()
    page()
