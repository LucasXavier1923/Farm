$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$path=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmDailyOrders.cs'
$content=[IO.File]::ReadAllText($path).Replace('get; init;','get; set;')
$payload=@{filePath='Assets/_Project/Scripts/Farming/FarmDailyOrders.cs';content=$content;requestId='daily-order-setters-v11'}|ConvertTo-Json -Compress
$result=$payload|& $cli run-tool script-update-or-create $project --input-file -
if($LASTEXITCODE-ne 0){throw 'Falha ao corrigir setters.'};$result|Select-Object -Last 12
