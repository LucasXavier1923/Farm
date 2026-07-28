using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Compact, renewable quarry loop. Nodes are collected once per farm day;
    /// their daily pickup IDs make the reward persist through save/load without
    /// committing this prototype to a permanent mine-map format.
    /// </summary>
    public sealed class FarmMiningSystem : MonoBehaviour
    {
        public const float InteractionDistance = 2.5f;
        private const int NodeCount = 4;
        private const int StoneYield = 2;
        private const int HitsToMine = 3;

        private readonly List<FarmMiningNode> nodes = new();
        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;
        private Transform player;
        private Transform quarryRoot;
        private FarmMiningNode nearbyNode;
        private int observedDay = -1;

        public bool IsInRange => nearbyNode != null;
        public string Prompt => nearbyNode == null ? string.Empty : !nearbyNode.IsAvailable
            ? nearbyNode.StewardshipPrompt
            : plot != null && plot.ActiveTool == FarmTool.Pickaxe
                ? FarmLocalization.Get("mining.prompt", "Stone outcrop: click or press F to mine.")
                : FarmLocalization.Get("mining.pickaxe_required", "Stone outcrop: equip the Pickaxe to mine.");

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmHudController ownerHud, Transform playerTransform)
        {
            if (quarryRoot != null) return;
            plot = owner;
            state = gameState;
            hud = ownerHud;
            player = playerTransform;
            if (plot == null || state == null || player == null) return;
            CreateQuarry();
            RefreshForCurrentDay();
        }

        private void OnDisable()
        {
            if (plot != null) plot.SetExternalPrompt(this, string.Empty);
        }

        private void Update()
        {
            if (state == null || player == null || quarryRoot == null) return;
            RefreshForCurrentDay();
            nearbyNode = FindNearbyNode();
            if (plot != null) plot.SetExternalPrompt(this, nearbyNode != null ? Prompt : string.Empty);
            if (nearbyNode == null || FarmHudController.IsModalOpen) return;

            var keyboard = Keyboard.current;
            var useKeyboard = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            var useMouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) && IsNodeUnderPointer(nearbyNode);
            if (useKeyboard || useMouse) Mine(nearbyNode);
        }

        private void CreateQuarry()
        {
            quarryRoot = new GameObject("Farm_Starter_Quarry").transform;
            quarryRoot.SetParent(transform, true);
            var side = Vector3.Cross(Vector3.up, plot.PlotForward).normalized;
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            var center = plot.PlotCenter + (side * 10.5f) - (plot.PlotForward * 1.5f);
            var offsets = new[]
            {
                new Vector3(-1.8f, 0f, -1.2f), new Vector3(1.5f, 0f, -1.0f),
                new Vector3(-1.2f, 0f, 1.65f), new Vector3(1.9f, 0f, 1.45f)
            };
            for (var index = 0; index < NodeCount; index++)
            {
                var nodeObject = new GameObject($"Stone_Outcrop_{index + 1}");
                nodeObject.transform.SetParent(quarryRoot, true);
                nodeObject.transform.position = PlaceOnGround(center + offsets[index]);
                var node = nodeObject.AddComponent<FarmMiningNode>();
                node.Initialize(index, state, hud, HitsToMine, StoneYield);
                nodes.Add(node);
            }
        }

        private void RefreshForCurrentDay()
        {
            var day = state.DayNumber;
            if (day == observedDay) return;
            observedDay = day;
            foreach (var node in nodes) node.Refresh(day);
        }

        private FarmMiningNode FindNearbyNode()
        {
            FarmMiningNode closest = null;
            var closestDistance = InteractionDistance;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                var distance = Vector3.Distance(player.position, node.transform.position);
                if (distance > closestDistance) continue;
                closestDistance = distance;
                closest = node;
            }
            return closest;
        }

        private static bool IsNodeUnderPointer(FarmMiningNode node)
        {
            if (node == null || Camera.main == null || Mouse.current == null) return false;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 300f)) return false;
            return hit.collider != null && hit.collider.GetComponentInParent<FarmMiningNode>() == node;
        }

        private void Mine(FarmMiningNode node)
        {
            if (node == null) return;
            if (!node.IsAvailable)
            {
                Steward(node);
                return;
            }
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.ToolAction, "Player", $"tool=mine;node={node.Index}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }

            var result = node.TryMine(plot.ActiveTool);
            if (!string.IsNullOrWhiteSpace(result)) hud?.ShowSystemToast(result, result.StartsWith("Inventory", StringComparison.OrdinalIgnoreCase));
        }

        private void Steward(FarmMiningNode node)
        {
            if (node == null) return;
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.Stewardship, "Player", $"node={node.Index}");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }
            var result = node.TrySteward();
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        public FarmSessionCommandResult ExecuteHostStewardNode(int nodeIndex, string requestedBy)
        {
            if (!FarmSessionTime.IsSimulationAuthority || string.IsNullOrWhiteSpace(requestedBy) || nodeIndex < 0 || nodeIndex >= nodes.Count)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("stewardship.invalid", "That gathering site cannot be restored."));
            return nodes[nodeIndex] != null
                ? nodes[nodeIndex].TrySteward()
                : new FarmSessionCommandResult(false, FarmLocalization.Get("stewardship.invalid", "That gathering site cannot be restored."));
        }

        private static Vector3 PlaceOnGround(Vector3 position)
        {
            if (Physics.Raycast(position + (Vector3.up * 50f), Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                position.y = hit.point.y;
            return position;
        }
    }

    public sealed class FarmMiningNode : MonoBehaviour
    {
        private FarmGameState state;
        private FarmHudController hud;
        private GameObject visual;
        private GameObject stewardshipMarker;
        private SphereCollider interactionCollider;
        private int maxHits;
        private int stoneYield;

        public int Index { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsStewarded => state != null && state.IsResourceNodeStewarded(NodeId);
        public string StewardshipPrompt => state == null
            ? string.Empty
            : FarmWeatherSystem.WeatherForDay(state.WorldSeed, state.DayNumber) != FarmWeather.Rain
                ? FarmLocalization.Get("stewardship.requires_rain", "Renewal works only while it is raining.")
                : IsStewarded
                    ? FarmLocalization.Get("stewardship.already", "This outcrop is already prepared for its next return.")
                    : FarmLocalization.Get("stewardship.prompt", "Resting outcrop: press F to spend Compost and improve tomorrow's yield.");
        private string NodeId => $"starter_quarry:{Index}";

        public void Initialize(int index, FarmGameState gameState, FarmHudController ownerHud, int hitCount, int yield)
        {
            Index = index;
            state = gameState;
            hud = ownerHud;
            maxHits = Mathf.Max(1, hitCount);
            stoneYield = Mathf.Max(1, yield);
            CreateVisual();
        }

        public void Refresh(int currentDay)
        {
            IsAvailable = state != null && !state.IsResourceNodeDepleted(NodeId);
            if (visual != null) visual.SetActive(IsAvailable);
            if (stewardshipMarker != null)
            {
                stewardshipMarker.SetActive(!IsAvailable);
                if (stewardshipMarker.TryGetComponent<Renderer>(out var renderer))
                    renderer.material.color = IsStewarded ? new Color(0.30f, 0.78f, 0.38f, 0.90f) : new Color(0.22f, 0.56f, 0.88f, 0.90f);
            }
            if (interactionCollider != null) interactionCollider.enabled = IsAvailable;
        }

        public string TryMine(FarmTool tool)
        {
            if (!IsAvailable || state == null) return FarmLocalization.Get("mining.depleted", "This outcrop will return tomorrow.");
            if (tool != FarmTool.Pickaxe) return FarmLocalization.Get("mining.pickaxe_required", "Stone outcrop: equip the Pickaxe to mine.");
            var wasStewarded = IsStewarded;
            var effectiveYield = stoneYield + (wasStewarded ? 1 : 0);
            if (!state.TryHitResourceNode(NodeId, maxHits, 1, "stone", effectiveYield, out var hitsRemaining, out var depleted))
                return FarmLocalization.Get("mining.inventory_full", "Inventory full — make room before mining.");

            state.SpendEnergy(FarmTool.Pickaxe, 1);
            if (!depleted) return FarmLocalization.Format("mining.hit", "Outcrop struck. {0} hits remaining.", hitsRemaining);
            IsAvailable = false;
            if (visual != null) visual.SetActive(false);
            if (interactionCollider != null) interactionCollider.enabled = false;
            hud?.ShowPickupToast("stone", effectiveYield);
            return wasStewarded
                ? FarmLocalization.Format("mining.success_stewarded", "Mined {0} Stone. Stewardship improved this outcrop.", effectiveYield)
                : FarmLocalization.Format("mining.success", "Mined {0} Stone. The outcrop will return tomorrow.", effectiveYield);
        }

        public FarmSessionCommandResult TrySteward()
        {
            if (state == null || IsAvailable)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("stewardship.requires_depleted", "Only a resting outcrop can be renewed."));
            return state.TryStewardResourceNode(NodeId, out var error)
                ? new FarmSessionCommandResult(true, FarmLocalization.Get("stewardship.success", "Compost settled into the rain. Tomorrow this outcrop will yield +1 Stone."))
                : new FarmSessionCommandResult(false, error);
        }

        private void CreateVisual()
        {
            visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            var prefab = Resources.Load<GameObject>("Crafting/StonePile");
            if (prefab != null)
            {
                var instance = Instantiate(prefab, visual.transform);
                instance.name = "StonePile";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(0f, Index * 38f, 0f);
                instance.transform.localScale = Vector3.one * 1.25f;
                foreach (var collider in instance.GetComponentsInChildren<Collider>()) DestroyForContext(collider);
            }
            else
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fallback.transform.SetParent(visual.transform, false);
                fallback.transform.localScale = new Vector3(1.55f, 1.1f, 1.4f);
                DestroyForContext(fallback.GetComponent<Collider>());
                fallback.GetComponent<Renderer>().material.color = new Color(0.38f, 0.42f, 0.46f);
            }

            interactionCollider = gameObject.AddComponent<SphereCollider>();
            interactionCollider.center = new Vector3(0f, 0.6f, 0f);
            interactionCollider.radius = 0.95f;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Mining_Marker";
            marker.transform.SetParent(visual.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            marker.transform.localScale = new Vector3(1.05f, 0.03f, 1.05f);
            DestroyForContext(marker.GetComponent<Collider>());
            if (Application.isPlaying)
                marker.GetComponent<Renderer>().material.color = new Color(0.42f, 0.72f, 0.92f, 0.85f);

            stewardshipMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stewardshipMarker.name = "Stewardship_Marker";
            stewardshipMarker.transform.SetParent(transform, false);
            stewardshipMarker.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            stewardshipMarker.transform.localScale = new Vector3(0.72f, 0.03f, 0.72f);
            DestroyForContext(stewardshipMarker.GetComponent<Collider>());
            stewardshipMarker.GetComponent<Renderer>().material.color = new Color(0.22f, 0.56f, 0.88f, 0.90f);
            stewardshipMarker.SetActive(false);
        }

        private static void DestroyForContext(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
