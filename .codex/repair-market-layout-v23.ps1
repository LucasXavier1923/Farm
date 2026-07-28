$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$asset = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$read = Join-Path $project 'Temp\read-market-layout-repair-v23.json'
$submit = Join-Path $project 'Temp\submit-market-layout-repair-v23.json'
[System.IO.File]::WriteAllText($read, (@{ filePath = $asset; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$lines = @(& $cli run-tool script-read $project --input-file $read 2>&1)
if ($LASTEXITCODE -ne 0) { throw 'Falha lendo HUD' }
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '{') { $start = $i; break } }
$content = [string]((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result)
$replacements = [ordered]@{
    'new Vector2(500f, 380f)' = 'new Vector2(620f, 450f)'
    'new Vector2(460f, 30f), TextAnchor.MiddleCenter' = 'new Vector2(580f, 30f), TextAnchor.MiddleCenter'
    'new Vector2(20f, -67f), new Vector2(52f, 54f)' = 'new Vector2(20f, -86f), new Vector2(52f, 54f)'
    '17, FontStyle.Normal, Color.white, new Vector2(80f, -56f), new Vector2(340f, 78f)' = '16, FontStyle.Normal, Color.white, new Vector2(80f, -55f), new Vector2(460f, 122f)'
    'new Vector2(428f, -67f), new Vector2(52f, 54f)' = 'new Vector2(548f, -86f), new Vector2(52f, 54f)'
    'new Vector2(20f, -174f), new Vector2(220f, 52f)' = 'new Vector2(20f, -205f), new Vector2(280f, 52f)'
    'new Vector2(260f, -174f), new Vector2(220f, 52f)' = 'new Vector2(320f, -205f), new Vector2(280f, 52f)'
    'new Vector2(20f, -242f), new Vector2(460f, 52f)' = 'new Vector2(20f, -273f), new Vector2(580f, 52f)'
    'new Vector2(140f, -310f), new Vector2(220f, 46f)' = 'new Vector2(200f, -351f), new Vector2(220f, 46f)'
}
foreach ($pair in $replacements.GetEnumerator()) {
    if (-not $content.Contains($pair.Key)) { throw "Layout alvo ausente: $($pair.Key)" }
    $content = $content.Replace($pair.Key, $pair.Value)
}
[System.IO.File]::WriteAllText($submit, (@{ filePath = $asset; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$output = @(& $cli run-tool script-update-or-create $project --input-file $submit 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Falha enviando HUD`n$($output -join "`n")" }
$output | Select-Object -Last 12
