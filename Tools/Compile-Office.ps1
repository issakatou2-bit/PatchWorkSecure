$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path -Parent $PSScriptRoot
Set-Location $projectDirectory
$unityDataDirectory = 'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data'
$compiler = "$unityDataDirectory/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll"
$runtime = "$unityDataDirectory/DotNetSdk/dotnet.exe"
$outputDirectory = 'Artifacts/CompileCheck'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null

foreach ($assembly in @('PatchWorkSecure', 'PatchWorkSecure.Editor', 'PatchWorkSecure.Tests')) {
    $response = "Library/Bee/artifacts/1900b0aE.dag/$assembly.rsp"
    if (!(Test-Path $response)) { throw "Unityの既存コンパイル設定がありません: $response" }
    $requiredReferences = '(netstandard|mscorlib|System|System.Core|Unity.Scripting|UnityEngine.CoreModule|UnityEngine.UI|Unity.TextMeshPro|UnityEngine.UIModule|UnityEngine.AudioModule|UnityEngine.IMGUIModule|UnityEditor.CoreModule|UnityEditor.UI|Unity.InputSystem|Unity.InputSystem.TestFramework|UnityEngine.InputLegacyModule|UnityEngine.ImageConversionModule|UnityEngine.TextRenderingModule|UnityEditor.TextCoreFontEngineModule|UnityEditor.TestRunner|UnityEngine.TestRunner|nunit.framework|UnityEngine.UnityWebRequestModule|UnityEngine.ScreenCaptureModule|UnityEngine.AnimationModule|UnityEngine.PhysicsModule|UnityEngine.Physics2DModule|UnityEngine.SharedInternalsModule|UnityEngine.TextCoreFontEngineModule|PatchWorkSecure.ref)\.dll'
    $arguments = Get-Content $response | Where-Object {
        $_ -notmatch '^-(out|refout|analyzer|define):' -and $_ -notmatch '^"Assets/' -and
        ($_ -notmatch '^-r:' -or ($_ -replace '\.ref\.dll', '.dll') -match $requiredReferences -or $_ -match 'PatchWorkSecure.ref.dll|UnityEngine.JSONSerializeModule')
    } | ForEach-Object {
        if ($_ -match '^-r:.*PatchWorkSecure\.ref\.dll') {
            '-r:Artifacts/CompileCheck/PatchWorkSecure.dll'
        } else { $_ }
    }
    $sourceDirectory = switch ($assembly) {
        'PatchWorkSecure' { 'Assets/Scripts' }
        'PatchWorkSecure.Editor' { 'Assets/Editor' }
        'PatchWorkSecure.Tests' { 'Assets/Tests' }
    }
    $sources = Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName }
    & $runtime $compiler @arguments '-define:UNITY_EDITOR;UNITY_INCLUDE_TESTS;ENABLE_INPUT_SYSTEM' "-out:$outputDirectory/$assembly.dll" @sources
    if ($LASTEXITCODE -ne 0) { throw "$assembly のコンパイルが失敗しました" }
    Write-Output "$assembly : コンパイル成功（Unity再生検証とは別）"
}
