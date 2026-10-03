#!/bin/sh
# GPUを重く使う前に呼ぶ（Claudeの声の生成など）。Codexのテスト（Unityのバッチの -runTests）が動いている間は待つ。
# テストのほうを優先する：取り合うと、テストが時間切れで失敗することがあるため（10/3）。
# 使い方: sh Tools/Wait-GpuFree.sh && <重い処理>
while powershell -NoProfile -Command "if (Get-CimInstance Win32_Process -Filter \"name='Unity.exe'\" | Where-Object { \$_.CommandLine -match '-runTests' }) { exit 0 } else { exit 1 }"; do
  sleep 30
done
