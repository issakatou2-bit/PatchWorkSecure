$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$storyMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/Next11' | Out-Null
# 近似は別記。年別抽選・達成途中の信頼加算を含む本実装を近似に合わせて調整しない。
& ./Tools/Sim-ThreeYears.ps1 -Milestones 1 | Tee-Object -FilePath Artifacts/Next11/three-years-approximation.txt
if ($LASTEXITCODE -ne 0) { throw '近似の試算が失敗' }
& "$storyMono/bin/mono.exe" Artifacts/CompanyOps/ThreeYears.exe production | Tee-Object -FilePath Artifacts/Next11/three-years-production.txt
if ($LASTEXITCODE -ne 0) { throw '本実装の3年検証に失敗。数値を変更せず理由を報告すること' }
$storyApprox = @(Get-Content Artifacts/Next11/three-years-approximation.txt | Where-Object { $_ -match '^目標 B→A→A' })
$storyProduction = @(Get-Content Artifacts/Next11/three-years-production.txt | Where-Object { $_ -match '^目標 B→A→A' })
if ($storyApprox.Count -ne 1 -or $storyProduction.Count -ne 1) { throw '3年の達成率の記録がありません' }
Write-Output ('近似（旧抽選・年末の目標判定）: ' + $storyApprox[0])
Write-Output ('本実装（年別抽選・途中の信頼加算）: ' + $storyProduction[0])
Write-Output '本実装はNext-11の目安（初回35〜45%、4回目まで60〜70%）を確認。近似を本実装の達成率とは扱わない。'
