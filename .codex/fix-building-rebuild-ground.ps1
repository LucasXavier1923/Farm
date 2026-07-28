$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$assetPath = 'Assets/_Project/Scripts/Farming/FarmBuildingSystem.cs'
$diskPath = Join-Path $project ($assetPath -replace '/', '\')
$content = [IO.File]::ReadAllText($diskPath)
$oldRebuild = @'
            for (var index = placedRoot.childCount - 1; index >= 0; index--)
                Destroy(placedRoot.GetChild(index).gameObject);
'@
$newRebuild = @'
            for (var index = placedRoot.childCount - 1; index >= 0; index--)
            {
                var child = placedRoot.GetChild(index);
                child.SetParent(null, true);
                Destroy(child.gameObject);
            }
'@
if (-not $content.Contains($oldRebuild)) { throw 'Bloco RebuildFromState não encontrado' }
$content = $content.Replace($oldRebuild, $newRebuild)
$oldGround = @'
                if (preview != null && hit.collider.transform.IsChildOf(preview.transform)) continue;
                if (hit.normal.y < 0.45f) continue;
'@
$newGround = @'
                if (preview != null && hit.collider.transform.IsChildOf(preview.transform)) continue;
                if (hit.collider.GetComponentInParent<FarmPlacedObject>() != null) continue;
                if (player != null && hit.collider.transform.IsChildOf(player)) continue;
                if (hit.normal.y < 0.45f) continue;
'@
if (-not $content.Contains($oldGround)) { throw 'Bloco TryFindGround não encontrado' }
$content = $content.Replace($oldGround, $newGround)
$inputDirectory = Join-Path $project 'Temp\CodexMcp'
$inputPath = Join-Path $inputDirectory 'fix-building-rebuild-ground.json'
$payload = @{ filePath = $assetPath; content = $content } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($inputPath, $payload, [Text.UTF8Encoding]::new($false))
& 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd' run-tool script-update-or-create $project --input-file $inputPath
if ($LASTEXITCODE -ne 0) { throw 'Falha ao corrigir FarmBuildingSystem' }
