param([int]$Base = -1, [int]$Linear = -1, [int]$Quadratic = -1, [int]$Income = -1, [int]$Factors = 1, [int]$Cohorts = 100, [int]$Margin = 8, [string]$Id = 'final', [switch]$CheckTargets)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$endlessMono = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
$simulationDirectory = [IO.Path]::GetFullPath("Artifacts/Next12/Endless/$Id")
if ($Id -notmatch '^[a-zA-Z0-9_-]+$') { throw '試算の出力名が不正です' }
New-Item -ItemType Directory -Force $simulationDirectory | Out-Null
$simulationSources = @('OpsMinigame','OpsContainmentMinigame','OpsMailMinigame','OpsMfaMinigame','OpsLogMinigame','OpsBlockMinigame','OpsRestoreMinigame',
    'OpsCatalog','OpsYearEquipment','OpsYearAllies','OpsYearContent','OpsYearBosses','OpsEventCatalog','OpsState','OpsState.Events','OpsState.Growth','OpsState.Peaks','OpsStory','OpsEndless','OpsRecords','OpsDiaryCatalog') | ForEach-Object { "Assets/Scripts/CompanyOps/$_.cs" }
if ($Base -ge 0 -or $Linear -ge 0 -or $Quadratic -ge 0 -or $Income -ge 0) {
    # 数値の候補は成果物用の複製だけを機械的に置換する。ゲーム・保存・本編の数値は変えない。
    $candidate = [IO.File]::ReadAllText([IO.Path]::GetFullPath('Assets/Scripts/CompanyOps/OpsEndless.cs'))
    foreach ($field in @(@('EndlessPressureBase',$Base),@('EndlessPressureLinear',$Linear),@('EndlessPressureQuadratic',$Quadratic),@('EndlessMonthlyIncomePerYear',$Income))) {
        if ($field[1] -ge 0) { $candidate = [regex]::Replace($candidate, ($field[0] + '=\d+'), ($field[0] + '=' + $field[1])) }
    }
    $candidatePath = Join-Path $simulationDirectory 'OpsEndless-candidate.cs'
    [IO.File]::WriteAllText($candidatePath,$candidate,[Text.UTF8Encoding]::new($true))
    $simulationSources = $simulationSources | ForEach-Object { if ($_ -eq 'Assets/Scripts/CompanyOps/OpsEndless.cs') { $candidatePath } else { $_ } }
}
$simulationExe = Join-Path $simulationDirectory 'EndlessYears.exe'
& "$endlessMono/bin/mono.exe" "$endlessMono/lib/mono/4.5/csc.exe" -nologo "-out:$simulationExe" @simulationSources Assets/Tests/CompanyOpsPersonaPolicy.cs Tools/CompanyOpsThreeYears.cs
if ($LASTEXITCODE -ne 0) { throw '終わりなき年度の試算コンパイルに失敗' }
$mode = if ($CheckTargets) { 'check' } else { 'report' }
& "$endlessMono/bin/mono.exe" $simulationExe endless $simulationDirectory $Factors $mode $Cohorts $Margin | Tee-Object -FilePath (Join-Path $simulationDirectory 'report.txt')
if ($LASTEXITCODE -ne 0) { throw '終わりなき年度の試算が目安の外、またはルールの整合に失敗' }
