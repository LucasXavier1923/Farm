$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-Script([string]$path, [string]$tag) {
    $input = Join-Path $project "Temp\read-$tag-repair-v22.json"
    [System.IO.File]::WriteAllText($input, (@{ filePath = $path; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $lines = @(& $cli run-tool script-read $project --input-file $input 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Leitura falhou: $path" }
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '{') { $start = $i; break } }
    if ($start -lt 0) { throw 'Resposta JSON ausente' }
    return [string]((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result)
}

function Submit-Script([string]$path, [string]$content, [string]$tag) {
    $input = Join-Path $project "Temp\submit-$tag-repair-v22.json"
    [System.IO.File]::WriteAllText($input, (@{ filePath = $path; content = $content } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $input 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Envio falhou: $path`n$($output -join "`n")" }
    $output | Select-Object -Last 12
}

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$state = Read-Script $statePath 'state'
$oldStorageQuantity = @'
        public int GetStorageQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in storage)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }
'@
$newStorageQuantity = @'
        public int GetStorageQuantity(string itemId)
        {
            var total = 0;
            foreach (var stack in storage)
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase)) total += stack.Quantity;
            return total;
        }

        public int GetStorageQuantity(string itemId, FarmItemQuality quality) =>
            GetQuantityInList(storage, itemId, quality);
'@
if (-not $state.Contains($oldStorageQuantity)) { throw 'Metodo GetStorageQuantity esperado nao encontrado' }
$state = $state.Replace($oldStorageQuantity, $newStorageQuantity)
Submit-Script $statePath $state 'FarmGameState'

$storagePath = 'Assets/_Project/Scripts/Farming/FarmStorageUiInteractions.cs'
$storage = Read-Script $storagePath 'storage-view'
$storage = $storage.Replace('        private string itemId;', "        private string itemId;`n        private FarmItemQuality quality;")
$oldInit = @'
        public void Initialize(FarmHudController owner, bool sourceIsBackpack, string id)
        {
            hud = owner;
            fromBackpack = sourceIsBackpack;
            itemId = id;
        }
'@
$newInit = @'
        public void Initialize(FarmHudController owner, bool sourceIsBackpack, string id, FarmItemQuality itemQuality = FarmItemQuality.Normal)
        {
            hud = owner;
            fromBackpack = sourceIsBackpack;
            itemId = id;
            quality = FarmItemQualityRules.Clamp(itemQuality);
        }
'@
if (-not $storage.Contains($oldInit)) { throw 'Initialize do deposito nao encontrado' }
$storage = $storage.Replace($oldInit, $newInit)
$storage = $storage.Replace('hud.TransferHalf(itemId, fromBackpack)', 'hud.TransferHalf(itemId, quality, fromBackpack)')
$storage = $storage.Replace('hud?.ShowItemTooltip(itemId, eventData.position, fromBackpack ? "MOCHILA" : "DEPÓSITO")', 'hud?.ShowItemTooltip(itemId, quality, eventData.position, fromBackpack ? "MOCHILA" : "DEPÓSITO")')
$storage = $storage.Replace('hud.TransferToStorage(itemId, amount)', 'hud.TransferToStorage(itemId, quality, amount)')
$storage = $storage.Replace('hud.TransferFromStorage(itemId, amount)', 'hud.TransferFromStorage(itemId, quality, amount)')
Submit-Script $storagePath $storage 'FarmStorageUiInteractions'
