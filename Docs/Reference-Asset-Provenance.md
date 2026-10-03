# 参考画像に合わせた暫定素材の来歴

組み込み画像生成ツールを使用。外部のCLI/APIへの切り替えは行っていない。

- 背景: `Assets/Art/Office/office-topdown.png`
- ひなた: `Assets/Sprites/Hinata/hinata_normal.png`
- ユーザーから手描き希望が伝えられた後、新しい生成は行わず、承認された背景切り抜きと衣装のベクター編集を実施。
- ひなたは生成画像に手作業相当の修整を施した暫定素材であり、人間が手描きした原画ではない。

## 背景の生成指示

Use case: stylized-concept. Asset type: top-down pixel-art office background for a real Unity game. Image 1 is the visual reference, use ONLY the central office map as style/composition reference. Create one square 1024x1024 tilemap-like office environment matching that central map: orthographic three-quarter top-down RPG perspective, NOT an isometric diamond. Roof removed, rectangular office with five rooms, reception corridor, cubicle desks, monitors, server room in upper center, training/meeting room upper right, storage lower right, entrance at bottom, hedges and trees around exterior and a small parking area below. A handful of tiny chibi employee sprites as environmental details. Crisp restrained 16-bit pixel art, consistent pixel clusters and grid-aligned walls, muted moss greens, blue-grey floors, warm beige corridor, subdued contrast, no painterly gradients or photorealism. Composition fills the square with office and grounds, no empty cream margins. This is just the game map asset. Absolutely NO UI frames, no large portrait, no speech bubbles, no icons floating above rooms, no text or labels, no letters, no numbers, no watermark. Preserve the reference's cozy simulation-game atmosphere.

## 暫定キャラ原画の生成指示

Use case: identity-preserve. Asset type: ONE transparent full-body chibi navigator game sprite. Image 1 is the character identity and art-style reference. Isolate and faithfully recreate ONLY the small pink/salmon-haired girl at the BOTTOM LEFT underneath the pink label, NOT the large right-side portrait and not the blue or purple girls. Keep her recognizable design: short salmon pink twin-tails with dark ribbons, side-swept bangs, large reddish-brown eyes, tiny body about 2 heads tall, pink blazer with dark piping over white shirt and dark red bow, pink skirt, dark shoes. Same friendly cheerful expression and little hands raised close to her chest. Thick slightly irregular dark outline, simple flat cel colors and small highlights, cute hand-drawn game mascot feel, NOT glossy AI anime rendering, no realistic skin, no 3D. Full body including shoes, centered, with 8% empty padding all around. Preserve face shape, hair silhouette, outfit and tiny proportions from reference. Output ONLY this single character on genuinely TRANSPARENT background with real alpha. No checkerboard painted into background, no white rectangular background, no floor or shadow, no text, no speech bubble, no badge, no other characters, no extra props. 1024x1024 PNG.

この出力は実際にはRGB画像にチェック柄が描かれていたため、そのまま透過済みとして採用しなかった。`Tools/Prepare-Hinata.cjs` でアルファを作り、元の学生服風衣装を `Tools/Hinata-workwear.svg` で置き換えた。

## 2026-09-28 承認済み計画モックのUI素材

`Assets/Art/UI/`は加藤さん承認済みの`Docs/Mockups/planning-screen.html`のCSS・SVG図形を、`Tools/Generate-PlanningUI.py`（Pillow）でPNG化したもの。外部の素材サイト・既存ゲームの画像は使用していない。形状・色・線幅はリポジトリ内モックから制作したプロジェクト用素材で、新たな第三者ライセンスやクレジットはない。人間が描いたキャラクター原画とは扱わない。

- `round-*`／`stage-top`：白い角丸と上だけ丸いマスク。EditorのSpriteDataProviderで9-slice境界を設定。
- `soft-shadow`：半透明の紺の角丸にぼかし。標準Imageで描画し、独自シェーダーは不使用。
- `planning-gradient`／`stage-shade`／`button-shine`／`ribbon`／`speech-tail`／`petal`／`snow`：モックのグラデーション・帯・吹き出し・季節・光。
- `icon-*`：モック内の24単位SVGを同じ太さの図形として書き出し。
- `office-blur`：既存オフィス画像の彩度1.2・ぼかし（900pxで7px、画面で2倍になり14px）。元画像は変更しない。
- `hinata-shadow`：既存ひなたのアルファから作った影。ひなたの元画像・顔・衣装は変更しない。

