# 声の一覧 その3（10/2）：①気に入った声の別の台詞 ②名作の一節を演技つきで ③ひなたの声の再現の試し
# 使い方（Irodori-TTSの仮想環境のPython。モデルごとに別々に実行する）:
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-Voices3.py --model large
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-Voices3.py --model small
# 出力：Artifacts/VoiceAudition/v3/（gitで追跡しない）。
# 名作は著作権の切れた作品だけ（太宰治・夏目漱石・宮沢賢治・枕草子・平家物語）。
# ③は加藤さんのElevenLabsのひなたの声（無料枠で作った音声）を見本にした私的な試し。ゲーム・配布には使わない（理由はページに書く）。
import html, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / 'Artifacts' / 'VoiceAudition' / 'audio'
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v3'
HINATA = ROOT / 'Assets/Audio/CompanyYear/VoiceTest/Hinata'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {'small': 'Aratako/Irodori-TTS-v4.1-Small', 'large': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'}
SEED = 20261012
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
SEC_CAP = 'はっきりした発声の、仕事ができる女性秘書。少し早口で、さばさばと辛口に話すが、根は優しい。'
ENG_CAP = '若い女性。眠たげで低めの落ち着いた声。淡々と、抑揚少なめに話すが、どこか優しい。' + CLEAR
NEW2 = '物静かで丁寧な、知的な少女。ゆっくり、少し眠たげに話す。声の高さは中くらい。' + CLEAR
NEW3 = '小柄で落ち着いた女性。淡々として丁寧だが、ときどき柔らかく笑う。' + CLEAR
NEW4 = '穏やかで理知的な若い女性。丁寧な言葉で、少し眠たげに、やさしく諭すように話す。' + CLEAR
HIN_CAP = '明るく元気な若い女性。少し幼く、勢いよく弾むように話す。' + CLEAR
KEEP = ROOT / 'ArtSource/Voice/Irodori-keep'
REFS = ROOT / 'ArtSource/Voice/Irodori-refs'

ALT = ['今日の会議の資料、もう共有フォルダに入れておきましたよ。', 'えっ、それ本当ですか？　……ちょっと待ってくださいね。',
       '大丈夫、大丈夫。深呼吸して、ひとつずつ片付けよう。', 'ふふ、それじゃあ、また明日。おやすみなさい。']
# (台詞, 声用の読み)
CLASSICS = {
    'meros': ('メロスは激怒した。必ず、かの邪智暴虐の王を除かなければならぬと決意した。', 'メロスは激怒した。必ず、かの、じゃちぼうぎゃくの王を除かなければならぬと決意した。', '太宰治『走れメロス』'),
    'neko': ('吾輩は猫である。名前はまだ無い。どこで生れたかとんと見当がつかぬ。', 'わがはいは猫である。名前はまだ無い。どこで生れたか、とんと見当がつかぬ。', '夏目漱石『吾輩は猫である』'),
    'makura': ('春はあけぼの。やうやう白くなりゆく山ぎは、すこしあかりて、紫だちたる雲のほそくたなびきたる。', 'はるは、あけぼの。ようよう白くなりゆく山ぎわ、すこしあかりて、むらさきだちたる雲の、ほそくたなびきたる。', '清少納言『枕草子』'),
    'ame': ('雨ニモマケズ　風ニモマケズ　雪ニモ夏ノ暑サニモマケヌ　丈夫ナカラダヲモチ', '雨にも負けず、風にも負けず、雪にも夏の暑さにも負けぬ、丈夫なからだを持ち。', '宮沢賢治『雨ニモマケズ』'),
    'heike': ('祇園精舎の鐘の声、諸行無常の響きあり。沙羅双樹の花の色、盛者必衰の理をあらはす。', 'ぎおんしょうじゃの、かねのこえ。しょぎょうむじょうの、ひびきあり。さらそうじゅの、はなのいろ。じょうしゃひっすいの、ことわりをあらわす。', '『平家物語』'),
}
HIN_LINES = [('maxim_backup', 'バックアップは、戻せてこそバックアップ、だよ！'), ('diary_y1_10', 'セキュリティ会社のエンジニアさん、すごかった。ポテト食べてたけど'),
             ('mg_start_01', 'よーし、いくよっ！'), ('think_01', 'う〜ん……'),
             (None, 'おはよう、先輩！　今日もいちにち、がんばろうね！'), (None, 'えっ、そのメール開いちゃった？　大丈夫、すぐ言ってくれてありがとう！')]
HIN_REF_IDS = ['tutorial_1', 'mg_end_good', 'peak_goal_06', 'diary_y1_06']

# (まとまり, 名前, 見本, キャプション, モデル, [(字幕, 声用の読み, 注記)], 追加の引数)
def items():
    v = []
    for name, ref, cap, m, extra in [
        ('キープ1　秘書さん3「ふふっ」', KEEP / 'keep1-secretary-small-c3-fufu.wav', SEC_CAP, 'large', []),
        ('キープ2　秘書さん3　大型', KEEP / 'keep2-secretary-large-c3.wav', SEC_CAP, 'large', []),
        ('新しい声2の2個目', SRC / 'r4/new2-02.wav', NEW2, 'large', []),
        ('新しい声3の2個目', SRC / 'r4/new3-02.wav', NEW3, 'large', []),
        ('新しい声4の2個目', SRC / 'r4/new4-02.wav', NEW4, 'large', [])]:
        v.append(('気に入った声の別の台詞', name, ref, cap, m, [(t, t, '') for t in ALT], extra))
    v.append(('気に入った声の別の台詞', 'かのん', REFS / 'secretary-ref.wav', SEC_CAP, 'large',
              [(t, t, '') for t in ['予算の話は、結論から。三行でまとめてきなさい。', 'あら、もうこんな時間。今日はもう帰りなさい、ひなたちゃん。',
                                    '社長には内緒よ？　……でも、よくやったわね。']], []))
    v.append(('気に入った声の別の台詞', 'エンジニアさん', REFS / 'engineer-ref.wav', ENG_CAP, 'small',
              [(t, t, '') for t in ['その設定、三年前のままだね。……直しとく。', 'ポテト、塩じゃないの？　……まあ、いいけど。',
                                    'ログは嘘をつかない。……人は、ときどき忘れるけど。']], ['--duration-scale', '0.8']))
    for key, who, ref, cap, m, extra, act in [
        ('meros', 'かのん', REFS / 'secretary-ref.wav', SEC_CAP, 'large', [], '激しい怒りを込めて、力強く、朗読するように。'),
        ('meros', 'ひなた（再現）', None, HIN_CAP, 'large', [], '激しい怒りを込めて、力強く、朗読するように。'),
        ('neko', 'エンジニアさん', REFS / 'engineer-ref.wav', ENG_CAP, 'small', [], 'とぼけた調子で、淡々と朗読する。'),
        ('neko', 'キープ2', KEEP / 'keep2-secretary-large-c3.wav', SEC_CAP, 'large', [], 'とぼけた調子で、少しおかしそうに朗読する。'),
        ('makura', '新しい声2の2個目', SRC / 'r4/new2-02.wav', NEW2, 'large', [], 'みやびに、ゆったりと情景を味わうように朗読する。'),
        ('makura', 'かのん', REFS / 'secretary-ref.wav', SEC_CAP, 'large', [], 'みやびに、ゆったりと情景を味わうように朗読する。'),
        ('ame', '新しい声4の2個目', SRC / 'r4/new4-02.wav', NEW4, 'large', [], '静かに、祈るように、一語ずつ噛みしめて朗読する。'),
        ('heike', 'キープ1', KEEP / 'keep1-secretary-small-c3-fufu.wav', SEC_CAP, 'large', [], '重々しく、語り部のように、ゆっくり朗読する。')]:
        text, reading, src = CLASSICS[key]
        v.append(('名作の一節（演技つき）', f'{src}　{who}', ref, cap + act, m, [(text, reading, src)], extra))
    v.append(('ひなたの声の再現（私的な試し）', 'ひなた（再現・大型）', None, HIN_CAP, 'large', [(t, t, i or '') for i, t in HIN_LINES], []))
    v.append(('ひなたの声の再現（私的な試し）', 'ひなた（再現・小さいモデル）', None, HIN_CAP, 'small', [(t, t, i or '') for i, t in HIN_LINES], []))
    return v


def hinata_ref():
    # ひなたの声4本を0.3秒あけて1本にする（見本は長い方が安定する）
    out = OUT / 'refs' / 'hinata-ref.wav'
    if out.exists():
        return out
    import numpy as np, soundfile as sf
    parts, sr = [], None
    for i in HIN_REF_IDS:
        d, s = sf.read(HINATA / f'{i}.mp3')
        d = d.mean(1) if d.ndim > 1 else d
        sr = sr or s
        parts += [d, np.zeros(int(sr * 0.3))]
    out.parent.mkdir(parents=True, exist_ok=True)
    sf.write(out, np.concatenate(parts), sr)
    return out


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
    infer.InferenceRuntime = Cached
    done = 0
    for k, (group, name, ref, cap, m, lines, extra) in enumerate(items()):
        if m != model:
            continue
        ref = ref or hinata_ref()
        for i, (text, reading, note) in enumerate(lines):
            out = path(k, i)
            if out.exists():
                continue
            out.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[m], '--model-precision', 'bf16', '--text', reading, '--caption', cap,
                        '--ref-wav', str(ref), '--seed', str(SEED + k), *extra, '--output-wav', str(out)]
            infer.main()
            done += 1
    print('生成', done, '本')


