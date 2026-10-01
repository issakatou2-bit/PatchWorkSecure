# 声の一覧（刷新版、10/2）：加藤さんが「いい」と言った声を見本にして、同じ声のまま新しい台詞を言わせる。
# 使い方（Irodori-TTSの仮想環境のPython。モデルごとに別々に実行する）:
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-Voices.py --model large
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-Voices.py --model small
# 出力：Artifacts/VoiceAudition/v2/（gitで追跡しない）。index.html をブラウザで開く。
# 「アニメ系・声優系」の指定は一般的な言い方だけにする（特定の声優・キャラクターの名前は使わない。Irodoriの利用条件）。
import html, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / 'Artifacts' / 'VoiceAudition' / 'audio'
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v2'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {'small': 'Aratako/Irodori-TTS-v4.1-Small', 'large': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'}
SEED = 20261002
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
SEC_CAP = 'はっきりした発声の、仕事ができる女性秘書。少し早口で、さばさばと辛口に話すが、根は優しい。'
ENG_CAP = '若い女性。眠たげで低めの落ち着いた声。淡々と、抑揚少なめに話すが、どこか優しい。' + CLEAR

SEC_LINES = ['社長のスケジュール、来週の火曜なら三十分空けられるわ。', 'ミスを責めても、次は防げないわ。仕組みで守るのよ。',
             'ふふ、困った顔も可愛いけど……そろそろ答えを聞かせて？', 'あら、今日はずいぶん頑張ったのね。コーヒー、淹れてあげる。', 'ええ、任せて。']
ENG_LINES = ['ログの突き合わせ、取れた。侵入は二時十二分。入口は、委託先の保守ID。', 'この設計なら、拠点が一つ止まっても、仕事は止まらない。',
             '……あ、今日って会議だった？　何時から？　……もう始まってる？', '試験？　うん、受かった。……それより、お昼ごはん食べたっけ。', '……ん。了解。']
# まだ役の決まっていない声は、気持ちの幅が分かる5行で比べる
RANGE = ['えへへ、ちょっとだけ、褒めてもらえたら嬉しいです。', '待ってください！　そのメール、開く前に確かめましょう！',
         '……大丈夫です。わたしが、ちゃんと見ていますから。', 'やったぁ！　ぜんぶ戻せましたね！', 'ふぁ……すみません、ちょっと眠くて。']

NEW_CAPS = {1: '小柄な若い女性。落ち着いていて丁寧、少し眠たげ。知的で、声の高さは中くらい。' + CLEAR,
            2: '物静かで丁寧な、知的な少女。ゆっくり、少し眠たげに話す。声の高さは中くらい。' + CLEAR,
            3: '小柄で落ち着いた女性。淡々として丁寧だが、ときどき柔らかく笑う。' + CLEAR,
            4: '穏やかで理知的な若い女性。丁寧な言葉で、少し眠たげに、やさしく諭すように話す。' + CLEAR}
VOICES = [
    # (まとまり, 名前, 見本（なければNone）, キャプション, モデル, 台詞, 追加の引数)
    ('決まった声', 'かのん（秘書）', 'secretary-small-c3-l1.wav', SEC_CAP, 'large', SEC_LINES, []),
    ('決まった声', 'エンジニアさん（2割速く）', 'engineer-large-c1-l1.wav', ENG_CAP, 'small', ENG_LINES, ['--duration-scale', '0.8']),
    ('キープ', 'キープ1　秘書さん3「ふふっ」', str(ROOT / 'ArtSource/Voice/Irodori-keep/keep1-secretary-small-c3-fufu.wav'), SEC_CAP, 'large', RANGE, []),
    ('キープ', 'キープ2　秘書さん3　大型', str(ROOT / 'ArtSource/Voice/Irodori-keep/keep2-secretary-large-c3.wav'), SEC_CAP, 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声1の2個目', 'r4/new1-02.wav', NEW_CAPS[1], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声1の3個目', 'r4/new1-03.wav', NEW_CAPS[1], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声2の2個目（落ち着いた幼い系）', 'r4/new2-02.wav', NEW_CAPS[2], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声2の3個目', 'r4/new2-03.wav', NEW_CAPS[2], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声2の4個目', 'r4/new2-04.wav', NEW_CAPS[2], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声3の2個目（落ち着いた美少女）', 'r4/new3-02.wav', NEW_CAPS[3], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声4の2個目（真面目な美少女）', 'r4/new4-02.wav', NEW_CAPS[4], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声4の3個目', 'r4/new4-03.wav', NEW_CAPS[4], 'large', RANGE, []),
    ('いいね（新しい声）', '新しい声4（種を変えた版）の2個目', 'r4/new4s-02.wav', NEW_CAPS[4], 'large', RANGE, []),
    ('アニメ系の指定（見本なし）', 'アニメ系1　透明感のあるヒロイン', None, 'アニメのヒロインのような、透明感のある可愛い声の少女。プロの声優のように感情豊かに演じる。' + CLEAR, 'large', RANGE, []),
    ('アニメ系の指定（見本なし）', 'アニメ系2　元気な後輩', None, 'アニメに出てくる元気な後輩キャラクターのような、明るく弾む可愛い声。声優の演技のように表情がはっきりしている。' + CLEAR, 'large', RANGE, []),
    ('アニメ系の指定（見本なし）', 'アニメ系3　クールな美少女', None, 'アニメのクールな美少女キャラクターのような、落ち着いた澄んだ声。声優らしい、繊細で感情のこもった演技。' + CLEAR, 'large', RANGE, []),
    ('アニメ系の指定（見本なし）', 'アニメ系4　おっとりお姉さん', None, 'アニメのおっとりしたお姉さんキャラクターのような、柔らかく甘い声。声優の演技のように、ゆったりと優しく話す。' + CLEAR, 'large', RANGE, []),
]