画像・参照設定は`OpsPlanningArt`にまとめ、シーン再生成でも同じ設定を読み込む。既存アートの生成由来については上の記録を引き継ぐ。

## 2026-09-29 効果音の試聴候補（B案を試遊用に投入）

`ArtSource/SfxCandidates/`の22音と連続試聴2本は、`Tools/Generate-Sfx-Candidates.py`によるプロジェクト用の独自プロシージャル生成。倍音・ノイズ・短い残響から作り、第三者の録音・サンプル・音楽素材・学習モデルは使用していない。AIサービスで生成した音とは扱わない。

新たな第三者素材ライセンスや追加クレジットはない。加藤さんの「B案かなぁ。一旦そのSEでやってみて」の回答に基づき、B-brightの11音だけを`Assets/Audio/CompanyYear/SFX/B-bright/`と`YearSounds.asset`へ登録した。原音は変更せず保持。最終ミックスの承認ではない。詳細と場面対応は`Docs/Sfx-Candidates-2026-09-29.md`。

## 2026-09-29 ひなたの新デザイン（生成・承認後に投入）

- 生成：ChatGPT（加藤さんのログイン済みアカウント、アプリ内ブラウザからClaude Codeが操作）。指示文の原型は`Docs/Hinata-Sheet-Prompts.md`。
- 経緯：8頭身の設定画 → 2〜5頭身の比較 → デフォルメ強め（色付き・線画）→ **加藤さんが3頭身の線画を選択** → 色鉛筆塗り → アニメ塗り → **太い輪郭線のアニメ塗りに決定**。表情18種・ポーズ18種を3×2のシートで生成。
- 原画：`ArtSource/Hinata/gen-20260929/hinata-*.png`（ChatGPTの生成物をそのまま保存）。切り抜き：同`cut/`（`Tools/Cut-CharacterSheet.py`で外周の白だけを透過）。一覧：同`contact-sheet.png`。
- 加藤さんの全面置き換え指示に基づき、共通キャンバスの`final/`から表情18・ポーズ18を新試作と旧版へ投入。今回は再生成・描き直しをしていない。旧暫定絵は`ArtSource/Hinata/legacy/`へ退避。手の見える5ポーズを原寸確認したが、隠れた指までの全数保証はできない。詳細は`Docs/Hinata-Replacement-2026-09-29.md`。販売物への利用可否は生成サービスの規約を確認すること。

## 2026-09-29 ロゴとアイコン

`Assets/Art/UI/Logo/`はプロジェクト内のSVG（`ArtSource/Logo/*.html`）からClaude Codeが書き出した透過PNG。ツギハギの盾とプロジェクト名を組み合わせた独自図形。他作品のロゴ素材は使用していない。書体M PLUS Rounded 1cのOFL原文は同梱のものを保持。配置は`Docs/Mockups/Logo.md`。Default Iconは設定済み、製品名の案は未採用。

## 2026-09-29 ひなたの差分と感情マーク

- 基本ポーズの目口差分は、Claude Codeが用意した`ArtSource/Hinata/gen-20260929/final/pose_fists*.png`（`Mockups/Hinata-Motion.md`と`Tools/Make-FaceFrames.py`）をそのまま利用。新たな生成・顔の描き直しは行わず、目と口の範囲をUnityのマスクで重ねている。原画の生成由来・商用利用前の確認は上の記録を引き継ぐ。
- `Assets/Art/UI/HinataEmotions/`の6PNGは`Tools/Generate-Hinata-Emotions.py`で、紺`#1d2a44`の輪郭の独自図形を描画。キャラクター画像・第三者素材・フォント・AI画像生成は使用していない。新たな素材ライセンスやクレジットはない。キャラの手描き完成原画とは扱わない。
- 演出は既存のUnity UI・画像・B案SEを使用。音程差とBGMの音量変化を追加したが、原音・曲・未承認のA案は変更していない。実装と検証は`Docs/Hinata-Motion-and-Feel-2026-09-29.md`。

