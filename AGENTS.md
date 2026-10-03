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
| `Assets/Scenes/CompanyYear.unity` | **現在の試遊対象**「情シスの一年」v0.16。12か月、工数と予算で改善の順番を選ぶ | `Assets/Scripts/CompanyOps/`、生成は`Assets/Editor/CompanyOpsSceneBuilder.cs` | `Docs/CompanyYear-Prototype.md` |
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
# PlayModeテスト（画面を撮影するので -nographics は付けない）。Next-24時点で319件
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

Next-19の分類規則（Next-24時点）：普段は印なし184件、`Capture`は撮影130件、`Long`は3役×5年度とゲームパッド通しの5件。合計319件。Longは撮影してもCaptureを重ねない。Explicit撮影7件は別枠。既存317件の名前と分類は維持し、Next-24の公開音声2件だけ普段へ追加。元々294件に含まれるストア画面の回帰撮影はCaptureで維持する。
起動済みEditorでは`./Tools/Run-TestGroup.ps1 -Group RegularCapture`（普段＋撮影）、`-Group Normal`（普段）、`-Group Long`、`-Group All`を使う。`-Group Inspect`で一覧確認。失敗/関係分は`-Only`に完全名の配列を渡す。実行ごとのJSON/XMLは`Artifacts/TestGroups/`へ保存し、0件や中断を成功扱いにしない。

## 5. 使用量を抑える作業ルール（品質は落とさない）

- 大きいファイル（`SceneBuilder.cs` 82KB、`GameManager.cs` 61KB、`OpsGame.cs` 40KB）は全文を読まない。Grepで位置を特定し、必要な範囲だけを読む。
- ログは全文を読まない。`test_results.xml`は`total/passed/failed`だけ、`batch_log.txt`は`error CS|Exception|warning CS`で絞る。
- 画面の確認には、テストが`Artifacts/CompanyOps/`・`Artifacts/OfficeReview/`に保存したスクリーンショットを使い、必要な枚数だけを見る。
- 経緯・計測値・調査結果は`Docs/`に書く。このファイルに足すのは「次の作業者が毎回知るべき規則と現状」だけ。
- 同じ検証の重複実行や、不要なサブエージェントの起動はしない。報告は結論から短く書く。
- **Next-19の実行回数を守る**：各項目のコミット前は関係分＋コンパイル。指示の最後は普段＋Captureを1回。失敗は直して失敗分と関係分だけ再実行し、全件を最初から流し直さない。Windows私的試遊版はLong込み全件1回＋普段をもう1回（Longは1回）。公開/Steam版だけLong込み全件を2回連続。Windows再ビルドは依頼時だけ。数値を変えたらVerify系と1年CSVのSHA256照合も行う。
- 中断実行は成功回数に数えず、フォントの作業用データとPlayerPrefsの存在/型/値の復帰を毎回確認する。撮影が時間切れならClaudeのIrodori生成とのGPU競合をまず確認し、同じテストだけ再実行する（`Docs/Voice/Irodori-Direction-Rules.md`2e）。
- **加藤さんへの返答は常に日本語**。英語に切り替えない。

## 6. 現状（2026-10-04）— 詳細は`Docs/Dev-Status.md`

- **Next-24完了**：承認CSVのH09公開197本を無加工で配置、除外は`tutorial_1`だけ。字幕/ポーズ/ルールは不変。全319件は316成功/3失敗から失敗＋関係6/6で解消、追加の普段184/184成功。Windows検査の空参照誤認も専用処理だけ修正し関係2/2成功。Windows0.16.0の公開197参照/4行/連鎖七段階と7以上/12か月/音/画面確認成功・終了0。各CSVのSHA256、設定15キー/8フォント一致、一時シーン0。公開検査は私的素材を拒否、Depths+27ptは既知未達。詳細は`Docs/Next-24-Implementation-2026-10-04.md`。

