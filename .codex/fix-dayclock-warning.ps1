$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm'
$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$path=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmDayClock.cs'
$content=[IO.File]::ReadAllText($path)
$old='var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);'
$new='var lights = FindObjectsByType<Light>();'
if(-not $content.Contains($old)){throw 'API obsoleta nao encontrada.'}
$content=$content.Replace($old,$new)
$payload=@{filePath='Assets/_Project/Scripts/Farming/FarmDayClock.cs';content=$content;requestId='dayclock-unity6-warning-fix'}|ConvertTo-Json -Compress
$result=$payload|& $cli run-tool script-update-or-create $project --input-file -
if($LASTEXITCODE-ne 0){throw 'Falha ao enviar correcao.'}
$result|Select-Object -Last 12