def page():
    (OUT / 'orig').mkdir(parents=True, exist_ok=True)
    for i, _ in HIN_LINES:
        if i:
            shutil.copy(HINATA / f'{i}.mp3', OUT / 'orig' / f'{i}.mp3')
    blocks = []
    for g in dict.fromkeys(x[0] for x in items()):
        rows = []
        for k, (group, name, ref, cap, m, lines, extra) in enumerate(items()):
            if group != g:
                continue
            files = [f'audio/{path(k, i).name}' for i in range(len(lines)) if path(k, i).exists()]
            chips = ''
            for i, (text, reading, note) in enumerate(lines):
                if not path(k, i).exists():
                    continue
                orig = f'<button class="sm o" title="元の声（ElevenLabs）" onclick="play(this,&quot;orig/{note}.mp3&quot;)">元</button>' if g.startswith('ひなた') and note else ''
                chips += f'<span class="chip"><button class="sm" onclick="play(this,&quot;audio/{path(k, i).name}&quot;)">▶</button>{orig}{html.escape(text)}</span>'
            how = '・'.join(['大型' if m == 'large' else '小さいモデル'] + (['2割速く'] if '0.8' in extra else []))
            rows.append(f'<div class="voice"><div class="row"><b>{html.escape(name)}</b><button class="all" onclick="playAll(this,{html.escape(json.dumps(files))})">全部再生</button>'
                        f'<label class="fav"><input type="checkbox"> いい</label></div><div class="cap">{html.escape(cap)}（{how}）</div><div class="chips">{chips}</div></div>')
        note = ''
        if g.startswith('ひなた'):
            note = ('<p class="warn">ElevenLabsのひなたの声4本（あいさつ・高評価・山場・日記）を見本にしたIrodoriの再現。「元」は元のElevenLabsの声。'
                    '<b>私的な試しだけ</b>：見本が無料枠の音声なので、この再現をゲームに使ったり配布したりはしない。使うなら、課金中に作った音声を見本にし直し、ElevenLabsの規約（出力の使い方）を確認してから。</p>')
        if g.startswith('名作'):
            note = '<p class="warn">著作権の切れた作品だけ（太宰治・夏目漱石・宮沢賢治・清少納言・平家物語）。古い読みは声用にかなで書いた（字幕は原文）。</p>'
        blocks.append(f'<section><h2>{html.escape(g)}</h2>{note}{"".join(rows)}</section>')
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>声の一覧 その3</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12)}}
h2{{font-family:'M PLUS Rounded 1c';margin:0 0 8px;font-size:22px}} .warn{{background:#fff6d6;border-radius:12px;padding:8px 12px;font-size:13px;line-height:1.6;color:#6a5200}}
.voice{{padding:12px 0;border-top:1px solid #eef1f6}} .row{{display:flex;align-items:center;gap:12px}} .row b{{font-size:17px}} .cap{{color:#6b7894;font-size:13px;margin:4px 0 8px;line-height:1.5}}
.chips{{display:flex;flex-wrap:wrap;gap:8px}} .chip{{display:inline-flex;align-items:center;gap:6px;background:#f3f6fb;border-radius:14px;padding:5px 12px 5px 5px;font-weight:700;font-size:14px}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}}
button.sm{{width:34px;height:34px;font-size:13px}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894;font-size:12px}}
button.all{{height:36px;padding:0 16px;font-size:14px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}} .fav{{margin-left:auto;font-weight:700;color:#6b7894}}
</style></head><body><header><h1>声の一覧 その3</h1><p>気に入った声の別の台詞／名作の一節（演技つき）／ひなたの声の再現の試し。Irodori-TTS（ローカル・無料）。</p></header>
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
