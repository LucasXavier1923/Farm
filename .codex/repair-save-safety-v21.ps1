$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$assetPath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$readPath = Join-Path $project 'Temp\read-state-repair-v21.json'
$submitPath = Join-Path $project 'Temp\submit-state-repair-v21.json'
[System.IO.File]::WriteAllText($readPath, (@{ filePath = $assetPath; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$lines = @(& $cli run-tool script-read $project --input-file $readPath 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Falha lendo FarmGameState`n$($lines -join "`n")" }
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Trim() -eq '{') { $start = $i; break }
}
if ($start -lt 0) { throw 'JSON ausente na leitura do reparo' }
$response = (($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json)
$content = [string]$response.structured.result
$old = @'
        {
            LastLoadUsedBackup = false;
            if (TryRead(path, out data, out var primaryError)) return true;
'@
$new = @'
        {
            LastLoadUsedBackup = false;
            error = null;
            if (TryRead(path, out data, out var primaryError)) return true;
'@
if (-not $content.Contains($old)) { throw 'Trecho de reparo nao encontrado' }
$content = $content.Replace($old, $new)
[System.IO.File]::WriteAllText($submitPath, (@{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$output = @(& $cli run-tool script-update-or-create $project --input-file $submitPath 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Falha enviando reparo`n$($output -join "`n")" }
$output | Select-Object -Last 15