- **Next-23完了**：既存の入場・数え上げ・判子・切替・発動等の待ちを`OpsPresentationTiming`へ集約。初回は全表示、同種の再訪はクリック/決定/南ボタンで終端、速いは2倍。声を止め入力を次の行動へ渡さず、遊び中は入力を奪わない。生成前の時間加算と上書きポーズの待ち残りも修正。全317件は310成功/7失敗から関係分だけ再確認し未解決0、追加の普段182件も180成功/2失敗から当該2件を成功。7画面の前後比較、CSVのSHA256不変、設定15キー/8フォント一致・一時シーン0。Windows私的試遊版0.16.0は更新・12か月/音/画面確認成功。Depths+27ptは既知未達のまま。詳細は`Docs/Next-23-Implementation-2026-10-03.md`、比較は`Artifacts/Next23/`。

- **Next-22完了（棚卸しのみ）**：画面ごとの動き・音・待ち・速さ設定を`Docs/Presentation-Audit-2026-10-03.md`へ記録。Next-18の土台で54短編・4,215コマ（1920×1080/30fps）と3コマ一覧9枚を`Artifacts/Next22/`へ保存。Explicit3/3＋補足1/1成功（補足の撮影用待ちの時間切れは修正して当該1件だけ再実行）。普段175/Capture130/Long5の310件と完全名・分類は不変、Explicitだけ2→6。コンパイル成功、設定15キー/8フォント一致・一時シーン0。ゲーム本体/見た目/数値は不変、全件・Verify・Windows再ビルドなし。WindowsはNext-21のまま。

