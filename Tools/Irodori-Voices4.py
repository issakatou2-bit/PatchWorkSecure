# 声の一覧 その4（10/2）：①3人の掛け合い ②3人それぞれ300文字ほどの文学の一節 ③ひなたの再現の改良（見本を長く・寄せ方を強く）
# 使い方（Irodori-TTSの仮想環境のPython。モデルごとに別々に実行する）:
#   .../.venv/Scripts/python.exe Tools/Irodori-Voices4.py --model large   … かのん・ひなた（再現）
#   .../.venv/Scripts/python.exe Tools/Irodori-Voices4.py --model small   … エンジニア
#   .../.venv/Scripts/python.exe Tools/Irodori-Voices4.py --check         … 書き起こし（faster-whisper）で読みを確かめる
# 出力：Artifacts/VoiceAudition/v4/（gitで追跡しない）。文学は青空文庫の本文（著作権の切れた作品）。
# 読み：声に渡す文は、振り仮名と、読み違えやすい漢字をかなで書いた「読み」。字幕は原文。
# ひなたの再現は、ElevenLabsの無料枠の音声を見本にした私的な試しで、ゲーム・配布には使わない。
import html, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v4'
HINATA = ROOT / 'Assets/Audio/CompanyYear/VoiceTest/Hinata'
REFS = ROOT / 'ArtSource/Voice/Irodori-refs'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {'small': 'Aratako/Irodori-TTS-v4.1-Small', 'large': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only'}
SEED = 20261013
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
VOICE = {
    'kanon': dict(model='large', ref=REFS / 'secretary-ref.wav', extra=[],
                  cap='はっきりした発声の、仕事ができる女性秘書。少し早口で、さばさばと辛口に話すが、根は優しい。'),
    'eng': dict(model='small', ref=REFS / 'engineer-ref.wav', extra=['--duration-scale', '0.8'],
                cap='若い女性。眠たげで低めの落ち着いた声。淡々と、抑揚少なめに話すが、どこか優しい。' + CLEAR),
    'hinata': dict(model='large', ref=None, extra=['--cfg-scale-speaker', '7'],
                   cap='明るく元気な若い女性。少し幼く、勢いよく弾むように話す。息づかいまで自然に。' + CLEAR),
}
NAMES = {'kanon': 'かのん', 'eng': 'エンジニア', 'hinata': 'ひなた（再現）'}
# 掛け合い（字幕, 読み）
SKIT = [
    ('kanon', 'ほら、起きて。会議、五分前よ。', 'ほら、起きて。会議、ごふんまえよ。'),
    ('eng', '……ん。会議って、今日だっけ。', '……ん。会議って、きょうだっけ。'),
    ('hinata', '今日だよ！　しかも、エンジニアさんの発表の日だよ！', 'きょうだよ！　しかも、エンジニアさんの発表の日だよ！'),
    ('eng', '……あ。資料、まだ半分。', '……あ。資料、まだはんぶん。'),
    ('kanon', '半分あれば十分よ。数字のところは、わたしが補うわ。', 'はんぶんあれば、じゅうぶんよ。数字のところは、わたしがおぎなうわ。'),
    ('hinata', 'わたし、ログのグラフ、すぐ出せるよ！　毎晩見てるもん！', 'わたし、ログのグラフ、すぐ出せるよ！　まいばん見てるもん！'),
    ('eng', '……助かる。じゃあ、侵入の経路から話す。', '……たすかる。じゃあ、しんにゅうの、けいろから話す。'),
    ('kanon', '結論から、三行で。社長は、長い話が苦手なの。', 'けつろんから、さんぎょうで。社長は、長い話がにがてなの。'),
    ('eng', '三行……。入口は保守ID。止めたのは一台だけ。被害は、なし。', 'さんぎょう……。いりぐちは、ほしゅアイディー。止めたのは、いちだいだけ。ひがいは、なし。'),
    ('hinata', 'すごい、ほんとに三行！', 'すごい、ほんとに、さんぎょう！'),
    ('kanon', 'ふふっ、上出来よ。……次は、遅刻しないでね。', 'ふふっ、じょうできよ。……つぎは、ちこくしないでね。'),
    ('eng', '……努力する。たぶん。いや、する。', '……どりょくする。たぶん。いや、する。'),
]
# 文学（字幕の原文, 読み, 出典）。読みは長いので文の切れ目で分けて作り、短い間でつなぐ
LIT = {
    'kanon': ('山路を登りながら、こう考えた。智に働けば角が立つ。情に棹させば流される。意地を通せば窮屈だ。とかくに人の世は住みにくい。住みにくさが高じると、安い所へ引き越したくなる。どこへ越しても住みにくいと悟った時、詩が生れて、画が出来る。人の世を作ったものは神でもなければ鬼でもない。やはり向う三軒両隣りにちらちらするただの人である。ただの人が作った人の世が住みにくいからとて、越す国はあるまい。あれば人でなしの国へ行くばかりだ。人でなしの国は人の世よりもなお住みにくかろう。',
              'やまみちを登りながら、こう考えた。ちに働けば、かどが立つ。じょうに、さおさせば、流される。意地をとおせば、きゅうくつだ。とかくに人の世は、住みにくい。|住みにくさが、こうじると、安いところへ、引き越したくなる。どこへ越しても、住みにくいと、さとった時、詩がうまれて、えができる。|人の世を作ったものは、神でもなければ、鬼でもない。やはり、むこうさんげん、りょうどなりに、ちらちらする、ただの人である。|ただの人が作った人の世が、住みにくいからとて、こす国はあるまい。あれば、ひとでなしの国へ行くばかりだ。ひとでなしの国は、人の世よりも、なお住みにくかろう。',
              '夏目漱石『草枕』（青空文庫）', '落ち着いた大人の声で、しみじみと、物語を読むように朗読する。'),
    'eng': ('ある日の事でございます。御釈迦様は極楽の蓮池のふちを、独りでぶらぶら御歩きになっていらっしゃいました。池の中に咲いている蓮の花は、みんな玉のようにまっ白で、そのまん中にある金色の蕊からは、何とも云えない好い匂が、絶間なくあたりへ溢れて居ります。極楽は丁度朝なのでございましょう。やがて御釈迦様はその池のふちに御佇みになって、水の面を蔽っている蓮の葉の間から、ふと下の容子を御覧になりました。この極楽の蓮池の下は、丁度地獄の底に当って居りますから、水晶のような水を透き徹して、三途の河や針の山の景色が、丁度覗き眼鏡を見るように、はっきりと見えるのでございます。',
            'ある日のことで、ございます。おしゃかさまは、ごくらくの、はすいけのふちを、ひとりで、ぶらぶら、おあるきになって、いらっしゃいました。|いけの中に咲いている、はすの花は、みんな、たまのように、まっしろで、そのまんなかにある、きんいろの、ずいからは、なんとも言えない、よいにおいが、たえまなく、あたりへ、あふれております。ごくらくは、ちょうど、あさなのでございましょう。|やがて、おしゃかさまは、そのいけのふちに、おたたずみになって、みずのおもてを、おおっている、蓮の葉の、あいだから、ふと、したのようすを、ごらんになりました。|このごくらくの、はすいけの下は、ちょうど、じごくの底に、あたっておりますから、すいしょうのような水を、すきとおして、さんずのかわや、はりのやまの、けしきが、ちょうど、のぞきめがねを見るように、はっきりと見えるのでございます。',
            '芥川龍之介『蜘蛛の糸』（青空文庫）', '静かに、淡々と、語り聞かせるように朗読する。'),
    'hinata': ('寒い冬が北方から、狐の親子の棲んでいる森へもやって来ました。或朝洞穴から子供の狐が出ようとしましたが、「あっ」と叫んで眼を抑えながら母さん狐のところへころげて来ました。「母ちゃん、眼に何か刺さった、ぬいて頂戴早く早く」と言いました。母さん狐がびっくりして、あわてふためきながら、眼を抑えている子供の手を恐る恐るとりのけて見ましたが、何も刺さってはいませんでした。母さん狐は洞穴の入口から外へ出て始めてわけが解りました。昨夜のうちに、真白な雪がどっさり降ったのです。その雪の上からお陽さまがキラキラと照していたので、雪は眩しいほど反射していたのです。雪を知らなかった子供の狐は、あまり強い反射をうけたので、眼に何か刺さったと思ったのでした。',
               '寒い冬が、ほっぽうから、きつねの親子の、すんでいる森へも、やって来ました。|あるあさ、ほらあなから、こどもの、きつねが、出ようとしましたが、「あっ」と叫んで、めをおさえながら、かあさんぎつねのところへ、ころげて来ました。|「かあちゃん、めに、なにか、ささった。ぬいてちょうだい、はやく、はやく」と言いました。|かあさんぎつねが、びっくりして、あわてふためきながら、めをおさえている、こどもの手を、おそるおそる、とりのけて見ましたが、なにも、ささっては、いませんでした。|かあさんぎつねは、ほら穴の入口から、外へ出て、はじめて、わけがわかりました。|ゆうべのうちに、まっしろな雪が、どっさり降ったのです。その雪の上から、おひさまが、キラキラと、てらしていたので、雪は、まぶしいほど、はんしゃしていたのです。|雪を知らなかった、こどもの、きつねは、あまり強い、はんしゃを、うけたので、めに、なにか、ささったと、思ったのでした。',
               '新美南吉『手袋を買いに』（青空文庫）', '子どもに読み聞かせるように、表情豊かに、やさしく朗読する。'),
}
# ひなたの再現の比べ用（元の声があるもの）
HIN_CMP = [('maxim_backup', 'バックアップは、戻せてこそバックアップ、だよ！'), ('diary_y1_10', '開発部のエンジニアさん、すごかった。ポテト食べてたけど'),
           ('mg_end_good', '完璧〜っ！　プロの仕事だね！'), ('peak_goal_06', '今月は最初の山場だよ！　目標、ちゃんと見ておいてね！')]
HIN_REF_IDS = ['tutorial_1', 'mg_start_01', 'mg_start_02', 'mg_combo_01', 'mg_combo_02', 'mg_miss_01', 'mg_miss_02', 'mg_end_ok', 'mg_end_bad',
               'diary_y1_06', 'diary_y1_04', 'diary_y1_07', 'think_02', 'maxim_restore', 'maxim_report', 'maxim_human']


def hinata_ref():
    out = OUT / 'refs' / 'hinata-ref-long.wav'
    if out.exists():
        return out
    import numpy as np, soundfile as sf
    parts, sr = [], None
    for i in HIN_REF_IDS:
        p = HINATA / f'{i}.mp3'
        if not p.exists():
            continue
        d, s = sf.read(p)
        d = d.mean(1) if d.ndim > 1 else d
        sr = sr or s
        parts += [d, np.zeros(int(sr * 0.35))]
    out.parent.mkdir(parents=True, exist_ok=True)
    sf.write(out, np.concatenate(parts), sr)
    return out


def jobs():
    for i, (who, text, read) in enumerate(SKIT):
        yield ('skit', f'skit-{i + 1:02d}.wav', who, [read])
    for who, (text, read, src, act) in LIT.items():
        yield ('lit', f'lit-{who}.wav', who, read.split('|'))
    for i, (rid, text) in enumerate(HIN_CMP):
        yield ('hcmp', f'hcmp-{i + 1}.wav', 'hinata', [text])


def generate(model):
    import numpy as np, soundfile as sf
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    done = 0
    for kind, name, who, chunks in jobs():
        v = VOICE[who]
        if v['model'] != model:
            continue
        out = OUT / 'audio' / name
        if out.exists():
            continue
        out.parent.mkdir(parents=True, exist_ok=True)
        ref = v['ref'] or hinata_ref()
        cap = v['cap'] + (LIT[who][3] if kind == 'lit' else '')
        pieces, sr = [], None
        for j, chunk in enumerate(chunks):
            tmp = OUT / 'tmp' / f'{name}-{j}.wav'
            tmp.parent.mkdir(parents=True, exist_ok=True)
            sys.argv = ['infer.py', '--hf-checkpoint', MODELS[model], '--model-precision', 'bf16', '--text', chunk, '--caption', cap,
                        '--ref-wav', str(ref), '--seed', str(SEED), *v['extra'], '--output-wav', str(tmp)]
            infer.main()
            d, s = sf.read(tmp)
            sr = sr or s
            pieces += [d, np.zeros(int(sr * 0.45)) if d.ndim == 1 else np.zeros((int(sr * 0.45), d.shape[1]))]
        sf.write(out, np.concatenate(pieces[:-1]), sr)
        done += 1
    print('生成', done, '本')


def check():
    # 書き起こして、読みの食い違いを見つける（人が最後に聞いて確かめる前の、ふるい分け）
    import os, torch
    # faster-whisperのGPU部品（cublas）はPyTorchに同梱のものを使う
    os.add_dll_directory(os.path.join(os.path.dirname(torch.__file__), 'lib'))
    os.environ['PATH'] = os.path.join(os.path.dirname(torch.__file__), 'lib') + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    m = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16')
    res = {}
    for kind, name, who, chunks in jobs():
        p = OUT / 'audio' / name
        if p.exists():
            # 長い朗読はつないだ後だと書き起こしが崩れる（無い言葉が足される）ので、区切りごとに書き起こして「｜」でつなぐ
            parts = [OUT / 'tmp' / f'{name}-{j}.wav' for j in range(len(chunks))] if kind == 'lit' else [p]
            res[name] = '｜'.join(''.join(s.text for s in m.transcribe(str(q), language='ja', beam_size=5)[0]) for q in parts if q.exists())
    (OUT / 'transcripts.json').write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding='utf-8')
    print('書き起こし', len(res), '本')


