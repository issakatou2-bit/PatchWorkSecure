#!/bin/sh
# 声の生成を1本の順番待ちで流す（10/3）。待ち合わせの背景の処理を何本も立てると、2時間で止まって続きが流れなくなるため。
# 使い方: sh Tools/Voice-Queue.sh <ログの置き場所> <手順1> <手順2> ...
#   手順の書き方: "wait:<ファイル>"（そのファイルができるまで待つ）／"full"（完全版の試し。見張りつき・テスト中は待つ）／それ以外はそのままシェルで実行
P=/c/Users/issak/Tools/Irodori-TTS/.venv/Scripts/python.exe
LOG="$1"; shift
export PYTHONIOENCODING=utf-8
for step in "$@"; do
  echo "[$(date +%H:%M:%S)] 開始 $step" >> "$LOG"
  case "$step" in
    wait:*) f="${step#wait:}"; until [ -f "$f" ]; do sleep 15; done ;;
    full)
      sh Tools/Wait-GpuFree.sh
      powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Watch-FullTest.ps1 -Log "$LOG" &
      $P Tools/Irodori-FullTest.py --model full >> "$LOG.full" 2>&1
      wait ;;
    *) sh -c "$step" >> "$LOG.out" 2>&1 ;;
  esac
  echo "[$(date +%H:%M:%S)] 終了 $step（$?）" >> "$LOG"
done
