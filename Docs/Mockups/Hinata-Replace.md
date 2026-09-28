# 旧ひなたを新デザインへ全面置き換え — 実装指示（2026-09-29）

加藤さんの指示：**旧ひなたはすべて置き換える**（モックも、ゲーム本体も）。新デザインは加藤さんが選んで決定済み（太い輪郭線のアニメ塗り、3頭身）。
モック（`Docs/Mockups/*.html`）は置き換え済み。ゲーム本体は、新試作・旧版の両方で旧ひなたを使わないようにする。

## 素材

`ArtSource/Hinata/gen-20260929/final/`（透過PNG。共通キャンバスにそろえ済み）

- `pose_*.png` 18種：496×615。**足元を下端中央にそろえてある**ので、切り替えても位置がずれない。
- `face_*.png` 18種：546×548。胸から上。吹き出しの顔アイコン、月報、会話ウィンドウに使う。
- 全身の設定画：`../cut/base_fullbody.png`。一覧：`../contact-sheet.png`。
- 作り方：`Tools/Cut-CharacterSheet.py`（切り抜き）、`Tools/Normalize-CharacterSet.py`（キャンバス統一）。来歴は`Docs/Reference-Asset-Provenance.md`。

`Assets/Sprites/Hinata/v2/`へ取り込む。設定：Sprite (2D and UI)、Mesh Type Full Rect、ミップマップなし、圧縮は高品質（線がにじまないこと）。
既存の`PersonaSpriteImporter`はファイル名のキーワードで自動割り当てするため、v2は専用フォルダに置き、割り当ては明示的に行う。

## 割り当て

- `NavigatorPersona`の6表情：normal→`face_normal`、proud→`face_proud`、worried→`face_worried`、alert→`face_alert`、relieved→`face_relieved`、sad→`face_sad`。
- 残りの表情12種とポーズ18種は、IDで引ける一覧を追加する（例：`Sprite Face(string id)`、`Sprite Pose(string id)`）。見つからなければ通常にフォールバック。
- セリフごとの表情・ポーズは`Docs/Voice/hinata-script-v1.csv`の「表情」「ポーズ」列を使う。

| 画面・場面 | ポーズ | 表情アイコン |
|---|---|---|
| 計画画面（通常） | `pose_fists` | `face_normal` |
| チュートリアル・依頼書 | `pose_point` | `face_normal` |
| 事件の発生 | `pose_startled` | `face_alert` |
| 発動演出（良い結果） | `pose_jump` | `face_doya` |
| 発動演出（悪い結果） | `pose_exhausted` | `face_sad` |
| 月報（良い／悪い） | `pose_peace`／`pose_think` | `face_sparkle`／`face_worried` |
| 年間評価 | `pose_jump` | `face_crying`（うれし泣き） |
| タイトル | `pose_wave` | — |
| 休む | `pose_coffee` | `face_relieved` |
| 疲労が高い月 | `pose_exhausted` | `face_sleepy` |

表示の高さの目安（1600×900基準）：計画画面430、依頼書440、事件330、発動380、タイトル700。ステージの枠内に収め、行動ボタンより奥に置く（加藤さんがモックで調整した配置）。

## 旧ひなたの撤去

- `Assets/Sprites/Hinata/hinata_normal.png`（旧暫定絵）をゲームから参照しない。ファイルは履歴として`ArtSource/`へ移す。
- 新試作・旧版（SampleScene）・タイトル・チュートリアル・テストの撮影画像まで、旧ひなたが写らないことを確認する。
- 旧版のアリア・クロエの素材はそのまま。

## 確認

- 全画面のスクリーンショットで、旧ひなたが残っていないこと、足元の位置がポーズ切り替えでずれないこと。
- 指の本数：`pose_point`・`pose_peace`・`pose_please`・`pose_salute`・`pose_wave`など手の見えるポーズを原寸で確認し、不自然なものは加藤さんに報告する（勝手に描き直さない）。
- PlayMode全件・Verify系を通す。
