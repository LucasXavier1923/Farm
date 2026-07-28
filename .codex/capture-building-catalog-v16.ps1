$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$output = (& $cli run-tool screenshot-game-view $project --input-file (Join-Path $project '.codex\screenshot-game-view.json')) | Out-String
$match = [regex]::Match($output, '"data":\s*"(?<image>iVBOR[^"]+)"')
if (-not $match.Success) {
    throw 'A captura nao retornou uma imagem PNG.'
}
$target = Join-Path $project 'Temp\farm-building-catalog-v16.png'
[IO.File]::WriteAllBytes($target, [Convert]::FromBase64String($match.Groups['image'].Value))
Write-Output $target
