$ErrorActionPreference='Stop';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd';$project='D:\Dev\Unity\Farm\Farm'
$setup=& $cli run-tool script-execute $project --input-file '.codex/show-strawberry-spring-reference.json'|Out-String;if($LASTEXITCODE-ne 0){throw $setup}
Start-Sleep -Milliseconds 800
$output=& $cli run-tool screenshot-game-view $project --input-file '.codex/screenshot-game-view.json'|Out-String
$match=[regex]::Match($output,'iVBOR[A-Za-z0-9+/=]+');if(-not $match.Success){throw 'PNG base64 nao encontrado.'}
$target=Join-Path $project 'Temp\farm-strawberry-spring.png';[IO.File]::WriteAllBytes($target,[Convert]::FromBase64String($match.Value));Write-Output $target
