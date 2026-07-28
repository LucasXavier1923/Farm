$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\read-$tag-v23.json")
    [System.IO.File]::WriteAllText($inputPath, (@{ filePath = $assetPath; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $lines = @(& $cli run-tool script-read $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha lendo $assetPath`n$($lines -join "`n")" }
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '{') { $start = $i; break } }
    if ($start -lt 0) { throw "JSON ausente na leitura de $assetPath" }
    return [string](((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result))
}

function Submit-UnityScript([string]$assetPath, [string]$content, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\submit-$tag-v23.json")
    [System.IO.File]::WriteAllText($inputPath, (@{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 12
}

function Replace-Checked([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old)) { throw "Trecho ausente: $label" }
    return $text.Replace($old, $new)
}

$marketPath = 'Assets/_Project/Scripts/Farming/FarmMarketRules.cs'
$market = @'
using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmMarketTrend
    {
        Weak,
        Stable,
        Strong,
        Peak
    }

    public readonly struct FarmMarketQuote
    {
        public FarmMarketQuote(FarmMarketTrend trend, float multiplier)
        {
            Trend = trend;
            Multiplier = multiplier;
        }

        public FarmMarketTrend Trend { get; }
        public float Multiplier { get; }
        public string Label => Trend switch
        {
            FarmMarketTrend.Weak => "BAIXA",
            FarmMarketTrend.Strong => "ALTA",
            FarmMarketTrend.Peak => "PICO",
            _ => "EST\u00C1VEL"
        };
        public string Indicator => Trend switch
        {
            FarmMarketTrend.Weak => "\u2193",
            FarmMarketTrend.Strong => "\u2191",
            FarmMarketTrend.Peak => "\u2605",
            _ => "="
        };
        public string CompactText => $"{Indicator} {Label} x{Multiplier:0.00}";
    }

    public static class FarmMarketRules
    {
        public static FarmMarketQuote Quote(int worldSeed, int day, string itemId)
        {
            var bucket = StableHash(worldSeed, Mathf.Max(1, day), itemId) % 100u;
            if (bucket < 20u) return new FarmMarketQuote(FarmMarketTrend.Weak, 0.85f);
            if (bucket < 60u) return new FarmMarketQuote(FarmMarketTrend.Stable, 1f);
            if (bucket < 90u) return new FarmMarketQuote(FarmMarketTrend.Strong, 1.20f);
            return new FarmMarketQuote(FarmMarketTrend.Peak, 1.45f);
        }

        public static int UnitPrice(ItemDefinition definition, FarmItemQuality quality, int worldSeed, int day)
        {
            if (definition == null) return 0;
            var quote = Quote(worldSeed, day, definition.Id);
            return Mathf.CeilToInt(
                Mathf.Max(0, definition.BaseSellPrice) *
                FarmItemQualityRules.SellMultiplier(quality) *
                quote.Multiplier);
        }

        private static uint StableHash(int worldSeed, int day, string itemId)
        {
            unchecked
            {
                var hash = 2166136261u;
                Mix(ref hash, (uint)worldSeed);
                Mix(ref hash, (uint)day);
                var normalized = itemId ?? string.Empty;
                for (var index = 0; index < normalized.Length; index++)
                    Mix(ref hash, char.ToUpperInvariant(normalized[index]));
                return hash;
            }
        }

        private static void Mix(ref uint hash, uint value)
        {
            hash ^= value;
            hash *= 16777619u;
        }
    }
}
'@
Submit-UnityScript $marketPath $market 'FarmMarketRules'

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$state = Read-UnityScript $statePath 'market-state'
$state = Replace-Checked $state @'
        public int Pumpkins => GetQuantity(PumpkinId);
        public event Action Changed;
'@ @'
        public int Pumpkins => GetQuantity(PumpkinId);
        public FarmMarketQuote GetMarketQuote(string itemId, int dayOffset = 0) =>
            FarmMarketRules.Quote(worldSeed, Mathf.Max(1, dayNumber + dayOffset), itemId);
        public int GetMarketUnitPrice(string itemId, FarmItemQuality quality, int dayOffset = 0) =>
            FarmMarketRules.UnitPrice(
                FarmContentDatabase.GetItem(itemId),
                quality,
                worldSeed,
                Mathf.Max(1, dayNumber + dayOffset));
        public event Action Changed;
'@ 'consultas de mercado no estado'
$state = Replace-Checked $state @'
        private static int SaleValueInList(List<InventoryStack> stacks, ItemDefinition definition)
        {
            var total = 0;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, definition.Id, StringComparison.OrdinalIgnoreCase))
                    total += stack.Quantity * FarmItemQualityRules.UnitSellPrice(definition, stack.Quality);
            return total;
        }
