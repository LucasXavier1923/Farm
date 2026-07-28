$ErrorActionPreference='Stop';$project='D:\Dev\Unity\Farm\Farm';$cli='C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function ReplaceExact([string]$c,[string]$o,[string]$n,[string]$l){if(-not $c.Contains($o)){throw "Trecho nao encontrado: $l"};$c.Replace($o,$n)}
function Submit([string]$p,[string]$c,[string]$id){$payload=@{filePath=$p;content=$c;requestId=$id}|ConvertTo-Json -Compress;$result=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $p"};$result|Select-Object -Last 12;Start-Sleep -Seconds 3}

$hudPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs';$hud=[IO.File]::ReadAllText($hudPath);$nl=if($hud.Contains("`r`n")){"`r`n"}else{"`n"}
if(-not $hud.Contains('IsSettingsOpen')){
 $hud=ReplaceExact $hud '        private bool dailyOrdersOpen;' ('        private bool dailyOrdersOpen;'+$nl+'        private bool settingsOpen;') 'hud field'
 $hud=ReplaceExact $hud '        public bool IsDailyOrdersOpen => dailyOrdersOpen;' ('        public bool IsDailyOrdersOpen => dailyOrdersOpen;'+$nl+'        public bool IsSettingsOpen => settingsOpen;') 'hud property'
 $needle='        public void CompleteDailyOrder(int index) => plot?.TryCompleteDailyOrder(index);'
 $insert=('        public void SetSettingsOpen(bool value)'+$nl+'        {'+$nl+'            settingsOpen = value;'+$nl+'            UpdateModalState();'+$nl+'        }'+$nl+$nl+$needle)
 $hud=ReplaceExact $hud $needle $insert 'hud method'
 $hud=$hud.Replace('inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen;','inventoryOpen || storageOpen || journalOpen || sleepConfirmationOpen || dailyOrdersOpen || settingsOpen;')
 Submit 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'farm-hud-settings-modal'
}

$cameraPath=Join-Path $project 'Assets\_Project\Scripts\Player\FarmThirdPersonCamera.cs';$camera=[IO.File]::ReadAllText($cameraPath);$nl=if($camera.Contains("`r`n")){"`r`n"}else{"`n"}
if(-not $camera.Contains('ConfiguredSensitivity')){
 $camera=$camera.Replace('        [SerializeField] private float zoomStep = 2f;'+$nl,'').Replace('        [SerializeField] private float sensitivity = 0.12f;'+$nl,'')
 $camera=ReplaceExact $camera '        private float pitch = 55f;' ('        private float pitch = 55f;'+$nl+$nl+'        public float ConfiguredSensitivity => FarmSettings.CameraSensitivity;'+$nl+'        public float ConfiguredZoomStep => FarmSettings.ZoomStep;'+$nl+'        public bool InvertVertical => FarmSettings.InvertVertical;'+$nl+'        public float CurrentDistance => distance;') 'camera properties'
 $startOld=('        private void Start()'+$nl+'        {'+$nl+'            FindTarget();')
 $startNew=('        private void Start()'+$nl+'        {'+$nl+'            FarmSettings.EnsureLoaded();'+$nl+'            FindTarget();')
 $camera=ReplaceExact $camera $startOld $startNew 'camera load'
 $rotationOld=('                    yaw += delta.x * sensitivity;'+$nl+'                    pitch -= delta.y * sensitivity;')
 $rotationNew=('                    yaw += delta.x * FarmSettings.CameraSensitivity;'+$nl+'                    pitch += delta.y * FarmSettings.CameraSensitivity * (FarmSettings.InvertVertical ? 1f : -1f);')
 $camera=ReplaceExact $camera $rotationOld $rotationNew 'camera rotation'
 $camera=$camera.Replace('Mathf.Sign(scroll) * zoomStep','Mathf.Sign(scroll) * FarmSettings.ZoomStep')
 Submit 'Assets/_Project/Scripts/Player/FarmThirdPersonCamera.cs' $camera 'farm-camera-settings'
}

$plotPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs';$plot=[IO.File]::ReadAllText($plotPath);$nl=if($plot.Contains("`r`n")){"`r`n"}else{"`n"}
if(-not $plot.Contains('FarmSettingsMenu')){
 $old=('            hud.Initialize(this);'+$nl+'            dayClock = GetComponent<FarmDayClock>();')
 $new=('            hud.Initialize(this);'+$nl+'            var settingsMenu = GetComponent<FarmSettingsMenu>();'+$nl+'            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<FarmSettingsMenu>();'+$nl+'            settingsMenu.Initialize(hud);'+$nl+'            dayClock = GetComponent<FarmDayClock>();')
 $plot=ReplaceExact $plot $old $new 'settings bootstrap'
 Submit 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'farm-settings-bootstrap'
}
Write-Output 'Settings system integrated.'
