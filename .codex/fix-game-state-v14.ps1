$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$assetPath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$diskPath = Join-Path $project ($assetPath -replace '/', '\')
$content = [IO.File]::ReadAllText($diskPath)

function Replace-Once {
    param([string]$Text, [string]$Pattern, [string]$Replacement, [string]$Label)
    $regex = [regex]::new($Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $regex.IsMatch($Text)) { throw "Padrão não encontrado: $Label" }
    return $regex.Replace($Text, $Replacement, 1)
}

$content = Replace-Once $content `
    '(public FarmMasteryProgress Mastery = new\(\);\r?\n)(\s*public int PumpkinSeeds;)' `
    ('$1        public List<FarmPlacedObjectSaveData> PlacedObjects = new();' + "`n" + '$2') `
    'FarmSaveData.PlacedObjects'

$content = Replace-Once $content `
    '(\[SerializeField\] private FarmMasteryProgress mastery = new\(\);\r?\n)(\s*private bool lastEnergyActionWasFree;)' `
    ('$1        [SerializeField] private List<FarmPlacedObjectSaveData> placedObjects = new();' + "`n" + '$2') `
    'campo placedObjects'

$content = Replace-Once $content `
    '(public FarmMasteryProgress Mastery => mastery;\r?\n)(\s*public bool LastEnergyActionWasFree)' `
    ('$1        public IReadOnlyList<FarmPlacedObjectSaveData> PlacedObjects => placedObjects;' + "`n" + '$2') `
    'propriedade PlacedObjects'

$content = Replace-Once $content `
    '(Mastery = \(mastery \?\? new FarmMasteryProgress\(\)\)\.Clone\(\),\r?\n)(\s*PumpkinSeeds = PumpkinSeeds,)' `
    ('$1                PlacedObjects = ClonePlacedObjects(placedObjects),' + "`n" + '$2') `
    'snapshot PlacedObjects'

$content = Replace-Once $content `
    '(mastery = data\.Version >= 13 && data\.Mastery != null \? data\.Mastery\.Clone\(\) : new FarmMasteryProgress\(\);\r?\n)(\s*lastEnergyActionWasFree = false;)' `
    ('$1            placedObjects.Clear();' + "`n" +
     '            if (data.Version >= 14 && data.PlacedObjects != null)' + "`n" +
     '                foreach (var placed in data.PlacedObjects)' + "`n" +
     '                    if (IsValidPlacedObject(placed)) placedObjects.Add(placed.Clone());' + "`n" +
     '$2') `
    'restore PlacedObjects'

$inputDirectory = Join-Path $project 'Temp\CodexMcp'
[IO.Directory]::CreateDirectory($inputDirectory) | Out-Null
$inputPath = Join-Path $inputDirectory 'fix-game-state-v14.json'
$payload = @{ filePath = $assetPath; content = $content } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($inputPath, $payload, [Text.UTF8Encoding]::new($false))
& 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd' run-tool script-update-or-create $project --input-file $inputPath
if ($LASTEXITCODE -ne 0) { throw 'Falha ao corrigir FarmGameState v14' }
