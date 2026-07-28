$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$assetPath = 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs'
$diskPath = Join-Path $project ($assetPath -replace '/', '\')
$content = [IO.File]::ReadAllText($diskPath)

function Replace-Once {
    param([string]$Text, [string]$Pattern, [string]$Replacement, [string]$Label)
    $regex = [regex]::new($Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $regex.IsMatch($Text)) { throw "Padrão não encontrado: $Label" }
    return $regex.Replace($Text, $Replacement, 1)
}

$content = Replace-Once $content `
    '(private FarmCraftingSystem craftingSystem;\r?\n)(\s*private FarmTestTile hoveredTile;)' `
    ('$1        private FarmBuildingSystem buildingSystem;' + "`n" + '$2') `
    'campo buildingSystem'

$content = Replace-Once $content `
    '(public FarmCraftingSystem CraftingSystem => craftingSystem;\r?\n)(\s*public CropDefinition ActiveCrop)' `
    ('$1        public FarmBuildingSystem BuildingSystem => buildingSystem;' + "`n" + '$2') `
    'propriedade BuildingSystem'

$content = Replace-Once $content `
    '(craftingSystem\.Initialize\(this, gameState, hud, player\);\r?\n)(\s*dayClock = GetComponent<FarmDayClock>\(\);)' `
    ('$1            buildingSystem = GetComponent<FarmBuildingSystem>();' + "`n" +
     '            if (buildingSystem == null) buildingSystem = gameObject.AddComponent<FarmBuildingSystem>();' + "`n" +
     '            buildingSystem.Initialize(this, gameState, hud, player);' + "`n" +
     '$2') `
    'bootstrap buildingSystem'

$content = Replace-Once $content `
    '(UpdateHotbarSelection\(\);\r?\n)(\s*UpdateHoveredTarget\(\);)' `
    ('$1            if (buildingSystem != null && buildingSystem.IsPlacing)' + "`n" +
     '            {' + "`n" +
     '                if (saveQueued && Time.unscaledTime >= saveAt) SaveGame(false);' + "`n" +
     '                return;' + "`n" +
     '            }' + "`n" +
     '$2') `
    'bloqueio de interacao durante placement'

$content = Replace-Once $content `
    '(weatherSystem\?\.Refresh\(\);\r?\n)(\s*ApplySelectedHotbarEntry\(false\);)' `
    ('$1            buildingSystem?.RebuildFromState();' + "`n" + '$2') `
    'rebuild ao carregar'

$content = $content.Replace(
    'saveStatus = data.Version < 13 ? "Save migrado para v13" : "Save carregado";',
    'saveStatus = data.Version < 14 ? "Save migrado para v14" : "Save carregado";')

$inputDirectory = Join-Path $project 'Temp\CodexMcp'
$inputPath = Join-Path $inputDirectory 'fix-plot-v14.json'
$payload = @{ filePath = $assetPath; content = $content } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($inputPath, $payload, [Text.UTF8Encoding]::new($false))
& 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd' run-tool script-update-or-create $project --input-file $inputPath
if ($LASTEXITCODE -ne 0) { throw 'Falha ao corrigir FarmTestPlot v14' }