def path(k, i):
    return OUT / 'audio' / f'v{k + 1:02d}-{i + 1}.wav'


def generate(model):
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached  # 同じモデルを1回だけ読み込む（モデルの切替は別の実行で）
    done = 0
    for k, (group, name, ref, cap, m, lines, extra) in enumerate(VOICES):
        if m != model:
            continue
        ref_args = ['--no-ref'] if ref is None else ['--ref-wav', ref if ':' in ref else str(SRC / ref)]
        for i, text in enumerate(lines):
            out = path(k, i)
            if out.exists():
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[m], '--model-precision', 'bf16', '--text', text, '--caption', cap,
                        *ref_args, '--seed', str(SEED + k), *extra, '--output-wav', str(out)]
            infer.main()
            done += 1
    print('生成', done, '本')


def page():
    (OUT / 'art').mkdir(parents=True, exist_ok=True)
    for f in ('focus-secretary.png', 'focus-engineer.png'):
        shutil.copy(ROOT / 'Assets/Art/KeyVisual' / f, OUT / 'art' / f)
    blocks = []
    for g in dict.fromkeys(v[0] for v in VOICES):
        rows = []
        for k, (group, name, ref, cap, m, lines, extra) in enumerate(VOICES):
            if group != g:
                continue
            files = [f'audio/{path(k, i).name}' for i in range(len(lines)) if path(k, i).exists()]
            chips = ''.join(f'<span class="chip"><button class="sm" onclick="play(this,&quot;audio/{path(k, i).name}&quot;)">▶</button>{html.escape(t)}</span>'
                            for i, t in enumerate(lines) if path(k, i).exists())
            art = '<img class="face" src="art/focus-secretary.png">' if 'かのん' in name else '<img class="face" src="art/focus-engineer.png">' if 'エンジニア' in name else ''
            how = ('見本あり' if ref else '見本なし（文字の説明だけ）') + '・' + ('大型' if m == 'large' else '小さいモデル') + ('・2割速く' if '0.8' in extra else '')
            rows.append(f'<div class="voice">{art}<div class="body"><div class="row"><b>{html.escape(name)}</b><button class="all" onclick="playAll(this,{html.escape(json.dumps(files))})">全部再生</button>'
                        f'<label class="fav"><input type="checkbox"> いい</label></div><div class="cap">{html.escape(cap)}（{how}）</div><div class="chips">{chips}</div></div></div>')
        blocks.append(f'<section><h2>{html.escape(g)}</h2>{"".join(rows)}</section>')
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>声の一覧</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.6}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12)}}
h2{{font-family:'M PLUS Rounded 1c';margin:0 0 8px;font-size:22px}}
.voice{{display:flex;gap:14px;padding:12px 0;border-top:1px solid #eef1f6}} .voice:first-of-type{{border-top:0}}
.face{{width:110px;height:110px;object-fit:cover;object-position:50% 14%;border-radius:16px;flex:none}}
.body{{flex:1}} .row{{display:flex;align-items:center;gap:12px}} .row b{{font-size:17px}} .cap{{color:#6b7894;font-size:13px;margin:4px 0 8px;line-height:1.5}}
.chips{{display:flex;flex-wrap:wrap;gap:8px}} .chip{{display:inline-flex;align-items:center;gap:8px;background:#f3f6fb;border-radius:14px;padding:5px 12px 5px 5px;font-weight:700;font-size:14px}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:13px}} button.all{{height:36px;padding:0 16px;font-size:14px;font-family:'M PLUS Rounded 1c'}}
button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}} .fav{{margin-left:auto;font-weight:700;color:#6b7894}}
</style></head><body><header><h1>声の一覧（刷新版）</h1>
<p>Irodori-TTS（ローカル・無料）。加藤さんが「いい」と言った声を見本にして、同じ声のまま新しい台詞を言わせた。役の決まっていない声は、気持ちの幅が分かる共通の5行。<br>
「アニメ系の指定」は、一般的な言い方だけで作った候補（特定の声優さん・キャラクターは手本にしていない）。「いい」の印はこの画面の中だけ。</p></header>
{"".join(blocks)}
<script>let a=null,b=null,q=[];function play(btn,src){{q=[];if(a){{a.pause();b&&b.classList.remove('on')}}a=new Audio(src);b=btn;btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}
function playAll(btn,list){{q=list.slice();if(a)a.pause();step(btn)}}function step(btn){{if(!q.length){{btn.classList.remove('on');return}}a=new Audio(q.shift());b=btn;btn.classList.add('on');a.onended=()=>setTimeout(()=>step(btn),350);a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--only-page' not in sys.argv:
        generate(sys.argv[sys.argv.index('--model') + 1] if '--model' in sys.argv else 'large')
    page()
