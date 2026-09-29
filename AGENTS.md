# PatchWorkSecure — Codex 引き継ぎ

このファイルは起動時に自動で読み込まれ、**毎ターンの使用量に直結する**。規則・構成・現状の要約だけを書き、経緯・計測値・詳細は`Docs/Dev-Status.md`など`Docs/`に置く（目安：日本語5,000字以内）。
**同じ内容を`CLAUDE.md`（Claude Code用）にも置いてある。エージェント名以外は常に同一に保つこと**（片方だけ更新すると、次に作業するエージェントが古い前提で動く）。
このプロジェクトはClaude CodeとCodexが交互に作業している。**作業を始める前に`git status`で相手の未コミット作業が無いか確認し、あれば検証してからコミットすること**（2か月分が未コミットのまま残っていたことがある）。

## 0. プロジェクト

PatchWorkSecure — 企業の情シス担当として日常業務をこなしながら、サイバー攻撃から会社を守るセキュリティ教育シミュレーション。
「なにごともない、いつもの平穏なオフィスの日常をツギハギ（PatchWork）しながら守り抜く」。
開発者は加藤さん（個人開発、Steam配信も視野）。思想の芯：①人のためのセキュリティ ②性弱説（ミスを責めず、致命傷にしない構造） ③透明性と信頼。

## 1. 技術構成

- Unity `6000.5.6f1`（URP 2D）、`C:\Projects\PatchWorkSecure`、GitHub `issakatou2-bit/PatchWorkSecure`
- 作業はすべて`feature/title-quiz-ending-flow`。`main`は初期状態のまま、PRは未作成（この環境に`gh`が無い）。
- 旧版の書体：TMP `Assets/Fonts/meiryo SDF`（動的生成、TMPの既定兼フォールバック）。新試作の書体は下記。

## 2. 2つのゲームが並存（どちらを本命にするかは加藤さんが未決定）

| シーン | 中身 | コード | 詳細 |
|---|---|---|---|
| `Assets/Scenes/CompanyYear.unity` | **現在の試遊対象**「情シスの一年」v0.15。12か月、工数と予算で改善の順番を選ぶ | `Assets/Scripts/CompanyOps/`、生成は`Assets/Editor/CompanyOpsSceneBuilder.cs` | `Docs/CompanyYear-Prototype.md` |
| `Assets/Scenes/SampleScene.unity` | 旧版。36期、攻撃10種×対策8種、パリィ、教育クイズ。オフィス背景に改修済み | `Assets/Scripts/`直下、生成は`Assets/Editor/SceneBuilder*.cs` | `Docs/Office-Rework.md` |

