param(
    [ValidateSet('Normal','Capture','Long','RegularCapture','All','Inspect')][string]$Group='RegularCapture',
    [string]$Output,
    [string[]]$Only,
    [string]$UnityCli='C:/Program Files/Unity Hub/resources/cli/unity.exe'
)
$ErrorActionPreference='Stop'
$projectDirectory=Split-Path -Parent $PSScriptRoot
Set-Location $projectDirectory
if($PSBoundParameters.ContainsKey('Only') -and (-not $Only -or @($Only|Where-Object{[string]::IsNullOrWhiteSpace($_)}).Count -gt 0)){throw '再実行する完全名を1件以上指定してください。空の指定で全件は流しません'}
if(-not $Output){$Output='Artifacts/TestGroups/'+$Group+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json'}
$outputPath=[IO.Path]::GetFullPath((Join-Path $projectDirectory $Output))
if(-not $outputPath.StartsWith($projectDirectory+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw '出力先はプロジェクト内にしてください'}
if(Test-Path -LiteralPath $outputPath){throw '既存の実行記録は上書きしません'}
function Literal([string]$value){return '"'+$value.Replace('\','\\').Replace('"','\"').Replace("`r",'\r').Replace("`n",'\n')+'"'}
$type='PatchWorkSecure.EditorTools.CompanyOpsTestGroups'
if($Group -eq 'Inspect'){$code=$type+'.Inspect('+(Literal $Output)+'); return "一覧の取得開始";'}
else{
    $names=if($Only){'new string[]{'+(($Only|ForEach-Object{Literal $_})-join ',')+'}'}else{'null'}
    $code=$type+'.Start('+(Literal $Group)+','+(Literal $Output)+','+$names+'); return "実行の開始要求";'
}
$response=& $UnityCli command eval --project-path $projectDirectory --code $code --format json
if(-not ($response|ConvertFrom-Json).success){throw "開始に失敗しました。通信断の場合は記録とEditorの状態を確認してから判断してください: $response"}
$deadline=(Get-Date).AddHours(2)
$lastCount=-1
do{
    Start-Sleep -Seconds 5
    if((Get-Date) -gt $deadline){throw '実行待ちの期限です。Editorを自動中断しません。実行記録を確認してください'}
    if(-not (Test-Path -LiteralPath $outputPath)){continue}
    try{$record=Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8|ConvertFrom-Json}catch{continue}
    if($Group -eq 'Inspect'){
        $record|Select-Object total,normal,capture,longer,explicitCount|ConvertTo-Json -Compress
        if($record.total -eq 0){throw '対象0件は成功扱いにしません'}
        exit 0
    }
    if($record.tests.Count -ne $lastCount){$lastCount=$record.tests.Count;Write-Output ($Group+': '+$lastCount+'/'+$record.expected)}
}while($record.status -in @('starting','running') -or -not $record)
$record|Select-Object group,status,expected,passed,failed,skipped,inconclusive,seconds,wallSeconds|ConvertTo-Json -Compress
$record.tests|Where-Object status -ne 'Passed'|Select-Object name,status,message|ConvertTo-Json -Depth 3
if($record.status -ne 'passed'){throw '未完了・失敗・中断は成功回数に数えません。失敗分と関係分だけを直して再実行してください'}
