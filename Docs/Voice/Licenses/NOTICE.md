# かのん・エンジニアの合成音声

確認日：2026-10-02。ゲームには生成済みのWAVだけを同梱し、音声生成モデル・推論プログラムは同梱しない。プレイ中の通信・音声生成は行わない。

- かのん：Aratako / Irodori-TTS-v4-Large-Quantized、int8-weight-only。
- エンジニア：Aratako / Irodori-TTS-v4.1-Small。
- 台本：Docs/Voice/kanon-engineer-script.csv、制作記録：Docs/Reference-Asset-Provenance.md。
- 参照音声は文字の説明から作った架空の声。実在する人の音声を参照していない。人物のなりすましや偽情報を目的とした使用を行わない。偶然の声の類似はあり得る。
- 音声には生成時のSilentCipher透かしを維持する。

コードとSmallモデルのMIT表記は `Irodori-MIT.txt`。各モデルの公式カードも保存した。Largeモデルの条件として案内されているGoogleの規約・禁止用途の文書は、公式ページの本文の写しを `Gemma-Terms.html`・`Gemma-Prohibited-Use.html` に保存した。ページのナビゲーションは取り除いた。両HTMLはGoogle Developers、CC BY 4.0の出典表記を含む。

規約はモデル・モデル派生物と出力を区別している。このゲームへの規約資料の同梱はプロジェクトの出典管理方針によるもので、生成音声をモデル本体と同一扱いするという意味ではない。配布条件の最終確認時は公式の最新版も確認する。

- [Largeモデルカード](https://huggingface.co/Aratako/Irodori-TTS-v4-Large-Quantized)
- [Smallモデルカード](https://huggingface.co/Aratako/Irodori-TTS-v4.1-Small)
- [Irodori-TTS](https://github.com/Aratako/Irodori-TTS)
- [Gemma Terms of Use](https://ai.google.dev/gemma/terms)
- [Gemma Prohibited Use Policy](https://ai.google.dev/gemma/prohibited_use_policy)

注意：この記録は仲間24本についてのもの。ひなたのElevenLabs無料枠の私的試遊音声を含む現在のWindows版は、引き続き配布・公開・動画投稿をしない。
