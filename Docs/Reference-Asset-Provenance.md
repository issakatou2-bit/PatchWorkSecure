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
