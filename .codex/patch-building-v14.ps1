$ErrorActionPreference = 'Stop'

function Invoke-UnityScriptUpdate {
    param(
        [string]$ProjectPath,
        [string]$AssetPath,
        [string]$Content,
        [string]$InputName
    )

    $inputDirectory = Join-Path $ProjectPath 'Temp\CodexMcp'
    [IO.Directory]::CreateDirectory($inputDirectory) | Out-Null
    $inputPath = Join-Path $inputDirectory $InputName
    $payload = @{ filePath = $AssetPath; content = $Content } | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText($inputPath, $payload, [Text.UTF8Encoding]::new($false))
    & 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd' run-tool script-update-or-create $ProjectPath --input-file $inputPath
    if ($LASTEXITCODE -ne 0) { throw "Falha ao atualizar $AssetPath" }
}

$project = 'D:\Dev\Unity\Farm\Farm'
$statePath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmGameState.cs'
$state = [IO.File]::ReadAllText($statePath)

$tileBlock = @'
    [Serializable]
    public sealed class FarmTileSaveData
    {
        public int Index;
        public int State;
        public string CropId;
        public float GrowthSecondsRemaining;
    }
'@
$placedBlock = @'
    [Serializable]
    public sealed class FarmTileSaveData
    {
        public int Index;
        public int State;
        public string CropId;
        public float GrowthSecondsRemaining;
    }

    [Serializable]
    public sealed class FarmPlacedObjectSaveData
    {
        public string PersistentId;
        public string ItemId;
        public float X;
        public float Y;
        public float Z;
        public float RotationY;

        public FarmPlacedObjectSaveData Clone() => new()
        {
            PersistentId = PersistentId,
            ItemId = ItemId,
            X = X,
            Y = Y,
            Z = Z,
            RotationY = RotationY
        };
    }
'@
if (-not $state.Contains($tileBlock)) { throw 'Bloco FarmTileSaveData não encontrado' }
$state = $state.Replace($tileBlock, $placedBlock)
$state = $state.Replace('public int Version = 13;', 'public int Version = 14;')
$state = $state.Replace(
    '        public FarmMasteryProgress Mastery = new();' + "`r`n" +
    '        public int PumpkinSeeds;',
    '        public FarmMasteryProgress Mastery = new();' + "`r`n" +
    '        public List<FarmPlacedObjectSaveData> PlacedObjects = new();' + "`r`n" +
    '        public int PumpkinSeeds;')
$state = $state.Replace(
    '        [SerializeField] private FarmMasteryProgress mastery = new();' + "`r`n" +
    '        private bool lastEnergyActionWasFree;',
    '        [SerializeField] private FarmMasteryProgress mastery = new();' + "`r`n" +
    '        [SerializeField] private List<FarmPlacedObjectSaveData> placedObjects = new();' + "`r`n" +
    '        private bool lastEnergyActionWasFree;')
$state = $state.Replace(
    '        public FarmMasteryProgress Mastery => mastery;' + "`r`n" +
    '        public bool LastEnergyActionWasFree',
    '        public FarmMasteryProgress Mastery => mastery;' + "`r`n" +
    '        public IReadOnlyList<FarmPlacedObjectSaveData> PlacedObjects => placedObjects;' + "`r`n" +
    '        public bool LastEnergyActionWasFree')
$state = $state.Replace('                Version = 13,', '                Version = 14,')
$state = $state.Replace(
    '                Mastery = (mastery ?? new FarmMasteryProgress()).Clone(),' + "`r`n" +
    '                PumpkinSeeds = PumpkinSeeds,',
    '                Mastery = (mastery ?? new FarmMasteryProgress()).Clone(),' + "`r`n" +
    '                PlacedObjects = ClonePlacedObjects(placedObjects),' + "`r`n" +
    '                PumpkinSeeds = PumpkinSeeds,')
$state = $state.Replace(
    '            mastery = data.Version >= 13 && data.Mastery != null ? data.Mastery.Clone() : new FarmMasteryProgress();' + "`r`n" +
    '            lastEnergyActionWasFree = false;',
    '            mastery = data.Version >= 13 && data.Mastery != null ? data.Mastery.Clone() : new FarmMasteryProgress();' + "`r`n" +
    '            placedObjects.Clear();' + "`r`n" +
    '            if (data.Version >= 14 && data.PlacedObjects != null)' + "`r`n" +
    '                foreach (var placed in data.PlacedObjects)' + "`r`n" +
    '                    if (IsValidPlacedObject(placed)) placedObjects.Add(placed.Clone());' + "`r`n" +
    '            lastEnergyActionWasFree = false;')

