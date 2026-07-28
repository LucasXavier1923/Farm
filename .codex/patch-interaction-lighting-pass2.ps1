$ErrorActionPreference = 'Stop'

function Update-UnityScript {
    param(
        [string]$ProjectPath,
        [string]$AssetPath,
        [scriptblock]$Transform,
        [string]$InputName
    )

    $diskPath = Join-Path $ProjectPath ($AssetPath -replace '/', '\')
    $content = [IO.File]::ReadAllText($diskPath)
    $updated = & $Transform $content
    if ($updated -eq $content) {
        throw "Nenhuma alteração aplicada em $AssetPath"
    }

    $payload = @{
        filePath = $AssetPath
        content = $updated
    } | ConvertTo-Json -Depth 5
    $inputDirectory = Join-Path $ProjectPath 'Temp\CodexMcp'
    [IO.Directory]::CreateDirectory($inputDirectory) | Out-Null
    $inputPath = Join-Path $inputDirectory $InputName
    [IO.File]::WriteAllText($inputPath, $payload, [Text.UTF8Encoding]::new($false))
    & 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd' run-tool script-update-or-create $ProjectPath --input-file $inputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao atualizar $AssetPath pelo Unity MCP"
    }
}

$project = 'D:\Dev\Unity\Farm\Farm'

Update-UnityScript $project 'Assets/_Project/Scripts/Farming/FarmCrafting.cs' {
    param($text)
    $text = $text.Replace(
        'if (keyboard != null && keyboard.fKey.wasPressedThisFrame)' + "`r`n" +
        '            {' + "`r`n" +
        '                if (open) SetOpen(false);' + "`r`n" +
        '                else if (near) SetOpen(true);' + "`r`n" +
        '                else hud.ShowSystemToast("Chegue a at\u00E9 1 unidade da bancada.", true);' + "`r`n" +
        '            }',
        'if (!open && keyboard != null && keyboard.fKey.wasPressedThisFrame)' + "`r`n" +
        '            {' + "`r`n" +
        '                if (near) SetOpen(true);' + "`r`n" +
        '                else hud.ShowSystemToast("Aproxime-se da bancada para interagir.", true);' + "`r`n" +
        '            }')
    $text.Replace('FECHAR  [F]', 'FECHAR  [ESC]')
} 'update-crafting-interaction-pass2.json'

Update-UnityScript $project 'Assets/_Project/Scripts/Farming/FarmHudController.cs' {
    param($text)
    $text = $text.Replace(
        'I/Tab invent\u00E1rio  \u2022  J di\u00E1rio  \u2022  K dom\u00EDnio  \u2022  F5 salvar',
        'F interagir  \u2022  I/Tab invent\u00E1rio  \u2022  J di\u00E1rio  \u2022  K dom\u00EDnio  \u2022  F5 salvar')
    $text.Replace('FECHAR  [P]', 'FECHAR  [ESC]')
} 'update-hud-interaction-pass2.json'

Update-UnityScript $project 'Assets/_Project/Scripts/Farming/FarmDayClock.cs' {
    param($text)
    $text = $text.Replace('new Color(0.28f, 0.38f, 0.58f)', 'new Color(0.34f, 0.40f, 0.58f)')
    $text = $text.Replace('new Color(0.28f, 0.30f, 0.40f)', 'new Color(0.34f, 0.36f, 0.46f)')
    $text = $text.Replace('new Color(0.16f, 0.18f, 0.25f)', 'new Color(0.22f, 0.23f, 0.30f)')
    $text = $text.Replace('Mathf.Max(0.72f, originalAmbientIntensity * 0.72f)', 'Mathf.Max(0.80f, originalAmbientIntensity * 0.80f)')
    $text = $text.Replace('Mathf.Lerp(0.34f, 1.05f, CurrentLightFactor)', 'Mathf.Lerp(0.40f, 1.05f, CurrentLightFactor)')
    $text = $text.Replace('return 0.32f;', 'return 0.40f;')
    $text = $text.Replace('Mathf.SmoothStep(0.32f, 0.85f', 'Mathf.SmoothStep(0.40f, 0.85f')
    $text = $text.Replace('Mathf.SmoothStep(1f, 0.32f', 'Mathf.SmoothStep(1f, 0.40f')
    $text
} 'update-dayclock-cozy-pass2.json'
