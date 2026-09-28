# ひなた 設定画・表情差分・ポーズ差分 — 画像生成の指示文（2026-09-28）

Claude（Opus 5.5）は画像を生成できないため、ChatGPTなど画像生成のできるツールで使う指示文として用意した。
衣装は`Docs/Mockups/hinata-outfit-a.html`の案A。**まだ案であり、採用は加藤さんが絵を見て決める。**
基本設定：ピンクのツインテール、黒いリボン、赤い瞳、元気で明るい。学生服にしない。

## 使い方（同じキャラに見せるコツ）

1. **最初に設定画を1枚作る**（下のA）。気に入るまで作り直す。
2. 2枚目以降は、**必ずAの画像を添付して**「この画像と同じキャラクター」と指定する。
3. 1回に1枚ずつ作る。気に入らない部分は「髪型はそのままで、口だけ〜」のように部分的に直させる。
4. 背景は「transparent background」か白を指定する。白で作った場合は、後で透過処理する。
5. 表情差分は、**同じ構図・同じ大きさ・同じ位置**で作らないと、ゲーム内で切り替えたときに体がずれる。

## A. 設定画（最初の1枚）

```
Anime-style character design sheet of one girl, full body, front view, standing naturally, plain white background.
Pink hair in twin tails tied with black ribbons, red eyes, bright cheerful smile, energetic and friendly, early twenties office worker.
Outfit: white round-collar blouse with a thin pink ribbon tie, pastel sky-blue short cardigan with slightly long sleeves,
navy high-waist pleated knee-length skirt, black tights, white low-heel strap shoes,
employee ID card on a pink lanyard. Clean line art, soft cel shading, game character sprite quality, no text.
```

## B. 表情差分（ゲームで使う6種）

Aを添付し、次の共通文に各表情の文を足す。

```
Same character as the attached image, same outfit, same pose, same framing and size, upper body to knees, plain white background.
Change only the facial expression:
```

| ファイル名 | 表情 | 追加する文 |
|---|---|---|
| `hinata_normal` | 通常 | `a gentle, cheerful smile, relaxed eyebrows` |
| `hinata_proud` | 得意・成功 | `a proud confident grin with one eye winking, sparkle of joy` |
| `hinata_worried` | 心配 | `a worried look, eyebrows raised in the middle, small frown, eyes looking slightly aside` |
| `hinata_alert` | 警戒・緊急 | `a surprised alert face, wide eyes, small open mouth, a drop of sweat on the temple` |
| `hinata_relieved` | 安心 | `a relieved soft smile with closed eyes, cheeks slightly flushed` |
| `hinata_sad` | 落ち込み | `a sad disappointed face, lowered eyebrows, teary eyes, mouth pressed small` |

## C. ポーズ差分

Aを添付し、次の共通文に各ポーズの文を足す。

```
Same character as the attached image, same outfit, full body, plain white background. Pose:
```

| 用途 | ポーズ | 追加する文 |
|---|---|---|
| 通常・案内 | 両手ガッツポーズ | `both fists raised near her chest, cheerful, slight lean forward` |
| 説明・チュートリアル | 指さし | `pointing to the side with her right index finger, left hand on hip, explaining something` |
| 計画・仕事中 | ノートPCを抱える | `holding a closed laptop against her chest with both arms, a sticker on the laptop` |
| 事件の発生 | 驚いて身を引く | `startled, leaning back with both hands raised in front of her, surprised face` |
| 成功・年度クリア | ジャンプして喜ぶ | `jumping with joy, one arm raised high, big smile, hair and skirt fluttering` |
| 考える | あごに指 | `thinking pose, index finger on her chin, looking up, slightly puzzled` |

## D. まばたき・口パク用（`Docs/Mockups/Next-Screens-2.md`の⑤）

Bの`hinata_normal`を添付し、「同じ画像のまま、目だけ（口だけ）を変える」と指定する。

```
Exactly the same image as attached. Do not change anything except the eyes: half-closed eyes.
```

`half-closed eyes`の部分を、`fully closed eyes`、`mouth open as if talking`、`mouth slightly open`に替えて4枚作る。
生成では細部がずれやすい。ずれた場合は、顔の部分だけを切り出して重ねる方式にする（ゲーム側で対応できる）。

## できた絵の扱い

- 生成物は暫定素材として`ArtSource/`に保存し、ゲームに入れる前に加藤さんが確認する。
- 採用したら、作成日・ツール・指示文を`Docs/Reference-Asset-Provenance.md`に記録する。
- 透過とファイル名が揃えば、「PatchWorkSecure → キャラ立ち絵を取り込む」で表情の枠に自動で割り当てられる。
