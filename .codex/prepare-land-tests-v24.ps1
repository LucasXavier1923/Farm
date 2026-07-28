$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'

$validation = @'
var plot = UnityEngine.Object.FindAnyObjectByType<FarmPrototype.Farming.FarmTestPlot>();
var state = plot != null ? plot.GameState : null;
var hud = UnityEngine.Object.FindAnyObjectByType<FarmPrototype.Farming.FarmHudController>();
var player = UnityEngine.GameObject.Find("Player");
var sellStation = UnityEngine.GameObject.Find("Farm_Sell_Station");
if (plot == null || state == null || hud == null || player == null || sellStation == null) throw new System.Exception("Base da expansão de terreno incompleta");
var originalPlayerPosition = player.transform.position;
var controller = player.GetComponent<UnityEngine.CharacterController>();
var movement = player.GetComponent<FarmPrototype.Player.FarmPlayerController>();
var initialTiles = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTestTile>(UnityEngine.Object.FindObjectsByType<FarmPrototype.Farming.FarmTestTile>());
initialTiles.Sort((a,b) => a.Index.CompareTo(b.Index));
if (initialTiles.Count != 9 || plot.LandTileCount != 9) throw new System.Exception("Fazenda inicial deveria ter 9 canteiros");
var initialTileSaves = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTileSaveData>();
var originalPositions = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (var tile in initialTiles) { initialTileSaves.Add(tile.CaptureSaveData()); originalPositions.Add(tile.transform.position); }
var snapshot = state.CreateSaveData(initialTileSaves);
try
{
    if (controller != null) controller.enabled = false;
    if (movement != null) movement.enabled = false;
    player.transform.position = sellStation.transform.position + UnityEngine.Vector3.right;
    var funded = state.CreateSaveData(initialTileSaves);
    funded.Version = 19;
    funded.LandLevel = 1;
    funded.Money = 3000;
    state.Restore(funded);
    plot.EnsureFarmTilesForLandLevel();
    if (state.GetLandUpgradeCost() != 500 || state.LandTileCount != 9) throw new System.Exception("Economia N1 incorreta");

    if (!state.TryUpgradeLand(out var level2, out var cost2) || level2 != 2 || cost2 != 500 || state.Money != 2500) throw new System.Exception("Compra N2 incorreta");
    plot.EnsureFarmTilesForLandLevel();
    var tier2 = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTestTile>(UnityEngine.Object.FindObjectsByType<FarmPrototype.Farming.FarmTestTile>());
    tier2.Sort((a,b) => a.Index.CompareTo(b.Index));
    if (tier2.Count != 15 || plot.LandTileCount != 15) throw new System.Exception("N2 deveria ter 15 canteiros");
    for (var i = 0; i < 9; i++) if (UnityEngine.Vector3.Distance(originalPositions[i], tier2[i].transform.position) > 0.001f) throw new System.Exception("Canteiro original moveu no N2: " + i);
    for (var i = 0; i < tier2.Count; i++) if (tier2[i].Index != i) throw new System.Exception("Índice instável no N2: " + i);
    tier2[14].Restore(new FarmPrototype.Farming.FarmTileSaveData { Index = 14, State = 1, CropId = "", GrowthSecondsRemaining = 0f });
    var tier2Saves = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTileSaveData>();
    foreach (var tile in tier2) tier2Saves.Add(tile.CaptureSaveData());
    var savedTier2 = state.CreateSaveData(tier2Saves);
    if (savedTier2.Version != 19 || savedTier2.LandLevel != 2 || savedTier2.Tiles.Count != 15) throw new System.Exception("Save N2 incompleto");

    hud.SetShopOpen(true);
    typeof(FarmPrototype.Farming.FarmHudController).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(hud, null);
    var landButton = UnityEngine.GameObject.Find("UpgradeLand");
    var landText = landButton != null ? landButton.GetComponentInChildren<UnityEngine.UI.Text>() : null;
    if (landText == null || !landText.text.Contains("25 CANTEIROS") || !landText.text.Contains("$1500")) throw new System.Exception("Loja não mostra próxima expansão: " + (landText != null ? landText.text : "ausente"));

    if (!state.TryUpgradeLand(out var level3, out var cost3) || level3 != 3 || cost3 != 1500 || state.Money != 1000) throw new System.Exception("Compra N3 incorreta");
    plot.EnsureFarmTilesForLandLevel();
    var tier3 = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTestTile>(UnityEngine.Object.FindObjectsByType<FarmPrototype.Farming.FarmTestTile>());
    tier3.Sort((a,b) => a.Index.CompareTo(b.Index));
    if (tier3.Count != 25 || plot.LandTileCount != 25 || !state.IsLandMaxed || state.GetLandUpgradeCost() != 0) throw new System.Exception("N3 deveria ter 25 canteiros e estar maximizado");
    var unique = new System.Collections.Generic.HashSet<string>();
    for (var i = 0; i < tier3.Count; i++)
    {
        if (tier3[i].Index != i) throw new System.Exception("Índice instável no N3: " + i);
        unique.Add($"{tier3[i].transform.position.x:F2}:{tier3[i].transform.position.z:F2}");
        if (i < 9 && UnityEngine.Vector3.Distance(originalPositions[i], tier3[i].transform.position) > 0.001f) throw new System.Exception("Canteiro original moveu no N3: " + i);
    }
    if (unique.Count != 25) throw new System.Exception("Posições duplicadas na expansão");
    hud.SetShopOpen(true);
    typeof(FarmPrototype.Farming.FarmHudController).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(hud, null);
    if (!landText.text.Contains("TERRENO M") || !landText.text.Contains("XIMO") || !landText.text.Contains("25 CANTEIROS")) throw new System.Exception("Loja não mostra terreno máximo: " + landText.text + " | level=" + state.LandLevel + " tiles=" + plot.LandTileCount);

    var legacy = state.CreateSaveData(new System.Collections.Generic.List<FarmPrototype.Farming.FarmTileSaveData>());
    legacy.Version = 18;
    legacy.LandLevel = 3;
    state.Restore(legacy);
    plot.EnsureFarmTilesForLandLevel();
    if (state.LandLevel != 1 || plot.LandTileCount != 9) throw new System.Exception("Migração v18 deveria iniciar com 9 canteiros");

    state.Restore(savedTier2);
    plot.EnsureFarmTilesForLandLevel();
    foreach (var tileData in savedTier2.Tiles)
    {
        var tile = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTestTile>(UnityEngine.Object.FindObjectsByType<FarmPrototype.Farming.FarmTestTile>()).Find(item => item.Index == tileData.Index);
        if (tile != null) tile.Restore(tileData);
    }
    var restored14 = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTestTile>(UnityEngine.Object.FindObjectsByType<FarmPrototype.Farming.FarmTestTile>()).Find(item => item.Index == 14);
    if (plot.LandTileCount != 15 || restored14 == null || restored14.CaptureSaveData().State != 1) throw new System.Exception("Load N2 perdeu terreno ou estado do canteiro 14");
    UnityEngine.Debug.Log("[LAND_V24_OK] 9->15 $500 | 15->25 $1500 | stable original positions | stable indices | unique grid | UI | save19 | migration18");
}
finally
{
    hud.SetShopOpen(false);
    state.Restore(snapshot);
    plot.EnsureFarmTilesForLandLevel();
    var restored = new System.Collections.Generic.List<FarmPrototype.Farming.FarmTestTile>(UnityEngine.Object.FindObjectsByType<FarmPrototype.Farming.FarmTestTile>());
    foreach (var tile in restored)
    {
        var saved = initialTileSaves.Find(item => item.Index == tile.Index);
        if (saved != null) tile.Restore(saved);
    }
    plot.DayClock.SetClockForTesting(snapshot.DayNumber, snapshot.MinutesOfDay);
    plot.DayClock.SetSimulationSpeed(1f);
    UnityEngine.Time.timeScale = 1f;
    player.transform.position = originalPlayerPosition;
    if (controller != null) controller.enabled = true;
    if (movement != null) movement.enabled = true;
}
'@

$payload = @{
    csharpCode = $validation
    className = 'ValidateLandV24'
    methodName = 'Run'
    parameters = @()
    isMethodBody = $true
} | ConvertTo-Json -Compress
[System.IO.File]::WriteAllText((Join-Path $project 'Temp\validate-land-v24.json'), $payload, [System.Text.UTF8Encoding]::new($false))

$validationFiles = Get-ChildItem -LiteralPath (Join-Path $project 'Temp') -Filter 'validate-*.json'
foreach ($file in $validationFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text.Contains('Version!=18')) {
        [System.IO.File]::WriteAllText($file.FullName, $text.Replace('Version!=18', 'Version!=19'), [System.Text.UTF8Encoding]::new($false))
    }
}

Write-Output 'LAND_V24_TESTS_PREPARED'
