$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm'
$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function Replace-Once([string]$content,[string]$old,[string]$new,[string]$label){if(-not $content.Contains($old)){throw "Trecho nao encontrado: $label"};$content.Replace($old,$new)}
function Submit([string]$path,[string]$content,[string]$id){$payload=@{filePath=$path;content=$content;requestId=$id}|ConvertTo-Json -Compress;$result=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $path"};$result|Select-Object -Last 12;Start-Sleep -Seconds 2}

$cropPath=Join-Path $project 'Assets\_Project\Scripts\Farming\CropDefinition.cs'
$crop=[IO.File]::ReadAllText($cropPath)
if(-not $crop.Contains('PreferredSeasonYieldBonus')){
  $crop=Replace-Once $crop @'
        [Min(1)] public int HarvestYield = 1;
'@ @'
        [Min(1)] public int HarvestYield = 1;
        public FarmSeason PreferredSeason = FarmSeason.Spring;
        [Min(0)] public int PreferredSeasonYieldBonus = 1;
'@ 'season fields'
  $crop=Replace-Once $crop @'
        [Min(0)] public int SeedPackPrice = 20;
'@ @'
        [Min(0)] public int SeedPackPrice = 20;

        public bool IsPreferredSeason(FarmSeason season) => season == PreferredSeason;

        public int HarvestYieldForSeason(FarmSeason season) =>
            HarvestYield + (IsPreferredSeason(season) ? Mathf.Max(0, PreferredSeasonYieldBonus) : 0);

        public string AffinityText => $"{FarmDayClock.SeasonName(PreferredSeason)}  \u2022  +{Mathf.Max(0, PreferredSeasonYieldBonus)} na colheita";
'@ 'season API'
  Submit 'Assets/_Project/Scripts/Farming/CropDefinition.cs' $crop 'crop-season-affinity-v10'
}

$plotPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'
$plot=[IO.File]::ReadAllText($plotPath)
if(-not $plot.Contains('HarvestYieldForSeason')){
  $plot=Replace-Once $plot @'
            if (tool == FarmTool.Harvest && state == State.Ready)
            {
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, cropDefinition.HarvestYield))
                    return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                inventory.RecordJournal(FarmJournalMetric.HarvestedUnits, cropDefinition.HarvestYield, cropDefinition.Id);
                state = State.Tilled;
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Harvested);
                return $"Colheu {cropDefinition.HarvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s).";
            }
'@ @'
            if (tool == FarmTool.Harvest && state == State.Ready)
            {
                var season = plot.DayClock != null ? plot.DayClock.CurrentSeason : FarmSeason.Spring;
                var harvestYield = cropDefinition.HarvestYieldForSeason(season);
                if (!inventory.AddItem(cropDefinition.HarvestItem.Id, harvestYield))
                    return "Invent\u00E1rio cheio. Libere espa\u00E7o antes de colher.";
                inventory.RecordJournal(FarmJournalMetric.HarvestedUnits, harvestYield, cropDefinition.Id);
                state = State.Tilled;
                RefreshVisual();
                plot.NotifyTileChanged();
                plot.MarkMilestone(FarmMilestone.Harvested);
                var seasonalBonus = harvestYield - cropDefinition.HarvestYield;
                return seasonalBonus > 0
                    ? $"Colheu {harvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s), incluindo +{seasonalBonus} da afinidade com {FarmDayClock.SeasonName(season)}."
                    : $"Colheu {harvestYield} {cropDefinition.DisplayName.ToLowerInvariant()}(s).";
            }
'@ 'season harvest'
  Submit 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'plot-season-harvest-v10'
}

$hudPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs'
$hud=[IO.File]::ReadAllText($hudPath)
if(-not $hud.Contains('shopCrop.AffinityText')){
  $hud=Replace-Once $hud @'
                shopInfoText.text = $"{shopCrop.DisplayName.ToUpperInvariant()}  ({plot.ShopCropIndex + 1}/{plot.ShopCropCount})\n{shopCrop.SeedPackAmount} sementes: ${shopCrop.SeedPackPrice}  \u2022  Venda: ${shopCrop.HarvestItem.BaseSellPrice} cada";
'@ @'
                shopInfoText.text = $"{shopCrop.DisplayName.ToUpperInvariant()}  ({plot.ShopCropIndex + 1}/{plot.ShopCropCount})\nAfinidade: {shopCrop.AffinityText}\n{shopCrop.SeedPackAmount} sementes: ${shopCrop.SeedPackPrice}  \u2022  Venda: ${shopCrop.HarvestItem.BaseSellPrice} cada";
'@ 'shop affinity'
  Submit 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'hud-season-affinity-v10'
}
Write-Output 'Season affinity scripts submitted.'
