# 声の一覧 その13（10/3）：大型の完全版と量子化版（int8）を、同じ台詞・同じ設定・同じ種で比べる
# 加藤さん：息づかいや細部の質を上げたい。完全版を入れて少数で試したい（10/3にダウンロードを承認）。
# 完全版：Aratako/Irodori-TTS-v4-Large（model.safetensors 13.15GB、Gemmaの利用規約）。量子化版：同じ作者の int8-weight-only（今まで使っていたもの）。
# 使い方（モデルごとに別々に実行する。同じ処理で切り替えると落ちる。完全版はメモリを大きく使うので、Codexのテスト中は待つ）:
#   .../.venv/Scripts/python.exe Tools/Irodori-FullTest.py --model quant
#   sh Tools/Wait-GpuFree.sh && .../.venv/Scripts/python.exe Tools/Irodori-FullTest.py --model full
#   .../.venv/Scripts/python.exe Tools/Irodori-FullTest.py --page
# 出力：Artifacts/VoiceAudition/v13/（gitで追跡しない）
import html, json, sys, time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Artifacts' / 'VoiceAudition' / 'v13'
VA = ROOT / 'Artifacts' / 'VoiceAudition'
REFS = ROOT / 'ArtSource/Voice/Irodori-refs'
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {'quant': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only', 'full': 'Aratako/Irodori-TTS-v4-Large'}
SEED = 20261131
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
READ = CLEAR + '読み上げる文の意味を分かって、文のまとまりごとに自然な抑揚で読む。話すときより声を低く落とし、落ち着いたピッチで、ゆったりと朗読する。'
TALK = '同僚どうしの、オフィスでのくだけた会話。読み上げではなく、目の前の相手に話しかけている。'
HIN = '社会人2年目の、明るく元気な女性。人懐っこく、ほがらかで、やわらかく甘さのある高めの声。' + CLEAR + '情シスの後輩が、職場の先輩に話しかけている。読み上げではなく、目の前の相手との会話。'
# （名前, 見本, 説明, 声に渡す文, 追加の設定）
JOBS = [
    ('kanon-lit1', REFS / 'secretary-ref.wav', '落ち着いた大人の女性が、しみじみと、物語を読むように朗読する。いつもの話し声よりはっきり低い、深みのある声で読む。' + READ,
     '📖意地を通せば窮屈だ。とかくに人の世は住みにくい。', ['--duration-scale', '0.9', '--cfg-scale-speaker', '3']),
    ('kanon-lit2', REFS / 'secretary-ref.wav', '落ち着いた大人の女性が、しみじみと、物語を読むように朗読する。いつもの話し声よりはっきり低い、深みのある声で読む。' + READ,
     '📖どこへ越しても住みにくいと悟った時、詩が生まれて、絵が出来る。', ['--duration-scale', '0.9', '--cfg-scale-speaker', '3']),
    ('lily-1', REFS / 'engineer-ref.wav', '若い女性。眠たげで低めの落ち着いた声。' + CLEAR + TALK + '二人に感謝して、少しだけやわらかく。後半は仕事の話になり、淡々と早めに。',
     '……助かる😌。じゃあ、侵入の経路から話す。', ['--duration-scale', '0.85', '--cfg-scale-speaker', '3']),
    ('lily-2', REFS / 'engineer-ref.wav', '若い女性。眠たげで低めの落ち着いた声。' + CLEAR + TALK + '少し考えてから、要点だけを短く、正確に、自信をもって言い切る。',
     'さんぎょう……🤔。入口は保守アイディー。止めたのは一台だけ。被害は、なし。', ['--duration-scale', '0.85', '--cfg-scale-speaker', '3']),
    ('hinata-1', VA / 'v9' / 'audio' / 'H09-greet.wav', HIN + '心から感心して、弾むようにほめる。',
     'かんぺき〜っ！😆　プロの仕事だね！', ['--cfg-scale-speaker', '5']),
    ('hinata-2', VA / 'v9' / 'audio' / 'H09-greet.wav', HIN + 'やさしく、相手を責めないように言う。',
     '人は間違えるもの。🫶だから、仕組みで守ろ！', ['--cfg-scale-speaker', '5']),
]


def generate(model):
    sys.path.insert(0, str(IRODORI))
    import infer, torch
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    stats = {}
    for name, ref, cap, text, extra in JOBS:
        out = OUT / 'audio' / f'{model}-{name}.wav'
        out.parent.mkdir(parents=True, exist_ok=True)
        t = time.time()
        sys.argv = ['infer.py', '--hf-checkpoint', MODELS[model], '--model-precision', 'bf16', '--text', text, '--caption', cap,
                    '--ref-wav', str(ref), '--cfg-scale-caption', '5', '--seed', str(SEED), *extra, '--output-wav', str(out)]
        infer.main()
        stats[name] = round(time.time() - t, 1)
    stats['peak_vram_gb'] = round(torch.cuda.max_memory_allocated() / 2 ** 30, 2)
    (OUT / f'stats-{model}.json').write_text(json.dumps(stats, ensure_ascii=False, indent=1), encoding='utf-8')
    print(model, stats)


def page():
    st = {m: json.loads((OUT / f'stats-{m}.json').read_text(encoding='utf-8')) for m in MODELS if (OUT / f'stats-{m}.json').exists()}

    def btn(m, name):
        f = OUT / 'audio' / f'{m}-{name}.wav'
        return f'<button class="sm" onclick="play(this,&quot;audio/{m}-{name}.wav&quot;)">▶</button>' if f.exists() else '<span class="miss">未生成</span>'
    rows = ''.join(f'<tr><td><b>{html.escape(text.replace("📖", ""))}</b><br><small>{name}</small></td><td>{btn("quant", name)}<small>{st.get("quant", {}).get(name, "")}秒</small></td>'
                   f'<td>{btn("full", name)}<small>{st.get("full", {}).get(name, "")}秒</small></td></tr>' for name, _, _, text, _ in JOBS)
    vram = '　'.join(f'{"量子化版" if m == "quant" else "完全版"}：GPUのメモリ最大{s.get("peak_vram_gb", "?")}GB' for m, s in st.items())
    doc = f'''<!doctype html><html lang="ja"><head><meta charset="utf-8"><title>完全版と量子化版 その13</title>
<link href="https://fonts.googleapis.com/css2?family=M+PLUS+Rounded+1c:wght@800&family=Zen+Kaku+Gothic+New:wght@500;700&display=swap" rel="stylesheet">
<style>
body{{margin:0;font-family:'Zen Kaku Gothic New',sans-serif;color:#1d2a44;background:linear-gradient(160deg,#fff4ec,#ffe3ec 50%,#dcefff);min-height:100vh}}
header{{padding:22px 28px 4px}} h1{{font-family:'M PLUS Rounded 1c';margin:0;font-size:28px}} header p{{margin:6px 0 0;color:#52607a;font-weight:700;line-height:1.7;font-size:14px}}
section{{margin:14px 28px;background:#fff;border-radius:22px;padding:14px 18px;box-shadow:0 0 0 3px #fff,0 12px 30px rgba(27,35,64,.12)}}
table{{border-collapse:collapse;width:100%}} td,th{{border-top:1px solid #eef1f6;padding:8px;text-align:left;font-weight:700}} th{{font-size:13px;color:#52607a}} small{{color:#8a8399;font-weight:500;margin-left:6px}} .miss{{color:#b9a;font-size:12px}}
button{{border:0;cursor:pointer;color:#fff;background:linear-gradient(180deg,#ff94ae,#ff6f91);box-shadow:0 4px 0 #c9486c;border-radius:10px}} button.sm{{width:34px;height:34px}} button.on{{background:linear-gradient(180deg,#7fc4ff,#3fa9f5);box-shadow:0 4px 0 #1f75b8}}
</style></head><body><header><h1>その13　大型の完全版と量子化版の聞き比べ</h1>
<p>同じ台詞・同じ見本・同じ設定・同じ種で、モデルだけを替えた。息づかい・細部・声の安定を比べる。秒数は作るのにかかった時間。<br>{vram}</p></header>
<section><table><tr><th>台詞</th><th>量子化版（int8・今まで）</th><th>完全版</th></tr>{rows}</table></section>
<script>let a=null;function play(btn,src){{if(a)a.pause();document.querySelectorAll('.on').forEach(e=>e.classList.remove('on'));a=new Audio(src);btn.classList.add('on');a.onended=()=>btn.classList.remove('on');a.play()}}</script>
</body></html>'''
    (OUT / 'index.html').write_text(doc, encoding='utf-8')
    print('一覧', OUT / 'index.html')


if __name__ == '__main__':
    if '--page' not in sys.argv:
        generate(sys.argv[sys.argv.index('--model') + 1])
    page()
