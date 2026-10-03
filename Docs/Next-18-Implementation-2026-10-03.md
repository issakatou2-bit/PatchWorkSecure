# Next-18 宣伝素材の撮影（2026-10-03）

Next-17の完了報告後、承認済み画面だけを撮影。ルール・数値・デザインは変更しない。Windows版はNext-17の0.16.0のまま、再ビルドしない。

## 素材

`Artifacts/Promo/shots/`の9フォルダに1920×1080・30fpsのPNG連番と`sounds.json`。合計1,380コマ・46秒。素材はgit追跡対象外。

| 場面 | 秒 | コマ | 中身 |
|---|---:|---:|---|
| 01-office | 5 | 150 | 平穏なオフィスとひなたの手振り |
| 02-planning | 6 | 180 | カード選択からランクアップ |
| 03-incident | 6 | 180 | 警告・揺れ・事件の札 |
| 04-mini-b | 4 | 120 | 封じ込めから成功の判子 |
| 05-mini-c | 4 | 120 | メール判定から成功の判子 |
| 06-mini-e | 4 | 120 | ログ調査から成功の判子 |
| 07-rivals | 5 | 150 | 2年目の強敵の影絵3体 |
| 08-pairs | 6 | 180 | 日記の二人絵2枚・会議の二人絵を各2秒 |
| 09-title | 6 | 180 | タイトルとロゴ |

計画画面に判子は無い。加藤さんの10/3回答どおりカード選択→ランクアップを採用し、月報の追加素材や新しい判子は作らない。

`Tools/Create-Promo-Contact.py`は全PNGのヘッダー実寸・連番欠落・音のコマ範囲・音源の存在を検査し、最初/中央/最後の27枚を`Artifacts/Promo/contact-sheet.png`にまとめる。`manifest.json`に各場面の秒数・コマ数・音イベント数を記録する。

## 撮影の仕組み

`CompanyOpsNext18PromoTests.cs`の`Next18Promo_九場面の30Hz連番と実再生の音を保存する`はExplicit。撮影名を指定し、Explicitを許可した実行だけで動く。通常の全件には入れない。

`Time.captureFramerate=30`を設定し、撮影専用カメラで毎コマをPNGへ保存。通常のCanvas設定・撮影前のcaptureFramerateは終了時に復帰する。既存の切替演出が完了してからタイトルを撮り始める。再撮影では既存の連番と音一覧を同フォルダの`previous-*`へ退避し、混在させない。

