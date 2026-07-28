$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$inputDirectory = Join-Path $project 'Temp\CodexMcp'

function Update-UnityScript {
    param([string]$AssetPath, [string]$Content, [string]$InputName)
    $inputPath = Join-Path $inputDirectory $InputName
    $payload = @{ filePath = $AssetPath; content = $Content } | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText($inputPath, $payload, [Text.UTF8Encoding]::new($false))
    & $cli run-tool script-update-or-create $project --input-file $inputPath
    if ($LASTEXITCODE -ne 0) { throw "Falha ao atualizar $AssetPath" }
}

$buildingPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmBuildingSystem.cs'
$building = [IO.File]::ReadAllText($buildingPath)
$start = $building.IndexOf('    [CreateAssetMenu')
$end = $building.IndexOf('    public sealed class FarmPlacedObject')
if ($start -lt 0 -or $end -le $start) { throw 'Bloco de definição não encontrado' }
$building = $building.Remove($start, $end - $start)
Update-UnityScript 'Assets/_Project/Scripts/Farming/FarmBuildingSystem.cs' $building 'remove-buildable-definition-from-system.json'

$definition = [IO.File]::ReadAllText((Join-Path $project 'Temp\FarmBuildableDefinition.cs.txt'))
Update-UnityScript 'Assets/_Project/Scripts/Farming/FarmBuildableDefinition.cs' $definition 'create-buildable-definition-script.json'
