$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function Replace-Once([string]$c,[string]$o,[string]$n,[string]$l){if(-not $c.Contains($o)){throw "Trecho nao encontrado: $l"};$c.Replace($o,$n)}
function Submit([string]$p,[string]$c,[string]$id){$payload=@{filePath=$p;content=$c;requestId=$id}|ConvertTo-Json -Compress;$r=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $p"};$r|Select-Object -Last 12;Start-Sleep -Seconds 2}

$statePath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmGameState.cs';$state=[IO.File]::ReadAllText($statePath)
if(-not $state.Contains('MaxEnergy')){
 $state=Replace-Once $state 'public int Version = 11;' 'public int Version = 12;' 'save version'
 $state=Replace-Once $state @'
        public FarmDailyOrderProgress DailyOrders = new();
        public int PumpkinSeeds;
'@ @'
        public FarmDailyOrderProgress DailyOrders = new();
        public int Energy = FarmGameState.MaxEnergy;
        public int PumpkinSeeds;
'@ 'save energy'
 $state=Replace-Once $state @'
        public const int MaxToolLevel = 3;
'@ @'
        public const int MaxToolLevel = 3;
        public const int MaxEnergy = 100;
'@ 'energy constant'
 $state=Replace-Once $state @'
        [SerializeField] private FarmDailyOrderProgress dailyOrders = new();
'@ @'
        [SerializeField] private FarmDailyOrderProgress dailyOrders = new();
        [SerializeField, Range(0, MaxEnergy)] private int energy = MaxEnergy;
'@ 'runtime energy'
 $state=Replace-Once $state @'
        public int PumpkinSeeds => GetQuantity(PumpkinSeedId);
'@ @'
        public int Energy => energy;
        public float EnergyRatio => energy / (float)MaxEnergy;
        public bool IsExhausted => energy <= 0;
        public int PumpkinSeeds => GetQuantity(PumpkinSeedId);
'@ 'energy properties'
 $state=Replace-Once $state @'
            EnsureHotbar();
        }

        public int GetQuantity
'@ @'
            energy = Mathf.Clamp(energy, 0, MaxEnergy);
            EnsureHotbar();
        }

        public static int EnergyCostPerTile(FarmTool tool) => tool switch
        {
            FarmTool.Hoe => 4,
            FarmTool.Seeds => 1,
            FarmTool.WateringCan => 3,
            FarmTool.Harvest => 2,
            _ => 0
        };

        public int SpendEnergy(FarmTool tool, int changedTiles)
        {
            var requested = EnergyCostPerTile(tool) * Mathf.Max(0, changedTiles);
            if (requested <= 0) return 0;
            var previous = energy;
            energy = Mathf.Max(0, energy - requested);
            if (energy != previous) NotifyChanged();
            return previous - energy;
        }

        public int RestoreEnergy()
        {
            var recovered = MaxEnergy - energy;
            if (recovered <= 0) return 0;
            energy = MaxEnergy;
            NotifyChanged();
            return recovered;
        }

        public int GetQuantity
'@ 'energy API'
 $state=Replace-Once $state 'Version = 11,' 'Version = 12,' 'snapshot version'
 $state=Replace-Once $state @'
                DailyOrders = DailyOrders.Clone(),
                PumpkinSeeds = PumpkinSeeds,
'@ @'
                DailyOrders = DailyOrders.Clone(),
                Energy = energy,
                PumpkinSeeds = PumpkinSeeds,
'@ 'snapshot energy'
 $state=Replace-Once $state @'
            EnsureDailyOrdersForCurrentDay(false);
            NotifyChanged();
        }

        private void EnsureHotbar
'@ @'
            EnsureDailyOrdersForCurrentDay(false);
            energy = data.Version >= 12 ? Mathf.Clamp(data.Energy, 0, MaxEnergy) : MaxEnergy;
            NotifyChanged();
        }

        private void EnsureHotbar
'@ 'restore energy'
 Submit 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $state 'energy-game-state-v12'
}

$plotPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs';$plot=[IO.File]::ReadAllText($plotPath)
if(-not $plot.Contains('EffectiveToolLevel')){
 $plot=Replace-Once $plot @'
        public int ActiveToolLevel => gameState != null ? gameState.GetToolLevel(activeTool) : 1;
        public string ActiveToolAreaText => ToolAreaText(activeTool, ActiveToolLevel);
        public string ActiveToolDisplayName => FarmGameState.IsUpgradeableTool(activeTool)
            ? $"{ToolName(activeTool)} L{ActiveToolLevel} \u2022 {ActiveToolAreaText}"
            : ToolName(activeTool);
'@ @'
        public int ActiveToolLevel => gameState != null ? gameState.GetToolLevel(activeTool) : 1;
        public int EffectiveToolLevel => gameState != null && gameState.IsExhausted && FarmGameState.IsUpgradeableTool(activeTool) ? 1 : ActiveToolLevel;
        public string ActiveToolAreaText => ToolAreaText(activeTool, EffectiveToolLevel);
        public string ActiveToolDisplayName => FarmGameState.IsUpgradeableTool(activeTool)
            ? $"{ToolName(activeTool)} L{ActiveToolLevel} \u2022 {ActiveToolAreaText}{(gameState != null && gameState.IsExhausted ? " (cansado)" : string.Empty)}"
            : ToolName(activeTool);
'@ 'effective tool level'
 $plot=Replace-Once $plot @'
            var nextDay = Mathf.Max(1, gameState.DayNumber + 1);
            dayClock.SetClock(nextDay, 360f);
'@ @'
            var recoveredEnergy = gameState.RestoreEnergy();
            var nextDay = Mathf.Max(1, gameState.DayNumber + 1);
            dayClock.SetClock(nextDay, 360f);
'@ 'sleep recovery'
 $plot=Replace-Once $plot @'
            feedback = advancedCrops > 0
                ? $"Bom dia! {advancedCrops} cultivo(s) avan\u00E7aram durante a noite."
                : "Bom dia! Um novo dia come\u00E7ou na fazenda.";
'@ @'
            feedback = advancedCrops > 0
                ? $"Bom dia! {advancedCrops} cultivo(s) avan\u00E7aram durante a noite."
                : "Bom dia! Um novo dia come\u00E7ou na fazenda.";
            feedback += recoveredEnergy > 0 ? $" Energia restaurada: +{recoveredEnergy}." : " Energia j\u00E1 estava completa.";
'@ 'sleep feedback'
 $plot=Replace-Once $plot @'
            var level = gameState != null && FarmGameState.IsUpgradeableTool(tool) ? gameState.GetToolLevel(tool) : 1;
'@ @'
            var level = gameState != null && FarmGameState.IsUpgradeableTool(tool)
                ? (gameState.IsExhausted ? 1 : gameState.GetToolLevel(tool))
                : 1;
'@ 'exhausted area'
 $plot=Replace-Once $plot @'
            if (changed <= 1) return !string.IsNullOrEmpty(successMessage) ? successMessage : targetMessage;
            return activeTool switch
            {
                FarmTool.Hoe => $"Preparou {changed} canteiros de uma vez.",
                FarmTool.WateringCan => $"Regou {changed} canteiros de uma vez.",
                FarmTool.Harvest => $"Colheu {changed} canteiros de uma vez.",
                _ => successMessage
            };
'@ @'
            var resultMessage = changed <= 1
                ? (!string.IsNullOrEmpty(successMessage) ? successMessage : targetMessage)
                : activeTool switch
                {
                    FarmTool.Hoe => $"Preparou {changed} canteiros de uma vez.",
                    FarmTool.WateringCan => $"Regou {changed} canteiros de uma vez.",
                    FarmTool.Harvest => $"Colheu {changed} canteiros de uma vez.",
                    _ => successMessage
                };
            if (changed <= 0) return resultMessage;

            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(activeTool, changed);
            if (!wasExhausted && gameState.IsExhausted)
                resultMessage += " Energia esgotada; voc\u00EA continua agindo, mas ferramentas de \u00E1rea passam a usar 1 canteiro at\u00E9 dormir.";
            else if (wasExhausted)
                resultMessage += " Cansado, mas a a\u00E7\u00E3o foi conclu\u00EDda.";
            return resultMessage;
'@ 'energy spending'
 $plot=$plot.Replace('data.Version < 11 ? "Save migrado para v11" : "Save carregado"','data.Version < 12 ? "Save migrado para v12" : "Save carregado"')
 Submit 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'energy-plot-v12'
}

$hudPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs';$hud=[IO.File]::ReadAllText($hudPath)
if(-not $hud.Contains('energyFill')){
 $hud=Replace-Once $hud @'
        private Text weatherText;
'@ @'
        private Text weatherText;
        private Image energyFill;
        private Text energyText;
'@ 'energy HUD fields'
 $hud=Replace-Once $hud @'
            resourcesText.text = $"${state.Money}    Sementes: {state.GetQuantity(crop.SeedItem.Id)}    {crop.DisplayName}: {state.GetQuantity(crop.HarvestItem.Id)}";
'@ @'
            resourcesText.text = $"${state.Money}    Sementes: {state.GetQuantity(crop.SeedItem.Id)}    {crop.DisplayName}: {state.GetQuantity(crop.HarvestItem.Id)}";
            energyFill.fillAmount = state.EnergyRatio;
            energyFill.color = state.IsExhausted ? new Color(0.86f, 0.25f, 0.20f)
                : state.Energy <= 25 ? new Color(0.96f, 0.56f, 0.16f)
                : new Color(0.30f, 0.78f, 0.38f);
            energyText.text = state.IsExhausted
                ? $"ENERGIA  {state.Energy}/{FarmGameState.MaxEnergy}  \u2022  CANSADO: \u00E1rea reduzida"
                : $"ENERGIA  {state.Energy}/{FarmGameState.MaxEnergy}";
'@ 'energy HUD update'
 $hud=Replace-Once $hud 'clockText == null || calendarText == null || weatherText == null' 'clockText == null || calendarText == null || weatherText == null || energyFill == null || energyText == null' 'energy interface ready'
 $hud=Replace-Once $hud @'
            weatherText = CreateText("WeatherValue", clockPanel.transform, "Ensolarado   \u2022   Amanh\u00E3: Nublado", 14, FontStyle.Bold, new Color(1f, 0.82f, 0.34f), new Vector2(15f, -98f), new Vector2(410f, 24f), TextAnchor.MiddleCenter);
            var statusPanel = CreatePanel
'@ @'
            weatherText = CreateText("WeatherValue", clockPanel.transform, "Ensolarado   \u2022   Amanh\u00E3: Nublado", 14, FontStyle.Bold, new Color(1f, 0.82f, 0.34f), new Vector2(15f, -98f), new Vector2(410f, 24f), TextAnchor.MiddleCenter);

            var energyPanel = CreatePanel("EnergyPanel", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(440f, 48f), new Vector2(0.5f, 1f), PanelColor);
            var energyTrack = CreatePanel("EnergyTrack", energyPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -9f), new Vector2(416f, 30f), new Vector2(0f, 1f), new Color(0.08f, 0.10f, 0.075f, 1f));
            var energyFillObject = CreatePanel("EnergyFill", energyTrack.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(416f, 30f), new Vector2(0f, 1f), new Color(0.30f, 0.78f, 0.38f));
            energyFill = energyFillObject.GetComponent<Image>();
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.fillOrigin = 0;
            energyFill.fillAmount = 1f;
            energyText = CreateText("EnergyText", energyPanel.transform, "ENERGIA  100/100", 15, FontStyle.Bold, Color.white, new Vector2(12f, -9f), new Vector2(416f, 30f), TextAnchor.MiddleCenter);
            var statusPanel = CreatePanel
'@ 'energy panel'
 Submit 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'energy-hud-v12'
}
Write-Output 'Energy v12 submitted.'
