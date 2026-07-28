using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// A calm, limited pond loop. Every catch comes from a deterministic shared
    /// ecology catalog and is committed only through the host boundary.
    /// </summary>
    public sealed class FarmFishingSystem : MonoBehaviour
    {
        private const float InteractionDistance = 3f;
        private const int BaseDailyCatchLimit = 3;

        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;
        private Transform player;
        private Transform pondRoot;
        private Collider pondCollider;
        private GameObject bobber;
        private bool fishing;
        private float biteCountdown;

        public bool IsInRange => player != null && pondRoot != null && Vector3.Distance(player.position, pondRoot.position) <= InteractionDistance;
        public bool IsFishing => fishing;
        public int CatchesToday { get; private set; }
        public int DailyCatchLimit => BaseDailyCatchLimit + (state != null && state.HasNeighborhoodUnlock("bram") ? 1 : 0);

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmHudController ownerHud, Transform playerTransform)
        {
            if (pondRoot != null) return;
            plot = owner;
            state = gameState;
            hud = ownerHud;
            player = playerTransform;
            if (plot == null || state == null || player == null) return;
            CreatePond();
            RefreshCatches();
        }

        private void OnDisable()
        {
            fishing = false;
            if (bobber != null) bobber.SetActive(false);
            plot?.SetExternalPrompt(this, string.Empty);
        }

        private void Update()
        {
            if (state == null || pondRoot == null) return;
            RefreshCatches();
            plot?.SetExternalPrompt(this, IsInRange ? BuildPrompt() : string.Empty);
            if (fishing)
            {
                biteCountdown -= FarmSessionTime.DeltaTime;
                if (biteCountdown <= 0f) CompleteCatch();
                return;
            }
            if (!IsInRange || FarmHudController.IsModalOpen) return;

            var keyboard = Keyboard.current;
            var useKeyboard = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            var useMouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) && IsPondUnderPointer();
            if (useKeyboard || useMouse) TryBeginFishing();
        }

        private void CreatePond()
        {
            pondRoot = new GameObject("Farm_Fishing_Pond").transform;
            pondRoot.SetParent(transform, true);
            var side = Vector3.Cross(Vector3.up, plot.PlotForward).normalized;
            if (side.sqrMagnitude < 0.01f) side = Vector3.left;
            pondRoot.position = PlaceOnGround(plot.PlotCenter - (side * 10.5f) - (plot.PlotForward * 2.5f));

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "PondWater";
            water.transform.SetParent(pondRoot, false);
            water.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            water.transform.localScale = new Vector3(4.8f, 0.10f, 4.1f);
            pondCollider = water.GetComponent<Collider>();
            var material = CreateWaterMaterial();
            if (material != null) water.GetComponent<Renderer>().material = material;

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PondRim";
            rim.transform.SetParent(pondRoot, false);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localScale = new Vector3(5.35f, 0.06f, 4.65f);
            Destroy(rim.GetComponent<Collider>());
            rim.GetComponent<Renderer>().material.color = new Color(0.22f, 0.30f, 0.18f);

            bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bobber.name = "Fishing_Bobber";
            bobber.transform.SetParent(pondRoot, false);
            bobber.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            bobber.transform.localScale = Vector3.one * 0.20f;
            Destroy(bobber.GetComponent<Collider>());
            bobber.GetComponent<Renderer>().material.color = new Color(1f, 0.42f, 0.18f);
            bobber.SetActive(false);
        }

        private void RefreshCatches()
        {
            if (state == null) return;
            var count = 0;
            for (var index = 0; index < DailyCatchLimit; index++)
                if (state.IsPickupCollected(DailyCatchId(index))) count++;
            CatchesToday = count;
        }

        private string BuildPrompt()
        {
            if (fishing) return FarmLocalization.Get("fishing.waiting", "Fishing... wait for a bite.");
            if (CatchesToday >= DailyCatchLimit) return FarmLocalization.Get("fishing.limit", "The pond is resting. More fish return tomorrow.");
            if (!IsPrimeTime()) return FarmLocalization.Get("fishing.quiet", "Fish are quiet now. Try Morning or Dusk.");
            var phase = plot?.DayClock?.Phase ?? FarmDayClock.ResolvePhase(state.MinutesOfDay);
            var weather = CurrentWeather;
            return FarmLocalization.Format("fishing.prompt_conditions", "Pond: click or press F to fish ({0}/{1} catches today)\n{2}", CatchesToday, DailyCatchLimit, FarmFishingCatalog.ConditionHint(state.DayNumber, phase, weather));
        }

        private void TryBeginFishing()
        {
            if (CatchesToday >= DailyCatchLimit)
            {
                hud?.ShowSystemToast(FarmLocalization.Get("fishing.limit", "The pond is resting. More fish return tomorrow."), true);
                return;
            }
            if (!IsPrimeTime())
            {
                hud?.ShowSystemToast(FarmLocalization.Get("fishing.quiet", "Fish are quiet now. Try Morning or Dusk."), true);
                return;
            }
            if (!FarmSessionTime.IsSimulationAuthority)
            {
                FarmSessionIntentBus.Raise(FarmSessionIntentKind.Fishing, "Player", "action=catch;pond=starter");
                hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
                return;
            }

            fishing = true;
            biteCountdown = CurrentWeather == FarmWeather.Rain ? 0.85f : 1.35f;
            if (bobber != null) bobber.SetActive(true);
            hud?.ShowSystemToast(FarmLocalization.Get("fishing.cast", "Line cast. Wait for a bite..."));
        }

        private void CompleteCatch()
        {
            fishing = false;
            if (bobber != null) bobber.SetActive(false);
            var result = ExecuteHostCatch("host");
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        /// <summary>Host-only catch boundary used by local fishing and peer intents.</summary>
        public FarmSessionCommandResult ExecuteHostCatch(string requestedBy)
        {
            if (!FarmSessionTime.IsSimulationAuthority || state == null || string.IsNullOrWhiteSpace(requestedBy))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("fishing.command.invalid", "Invalid fishing command."));
            RefreshCatches();
            if (CatchesToday >= DailyCatchLimit)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("fishing.limit", "The pond is resting. More fish return tomorrow."));
            if (!IsPrimeTime())
                return new FarmSessionCommandResult(false, FarmLocalization.Get("fishing.quiet", "Fish are quiet now. Try Morning or Dusk."));

            var phase = plot?.DayClock?.Phase ?? FarmDayClock.ResolvePhase(state.MinutesOfDay);
            var catchData = FarmFishingCatalog.Resolve(state.WorldSeed, state.DayNumber, CatchesToday, phase, CurrentWeather);
            if (!state.TryCollectPickup(DailyCatchId(CatchesToday), catchData.ItemId, catchData.Quantity, catchData.Quality))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("fishing.inventory_full", "Inventory full — the fish got away."));
            state.SpendEnergy(FarmTool.Harvest, 1);
            RefreshCatches();
            var item = FarmContentDatabase.GetItem(catchData.ItemId);
            return new FarmSessionCommandResult(true, FarmLocalization.Format("fishing.success_species", "Caught {0} {1}!", FarmItemQualityRules.DisplayName(catchData.Quality), item != null ? item.LocalizedName : catchData.ItemId));
        }

        private FarmWeather CurrentWeather => plot?.WeatherSystem != null ? plot.WeatherSystem.CurrentWeather : FarmWeatherSystem.WeatherForDay(state.WorldSeed, state.DayNumber);
        private bool IsPrimeTime() => (plot?.DayClock?.Phase ?? FarmDayClock.ResolvePhase(state?.MinutesOfDay ?? 0f)) is FarmDayPhase.Morning or FarmDayPhase.Dusk;
        private string DailyCatchId(int index) => $"starter_pond:{Mathf.Max(1, state.DayNumber)}:{index}";

        private bool IsPondUnderPointer()
        {
            if (pondCollider == null || Camera.main == null || Mouse.current == null) return false;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 300f)) return false;
            return hit.collider == pondCollider;
        }

        private static Vector3 PlaceOnGround(Vector3 position)
        {
            if (Physics.Raycast(position + (Vector3.up * 50f), Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore)) position.y = hit.point.y;
            return position;
        }

        private static Material CreateWaterMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            var material = new Material(shader);
            var color = new Color(0.18f, 0.56f, 0.78f, 0.94f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", 0.78f);
            return material;
        }
    }
}
