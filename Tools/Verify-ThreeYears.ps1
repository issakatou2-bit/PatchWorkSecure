$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$storyMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/Next8' | Out-Null
# 同じ方針・種で、試算の年度生成を本実装へ置換。全挑戦で引き継ぎと目標終了も照合する。
& ./Tools/Sim-ThreeYears.ps1 | Tee-Object -FilePath Artifacts/Next8/three-years-reference.txt
if ($LASTEXITCODE -ne 0) { throw '承認済みの試算が失敗' }
& "$storyMono/bin/mono.exe" Artifacts/CompanyOps/ThreeYears.exe 12 24 1 1 999 production | Tee-Object -FilePath Artifacts/Next8/three-years-production.txt
if ($LASTEXITCODE -ne 0) { throw '本実装の3年検証に失敗。数値を変更せず理由を報告すること' }
$storyReference = @(Get-Content Artifacts/Next8/three-years-reference.txt | Where-Object { $_ -match '脅威|年目のランク|^目標' } | ForEach-Object { $_.Trim([char]0xfeff) })
$storyProduction = @(Get-Content Artifacts/Next8/three-years-production.txt | Where-Object { $_ -match '脅威|年目のランク|^目標' } | ForEach-Object { $_.Trim([char]0xfeff) })
if ($storyReference.Count -ne 10 -or $storyProduction.Count -ne 10 -or (Compare-Object $storyReference $storyProduction)) { throw '本実装と試算の年度ランク・達成率が違う' }
Write-Output '3年の本実装と試算が一致。初回51%、4回目まで73%。'