UnityのcaptureFramerateは非スケール時間を固定しないため、PNG保存に時間がかかるとUIの非スケール演出だけが飛ぶ。CompanyOps内のテスト限定時計を撮影中だけ1/30秒ずつ進める。通常のテストではUnityの時間をそのまま返し、テストを含まないPlayerには時計の定義も音観測の実装も無い。根拠：[Unity Time.captureDeltaTime](https://docs.unity3d.com/ScriptReference/Time-captureDeltaTime.html)。

ミニゲームは既存のUIを自動操作。採点・制限時間は変更しない。C/Eは終盤まで進めてから4秒を撮る。Eの初回撮影では最後の数え上げが終わらず判子がまだ非表示だったため、開始前の進行を延ばして再撮影。点数を直接指定していない。

## 音一覧と使用条件

実際のAudioSource再生後に、テスト限定の観測口でコマ番号・音源・音量・pitchを記録する。場面開始時に継続中の音は`ongoing`と再生位置、音量の変更は`gain`として記録。`source`は場面内の音源番号。実録音や完成した音声ミックスではない。

音源アセットは実在する`Assets/`からの道のり。コードでその場に作る封じ込めSEにはアセットファイルが無いため、実際のAudioClipをPCM16 WAVとして`Artifacts/Promo/audio/generated/`へ保存し、その道のりを記録する。存在しないAssetsパスを作らない。

- ひなたの無料プラン音声：一覧には含むが、**動画で使用禁止**。各行にprivate-onlyを付記。
- BGM：利用条件確認まで動画に使用禁止。各行にpendingを付記。
- かのん・りりぃ：既存の出典・利用条件に従う。AI音声の表示が必要。
- SE：既存の生成SE。新しい外部音源の追加は無い。

文字点検では実在する公開IPや実URLを禁止し、ゲーム内の私有ネットワーク10系・文書用IP・予約ドメインだけを許可する。既存の架空会社の画面を撮影し、デバッグ表示や宣伝用の新字幕は追加しない。動画の編集・公開は行っていない。

## 検証

撮影専用テスト1/1、3アセンブリのコンパイルは成功。撮影後のPlayerPrefs15キーの存在/型/値とフォント8ファイルのSHA256が開始時と一致、一時シーン0。`Artifacts/next-18-capture-resume-tests.json`、`Artifacts/Next17/preservation-next18-capture.json`に記録。

最終Verifyは`Artifacts/Next18/verify-summary.json`。Core/CompanyOps/Personas/ThreeYears/EndlessYearsは成功。Depthsだけは既知の最適化完走率58%→85%（+27pt）で未達のまま。±5ptの基準を緩めず、成功扱いにしない。

- 本編：2,160挑戦、初回225/540=41.67%、4回目まで365/540=67.59%で従来どおり。
- 終わりなき年度：900挑戦、考える6方針の中央6.50〜7.75年、最長8.33年。SS60/600=10%。受入済みNext-12のruns.csv/summary.csvとSHA256一致。
- 1年だけの遊び：900年度・10,779月のCSVがNext-8前とSHA256一致。
  - years.csv：`44EE642013CE5D216A40C75AFA8F8D718EB650E9EFF98087861613D7CCB7FA58`
  - turns.csv：`830D58BB7C57A016EB67DCCA407B68B5A0E9465CE4776211C90DF86CEFDF2C63`

全件1回目は294/294成功（`Artifacts/next-18-all-1-tests.json`）。Next-17全件とテスト名/件数が完全一致し、撮影テストは0件。1回目後も設定15キー・8フォントのSHA256一致、一時シーン0（`Artifacts/Next17/preservation-next18-all-1.json`）。テスト定義を含まない本体ソースも別コンパイル成功（Windows版の再ビルドではない）。

2回目では、Next17の仮想パッド六本試遊で`MfaDeny`の対象が無いという1件の失敗が出た。残り時間が回答選択の途中で尽きると、正常に結果へ切り替わって回答ボタンが消える。テスト用の選択/決定へ適用条件を追加し、終了済みなら古い回答を探したり結果のボタンを押したりしない。通常のメニュー検査は従来どおり対象の存在を厳密に検査する。六本試遊の同じテスト内で、終了直後の古い拒否要求を明示的に再現し、結果を進めないことも検査する。時間延長・採点変更・S判定の緩和は無い。

失敗した2回目は中断し、成功には数えない。中断後のPlayerPrefs15キーと8フォントを復帰/照合、一時シーン0。修正後の六本パッド試遊（境目の回帰も同じテスト内で検査）は1/1成功、3アセンブリのコンパイル成功、設定/フォント/一時シーンも一致。`Artifacts/next-18-pad-boundary-tests.json`、`Artifacts/Next17/preservation-next18-pad-boundary.json`。

修正後の全件は**294/294を2回連続成功**（`Artifacts/next-18-all-1-clean-tests.json`、`Artifacts/next-18-all-2-clean-tests.json`）。両回とも設定15キー・8フォントは元どおり、一時シーン0（`Artifacts/Next17/preservation-next18-all-1-clean.json`、`preservation-next18-all-2-clean.json`）。終了後に9場面1,380枚と音一覧を再点検し、確認一覧を更新。文字のはみ出し/デバッグ表示なし、B/C/Eの最終コマでS判子、二人絵3枚、タイトルの最初のコマも切替済みの画面であることを確認。

Unityは編集状態で`Assets/Scenes/CompanyYear.unity`、版0.16.0・背景false・resizable true・FullScreenWindowを維持。Windows版は再ビルドしていない。Next-18の完了報告後、追加指示のNext-19（テストの組分けと実行回数の最適化）だけを続ける。
