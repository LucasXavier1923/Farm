$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$asset = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$read = Join-Path $project 'Temp\read-quality-summary-repair-v22.json'
$submit = Join-Path $project 'Temp\submit-quality-summary-repair-v22.json'
[System.IO.File]::WriteAllText($read, (@{ filePath = $asset; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$lines = @(& $cli run-tool script-read $project --input-file $read 2>&1)
if ($LASTEXITCODE -ne 0) { throw 'Falha lendo HUD' }
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '{') { $start = $i; break } }
$content = [string]((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result)
$old = '                builder.Append("\u2022 ").Append(definition != null ? definition.DisplayName : stack.ItemId).Append("  x").Append(stack.Quantity).Append(''\n'');'
$new = '                builder.Append("\u2022 ").Append(definition != null ? definition.DisplayName : stack.ItemId).Append(QualityInline(stack.Quality)).Append("  x").Append(stack.Quantity).Append(''\n'');'
if (-not $content.Contains($old)) { throw 'Linha do resumo nao encontrada' }
$content = $content.Replace($old, $new)
[System.IO.File]::WriteAllText($submit, (@{ filePath = $asset; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$output = @(& $cli run-tool script-update-or-create $project --input-file $submit 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Falha enviando HUD`n$($output -join "`n")" }
$output | Select-Object -Last 12