'@ @'
        private int SaleValueInList(List<InventoryStack> stacks, ItemDefinition definition)
        {
            var total = 0;
            foreach (var stack in stacks)
                if (string.Equals(stack.ItemId, definition.Id, StringComparison.OrdinalIgnoreCase))
                    total += stack.Quantity * FarmMarketRules.UnitPrice(
                        definition, stack.Quality, worldSeed, dayNumber);
            return total;
        }
'@ 'venda pela cotacao do dia'
Submit-UnityScript $statePath $state 'FarmGameState'

$hudPath = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$hud = Read-UnityScript $hudPath 'market-hud'
$hud = Replace-Checked $hud @'
                shopInfoText.text = $"{shopCrop.DisplayName.ToUpperInvariant()}  ({plot.ShopCropIndex + 1}/{plot.ShopCropCount})\nAfinidade: {shopCrop.AffinityText}\n{shopCrop.SeedPackAmount} sementes: ${shopCrop.SeedPackPrice}  \u2022  Venda: ${shopCrop.HarvestItem.BaseSellPrice} cada";
'@ @'
                var todayQuote = state.GetMarketQuote(shopCrop.HarvestItem.Id);
                var tomorrowQuote = state.GetMarketQuote(shopCrop.HarvestItem.Id, 1);
                var normalPrice = state.GetMarketUnitPrice(shopCrop.HarvestItem.Id, FarmItemQuality.Normal);
                var silverPrice = state.GetMarketUnitPrice(shopCrop.HarvestItem.Id, FarmItemQuality.Silver);
                var goldPrice = state.GetMarketUnitPrice(shopCrop.HarvestItem.Id, FarmItemQuality.Gold);
                shopInfoText.text =
                    $"{shopCrop.DisplayName.ToUpperInvariant()}  ({plot.ShopCropIndex + 1}/{plot.ShopCropCount})\n" +
                    $"Afinidade: {shopCrop.AffinityText}\n" +
                    $"Mercado hoje: {todayQuote.CompactText}  \u2022  Amanh\u00E3: {tomorrowQuote.CompactText}\n" +
                    $"Venda N/P/O: ${normalPrice} / ${silverPrice} / ${goldPrice}  \u2022  {shopCrop.SeedPackAmount} sementes: ${shopCrop.SeedPackPrice}";
