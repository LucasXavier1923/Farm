$ErrorActionPreference = 'Stop'

$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'

function Replace-Checked([string]$content, [string]$old, [string]$new, [string]$label) {
    $content = $content.Replace("`r`n", "`n")
    $old = $old.Replace("`r`n", "`n")
    $new = $new.Replace("`r`n", "`n")
    if (-not $content.Contains($old)) {
        throw "Trecho ausente: $label"
    }
    return $content.Replace($old, $new)
}

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
    $result | Select-Object -Last 14
    Start-Sleep -Seconds 2
}

$statePath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmGameState.cs'
$state = [IO.File]::ReadAllText($statePath)
$state = Replace-Checked $state @'
        public bool RemovePlacedObject(string persistentId)
'@ @'
        public bool UpdatePlacedObject(FarmPlacedObjectSaveData data)
        {
            if (!IsValidPlacedObject(data)) return false;
            for (var index = 0; index < placedObjects.Count; index++)
            {
                var existing = placedObjects[index];
                if (!string.Equals(existing.PersistentId, data.PersistentId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(existing.ItemId, data.ItemId, StringComparison.OrdinalIgnoreCase)) return false;
                placedObjects[index] = data.Clone();
                NotifyChanged();
                return true;
            }
            return false;
        }

        public bool RemovePlacedObject(string persistentId)
'@ 'atualizacao persistente de construcao'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmGameState.cs' $state 'building-move-state-v16'

$buildingPath = Join-Path $project 'Assets\_Project\Scripts\Farming\FarmBuildingSystem.cs'
$building = [IO.File]::ReadAllText($buildingPath)
$building = Replace-Checked $building @'
        private Material fenceSnapMaterial;
'@ @'
        private Material fenceSnapMaterial;
        private FarmPlacedObject movingObject;
        private FarmPlacedObjectSaveData movingOriginalData;
        private FarmBuildGridVisual gridVisual;
        private bool gridVisible = true;
'@ 'campos de movimento e grade'
$building = Replace-Checked $building @'
        public FarmBuildingCatalog Catalog => catalog;
'@ @'
        public FarmBuildingCatalog Catalog => catalog;
        public bool IsMoving => movingObject != null;
        public bool IsGridVisible => gridVisual != null && gridVisual.IsVisible;
        public int GridLineSegmentCount => gridVisual != null ? gridVisual.LineSegmentCount : 0;
'@ 'propriedades de movimento e grade'
$building = Replace-Checked $building @'
            CreateInterface();
            catalog = GetComponent<FarmBuildingCatalog>();
'@ @'
            var gridObject = new GameObject("FarmBuildGridVisual");
            gridObject.transform.SetParent(owner.transform, true);
            gridVisual = gridObject.AddComponent<FarmBuildGridVisual>();
            gridVisual.Initialize(GridSize);
            CreateInterface();
            catalog = GetComponent<FarmBuildingCatalog>();
'@ 'inicializacao da grade'
$building = Replace-Checked $building @'
                if (!FarmHudController.IsModalOpen && keyboard != null && keyboard.xKey.wasPressedThisFrame)
                    TryReclaimLookTarget();
                return;
'@ @'
                if (!FarmHudController.IsModalOpen && keyboard != null && keyboard.xKey.wasPressedThisFrame)
                    TryReclaimLookTarget();
                if (!FarmHudController.IsModalOpen && keyboard != null && keyboard.mKey.wasPressedThisFrame)
                    TryMoveLookTarget();
                return;
'@ 'atalho para mover'
$building = Replace-Checked $building @'
            UpdatePreviewFromPointer();
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
'@ @'
            UpdatePreviewFromPointer();
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            {
                gridVisible = !gridVisible;
                RefreshGrid();
                RefreshInstruction();
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
'@ 'atalho da grade'
$oldBegin = @'
            CancelPlacement();
            activeDefinition = definition;
            previewRotation = SnapRotation(player != null ? player.eulerAngles.y : 0f, definition.RotationStep);
            preview = Instantiate(definition.Prefab);
            preview.name = $"BuildPreview_{definition.Id}";
            preview.transform.localScale = definition.PlacedScale;
            preview.transform.rotation = Quaternion.Euler(0f, previewRotation, 0f);
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            ClonePreviewMaterials();
            if (activeDefinition.Function == FarmBuildableFunction.Fence) CreateFenceSnapMarker();
            if (activeDefinition.Function == FarmBuildableFunction.Sprinkler)
                preview.AddComponent<FarmSprinklerEmitter>().Initialize(activeDefinition.EffectRadius, true);
            else if (activeDefinition.Function == FarmBuildableFunction.Scarecrow)
                preview.AddComponent<FarmBuildableRadiusIndicator>().Initialize(activeDefinition.EffectRadius, true, new Color(1f, 0.72f, 0.16f, 1f));
            instructionPanel.SetActive(true);
            UpdatePreviewFromPointer();
            return true;
'@
$newBegin = @'
            CancelPlacement();
            return StartPreview(
                definition,
                SnapRotation(player != null ? player.eulerAngles.y : 0f, definition.RotationStep));
'@
$building = Replace-Checked $building $oldBegin $newBegin 'refatoracao do preview'
$building = Replace-Checked $building @'
        public void CancelPlacement()
'@ @'
        public bool BeginMove(FarmPlacedObject placed)
        {
            if (FarmHudController.IsModalOpen || state == null || placed == null ||
                placed.Definition == null || IsPlacing) return false;
            if (player != null && Vector3.Distance(player.position, placed.transform.position) > ReclaimDistance)
            {
                hud?.ShowSystemToast("Chegue mais perto para mover.", true);
                return false;
            }

            FarmPlacedObjectSaveData saved = null;
            foreach (var data in state.PlacedObjects)
                if (string.Equals(data.PersistentId, placed.PersistentId, StringComparison.OrdinalIgnoreCase))
                {
                    saved = data.Clone();
                    break;
                }
            if (saved == null) return false;

            CancelPlacement();
            movingObject = placed;
            movingOriginalData = saved;
            placed.gameObject.SetActive(false);
            if (StartPreview(placed.Definition, placed.transform.eulerAngles.y)) return true;
            CancelPlacement();
            return false;
        }

        private bool StartPreview(FarmBuildableDefinition definition, float initialRotation)
        {
            if (definition == null || definition.Prefab == null) return false;
            activeDefinition = definition;
            previewRotation = SnapRotation(initialRotation, definition.RotationStep);
            preview = Instantiate(definition.Prefab);
            preview.name = $"BuildPreview_{definition.Id}";
            preview.transform.localScale = definition.PlacedScale;
            preview.transform.rotation = Quaternion.Euler(0f, previewRotation, 0f);
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            ClonePreviewMaterials();
            if (activeDefinition.Function == FarmBuildableFunction.Fence) CreateFenceSnapMarker();
            if (activeDefinition.Function == FarmBuildableFunction.Sprinkler)
                preview.AddComponent<FarmSprinklerEmitter>().Initialize(activeDefinition.EffectRadius, true);
            else if (activeDefinition.Function == FarmBuildableFunction.Scarecrow)
                preview.AddComponent<FarmBuildableRadiusIndicator>().Initialize(activeDefinition.EffectRadius, true, new Color(1f, 0.72f, 0.16f, 1f));
            instructionPanel.SetActive(true);
            UpdatePreviewFromPointer();
            RefreshGrid();
            return true;
        }

        public void CancelPlacement()
'@ 'inicio do modo mover'
$building = Replace-Checked $building @'
        public void CancelPlacement()
        {
            if (preview != null) Destroy(preview);
            preview = null;
            activeDefinition = null;
'@ @'
        public void CancelPlacement()
        {
            if (preview != null) Destroy(preview);
            if (movingObject != null) movingObject.gameObject.SetActive(true);
            movingObject = null;
            movingOriginalData = null;
            preview = null;
            activeDefinition = null;
'@ 'rollback do movimento'
$building = Replace-Checked $building @'
            fenceSnapMarker = null;
            fenceSnapMaterial = null;
            if (instructionPanel != null) instructionPanel.SetActive(false);
'@ @'
            fenceSnapMarker = null;
            fenceSnapMaterial = null;
            gridVisual?.SetVisible(false);
            if (instructionPanel != null) instructionPanel.SetActive(false);
'@ 'ocultar grade ao cancelar'
$building = Replace-Checked $building @'
            var definition = activeDefinition;
            var itemId = definition.KitItem.Id;
'@ @'
            if (movingObject != null) return ConfirmMove();

            var definition = activeDefinition;
            var itemId = definition.KitItem.Id;
'@ 'confirmacao de movimento'
$building = Replace-Checked $building @'
        public bool TryPlaceDirectForTesting(
'@ @'
        private bool ConfirmMove()
        {
            var placed = movingObject;
            if (placed == null || movingOriginalData == null) return false;
            var data = new FarmPlacedObjectSaveData
            {
                PersistentId = movingOriginalData.PersistentId,
                ItemId = movingOriginalData.ItemId,
                X = previewPosition.x,
                Y = previewPosition.y,
                Z = previewPosition.z,
                RotationY = previewRotation
            };
            if (!state.UpdatePlacedObject(data))
            {
                hud?.ShowSystemToast("N\u00E3o foi poss\u00EDvel atualizar a constru\u00E7\u00E3o.", true);
                return false;
            }

            movingObject = null;
            movingOriginalData = null;
            placed.transform.SetPositionAndRotation(
                new Vector3(data.X, data.Y, data.Z),
                Quaternion.Euler(0f, data.RotationY, 0f));
            placed.gameObject.SetActive(true);
            var displayName = placed.Definition != null ? placed.Definition.DisplayName : "Constru\u00E7\u00E3o";
            hud?.ShowSystemToast($"{displayName} movido sem gastar kit.", false);
            CancelPlacement();
            return true;
        }

        public bool TryPlaceDirectForTesting(
'@ 'metodo de confirmacao do movimento'
$building = Replace-Checked $building @'
        public bool TryReclaim(FarmPlacedObject placed)
'@ @'
        public bool TryMoveDirectForTesting(
            FarmPlacedObject placed,
            Vector3 desiredPosition,
            float rotationY,
            out string error)
        {
            error = string.Empty;
            if (!BeginMove(placed))
            {
                error = "N\u00E3o iniciou o modo de mover.";
                return false;
            }
            if (!TryFindGround(desiredPosition, out var groundedPosition, out var groundCollider))
            {
                error = "Terreno n\u00E3o encontrado.";
                CancelPlacement();
                return false;
            }
            previewPosition = SnapPosition(groundedPosition) + Vector3.up * activeDefinition.GroundOffset;
            previewRotation = SnapRotation(rotationY, activeDefinition.RotationStep);
            UpdateFenceSnap(previewPosition);
            preview.transform.SetPositionAndRotation(
                previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity(groundCollider);
            RefreshGrid();
            if (!previewValid)
            {
                error = invalidReason;
                CancelPlacement();
                return false;
            }
            return ConfirmPlacement();
        }

        public void SetGridVisibleForTesting(bool visible)
        {
            gridVisible = visible;
            RefreshGrid();
            RefreshInstruction();
        }

        public bool TryReclaim(FarmPlacedObject placed)
'@ 'api de teste do movimento'
$building = Replace-Checked $building @'
        private void TryReclaimLookTarget()
'@ @'
        private void TryMoveLookTarget()
        {
            var target = FindLookTarget();
            if (target == null)
            {
                hud?.ShowSystemToast("Mire em uma constru\u00E7\u00E3o pr\u00F3xima para mover.", true);
                return;
            }
            BeginMove(target);
        }

        private void TryReclaimLookTarget()
'@ 'selecao do alvo a mover'
$building = Replace-Checked $building @'
            preview.transform.SetPositionAndRotation(
                previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity(chosen.Value.collider);
'@ @'
            preview.transform.SetPositionAndRotation(
                previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity(chosen.Value.collider);
            RefreshGrid();
'@ 'atualizacao visual da grade'
$building = Replace-Checked $building @'
                var definition = placed != null ? placed.Definition : null;
'@ @'
                if (placed == movingObject) continue;
                var definition = placed != null ? placed.Definition : null;
'@ 'ignorar cerca movida no snap'
$building = Replace-Checked $building @'
                if (placed == null || placed.Definition == null || placed.Definition.Function != FarmBuildableFunction.Fence) continue;
'@ @'
                if (placed == movingObject) continue;
                if (placed == null || placed.Definition == null || placed.Definition.Function != FarmBuildableFunction.Fence) continue;
'@ 'ignorar centro da cerca movida'
$building = Replace-Checked $building @'
            preview.transform.SetPositionAndRotation(previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity();
            return fenceSnapped && previewValid;
'@ @'
            preview.transform.SetPositionAndRotation(previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity();
            RefreshGrid();
            return fenceSnapped && previewValid;
'@ 'grade no teste de cerca'
$building = Replace-Checked $building @'
                    if (placed == null || placed.Definition == null) continue;
'@ @'
                    if (placed == movingObject) continue;
                    if (placed == null || placed.Definition == null) continue;
'@ 'ignorar construcao movida na sobreposicao'
$oldInstruction = @'
            var status = fenceSnapped && previewValid ? "ENCAIXADA" : previewValid ? "LOCAL V\u00C1LIDO" : invalidReason.ToUpperInvariant();
            var action = activeDefinition.Function == FarmBuildableFunction.Fence
                ? "Clique: posicionar/continuar  \u2022  R: 90\u00B0  \u2022  Esc: terminar"
                : "Clique esquerdo: posicionar  \u2022  R: girar  \u2022  Esc/direito: cancelar";
            instructionText.text = $"{activeDefinition.DisplayName.ToUpperInvariant()}  \u2022  {status}\n{action}";
'@
$newInstruction = @'
            var status = fenceSnapped && previewValid ? "ENCAIXADA" : previewValid ? "LOCAL V\u00C1LIDO" : invalidReason.ToUpperInvariant();
            var grid = gridVisible ? "grade ligada" : "grade desligada";
            string action;
            if (movingObject != null)
                action = $"Clique: confirmar mudan\u00E7a  \u2022  R: girar  \u2022  H: {grid}  \u2022  Esc: desfazer";
            else if (activeDefinition.Function == FarmBuildableFunction.Fence)
                action = $"Clique: posicionar/continuar  \u2022  R: 90\u00B0  \u2022  H: {grid}  \u2022  Esc: terminar";
            else
                action = $"Clique: posicionar  \u2022  R: girar  \u2022  H: {grid}  \u2022  Esc: cancelar";
            var mode = movingObject != null ? "MOVER  \u2022  " : string.Empty;
            instructionText.text = $"{mode}{activeDefinition.DisplayName.ToUpperInvariant()}  \u2022  {status}\n{action}";
'@
$building = Replace-Checked $building $oldInstruction $newInstruction 'instrucoes de movimento e grade'
$building = Replace-Checked $building @'
        private void ClonePreviewMaterials()
'@ @'
        private void RefreshGrid()
        {
            if (gridVisual == null) return;
            gridVisual.SetVisible(IsPlacing && gridVisible);
            if (IsPlacing) gridVisual.SetCenter(previewPosition);
        }

        private void ClonePreviewMaterials()
'@ 'controle da grade'
Submit-Script 'Assets/_Project/Scripts/Farming/FarmBuildingSystem.cs' $building 'building-move-system-v16'

$gridContent = [IO.File]::ReadAllText((Join-Path $project '.codex\FarmBuildGridVisual.v16.cs.txt'))
Submit-Script 'Assets/_Project/Scripts/Farming/FarmBuildGridVisual.cs' $gridContent 'building-grid-visual-v16'
