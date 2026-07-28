$ErrorActionPreference='Stop';$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$path=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs';$content=[IO.File]::ReadAllText($path)
$content=$content.Replace('data.Version < 10 ? "Save migrado para v10" : "Save carregado"','data.Version < 11 ? "Save migrado para v11" : "Save carregado"')
$payload=@{filePath='Assets/_Project/Scripts/Farming/FarmTestPlot.cs';content=$content;requestId='save-status-v11'}|ConvertTo-Json -Compress
$result=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw 'Falha status v11'};$result|Select-Object -Last 12
