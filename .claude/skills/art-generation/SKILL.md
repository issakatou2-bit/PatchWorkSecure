---
name: art-generation
description: PatchWorkSecureのキャラ絵・二人絵・強敵・カードなどをChatGPTの画像生成で作る・直すときの手順と点検。内蔵ブラウザでChatGPTを使うとき、ArtSource/ に絵を足すとき、強敵の擬人化を作るときに使う。
---

# 絵づくり

作る・直す前に、必ず `Docs/Art/Image-Generation-Rules.md` を全部読み、その手順と点検に従う。強敵は `Docs/Rivals-Persona-Design-2026-10-03.md` も読む。

要点：
1. 同じ絵柄は同じChatGPTの会話の続きで頼む。基準の絵はライブラリの決まった3枚だけを使い、加藤さんの他のファイルには触れない。
2. 保存は `touch "$CLAUDE_SCRATCH/dl-marker"` → 画像を開いて保存ボタン → `sh Tools/Take-ChatGPTDownload.sh <保存先>`。
3. 保存した絵は自分で開き、手と指・持ち物の矛盾・頭身と輪郭・表情の強さ・対等な関係・文字・似せないを点検してから見せる。
4. フォルダのREADMEに採用・不採用と理由を書いてコミットする。

加藤さんの指摘で新しく分かったことは、`Docs/Art/Image-Generation-Rules.md` の点検に書き足す。
