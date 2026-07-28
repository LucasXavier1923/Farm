$ErrorActionPreference = 'Stop'

$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
$project = 'D:\Dev\Unity\Farm\Farm'

function Read-UnityScript([string]$assetPath, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\read-$tag-v21.json")
    $input = @{ filePath = $assetPath; lineFrom = 1; lineTo = -1 } | ConvertTo-Json -Compress
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

function Submit-UnityScript([string]$assetPath, [string]$content, [string]$tag) {
    $inputPath = Join-Path $project ("Temp\submit-$tag-v21.json")
    $payload = @{ filePath = $assetPath; content = $content } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($inputPath, $payload, [System.Text.UTF8Encoding]::new($false))
    $output = @(& $cli run-tool script-update-or-create $project --input-file $inputPath 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Falha enviando $assetPath`n$($output -join "`n")" }
    $output | Select-Object -Last 10
}

$statePath = 'Assets/_Project/Scripts/Farming/FarmGameState.cs'
$plotPath = 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs'
$state = Read-UnityScript $statePath 'state-full'
$plot = Read-UnityScript $plotPath 'plot-full'

$marker = '    public static class FarmSaveSystem'
$markerIndex = $state.IndexOf($marker, [System.StringComparison]::Ordinal)
if ($markerIndex -lt 0) { throw 'Classe FarmSaveSystem nao encontrada' }
$statePrefix = $state.Substring(0, $markerIndex)
$saveSystem = @'
    public static class FarmSaveSystem
    {
        private const string SaveFileName = "farm-prototype-save.json";
        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupPath => SavePath + ".bak";
        public static bool LastLoadUsedBackup { get; private set; }

        public static bool Save(FarmSaveData data, out string error) =>
            SaveToPath(data, SavePath, BackupPath, out error);

        public static bool SaveToPath(FarmSaveData data, string path, string backupPath, out string error)
        {
            error = null;
            var tempPath = string.IsNullOrWhiteSpace(path) ? null : path + ".tmp";
            try
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("O caminho do save está vazio.", nameof(path));
                if (string.IsNullOrWhiteSpace(backupPath)) backupPath = path + ".bak";
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

                var json = JsonUtility.ToJson(data, true);
                if (string.IsNullOrWhiteSpace(json) || JsonUtility.FromJson<FarmSaveData>(json) == null)
                    throw new InvalidDataException("Os dados serializados do save são inválidos.");

                var bytes = new System.Text.UTF8Encoding(false).GetBytes(json);
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tempPath, path, backupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceWithPortableFallback(tempPath, path, backupPath);
                    }
                    catch (IOException)
                    {
                        ReplaceWithPortableFallback(tempPath, path, backupPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                TryDelete(tempPath);
                return false;
            }
        }

        public static bool TryLoad(out FarmSaveData data, out string error) =>
            TryLoadFromPaths(SavePath, BackupPath, out data, out error);

        public static bool TryLoadFromPaths(string path, string backupPath, out FarmSaveData data, out string error)
        {
            LastLoadUsedBackup = false;
            error = null;
            if (TryRead(path, out data, out var primaryError)) return true;
            if (TryRead(backupPath, out data, out var backupError))
            {
                LastLoadUsedBackup = true;
                error = null;
                return true;
            }

            data = null;
            if (string.IsNullOrEmpty(primaryError) && string.IsNullOrEmpty(backupError))
            {
                error = null;
                return false;
            }
            error = string.IsNullOrEmpty(backupError)
                ? primaryError
                : $"Save principal: {primaryError} Backup: {backupError}";
            return false;
        }

        private static bool TryRead(string path, out FarmSaveData data, out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidDataException("O arquivo está vazio.");
                data = JsonUtility.FromJson<FarmSaveData>(json);
                if (data == null) throw new InvalidDataException("O arquivo é inválido.");
                return true;
            }
            catch (Exception exception)
            {
                data = null;
                error = exception.Message;
                return false;
            }
        }

        private static void ReplaceWithPortableFallback(string tempPath, string path, string backupPath)
        {
            File.Copy(path, backupPath, true);
            File.Delete(path);
            File.Move(tempPath, path);
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Uma sobra temporária não deve esconder o erro original de salvamento.
            }
        }
    }
}
'@
$state = $statePrefix + $saveSystem

$oldLoadStatus = @'
            saveQueued = false;
            saveStatus = data.Version < 16 ? "Save migrado para v16" : "Save carregado";
            if (showFeedback) feedback = "Jogo carregado.";
'@
$newLoadStatus = @'
            saveQueued = false;
            if (FarmSaveSystem.LastLoadUsedBackup)
            {
                saveStatus = "Save recuperado do backup";
                if (showFeedback) feedback = "Save principal danificado. Backup recuperado.";
                Debug.Log("Save da fazenda recuperado pelo backup automático.");
            }
            else
            {
                saveStatus = data.Version < 17 ? "Save migrado para v17" : "Save carregado";
                if (showFeedback) feedback = "Jogo carregado.";
            }
'@
if (-not $plot.Contains($oldLoadStatus)) { throw 'Status de load esperado nao encontrado' }
$plot = $plot.Replace($oldLoadStatus, $newLoadStatus)

Submit-UnityScript $statePath $state 'FarmGameState'
Submit-UnityScript $plotPath $plot 'FarmTestPlot'
