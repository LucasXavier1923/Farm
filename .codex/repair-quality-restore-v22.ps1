$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$asset = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$read = Join-Path $project 'Temp\read-quality-restore-repair-v22.json'
$submit = Join-Path $project 'Temp\submit-quality-restore-repair-v22.json'
[System.IO.File]::WriteAllText($read, (@{ filePath = $asset; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$lines = @(& $cli run-tool script-read $project --input-file $read 2>&1)
if ($LASTEXITCODE -ne 0) { throw 'Falha lendo FarmGameState' }
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '{') { $start = $i; break } }
if ($start -lt 0) { throw 'JSON ausente' }
$content = [string]((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result)
$oldInventory = '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddInternal(stack.ItemId, stack.Quantity);'
$newInventory = '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddInternal(stack.ItemId, stack.Quantity, data.Version >= 18 ? stack.Quality : FarmItemQuality.Normal);'
$oldStorage = '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddToStorageInternal(stack.ItemId, stack.Quantity);'
$newStorage = '                    if (stack != null && stack.Quantity > 0 && !string.IsNullOrWhiteSpace(stack.ItemId)) AddToStorageInternal(stack.ItemId, stack.Quantity, data.Version >= 18 ? stack.Quality : FarmItemQuality.Normal);'
if (-not $content.Contains($oldInventory) -or -not $content.Contains($oldStorage)) { throw 'Trechos de Restore nao encontrados' }
$content = $content.Replace($oldInventory, $newInventory).Replace($oldStorage, $newStorage)
[System.IO.File]::WriteAllText($submit, (@{ filePath = $asset; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$output = @(& $cli run-tool script-update-or-create $project --input-file $submit 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Falha enviando Restore`n$($output -join "`n")" }
$output | Select-Object -Last 12
