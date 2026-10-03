# 大型の完全版の試し（Tools/Irodori-FullTest.py --model full）を見張り、PCが重くなる前に止める（10/3、加藤さん「PCが落ちるのが一番嫌」）。
# 止める条件：メモリの空きが4GB未満／仮想メモリ（ページファイル込み）の空きが3GB未満／25分を超えた。止めた理由はログに残す。
# 使い方：powershell -NoProfile -File Tools/Watch-FullTest.ps1 -Log <ログの置き場所>
param([string]$Log = "$env:TEMP\watch-fulltest.log")
$start = $null
while ($true) {
    $p = Get-CimInstance Win32_Process -Filter "name='python.exe'" | Where-Object { $_.CommandLine -match 'Irodori-FullTest\.py' -and $_.CommandLine -match '--model full' }
    if ($p) {
        if (-not $start) { $start = Get-Date; Add-Content $Log "開始 $(Get-Date -Format HH:mm:ss) pid=$($p.ProcessId)" }
        $o = Get-CimInstance Win32_OperatingSystem
        $free = $o.FreePhysicalMemory / 1MB; $virt = $o.FreeVirtualMemory / 1MB; $mins = ((Get-Date) - $start).TotalMinutes
        $reason = if ($free -lt 4) { "メモリの空き $([math]::Round($free,1))GB" } elseif ($virt -lt 3) { "仮想メモリの空き $([math]::Round($virt,1))GB" } elseif ($mins -gt 25) { "25分を超えた" } else { $null }
        if ($reason) {
            Stop-Process -Id $p.ProcessId -Force
            Add-Content $Log "停止 $(Get-Date -Format HH:mm:ss) 理由：$reason"
            break
        }
    } elseif ($start) {
        Add-Content $Log "終了 $(Get-Date -Format HH:mm:ss)（見張り中に止める必要なし）"
        break
    }
    Start-Sleep -Seconds 2
}
