$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$opsMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompanyOps' | Out-Null
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs -out:Artifacts/CompanyOps/Checks.exe `
    Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs Tools/CompanyOpsChecks.cs
if ($LASTEXITCODE -ne 0) { throw '試作ルールのコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/Checks.exe
if ($LASTEXITCODE -ne 0) { throw '年間検証に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs -out:Artifacts/CompanyOps/Playtest.exe `
    Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs Tools/CompanyOpsPlaytest.cs
if ($LASTEXITCODE -ne 0) { throw '仮想試遊のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/Playtest.exe
if ($LASTEXITCODE -ne 0) { throw '仮想試遊に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs -out:Artifacts/CompanyOps/GrowthChecks.exe Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs Tools/CompanyOpsGrowthChecks.cs
if ($LASTEXITCODE -ne 0) { throw '育成検証のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/GrowthChecks.exe
if ($LASTEXITCODE -ne 0) { throw '育成と年間負荷の検証に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs -out:Artifacts/CompanyOps/EventChecks.exe Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs Tools/CompanyOpsEventChecks.cs
if ($LASTEXITCODE -ne 0) { throw 'ランダム年度のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/EventChecks.exe
if ($LASTEXITCODE -ne 0) { throw 'ランダム年度の検証に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs -out:Artifacts/CompanyOps/RewardChecks.exe Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs Tools/CompanyOpsRewardChecks.cs
if ($LASTEXITCODE -ne 0) { throw '臨時予算の比較のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/RewardChecks.exe
if ($LASTEXITCODE -ne 0) { throw '臨時予算の比較に失敗' }
& "$opsMono/bin/mono.exe" "$opsMono/lib/mono/4.5/csc.exe" -nologo Assets/Scripts/CompanyOps/OpsMinigame.cs Assets/Scripts/CompanyOps/OpsContainmentMinigame.cs Assets/Scripts/CompanyOps/OpsMailMinigame.cs Assets/Scripts/CompanyOps/OpsMfaMinigame.cs -out:Artifacts/CompanyOps/RankChecks.exe Assets/Scripts/CompanyOps/OpsCatalog.cs Assets/Scripts/CompanyOps/OpsEventCatalog.cs Assets/Scripts/CompanyOps/OpsState.cs Assets/Scripts/CompanyOps/OpsState.Events.cs Assets/Scripts/CompanyOps/OpsState.Growth.cs Assets/Scripts/CompanyOps/OpsState.Peaks.cs Tools/CompanyOpsRankChecks.cs
if ($LASTEXITCODE -ne 0) { throw 'ランクの恩恵のコンパイルに失敗' }
& "$opsMono/bin/mono.exe" Artifacts/CompanyOps/RankChecks.exe
if ($LASTEXITCODE -ne 0) { throw 'ランクの恩恵の検証に失敗' }
