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
| `Assets/Scenes/CompanyYear.unity` | **現在の試遊対象**「情シスの一年」v0.12。12か月、工数と予算で改善の順番を選ぶ | `Assets/Scripts/CompanyOps/`、生成は`Assets/Editor/CompanyOpsSceneBuilder.cs` | `Docs/CompanyYear-Prototype.md` |
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
# PlayModeテスト（画面を撮影するので -nographics は付けない）。9/29時点で132件
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

- 新試作v0.10：共通UI・次画面・新ひなた・ロゴ・質感、ひなたの動き①②と演出調査◎9技法、Quick-Wins-2の0〜8を実装済み。PlayMode121/121、最後の音量補正後も追加10件10/10、Verify4系統・コンパイル成功。報酬・難易度は変更なし。詳細は`Docs/Quick-Wins-2-Implementation-2026-09-29.md`と`Docs/Hinata-Motion-and-Feel-2026-09-29.md`。社員の事件支援・顔マーク・工数追加・日常委任は実装済み。計画行動の追加効果と顔マークは未実装（ルール追加は確認中）。
- v0.11：Next-Screensのタイトル・遷移、依頼書、部屋操作、文化・信頼のランク恩恵まで実装。追加報酬は新年度だけ。全125件で123成功、失敗2件は確認先・待機修正後に個別成功、最終対象4/4・Verify4系成功。全125件の最終版一括成功とは区別する。Windows版は据え置き。詳細は`Docs/Next-Screens-Remaining-2026-09-29.md`。
- v0.12：UI不具合4件を修正し、ひなたV9-2の105行（掛け声54・追加11・全文40）を場面へ配線。字幕・表情・ポーズ・操作キャンセル・BGM減衰・音量設定に対応。公開側は音源なしの台本、音声入りResourcesは追跡対象外。全132件は130成功、失敗2件は旧検査を更新して個別成功。再開始・再昇格の全文再生も補正後に確認し、最終関係分7/7、Verify4系・コンパイル成功。最終版の全件一括成功とは区別する。ルール・臨時予算1万円は変更せず、Windows版は据え置き。画像比較と結果は`Docs/UI-Repair-and-Hinata-Voice-2026-09-29.md`。
- 自動方針では放置6/300、他の方針は300/300が完走。限定対応に偏り、慣れた方針には易しい。**人間の初見プレイはまだ誰もしていない**（自動で完走できる＝面白い、ではない）。
- ひなた：承認済み新デザイン（太線アニメ塗り・3頭身）の表情18・ポーズ18を`Assets/Sprites/Hinata/v2/`へ投入し、新試作・旧版とも置き換え済み。旧画像は`ArtSource/Hinata/legacy/`。基本ポーズ`pose_fists`は4素材の目口差分、他17ポーズは1枚絵で動く。移動・拡縮・回転を減らしてもまばたきは残る。来歴は`Docs/Hinata-Replacement-2026-09-29.md`と`Docs/Reference-Asset-Provenance.md`。学生服は不可。ボイス台本は`Docs/Voice/`。
- 音：SEは加藤さん指定のB-brightの11音、BGMはGemini / Lyriaの生成2曲。**声はElevenLabs「Hinata V9-2」の台本v2・105本を組み込み済み**（無料プランの私的試遊限定、音源と音声入りResourcesはgit追跡対象外）。配布・公開・動画投稿は禁止。実装指示は`Docs/Voice/Integration.md`。目口差分は`NavigatorPersona.AnimationFrames`、差分のないポーズは1枚絵を維持。
- **総合評価（9/29）：`Docs/Evaluation-2026-09-29.md`**。見た目・キャラは良いが、判断が浅い（限定が85〜90%、迷える選択肢が平均1.2〜1.5）、ほぼ全方針で完走、1年で終わる。Steamの予測は今のままで好評率60〜70%。改善の優先順位とUIの不具合4件を記載。追加キャラの案は`Docs/Characters-Ideas.md`（ひなた・秘書・金髪エンジニアの3人に絞った。本格着手は後）。困りごとの泡（ワンタッチで解決、小さな報酬、稀にレア）の案は`Docs/Ideas-Office-Bubbles.md`（未承認）。
- UI・UXの評価と改善候補：`Docs/UI-UX-Review-2026-09-28.md`（画面の不具合6件と演出・音）、`Docs/UI-UX-Research-2026-09-28.md`（方針）。

## 7. 次にやること（優先順）

1. **加藤さんの試遊**で、どちらを本命にするかを決める。決まる前に機能を足し続けない。
2. 新ひなたの動き・差分・105本の声と音のミックスを加藤さんが私的試遊で確認する。現在はUnity Editorで確認でき、Windows版は未更新。配布前は有料プランで音声を再制作し、VoiceTestのResourcesを除いて公開用音声へ移す。追加ポーズの目口素材は同じキャンバス・基準点で登録し、1枚絵フォールバックは維持。Live2D等の部品分けは未着手。
3. `Docs/Mockups/Next-Screens.md`の①〜⑥は実装済み。新年度の依頼報酬に臨時予算1万円を追加。文化・信頼のランク恩恵もVerify系で比較済み。旧年度の途中には追加ルールを適用しない。次は実プレイでテンポ・部屋の操作・恩恵の伝わり方を確認する。
4. 生成BGM2曲の試聴・ループ調整、B案SEを実プレイで確認してミックスを詰める（別案は承認前に登録しない）。図鑑は年度内遭遇集計と枠まで、一覧画面・年度をまたぐ集計は未実装。商用本採用前に利用条件を再確認する。
5. **判断の深さ（面白さの芯）**：臨時予算1万円の見直しもここで扱う（UI・ボイス修正では変更しない）。状況で最善の方針が変わる仕組み。Claudeが設計案と自動プレイでの検証を作り、Codexが実装する。設計案と900年度の結果は`Docs/Decision-Depth-Design-2026-09-29.md`（推奨案で限定43%・迷える選択肢1.95。加藤さんの承認待ち）。目標は「迷える選択肢」平均2.0以上、限定の比率60%以下。続いて難易度の段階、兆候の連鎖、3年キャンペーン、図鑑、会話イベント（`Docs/Evaluation-2026-09-29.md`の4）。限定対応を弱めるだけにしない。旧版は逆に難しい（準備＋復旧の方針でも258/1000）。
6. Player Settings（Steam向け）、ブランチ整理。`planning-bubbles.html`と`title-screen-kv.html`は承認前なので実装しない。

## 8. 対話スタイル

加藤さんは率直・簡潔で実質的な回答を好み、汎用的・無難な成果物を嫌う。「面白いゲーム」への関心が強く、確率計算の冷たさを避けたい。コード内コメントとUI文字列は日本語で統一する。
