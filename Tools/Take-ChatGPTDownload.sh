#!/bin/sh
# ChatGPTで作った画像を、ダウンロードのフォルダから保存先へ移す（10/2から使っている手順をまとめたもの）。
# 手順：①touch "$CLAUDE_SCRATCH/dl-marker"（目印） ②内蔵ブラウザで画像を開いて右上の保存ボタン ③sh Tools/Take-ChatGPTDownload.sh <保存先.png>
# 目印より新しい .tmp（ChatGPTからのダウンロード）だけを拾う。見つからなければ何もしない。
# CLAUDE_SCRATCH：目印を置く作業用の場所（Claudeのスクラッチパッド）。無ければ TEMP を使う。
S="${CLAUDE_SCRATCH:-$TEMP}"
for i in 1 2 3 4 5 6 7 8; do
  sleep 2
  f=$(find /c/Users/issak/Downloads -maxdepth 1 -name '*.tmp' -newer "$S/dl-marker" | head -1)
  if [ -n "$f" ]; then
    s1=$(stat -c %s "$f"); sleep 1; s2=$(stat -c %s "$f")
    if [ "$s1" = "$s2" ] && [ "$s1" -gt 100000 ]; then
      cp "$f" "$1" && rm "$f" && echo "saved $1 ($s1 bytes)"
      exit 0
    fi
  fi
done
echo "not found"
