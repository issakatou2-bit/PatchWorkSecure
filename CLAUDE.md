# PatchWorkSecure — Claude Code 引き継ぎ

このファイルはプロジェクトルート（`C:\Projects\PatchWorkSecure\CLAUDE.md`）に置くこと。
Claude Codeは起動時にこれを自動で読み込むので、毎回コピペし直す必要はない。

---

## 0. これは何のプロジェクトか

**PatchWorkSecure** — 企業の情シス担当者となり、日常業務をこなしながらサイバー攻撃から
会社を守る、サイバーセキュリティ教育目的のシミュレーションゲーム。

> 「なにごともない、いつもの平穏なオフィスの日常をツギハギ（PatchWork）しながら守り抜く」

- 開発者：加藤さん（個人開発、将来的にSteam配信も視野）
- 開発パートナー：Claude（chat版で設計・実装・デバッグを伴走してきた。このCLAUDE.md以降はClaude Codeに引き継ぐ）
- 思想の芯：①人のためのセキュリティ ②性弱説（ミスを責めず、致命傷にならない構造） ③透明性と信頼

---

## 1. 技術構成

| 項目 | 値 |
|---|---|
| プロジェクトパス | `C:\Projects\PatchWorkSecure` |
| Unity Editorバージョン | `6000.5.6f1`（Universal 2D / URP。当初6.3 LTSを想定していたが、プロジェクト作成時に実際にはこのバージョンで作成された） |
| テンプレート | Universal 2D |
| GitHub | `https://github.com/issakatou2-bit/PatchWorkSecure.git`（`main`ブランチ） |
| UIフォント | TextMeshPro、`Assets/Fonts/Meiryo SDF`（日本語用にCustom Charactersで生成済み） |

---

## 2. アーキテクチャ（ここが一番重要）

```
GameData.cs        ← マスターデータ(攻撃10種・防御8種・SC用語)。static、MonoBehaviour非依存
GameState.cs        ← コアロジック(防御率計算・攻撃判定・資源管理)。MonoBehaviour非依存の純粋C#
GameManager.cs       ← MonoBehaviour。GameStateの状態をUIに反映し、フェーズ進行を管理する
EducationTracker.cs  ← 教育クイズ(事前/事後)・PlayerPrefsへの永続化・CSV出力
Assets/Editor/SceneBuilder.cs ← エディタ拡張。UnityメニューからUIシーン全体を自動生成する
```

**設計原則：ロジック(GameState/GameData)とUI(GameManager)を分離する。**
バランス調整は`GameData.cs`の数値を変えるだけで完結するようにしてある。

### GameManagerのフェーズフロー

```
Start() → ShowTitle()
  → (「はじめる」) → BeginQuiz(isPre:true) → 事前クイズ3問
    → FinishQuiz() → _state = new GameState(); ShowDayPhase()
      → [雑務 → 攻撃判定 → (選択→パリィ→結果) → 次の日] のループ
        → IsGameOver / IsCleared → ShowGameOver() / ShowClear() → endingPanel表示
          → (「結果を振り返る」) → BeginQuiz(isPre:false) → 事後クイズ3問
            → FinishQuiz() → EducationTracker.RecordSession(...) → ShowSummary()
              → (「タイトルへ戻る」) → ShowTitle()
```

### 重要な実装パターン（新機能を足すときはこれを踏襲する）

1. **ボタンの配線はコードから行う。Inspectorの`OnClick()`に手動登録する方式は使わない。**
   `GameManager.WireButtons()`内で`button.onClick.AddListener(...)`する。
   理由：`SceneBuilder.cs`が`SerializedObject`経由でInspector参照を自動割当する設計と噛み合わせるため。
   新しいボタンを追加したら、①`GameManager`にButtonフィールドを追加 → ②`WireButtons()`に配線を追加
   → ③`SceneBuilder.cs`側で生成して`SetRef()`する、の3点セットを必ず揃える。

2. **選択肢・リスト系UIはプレハブ+動的Instantiateパターンを使う。**
   `BuildChoiceButtons()` / `BuildDefensePanel()` / `BuildQuizOptions()` が参考実装。
   `GetComponentInChildren<TextMeshProUGUI>()`でラベルを取得する構造なので、
   新しいプレハブも「ルートにButton+Image、子にTextMeshProUGUI」という構造を守ること。

3. **パネルの表示切り替えは`HideAllPanels()` → 対象パネル`SetActive(true)` → `StartCoroutine(FadeInPanel(...))`。**
   新しいパネルを追加したら`HideAllPanels()`にも追加を忘れないこと（忘れると多重表示のバグになる）。

