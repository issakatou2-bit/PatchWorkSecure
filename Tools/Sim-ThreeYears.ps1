# 3年の本編の試算（Unity不要）。引数：2年目の脅威 3年目の脅威 設備の経年（0=そのまま,1=Lv2→Lv1,2=全部1段下げる）
param([int]$P2 = 6, [int]$P3 = 12, [int]$Decay = 1)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$mono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
$sources = @('OpsMinigame','OpsContainmentMinigame','OpsMailMinigame','OpsMfaMinigame','OpsLogMinigame','OpsBlockMinigame','OpsRestoreMinigame',
    'OpsCatalog','OpsEventCatalog','OpsState','OpsState.Events','OpsState.Growth','OpsState.Peaks') | ForEach-Object { "Assets/Scripts/CompanyOps/$_.cs" }
if (-not (Test-Path Artifacts/CompanyOps/ThreeYears.exe) -or ((Get-Item Tools/CompanyOpsThreeYears.cs).LastWriteTime -gt (Get-Item Artifacts/CompanyOps/ThreeYears.exe).LastWriteTime)) {
    & "$mono/bin/mono.exe" "$mono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/ThreeYears.exe @sources Assets/Tests/CompanyOpsPersonaPolicy.cs Tools/CompanyOpsThreeYears.cs
    if ($LASTEXITCODE -ne 0) { throw '3年試算のコンパイルに失敗' }
}
& "$mono/bin/mono.exe" Artifacts/CompanyOps/ThreeYears.exe $P2 $P3 $Decay
