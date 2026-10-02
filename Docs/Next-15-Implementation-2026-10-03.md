# Next-15 実装と確認

Next-14の全280件2回連続成功・Windows更新・完了報告の後に着手。数値・抽選・採点は変更しない。

## ① 情報の点検の残り

- 54出来事の台詞/兆候/調査、B/G/F2の説明、18チケット、36季節と旧暦12月、社員4人、ひなた191行、仲間24行、7出典と対応を点検。結果・未確認・要相談5件は`Docs/Fact-Check-2026-10-02.md`。
- 明らかな言い切り1行を`maxim_segment_v2`「区切っておけば、広がりを抑えられる！」へ。旧音源は再生しない。音源が届いたら同名ファイルで接続する。191行・20格言と旧既読キーを維持。
- 比較の変更前は旧台本文を同じUnity画面で再現したもの。旧バイナリの撮影ではない。音源を差し戻さず、実データの文も直後に復帰する。
- 関係テスト：Next15Fact 2/2、Next13Fact 3/3、Next13Maxim 4/4。3アセンブリのコンパイル成功。字幕全文の比較は`Artifacts/Next15/01-fact-comparison.png`。

## ② Steam向けの画面設定

- 版0.16.0、初回はボーダーレスのフルスクリーン。既存設定の行・ボタンでウィンドウ1920×1080への切替を追加。ウィンドウはサイズ変更可。会社名・製品名・アイコンは維持。
- 実装前の`runInBackground`はfalseだったので指示どおり維持。隠した自動検証だけが一時的にtrueにする従来の仕組みも維持した。通常プレイの背景音継続を新たに保証したものではない。
- 新しいPlayerPrefsキーも存在・型・値ごと復帰する。Editor/TestModeではOSの解像度を変えない。Canvasの既存Expand（1600×900）のまま、上下・左右に余白で収める。
- Next15Display 2/2、3アセンブリのコンパイル成功。13画面×5サイズ＝65枚を実寸描画し、全操作領域の画面内・重要文字の欠け・状態不変を検査。5枚の一覧と設定前後を目視。`Artifacts/Next15/<サイズ>/`、`02-settings-comparison.png`。写真を拡大して解像度対応と見せていない。

## ③ テストの後片付け

- TestRunnerの終了後・Editor起動時に遅延点検。テスト中、コンパイル中、開いているシーンは削除しない。
- `Assets`直下の`InitTestScene<GUID>.unity`かつTest Frameworkの起動用コンポーネントを持つものだけ。元ファイルとmetaを`Artifacts/TestCleanup/`へ退避後、AssetDatabaseで削除する。名前が似ているだけの通常シーンは残す。
- Next15Cleanup 2/2、3アセンブリのコンパイル成功。Editorで作った検証用2シーンにより、開いているもの0件削除・確認できた取り残し1件を退避して削除・通常シーン0件削除を確認。`Artifacts/Next15/cleanup-integration.json`。利用者の素材を削除した検証ではない。

## ④ 最終確認

- Core/CompanyOps/Personas/ThreeYears/EndlessYearsは成功。Depthsの58%→85%（+27pt）は従来どおり未達。±5ptを緩めず、全Verify成功としない。限定50.25%、非優越候補平均1.980、SS92/900。
- 1年900年度・10,779月のCSVはNext-8前とSHA256一致：`years.csv`=`44EE642013CE5D216A40C75AFA8F8D718EB650E9EFF98087861613D7CCB7FA58`、`turns.csv`=`830D58BB7C57A016EB67DCCA407B68B5A0E9465CE4776211C90DF86CEFDF2C63`。
- 3年本実装は初回225/540（41.67%）、4回目まで365/540（67.59%）を維持。エンドレス900挑戦の2CSVもNext-12確定版とSHA256一致。6方針中央6.50〜7.75年、最長8.33年、総合B/A/S/SS=7116/8032/11707/12329、SS60/600。人間の楽しさ・学習効果・10年到達の証明ではない。
- 記録：`Artifacts/Next15/verify-summary.json`と同フォルダの各Verifyログ。
- 全PlayModeの1回目は286/286（1867.89秒）。PlayerPrefs15キーの存在・型・値、8フォントのSHA256が開始時と一致し、一時シーンの残存は0。`Artifacts/next-Next15-full-1-tests.json`、`Artifacts/Next15/preservation-full-1.json`。
- 2回目も286/286、失敗・スキップ・未確定は各0。試験Durationの合計1866.87秒。2回目の後も15設定・8フォントが一致し、一時シーン0。`Artifacts/next-Next15-full-2-tests.json`、`preservation-full-2.json`。
- Windows私的試遊版0.16.0を更新。Editorの呼出しは5秒の応答待ち上限に達したが、ビルド自体は継続して成功した（重複起動していない）。8フォントはビルド前にUnity標準パッケージで退避し、変化した作業データを取り込み直してSHA256一致。`fonts-before-build.unitypackage`、`build-font-preservation.json`。
- Windows版を非表示で起動し、ウィンドウ1920×1080・フルスクリーン切替・版0.16.0、SE15用途、BGM2曲、ひなたの事件音声、12か月の年間通しがすべてPASSED、終了コード0。従来からの終了時ComputeBuffer診断は残る。`player-smoke.log`。Steam Deck実機での性能・操作を確認したものではない。
- ビルドとWindows動作確認の後も15設定の存在/型/値・8フォントのSHA256が開始時と一致、一時シーン0。`preservation-final.json`。Editorは変更なしの`CompanyYear`を開いた状態。
- ①②③は`8e59cf5`/`d55ca57`/`54eef84`の別コミット。情報点検の要相談5件を報告し、新しい機能・見た目の作業は始めない。ひなたを含むWindows版は私的試遊限定、公開・配布・動画投稿は禁止。