- CompanyYearが試遊対象。**Next-24完了、H09公開197本をWindows私的試遊版0.16.0へ反映済み**。旧版は回帰確認のみ。承認済み画面・UIキット・Polish-4・Quick-Wins-2を維持。B/C/D/E/F2/Gは同じOpsMinigame土台。3年目3月の総決算だけB→G（ランサム）、正常Gは練習で遊べる。
- **50点は従来結果のまま**。総決算はB・G両方50点で全状態が従来と一致。その他は係数適用後、被害/停止それぞれ[min(従来結果,公開見積もり下限),max(従来結果,公開見積もり上限)]内。Gは停止だけ。元から幅外の結果をさらに広げず、未確認の真相を表示に漏らさない。
- 3年本編は目標B→A→A、途中保存、因子Lv1・最大3枠、承認済み6画面。**予算は76万円＋前年の残額全額**。設備Lv2→Lv1の見直し、社員経験/相談文化を維持、信頼は45との平均。1年モードに因子は適用しない。
- Next-10/11：年別の暦・重複なし抽選、日記36話＋結末3つ・Klee One、因子演出、2・3年目の開幕、新設備4つ、月1工数0調査、かのんの山場予算、後輩4人目、強敵/図鑑。EDRは監視の即時表示を維持し、感染部屋の強調と最初の感染端末1台の時間消費なし隔離。旧11設備保存の4画面と元バイトのバックアップを回帰確認。
- 新2・3年目の脅威は**+13/+26**。旧途中保存は+12/+24を維持し、次年度から新数値。2,160挑戦の初回225/540＝41.67%、4回目まで365/540＝67.59%を維持。1年/初年度900年度・10,779月CSVはNext-8前とSHA256完全一致。仮想方針の検証で、人間の楽しさ・学習効果の測定ではない。
- Next-12のエンドレスは4年目以降24+4n+2n²（n=年−3）、月収+2万円/年。試算だけ維持費控除後の余裕8万円で資金繰りを判断。考える6方針の中央6.50〜7.75年、最長8.33年。気軽の2〜4年は正式基準から除外。総合B/A/S/SS=7116/8032/11707/12329、SS60/600。全900挑戦のCSVもNext-12と一致。10年称号は維持、人間の到達可能性は未測定。
- **Next-13①〜⑤・⑦・⑧を別コミットで実装**。仲間24音声/丸い顔札、総決算B→G、演出速度、記録から日付固定の6練習/自己ベスト/全S称号（計10称号）、格言20種の既読と年度内1回。練習は本編の途中保存を保ち、記録だけの保存にも対応。速いでもミニゲームの制限時間・採点・声の速度は不変。詳細は`Docs/Next-13-Implementation-2026-10-02.md`、比較は`Artifacts/Next13/`。
- ⑦の情報修正は文だけで抽選・採点不変。格言は`maxim_hurry_v2`/`maxim_link_v2`。Next-14の改名で日記は`diary_y1_10_v3`/`diary_y3_06_v2`へ移し、当時は字幕先行（現在は新版IDのH09音声を登録済み）。旧声は再生しない。同僚3人は対等な正社員、りりぃは2年目に開発部から情シスへ異動。Engineer/eng_*と保存キーは維持。承認二人絵3枚を日記2ページと会議へ追加。
- **Next-17は通常画面とB〜Gのゲームパッド操作を別コミットで実装**。十字キー/左スティック、南決定・東戻る・Start設定・LB/RB既読日記。F2は作業→マス→決定、X回転。既存のホバー枠とつまみ輪郭を再利用し、マウス/キーボードへの持ち替えを維持。仮想パッドで一年通しと六本S、1280×800/1920×1080で撮影・モック比較。実機Xbox/PS/Steam Deckは未確認。
- Next-17最終確認：**全294/294を2回連続成功**。全件後・ビルド後の8フォントとPlayerPrefs15キーの存在/型/値が一致、一時シーン0。Windows版の画面切替・12か月・BGM2曲・SE15用途・事件音声はPASSED、終了コード0、既知の終了時ComputeBuffer診断は残る。Core/CompanyOps/Personas/ThreeYears/EndlessYearsは成功、各CSV・採点・制限時間は不変。詳細/比較は`Docs/Next-17-Implementation-2026-10-03.md`/`Artifacts/Next17/`。旧版・Next-15/16の撮影は維持。仮想入力切替前後のUI実行キャッシュとCanvasをテスト側で破棄し、通常描画への復帰も回帰検証。
- 情報点検の要相談5件は加藤さんの決定どおり反映。`maxim_report_v2`/`diary_y3_07_v2`は当時字幕先行、現在は新版IDのH09音声を登録済みで旧声を鳴らさず、格言20種/旧既読キーを維持。かのんの開幕字幕を同期、りりぃの因子台詞は維持。科目B・架空の想定・IdP資料の参考表示を反映し、数値/抽選/採点/source/URLは不変。IPA本編PDF・個別実例の裏付けは未確認のまま（`Docs/Fact-Check-2026-10-02.md`）。
- **Depthsの最適化完走率58%→85%（+27pt）は既知の未達**。±5pt基準を緩めず成功扱いにしない。限定50.25%、非優越候補平均1.980、SS10.22%。
- ひなたの新絵・目口差分/1枚絵フォールバックは両版。SEはB-bright、BGMはGemini/Lyria生成2曲。Next-21から同じIDの`Voice/Hinata/<id>.wav`（Irodori H09）を優先し、無ければ私的ElevenLabs V9-2、両方無ければ字幕。**台本198行のうち公開H09音源197本を配置済み。`tutorial_1`だけ未配置で従来の私的音声/字幕へ戻る**。字幕・ポーズは不変。音源なし/消音でも進む。現Windows版は私的試遊限定、**配布・公開・動画投稿禁止**。公開ビルドはVoiceTestのResources/依存が残ると停止。仲間24音声は配布可、条件は`Docs/Reference-Asset-Provenance.md`。
- TestModeはTMP・フォールバック・画像・Materialを専用コピーにし、PlayerPrefs15キーを存在・型付き値ごと復帰。一時シーンは所有/非使用を確認し退避後に回収する。無関係の設定・素材metaはコミットしない。

- **Next-18完了**：宣伝9場面の1920×1080/30fps連番1,380枚・46秒、各sounds.jsonと確認一覧は`Artifacts/Promo/`。カード選択→ランクアップを採用、月報の追加撮影なし。撮影だけExplicit/30Hz時計、通常のテスト/Playerには影響させない。期限後の古いパッド回答をテスト側で取り消し、全294/294を2回連続成功。各CSV不変、設定15キー/8フォント一致・一時シーン0。Windows再ビルドなし。ひなたの声は動画禁止、BGMは利用条件確認待ち。詳細は`Docs/Next-18-Implementation-2026-10-03.md`。

- **Next-19完了**：テスト本文/完全名を維持して普段160/Capture129/Long5へ分類、各1回ですべて成功。実行手順は4・5章へ反映。1年CSVのSHA256、設定15キー/8フォント一致、一時シーン0。Windows再ビルドなし。詳細は`Docs/Next-19-Implementation-2026-10-03.md`。

