# Irodori-TTS（ローカル・無料）で、秘書さんとエンジニアさんの声の候補を作り、キャラの1枚絵つきの試聴ページを書き出す。
# 使い方（Irodori-TTSの仮想環境のPythonで実行）:
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-Audition.py [--model small|large] [--lock|--round2] [--only-page]
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


# 声を固定する：気に入った1本を「声の見本」（参照音声）にして、同じ声で別の台詞を言わせる。
# 見本はこの道具で作った音声だけを使う（実在の人の声は使わない）。
EXTRA = {
    'secretary': ['また無理してるでしょ。顔に書いてあるわよ。', '予算、通ったわ。……ちゃんと結果で返してね？'],
    'engineer': ['……この時間の通信、いつもと違う。', 'おつかれ。……今日は、平和だったね。'],
}
LOCKS = [
    # (名前, キャラid, 見本のファイル, 使うキャプションの番号, 見本を選んだ理由)
    ('秘書さん3「数字は」', 'secretary', 'secretary-small-c3-l1.wav', 2, '加藤さん：3の「数字は」がかわいい'),
    ('秘書さん3「ふふっ」', 'secretary', 'secretary-small-c3-l3.wav', 2, '加藤さん：3の「ふふっ」もかわいい'),
    ('エンジニアさん1 大型', 'engineer', 'engineer-large-c1-l1.wav', 0, '加藤さん：1の大型がすごく良い'),
    ('エンジニアさん3 大型', 'engineer', 'engineer-large-c3-l1.wav', 2, '加藤さん：3は大型なら良いかも'),
]


def lock_lines(cid):
    c = next(x for x in CHARS if x['id'] == cid)
    return c, c['lines'] + EXTRA[cid]


def lock_name(li, k):
    return f"lock{k + 1}-l{li + 1}.wav"


def generate_locks(model):
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    done = 0
    for k, (name, cid, ref, ci, why) in enumerate(LOCKS):
        c, lines = lock_lines(cid)
        for li, line in enumerate(lines):
            out = OUT / 'audio' / model / lock_name(li, k)
            if out.exists():
                continue
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[model][1], '--model-precision', 'bf16', '--text', line,
                        '--caption', c['captions'][ci], '--ref-wav', str(OUT / 'audio' / ref), '--seed', str(SEED), '--output-wav', str(out)]
            infer.main()
            done += 1
    print('固定した声', done, '本')


def lock_section():
    rows = []
    for k, (name, cid, ref, ci, why) in enumerate(LOCKS):
        c, lines = lock_lines(cid)
        def cell(model, li):
            f = f'audio/{model}/{lock_name(li, k)}'
            return f'<button onclick="play(this,&quot;{f}&quot;)">▶</button>' if (OUT / f).exists() else '<span class="miss">未生成</span>'
        for model in ('large', 'small'):
            cells = ''.join(f'<td>{cell(model, li)}</td>' for li in range(len(lines)))
            label = MODELS[model][0].split('（')[0]
            rows.append(f'<tr><th>{html.escape(name) if model == "large" else ""}</th><td class="cap">{html.escape(why) if model == "large" else ""}</td>'
                        f'<td><button onclick="play(this,&quot;audio/{ref}&quot;)">見本</button></td><td class="md">{label}</td>{cells}</tr>')
    heads = lambda cid: ''.join(f'<th class="ln">{html.escape(l)}</th>' for l in lock_lines(cid)[1])
    return f'''<section class="char" style="display:block;padding:20px 24px"><h2>声を固定した版（見本の声のまま、5行を言わせる）</h2>
<p class="about">気に入った1本を「声の見本」にして作り直した。上の段が話題の大型モデル、下の段が小さいモデル。台詞の列：秘書さんは上2つ、エンジニアさんは下2つの見本。</p>
<table><tr><th></th><th>見本を選んだ理由</th><th>見本</th><th>モデル</th>{heads('secretary')}</tr>{''.join(rows[:4])}
<tr><th></th><th></th><th></th><th></th>{heads('engineer')}</tr>{''.join(rows[4:])}</table></section>'''


