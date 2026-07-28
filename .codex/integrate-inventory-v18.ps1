$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath) {
    $inputPath = Join-Path $project 'Temp\read-v18-current.json'
    $input = @{
        filePath = $assetPath
        lineFrom = 1
        lineTo = -1
    } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inputPath, $input, [System.Text.UTF8Encoding]::new($false))
    $lines = @(& $cli run-tool script-read $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha lendo $assetPath`n$($lines -join "`n")" }
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '{') { $start = $i; break }
    }
    if ($start -lt 0) { throw "JSON ausente na leitura de $assetPath" }
    $response = (($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json)
    return [string]$response.structured.result
}

function Replace-Checked([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old)) { throw "Trecho ausente: $label" }
    return $text.Replace($old, $new)
}

function Submit-UnityScript([string]$assetPath, [string]$content, [string]$className) {
    $inputPath = Join-Path $project ("Temp\submit-" + $className + "-v18.json")
    $payload = @{
        filePath = $assetPath
        content = $content
    } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inputPath, $payload, [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 12
}

$hudPath = 'Assets/_Project/Scripts/Farming/FarmHudController.cs'
$hud = Read-UnityScript $hudPath

$hud = Replace-Checked $hud @'
        private string draggedItemId;
        private bool inventoryOpen;
'@ @'
        private string draggedItemId;
        private GameObject itemTooltip;
        private CanvasGroup itemTooltipGroup;
        private Text itemTooltipTitle;
        private Text itemTooltipBody;
        private string tooltipItemId;
        private string tooltipContext;
        private bool inventoryOpen;
'@ 'campos do tooltip'

$hud = Replace-Checked $hud @'
        public string CurrentToastText => pickupToastText != null ? pickupToastText.text : string.Empty;
'@ @'
        public string CurrentToastText => pickupToastText != null ? pickupToastText.text : string.Empty;
        public bool IsItemTooltipVisible => itemTooltipGroup != null && itemTooltipGroup.alpha > 0.5f;
        public string TooltipItemId => tooltipItemId ?? string.Empty;
        public string TooltipText => itemTooltipTitle == null || itemTooltipBody == null
            ? string.Empty
            : itemTooltipTitle.text + "\n" + itemTooltipBody.text;
'@ 'propriedades do tooltip'

$hud = Replace-Checked $hud @'
            if (!value) EndItemDrag();
            UpdateModalState();
'@ @'
            if (!value)
            {
                EndItemDrag();
                HideItemTooltip();
            }
            UpdateModalState();
'@ 'fechamento da mochila'

$hud = Replace-Checked $hud @'
            }
            UpdateModalState();
        }

        public void TransferToStorage
'@ @'
            }
            else HideItemTooltip();
            UpdateModalState();
        }

        public void TransferToStorage
'@ 'fechamento do deposito'

$hud = Replace-Checked $hud @'
        public void ToggleJournal() => SetJournalOpen(!journalOpen);
'@ @'
        public int TransferHalf(string itemId, bool fromBackpack)
        {
            if (plot == null || plot.GameState == null || string.IsNullOrWhiteSpace(itemId)) return 0;
            var available = fromBackpack
                ? plot.GameState.GetQuantity(itemId)
                : plot.GameState.GetStorageQuantity(itemId);
            if (available <= 0) return 0;
            var amount = Mathf.Max(1, Mathf.CeilToInt(available * 0.5f));
            var before = fromBackpack
                ? plot.GameState.GetQuantity(itemId)
                : plot.GameState.GetStorageQuantity(itemId);
            if (fromBackpack) TransferToStorage(itemId, amount);
            else TransferFromStorage(itemId, amount);
            var after = fromBackpack
                ? plot.GameState.GetQuantity(itemId)
                : plot.GameState.GetStorageQuantity(itemId);
            return Mathf.Max(0, before - after);
        }

        public void ToggleJournal() => SetJournalOpen(!journalOpen);
'@ 'transferencia de meia pilha'

$hud = Replace-Checked $hud @'
        public void BeginItemDrag(string itemId, Vector2 screenPosition)
