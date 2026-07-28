param(
    [Parameter(Mandatory = $true)]
    [string]$OutputName
)

$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$previousPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$raw = (& $cli run-tool screenshot-game-view $project --input-file '.codex\screenshot-game-view.json' --raw 2>&1) -join "`n"
$exitCode = $LASTEXITCODE
$ErrorActionPreference = $previousPreference
if ($exitCode -ne 0) { throw $raw }
$jsonStart = $raw.IndexOf('{')
$jsonEnd = $raw.LastIndexOf('}')
if ($jsonStart -lt 0 -or $jsonEnd -lt $jsonStart) { throw 'Resposta JSON ausente na captura da Game View' }
$response = $raw.Substring($jsonStart, $jsonEnd - $jsonStart + 1) | ConvertFrom-Json
$image = $response.content | Where-Object { $_.type -eq 'image' } | Select-Object -First 1
if ($null -eq $image -or [string]::IsNullOrWhiteSpace($image.data)) { throw 'Imagem ausente na captura da Game View' }
$outputPath = Join-Path $project ("Temp\" + $OutputName)
[System.IO.File]::WriteAllBytes($outputPath, [System.Convert]::FromBase64String($image.data))
Write-Output $outputPath
