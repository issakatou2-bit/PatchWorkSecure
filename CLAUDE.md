# PatchWorkSecure — Claude Code 引き継ぎ

このファイルは起動時に自動で読み込まれ、**毎ターンの使用量に直結する**。規則・構成・現状の要約だけを書き、経緯・計測値・詳細は`Docs/Dev-Status.md`など`Docs/`に置く（目安：日本語5,000字以内）。
**同じ内容を`AGENTS.md`（Codex用）にも置いてある。エージェント名以外は常に同一に保つこと**（片方だけ更新すると、次に作業するエージェントが古い前提で動く）。
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
| `Assets/Scenes/CompanyYear.unity` | **現在の試遊対象**「情シスの一年」v0.14。12か月、工数と予算で改善の順番を選ぶ | `Assets/Scripts/CompanyOps/`、生成は`Assets/Editor/CompanyOpsSceneBuilder.cs` | `Docs/CompanyYear-Prototype.md` |
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
# PlayModeテスト（画面を撮影するので -nographics は付けない）。9/29時点で145件
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

- 新試作v0.14：Next-4①ルール、②画面、③声を別コミット。四つの山場目標と報酬、年間SS/S/A/B/C、目標と見込み・達成カットイン・盾の勲章・未達の改善候補、声12行追加を実装。④の比較・撮影記録は`Docs/Next-4-Implementation-2026-09-29.md`。Windows版は据え置き。
- 新規年度だけ`peakGoalRules=1`。旧保存は目標なし・従来の得点/ランク。数値はOpsCatalog。見込みは公開Estimateだけで出し、未来の事件や確定被害を先読みしない。未来の山場は現在の公開見積もりを将来の目標に当てはめた参考と明記する。
- 山場の達成率は深度0/深度1・2の集約8比較で目安±10ポイント以内。900年度で限定50.25%、非優越候補平均1.980。①前は山場なしの同じ種・同じ方針で再計測する。
- **完走率差は最適化方針で58%→85%（+27pt）、SSは28.78%で目安約10%を超える**。加藤さんは結果確認後「指定数値を維持して試遊」を選択。Depthsは±5pt基準で失敗するまま残し、成功扱いにしない。Verifyの他3系統とコンパイルは成功。
- PlayMode全145件を1回実行して144成功。残った仮想キー入力の1件はデバイス指定を明示して個別再実行1/1成功、最後の表示補正と再撮影もNext4UIの2/2成功。最終版の全件一括成功とは区別する。3視点各5年度の画面操作は180か月・2,091操作を完走、文字の収まり指摘0。**人間の初見プレイ・楽しさ・学習効果の測定ではない**。
- Next-3の表示8件・判断A〜E・月4個の泡・承認済みタイトルKV、共通UI、次画面、設備/社員支援、ランク恩恵、Quick-Wins-2は維持。旧版は変更せず回帰検証。計画行動への社員の追加効果と顔マークは未実装。
- ひなた：新デザインの表情18・ポーズ18を両版へ適用。基本ポーズは目口差分、他は1枚絵フォールバック。タイトルは承認済みKV。SEは承認済みB-bright、BGMはGemini/Lyriaの生成2曲。
- 声はElevenLabs「Hinata V9-2」117行。音声なし/消音でも字幕・表情・ポーズと操作で進む。音源・音声入りResourcesはgit除外。無料プランの私的試遊限定で**配布・公開・動画投稿は禁止**。利用条件を満たす公開用音源は別途再制作。
- UI評価と過去の計測は`Docs/UI-UX-Review-2026-09-28.md`、`Docs/Evaluation-2026-09-29.md`。変更前の「限定85〜90%」等と現在を混同しない。

## 7. 次にやること（優先順）

1. 加藤さんがUnity EditorのCompanyYearを私的試遊する。山場への準備・達成/未達・次ランクへの動機・声・テンポを確認。数値は指定のままで、上記の完走率/SSの偏りも評価する。本命の旧版/新試作の選択は未決定。
2. 声117行・目口・SE/BGMのミックスを確認。配布前に公開可能な音声を再制作し、VoiceTestのResourcesは除く。Windows版更新は依頼時だけ。
3. Next-3/Next-4/Next-Screensの実装済み項目を繰り返し作らない。旧保存と新年度の追加ルールを区別。臨時予算1万円は維持し、見直しは別途設計・比較する。検証基準を数値変更に合わせて勝手に緩めない。
4. 難易度、兆候の連鎖、複数年、図鑑・会話、追加キャラは設計候補。承認後に進める。山場の目標/年間5段ランクは実装済み。
5. Player Settings（Steam向け）、ブランチ整理。旧版を新試作へ自動置換しない。`planning-bubbles.html`と`title-screen-kv.html`はNext-3で承認・実装済み。

## 8. 対話スタイル

加藤さんは率直・簡潔で実質的な回答を好み、汎用的・無難な成果物を嫌う。「面白いゲーム」への関心が強く、確率計算の冷たさを避けたい。コード内コメントとUI文字列は日本語で統一する。
