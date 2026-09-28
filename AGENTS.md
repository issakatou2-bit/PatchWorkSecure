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
| `Assets/Scenes/CompanyYear.unity` | **現在の試遊対象**「情シスの一年」v0.8。12か月、工数と予算で改善の順番を選ぶ | `Assets/Scripts/CompanyOps/`、生成は`Assets/Editor/CompanyOpsSceneBuilder.cs` | `Docs/CompanyYear-Prototype.md` |
| `Assets/Scenes/SampleScene.unity` | 旧版。36期、攻撃10種×対策8種、パリィ、教育クイズ。オフィス背景に改修済み | `Assets/Scripts/`直下、生成は`Assets/Editor/SceneBuilder*.cs` | `Docs/Office-Rework.md` |

- 新試作：`OpsCatalog`(内容) → `OpsState`(Unity非依存のルール) → `OpsGame`(画面。partialで分割) / `OpsSaveStore`(保存)。旧版の`GameState`には依存しない（共有は`NavigatorPersona`のみ）。
- 新試作の見た目：本文 Zen Kaku Gothic New Medium、見出し・数値・操作 M PLUS Rounded 1c Bold（`Docs/CompanyYear-Typography-2026-09.md`）。現行はチャコール＋白＋青の操作色（参考作品寄せのモックで見直し中）。
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
# PlayModeテスト（画面を撮影するので -nographics は付けない）。9/28時点で63件（新試作54＋旧版の通し2＋オフィス7）
& "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe" -batchmode -runTests -testPlatform PlayMode `
  -projectPath "C:\Projects\PatchWorkSecure" -testResults "<出力先>\test_results.xml" -logFile "<出力先>\batch_log.txt"
# コンパイル確認だけなら -batchmode -quit。メニューの処理は -executeMethod PatchWorkSecure.EditorTools.SceneBuilder.BuildScene
```

Unity不要（Editorの起動中でも可）。**数値を変えたら必ず流す。**
`./Tools/Verify-Core.ps1`（旧版）、`./Tools/Verify-CompanyOps.ps1`（新試作）、`./Tools/Verify-CompanyOps-Personas.ps1`（3タイプの仮想方針）、`./Tools/Compile-Office.ps1`（3アセンブリのコンパイル）。
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

## 6. 現状（2026-09-28）— 詳細は`Docs/Dev-Status.md`

- 新試作v0.8：出来事40件・日常業務18件、社員育成・季節負荷、3列の対応比較、浮遊する増減値、反応台詞36候補。PlayMode 63/63、Verify系も成功。Windows版の12か月通し検証もPASSED。
- 自動方針では放置6/300、他の方針は300/300が完走。限定対応に偏り、慣れた方針には易しい。**人間の初見プレイはまだ誰もしていない**（自動で完走できる＝面白い、ではない）。
- ひなた：`Assets/Sprites/Hinata/hinata_normal.png`の暫定1表情だけ（生成画像を加工したもの）。加藤さんの希望は、アニメ／萌え寄り・かわいい仕事着・表情とポーズの差分。学生服は不可。旧顔を保持する案、A/B/C線画、コードで顔を描き直す案はいずれも不採用。仕様は`Docs/Hinata-Art-Brief.md`。
- 音：SEは簡易合成音（`OpsSoundDesign`）。BGMと声は未投入（声の試聴は`Docs/Hinata-Voice-2026-09-28.md`）。
- UI・UXの評価と改善候補：`Docs/UI-UX-Review-2026-09-28.md`（画面の不具合6件と演出・音）、`Docs/UI-UX-Research-2026-09-28.md`（方針）。

## 7. 次にやること（優先順）

1. **加藤さんの試遊**で、どちらを本命にするかを決める。決まる前に機能を足し続けない。
2. ひなたの原画（6表情：`hinata_normal / proud / worried / alert / relieved / sad`）を`Assets/Sprites/Hinata/`へ置く。「PatchWorkSecure → キャラ立ち絵を取り込む」で反映する（透過も検査される）。
3. UI・UX評価の不具合6件（重なり、用語の不統一、孤立した改行など）。
4. BGMとSEを実素材にする（ライセンスを確認してから）。
5. 難易度：限定対応を弱めるより、状況で選び分ける理由を増やす。旧版は逆に難しい（準備＋復旧の方針でも258/1000）。
6. Player Settings（Steam向け）、ブランチ整理。

## 8. 対話スタイル

加藤さんは率直・簡潔で実質的な回答を好み、汎用的・無難な成果物を嫌う。「面白いゲーム」への関心が強く、確率計算の冷たさを避けたい。コード内コメントとUI文字列は日本語で統一する。