## 2026-09-29 Quick-Wins-2の表示と音

コイン・前月比の矢印はUnity UIの図形と文字で描画。社員の顔マークは既存の図形アイコンを実際の支援者に結び付けて使用。新しい画像・第三者素材は追加していない。心音・無効操作の短い二打は登録済みB案のダメージ音の再生時間・音程・音量を調整し、原音とアセット登録は変更していない。専用の新しいSEを承認前に投入したものではない。

## 一枚絵・追加キャラ（2026-09-29）

- `Assets/Art/KeyVisual/title-kv.png`（タイトルの集合絵）、`focus-secretary.png`（秘書の主役絵）、`focus-hinata.png`（ひなたの主役絵＝`focus-hinata-14.png`）、`focus-engineer.png`（エンジニアの主役絵＝`focus-engineer-8.png`）は、ChatGPT（加藤さんのログイン済みアカウント、アプリ内ブラウザからClaude Codeが操作）で生成したもの。原画と試行の履歴は`ArtSource/Characters/ideas-20260929/`（`title-kv-1〜3`、`focus-*`）。加藤さんが4枚を本採用と判断（ひなた・エンジニアは基準の絵を添付し、範囲の編集で細部を直した版）。
- 追加キャラ（社長秘書・金髪エンジニア）はオリジナルの設定。実在の人物・他作品のキャラの名前や画像は指示に使っていない（金髪エンジニアの方向性は加藤さんの参考作品の雰囲気を言葉で伝えただけ）。
- 生成サービスの規約上の商用利用の可否は、販売前に確認すること。
- `Assets/Art/UI/Bubbles/`の4PNG（困りごとの泡の青・ピンク・金と、金の点線の輪）は、`ArtSource/UI/bubbles.html`のCSS（放射グラデーション・縁・影）をClaude CodeがEdgeで背景透明のまま書き出したもの。AI画像生成・第三者素材は使っていない。
- `Assets/Art/UI/Craft/`の8PNG（縫い目の枠・上からの光沢・箔押しの光・斜めの帯・紙の地紋・網点・走査線・集中線）は、`Tools/Make-CraftTextures.py`でClaude CodeがPillowで描いたもの。白で描き、Unity側で色を付けて使う。AI画像生成・第三者素材は使っていない。使い方は`Docs/Visual-Craft-2026-09-29.md`。

## 2026-10-01 日記の手書き風字体 Klee One

- 正規配布元：Google Fonts `https://github.com/google/fonts/tree/main/ofl/kleeone`。`KleeOne-SemiBold.ttf`（600）と同フォルダのOFL.txtを取得。Copyright 2020 The Klee Project Authors。SIL Open Font License 1.1。
- 取得した字体のSHA256：`B031EC426C23CA1143EF1F7D58BEE7A79EFE119ED654152F121C922202B303FD`。ライセンス原文は`Assets/Fonts/CompanyYear/KleeOne-OFL.txt`。Windows版の`FontLicenses/`へ同梱する。
- TMP用は`Resources/KleeOneDiary`。44pt・余白5・1024の動的アトラス、スケール1、ビルド時に作業字形を消去。TestModeでは字体とフォールバックを専用コピーにし、元の作業用データを変えない。
- ノート・机・テープ・リング・天気・シールは承認HTMLのCSSをUnity UIで描画したもの。第三者画像や新しい生成画像は使っていない。

## 2026-10-01 秘書さん・エンジニアさんの声の試し（Irodori-TTS）

