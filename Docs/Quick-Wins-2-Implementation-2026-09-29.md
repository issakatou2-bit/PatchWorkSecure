# 手応え追加の実装記録

`Mockups/Quick-Wins-2.md`の0→8を順に実装。ルール・報酬・難易度のファイルは変更しない。
各項目は専用PlayModeテスト・コンパイル・Verify系を確認してから個別コミットし、最後にPlayMode全件も実行する。
画面の検証は自動撮影と目視によるもの。人間の試遊評価とは区別する。

## 0 事件の説明札

- 説明札を上へ100px移動し、症状を2行まで表示。ひなたの位置は維持。
- 全40事件を通常・省演出の両方で確認するテストを追加。
- 撮影：`Artifacts/CompanyOps/110-quickwins0-incident.png`、`110-quickwins0-reduced.png`。
- 専用PlayMode 1/1成功（80条件）。3アセンブリのコンパイル・Verify4本成功。
