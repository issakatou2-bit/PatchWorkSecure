# Irodori-TTSで声を作るときの共通部品（10/3）。今までの Tools/Irodori-*.py で毎回書いていた処理をまとめたもの。
# 決まり（読み方・説明の書き方・見本・こもり・速さ）は Docs/Voice/Irodori-Direction-Rules.md。新しい試しは Tools/Irodori-Make.py と、
# Docs/Voice/jobs/*.json（作る声と台詞の一覧）で作る。Irodori-TTSの仮想環境のPythonで動かす。
import difflib, json, os, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
IRODORI = Path(r'C:/Users/issak/Tools/Irodori-TTS')
MODELS = {
    'large': 'Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only',  # Gemmaの利用規約
    'small': 'Aratako/Irodori-TTS-v4.1-Small',  # MIT
    'full': 'Aratako/Irodori-TTS-v4-Large',  # Gemmaの利用規約。メモリを大きく使うので、Tools/Watch-FullTest.ps1 で見張る
}
CLEAR = '近いマイクで録った、こもりのないクリアな音質。'
_infer = None


def _runtime():
    # 同じ処理の中でモデルを切り替えると落ちるので、1回の実行では1つのモデルだけを使う
    global _infer
    if _infer is None:
        sys.path.insert(0, str(IRODORI))
        import infer
        from irodori_tts.inference_runtime import get_cached_runtime

        class Cached:
            @staticmethod
            def from_key(key):
                return get_cached_runtime(key)[0]
        infer.InferenceRuntime = Cached
        _infer = infer
    return _infer


def synth(model, text, caption, out, seed, refs=None, extra=()):
    # refs が空なら、見本なし（文字の説明だけ）で作る
    out = Path(out)
    out.parent.mkdir(parents=True, exist_ok=True)
    ref_args = ['--ref-wavs', *map(str, refs)] if refs else ['--no-ref']
    sys.argv = ['infer.py', '--hf-checkpoint', MODELS[model], '--model-precision', 'bf16', '--text', text, '--caption', caption,
                *ref_args, '--seed', str(seed), *extra, '--output-wav', str(out)]
    _runtime().main()
    return out


def muffled(path):
    # 0.5秒ごとに、4kHzより上の音の割合を調べる。声のある区間で0.001未満が1つでもあれば「こもり」（通話のような音）
    import numpy as np, soundfile as sf
    d, sr = sf.read(str(path))
    d = d.mean(1) if d.ndim > 1 else d
    w, bad, n = int(sr * .5), 0, 0
    for s in range(0, max(len(d) - w, 1), w):
        x = d[s:s + w]
        if float((x ** 2).mean()) < 1e-4:
            continue
        sp = np.abs(np.fft.rfft(x)) ** 2
        f = np.fft.rfftfreq(len(x), 1 / sr)
        n += 1
        bad += float(sp[f > 4000].sum() / sp.sum()) < 0.001
    return bad, n


def whisper():
    # 書き起こし（faster-whisper）。GPUの部品（cublas）はPyTorchに同梱のものを使う
    import torch
    lib = os.path.join(os.path.dirname(torch.__file__), 'lib')
    os.add_dll_directory(lib)
    os.environ['PATH'] = lib + os.pathsep + os.environ['PATH']
    from faster_whisper import WhisperModel
    m = WhisperModel('large-v3-turbo', device='cuda', compute_type='int8_float16')
    return lambda p: ''.join(s.text for s in m.transcribe(str(p), language='ja', beam_size=5)[0])


def norm(s):
    return ''.join(c for c in s if c.isalnum())


def similarity(heard, expected):
    # 書き起こしと台詞の近さ（0〜1）。同じ音の別の字（三行→産業）でも下がるので、印を付けるだけに使う
    return difflib.SequenceMatcher(None, norm(heard), norm(expected)).ratio()