'@ 'painel com cotacao e previsao'
$hud = Replace-Checked $hud @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(500f, 380f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("ShopTitle", shopPanel.transform, "CAIXOTE DE COM\u00C9RCIO", 23, FontStyle.Bold, AccentColor, new Vector2(20f, -16f), new Vector2(460f, 30f), TextAnchor.MiddleCenter);
            previousCropButton = CreateButton("PreviousCrop", shopPanel.transform, "<", new Vector2(20f, -67f), new Vector2(52f, 54f));
            shopInfoText = CreateText("ShopInfo", shopPanel.transform, "", 17, FontStyle.Normal, Color.white, new Vector2(80f, -56f), new Vector2(340f, 78f), TextAnchor.MiddleCenter);
            nextCropButton = CreateButton("NextCrop", shopPanel.transform, ">", new Vector2(428f, -67f), new Vector2(52f, 54f));
            sellButton = CreateButton("Sell", shopPanel.transform, "VENDER TODOS", new Vector2(20f, -174f), new Vector2(220f, 52f));
            buyButton = CreateButton("Buy", shopPanel.transform, "COMPRAR SEMENTES", new Vector2(260f, -174f), new Vector2(220f, 52f));
'@ @'
            shopPanel = CreatePanel("Shop", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(620f, 450f), new Vector2(0.5f, 0.5f), PanelColor);
            CreateText("ShopTitle", shopPanel.transform, "CAIXOTE DE COM\u00C9RCIO", 23, FontStyle.Bold, AccentColor, new Vector2(20f, -16f), new Vector2(580f, 30f), TextAnchor.MiddleCenter);
            previousCropButton = CreateButton("PreviousCrop", shopPanel.transform, "<", new Vector2(20f, -86f), new Vector2(52f, 54f));
            shopInfoText = CreateText("ShopInfo", shopPanel.transform, "", 16, FontStyle.Normal, Color.white, new Vector2(80f, -55f), new Vector2(460f, 122f), TextAnchor.MiddleCenter);
            nextCropButton = CreateButton("NextCrop", shopPanel.transform, ">", new Vector2(548f, -86f), new Vector2(52f, 54f));
            sellButton = CreateButton("Sell", shopPanel.transform, "VENDER TODOS", new Vector2(20f, -205f), new Vector2(280f, 52f));
            buyButton = CreateButton("Buy", shopPanel.transform, "COMPRAR SEMENTES", new Vector2(320f, -205f), new Vector2(280f, 52f));
'@ 'layout ampliado do comercio'
$hud = Replace-Checked $hud @'
            upgradeToolButton = CreateButton("UpgradeTool", shopPanel.transform, "MELHORAR FERRAMENTA", new Vector2(20f, -242f), new Vector2(460f, 52f));
'@ @'
            upgradeToolButton = CreateButton("UpgradeTool", shopPanel.transform, "MELHORAR FERRAMENTA", new Vector2(20f, -273f), new Vector2(580f, 52f));
'@ 'botao de melhoria reposicionado'
$hud = Replace-Checked $hud @'
            var closeShopButton = CreateButton("CloseShop", shopPanel.transform, "FECHAR  [ESC]", new Vector2(140f, -310f), new Vector2(220f, 46f));
'@ @'
            var closeShopButton = CreateButton("CloseShop", shopPanel.transform, "FECHAR  [ESC]", new Vector2(200f, -351f), new Vector2(220f, 46f));
'@ 'fechar reposicionado'
$hud = Replace-Checked $hud @'
            var unitValue = definition.Category == ItemCategory.Crop
                ? FarmItemQualityRules.UnitSellPrice(definition, quality)
                : definition.BaseSellPrice;
            var value = unitValue > 0 ? $"Valor unit\u00E1rio: ${unitValue}" : "Sem venda direta";
'@ @'
            var qualityBaseValue = definition.Category == ItemCategory.Crop
                ? FarmItemQualityRules.UnitSellPrice(definition, quality)
                : definition.BaseSellPrice;
            var unitValue = definition.Category == ItemCategory.Crop
                ? plot.GameState.GetMarketUnitPrice(itemId, quality)
                : qualityBaseValue;
            var value = unitValue > 0
                ? definition.Category == ItemCategory.Crop
                    ? $"Hoje: ${unitValue}  \u2022  Base da qualidade: ${qualityBaseValue}"
                    : $"Valor unit\u00E1rio: ${unitValue}"
                : "Sem venda direta";
'@ 'tooltip com preco de mercado'
Submit-UnityScript $hudPath $hud 'FarmHudController'

$plotPath = 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs'
$plot = Read-UnityScript $plotPath 'market-plot'
$plot = Replace-Checked $plot '                    feedback = $"Vendeu {quantity} produto(s) por ${earned}.";' '                    feedback = $"Vendeu {quantity} produto(s) por ${earned} nas cota\u00E7\u00F5es de hoje.";' 'feedback da venda'
Submit-UnityScript $plotPath $plot 'FarmTestPlot'
