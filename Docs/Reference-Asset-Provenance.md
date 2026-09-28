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

## 2026-09-29 ひなたの新デザイン（生成・未投入）

- 生成：ChatGPT（加藤さんのログイン済みアカウント、アプリ内ブラウザからClaude Codeが操作）。指示文の原型は`Docs/Hinata-Sheet-Prompts.md`。
- 経緯：8頭身の設定画 → 2〜5頭身の比較 → デフォルメ強め（色付き・線画）→ **加藤さんが3頭身の線画を選択** → 色鉛筆塗り → アニメ塗り → **太い輪郭線のアニメ塗りに決定**。表情18種・ポーズ18種を3×2のシートで生成。
- 原画：`ArtSource/Hinata/gen-20260929/hinata-*.png`（ChatGPTの生成物をそのまま保存）。切り抜き：同`cut/`（`Tools/Cut-CharacterSheet.py`で外周の白だけを透過）。一覧：同`contact-sheet.png`。
- 指の本数は「5本」と指定したが、原寸での全数確認は未実施。**ゲームへの投入・採用は加藤さんの確認後**。販売物への利用可否は生成サービスの規約を確認すること。