# 2回目の聞き比べ（10/1、加藤さんの感想を受けて）。見本の声を固定したまま、声の性格の文字を少しずつ変える。
SEC_CAP = CHARS[0]['captions'][2]
ENG_CAP = CHARS[1]['captions'][0]
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
ROUND2 = [
    # (まとまり, 名前, キャラid, 見本, キャプション, モデル)
    ('秘書さん：上段（大型）の派生', '派生1 そのまま', 'secretary', 'secretary-small-c3-l1.wav', SEC_CAP, 'large'),
    ('秘書さん：上段（大型）の派生', '派生2 少し若く、いたずらっぽく', 'secretary', 'secretary-small-c3-l1.wav', SEC_CAP + '20代後半。声を少し高めに、笑みを含んでいたずらっぽく。', 'large'),
    ('秘書さん：上段（大型）の派生', '派生3 やわらかく、面倒見よく', 'secretary', 'secretary-small-c3-l1.wav', SEC_CAP + '親しい後輩を甘やかすように、やわらかく。', 'large'),
    ('秘書さん：上段（大型）の派生', '派生4 テンポよく、楽しそうに', 'secretary', 'secretary-small-c3-l1.wav', SEC_CAP + 'テンポよく、楽しそうに話す。', 'large'),
    ('エンジニアさん：上段の見本で、くぐもりを晴らす', '派生1 クリアに', 'engineer', 'engineer-large-c1-l1.wav', ENG_CAP + CLEAR, 'small'),
    ('エンジニアさん：上段の見本で、くぐもりを晴らす', '派生2 クールで少し抜けている', 'engineer', 'engineer-large-c1-l1.wav', 'クールで淡々としているが、少し抜けていて、語尾がふわっと気の抜けた若い女性。' + CLEAR, 'small'),
    ('エンジニアさん：上段の見本で、くぐもりを晴らす', '派生3 眠たげだが明るめ、とぼけた感じ', 'engineer', 'engineer-large-c1-l1.wav', '眠たげだが声は明るめの若い女性。クールで、少しとぼけている。はっきりした発音。' + CLEAR, 'small'),
    ('エンジニアさん：上段の見本で、くぐもりを晴らす', '派生4 派生2を大型で', 'engineer', 'engineer-large-c1-l1.wav', 'クールで淡々としているが、少し抜けていて、語尾がふわっと気の抜けた若い女性。' + CLEAR, 'large'),
]
KEEP_DIR = ROOT / 'ArtSource' / 'Voice' / 'Irodori-keep'
for kfile, kname, kcid, kcap in [('keep1-secretary-small-c3-fufu.wav', 'キープ1 秘書さん3「ふふっ」', 'secretary', SEC_CAP),
                                 ('keep2-secretary-large-c3.wav', 'キープ2 秘書さん3 大型', 'secretary', SEC_CAP),
                                 ('keep3-engineer-large-c4.wav', 'キープ3 エンジニアさん4 大型', 'engineer', CHARS[1]['captions'][3])]:
    for m in ('large', 'small'):
        ROUND2.append(('キープの声で5行', kname + '（' + MODELS[m][0].split('（')[0] + '）', kcid, str(KEEP_DIR / kfile), kcap, m))


def r2_path(k, li):
    return OUT / 'audio' / 'r2' / f'v{k + 1:02d}-l{li + 1}.wav'


def generate_round2(model):
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    done = 0
    for k, (group, name, cid, ref, cap, m) in enumerate(ROUND2):
        if m != model:
            continue
        ref_path = ref if ':' in ref else str(OUT / 'audio' / ref)
        for li, line in enumerate(lock_lines(cid)[1]):
            out = r2_path(k, li)
            if out.exists():
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[m][1], '--model-precision', 'bf16', '--text', line,
                        '--caption', cap, '--ref-wav', ref_path, '--seed', str(SEED), '--output-wav', str(out)]
            infer.main()
            done += 1
    print('2回目', done, '本')


