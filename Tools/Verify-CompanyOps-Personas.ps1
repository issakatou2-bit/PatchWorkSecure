$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$personaMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
& "$personaMono/bin/mono.exe" "$personaMono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/PersonaChecks.exe `
    Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs `
    Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs `
    Assets/Tests/CompanyOpsPersonaPolicy.cs Tools/CompanyOpsPersonaChecks.cs
if ($LASTEXITCODE -ne 0) { throw '仮想方針のコンパイルに失敗' }
& "$personaMono/bin/mono.exe" Artifacts/CompanyOps/PersonaChecks.exe
if ($LASTEXITCODE -ne 0) { throw '仮想方針450年度の検証に失敗' }
