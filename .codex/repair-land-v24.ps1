$ErrorActionPreference = 'Stop'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$readInput = Join-Path $project 'Temp\read-land-repair-v24.json'
[System.IO.File]::WriteAllText($readInput, (@{ filePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$lines = @(& $cli run-tool script-read $project --input-file $readInput 2>&1)
if ($LASTEXITCODE -ne 0) { throw ($lines -join "`n") }
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Trim() -eq '{') { $start = $i; break }
}
if ($start -lt 0) { throw 'Resposta JSON ausente' }
$state = [string]((($lines[$start..($lines.Count - 1)] -join "`n") | ConvertFrom-Json).structured.result)
$wrong = @'
        public int SpendEnergy(FarmTool tool, int changedTiles)
        {
            landLevel = data.Version >= 19
                ? Mathf.Clamp(data.LandLevel, MinLandLevel, MaxLandLevel)
                : MinLandLevel;
            lastEnergyActionWasFree = false;
'@
$right = @'
        public int SpendEnergy(FarmTool tool, int changedTiles)
        {
            lastEnergyActionWasFree = false;
'@
if (-not $state.Contains($wrong)) { throw 'Insercao incorreta nao encontrada' }
$state = $state.Replace($wrong, $right)
$submitInput = Join-Path $project 'Temp\submit-FarmGameState-land-repair-v24.json'
[System.IO.File]::WriteAllText($submitInput, (@{ filePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'; content = $state } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))
$output = @(& $cli run-tool script-update-or-create $project --input-file $submitInput 2>&1)
if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
$output | Select-Object -Last 14
