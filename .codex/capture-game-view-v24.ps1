$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$raw = (& $cli run-tool screenshot-game-view $project --input-file '.codex\screenshot-game-view.json' --raw 2>&1) -join "`n"
if ($LASTEXITCODE -ne 0) { throw $raw }
$response = $raw | ConvertFrom-Json
$image = $response.content | Where-Object { $_.type -eq 'image' } | Select-Object -First 1
if ($null -eq $image -or [string]::IsNullOrWhiteSpace($image.data)) { throw 'Imagem ausente na captura da Game View' }
[System.IO.File]::WriteAllBytes((Join-Path $project 'Temp\farm-land-v24.png'), [System.Convert]::FromBase64String($image.data))
Write-Output 'Temp\farm-land-v24.png'
