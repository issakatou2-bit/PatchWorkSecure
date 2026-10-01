# 3年の本編の試算（Unity不要）。引数：2年目・3年目の脅威、設備の経年（0=そのまま,1=Lv2→Lv1,2=全部1段下げる）、予算の繰越（÷Div、最大Max）
param([int]$P2 = 12, [int]$P3 = 24, [int]$Decay = 1, [int]$Div = 1, [int]$Max = 999, [int]$Milestones = 0)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$mono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
$sources = @('OpsMinigame','OpsContainmentMinigame','OpsMailMinigame','OpsMfaMinigame','OpsLogMinigame','OpsBlockMinigame','OpsRestoreMinigame',
    'OpsCatalog','OpsEventCatalog','OpsState','OpsState.Events','OpsState.Growth','OpsState.Peaks','OpsStory','OpsDiaryCatalog') | ForEach-Object { "Assets/Scripts/CompanyOps/$_.cs" }
# ルール側の更新もキャッシュ失効の対象にする。
if (-not (Test-Path Artifacts/CompanyOps/ThreeYears.exe) -or (@($sources + @('Assets/Tests/CompanyOpsPersonaPolicy.cs','Tools/CompanyOpsThreeYears.cs') | Where-Object { (Get-Item $_).LastWriteTime -gt (Get-Item Artifacts/CompanyOps/ThreeYears.exe).LastWriteTime }).Count -gt 0)) {
    & "$mono/bin/mono.exe" "$mono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/ThreeYears.exe @sources Assets/Tests/CompanyOpsPersonaPolicy.cs Tools/CompanyOpsThreeYears.cs
    if ($LASTEXITCODE -ne 0) { throw '3年試算のコンパイルに失敗' }
}
& "$mono/bin/mono.exe" Artifacts/CompanyOps/ThreeYears.exe $P2 $P3 $Decay $Div $Max $Milestones
