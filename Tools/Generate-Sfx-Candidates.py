"""試聴専用の効果音を手続き生成する。Assetsには書かず、ゲームへ登録しない。"""
from pathlib import Path
import json
import math
import wave
import numpy as np

RATE = 48000
OUT = Path(__file__).resolve().parents[1] / "ArtSource" / "SfxCandidates"
NAMES = ["click", "confirm", "purchase", "growth", "count", "alert", "shield", "damage", "stamp", "clear", "transition"]
LABELS = ["通常ボタン", "決定", "設備導入", "ランクアップ", "数え上げ", "事件発生", "備え発動", "被害確定", "判子", "年度クリア", "画面遷移"]

def bell(seconds, frequency, bright, seed):
    t = np.arange(int(seconds * RATE)) / RATE
    result = np.zeros_like(t)
    # 金属・木材の倍音を重ね、単音の電子ビープと区別する。
    for k, ratio in enumerate([1, 2.76, 5.42, 8.93]):
        if frequency * ratio > RATE * .45:
            continue
        result += (0.46 ** k) * np.sin(2 * math.pi * frequency * ratio * t) * np.exp(-t * (5 + k * 6) / seconds)
    attack = 1 - np.exp(-t * (1100 if bright else 650))
    return result * attack

def noise(seconds, seed, low=0.03):
    rng = np.random.default_rng(seed)
    raw = rng.normal(0, 1, int(seconds * RATE))
    # 一次ローパス。ファイル生成なので、ゲーム実行時の負荷は無い。
    filtered = np.zeros_like(raw)
    for i in range(1, len(raw)):
        filtered[i] = filtered[i-1] + low * (raw[i] - filtered[i-1])
    return filtered / max(0.01, np.std(filtered))

