$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$asset = 'Assets/_Project/Scripts/Farming/FarmItemQuality.cs'
$read = Join-Path $project 'Temp\read-quality-symbols-repair-v22.json'
$submit = Join-Path $project 'Temp\submit-quality-symbols-repair-v22.json'
[System.IO.File]::WriteAllText($read, (@{ filePath = $asset; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$lines = @(& $cli run-tool script-read $project --input-file $read 2>&1)
if ($LASTEXITCODE -ne 0) { throw 'Falha lendo FarmItemQuality' }
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '{') { $start = $i; break } }
if ($start -lt 0) { throw 'JSON ausente' }
$content = [string]((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result)
$content = $content.Replace('"◆"', '"\u25C6"').Replace('"★"', '"\u2605"')
if (-not $content.Contains('"\u25C6"') -or -not $content.Contains('"\u2605"')) { throw 'Escapes Unicode nao foram aplicados' }
[System.IO.File]::WriteAllText($submit, (@{ filePath = $asset; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$output = @(& $cli run-tool script-update-or-create $project --input-file $submit 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Falha enviando simbolos`n$($output -join "`n")" }
$output | Select-Object -Last 12
