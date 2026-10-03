# 宣伝の30秒動画（試作）をつなぐ（10/3）。素材はCodexがNext-18で撮った連番（Artifacts/Promo/shots/）と、鳴った音の一覧（sounds.json）。
# 構成は Docs/Store-and-Promo-Plan-2026-10-02.md の2章。音の決まり：効果音と、かのん・りりぃの声（Irodori、配布可）だけ。
# ひなたの声（ElevenLabsの無料枠）とBGM（利用条件の確認前）は入れない。ひなたの台詞は字幕で出す。
# 使い方：C:/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe Tools/Make-PromoVideo.py
# 出力：Artifacts/Promo/promo-30s-draft.mp4（gitで追跡しない）
import json, os, shutil, subprocess
from pathlib import Path
import numpy as np, soundfile as sf

ROOT = Path(__file__).resolve().parent.parent
PROMO = ROOT / 'Artifacts' / 'Promo'
FPS, SR = 30, 48000
FONT = (ROOT / 'Assets/Fonts/CompanyYear/RoundedMplus1c-Bold.ttf').as_posix().replace(':', '\\:')
FFMPEG = subprocess.run(['py', '-3', '-c', 'import imageio_ffmpeg as f;print(f.get_ffmpeg_exe())'], capture_output=True, text=True).stdout.strip()
VOICE = ROOT / 'Assets/Audio/CompanyYear/Voice'
# （場面, 使い始めのコマ, コマ数, 字幕, 足す声（ファイル, 場面の頭から何秒））
CUTS = [
    ('01-office', 1, 90, 'なにごともない、いつもの平穏。', None),
    ('02-planning', 61, 120, '情シスの仕事は、守る順番を選ぶこと', None),
    ('03-incident', 61, 120, 'でも、事件はいつも急に来る', None),
    ('04-mini-b', 61, 60, '広がる前に、封じ込める', None),
    ('05-mini-c', 61, 60, '怪しいメールを、見分ける', None),
    ('06-mini-e', 61, 60, 'ログから、手がかりを探す', None),
    ('07-rivals', 31, 120, 'りりぃ「正体、分かった。……思ったより、地味な手口。」', (VOICE / 'Engineer/eng_rival_reveal.wav', 0.3)),
    ('08-pairs', 1, 180, 'かのん「数字のことは、わたしに任せて。あなたたちは、守ることに集中して。」', (VOICE / 'Kanon/kanon_opening.wav', 0.2)),
    ('09-title', 1, 120, 'PatchWorkSecure　開発中', None),
]
USE = {'sfx', 'sfx-generated'}  # 動画に使ってよい音の種類


def esc(t):
    return t.replace('\\', '\\\\').replace(':', '\\:').replace("'", "\\'").replace('%', '\\%')


def main():
    tmp = PROMO / 'build'
    shutil.rmtree(tmp, ignore_errors=True)
    tmp.mkdir(parents=True)
    n, mix, texts = 0, [], []
    for shot, start, count, text, voice in CUTS:
        t0 = n / FPS
        for k in range(count):
            src = PROMO / 'shots' / shot / f'frame_{start + k:05d}.png'
            n += 1
            os.link(src, tmp / f'{n:05d}.png')
        texts.append((t0, n / FPS, text))
        cues = json.loads((PROMO / 'shots' / shot / 'sounds.json').read_text(encoding='utf-8'))['sounds']
        for c in cues:
            if c.get('kind') in USE and c.get('operation') == 'play' and start <= c['frame'] < start + count:
                mix.append((t0 + (c['frame'] - start) / FPS, ROOT / c['file'], float(c.get('volume', 1))))
        if voice:
            mix.append((t0 + voice[1], voice[0], 1.0))
    total = n / FPS
    audio = np.zeros(int(total * SR) + SR, dtype=np.float32)
    for at, f, vol in mix:
        d, s = sf.read(str(f), dtype='float32', always_2d=True)
        d = d.mean(1)
        if s != SR:
            d = np.interp(np.arange(0, len(d), s / SR), np.arange(len(d)), d).astype(np.float32)
        i = int(at * SR)
        j = min(len(audio), i + len(d))
        audio[i:j] += d[:j - i] * vol
    peak = float(np.abs(audio).max()) or 1.0
    sf.write(tmp / 'audio.wav', audio[:int(total * SR)] / max(peak, 1.0) * 0.9, SR)
    draw = []
    for a, b, t in texts:
        size = 64 if t.startswith('PatchWorkSecure') else 46
        draw.append(f"drawtext=fontfile='{FONT}':text='{esc(t)}':fontsize={size}:fontcolor=white:box=1:boxcolor=0x1d2a44@0.6:boxborderw=18:"
                    f"x=(w-text_w)/2:y=h-text_h-90:enable='between(t,{a:.2f},{b - 0.02:.2f})'")
    a = texts[-1][0]
    draw.append(f"drawtext=fontfile='{FONT}':text='{esc('画像・音声の一部に生成AIを使用しています')}':fontsize=26:fontcolor=white:borderw=4:bordercolor=0x1d2a44:"
                f"x=w-text_w-40:y=h-text_h-30:enable='gte(t,{a:.2f})'")
    out = PROMO / 'promo-30s-draft.mp4'
    subprocess.run([FFMPEG, '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', str(tmp / '%05d.png'), '-i', str(tmp / 'audio.wav'),
                    '-vf', ','.join(draw), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', '-c:a', 'aac', '-b:a', '192k', '-shortest', str(out)], check=True)
    shutil.rmtree(tmp, ignore_errors=True)
    print('動画', out, f'{total:.1f}秒', '音', len(mix), '個')


if __name__ == '__main__':
    main()