- **Next-20完了**：Unity非依存の`OpsThreatCatalog`に6糸口/10脅威/33技を追加。名前は仮、糸口・設備の対応はゲーム用の整理、画面/既存7強敵/ルールには未接続。説明の要相談なし。普段のテスト10件追加、関係10/10・最後の普段170＋Capture129は299/299を1回成功。既存294件の名前/組/本文は維持。Verifyは既知Depths未達以外成功、1年CSV/3年試算出力/エンドレスCSVのSHA256一致、設定15キー/8フォント一致、一時シーン0。Windows据え置き。詳細は`Docs/Next-20-Implementation-2026-10-03.md`。

- **Next-21完了**：B〜Gの連続成功は`combo_1`〜`combo_7`（7以上はパーフェクト）、失敗/次ゲームでリセット。採点と別の反応専用カウンタ、既存字幕を全文即時表示し声は即時差替。①②は別コミット。Long込み310件を1回で309成功/旧期待値1件失敗、テスト修正後に関係3/3と普段175/175を成功、全件再実行なし。Verifyは既知Depths以外成功、各CSVのSHA256不変、設定15キー/8フォント一致・一時シーン0。Windows版の画面切替/12か月/BGM2/SE15/事件音声PASSED・終了0。詳細は`Docs/Next-21-Implementation-2026-10-03.md`、比較は`Artifacts/Next21/`。

## 7. 次にやること（優先順）

1. **Next-24完了。追加指示が来るまで新しい作業を始めない**。公開H09音源197本を無加工で配置、`tutorial_1`は除外。全319件の失敗分/関係再確認と普段184件の追加1回を完了、Windows版も更新済み。字幕・ポーズ・数値・採点・制限時間・CSVは不変。詳細は`Docs/Next-24-Implementation-2026-10-04.md`、記録は`Artifacts/Next24/`。棚卸しの不足演出や10大脅威の画面接続は別指示後。
2. **Next-24のWindows版を私的試遊**。H09の会話と連鎖七段階/7以上、仲間の声、初回全表示・再訪スキップ・速い設定を確認する。`tutorial_1`は従来の私的音声。実機Xbox/PS/Steam Deckの持ち替えと六本の操作を評価し、仮想入力の自動検証と人間の初見を区別する。既存の字幕/資料・二人絵・総決算・速度・格言の確認も維持し、重複改修をしない。
3. ひなたの公開音声H09は197行を接続済み。`tutorial_1`は選定中で今回追加しない。公開版を作る際はVoiceTestをAssets外へ保全して除く（残っていればビルドを停止）。新たな音源/絵の制作は追加指示後。規則は`Docs/Voice/README.md`、設定は`Docs/Character-Profiles-2026-10-01.md`。
4. 計画行動の支援拡張・施策カード・追加キャラは未実装。既存の承認済み画面を繰り返し作り直さず比較を残す。臨時予算1万円と基準を勝手に変えない。
5. Steam向けの会社名/アイコン・`tutorial_1`の公開音声・公開ビルドの私的素材除外・ブランチ整理は未決定。0.16.0と画面設定は完了。旧版を自動置換しない。
- やり込みの設計は `Docs/Endless-Years-Design-2026-09-29.md`（試作 `proto-a-cards.html`）。現行への適用はNext-12を優先。
- 原則をゲーム全体に通す表とひなたの格言は `Docs/Principles-in-Play-2026-09-29.md`。

## 8. 対話スタイル

加藤さんは率直・簡潔で実質的な回答を好み、汎用的・無難な成果物を嫌う。「面白いゲーム」への関心が強く、確率計算の冷たさを避けたい。コード内コメントとUI文字列は日本語で統一する。

**補足追記：IPA公式資料の限定的な確認結果（2026-10-03 JST）**：本編の冊子p.29・31で、入退社・異動・契約終了の学習論点の根拠を確認。その他の箇所とゲーム全体の照合・個別実例は未確認。詳細は[点検記録の補足追記](Docs/Fact-Check-2026-10-02.md)。過去の未確認記録は保持し、既存方針・source割当の変更や新作業の指示ではありません。
