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
| UIフォント | TextMeshPro、`Assets/Fonts/meiryo SDF`（**動的生成モード**。未収録の文字は実行時に`meiryo.ttc`から自動生成されるので文字が抜けない。TMPの既定フォント兼フォールバックにも設定済み） |

---

## 2. アーキテクチャ（ここが一番重要）

```
GameData.cs          ← マスターデータ(攻撃10種・防御8種・SC用語)。static、MonoBehaviour非依存
GameState.cs         ← コアロジック(防御率計算・攻撃判定・資源管理)。MonoBehaviour非依存の純粋C#
GameManager.cs       ← MonoBehaviour。GameStateの状態をUIに反映し、フェーズ進行を管理する
AudioManager.cs      ← BGM/SEの一括管理。AudioClip未設定でも無音で動く(素材が無くても全アクションにフックを仕込める)
UIEffects.cs         ← 演出専用。フラッシュ/シェイク/中央バナー/浮遊テキスト/バースト/スケールパンチ
NavigatorPersona.cs  ← ナビゲーターキャラ1人分のデータ(ScriptableObject)。名前・イメージカラー・表情・セリフ
UIButtonPunch.cs     ← ボタン押下時のスケール演出(IPointerDown/Up)
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
   実行前の手動削除は不要（`ClearGeneratedObjects()`が前回生成分を消してから作り直す）。
   **生成後は必ず`EditorSceneManager.SaveScene()`でシーンを保存すること。**
   これを忘れると生成物はメモリ上にしか無く、Unityを閉じた時点で消える
   （実際にSampleScene.unityが空のまま数セッション進んでしまった経緯がある）。

5. **絵文字はUI文字列に使わない。** `meiryo.ttc`自体が色付き絵文字を持たないため、
   フォントを動的生成モードにした今も絵文字だけは表示できない（漢字・記号は解決済み）。
   アイコンが欲しい場合は`SceneBuilder.AddAccentBar()`や`DefenseRowView.IconFrame`のような
   色付き図形で表現すること。対策8種はキーごとの色（`GameManager.DefenseIconColor()`）で
   見分けられるようにしてある。

6. **角丸・影は独自テクスチャ生成をせず、Unity組み込みアセットで実現する。**
   `SceneBuilder.ApplyRounded()`がUnity標準の`UI/Skin/Background.psd`(パネル用)・
   `UI/Skin/UISprite.psd`(ボタン用)を使い、`AddShadow()`が`UnityEngine.UI.Shadow`コンポーネントで
   ドロップシャドウを付ける。Claude Codeは見た目をエディタ上で目視確認できないため、
   実績のある標準機能だけで組むという方針（独自シェーダー/生成テクスチャは避ける）。

7. **ナビゲーターキャラは`NavigatorPersona`(ScriptableObject)経由にする。**
   `GameManager`に`faceNormal`等を直接持たせる方式は廃止済み。`Assets/Personas/`配下の
   `Persona_Hinata.asset`等から、`GameManager.personas[]`で選ばれた1体が`_activePersona`に入る。
   表情スプライトだけでなく**セリフもキャラごとにアセット側が持つ**（空欄なら`GameManager`の
   共通セリフに自動フォールバックする`Pick()`）。タイトル画面の「ナビゲーターを選ぶ」ボタン列から
   選択でき、`PlayerPrefs`(`pws_selected_persona_index`)に永続化される。新しいキャラを増やすときは
   `SceneBuilder.BuildNavigatorPersonas()`に`GetOrCreatePersona(...)`を追加するだけでよい。

8. **ステータス表示は必ず「項目名＋数値」をひとつの文字列で更新する。**
   `RefreshUI()`が数値だけを書き込むと、SceneBuilderが置いた「予算 100」というラベルが
   「100」に上書きされ、何のゲージか分からなくなる(実際に発生した不具合)。
   `GameManager.ApplyStat()`に`v => $"予算　¥{v}"`のような書式デリゲートを渡す方式を守ること。

9. **LayoutGroupの`childControlWidth/Height`をfalseにするなら、子のサイズは自分で設定する。**
   falseのとき`LayoutElement.preferredWidth/Height`は無視され、子は自身の`sizeDelta`のままになる
   （動的生成した子は0サイズになって見えなくなる）。`BuildPersonaSelectButtons()`が
   `rect.sizeDelta`を明示しているのはこのため。逆にtrueにした場合は、子に必ず`LayoutElement`で
   サイズを与えること。

10. **画面シェイクはCanvasではなく`ShakeRoot`を動かす。**
    Screen Space - OverlayのCanvasはRectTransformがUnity側に固定されていて動かせない。
    `SceneBuilder`がCanvas直下に`ShakeRoot`を作り、ゲームUIを全てその配下に入れている。
    演出レイヤー(`EffectLayer`)だけは`ShakeRoot`の外に置き、揺れの影響を受けないようにしてある。

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

**重要：バッチモードはUnity Editorが起動中だと使えない**（「別のインスタンスが開いている」で失敗する）。
逆に言えば、加藤さんがUnityを閉じてくれさえすれば、Claude Code側でコンパイル確認・シーン構築・
生成結果の検証（`SampleScene.unity`をgrepしてオブジェクトの有無や重複、未割当参照を調べる）まで
自力でできる。UIを大きく変えたときは、憶測で「できたはず」と言わずにこの手順で必ず裏を取ること。

### 通しテスト（これが一番確実な裏取り）

`Assets/Tests/GameFlowSmokeTest.cs`が、実際にシーンを再生して
タイトル→事前クイズ→本編→雑務→次フェーズまで例外なく進むかを確認する。
Playした瞬間に出るNullReferenceの類はここで捕まるので、UIやフローを触ったら必ず流すこと。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe" -batchmode -runTests `
  -testPlatform PlayMode -projectPath "C:\Projects\PatchWorkSecure" `
  -testResults "C:\Projects\PatchWorkSecure\test_results.xml" `
  -logFile "C:\Projects\PatchWorkSecure\batch_log.txt"
