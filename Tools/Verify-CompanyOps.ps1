$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$opsMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/Checks.exe `
    Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Tools/CompanyOpsChecks.cs
if ($LASTEXITCODE -ne 0) { throw '試作ルールのコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/Checks.exe
if ($LASTEXITCODE -ne 0) { throw '年間検証に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/Playtest.exe `
    Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Tools/CompanyOpsPlaytest.cs
if ($LASTEXITCODE -ne 0) { throw '仮想試遊のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/Playtest.exe
if ($LASTEXITCODE -ne 0) { throw '仮想試遊に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/GrowthChecks.exe Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Tools/CompanyOpsGrowthChecks.cs
if ($LASTEXITCODE -ne 0) { throw '育成検証のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/GrowthChecks.exe
if ($LASTEXITCODE -ne 0) { throw '育成と年間負荷の検証に失敗' }
