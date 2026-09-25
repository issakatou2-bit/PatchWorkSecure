$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$opsMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo -out:Artifacts/CompanyOps/Checks.exe `
    Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Tools/CompanyOpsChecks.cs
if ($LASTEXITCODE -ne 0) { throw '試作ルールのコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/Checks.exe
if ($LASTEXITCODE -ne 0) { throw '年間検証に失敗' }