```

`test_results.xml`の`total`/`passed`/`failed`を見る。
テストを足すときの注意：テスト用asmdefの`includePlatforms`は**空**にすること
（`["Editor"]`にするとPlayModeテストの対象外になり、1件も実行されないまま成功扱いになる）。

### アセンブリ構成

テストからゲーム本体を参照するため、3つのアセンブリに分かれている。
新しいスクリプトを足す場所を間違えると参照が通らないので注意。

| 置き場所 | アセンブリ | 用途 |
|---|---|---|
| `Assets/Scripts/` | `PatchWorkSecure` | ゲーム本体 |
| `Assets/Editor/` | `PatchWorkSecure.Editor` | エディタ拡張（Editor限定） |
| `Assets/Tests/` | `PatchWorkSecure.Tests` | テスト（`UNITY_INCLUDE_TESTS`時のみ） |

---

## 4. 現状（完了していること）

- 攻撃10種フル実装（IPA情報セキュリティ10大脅威2026準拠、グレード別キャラ付け・台詞付き）
- 防御8種フル実装（Lv1〜3、基礎防御率70%上限あり）
- モンテカルロ・バランス検証済み（`balance_sim.js`、放置プレイ5.6% vs 熟練78%）
- タイトル→事前クイズ→本編→エンディング→事後クイズ→結果サマリーの一連のフロー実装済み
- 画面レイアウトはダッシュボード型（上=ステータスバー / 左=対策リスト・リスク・ログ /
  中央=フェーズパネル / 下=キャラ立ち絵と吹き出し / 最前面=演出レイヤー）
- ナビゲーター「ひなた」実装済み（明るく元気な口調のセリフ16種、イメージカラー=ピンク）。
  `Persona_Aria`/`Persona_Chloe`は器だけのプレースホルダー（セリフ未入力＝共通セリフで動く）。
  表情スプライトは3体とも全種null（イメージカラーで塗った枠が代わりに出る）
- セリフは吹き出し内に1文字ずつ表示（タイプライター演出）
- パリィ演出（スイートゾーン可視化、PERFECT!!/GOOD!/MISS...判定、攻撃グレード別の速度スケーリング）
- `AudioManager`によるBGM/SEフック実装済み（クリップは全て未設定＝無音、素材が届けば差すだけでよい）
- `UIEffects`による手応え演出（被弾＝赤フラッシュ+シェイク+「被弾！」バナー、防御成功＝金フラッシュ+
  バナー、数値増減＝カウントアップ+浮遊テキスト+バースト、クイズ正誤＝フラッシュ+バナー）
- リスク表示（`GameState.EstimateExpectedDamage()`）。対策を買うと被害予測がその場で下がるので、
  投資の意味が数字で伝わる
- UIは角丸カード+ドロップシャドウ主体（Unity組み込みスプライトのみ、独自テクスチャなし）
- 教育クイズ（事前/事後、8問プール）実装済み、`EducationTracker`でPlayerPrefs永続化・CSV出力対応
- 対策強化パネル・攻撃選択パネルの動的UI生成
- Git/GitHub連携済み

## 5. 未完了・次にやってほしいこと（優先順位順）

1. **立ち絵素材の投入**：`Assets/Sprites/<キャラ名>/`（例: `Assets/Sprites/Hinata/`）に
   **透過PNGを1表情1ファイル**で置き、Unityメニュー「PatchWorkSecure → キャラ立ち絵を取り込む」
   を実行するだけでよい。`PersonaSpriteImporter`がファイル名から表情スロットを判定し、
   Sprite化・透過・ミップマップ無効といったインポート設定まで自動で整える
   （「シーンを自動構築」でも同時に走る）。
   必要な6表情とファイル名: `normal` / `proud`(confident) / `worried`(thinking) /
   `alert`(shocked) / `relieved`(embarrassed) / `sad`(crying)。
   素材が入るまでは組み込みスプライトで組んだチビキャラが代役として表示される。
   キャラ2・3を正式に作る場合は、名前・性格が決まり次第`Persona_Aria`/`Persona_Chloe`の
   `DisplayName`/`Description`/セリフ欄を更新すること（空のままでも共通セリフで動く）。
2. **ドット絵・見下ろし視点のオフィス背景ビジュアル**（将来的な検討事項、加藤さんが参考UIを提示済み）：
   部屋レイアウト・社員ドット絵・什器アイコンなど専用素材が必要な大きめの機能。素材が揃うまでは
   現行のパネル/カードUIで進行し、揃った時点で背景として組み込む2段階の計画。
3. **サウンド素材の調達**：`AudioManager`のフックは全て仕込み済みなので、BGM4種・SE13種の
   音源ファイルをInspectorの各AudioClip欄に割り当てるだけで鳴るようになる。
4. **攻撃アイコン10種の作成**（現在はアイコンなし、テキストのみ。優先度低）。
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
