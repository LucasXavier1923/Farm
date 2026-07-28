$ErrorActionPreference='Stop'
$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project='D:\Dev\Unity\Farm\Farm'
$output=& $cli run-tool screenshot-game-view $project --input-file '.codex/screenshot-game-view.json'|Out-String
$match=[regex]::Match($output,'iVBOR[A-Za-z0-9+/=]+')
if(-not $match.Success){throw 'PNG base64 nao encontrado.'}
$target=Join-Path $project 'Temp\farm-calendar-v10.png'
[IO.File]::WriteAllBytes($target,[Convert]::FromBase64String($match.Value))
Write-Output $target
