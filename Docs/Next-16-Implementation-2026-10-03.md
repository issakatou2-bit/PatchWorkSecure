# Next-16 実装と確認（2026-10-03）

## 決定の反映

- 報告の格言を `maxim_report_v2` の字幕先行に変更。旧IDからの呼び出しも新版に寄せ、旧音声を再生しない。既読キーは旧IDへ正規化し、格言20種と保存互換を維持。
- かのんの `kanon_opening` を更新済み台本に同期。同じID・同じ音源参照を維持。りりぃの `eng_factor` は変更しない。
- 日記2年目9月・3年目7月の本文を「科目B」へ。3年目7月の冒頭は `diary_y3_07_v2` の字幕先行。現行台本は191行のまま。
- 事件の参考資料ボタンと同じ行へ、架空の想定であることと参考資料の位置付けを表示。個別の出来事のニュース13か所を「事例。」から「想定。」に変更。試験の文章を指す「事例」やタイトルの断り書きは維持。
- 入社・退社・異動・委託の4チケットに、IdPの資料が参考であることを表示。sourceの割当・URL・数値・抽選・採点は変更しない。
- 情報点検の5件を決定済みとして記録。IPA本編PDFと個別実例の裏付けは引き続き未確認。

## 表示の確認

変更前の実装を撮影してから変更した。1280×720・1280×800・1920×1080・2560×1440・3440×1440の事件画面5組を `Tools/Compare-Mock.py` で比較し、`Artifacts/Next16/compare-event-<解像度>.png` に保存して目視確認。4チケットも同じ5解像度で撮影し、合計25画面の文字の欠け・ボタンとの重なり・画面外を検査した。

比較ツールは、Unityの変更前画像を参照にする場合だけ元の解像度を維持できるようにした。HTMLモックを参照にする既定の1600×900処理は維持。

最初の関係テストでは共通フッター整列後の重なりを検出したため、注意書きがある既存フッターだけ配置と光の幅を調整。チケット撮影も開幕演出終了を待つようにした。修正後、関係PlayMode **3/3成功**、Unityと単体の3アセンブリのコンパイル成功。関連確認後のPlayerPrefs15キーとフォント8アセットは開始時と一致し、一時シーンは0。

証跡：`Artifacts/next-Next16-related-final2-tests.json`、`Artifacts/Next16/preservation-related.json`、`Artifacts/Next16/Before/`、`Artifacts/Next16/After/`。

## 最終確認

実装コミット：`6c9e683`、既存音声連動テストの新ID対応：`4e26738`。

最初の全件は288/289成功。旧音声連動テストが再生IDを `maxim_report` と期待していた1件だけ失敗した。新仕様の `maxim_report_v2` を期待するよう変更し、保存の既読キーの期待値 `maxim_report` は維持。修正後の関係テスト1/1と3アセンブリのコンパイルは成功。初回全件後も設定15キー・8フォントは復帰し、一時シーン0。失敗した実行を全件成功に含めない。

Verifyは実装後に1回実行。Core・CompanyOps・Personas・ThreeYears・EndlessYearsは成功。Depthsは既知の最適化完走率58→85%（+27ポイント）によって未達のまま。±5ポイントの基準を緩めず、成功扱いにしない。限定50.25%、非優越候補平均1.980、SS92/900も不変。

本実装の3年本編は2,160挑戦、初回225/540（41.67%）、4回目まで365/540（67.59%）で不変。エンドレスの考える6方針は中央6.50〜7.75年、最長8.33年、SS60/600で不変。

1年だけの900年度・10,779月はNext-8変更前、エンドレス900挑戦はNext-12確定版とSHA256一致：

| CSV | SHA256 |
|---|---|
| 1年 `years.csv` | `44EE642013CE5D216A40C75AFA8F8D718EB650E9EFF98087861613D7CCB7FA58` |
| 1年 `turns.csv` | `830D58BB7C57A016EB67DCCA407B68B5A0E9465CE4776211C90DF86CEFDF2C63` |
| エンドレス `runs.csv` | `67C4C0838E1822D217C4E3952FB41604D8922DC20489F4CCBB66C8857377D2EC` |
| エンドレス `summary.csv` | `5F7C2E89DEE8CA7D84E0FE7B2E02F626F5693D1C94BF8BDFE8A0E340E8F7295C` |

証跡：`Artifacts/Next16/verify-summary.json`、`verify-*.txt`。

### 全件と復帰

修正後に **全289/289を2回連続成功**（1881.22秒・1878.77秒）。両方とも失敗・スキップ・不確定0。全件後のPlayerPrefs15キーの存在・型・値と、8フォントのSHA256は開始時と一致。一時テストシーンは0。

証跡：`Artifacts/next-Next16-full-2-tests.json`、`Artifacts/next-Next16-full-3-tests.json`、`Artifacts/Next16/preservation-full-2.json`、`preservation-full-3.json`。

実バンクで台本191行・現行IDの音源169本・字幕のみ22行を確認。新ID2行はどちらも音声未接続で、同名の音源が届いたら従来の仕組みで接続できる。`Artifacts/Next16/voice-counts.json`。

### Windows私的試遊版

`Builds/CompanyYear/PatchWorkSecure-Year.exe` を0.16.0のまま更新。起動中のUnityで既存ビルド処理を実行し、フォントをネイティブのパッケージで退避・復帰。呼び出しの5秒の応答待ちは時間切れになったが、処理は継続してログのビルド成功を確認した（再実行していない）。ビルド前後の8フォントもSHA256一致。

Windows版そのものを隠れたウィンドウで起動し、画面切替/版・SE15用途・ひなたの事件音声・BGM2曲・12か月進行がすべてPASSED、終了コード0。従来からの終了時ComputeBuffer解放の診断は残る。これは人間の試遊評価ではなく、自動動作確認。

UnityはCompanyYearを開いた編集状態に復帰。通常の背景設定false・画面のサイズ変更有効を維持。最終の15設定・8フォントも一致し、一時シーン0。

証跡：`Artifacts/Next16/player-smoke.log`、`player-smoke-result.json`、`build-font-preservation.json`、`preservation-final.json`、`final-context.json`。

AGENTS.md/CLAUDE.mdの6・7を同じ内容に更新。開始時から存在する無関係なフォント作業データ・接続設定・素材metaはコミットしない。ひなたの音声を含む版は私的試遊限定（配布・公開・動画投稿禁止）。新しい作業は開始せず、ここで終了。
