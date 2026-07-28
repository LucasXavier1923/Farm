$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'
$checks = @(
    'Temp/clean-v15-state.json',
    '.codex/validate-portuguese-encoding-v13.json',
    'Temp/validate-foundation-v15.json',
    'Temp/validate-mastery-v15.json',
    '.codex/validate-f-interactions-cozy.json',
    '.codex/validate-crafting-runtime.json',
    'Temp/validate-building-v15.json',
    'Temp/validate-sprinkler-v15.json',
    'Temp/validate-pests-v15.json',
    'Temp/validate-fence-snap-v15.json',
    'Temp/validate-building-catalog-v16.json',
    'Temp/validate-building-move-grid-v16.json',
    'Temp/validate-mailbox-v17.json',
    'Temp/validate-inventory-tooltip-split-v18.json',
    'Temp/validate-collection-v19.json'
)

foreach ($check in $checks) {
    Write-Output "VALIDANDO $check"
    $output = & $cli run-tool script-execute $project --input-file $check 2>&1
    if ($LASTEXITCODE -ne 0) {
        $output | Select-Object -Last 30
        throw "Regressao falhou em $check"
    }
    $output | Select-Object -Last 8
}

Write-Output 'REGRESSION_V19_OK'
