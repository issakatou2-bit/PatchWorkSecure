param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Rest)
# Generate Hinata voice lines from the script CSV (see Tools/Generate-HinataVoice.py).
$tool = Join-Path $PSScriptRoot 'Generate-HinataVoice.py'
& py -3 $tool @Rest