'@ @'
        public void ShowItemTooltip(string itemId, Vector2 screenPosition, string context)
        {
            if (itemTooltipGroup == null || plot == null || plot.GameState == null ||
                string.IsNullOrWhiteSpace(itemId)) return;
            var definition = FarmContentDatabase.GetItem(itemId);
            if (definition == null) return;
            tooltipItemId = itemId;
            tooltipContext = context ?? string.Empty;
            var quantity = string.Equals(tooltipContext, "DEP\u00D3SITO", StringComparison.OrdinalIgnoreCase)
                ? plot.GameState.GetStorageQuantity(itemId)
                : plot.GameState.GetQuantity(itemId);
            itemTooltipTitle.text = definition.DisplayName.ToUpperInvariant();
            var category = definition.Category switch
            {
                ItemCategory.Seed => "SEMENTE",
                ItemCategory.Crop => "COLHEITA",
                ItemCategory.Tool => "FERRAMENTA",
                ItemCategory.Material => "MATERIAL",
                _ => "ITEM"
            };
            var use = definition.Category switch
            {
                ItemCategory.Seed => "Plante em um canteiro preparado.",
                ItemCategory.Crop => "Venda, entregue em pedidos ou guarde.",
                ItemCategory.Tool => "Equipe pela barra r\u00E1pida.",
                ItemCategory.Material => "Use em receitas e constru\u00E7\u00F5es.",
                _ => "Item da fazenda."
            };
            var value = definition.BaseSellPrice > 0 ? $"Valor base: ${definition.BaseSellPrice}" : "Sem venda direta";
            itemTooltipBody.text =
                $"{category}  \u2022  {tooltipContext}\n" +
                $"Quantidade: {quantity}  \u2022  Pilha m\u00E1x.: {definition.MaxStack}\n" +
                $"{value}\n{use}";
            SetCanvasGroup(itemTooltipGroup, true);
            itemTooltip.transform.SetAsLastSibling();
            MoveItemTooltip(screenPosition);
        }

        public void MoveItemTooltip(Vector2 screenPosition)
        {
            if (itemTooltip == null || itemTooltipGroup == null || itemTooltipGroup.alpha <= 0f) return;
            var rect = itemTooltip.GetComponent<RectTransform>();
            var x = Mathf.Clamp(screenPosition.x + 20f, 12f, Screen.width - rect.sizeDelta.x - 12f);
            var y = Mathf.Clamp(screenPosition.y - 18f, rect.sizeDelta.y + 12f, Screen.height - 12f);
            rect.position = new Vector2(x, y);
        }

        public void HideItemTooltip()
        {
            tooltipItemId = null;
            tooltipContext = null;
            SetCanvasGroup(itemTooltipGroup, false);
        }

        public void BeginItemDrag(string itemId, Vector2 screenPosition)
'@ 'metodos do tooltip'

$hud = Replace-Checked $hud @'
            dragGhost.SetActive(false);
        }

        private void CreatePickupToast
'@ @'
            dragGhost.SetActive(false);

            itemTooltip = CreatePanel("ItemTooltip", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(340f, 160f), new Vector2(0f, 1f), new Color(0.035f, 0.055f, 0.03f, 0.98f));
            itemTooltipTitle = CreateText("Title", itemTooltip.transform, "", 18, FontStyle.Bold, AccentColor, new Vector2(16f, -12f), new Vector2(308f, 26f));
            itemTooltipBody = CreateText("Body", itemTooltip.transform, "", 14, FontStyle.Normal, Color.white, new Vector2(16f, -45f), new Vector2(308f, 102f));
            itemTooltipGroup = itemTooltip.AddComponent<CanvasGroup>();
            itemTooltipGroup.blocksRaycasts = false;
            SetCanvasGroup(itemTooltipGroup, false);
        }

        private void CreatePickupToast
'@ 'criacao visual do tooltip'

$hud = Replace-Checked $hud @'
            storageFeedbackText.text = "Clique esquerdo transfere a pilha; clique direito transfere uma unidade.";
'@ @'
            storageFeedbackText.text = "Esquerdo: pilha inteira  \u2022  Shift + esquerdo: metade  \u2022  Direito: uma unidade.";
'@ 'instrucao ao abrir deposito'

$hud = Replace-Checked $hud @'
            CreateText("Hint", window.transform, "Esquerdo: pilha inteira  \u2022  Direito: uma unidade  \u2022  I/Tab abre a mochila", 15, FontStyle.Normal, new Color(0.78f, 0.84f, 0.72f), new Vector2(28f, -58f), new Vector2(1000f, 26f));
'@ @'
            CreateText("Hint", window.transform, "Esquerdo: tudo  \u2022  Shift + esquerdo: metade  \u2022  Direito: uma unidade  \u2022  Passe o mouse para detalhes", 15, FontStyle.Normal, new Color(0.78f, 0.84f, 0.72f), new Vector2(28f, -58f), new Vector2(1130f, 26f));
'@ 'hint permanente do deposito'

Submit-UnityScript $hudPath $hud 'FarmHudController'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmInventoryUiInteractions.cs' ([System.IO.File]::ReadAllText((Join-Path $project '.codex\FarmInventoryUiInteractions.v18.cs.txt'))) 'FarmInventoryUiInteractions'
Submit-UnityScript 'Assets/_Project/Scripts/Farming/FarmStorageUiInteractions.cs' ([System.IO.File]::ReadAllText((Join-Path $project '.codex\FarmStorageUiInteractions.v18.cs.txt'))) 'FarmStorageUiInteractions'

Write-Output 'INVENTORY_V18_SUBMITTED'
