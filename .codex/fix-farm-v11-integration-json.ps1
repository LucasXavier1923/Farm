$ErrorActionPreference='Stop';$path='D:\Dev\Unity\Farm\Farm\.codex\farm-v11-integration.json'
$json=Get-Content -Raw -Encoding UTF8 $path|ConvertFrom-Json
$old='if(!FarmPrototype.Farming.FarmSaveSystem.Save(snapshot,out var saveError)||!FarmPrototype.Farming.FarmSaveSystem.TryLoad(out var loaded,out var loadError))throw new System.Exception("Save/load v11 falhou: "+saveError+loadError);'
$new='var saveOk=FarmPrototype.Farming.FarmSaveSystem.Save(snapshot,out var saveError);var loadOk=FarmPrototype.Farming.FarmSaveSystem.TryLoad(out var loaded,out var loadError);if(!saveOk||!loadOk)throw new System.Exception("Save/load v11 falhou: "+saveError+loadError);'
if(-not $json.csharpCode.Contains($old)){throw 'Trecho do helper nao encontrado.'}
$json.csharpCode=$json.csharpCode.Replace($old,$new)
$json|ConvertTo-Json -Depth 10 -Compress|Set-Content -Encoding UTF8 $path
Write-Output 'farm-v11-integration.json corrigido.'