- 道具：Irodori-TTS（Aratako氏、GitHub `Aratako/Irodori-TTS`、コードはMIT License）。このPCの中だけで動かす（`C:\Users\issak\Tools\Irodori-TTS`、Python 3.11、PyTorch 2.10 CUDA 12.8、RTX 4060 Ti）。アカウント登録なし。
- モデル：`Aratako/Irodori-TTS-v4.1-Small`（重みもMIT。商用可、ライセンス表記を同梱）と、話題の大型`Aratako/Irodori-TTS-v4-Large-Quantized`の`int8-weight-only`（**Googleの「Gemmaの利用規約」と禁止用途の決まりが適用**。商用は可だが、配布物に規約の写しと注意書きが要る）。採用する前に、その時点のモデルカードの条件を確認する。
- 声の作り方：文字の説明（キャプション）だけで作り、参照の音声は使わない。実在の人（声優・著名人）の声を手本にしない（モデルの利用条件。偶然似る可能性はモデルカードにも注意書きあり）。
- 試聴の生成：`Tools/Irodori-Audition.py`（候補4つ×台詞3行＋大型モデルで1行目）。出力`Artifacts/VoiceAudition/`（gitで追跡しない）。採用が決まったら、台本・キャプション・種・モデル名をここに記録してから本番の音声を作る。

## 2026-10-02 年度開幕の仮表示

- 承認済み `Docs/Mockups/year-opening.html` のCSS・SVGをuGUIの図形描画へ移したもの。新しい第三者画像・生成画像は使わない。既存オフィスの色違いにはUI用の色相・彩度シェーダーを使用し、2・3年目の撮影で動作を確認する。
- 仲間の絵とタイトルKVは既存の承認素材。強敵の影はモックの仮図形。仲間のLv・相談文化は実状態を表示する。

## 2026-10-02 かのん・エンジニアの本編用の声（Irodori-TTS、配布可）

- 24本（各12）：`Assets/Audio/CompanyYear/Voice/Kanon/`・`Engineer/`、台本`Docs/Voice/kanon-engineer-script.csv`、作成`Tools/Irodori-CharacterLines.py`（種20261002）。
- かのん：`Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only`（**Gemmaの利用規約**。配布物に規約の写し・注意書きを同梱し、禁止用途の決まりを守る）。見本`ArtSource/Voice/Irodori-refs/secretary-ref.wav`。
- エンジニア：`Aratako/Irodori-TTS-v4.1-Small`（**MIT**。ライセンス表記を同梱）。見本`ArtSource/Voice/Irodori-refs/engineer-ref.wav`、話す速さ0.8倍。
- 見本はどちらもこの道具で文字の説明だけから作った声（実在の人の声は使っていない）。Irodoriは聞こえない透かし（SilentCipher）を入れる。
- 10/3：`kanon_opening`を「数字のことは、わたしに任せて。あなたたちは、守ることに集中して。」に作り直し（情報の点検FC15-02。同じid・同じファイル名、同じ声・説明・見本。種20261002は「数字」の高低が不自然だったので、加藤さんの試聴で自然だった2つのうち、加藤さんが選んだ種20261004（新3）を採用）。
- ElevenLabsの無料枠の音声（ひなた）と違い、gitで追跡してよく、配布できる（上の条件を守る）。

## 2026-10-02 承認済みの二人絵（Next-14）

- 加藤さん承認の3枚：`hinata-kanon-1.png`、`hinata-engineer-3.png`、`kanon-engineer-2.png`。原画と制作経緯は `ArtSource/Characters/pairs-20261002/README.md`。加藤さんのChatGPTで生成したオリジナルのキャラ絵。新たな画像生成や第三者の絵の流用はしていない。
- `Assets/Sprites/Pairs/` に原画をそのまま複製し、Unity標準Spriteとして取り込む。最大2048、非圧縮、MipMapなし、FullRect。横長の写真窓は原画全体の比率を保ち、顔を切り抜かない。
- 日記1年目6月/10月と3年目5月の会議の指定掛け合いだけに使用。キャプションは既存Klee One。写真の白い余白・影は既存UI部品。
- 公開時はストアのAI生成内容の申告対象。サービスの利用条件の確認はストア素材の既存方針に従う。音声を含まない画面写真で、ElevenLabs無料音声の公開禁止を回避しているだけで、動画用の公開音声へ変更したものではない。
