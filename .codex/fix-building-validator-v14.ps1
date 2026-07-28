$path = Join-Path $PSScriptRoot 'validate-building-v14.json'
$source = [IO.File]::ReadAllText($path)
$old = 'if(UnityEngine.GameObject.Find(\"OpenBuilding\")==null||UnityEngine.GameObject.Find(\"BuildingInstructions\")==null)'
$new = 'if(hud.GetComponentInChildren<UnityEngine.Canvas>(true).transform.Find(\"OpenBuilding\")==null||hud.GetComponentInChildren<UnityEngine.Canvas>(true).transform.Find(\"BuildingInstructions\")==null)'
if (-not $source.Contains($old)) {
    throw 'Trecho da interface nao encontrado no validador v14.'
}
[IO.File]::WriteAllText($path, $source.Replace($old, $new), [Text.UTF8Encoding]::new($false))
