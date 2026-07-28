$ErrorActionPreference='Stop';$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd';$path=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmJournal.cs'
$content=[IO.File]::ReadAllText($path);$old='new("crop_variety", "DESCOBERTA", "Tr\u00EAs sabores", "Colha ab\u00F3bora, cenoura e milho.", FarmJournalMetric.UniqueCrops, 3, 100)';$new='new("crop_variety", "DESCOBERTA", "Quatro esta\u00E7\u00F5es", "Colha morango, milho, ab\u00F3bora e cenoura.", FarmJournalMetric.UniqueCrops, 4, 125)'
if(-not $content.Contains($old)){throw 'Objetivo de tres culturas nao encontrado'};$content=$content.Replace($old,$new)
$payload=@{filePath='Assets/_Project/Scripts/Farming/FarmJournal.cs';content=$content;requestId='journal-four-crops'}|ConvertTo-Json -Compress
$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw 'Falha ao atualizar diario'}
