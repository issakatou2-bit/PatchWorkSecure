# CompanyYear：書体・角丸の見直し（2026-09-27）

## 参考調査と判断

- [シャインポスト Be Your アイドル！・公式ゲームモード](https://www.konami.com/games/shinepost/gamemode/)：2025年発売の育成・経営作品。公式掲載のメンバー選択画面をブラウザーで確認。名前・主要操作の太さ、数値と説明のサイズ差、プロフィールとキャラクターの区分を参考にする。実際に使用しているフォントの銘柄は不明で、今回の採用書体と同一とは主張しない。
- [トモダチコレクション わくわく生活・住人とふれあう](https://www.nintendo.com/jp/switch/blfga/enjoy/index.html)：2026年発売の生活系作品。公式の会話画面を確認。丸みのある白い吹き出し、太めの選択肢、短い会話という使い分けを参考にする。黄色い外枠など固有の意匠は採用しない。
- 以上は近年の実例から得た設計上の判断であり、売上ランキングの網羅調査や「角丸が流行の唯一解」という主張ではない。会社運営の情報量に合わせ、窓・カード・操作・補助情報の階層を作る。

## 採用書体

| 用途 | 書体 | 意図 |
|---|---|---|
| 本文・補足・会話 | Zen Kaku Gothic New Medium | 長文を読める角ゴシック。OS既定書体から独立 |
| 見出し・数値・ボタン | M PLUS Rounded 1c Bold | 太い丸ゴシックで育成ゲームらしい強弱 |

見出し31〜53、数値29〜52、本文20〜25、補助14〜18を基準とする。文字の幅は書体で変わるため、既存の領域での省略・クリック遮蔽を再検証する。詰めすぎていた負の行間も解除。
角丸はダイアログ・会話窓18、パネル14、大きな選択ボタン12、通常ボタン8を基準にする。標準Knobの円形テクスチャを共有した9-sliceスプライトを保存し、半径を用途ごとに指定。標準UISpriteの拡大では縁がぼやけたため採用しなかった。背景全面と細いゲージは四角のまま。影は主要窓のみ、黒22%・下3px。
前回の無彩色＋白＋青という配色は維持。画像素材・ゲームルール・旧版SampleSceneは変更しない。

## フォントの再配布と生成

- [Google Fonts / Zen Kaku Gothic New](https://github.com/google/fonts/tree/main/ofl/zenkakugothicnew)
- [Google Fonts / M PLUS Rounded 1c](https://github.com/google/fonts/tree/main/ofl/roundedmplus1c)
- いずれもSIL Open Font License 1.1。原文をAssets/Fonts/CompanyYearへ保存。WindowsビルドのFontLicensesへもコピーする。フォント単体販売はせず、権利表示・ライセンスを保持してゲームに同梱する。
- OSにインストールしない。既存のmeiryoと旧版設定は維持し、新試作のメイン書体のみ切り替える。
- CompanyOpsTypographyで既知の文字を静的アトラスに収録。同書体の動的フォールバックを1024角・複数枚対応・ビルド時データクリアで構成。44pt/余白5、Scale=1を揃える。
- Zen未収録の全角チルダ（～）はRounded M+で補完。絵文字フォントは導入しない。
- UIは標準の9-sliceとShadowを利用し、独自画像・シェーダーは追加しない。

取得元TTFのSHA-256：

- RoundedMplus1c-Bold.ttf：C358630584E8E2D8FBD6121D0F4693255FFEF6D1E6D4F3441FD6E5A963A11F9E
- ZenKakuGothicNew-Medium.ttf：651A3F7280B7F36262601EE76D8388A8DC4372DCC67AFF025A608939A562B525

## 最終検証（2026-09-28）

- PlayMode 47件中47件成功。本文・見出しの割当、日本語と全角記号の補完、クリック遮蔽、重要文字の省略を検査。
- 実画面 `Artifacts/CompanyOps/33-typography-rounded.png`（1280×720）と `34-typography-team.png`（1600×900）を目視確認。文字の強弱と角丸、会話面の白、社員カードの収まりを確認。
- Windows版を更新し、実行ファイルでも12か月 PASSED・終了コード0。文字欠け警告・ゲーム例外なし。両書体のOFL原文の同梱を確認。
- Unityのビルド時ライセンス検証警告、実行版終了時の既存ComputeBuffer解放警告は未解決。見た目の最終的な好みや楽しさは、この自動検証では判定しない。
- 表示変更は既存セーブにも反映する。旧版SampleSceneとゲームルールは変更していない。