def round2_section():
    groups = []
    for g in dict.fromkeys(x[0] for x in ROUND2):
        rows = []
        cid = next(x[2] for x in ROUND2 if x[0] == g)
        heads = ''.join(f'<th class="ln">{html.escape(l)}</th>' for l in lock_lines(cid)[1]) if g != 'キープの声で5行' else ''
        for k, (group, name, c, ref, cap, m) in enumerate(ROUND2):
            if group != g:
                continue
            cells = ''.join(f'<td><button onclick="play(this,&quot;audio/r2/{r2_path(k, li).name}&quot;)">▶</button></td>'
                            if r2_path(k, li).exists() else '<td><span class="miss">未生成</span></td>' for li in range(len(lock_lines(c)[1])))
            rows.append(f'<tr><th>{html.escape(name)}</th><td class="cap">{html.escape(cap)}</td><td class="md">{MODELS[m][0].split("（")[0]}</td>{cells}</tr>')
        if g == 'キープの声で5行':
            heads = '<th class="ln">台詞1〜5（秘書さんは秘書さんの5行、エンジニアさんはエンジニアさんの5行）</th>'
        groups.append(f'<h3>{html.escape(g)}</h3><table><tr><th></th><th>声の性格（キャプション）</th><th>モデル</th>{heads}</tr>{"".join(rows)}</table>')
    return f'''<section class="char" style="display:block;padding:20px 24px;box-shadow:0 0 0 3px #fff,0 0 0 7px #ffd23f,0 14px 34px rgba(27,35,64,.14)"><h2>2回目の聞き比べ（見本の声を固定したまま、性格の文字を変える）</h2>
<p class="about">秘書さんは上段の大型の声から4通り、エンジニアさんは上段の見本を小さいモデルで、くぐもりを晴らして「クールだけど抜けてる」方向へ4通り。キープの3本は、それぞれの声で5行。</p>{''.join(groups)}</section>'''


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
.body{{padding:20px 24px 22px 0;flex:1}} h2{{font-family:'M PLUS Rounded 1c';margin:0;font-size:28px}} h3{{font-family:'M PLUS Rounded 1c';margin:18px 0 6px;font-size:20px}} .about{{margin:4px 0 12px;color:#6b7894;font-weight:700}}
table{{border-collapse:collapse;width:100%;font-size:14px}} th,td{{padding:8px 6px;border-bottom:1px solid #eef1f6;text-align:center;vertical-align:middle}}
th{{color:#6b7894;font-weight:700}} .cap{{text-align:left;max-width:330px;line-height:1.5}} .ln{{font-size:12px;max-width:170px;line-height:1.4}} .lg{{background:#fff8e6}} .md{{font-size:12px;color:#6b7894}}
button{{width:46px;height:46px;border:0;border-radius:14px;cursor:pointer;font-size:18px;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c}}
button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}} .miss{{color:#b5bccb;font-size:12px}}
</style></head><body><header><h1>秘書さん・エンジニアさんの声の試聴</h1>
<p>Irodori-TTS（ローカル・無料）。声は文字の説明だけで作った候補です。気に入った候補に「これ」を付けてください（この画面の中だけの印です）。</p></header>
{round2_section() if (OUT / 'audio' / 'r2').exists() else ''}
{lock_section() if any((OUT / 'audio' / m).exists() for m in ('large', 'small')) else ''}
{''.join(cards)}
<script>let a=null,b=null;function play(btn,src){{if(a){{a.pause();b&&b.classList.remove('on')}}a=new Audio(src);b=btn;btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('試聴ページ', OUT / 'index.html')


if __name__ == '__main__':
    (OUT / 'audio').mkdir(parents=True, exist_ok=True)
    if '--round2' in sys.argv:
        generate_round2(sys.argv[sys.argv.index('--model') + 1] if '--model' in sys.argv else 'large')
    elif '--lock' in sys.argv:
        m = sys.argv[sys.argv.index('--model') + 1] if '--model' in sys.argv else 'large'
        (OUT / 'audio' / m).mkdir(parents=True, exist_ok=True)
        generate_locks(m)
    elif '--only-page' not in sys.argv:
        generate()
    page()