$tryCraftMarker = '        public bool TryCraft(CraftingRecipe recipe, out string error)'
$placedMethods = @'
        public bool AddPlacedObject(FarmPlacedObjectSaveData data)
        {
            if (!IsValidPlacedObject(data)) return false;
            foreach (var placed in placedObjects)
                if (string.Equals(placed.PersistentId, data.PersistentId, StringComparison.OrdinalIgnoreCase))
                    return false;
            placedObjects.Add(data.Clone());
            NotifyChanged();
            return true;
        }

        public bool RemovePlacedObject(string persistentId)
        {
            if (string.IsNullOrWhiteSpace(persistentId)) return false;
            for (var index = placedObjects.Count - 1; index >= 0; index--)
            {
                if (!string.Equals(placedObjects[index].PersistentId, persistentId, StringComparison.OrdinalIgnoreCase)) continue;
                placedObjects.RemoveAt(index);
                NotifyChanged();
                return true;
            }
            return false;
        }

        private static bool IsValidPlacedObject(FarmPlacedObjectSaveData data) =>
            data != null &&
            !string.IsNullOrWhiteSpace(data.PersistentId) &&
            FarmBuildableDatabase.GetByItemId(data.ItemId) != null &&
            float.IsFinite(data.X) &&
            float.IsFinite(data.Y) &&
            float.IsFinite(data.Z) &&
            float.IsFinite(data.RotationY);

'@
if (-not $state.Contains($tryCraftMarker)) { throw 'Marcador TryCraft não encontrado' }
$state = $state.Replace($tryCraftMarker, $placedMethods + $tryCraftMarker)

$cloneMarker = '        private static List<InventoryStack> CloneStacks(List<InventoryStack> source)'
$clonePlaced = @'
        private static List<FarmPlacedObjectSaveData> ClonePlacedObjects(List<FarmPlacedObjectSaveData> source)
        {
            var result = new List<FarmPlacedObjectSaveData>(source.Count);
            foreach (var placed in source)
                if (IsValidPlacedObject(placed)) result.Add(placed.Clone());
            return result;
        }

'@
if (-not $state.Contains($cloneMarker)) { throw 'Marcador CloneStacks não encontrado' }
$state = $state.Replace($cloneMarker, $clonePlaced + $cloneMarker)

Invoke-UnityScriptUpdate $project 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $state 'update-game-state-v14-building.json'

$buildingContent = [IO.File]::ReadAllText((Join-Path $project 'Temp\FarmBuildingSystem.cs.txt'))
Invoke-UnityScriptUpdate $project 'Assets/_Project/Scripts/Farming/FarmBuildingSystem.cs' $buildingContent 'create-building-system-v14.json'

$plotPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'
$plot = [IO.File]::ReadAllText($plotPath)
$plot = $plot.Replace(
    '        private FarmCraftingSystem craftingSystem;' + "`r`n" +
    '        private FarmTestTile hoveredTile;',
    '        private FarmCraftingSystem craftingSystem;' + "`r`n" +
    '        private FarmBuildingSystem buildingSystem;' + "`r`n" +
    '        private FarmTestTile hoveredTile;')
$plot = $plot.Replace(
    '        public FarmCraftingSystem CraftingSystem => craftingSystem;' + "`r`n" +
    '        public CropDefinition ActiveCrop',
    '        public FarmCraftingSystem CraftingSystem => craftingSystem;' + "`r`n" +
    '        public FarmBuildingSystem BuildingSystem => buildingSystem;' + "`r`n" +
    '        public CropDefinition ActiveCrop')
$plot = $plot.Replace(
    '            craftingSystem.Initialize(this, gameState, hud, player);' + "`r`n" +
    '            dayClock = GetComponent<FarmDayClock>();',
    '            craftingSystem.Initialize(this, gameState, hud, player);' + "`r`n" +
    '            buildingSystem = GetComponent<FarmBuildingSystem>();' + "`r`n" +
    '            if (buildingSystem == null) buildingSystem = gameObject.AddComponent<FarmBuildingSystem>();' + "`r`n" +
    '            buildingSystem.Initialize(this, gameState, hud, player);' + "`r`n" +
    '            dayClock = GetComponent<FarmDayClock>();')
$plot = $plot.Replace(
    '            UpdateHotbarSelection();' + "`r`n" +
    '            UpdateHoveredTarget();',
    '            UpdateHotbarSelection();' + "`r`n" +
    '            if (buildingSystem != null && buildingSystem.IsPlacing)' + "`r`n" +
    '            {' + "`r`n" +
    '                if (saveQueued && Time.unscaledTime >= saveAt) SaveGame(false);' + "`r`n" +
    '                return;' + "`r`n" +
    '            }' + "`r`n" +
    '            UpdateHoveredTarget();')
$plot = $plot.Replace(
    '            weatherSystem?.Refresh();' + "`r`n" +
    '            ApplySelectedHotbarEntry(false);',
    '            weatherSystem?.Refresh();' + "`r`n" +
    '            buildingSystem?.RebuildFromState();' + "`r`n" +
    '            ApplySelectedHotbarEntry(false);')
$plot = $plot.Replace(
    '            saveStatus = data.Version < 13 ? "Save migrado para v13" : "Save carregado";',
    '            saveStatus = data.Version < 14 ? "Save migrado para v14" : "Save carregado";')

Invoke-UnityScriptUpdate $project 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'update-test-plot-v14-building.json'