def clip(kind, variant):
    bright = variant == 1
    lengths = [.15, .65, 1.05, 1.25, 1.3, .85, 1.15, .55, .48, 3.6, .4]
    seconds = lengths[NAMES.index(kind)]
    t = np.arange(int(seconds * RATE)) / RATE
    data = np.zeros_like(t)
    shift = 1.12 if bright else 1.0

    def add(at, signal, volume=1):
        start = int(at * RATE)
        take = min(len(signal), len(data) - start)
        if take > 0:
            data[start:start+take] += signal[:take] * volume

    def note(at, frequency, duration=.5, volume=1):
        add(at, bell(duration, frequency * shift, bright, 91), volume)

    if kind == "click":
        data = np.sin(2*math.pi*(520*t + 350*t*np.exp(-t*35))) * np.exp(-t*55)
        data += noise(seconds, 1, .16) * np.exp(-t*170) * .12
    elif kind == "confirm":
        note(0, 1175, .5); note(.075, 1760, .5, .45)
    elif kind == "purchase":
        for at, freq in zip([0, .05, .1, .2], [1700, 2110, 2800, 1320]): note(at, freq, .6, .5)
        data += noise(seconds, 3, .25)*np.exp(-t*38)*.18
    elif kind == "growth":
        for at, freq in zip([0, .17, .34], [659, 831, 988]): note(at, freq, .8, .8)
        note(.5, 1318, .65, .35)
    elif kind == "count":
        for at in np.arange(0, .81, .055): note(float(at), 1900, .024, .38)
        add(.89, np.sin(2*math.pi*600*np.arange(int(.32*RATE))/RATE)*np.exp(-np.arange(int(.32*RATE))/RATE*20))
    elif kind == "alert":
        phase = 2*math.pi*(420*t + 95*np.sin(2*math.pi*4*t)/(2*math.pi*4))
        data = .45*np.sin(phase)*(t<.45)*(1-np.exp(-t*80))
        data += .55*np.sin(2*math.pi*70*t)*np.exp(-t*12) + .12*noise(seconds, 7)*np.exp(-t*18)
    elif kind == "shield":
        data = noise(seconds, 8, .4)*np.exp(-((t-.09)/.065)**2)*.25
        note(.11, 1250, .95); note(.11, 2180, .9, .32); note(.15, 3340, .65, .14)
        data += .3*np.sin(2*math.pi*(170*t+8*np.sin(t*18)))*np.exp(-t*11)
    elif kind == "damage":
        data = .85*np.sin(2*math.pi*(73*t + .8*(1-np.exp(-t*35))))*np.exp(-t*16)
        data += .15*noise(seconds, 9)*np.exp(-t*25)
    elif kind == "stamp":
        data = np.sin(2*math.pi*120*t)*np.exp(-t*21) + .24*noise(seconds, 10, .1)*np.exp(-t*45)
        add(.025, noise(.15, 11, .3)*np.exp(-np.arange(int(.15*RATE))/RATE*50), .18)
    elif kind == "clear":
        for at, chord in [(0,[523,659,784]),(.42,[587,740,880]),(.84,[659,831,988]),(1.26,[784,988,1175]),(1.8,[1047,1318,1568])]:
            for freq in chord: note(at, freq, 1.5, .38)
        for at in [0,.84,1.8]:
            pulse = np.arange(int(.35*RATE))/RATE
            add(at, np.sin(2*math.pi*(90*pulse+2*(1-np.exp(-pulse*20))))*np.exp(-pulse*15), .18)
        note(2.15, 2093, 1.4, .18)
    else:
        data = noise(seconds, 12, .15)*np.exp(-((t-.13)/.085)**2)
    # 少量の残響とステレオの時間差。終端フェード・DC除去・余裕を取ったピーク。
    if kind not in ["click", "count", "damage", "transition"]:
        for delay, level in [(.035,.13),(.067,.08),(.102,.045)]:
            d = int(delay*RATE)
            data[d:] += data[:-d].copy()*level
    data -= np.mean(data)
    ramp = min(int(.005*RATE), len(data)//2)
    data[:ramp] *= np.linspace(0,1,ramp); data[-int(.035*RATE):] *= np.linspace(1,0,int(.035*RATE))
    peaks = dict(click=.12, confirm=.22, purchase=.23, growth=.25, count=.14, alert=.27, shield=.34, damage=.24, stamp=.31, clear=.32, transition=.16)
    data *= peaks[kind] / max(.01, np.max(np.abs(data)))
    right = data.copy()
    if bright:
        d = int(.0005*RATE); right[d:] = data[:-d]; right[:d] = 0
    return np.stack([data, right], axis=1)

def write_wav(path, data):
    with wave.open(str(path), "wb") as w:
        w.setparams((2, 2, RATE, 0, "NONE", "not compressed"))
        w.writeframes((np.clip(data,-1,1)*32767).astype("<i2").tobytes())

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    entries=[]
    for variant, title in enumerate(["A-soft", "B-bright"]):
        folder=OUT/title; folder.mkdir(exist_ok=True)
        reel=[]; position=0
        for i, kind in enumerate(NAMES):
            signal=clip(kind, variant); filename=f"{i+1:02d}-{kind}.wav"; write_wav(folder/filename,signal)
            entries.append(dict(set=title,number=i+1,scene=LABELS[i],file=f"{title}/{filename}",start=round(position,3),seconds=round(len(signal)/RATE,3),peak=round(float(np.max(np.abs(signal))),4),rms=round(float(np.sqrt(np.mean(signal**2))),4)))
            reel.extend([signal,np.zeros((int(.65*RATE),2))]);position+=len(signal)/RATE+.65
        write_wav(OUT/f"audition-{title}.wav",np.concatenate(reel))
    (OUT/"manifest.json").write_text(json.dumps(dict(generator="独自プロシージャル生成。AIサービス・第三者サンプルは使用していない。",approval="未承認・ゲーム未投入",sample_rate=RATE,entries=entries),ensure_ascii=False,indent=2),encoding="utf-8")
    print(f"22候補と試聴用2本を作成: {OUT}")

if __name__ == "__main__": main()
