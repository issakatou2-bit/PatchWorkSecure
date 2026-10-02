# かのん・エンジニアの本編用の声を、決めた声（見本＋キャプション）でまとめて作る。
# 使い方（Irodori-TTSの仮想環境のPython。モデルごとに別々に実行する。同じ処理でモデルを切り替えると落ちる）:
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-CharacterLines.py --char kanon
#   C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Irodori-CharacterLines.py --char eng
# 台本：Docs/Voice/kanon-engineer-script.csv。出力：Assets/Audio/CompanyYear/Voice/{Kanon,Engineer}/<id>.wav
# 声の決め方とライセンスは Docs/Characters-Ideas.md と Docs/Reference-Asset-Provenance.md。
import csv, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
VOICES = {
    'kanon': dict(prefix='kanon_', folder='Kanon', model='Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only',
                  ref=ROOT / 'ArtSource/Voice/Irodori-refs/secretary-ref.wav', extra=[],
                  caption='はっきりした発声の、仕事ができる女性秘書。少し早口で、さばさばと辛口に話すが、根は優しい。'),
    'eng': dict(prefix='eng_', folder='Engineer', model='Aratako/Irodori-TTS-v4.1-Small',
                ref=ROOT / 'ArtSource/Voice/Irodori-refs/engineer-ref.wav', extra=['--duration-scale', '0.8'],
                caption='若い女性。眠たげで低めの落ち着いた声。淡々と、抑揚少なめに話すが、どこか優しい。' + CLEAR),
}
SEED = 20261002


def main():
    who = sys.argv[sys.argv.index('--char') + 1]
    v = VOICES[who]
    sys.path.insert(0, str(IRODORI))
    import infer
    from irodori_tts.inference_runtime import get_cached_runtime

    class Cached:
        @staticmethod
        def from_key(key):
            return get_cached_runtime(key)[0]
    infer.InferenceRuntime = Cached
    out_dir = ROOT / 'Assets/Audio/CompanyYear/Voice' / v['folder']
    out_dir.mkdir(parents=True, exist_ok=True)
    rows = list(csv.DictReader(open(ROOT / 'Docs/Voice/kanon-engineer-script.csv', encoding='utf-8')))
    done = 0
    for row in rows:
        if not row['id'].startswith(v['prefix']):
            continue
        out = out_dir / f"{row['id']}.wav"
        if out.exists() and '--force' not in sys.argv:
            continue
        text = row['読み（声用。空ならセリフのまま）'] or row['セリフ']
        sys.argv = ['infer.py', '--hf-checkpoint', v['model'], '--model-precision', 'bf16', '--text', text, '--caption', v['caption'],
                    '--ref-wav', str(v['ref']), '--seed', str(SEED), *v['extra'], '--output-wav', str(out)]
        infer.main()
        done += 1
    print('生成', done, '本 →', out_dir)


if __name__ == '__main__':
    main()
