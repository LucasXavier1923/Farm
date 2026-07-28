using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmPrototype.Farming
{
    public sealed class FarmPlacedObject : MonoBehaviour
    {
        public string PersistentId { get; private set; }
        public string ItemId { get; private set; }
        public FarmBuildableDefinition Definition { get; private set; }

        public void Initialize(string persistentId, FarmBuildableDefinition definition)
        {
            PersistentId = persistentId;
            Definition = definition;
            ItemId = definition != null && definition.KitItem != null ? definition.KitItem.Id : string.Empty;
        }
    }

    public class FarmBuildableRadiusIndicator : MonoBehaviour
    {
        private const int Segments = 64;
        private LineRenderer coverageLine;
        private Material runtimeMaterial;
        private Transform player;
        private float radius;
        private float pulseUntil;
        private bool preview;
        private Color baseColor = new(0.20f, 0.82f, 1f, 1f);

        public float Radius => radius;

        public void Initialize(float coverageRadius, bool isPreview) =>
            Initialize(coverageRadius, isPreview, new Color(0.20f, 0.82f, 1f, 1f));

        public void Initialize(float coverageRadius, bool isPreview, Color indicatorColor)
        {
            radius = Mathf.Max(0.5f, coverageRadius);
            baseColor = indicatorColor;
            preview = isPreview;
            player = GameObject.Find("Player")?.transform;
            var coverage = new GameObject("BuildableEffectRadius");
            coverage.transform.SetParent(transform, false);
            var scale = transform.lossyScale;
            coverage.transform.localScale = new Vector3(
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            coverageLine = coverage.AddComponent<LineRenderer>();
            coverageLine.useWorldSpace = false;
            coverageLine.loop = true;
            coverageLine.positionCount = Segments;
            coverageLine.widthMultiplier = isPreview ? 0.09f : 0.055f;
            coverageLine.numCornerVertices = 2;
            coverageLine.numCapVertices = 2;
            coverageLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coverageLine.receiveShadows = false;
            coverageLine.sortingOrder = 20;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader != null)
            {
                runtimeMaterial = new Material(shader) { name = "BuildableRadius_Runtime" };
                coverageLine.material = runtimeMaterial;
            }
            for (var index = 0; index < Segments; index++)
            {
                var angle = index * Mathf.PI * 2f / Segments;
                coverageLine.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0.08f, Mathf.Sin(angle) * radius));
            }
            RefreshColor();
        }

        public void Pulse()
        {
            pulseUntil = Time.time + 2.5f;
            if (coverageLine != null) coverageLine.enabled = true;
        }

        private void Update()
        {
            if (coverageLine == null) return;
            if (player == null) player = GameObject.Find("Player")?.transform;
            var pulsing = Time.time < pulseUntil;
            var nearby = player != null && Vector3.Distance(player.position, transform.position) <= radius + 4f;
            coverageLine.enabled = preview || pulsing || nearby;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (coverageLine == null) return;
            var alpha = preview ? 0.72f : Time.time < pulseUntil ? 0.95f : 0.22f;
            var color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            coverageLine.startColor = color;
            coverageLine.endColor = color;
            if (runtimeMaterial != null) runtimeMaterial.color = color;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
    public sealed class FarmSprinklerEmitter : FarmBuildableRadiusIndicator { }

    public sealed class FarmBuildingSystem : MonoBehaviour
    {
        private const float MaximumBuildDistance = 12f;
        private const float ReclaimDistance = 6f;
        private const float GridSize = 0.5f;
        private const float FenceSnapDistance = 1.25f;
        private static readonly Color ValidColor = new(0.24f, 0.92f, 0.38f, 1f);
        private static readonly Color InvalidColor = new(0.94f, 0.24f, 0.18f, 1f);
        private static readonly Color PanelColor = new(0.045f, 0.065f, 0.04f, 0.94f);

        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;
        private Transform player;
        private Transform placedRoot;
        private Canvas canvas;
        private Font font;
        private Button launcherButton;
        private Text launcherText;
        private FarmBuildingCatalog catalog;
        private GameObject instructionPanel;
        private Text instructionText;
        private FarmBuildableDefinition activeDefinition;
        private GameObject preview;
        private Vector3 previewPosition;
        private float previewRotation;
        private bool previewValid;
        private string invalidReason = string.Empty;
        private bool fenceSnapped;
        private Vector3 fenceSnapAnchor;
        private GameObject fenceSnapMarker;
        private Material fenceSnapMaterial;
        private FarmPlacedObject movingObject;
        private FarmPlacedObjectSaveData movingOriginalData;
        private FarmBuildGridVisual gridVisual;
        private bool gridVisible = true;

        public bool IsPlacing => activeDefinition != null && preview != null;
        public bool IsPreviewValid => IsPlacing && previewValid;
        public Vector3 PreviewPosition => previewPosition;
        public float PreviewRotation => previewRotation;
        public int PlacedCount => placedRoot != null ? placedRoot.childCount : 0;
        public Transform PlacedRoot => placedRoot;
        public bool IsFenceSnapped => fenceSnapped;
        public Vector3 FenceSnapAnchor => fenceSnapAnchor;
        public string ActiveItemId => activeDefinition != null && activeDefinition.KitItem != null
            ? activeDefinition.KitItem.Id
            : string.Empty;
        public FarmBuildingCatalog Catalog => catalog;
        public bool IsMoving => movingObject != null;
        public bool IsGridVisible => gridVisual != null && gridVisual.IsVisible;
        public int GridLineSegmentCount => gridVisual != null ? gridVisual.LineSegmentCount : 0;

        public void Initialize(
            FarmTestPlot owner,
            FarmGameState gameState,
            FarmHudController ownerHud,
            Transform playerTransform)
        {
            if (placedRoot != null) return;
            plot = owner;
            state = gameState;
            hud = ownerHud;
            player = playerTransform;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rootObject = new GameObject("Farm_Placed_Objects");
            rootObject.transform.SetParent(owner.transform, true);
            placedRoot = rootObject.transform;
            canvas = hud != null ? hud.GetComponentInChildren<Canvas>() : null;
            if (canvas == null) throw new InvalidOperationException("Canvas is missing for building mode.");
            var gridObject = new GameObject("FarmBuildGridVisual");
            gridObject.transform.SetParent(owner.transform, true);
            gridVisual = gridObject.AddComponent<FarmBuildGridVisual>();
            gridVisual.Initialize(GridSize);
            CreateInterface();
            catalog = GetComponent<FarmBuildingCatalog>();
            if (catalog == null) catalog = gameObject.AddComponent<FarmBuildingCatalog>();
            catalog.Initialize(this, state, hud, canvas);
            RebuildFromState();
        }

        private void Update()
        {
            if (state == null || hud == null) return;
            RefreshLauncher();
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (!IsPlacing)
            {
                if (!FarmHudController.IsModalOpen && keyboard != null && keyboard.gKey.wasPressedThisFrame)
                    BeginPlacement(plot != null ? plot.SelectedItemId : null);
                if (!FarmHudController.IsModalOpen && keyboard != null && keyboard.xKey.wasPressedThisFrame)
                    TryReclaimLookTarget();
                if (!FarmHudController.IsModalOpen && keyboard != null && keyboard.mKey.wasPressedThisFrame)
                    TryMoveLookTarget();
                return;
            }

            if (FarmHudController.IsModalOpen)
            {
                CancelPlacement();
                return;
            }

            UpdatePreviewFromPointer();
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            {
                gridVisible = !gridVisible;
                RefreshGrid();
                RefreshInstruction();
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                previewRotation = Mathf.Repeat(
                    previewRotation + Mathf.Max(1f, activeDefinition.RotationStep), 360f);
                if (activeDefinition.Function == FarmBuildableFunction.Fence) UpdatePreviewFromPointer();
                else
                {
                    preview.transform.rotation = Quaternion.Euler(0f, previewRotation, 0f);
                    RefreshPreviewValidity();
                }
            }
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                CancelPlacement();
                return;
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ConfirmPlacement();
        }

        private void OnDisable()
        {
            catalog?.Close();
            CancelPlacement();
        }

        public bool BeginPlacement(string itemId)
        {
            if (FarmHudController.IsModalOpen || state == null) return false;
            if (!EnsureSessionAuthority(FarmPermission.PlaceBuildable)) return false;
            var definition = FarmBuildableDatabase.GetByItemId(itemId);
            if (definition == null)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.select_kit", "Select a buildable kit from the hotbar."), true);
                return false;
            }
            if (state.GetQuantity(itemId) <= 0)
            {
                hud?.ShowSystemToast(FarmLocalization.Format("building.item_missing", "You do not have {0}.", definition.LocalizedName), true);
                return false;
            }

            CancelPlacement();
            return StartPreview(
                definition,
                SnapRotation(player != null ? player.eulerAngles.y : 0f, definition.RotationStep));
        }

        public bool BeginMove(FarmPlacedObject placed)
        {
            if (FarmHudController.IsModalOpen || state == null || placed == null ||
                placed.Definition == null || IsPlacing) return false;
            if (!EnsureSessionAuthority(FarmPermission.MoveBuildable)) return false;
            if (player != null && Vector3.Distance(player.position, placed.transform.position) > ReclaimDistance)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.move.too_far", "Move closer to reposition it."), true);
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
        {
            if (preview != null) Destroy(preview);
            if (movingObject != null) movingObject.gameObject.SetActive(true);
            movingObject = null;
            movingOriginalData = null;
            preview = null;
            activeDefinition = null;
            previewValid = false;
            invalidReason = string.Empty;
            fenceSnapped = false;
            fenceSnapAnchor = Vector3.zero;
            if (fenceSnapMarker != null) Destroy(fenceSnapMarker);
            if (fenceSnapMaterial != null) Destroy(fenceSnapMaterial);
            fenceSnapMarker = null;
            fenceSnapMaterial = null;
            gridVisual?.SetVisible(false);
            if (instructionPanel != null) instructionPanel.SetActive(false);
        }

        public bool ConfirmPlacement()
        {
            if (!IsPlacing) return false;
            if (!EnsureSessionAuthority(FarmPermission.PlaceBuildable))
            {
                CancelPlacement();
                return false;
            }
            RefreshPreviewValidity();
            if (!previewValid)
            {
                hud?.ShowSystemToast(invalidReason, true);
                return false;
            }

            if (movingObject != null) return ConfirmMove();

            var definition = activeDefinition;
            var itemId = definition.KitItem.Id;
            if (!state.TryRemoveItem(itemId, 1))
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.kit_missing", "That kit is no longer in your inventory."), true);
                CancelPlacement();
                return false;
            }

            var data = new FarmPlacedObjectSaveData
            {
                PersistentId = Guid.NewGuid().ToString("N"),
                ItemId = itemId,
                X = previewPosition.x,
                Y = previewPosition.y,
                Z = previewPosition.z,
                RotationY = previewRotation
            };
            if (!state.AddPlacedObject(data))
            {
                state.AddItem(itemId, 1);
                hud?.ShowSystemToast(FarmLocalization.Get("building.record_failed", "Could not record the building placement."), true);
                return false;
            }

            InstantiatePlaced(data);
            var continueFence = definition.Function == FarmBuildableFunction.Fence && state.GetQuantity(itemId) > 0;
            hud?.ShowSystemToast(continueFence
                ? FarmLocalization.Format("building.fence_connected", "{0} connected. Keep placing or press Esc.", definition.LocalizedName)
                : FarmLocalization.Format("building.placed", "{0} placed.", definition.LocalizedName), false);
            CancelPlacement();
            if (continueFence) BeginPlacement(itemId);
            return true;
        }

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
                hud?.ShowSystemToast(FarmLocalization.Get("building.update_failed", "Could not update the building placement."), true);
                return false;
            }

            movingObject = null;
            movingOriginalData = null;
            placed.transform.SetPositionAndRotation(
                new Vector3(data.X, data.Y, data.Z),
                Quaternion.Euler(0f, data.RotationY, 0f));
            placed.gameObject.SetActive(true);
            var displayName = placed.Definition != null ? placed.Definition.LocalizedName : FarmLocalization.Get("building.fallback_name", "Building");
            hud?.ShowSystemToast(FarmLocalization.Format("building.moved", "{0} moved without spending a kit.", displayName), false);
            CancelPlacement();
            return true;
        }

        public bool TryPlaceDirectForTesting(
            string itemId,
            Vector3 desiredPosition,
            float rotationY,
            out string error)
        {
            error = string.Empty;
            if (!BeginPlacement(itemId))
            {
                error = "Could not start building mode.";
                return false;
            }
            if (!TryFindGround(desiredPosition, out var groundedPosition, out var groundCollider))
            {
                error = "Ground not found.";
                CancelPlacement();
                return false;
            }
            previewPosition = SnapPosition(groundedPosition) + Vector3.up * activeDefinition.GroundOffset;
            previewRotation = SnapRotation(rotationY, activeDefinition.RotationStep);
            UpdateFenceSnap(previewPosition);
            preview.transform.SetPositionAndRotation(
                previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity(groundCollider);
            if (!previewValid)
            {
                error = invalidReason;
                CancelPlacement();
                return false;
            }
            return ConfirmPlacement();
        }

        public bool TryMoveDirectForTesting(
            FarmPlacedObject placed,
            Vector3 desiredPosition,
            float rotationY,
            out string error)
        {
            error = string.Empty;
            if (!BeginMove(placed))
            {
                error = "Could not start move mode.";
                return false;
            }
            if (!TryFindGround(desiredPosition, out var groundedPosition, out var groundCollider))
            {
                error = "Ground not found.";
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
        {
            if (placed == null || state == null || string.IsNullOrWhiteSpace(placed.ItemId)) return false;
            if (!EnsureSessionAuthority(FarmPermission.ReclaimBuildable)) return false;
            if (!state.CanReclaimPlacedObject(placed.PersistentId))
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.storage_shed.reclaim_blocked", "Move items out of storage before reclaiming this Farm Shed."), true);
                return false;
            }
            if (player != null && Vector3.Distance(player.position, placed.transform.position) > ReclaimDistance)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.reclaim.too_far", "Move closer to reclaim it."), true);
                return false;
            }
            if (!state.CanAdd(placed.ItemId, 1))
            {
                hud?.ShowInventoryFullToast();
                return false;
            }
            if (!state.AddItem(placed.ItemId, 1)) return false;
            if (!state.RemovePlacedObject(placed.PersistentId))
            {
                state.TryRemoveItem(placed.ItemId, 1);
                return false;
            }
            var displayName = placed.Definition != null ? placed.Definition.LocalizedName : FarmLocalization.Get("building.fallback_name", "Building");
            Destroy(placed.gameObject);
            hud?.ShowSystemToast(FarmLocalization.Format("building.reclaimed", "{0} returned to your inventory.", displayName), false);
            return true;
        }

        public void RebuildFromState()
        {
            CancelPlacement();
            if (placedRoot == null || state == null) return;
            for (var index = placedRoot.childCount - 1; index >= 0; index--)
            {
                var child = placedRoot.GetChild(index);
                child.SetParent(null, true);
                Destroy(child.gameObject);
            }
            foreach (var data in state.PlacedObjects) InstantiatePlaced(data);
        }

        public FarmPlacedObject FindPlaced(string persistentId)
        {
            if (placedRoot == null) return null;
            foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
                if (string.Equals(placed.PersistentId, persistentId, StringComparison.OrdinalIgnoreCase))
                    return placed;
            return null;
        }

        public int ApplyMorningSprinklers()
        {
            if (!FarmSessionTime.IsSimulationAuthority || placedRoot == null || plot == null) return 0;
            var watered = 0;
            foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
            {
                if (placed == movingObject) continue;
                var definition = placed != null ? placed.Definition : null;
                if (definition == null || definition.Function != FarmBuildableFunction.Sprinkler) continue;
                watered += plot.ApplySprinklerWatering(placed.transform.position, Mathf.Max(0.5f, definition.EffectRadius));
                placed.GetComponent<FarmBuildableRadiusIndicator>()?.Pulse();
            }
            return watered;
        }
        public bool IsProtectedByScarecrow(Vector3 position)
        {
            if (placedRoot == null) return false;
            foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
            {
                if (placed == movingObject) continue;
                var definition = placed != null ? placed.Definition : null;
                if (definition == null || definition.Function != FarmBuildableFunction.Scarecrow) continue;
                var delta = position - placed.transform.position;
                delta.y = 0f;
                var radius = Mathf.Max(0.5f, definition.EffectRadius);
                if (delta.sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        public void PulseScarecrows()
        {
            if (placedRoot == null) return;
            foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
                if (placed != null && placed.Definition != null && placed.Definition.Function == FarmBuildableFunction.Scarecrow)
                    placed.GetComponent<FarmBuildableRadiusIndicator>()?.Pulse();
        }

        private bool EnsureSessionAuthority(FarmPermission permission = FarmPermission.PlaceBuildable)
        {
            if (FarmPermissionPolicy.CanMutateLocally(permission)) return true;
            hud?.ShowSystemToast(FarmPermissionPolicy.DenialMessage(permission), true);
            return false;
        }

        private void TryMoveLookTarget()
        {
            var target = FindLookTarget();
            if (target == null)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.move.aim", "Aim at a nearby building to move it."), true);
                return;
            }
            BeginMove(target);
        }

        private void TryReclaimLookTarget()
        {
            var target = FindLookTarget();
            if (target == null)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("building.reclaim.aim", "Aim at a nearby building to reclaim it."), true);
                return;
            }
            TryReclaim(target);
        }

        private FarmPlacedObject FindLookTarget()
        {
            if (Camera.main == null || Mouse.current == null) return null;
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 250f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                var placed = hit.collider.GetComponentInParent<FarmPlacedObject>();
                if (placed != null && (player == null ||
                    Vector3.Distance(player.position, placed.transform.position) <= ReclaimDistance))
                    return placed;
            }
            return null;
        }

        private void UpdatePreviewFromPointer()
        {
            if (!IsPlacing || Camera.main == null || Mouse.current == null) return;
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 300f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            RaycastHit? chosen = null;
            foreach (var hit in hits)
            {
                if (preview != null && hit.collider.transform.IsChildOf(preview.transform)) continue;
                if (player != null && hit.collider.transform.IsChildOf(player)) continue;
                if (hit.normal.y < 0.45f) continue;
                chosen = hit;
                break;
            }
            if (!chosen.HasValue)
            {
                previewValid = false;
                invalidReason = FarmLocalization.Get("building.ground.aim", "Aim at reachable ground.");
                RefreshInstruction();
                return;
            }
            previewPosition = SnapPosition(chosen.Value.point) + Vector3.up * activeDefinition.GroundOffset;
            UpdateFenceSnap(previewPosition);
            preview.transform.SetPositionAndRotation(
                previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity(chosen.Value.collider);
            RefreshGrid();
        }

        private void CreateFenceSnapMarker()
        {
            fenceSnapMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fenceSnapMarker.name = "FenceSnapMarker";
            fenceSnapMarker.transform.localScale = Vector3.one * 0.24f;
            var collider = fenceSnapMarker.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = fenceSnapMarker.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (renderer != null && shader != null)
            {
                fenceSnapMaterial = new Material(shader) { name = "FenceSnap_Runtime" };
                fenceSnapMaterial.color = new Color(1f, 0.78f, 0.16f, 1f);
                renderer.material = fenceSnapMaterial;
            }
            fenceSnapMarker.SetActive(false);
        }

        private void UpdateFenceSnap(Vector3 rawPosition)
        {
            fenceSnapped = false;
            fenceSnapAnchor = Vector3.zero;
            if (fenceSnapMarker != null) fenceSnapMarker.SetActive(false);
            if (activeDefinition == null || activeDefinition.Function != FarmBuildableFunction.Fence) return;
            if (!TryGetFenceSnap(rawPosition, previewRotation, out var snappedPosition, out var anchor)) return;
            previewPosition = snappedPosition;
            fenceSnapped = true;
            fenceSnapAnchor = anchor;
            if (fenceSnapMarker != null)
            {
                fenceSnapMarker.transform.position = anchor + Vector3.up * 0.18f;
                fenceSnapMarker.SetActive(true);
            }
        }

        public bool TryGetFenceSnap(Vector3 rawPosition, float rotationY, out Vector3 snappedPosition, out Vector3 anchor)
        {
            snappedPosition = rawPosition;
            anchor = Vector3.zero;
            if (placedRoot == null) return false;
            var fenceDefinition = FarmBuildableDatabase.GetByItemId("fence_kit");
            if (fenceDefinition == null) return false;
            var previewHalf = Mathf.Max(0.15f, fenceDefinition.Footprint.x * 0.5f);
            var previewRight = Quaternion.Euler(0f, rotationY, 0f) * Vector3.right;
            var bestDistance = FenceSnapDistance;
            foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
            {
                if (placed == movingObject) continue;
                var definition = placed != null ? placed.Definition : null;
                if (definition == null || definition.Function != FarmBuildableFunction.Fence) continue;
                var placedHalf = Mathf.Max(0.15f, definition.Footprint.x * 0.5f);
                var placedRight = placed.transform.right;
                var endpoints = new[]
                {
                    placed.transform.position + placedRight * placedHalf,
                    placed.transform.position - placedRight * placedHalf
                };
                foreach (var endpoint in endpoints)
                {
                    var candidates = new[]
                    {
                        endpoint + previewRight * previewHalf,
                        endpoint - previewRight * previewHalf
                    };
                    foreach (var candidate in candidates)
                    {
                        var candidatePosition = candidate;
                        candidatePosition.y = rawPosition.y;
                        if (IsExistingFenceCenter(candidatePosition)) continue;
                        var delta = candidatePosition - rawPosition;
                        delta.y = 0f;
                        var distance = delta.magnitude;
                        if (distance > bestDistance) continue;
                        bestDistance = distance;
                        snappedPosition = candidatePosition;
                        anchor = endpoint;
                    }
                }
            }
            return bestDistance < FenceSnapDistance;
        }
        private bool IsExistingFenceCenter(Vector3 position)
        {
            if (placedRoot == null) return false;
            foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
            {
                if (placed == movingObject) continue;
                if (placed == null || placed.Definition == null || placed.Definition.Function != FarmBuildableFunction.Fence) continue;
                var delta = placed.transform.position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= 0.01f) return true;
            }
            return false;
        }

        public bool SetFencePreviewForTesting(Vector3 rawPosition, float rotationY)
        {
            if (!IsPlacing || activeDefinition.Function != FarmBuildableFunction.Fence) return false;
            previewRotation = SnapRotation(rotationY, activeDefinition.RotationStep);
            previewPosition = rawPosition;
            UpdateFenceSnap(rawPosition);
            preview.transform.SetPositionAndRotation(previewPosition, Quaternion.Euler(0f, previewRotation, 0f));
            RefreshPreviewValidity();
            RefreshGrid();
            return fenceSnapped && previewValid;
        }

        private static bool IsFenceEndpoint(FarmPlacedObject placed, Vector3 point)
        {
            if (placed == null || placed.Definition == null) return false;
            var half = Mathf.Max(0.15f, placed.Definition.Footprint.x * 0.5f);
            var first = placed.transform.position + placed.transform.right * half;
            var second = placed.transform.position - placed.transform.right * half;
            var firstDelta = first - point;
            var secondDelta = second - point;
            firstDelta.y = 0f;
            secondDelta.y = 0f;
            return firstDelta.sqrMagnitude <= 0.01f || secondDelta.sqrMagnitude <= 0.01f;
        }
        private void RefreshPreviewValidity(Collider groundCollider = null)
        {
            if (!IsPlacing) return;
            previewValid = ValidatePlacement(
                activeDefinition, previewPosition, previewRotation, groundCollider, out invalidReason);
            ApplyPreviewColor(previewValid ? ValidColor : InvalidColor);
            RefreshInstruction();
        }

        private bool ValidatePlacement(
            FarmBuildableDefinition definition,
            Vector3 position,
            float rotationY,
            Collider groundCollider,
            out string reason)
        {
            reason = string.Empty;
            if (definition == null || definition.Prefab == null)
            {
                reason = FarmLocalization.Get("building.definition.invalid", "Invalid building definition.");
                return false;
            }
            if (player != null && Vector3.Distance(player.position, position) > MaximumBuildDistance)
            {
                reason = FarmLocalization.Get("building.location.too_far", "That location is too far away.");
                return false;
            }

            if (placedRoot != null)
            {
                foreach (var placed in placedRoot.GetComponentsInChildren<FarmPlacedObject>(true))
                {
                    if (placed == movingObject) continue;
                    if (placed == null || placed.Definition == null) continue;
                    if (definition.Function == FarmBuildableFunction.Fence &&
                        placed.Definition.Function == FarmBuildableFunction.Fence && fenceSnapped &&
                        IsFenceEndpoint(placed, fenceSnapAnchor)) continue;
                    if (!FootprintsOverlap(
                        position, rotationY, definition.Footprint,
                        placed.transform.position, placed.transform.eulerAngles.y, placed.Definition.Footprint)) continue;
                    reason = FarmLocalization.Get("building.location.occupied", "A building already occupies that space.");
                    return false;
                }
            }
            var size = new Vector3(
                Mathf.Max(0.3f, definition.Footprint.x),
                Mathf.Max(0.5f, definition.Footprint.y),
                Mathf.Max(0.3f, definition.Footprint.z));
            var center = position + Vector3.up * (size.y * 0.5f + 0.04f);
            var overlaps = Physics.OverlapBox(
                center, size * 0.5f, Quaternion.Euler(0f, rotationY, 0f),
                ~0, QueryTriggerInteraction.Ignore);
            foreach (var collider in overlaps)
            {
                if (collider == null || collider == groundCollider) continue;
                if (preview != null && collider.transform.IsChildOf(preview.transform)) continue;
                if (player != null && collider.transform.IsChildOf(player)) continue;
                if (collider.GetComponentInParent<FarmPlacedObject>() != null) continue;
                if (collider.bounds.max.y <= position.y + 0.18f) continue;
                if (collider.bounds.size.x > 25f && collider.bounds.size.z > 25f &&
                    collider.bounds.max.y <= position.y + 0.8f) continue;
                reason = collider.GetComponentInParent<FarmPlacedObject>() != null
                    ? FarmLocalization.Get("building.location.occupied", "A building already occupies that space.")
                    : FarmLocalization.Get("building.location.blocked", "That space is blocked.");
                return false;
            }
            return true;
        }

        private static bool FootprintsOverlap(
            Vector3 firstPosition, float firstRotation, Vector3 firstFootprint,
            Vector3 secondPosition, float secondRotation, Vector3 secondFootprint)
        {
            var firstRight = Quaternion.Euler(0f, firstRotation, 0f) * Vector3.right;
            var firstForward = Quaternion.Euler(0f, firstRotation, 0f) * Vector3.forward;
            var secondRight = Quaternion.Euler(0f, secondRotation, 0f) * Vector3.right;
            var secondForward = Quaternion.Euler(0f, secondRotation, 0f) * Vector3.forward;
            var delta = secondPosition - firstPosition;
            delta.y = 0f;
            var firstHalf = new Vector2(Mathf.Max(0.15f, firstFootprint.x * 0.5f), Mathf.Max(0.15f, firstFootprint.z * 0.5f));
            var secondHalf = new Vector2(Mathf.Max(0.15f, secondFootprint.x * 0.5f), Mathf.Max(0.15f, secondFootprint.z * 0.5f));
            var axes = new[] { firstRight, firstForward, secondRight, secondForward };
            foreach (var axis in axes)
            {
                var distance = Mathf.Abs(Vector3.Dot(delta, axis));
                var firstRadius = Mathf.Abs(Vector3.Dot(firstRight, axis)) * firstHalf.x + Mathf.Abs(Vector3.Dot(firstForward, axis)) * firstHalf.y;
                var secondRadius = Mathf.Abs(Vector3.Dot(secondRight, axis)) * secondHalf.x + Mathf.Abs(Vector3.Dot(secondForward, axis)) * secondHalf.y;
                if (distance >= firstRadius + secondRadius - 0.03f) return false;
            }
            return true;
        }
        private bool TryFindGround(Vector3 desiredPosition, out Vector3 grounded, out Collider groundCollider)
        {
            var origin = desiredPosition + Vector3.up * 40f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (preview != null && hit.collider.transform.IsChildOf(preview.transform)) continue;
                if (hit.collider.GetComponentInParent<FarmPlacedObject>() != null) continue;
                if (player != null && hit.collider.transform.IsChildOf(player)) continue;
                if (hit.normal.y < 0.45f) continue;
                grounded = hit.point;
                groundCollider = hit.collider;
                return true;
            }
            grounded = desiredPosition;
            groundCollider = null;
            return false;
        }

        private void InstantiatePlaced(FarmPlacedObjectSaveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.PersistentId)) return;
            var definition = FarmBuildableDatabase.GetByItemId(data.ItemId);
            if (definition == null) return;
            var instance = Instantiate(definition.Prefab, placedRoot);
            instance.name = $"Placed_{definition.Id}_{data.PersistentId[..Mathf.Min(8, data.PersistentId.Length)]}";
            instance.transform.SetPositionAndRotation(
                new Vector3(data.X, data.Y, data.Z),
                Quaternion.Euler(0f, data.RotationY, 0f));
            instance.transform.localScale = definition.PlacedScale;
            if (instance.GetComponentInChildren<Collider>() == null)
            {
                var box = instance.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, Mathf.Max(0.25f, definition.Footprint.y * 0.5f), 0f);
                box.size = definition.Footprint;
            }
            instance.AddComponent<FarmPlacedObject>().Initialize(data.PersistentId, definition);
            if (definition.Function == FarmBuildableFunction.Sprinkler)
                instance.AddComponent<FarmSprinklerEmitter>().Initialize(definition.EffectRadius, false);
            else if (definition.Function == FarmBuildableFunction.Scarecrow)
                instance.AddComponent<FarmBuildableRadiusIndicator>().Initialize(definition.EffectRadius, false, new Color(1f, 0.72f, 0.16f, 1f));
        }

        private void RefreshLauncher()
        {
            if (launcherText == null || launcherButton == null || state == null) return;
            var ownedKits = 0;
            foreach (var definition in FarmBuildableDatabase.Definitions)
                if (definition != null && definition.KitItem != null)
                    ownedKits += state.GetQuantity(definition.KitItem.Id);
            launcherButton.interactable = !FarmHudController.IsModalOpen && !IsPlacing &&
                FarmBuildableDatabase.Definitions.Count > 0;
            launcherText.text = FarmLocalization.Format("building.launcher", "BUILD  [B]  •  {0} kit{1}", ownedKits, ownedKits == 1 ? string.Empty : "s");
            launcherText.color = launcherButton.interactable ? ValidColor : new Color(0.66f, 0.70f, 0.62f);
        }

        private void RefreshInstruction()
        {
            if (instructionText == null || activeDefinition == null) return;
            var status = fenceSnapped && previewValid
                ? FarmLocalization.Get("building.status.snapped", "SNAPPED")
                : previewValid ? FarmLocalization.Get("building.status.valid", "VALID LOCATION") : invalidReason.ToUpperInvariant();
            var grid = gridVisible
                ? FarmLocalization.Get("building.grid.on", "grid on")
                : FarmLocalization.Get("building.grid.off", "grid off");
            string action;
            if (movingObject != null)
                action = FarmLocalization.Format("building.instructions.move", "Click: confirm move  •  R: rotate  •  H: {0}  •  Esc: undo", grid);
            else if (activeDefinition.Function == FarmBuildableFunction.Fence)
                action = FarmLocalization.Format("building.instructions.fence", "Click: place/continue  •  R: 90°  •  H: {0}  •  Esc: finish", grid);
            else
                action = FarmLocalization.Format("building.instructions.place", "Click: place  •  R: rotate  •  H: {0}  •  Esc: cancel", grid);
            var mode = movingObject != null ? FarmLocalization.Get("building.mode.move", "MOVE  •  ") : string.Empty;
            instructionText.text = $"{mode}{activeDefinition.LocalizedName.ToUpperInvariant()}  \u2022  {status}\n{action}";
            instructionText.color = previewValid ? ValidColor : InvalidColor;
        }

        private void CreateInterface()
        {
            var launcherObject = CreatePanel(
                "OpenBuilding", canvas.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-18f, -440f), new Vector2(300f, 44f),
                new Vector2(1f, 1f), new Color(0.18f, 0.23f, 0.14f, 1f));
            launcherButton = launcherObject.AddComponent<Button>();
            launcherText = CreateText(
                "Label", launcherObject.transform, string.Empty, 14, FontStyle.Bold,
                Color.white, Vector2.zero, new Vector2(300f, 44f), TextAnchor.MiddleCenter);
            launcherText.rectTransform.anchorMin = Vector2.zero;
            launcherText.rectTransform.anchorMax = Vector2.one;
            launcherText.rectTransform.offsetMin = Vector2.zero;
            launcherText.rectTransform.offsetMax = Vector2.zero;
            launcherButton.onClick.AddListener(() => catalog?.Open());

            instructionPanel = CreatePanel(
                "BuildingInstructions", canvas.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 224f), new Vector2(900f, 76f),
                new Vector2(0.5f, 0f), PanelColor);
            instructionText = CreateText(
                "Text", instructionPanel.transform, string.Empty, 17, FontStyle.Bold,
                Color.white, Vector2.zero, new Vector2(900f, 76f), TextAnchor.MiddleCenter);
            instructionText.rectTransform.anchorMin = Vector2.zero;
            instructionText.rectTransform.anchorMax = Vector2.one;
            instructionText.rectTransform.offsetMin = new Vector2(12f, 6f);
            instructionText.rectTransform.offsetMax = new Vector2(-12f, -6f);
            instructionPanel.SetActive(false);
        }

        private void RefreshGrid()
        {
            if (gridVisual == null) return;
            gridVisual.SetVisible(IsPlacing && gridVisible);
            if (IsPlacing) gridVisual.SetCenter(previewPosition);
        }

        private void ClonePreviewMaterials()
        {
            if (preview == null) return;
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                var clones = new Material[source.Length];
                for (var index = 0; index < source.Length; index++)
                    clones[index] = source[index] != null ? new Material(source[index]) : null;
                renderer.materials = clones;
            }
        }

        private void ApplyPreviewColor(Color color)
        {
            if (preview == null) return;
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.color = color;
                }
        }

        private static Vector3 SnapPosition(Vector3 position)
        {
            position.x = Mathf.Round(position.x / GridSize) * GridSize;
            position.z = Mathf.Round(position.z / GridSize) * GridSize;
            return position;
        }

        private static float SnapRotation(float rotation, float step) =>
            Mathf.Round(rotation / Mathf.Max(1f, step)) * Mathf.Max(1f, step);

        private GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot,
            Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(
            string name,
            Transform parent,
            string content,
            int size,
            FontStyle style,
            Color color,
            Vector2 position,
            Vector2 dimensions,
            TextAnchor alignment)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = item.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
