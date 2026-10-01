$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$depthMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
& "$depthMono/bin/mono.exe" "$depthMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs Assets/Scripts/CompanyOps/OpsLogMinigame.cs Assets/Scripts/CompanyOps/OpsBlockMinigame.cs Assets/Scripts/CompanyOps/OpsRestoreMinigame.cs -out:Artifacts/CompanyOps/DepthChecks.exe `
Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsYearEquipment.cs Assets/Scripts/CompanyOps/OpsYearAllies.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs `
    Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs `
    Assets/Tests/CompanyOpsPersonaPolicy.cs Tools/CompanyOpsDepthChecks.cs
if ($LASTEXITCODE -ne 0) { throw '複数深度方針のコンパイルに失敗' }
& "$depthMono/bin/mono.exe" Artifacts/CompanyOps/DepthChecks.exe
if ($LASTEXITCODE -ne 0) { throw '複数深度900年度の検証に失敗' }