def page():
    tr = json.loads((OUT / 'transcripts.json').read_text(encoding='utf-8')) if (OUT / 'transcripts.json').exists() else {}
    def btn(name):
        return f'<button class="sm" onclick="play(this,&quot;audio/{name}&quot;)">▶</button>' if (OUT / 'audio' / name).exists() else '<span class="miss">未生成</span>'
    def heard(name):
        return f'<div class="heard">書き起こし：{html.escape(tr[name])}</div>' if name in tr else ''
    skit = ''.join(f'<div class="line"><span class="who {who}">{NAMES[who]}</span>{btn(f"skit-{i + 1:02d}.wav")}<div><div>{html.escape(text)}</div>{heard(f"skit-{i + 1:02d}.wav")}</div></div>'
                   for i, (who, text, read) in enumerate(SKIT))
    files = json.dumps([f'audio/skit-{i + 1:02d}.wav' for i in range(len(SKIT)) if (OUT / 'audio' / f'skit-{i + 1:02d}.wav').exists()])
    lit = ''.join(f'<div class="lit"><div class="row"><span class="who {who}">{NAMES[who]}</span>{btn(f"lit-{who}.wav")}<b>{html.escape(LIT[who][2])}</b></div>'
                  f'<div class="txt">{html.escape(LIT[who][0])}</div>{heard(f"lit-{who}.wav")}</div>' for who in LIT)
    cmp_rows = ''.join(f'<div class="line"><span class="who hinata">ひなた</span><button class="sm o" onclick="play(this,&quot;orig/{rid}.mp3&quot;)">元</button>{btn(f"hcmp-{i + 1}.wav")}<div><div>{html.escape(t)}</div>{heard(f"hcmp-{i + 1}.wav")}</div></div>'
                       for i, (rid, t) in enumerate(HIN_CMP))
    (OUT / 'orig').mkdir(parents=True, exist_ok=True)
    import shutil
    for rid, _ in HIN_CMP:
        shutil.copy(HINATA / f'{rid}.mp3', OUT / 'orig' / f'{rid}.mp3')
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>声の一覧 その4</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:26px 36px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:32px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.6}}
section{{margin:18px 36px;background:#fff;border-radius:22px;padding:16px 22px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12)}}
h2{{font-family:'M PLUS Rounded 1c';margin:0 0 8px;font-size:22px;display:flex;align-items:center;gap:12px}}
.line{{display:flex;align-items:flex-start;gap:10px;padding:7px 0;border-top:1px solid #eef1f6;font-weight:700}}
.who{{flex:none;width:110px;border-radius:10px;padding:4px 8px;text-align:center;color:#fff;font-family:'M PLUS Rounded 1c';font-size:14px}}
.who.kanon{{background:#5b4b8a}} .who.eng{{background:#2f93dc}} .who.hinata{{background:#ff6f91}}
.heard{{font-size:12px;color:#8a8399;font-weight:500;margin-top:2px}} .lit{{padding:10px 0;border-top:1px solid #eef1f6}} .row{{display:flex;align-items:center;gap:10px}}
.txt{{font-size:14px;line-height:1.8;margin:6px 0 2px}} .warn{{background:#fff6d6;border-radius:12px;padding:8px 12px;font-size:13px;line-height:1.6;color:#6a5200}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px;flex:none}}
button.sm{{width:34px;height:34px;font-size:13px}} button.o{{background:linear-gradient(180deg,#b9c4d6,#8a97b0);box-shadow:0 4px 0 #6b7894;font-size:12px}}
button.all{{height:36px;padding:0 16px;font-size:14px;font-family:'M PLUS Rounded 1c'}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>声の一覧 その4</h1><p>3人の掛け合い／3人それぞれの文学の一節（約300文字・青空文庫）／ひなたの再現の改良。<br>
声に渡す文は、振り仮名と、読み違えやすい漢字をかなで書いた「読み」。字幕は原文。「書き起こし」は、作った声を文字に起こした結果（読み違いを見つけるため）。</p></header>
<section><h2>掛け合い　会議を忘れたエンジニア<button class="all" onclick="playAll(this,{html.escape(files)})">全部再生</button></h2>{skit}</section>
<section><h2>文学の一節（約300文字）</h2>{lit}</section>
<section><h2>ひなたの再現の改良</h2><p class="warn">見本を4本から16本（約1分）に増やし、見本の声への寄せ方を強めた（5→7）。「元」はElevenLabsの元の声。<b>私的な試しだけ</b>（見本が無料枠の音声のため、ゲーム・配布には使わない）。</p>{cmp_rows}</section>
<script>let a=null,b=null,q=[];function play(btn,src){{q=[];if(a){{a.pause();b&&b.classList.remove('on')}}a=new Audio(src);b=btn;btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}
function playAll(btn,list){{q=list.slice();if(a)a.pause();step(btn)}}function step(btn){{if(!q.length){{btn.classList.remove('on');return}}a=new Audio(q.shift());b=btn;btn.classList.add('on');a.onended=()=>setTimeout(()=>step(btn),250);a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--check' in sys.argv:
        check()
    elif '--only-page' not in sys.argv:
        generate(sys.argv[sys.argv.index('--model') + 1] if '--model' in sys.argv else 'large')
    page()
