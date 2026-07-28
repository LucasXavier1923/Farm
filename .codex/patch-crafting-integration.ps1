$ErrorActionPreference = 'Stop'
function Replace-Required([string]$content,[string]$old,[string]$new,[string]$label) {
    if (-not $content.Contains($old)) { throw "Trecho ausente: $label" }
    $content.Replace($old,$new)
}
$root=(Get-Location).Path

$game=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmGameState.cs'))
$game=Replace-Required $game @'
        public int GetMasteryExperience(FarmMasterySkill skill)
'@ @'
        public bool TryCraft(CraftingRecipe recipe, out string error)
        {
            error = string.Empty;
            if (recipe == null || recipe.OutputItem == null || recipe.OutputQuantity <= 0 || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                error = "Receita inv\u00E1lida.";
                return false;
            }
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null || ingredient.Quantity <= 0)
                {
                    error = "A receita possui um ingrediente inv\u00E1lido.";
                    return false;
                }
                var owned = GetQuantity(ingredient.Item.Id);
                if (owned < ingredient.Quantity)
                {
                    error = $"Faltam {ingredient.Quantity - owned} {ingredient.Item.DisplayName.ToLowerInvariant()}.";
                    return false;
                }
            }

            foreach (var ingredient in recipe.Ingredients)
                RemoveFromList(inventory, ingredient.Item.Id, ingredient.Quantity);
            if (!CanAdd(recipe.OutputItem.Id, recipe.OutputQuantity))
            {
                foreach (var ingredient in recipe.Ingredients)
                    AddInternal(ingredient.Item.Id, ingredient.Quantity);
                error = "Mochila sem espa\u00E7o para o item fabricado.";
                return false;
            }
            AddInternal(recipe.OutputItem.Id, recipe.OutputQuantity);
            NotifyChanged();
            return true;
        }

        public int GetMasteryExperience(FarmMasterySkill skill)
'@ 'craft transaction'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmGameState.crafting.cs.txt'),$game,[Text.UTF8Encoding]::new($false))

$hud=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmHudController.cs'))
$hud=Replace-Required $hud @'
        private bool masteryOpen;
        private Text dailyOrdersSummaryText;
'@ @'
        private bool masteryOpen;
        private bool craftingOpen;
        private Text dailyOrdersSummaryText;
'@ 'crafting modal field'
$hud=Replace-Required $hud @'
        public bool IsMasteryOpen => masteryOpen;
        public float DayTransitionAlpha
'@ @'
        public bool IsMasteryOpen => masteryOpen;
        public bool IsCraftingOpen => craftingOpen;
        public float DayTransitionAlpha
'@ 'crafting modal property'
$hud=Replace-Required $hud @'
        public void ShowInventoryFullToast()
        {
            ShowToast("Mochila cheia \u2014 use o dep\u00F3sito para liberar espa\u00E7o.", true);
        }

        private void ShowToast
'@ @'
        public void ShowInventoryFullToast()
        {
            ShowToast("Mochila cheia \u2014 use o dep\u00F3sito para liberar espa\u00E7o.", true);
        }

        public void ShowSystemToast(string message, bool warning = false) => ShowToast(message, warning);

        private void ShowToast
'@ 'public toast'
$hud=Replace-Required $hud @'
        public void SetMasteryOpen(bool value)
        {
            masteryOpen = value;
            UpdateModalState();
        }

        public void CompleteDailyOrder
'@ @'
        public void SetMasteryOpen(bool value)
        {
            masteryOpen = value;
            UpdateModalState();
        }

        public void SetCraftingOpen(bool value)
        {
            craftingOpen = value;
            UpdateModalState();
        }

        public void CompleteDailyOrder
'@ 'crafting setter'
$hud=Replace-Required $hud 'IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen;' 'IsModalOpen = inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen || masteryOpen || craftingOpen;' 'crafting modal state'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmHudController.crafting.cs.txt'),$hud,[Text.UTF8Encoding]::new($false))

$plot=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'))
$plot=Replace-Required $plot @'
        private FarmActionFeedback actionFeedback;
        private FarmTestTile hoveredTile;
'@ @'
        private FarmActionFeedback actionFeedback;
        private FarmCraftingSystem craftingSystem;
        private FarmTestTile hoveredTile;
'@ 'crafting system field'
$plot=Replace-Required $plot @'
        public FarmActionFeedback ActionFeedback => actionFeedback;
        public CropDefinition ActiveCrop
'@ @'
        public FarmActionFeedback ActionFeedback => actionFeedback;
        public FarmCraftingSystem CraftingSystem => craftingSystem;
        public CropDefinition ActiveCrop
'@ 'crafting property'
$plot=Replace-Required $plot @'
            masteryMenu.Initialize(hud, gameState);
            dayClock = GetComponent<FarmDayClock>();
'@ @'
            masteryMenu.Initialize(hud, gameState);
            craftingSystem = GetComponent<FarmCraftingSystem>();
            if (craftingSystem == null) craftingSystem = gameObject.AddComponent<FarmCraftingSystem>();
            craftingSystem.Initialize(this, gameState, hud, player);
            dayClock = GetComponent<FarmDayClock>();
'@ 'crafting bootstrap'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmTestPlot.crafting.cs.txt'),$plot,[Text.UTF8Encoding]::new($false))

$settings=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmSettings.cs'))
$settings=Replace-Required $settings 'hud.IsMasteryOpen)) SetOpen(false);' 'hud.IsMasteryOpen || hud.IsCraftingOpen)) SetOpen(false);' 'settings crafting exclusivity'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmSettings.crafting.cs.txt'),$settings,[Text.UTF8Encoding]::new($false))

$mastery=[IO.File]::ReadAllText((Join-Path $root 'Assets\_Project\Scripts\Farming\FarmMastery.cs'))
$mastery=Replace-Required $mastery 'hud.IsDailyOrdersOpen || hud.IsSettingsOpen)' 'hud.IsDailyOrdersOpen || hud.IsSettingsOpen || hud.IsCraftingOpen)' 'mastery crafting exclusivity'
[IO.File]::WriteAllText((Join-Path $root 'Temp\FarmMastery.crafting.cs.txt'),$mastery,[Text.UTF8Encoding]::new($false))

Write-Output 'Crafting integration staging files created.'