4. **`SceneBuilder.cs`はUnityメニュー「PatchWorkSecure → シーンを自動構築」から実行する。**
   Canvas/GameManagerを一括生成し、Inspector参照も全部自動で埋める。UIレイアウトを変えたら、
   Inspector手作業ではなくこのスクリプト側を直すのが正しい直し方（車輪の再発明を防ぐため）。

---

## 3. 制約（Claude Codeでも変わらないこと）

Claude Codeはこのプロジェクトのファイルを直接読み書きでき、git操作もできる。
ただし以下は依然としてユーザー（加藤さん）の手作業が必要：

- **Unity Editorの画面を見る・操作すること**（オブジェクトの目視確認、Playボタンを押しての動作確認）
- 素材（アヤの立ち絵など）ができた後の、実際の見た目の最終判断

### 応用：バッチモードでのコンパイル確認（必須ではないが有効）

Unityはコマンドラインから`-batchmode -quit`で起動でき、GUIを開かずにコンパイルだけ走らせられる。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "C:\Projects\PatchWorkSecure" `
  -logFile "C:\Projects\PatchWorkSecure\batch_log.txt"
```

実行後`batch_log.txt`を読めば、コンパイルエラーの有無をClaude Code自身が確認できる
（数十秒〜数分かかる。Playモードでの実際の動作確認の代替にはならない点に注意）。
`SceneBuilder.BuildScene()`のような`[MenuItem]`メソッドも`-executeMethod`で直接叩けるので、
必要なら以下のように指定する：

```powershell
-executeMethod PatchWorkSecure.EditorTools.SceneBuilder.BuildScene
```

---

## 4. 現状（完了していること）

- 攻撃10種フル実装（IPA情報セキュリティ10大脅威2026準拠、グレード別キャラ付け・台詞付き）
- 防御8種フル実装（Lv1〜3、基礎防御率70%上限あり）
- モンテカルロ・バランス検証済み（`balance_sim.js`、放置プレイ5.6% vs 熟練78%）
- ナビゲーターキャラ「アヤ」のロジック実装済み（表情スプライトは未着手、6種すべてnull）
- 教育クイズ（事前/事後、8問プール）実装済み、`EducationTracker`でPlayerPrefs永続化・CSV出力対応
- Unity上での通しプレイ確認済み（タイトル→クイズ→本編→エンディング→クイズ→サマリー）
- 対策強化パネル・攻撃選択パネルの動的UI生成、フェード演出、バーのアニメーション実装済み
- Git/GitHub連携済み

## 5. 未完了・次にやってほしいこと（優先順位順）

1. **素材の実装**：加藤さんが学校のComfyUI環境（`/mnt/skills`ではなく別PC）でアヤの立ち絵（6表情）・
   背景を生成後、`pixelate.py`でドット化してGitHub経由で受け取る想定。届いたら
   `Assets/Sprites/`に配置し、`GameManager`の`faceNormal`等6フィールドへの割当を、
   `SceneBuilder`と同様の`SerializedObject`自動割当スクリプトを書いて省力化すること
   （ファイル名を`aya_normal.png`のように固定してもらえば、`AssetDatabase.LoadAssetAtPath`で
   自動検出できる）。
2. **パリィ演出の強化**：現状は単純な往復移動のみ。Papers Please／勇者のくせに生意気だ／
   パチンコ演出を参照軸に、もう少し緊張感のある見せ方を検討（画面シェイク、色変化等）。
3. **サウンド（BGM/SE）のフック追加**：`AudioSource`をGameManagerに持たせ、攻撃発生時・
   防御成功/失敗時などにSEを鳴らす仕組みを用意（音源自体は別途調達）。
4. **攻撃アイコン10種の作成**（現在は絵文字で代用、優先度低）。
5. **ビルド設定の整備**：Steam配信を視野に、アイコン・製品名・バージョン管理などの
   Player Settings整備（実際のビルド確認は加藤さんの手作業）。
6. 攻撃・防御データを変更した場合は、必ず`balance_sim.js`と`verify_csharp_logic.py`で
   再検証すること（過去に基礎防御率が120%に達する抜け道が見つかった経緯があるため）。

---

## 6. 対話・作業スタイルについて

- 加藤さんは率直な物言いを好み、遠回しな配慮より実質的な情報を求める
- 簡潔・実行可能な回答を好む。ヘッジングや過剰な前置きは不要
- 汎用的・無難な成果物を嫌い、個性と演出にこだわる
- 「面白いゲームにしたい」という関心が強く、単なる確率計算の冷たさを避けたいという明確な意向がある
- コード内コメント・UI文字列は日本語で統一すること
