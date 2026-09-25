$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$unityMonoDirectory = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/MonoBleedingEdge'
New-Item -ItemType Directory -Force 'Artifacts/CompileCheck' | Out-Null
& "$unityMonoDirectory/bin/mono.exe" "$unityMonoDirectory/lib/mono/4.5/csc.exe" -nologo `
    -out:Artifacts/CompileCheck/CoreChecks.exe Assets/Scripts/GameData.cs Assets/Scripts/GameState.cs `
    Assets/Scripts/GamePresentation.cs Tools/CoreChecks.cs
if ($LASTEXITCODE -ne 0) { throw 'コア検証のコンパイルに失敗しました' }
& "$unityMonoDirectory/bin/mono.exe" Artifacts/CompileCheck/CoreChecks.exe
if ($LASTEXITCODE -ne 0) { throw 'コア検証に失敗しました' }
