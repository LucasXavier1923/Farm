$ErrorActionPreference = 'Stop'

$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$relativeFarmCrafting = 'Assets/_Project/Scripts/Farming/FarmCrafting.cs'
$relativeCraftingRecipe = 'Assets/_Project/Scripts/Farming/CraftingRecipe.cs'
$sourcePath = Join-Path $project ($relativeFarmCrafting -replace '/', '\')
$source = [IO.File]::ReadAllText($sourcePath)
$definitionStart = $source.IndexOf('    [Serializable]')
$stationStart = $source.IndexOf('    public sealed class FarmCraftingStation')

if ($definitionStart -lt 0 -or $stationStart -le $definitionStart) {
    throw 'Nao foi possivel localizar as definicoes de crafting para separar.'
}

$namespaceOpening = $source.Substring(0, $definitionStart)
$definitions = $source.Substring($definitionStart, $stationStart - $definitionStart).TrimEnd()
$remaining = $source.Substring($stationStart)
$updatedFarmCrafting = $namespaceOpening + $remaining

$recipeHeader = @'
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FarmPrototype.Farming
{
'@
$recipeContent = $recipeHeader + "`r`n" + $definitions + "`r`n}`r`n"

function Submit-Script([string]$path, [string]$content, [string]$requestId) {
    $payload = @{
        filePath = $path
        content = $content
        requestId = $requestId
    } | ConvertTo-Json -Compress

    $result = $payload | & $cli run-tool script-update-or-create $project --input-file -
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao atualizar $path"
    }
    $result | Select-Object -Last 16
}

Submit-Script $relativeFarmCrafting $updatedFarmCrafting 'split-crafting-host-v15'
Start-Sleep -Seconds 2
Submit-Script $relativeCraftingRecipe $recipeContent 'split-crafting-definition-v15'