- 新試作：`OpsCatalog`(内容) → `OpsState`(Unity非依存のルール) → `OpsGame`(画面。partialで分割) / `OpsSaveStore`(保存)。旧版の`GameState`には依存しない。キャラデータの共有は`NavigatorPersona`、表示用の`OpsPortraitAnimator`／`OpsPortraitMotion`も両版で再利用。
- 新試作の見た目：本文 Zen Kaku Gothic New Medium、見出し・数値・操作 M PLUS Rounded 1c Bold。計画・事件・初回ガイド・月報・年間評価・設定を承認モックへ移行し、共通UIキットを適用。`Docs/UI-Kit-and-Next2-2026-09-28.md`。
- Windows版：`Builds/CompanyYear/PatchWorkSecure-Year.exe`（gitでは追跡しない）。
- 旧版は自動で新試作へ置き換えない。9/13の改修以降は機能追加をせず、回帰テストだけを続けている。
- 旧版の主なファイル：`GameData`(マスターデータ) / `GameState`(純粋C#のルール) / `GameManager`(UIとフェーズ進行) / `AudioManager` / `UIEffects`(フラッシュ・シェイク・バナー・浮遊テキスト等) / `NavigatorPersona` / `EducationTracker`(クイズ・CSV) / `GamePresentation` / `DefenseGlyph` / `DefenseDetailsTrigger`。
- 旧版の流れ：タイトル →（教育モードのみ事前クイズ）→ [雑務→攻撃判定→選択→パリィ→結果]の繰り返し → エンディング →（事後クイズ）→ まとめ。

## 3. 実装ルール（2026-09-28に見直し）

### デザインの方針

- **参考はウマ娘・シャインポスト。** 新試作は仕組みがウマ娘の育成（月＝ターン、行動＝トレーニング、社員支援＝サポートカード、事件＝レース、年間評価＝育成ランク）と同じなので、**画面の構造と表現**を参考にする。他作品の画像・ロゴ・固有デザインは複製しない。
- **文字で伝えていることを、絵・動き・音に置き換える。** 数値はランク文字・ゲージ・アイコンで、説明は必要時だけ開く。キャラを箱に閉じ込めず、画面に大きく出す。
- **見た目の大きな変更は、HTMLモックで加藤さんの承認を得てからUnityへ移す。** 計画画面は9/28に承認済み。実装指示とモック：`Docs/Mockups/README.md`（HTMLをブラウザで開くと同じ画面が見られる）。
- **今後の見た目の変更はすべてモックと並べて確認する。** `Tools/Compare-Mock.py`で変更前・変更後の比較画像を残し、差を1つずつ確かめてから完了とする。質感を単色で代用しない。Polish-4は`Artifacts/Polish4/`へ保存。
- **UI画像（帯・バッジ・ボタン・ランク文字・アイコン）は作ってよい。** 置き場所は`Assets/Art/UI/`。作り方・出典・ライセンスは`Docs/Reference-Asset-Provenance.md`に記録する。組み込みの`UI/Skin`・`Knob`・`UI.Shadow`も引き続き使える。独自シェーダーは必要なときだけ（URPのUIで動作をスクリーンショットで確認）。
- **絵文字は使わない**（フォントに無く、文字化けする）。アイコンは図形かUI画像で描く。
- **予測と確定を見た目で区別し、未確認を侵害確定のように見せない。** 派手に見せるための架空の数値は作らない（教育ゲームとしての誠実さ）。

### 両方のゲームに共通

1. **ロジックとUIを分離する。** バランス調整は`GameData`／`OpsCatalog`の数値だけで完結させる。
2. **UIはコード・生成スクリプト側を直す。** Inspectorの手作業やOnClickの手動登録はしない。
3. **ステータスは「項目名＋数値」を一緒に更新する**（数値だけで上書きしてラベルが消えた不具合があった）。
4. **キャラは`NavigatorPersona`経由にする。** 表情とセリフはアセット側に持たせ、空欄は共通セリフへフォールバック（`Pick()`）。
5. **UIを変えたら、憶測で「できたはず」と言わない。** テストとスクリーンショットで裏を取る。

### 旧版（SampleScene）固有

- ボタンは3点セット：①`GameManager`にフィールド ②`WireButtons()`で`AddListener` ③`SceneBuilder`で生成して`SetRef()`。新しいパネルは`HideAllPanels()`にも追加する。リストUIはプレハブ＋動的Instantiate。
- 生成後は`EditorSceneManager.SaveScene()`で保存する（忘れて空シーンのまま数セッション進んだことがある）。
- LayoutGroupで`childControlWidth/Height=false`なら子のサイズを自分で設定する（0サイズで見えなくなる）。
- 画面シェイクはCanvasではなく`ShakeRoot`を動かす（Overlay Canvasは動かせない）。

## 4. 検証

Unity Editorが起動中だとバッチモードは失敗する。
**バッチ実行後は`Library/LastSceneManagerSetup.txt`が空になり、無題のシーンが開いて「何も変わっていない」ように見える。実行後は`CompanyYear`（または`SampleScene`）を開き直すよう、必ず伝えること。**

```powershell
# PlayModeテスト（画面を撮影するので -nographics は付けない）。9/29時点で174件
& "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe" -batchmode -runTests -testPlatform PlayMode `
  -projectPath "C:\Projects\PatchWorkSecure" -testResults "<出力先>\test_results.xml" -logFile "<出力先>\batch_log.txt"
# コンパイル確認だけなら -batchmode -quit。メニューの処理は -executeMethod PatchWorkSecure.EditorTools.SceneBuilder.BuildScene
```

Unity不要（Editorの起動中でも可）。**数値を変えたら必ず流す。**
`./Tools/Verify-Core.ps1`（旧版）、`./Tools/Verify-CompanyOps.ps1`（新試作）、`./Tools/Verify-CompanyOps-Personas.ps1`（3タイプの仮想方針）、`./Tools/Verify-CompanyOps-Depths.ps1`（3視点×3深度・900年度）、`./Tools/Compile-Office.ps1`（3アセンブリのコンパイル）。
`.ps1`は**BOM付きUTF-8**で保存する（PowerShell 5.1はBOMなしをShift-JISとして読み、構文エラーになる。`.editorconfig`で固定済み）。

| 置き場所 | アセンブリ | 注意 |
|---|---|---|
| `Assets/Scripts/` | `PatchWorkSecure` | 本体 |
| `Assets/Editor/` | `PatchWorkSecure.Editor` | Editor限定 |
| `Assets/Tests/` | `PatchWorkSecure.Tests` | `includePlatforms`は**空**にする（`["Editor"]`だとPlayModeテストが0件のまま成功扱いになる） |

## 5. 使用量を抑える作業ルール（品質は落とさない）

- 大きいファイル（`SceneBuilder.cs` 82KB、`GameManager.cs` 61KB、`OpsGame.cs` 40KB）は全文を読まない。Grepで位置を特定し、必要な範囲だけを読む。
- ログは全文を読まない。`test_results.xml`は`total/passed/failed`だけ、`batch_log.txt`は`error CS|Exception|warning CS`で絞る。
- 画面の確認には、テストが`Artifacts/CompanyOps/`・`Artifacts/OfficeReview/`に保存したスクリーンショットを使い、必要な枚数だけを見る。
- 経緯・計測値・調査結果は`Docs/`に書く。このファイルに足すのは「次の作業者が毎回知るべき規則と現状」だけ。
- 同じ検証の重複実行や、不要なサブエージェントの起動はしない。報告は結論から短く書く。
- **テストは「途中は関係分、最後に全件」**：指示に複数の項目があるときは、各項目のコミット前に関係するテストとコンパイル確認だけを流し、指示のまとまりの最後に1回、PlayMode全件とVerify系を流す。Windows版の作り直しは頼まれたときだけ。全件を毎項目で流すと、確認に時間と使用量の大半を使ってしまう（9/29に加藤さんから指摘）。
- **加藤さんへの返答は常に日本語**。英語に切り替えない。

## 6. 現状（2026-09-29）— 詳細は`Docs/Dev-Status.md`

- 新試作v0.15：Next-5①土台、②点数の結果反映、③感染の封じ込めを別コミット。④の確認記録は`Docs/Next-5-Implementation-2026-09-29.md`。開始→20秒→S/A/B/Cの結果、委任50点、4部屋×5台・調査2回・端末/部屋の停止、監視/分離の備え、台本mg_*16行と手本の★演出を実装。Windows版は据え置き。
- **50点は現行結果そのまま**。他の点数は係数を掛け、被害/停止ごとに[min(現行結果,公開見積もり下限),max(現行結果,公開見積もり上限)]へ収める（加藤さんの追加指示）。元から幅外の結果をさらに広げない。未確認の真相は見積もりに漏らさず、正常事件は感染なし・正常端末の停止で減点。バックアップは既存計算のみで二重にしない。
- 委任50点の導入前後で900年度と10,779月のCSVが完全一致。限定50.25%、非優越候補平均1.980。現行閾値2135/1880/1563/1151でSS10.22%。**Depthsは山場導入前との最適化完走率58%→85%（+27pt）で既知の失敗**。指定数値維持は承認済み。基準を緩めず成功扱いにしない。他のVerify3系統は成功。
- Polish-4の表示16件＋追加4件を個別コミット。透過泡、配置、札、単位、空の備え欄、タイトルの輪郭と厚みを修正。未導入＋枠は導入画面だけ、備えは白/緑/水色で左詰め、把握ゲージ208px・説明1行、未確認は紺14px太字。変更前後20組をモックと並べ、`Artifacts/Polish4/`へ保存。数値・ルール・Windows版は変更なし。
- 最新PlayMode全174件を1回実行し172成功・2失敗（表示20件は全成功）。演出完了の待機上限と仮想入力のフォーカス依存をテスト側だけで修正し、失敗2件は個別各1/1成功。3アセンブリのコンパイル成功。全件再成功とは区別する。3視点各5年度・180か月の自動画面操作も成功。**人間の初見プレイ、楽しさ・学習効果の測定ではない**。詳細は`Docs/Polish-4-Implementation-2026-09-29.md`。
- Next-3/4、共通UI、次画面、設備/社員支援、ランク恩恵、Quick-Wins-2は維持。新年度だけ山場ルール、旧保存は従来の得点/ランク。見込みは公開Estimateだけ。旧版は変更せず回帰検証。計画行動への社員の追加効果と顔マークは未実装。
- ひなたは新デザインの表情18・ポーズ18を両版へ適用、基本ポーズの目口差分と1枚絵フォールバック。承認済みタイトルKV、SEはB-brightと同系統のミニゲーム音、BGMはGemini/Lyriaの生成2曲。
- 声はElevenLabs「Hinata V9-2」133行。音声なし/消音でも字幕・表情・ポーズと操作で進む。mg_*は場面専用（メール/MFA4行は登録のみ）。音源・音声入りResourcesはgit除外、無料プランの私的試遊限定で**配布・公開・動画投稿は禁止**。公開用音源は利用条件を満たして再制作。
- 過去の計測・UI評価は`Docs/Next-4-Implementation-2026-09-29.md`、`Docs/Evaluation-2026-09-29.md`。旧閾値のSS28.78%・旧限定85〜90%と現在を混同しない。

## 7. 次にやること（優先順）

0. **感染の封じ込めをUnityで私的試遊**：手本Bと比較して見つけやすさ、切り離し/調査/部屋停止の使い分け、遮断と正常停止の手触り、20秒の忙しさ、音と声の重なりを評価する。委任50点は維持。年間数値の偏りは既知として別に評価する。
1. 次のミニゲーム（Cメール/D多要素認証/Eログ/F2はめ込み/G復旧）と施策カードはHTML試作・設計で、Unity未実装。加藤さんの試遊・承認後に順番を決める。`Docs/Minigame-Design-2026-09-29.md`、`Docs/Mockups/proto-*.html`。Next-5の土台とBを作り直さない。
2. 声133行・目口・SE/BGMのミックスを確認。配布前に公開可能な音声を再制作し、VoiceTestのResourcesは除く。Windows版更新は依頼時だけ。
3. Next-3/4/Next-Screensの実装済み項目を繰り返し作らない。旧保存と新年度を区別。臨時予算1万円は維持し、変更は別途設計・比較。検証基準を勝手に緩めない。
4. 難易度、兆候の連鎖、複数年、図鑑・会話、追加キャラは設計候補。承認後に進める。本命の旧版/新試作の選択は未決定。
5. Player Settings（Steam向け）、ブランチ整理。旧版を新試作へ自動置換しない。`planning-bubbles.html`と`title-screen-kv.html`はNext-3で承認・実装済み。
- やり込み（終わりなき年度・自己ベスト・称号）の設計は `Docs/Endless-Years-Design-2026-09-29.md`（試作は `proto-a-cards.html`）。
- 原則をゲーム全体に通す表とひなたの格言は `Docs/Principles-in-Play-2026-09-29.md`。
- Polish-4の16件と追加4件は実装・比較済み。旧い灰色の未導入札や90pxの把握カードに戻さない。今後の見た目変更も`Tools/Compare-Mock.py`で前後を保存し、加藤さんの試遊で残る差を確認する。Windows版更新は依頼時だけ。

## 8. 対話スタイル

加藤さんは率直・簡潔で実質的な回答を好み、汎用的・無難な成果物を嫌う。「面白いゲーム」への関心が強く、確率計算の冷たさを避けたい。コード内コメントとUI文字列は日本語で統一する。
