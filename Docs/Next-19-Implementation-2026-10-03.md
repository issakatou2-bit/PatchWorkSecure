# Next-19 テストの組み分けと実行回数

ゲームの数値・ルール・見た目、テストが検証する内容は変更しない。Next-18の完了報告後に着手。

## 組み分け

| 組 | 対象 | 件数 |
|---|---|---:|
| 普段（属性なし） | 以下の2組以外 | 160 |
| Capture | 画面を撮影して保存するテスト。共通撮影処理を間接的に呼ぶものも含む | 129 |
| Long | 3役で5年度を通す3件、ゲームパッドの6本Sと1年通し | 5 |
| 合計 | 前回の通常実行と同じ完全名 | 294 |

Longは撮影を含んでもCaptureを重ねない。実コードのExplicit 2件（Next17Beforeの変更前撮影・Next18Promoの宣伝連番）は別枠のまま。Next-19の文書はストア撮影もExplicitとしていたが、既存のNext14StoreUI 2件は通常の294件に含まれていたため、元の属性を変えずCaptureとして残した。テストを分割・追加・削除せず、60ファイルの属性だけを追加した。属性を取り除いた本文が変更前と一致することと、Unityが返す294件の完全名がNext-18の最終全件結果と一致することを確認した。

根拠は`Artifacts/Next19/groups.json`、`classification.json`、`inventory-check.json`。

## 実行方法

`Tools/Run-TestGroup.ps1`から起動済みEditorの`CompanyOpsTestGroups`を使う。Unityの実際のテスト一覧を取得し、Explicitを除外した完全名配列で実行する。印のないテストにNormal属性を足したり、CLIの単純一致フィルタへ否定条件を渡したりしない。

```powershell
# 普段＋撮影を1回（通常の指示の最後）
./Tools/Run-TestGroup.ps1 -Group RegularCapture
# Windows私的試遊版では全件1回、その後に普段だけ1回
./Tools/Run-TestGroup.ps1 -Group All
./Tools/Run-TestGroup.ps1 -Group Normal
# 一覧／個別の組
./Tools/Run-TestGroup.ps1 -Group Inspect
./Tools/Run-TestGroup.ps1 -Group Capture
./Tools/Run-TestGroup.ps1 -Group Long
# 失敗分＋関係分は完全名で指定。元の記録は残す
./Tools/Run-TestGroup.ps1 -Group All -Only @('<失敗した完全名>','<関係する完全名>')
```

出力は実行ごとのJSON/XML。上書き・0件・組の重複・不明な再実行対象を拒否する。再読み込みをまたいで実行情報を保持し、予定した完全名と実際の成功結果の一致も確認する。中断・未実行・Skippedは成功回数に数えない。通信が切れても勝手に開始を繰り返さず、Editorと実行記録を確認する。新しいテストを追加するときは実際の一覧から自動で選ぶため、294という固定値を実行処理に埋め込まない。

公開版はLong込み全件を2回連続。各項目の途中は関係分＋コンパイル。数値変更はVerify系と1年CSVのSHA256照合も行う。フォント・PlayerPrefsの復帰、撮影中のGPU競合の点検は従来どおり。

## 最終確認

3アセンブリのコンパイル成功。各組を1回ずつ実行し、すべて初回成功。再実行・中断なし。3組の結果を合算した294件に重複・欠落がなく、Next-18の294件と完全名が一致することも確認した。

| 組 | 成功 | テスト実行時間 | 開始要求から完了まで |
|---|---:|---:|---:|
| 普段 | 160/160 | 146.87秒 | 153.15秒（2分33秒） |
| Capture | 129/129 | 1,266.97秒 | 1,270.81秒（21分11秒） |
| Long | 5/5 | 837.39秒 | 841.22秒（14分1秒） |
| 合計 | 294/294 | 2,251.24秒 | 2,265.18秒（37分45秒） |

通常の指示の最後に流す普段＋Captureは今回の計測では23分44秒。所要時間はGPU競合などで変動するので固定の期限とはしない。全件を追加で2回流さず、Windows版も作り直していない。

各組の終了後に設定15キー（存在・型・値）と8フォントのSHA256が元の状態と一致、一時シーン0を確認。EditorはCompanyYearの編集モードへ復帰し、撮影用時計は無効・Time.captureFramerateは0。1年モード900年度・10,779月のCSVもNext-8前のSHA256と一致した。数値を変更していないため、今回はVerifyの試算を重複実行せず、Next-18の最終試算を維持する。Depthsの既知の+27pt未達は基準を緩めず残す。

実行記録：`Artifacts/Next19/normal.json`/`capture.json`/`long.json`と各XML、合算`final-check.json`。保存状態は`Artifacts/Next17/preservation-next19-{normal,capture,long}.json`。

入力チェックも、空の`-Only`をUnityに接続せず拒否することを確認した。PowerShell 5.1向けにスクリプトのBOM付きUTF-8とJSONのUTF-8読み込みを明示。テストの追加・件数の増減なし。CLAUDE.mdとAGENTS.mdの4〜7を同期し、終了後は新しい作業に進まない。
