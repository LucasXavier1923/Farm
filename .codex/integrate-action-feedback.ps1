$ErrorActionPreference='Stop'
$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function ReplaceExact([string]$c,[string]$o,[string]$n,[string]$l){if(-not $c.Contains($o)){throw "Trecho nao encontrado: $l"};$c.Replace($o,$n)}
$path=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs';$plot=[IO.File]::ReadAllText($path)
if(-not $plot.Contains('public FarmActionFeedback ActionFeedback')){
 $plot=ReplaceExact $plot "        private FarmWeatherSystem weatherSystem;" "        private FarmWeatherSystem weatherSystem;`r`n        private FarmActionFeedback actionFeedback;" 'field'
 $plot=ReplaceExact $plot "        public FarmWeatherSystem WeatherSystem => weatherSystem;" "        public FarmWeatherSystem WeatherSystem => weatherSystem;`r`n        public FarmActionFeedback ActionFeedback => actionFeedback;" 'property'
 $plot=ReplaceExact $plot @'
            weatherSystem.Initialize(this, gameState, dayClock, player);
            RebuildWorldPickups();
'@ @'
            weatherSystem.Initialize(this, gameState, dayClock, player);
            actionFeedback = GetComponent<FarmActionFeedback>();
            if (actionFeedback == null) actionFeedback = gameObject.AddComponent<FarmActionFeedback>();
            actionFeedback.Initialize();
            RebuildWorldPickups();
'@ 'initialize'
 $plot=ReplaceExact $plot @'
                feedback = $"{ToolName(activeTool)} melhorada para L{newLevel}: {ToolAreaText(activeTool, newLevel)} por ${cost}.";
                RefreshHoveredArea();
'@ @'
                feedback = $"{ToolName(activeTool)} melhorada para L{newLevel}: {ToolAreaText(activeTool, newLevel)} por ${cost}.";
                actionFeedback?.PlayReward(sellStation.transform.position, newLevel);
                RefreshHoveredArea();
'@ 'upgrade reward'
 $plot=ReplaceExact $plot @'
                SaveGame(false);
            }
            else feedback = error;
        }

        private void RefreshDailyOrdersCache()
'@ @'
                actionFeedback?.PlayReward(orderBoardStation.transform.position, bonus > 0 ? 3 : 1);
                SaveGame(false);
            }
            else feedback = error;
        }

        private void RefreshDailyOrdersCache()
'@ 'order reward'
 $plot=ReplaceExact $plot @'
            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(activeTool, changed);
'@ @'
            var wasExhausted = gameState.IsExhausted;
            gameState.SpendEnergy(activeTool, changed);
            actionFeedback?.PlayTool(activeTool, target.transform.position, changed);
'@ 'tool feedback'
 $payload=@{filePath='Assets/_Project/Scripts/Farming/FarmTestPlot.cs';content=$plot;requestId='farm-action-feedback-integrate'}|ConvertTo-Json -Compress
 $payload|& $cli run-tool script-update-or-create $project --input-file -
 if($LASTEXITCODE-ne 0){throw 'Falha ao integrar feedback'}
}
Write-Output 'Action feedback integrated.'
