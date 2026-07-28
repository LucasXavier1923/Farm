using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// First animal-content loop: collect the daily feed supply, feed the coop,
    /// then collect one egg. Every daily state change uses a persistent world ID.
    /// </summary>
    public sealed class FarmAnimalSystem : MonoBehaviour
    {
        private const float InteractionDistance = 3f;
        private const string FeedItemId = "animal_feed";
        private const string EggItemId = "chicken_egg";
        private const string TreatItemId = "wildflower";

        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmHudController hud;
        private Transform player;
        private Transform coopRoot;
        private Transform feedSupply;
        private Transform feeder;
        private Transform treatBowl;
        private Transform eggNest;
        private Transform expansionPlan;
        private GameObject eggVisual;
        private readonly Dictionary<string, Transform> chickenVisuals = new(StringComparer.OrdinalIgnoreCase);

        private const int ExpansionWoodTarget = 12;
        private const int ExpansionStoneTarget = 8;

        public bool IsFedToday => state != null && state.IsPickupCollected(FeedActionId);
        public bool IsEggAvailable => IsFedToday && state != null && !state.IsPickupCollected(EggPickupId);
        public bool IsTreatedToday => state != null && state.IsPickupCollected(TreatActionId);
        public int CareStreak
        {
            get
            {
                if (state == null) return 0;
                var streak = 0;
                for (var day = Mathf.Max(1, state.DayNumber); day >= 1 && streak < 7; day--)
                {
                    if (!state.IsPickupCollected($"starter_coop:fed:{day}")) break;
                    streak++;
                }
                return streak;
            }
        }

        public bool IsCoopHarmonyToday
        {
            get
            {
                if (state?.Animals?.Chickens == null || state.Animals.Chickens.Count == 0) return false;
                foreach (var chicken in state.Animals.Chickens)
                    if (chicken == null || !HasFavoriteToday(chicken.Id)) return false;
                return true;
            }
        }

        public FarmItemQuality CurrentEggQuality
        {
            get
            {
                var quality = CareStreak >= 5 ? FarmItemQuality.Gold
                    : CareStreak >= 3 ? FarmItemQuality.Silver : FarmItemQuality.Normal;
                // Pip's trait used to be descriptive only. It is now applied at the
                // shared product boundary together with the other care choices.
                if (HasCaredToday("chicken_a") && GetAffection("chicken_a") >= 2) quality = UpgradeQuality(quality);
                if (IsTreatedToday) quality = UpgradeQuality(quality);
                if (IsCoopHarmonyToday) quality = UpgradeQuality(quality);
                return quality;
            }
        }

        public int CurrentEggYield => 1 + (HasCaredToday("chicken_b") && GetAffection("chicken_b") >= 3 ? 1 : 0);
        public int CurrentSpeckledEggYield => state?.Animals.CoopExpanded == true && HasCaredToday("chicken_c") && GetAffection("chicken_c") >= 2 ? 1 : 0;

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmHudController ownerHud, Transform playerTransform)
        {
            if (coopRoot != null) return;
            plot = owner;
            state = gameState;
            hud = ownerHud;
            player = playerTransform;
            if (plot == null || state == null || player == null) return;
            CreateCoop();
            RefreshVisuals();
        }

        private void OnDisable() => plot?.SetExternalPrompt(this, string.Empty);

        private void Update()
        {
            if (state == null || coopRoot == null) return;
            RefreshVisuals();
            plot?.SetExternalPrompt(this, BuildPrompt());
            if (FarmHudController.IsModalOpen || !PressedInteract()) return;

            var nearbyChicken = GetNearbyChickenId();
            if (nearbyChicken != null && IsChickenClosest()) InteractWithChicken(nearbyChicken);
            else
            {
                var station = GetClosestStation();
                if (station == feedSupply) CollectDailyFeed();
                else if (station == feeder) FeedChickens();
                else if (station == treatBowl) TreatChickens();
                else if (station == expansionPlan) ContributeToCoopExpansion();
                else if (station == eggNest) CollectEgg();
            }
        }

        private void CreateCoop()
        {
            coopRoot = new GameObject("Farm_Starter_Chicken_Coop").transform;
            coopRoot.SetParent(transform, true);
            var side = Vector3.Cross(Vector3.up, plot.PlotForward).normalized;
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            // Keep the animal loop as its own test area: quarry/market/crafting
            // occupy the near-right side of the starter plot.
            coopRoot.position = PlaceOnGround(plot.PlotCenter + (side * 18f) + (plot.PlotForward * 9f));
            coopRoot.rotation = Quaternion.LookRotation(-side, Vector3.up);

            var coopPrefab = Resources.Load<GameObject>("FarmContent/ChickenCoop");
            if (coopPrefab != null)
            {
                var coop = Instantiate(coopPrefab, coopRoot);
                coop.name = "ChickenCoop";
                coop.transform.localPosition = Vector3.zero;
                coop.transform.localRotation = Quaternion.identity;
            }
            else CreateFallbackCoop();

            feedSupply = CreateMarker("Daily_Feed_Supply", new Vector3(-2.2f, 0.35f, -1.7f), new Color(0.72f, 0.58f, 0.25f), new Vector3(0.55f, 0.55f, 0.55f));
            feeder = CreateMarker("Chicken_Feeder", new Vector3(1.8f, 0.25f, -1.5f), new Color(0.78f, 0.30f, 0.12f), new Vector3(0.75f, 0.35f, 0.75f));
            treatBowl = CreateMarker("Chicken_Treat_Bowl", new Vector3(-1.4f, 0.22f, 1.35f), new Color(0.76f, 0.45f, 0.82f), new Vector3(0.55f, 0.25f, 0.55f));
            eggNest = CreateMarker("Egg_Nest", new Vector3(1.85f, 0.22f, 1.45f), new Color(0.55f, 0.38f, 0.15f), new Vector3(0.62f, 0.30f, 0.62f));
            expansionPlan = CreateMarker("Coop_Expansion_Plan", new Vector3(-2.7f, 0.15f, 0.1f), new Color(0.20f, 0.54f, 0.72f), new Vector3(0.65f, 0.15f, 0.65f));
            eggVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eggVisual.name = "Chicken_Egg";
            eggVisual.transform.SetParent(eggNest, false);
            eggVisual.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            eggVisual.transform.localScale = new Vector3(0.20f, 0.28f, 0.20f);
            eggVisual.GetComponent<Renderer>().material.color = new Color(1f, 0.90f, 0.66f);
            EnsureChickenVisuals();
        }

        private Transform CreateMarker(string name, Vector3 localPosition, Color color, Vector3 scale)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(coopRoot, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = scale;
            marker.GetComponent<Renderer>().material.color = color;
            return marker.transform;
        }

        private void CreateFallbackCoop()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "ChickenCoop_Fallback";
            body.transform.SetParent(coopRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.transform.localScale = new Vector3(3.6f, 1.6f, 2.8f);
            body.GetComponent<Renderer>().material.color = new Color(0.60f, 0.24f, 0.10f);
        }

        private void CreateChicken(string animalId, string name, Vector3 localPosition, Color color)
        {
            if (chickenVisuals.ContainsKey(animalId)) return;
            var chicken = new GameObject(name).transform;
            chicken.SetParent(coopRoot, false);
            chicken.localPosition = localPosition;
            chickenVisuals[animalId] = chicken;

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(chicken, false);
            body.transform.localScale = new Vector3(0.42f, 0.34f, 0.50f);
            body.GetComponent<Renderer>().material.color = color;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(chicken, false);
            head.transform.localPosition = new Vector3(0f, 0.20f, 0.26f);
            head.transform.localScale = Vector3.one * 0.23f;
            head.GetComponent<Renderer>().material.color = color;

            var beak = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beak.name = "Beak";
            beak.transform.SetParent(chicken, false);
            beak.transform.localPosition = new Vector3(0f, 0.19f, 0.39f);
            beak.transform.localScale = new Vector3(0.10f, 0.07f, 0.12f);
            beak.GetComponent<Renderer>().material.color = new Color(1f, 0.65f, 0.14f);
        }

        private void RefreshVisuals()
        {
            EnsureChickenVisuals();
            if (feedSupply != null) feedSupply.gameObject.SetActive(!HasCollectedDailyFeed);
            if (eggVisual != null) eggVisual.SetActive(IsEggAvailable);
            if (expansionPlan != null) expansionPlan.gameObject.SetActive(state?.Animals.CoopExpanded != true);
            if (eggVisual != null && eggVisual.TryGetComponent<Renderer>(out var eggRenderer))
                eggRenderer.material.color = CurrentEggQuality switch
                {
                    FarmItemQuality.Gold => new Color(1f, 0.72f, 0.18f),
                    FarmItemQuality.Silver => new Color(0.78f, 0.86f, 0.92f),
                    _ => new Color(1f, 0.90f, 0.66f)
                };
        }

        private string BuildPrompt()
        {
            var nearbyChicken = GetNearbyChickenId();
            if (nearbyChicken != null && IsChickenClosest())
            {
                var record = state.Animals.Find(nearbyChicken);
                if (record == null) return string.Empty;
                return HasCaredToday(record.Id)
                    ? HasFavoriteToday(record.Id)
                        ? FarmLocalization.Format("animals.favorite.completed", "{0} has already received a favorite gift today.", record.DisplayName)
                        : SelectedItemId == record.FavoriteItemId
                            ? FarmLocalization.Format("animals.favorite.ready", "{0} recognizes their favorite, {1}. Press F to offer it.", record.DisplayName, FavoriteItemName(record))
                            : FarmLocalization.Format("animals.favorite.hint", "{0} loves {1}. Select it in the hotbar and press F for a favorite gift.", record.DisplayName, FavoriteItemName(record))
                    : !IsFedToday
                        ? FarmLocalization.Format("animals.care.requires_feed", "Feed the flock before caring for {0}.", record.DisplayName)
                        : FarmLocalization.Format("animals.care.prompt", "{0} - {1} Affection: press F to care ({2}).", record.DisplayName, record.Affection, TraitDisplayName(record.TraitId));
            }
            var station = GetClosestStation();
            if (station == feedSupply && !HasCollectedDailyFeed)
                return FarmLocalization.Get("animals.supply.prompt", "Feed supply: press F to collect animal feed.");
            if (station == feeder)
            {
                if (IsFedToday) return FarmLocalization.Get("animals.fed", "Chickens are fed for today.");
                return FarmLocalization.Format("animals.feeder.prompt", "Chicken feeder: press F to feed ({0} feed in inventory)", state.GetQuantity(FeedItemId));
            }
            if (station == treatBowl) return IsTreatedToday
                ? FarmLocalization.Get("animals.treated", "Chickens enjoyed a treat today.")
                : FarmLocalization.Format("animals.treat.prompt", "Treat bowl: press F to offer a Wildflower ({0} in inventory).", state.GetQuantity(TreatItemId)) +
                  FarmLocalization.Format("animals.flock", " Pip and Clover are waiting nearby.");
            if (station == expansionPlan)
            {
                var records = state.Animals;
                return records.CoopExpanded
                    ? FarmLocalization.Get("animals.expansion.complete", "Coop expansion complete: Maple has joined the flock.")
                    : FarmLocalization.Format("animals.expansion.prompt", "Coop expansion: Wood {0}/{1}, Stone {2}/{3}. Select Wood or Stone and press F.", records.ExpansionWood, ExpansionWoodTarget, records.ExpansionStone, ExpansionStoneTarget);
            }
            if (station == eggNest)
            {
                if (IsEggAvailable) return FarmLocalization.Get("animals.egg.prompt", "Egg nest: press F to collect an egg.");
                return IsFedToday
                    ? FarmLocalization.Get("animals.nest.empty", "Egg nest: collected for today.")
                    : FarmLocalization.Get("animals.nest.waiting", "Feed the chickens to receive an egg.");
            }
            return string.Empty;
        }

        private bool HasCollectedDailyFeed => state != null && state.IsPickupCollected(DailyFeedId);

        private void CollectDailyFeed()
        {
            if (!RequireAuthority("collect_feed")) return;
            var result = ExecuteHostCareAction("collect_feed", "host");
            if (result.Succeeded) hud?.ShowPickupToast(FeedItemId, 1);
            else hud?.ShowSystemToast(result.Message, true);
        }

        private void FeedChickens()
        {
            if (!RequireAuthority("feed_chickens")) return;
            var result = ExecuteHostCareAction("feed_chickens", "host");
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        private void CollectEgg()
        {
            if (!RequireAuthority("collect_egg")) return;
            var result = ExecuteHostCareAction("collect_egg", "host");
            if (result.Succeeded) hud?.ShowPickupToast(EggItemId, 1);
            else hud?.ShowSystemToast(result.Message, true);
        }

        private void TreatChickens()
        {
            if (!RequireAuthority("treat_chickens")) return;
            var result = ExecuteHostCareAction("treat_chickens", "host");
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        private void InteractWithChicken(string animalId)
        {
            var record = state?.Animals.Find(animalId);
            if (record == null) return;
            var action = HasCaredToday(animalId) && string.Equals(SelectedItemId, record.FavoriteItemId, StringComparison.OrdinalIgnoreCase)
                ? $"favorite_chicken:{animalId}"
                : $"care_chicken:{animalId}";
            if (!RequireAuthority(action)) return;
            var result = ExecuteHostCareAction(action, "host");
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        private void ContributeToCoopExpansion()
        {
            var entry = state?.SelectedHotbarEntry ?? string.Empty;
            if (!entry.StartsWith(FarmGameState.ItemPrefix, StringComparison.OrdinalIgnoreCase))
            {
                hud?.ShowSystemToast(FarmLocalization.Get("animals.expansion.material_required", "Select Wood or Stone from the hotbar."), true);
                return;
            }
            var itemId = entry[FarmGameState.ItemPrefix.Length..];
            var action = $"expand_coop:{itemId}";
            if (!RequireAuthority(action)) return;
            var result = ExecuteHostCareAction(action, "host");
            hud?.ShowSystemToast(result.Message, !result.Succeeded);
        }

        /// <summary>
        /// Host-only, idempotent care boundary for local input and peer intents.
        /// The caller chooses an action name; this method validates every state and
        /// inventory transition against the host's persisted daily identifiers.
        /// </summary>
        public FarmSessionCommandResult ExecuteHostCareAction(string action, string requestedBy)
        {
            if (!FarmSessionTime.IsSimulationAuthority)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."));
            if (state == null || string.IsNullOrWhiteSpace(requestedBy))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.command.invalid", "Invalid animal-care command."));

            var command = action?.Trim() ?? string.Empty;
            const string carePrefix = "care_chicken:";
            const string favoritePrefix = "favorite_chicken:";
            const string expansionPrefix = "expand_coop:";
            if (command.StartsWith(carePrefix, StringComparison.OrdinalIgnoreCase))
                return TryCareChicken(command.Substring(carePrefix.Length));
            if (command.StartsWith(favoritePrefix, StringComparison.OrdinalIgnoreCase))
                return TryOfferFavorite(command.Substring(favoritePrefix.Length));
            if (command.StartsWith(expansionPrefix, StringComparison.OrdinalIgnoreCase))
                return TryContributeCoopExpansion(command.Substring(expansionPrefix.Length));

            return command switch
            {
                "collect_feed" => TryCollectDailyFeed(),
                "feed_chickens" => TryFeedChickens(),
                "treat_chickens" => TryTreatChickens(),
                "collect_egg" => TryCollectEgg(),
                _ => new FarmSessionCommandResult(false, FarmLocalization.Get("animals.command.invalid", "Invalid animal-care command."))
            };
        }

        private FarmSessionCommandResult TryCollectDailyFeed()
        {
            if (HasCollectedDailyFeed)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.supply.collected", "Today's feed supply was already collected."));
            if (!state.TryCollectPickup(DailyFeedId, FeedItemId, 1))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.inventory_full", "Inventory full - leave space for animal goods."));
            return new FarmSessionCommandResult(true, FarmLocalization.Get("animals.feed_collected", "Animal feed collected."));
        }

        private FarmSessionCommandResult TryFeedChickens()
        {
            if (IsFedToday)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.fed", "Chickens are fed for today."));
            if (!state.TryRemoveItem(FeedItemId, 1))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.feed_missing", "You need animal feed first."));
            if (!state.TryRecordWorldAction(FeedActionId))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.fed", "Chickens are fed for today."));
            return new FarmSessionCommandResult(true, FarmLocalization.Format("animals.feed_success_streak", "Chickens are fed. Care streak: {0} day(s).", CareStreak));
        }

        private FarmSessionCommandResult TryCollectEgg()
        {
            if (!IsFedToday)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.nest.waiting", "Feed the chickens to receive an egg."));
            if (!IsEggAvailable)
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.nest.empty", "Egg nest: collected for today."));
            var quality = CurrentEggQuality;
            var regularEggs = CurrentEggYield;
            if (!state.TryCollectPickup(EggPickupId, EggItemId, regularEggs, quality))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.inventory_full", "Inventory full - leave space for animal goods."));
            var speckledEggs = CurrentSpeckledEggYield;
            if (speckledEggs > 0 && !state.AddItem("speckled_egg", speckledEggs))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.inventory_full", "Inventory full - leave space for animal goods."));
            return new FarmSessionCommandResult(true, speckledEggs > 0
                ? FarmLocalization.Format("animals.egg_collected_variety", "Collected {0} {1} egg(s) and {2} Speckled Egg.", regularEggs, FarmItemQualityRules.DisplayName(quality), speckledEggs)
                : FarmLocalization.Format("animals.egg_collected_amount", "Collected {0} {1} egg(s).", regularEggs, FarmItemQualityRules.DisplayName(quality)) +
                   (IsCoopHarmonyToday ? FarmLocalization.Get("animals.egg_harmony_suffix", " Coop Harmony improved the flock's quality.") : string.Empty));
        }

        private FarmSessionCommandResult TryTreatChickens()
        {
            if (IsTreatedToday) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.treated", "Chickens enjoyed a treat today."));
            if (!state.TryRemoveItem(TreatItemId, 1)) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.treat_missing", "You need a Wildflower for a chicken treat."));
            if (!state.TryRecordWorldAction(TreatActionId)) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.treated", "Chickens enjoyed a treat today."));
            return new FarmSessionCommandResult(true, FarmLocalization.Get("animals.treat_success", "Chickens loved the Wildflower. Today's egg quality improved."));
        }

        private FarmSessionCommandResult TryCareChicken(string animalId)
        {
            var chicken = state.Animals.Find(animalId);
            if (chicken == null) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.care.unknown", "That animal is not part of this coop."));
            if (!IsFedToday) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.care.feed_first", "Feed the flock before individual care."));
            if (HasCaredToday(animalId)) return new FarmSessionCommandResult(false, FarmLocalization.Format("animals.care.completed", "{0} has already received personal care today.", chicken.DisplayName));
            if (!state.TryRecordWorldAction(CareActionId(animalId))) return new FarmSessionCommandResult(false, FarmLocalization.Format("animals.care.completed", "{0} has already received personal care today.", chicken.DisplayName));
            chicken.LastCareDay = state.DayNumber;
            chicken.Affection = Mathf.Clamp(chicken.Affection + 1, 0, 10);
            state.NotifyAnimalsChanged();
            return new FarmSessionCommandResult(true, FarmLocalization.Format("animals.care.success", "{0} trusts the farm more. Affection: {1}. {2}", chicken.DisplayName, chicken.Affection, TraitBenefitText(chicken)));
        }

        private FarmSessionCommandResult TryOfferFavorite(string animalId)
        {
            var chicken = state.Animals.Find(animalId);
            if (chicken == null) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.care.unknown", "That animal is not part of this coop."));
            if (!IsFedToday) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.care.feed_first", "Feed the flock before individual care."));
            if (!HasCaredToday(animalId)) return new FarmSessionCommandResult(false, FarmLocalization.Format("animals.favorite.care_first", "Care for {0} before offering a favorite gift.", chicken.DisplayName));
            if (HasFavoriteToday(animalId)) return new FarmSessionCommandResult(false, FarmLocalization.Format("animals.favorite.completed", "{0} has already received a favorite gift today.", chicken.DisplayName));
            if (string.IsNullOrWhiteSpace(chicken.FavoriteItemId) || !state.TryRemoveItem(chicken.FavoriteItemId, 1))
                return new FarmSessionCommandResult(false, FarmLocalization.Format("animals.favorite.missing", "You need {0} for {1}.", FavoriteItemName(chicken), chicken.DisplayName));
            if (!state.TryRecordWorldAction(FavoriteActionId(animalId)))
                return new FarmSessionCommandResult(false, FarmLocalization.Format("animals.favorite.completed", "{0} has already received a favorite gift today.", chicken.DisplayName));
            chicken.Affection = Mathf.Clamp(chicken.Affection + 2, 0, 10);
            state.NotifyAnimalsChanged();
            return new FarmSessionCommandResult(true, IsCoopHarmonyToday
                ? FarmLocalization.Format("animals.favorite.harmony", "{0} loved the gift. Coop Harmony improves today's egg quality.", chicken.DisplayName)
                : FarmLocalization.Format("animals.favorite.success", "{0} loved the gift. Affection: {1}.", chicken.DisplayName, chicken.Affection));
        }

        private FarmSessionCommandResult TryContributeCoopExpansion(string itemId)
        {
            var records = state.Animals;
            if (records.CoopExpanded) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.expansion.complete", "Coop expansion complete: Maple has joined the flock."));
            if (itemId is not ("wood" or "stone")) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.expansion.material_required", "Select Wood or Stone from the hotbar."));
            var isWood = itemId == "wood";
            if ((isWood && records.ExpansionWood >= ExpansionWoodTarget) || (!isWood && records.ExpansionStone >= ExpansionStoneTarget))
                return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.expansion.material_filled", "That expansion material is already complete."));
            if (!state.TryRemoveItem(itemId, 1)) return new FarmSessionCommandResult(false, FarmLocalization.Get("animals.expansion.missing", "That material is no longer in the inventory."));
            if (isWood) records.ExpansionWood++; else records.ExpansionStone++;
            var complete = records.ExpansionWood >= ExpansionWoodTarget && records.ExpansionStone >= ExpansionStoneTarget;
            if (complete)
            {
                records.CoopExpanded = true;
                records.EnsureNormalized();
                EnsureChickenVisuals();
            }
            state.NotifyAnimalsChanged();
            return new FarmSessionCommandResult(true, complete
                ? FarmLocalization.Get("animals.expansion.success", "Coop expanded! Maple joined the flock and can produce Speckled Eggs.")
                : FarmLocalization.Get("animals.expansion.contributed", "Coop expansion material accepted."));
        }

        private bool RequireAuthority(string action)
        {
            if (FarmSessionTime.IsSimulationAuthority) return true;
            FarmSessionIntentBus.Raise(FarmSessionIntentKind.AnimalCare, "Player", $"action={action};coop=starter");
            hud?.ShowSystemToast(FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation."), true);
            return false;
        }

        private bool PressedInteract()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) return true;
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        }

        private bool IsNear(Transform target) => target != null && player != null && Vector3.Distance(player.position, target.position) <= InteractionDistance;
        private string DailyFeedId => $"starter_coop:feed_supply:{Mathf.Max(1, state.DayNumber)}";
        private string FeedActionId => $"starter_coop:fed:{Mathf.Max(1, state.DayNumber)}";
        private string EggPickupId => $"starter_coop:egg:{Mathf.Max(1, state.DayNumber)}";
        private string TreatActionId => $"starter_coop:treated:{Mathf.Max(1, state.DayNumber)}";
        private string CareActionId(string animalId) => $"starter_coop:care:{animalId}:{Mathf.Max(1, state.DayNumber)}";
        private string FavoriteActionId(string animalId) => $"starter_coop:favorite:{animalId}:{Mathf.Max(1, state.DayNumber)}";

        private bool HasCaredToday(string animalId) => state != null && state.IsPickupCollected(CareActionId(animalId));
        private bool HasFavoriteToday(string animalId) => state != null && state.IsPickupCollected(FavoriteActionId(animalId));
        private int GetAffection(string animalId) => state?.Animals.Find(animalId)?.Affection ?? 0;
        private string SelectedItemId
        {
            get
            {
                var entry = state?.SelectedHotbarEntry ?? string.Empty;
                return entry.StartsWith(FarmGameState.ItemPrefix, StringComparison.OrdinalIgnoreCase)
                    ? entry[FarmGameState.ItemPrefix.Length..]
                    : string.Empty;
            }
        }
        private static string FavoriteItemName(FarmAnimalRecord record) =>
            FarmContentDatabase.GetItem(record?.FavoriteItemId)?.LocalizedName ?? record?.FavoriteItemId ?? string.Empty;

        private Transform GetNearbyChicken()
        {
            string nearestId = null;
            var nearestDistance = InteractionDistance;
            foreach (var pair in chickenVisuals)
            {
                if (pair.Value == null) continue;
                var distance = Vector3.Distance(player.position, pair.Value.position);
                if (distance <= nearestDistance) { nearestDistance = distance; nearestId = pair.Key; }
            }
            return nearestId == null ? null : chickenVisuals[nearestId];
        }

        private Transform GetClosestStation()
        {
            Transform closest = null;
            var closestDistance = InteractionDistance;
            ConsiderStation(feedSupply, ref closest, ref closestDistance);
            ConsiderStation(feeder, ref closest, ref closestDistance);
            ConsiderStation(treatBowl, ref closest, ref closestDistance);
            ConsiderStation(expansionPlan, ref closest, ref closestDistance);
            ConsiderStation(eggNest, ref closest, ref closestDistance);
            return closest;
        }

        private bool IsChickenClosest()
        {
            var chicken = GetNearbyChicken();
            if (chicken == null) return false;
            var station = GetClosestStation();
            return station == null || Vector3.Distance(player.position, chicken.position) <= Vector3.Distance(player.position, station.position);
        }

        private void ConsiderStation(Transform station, ref Transform closest, ref float closestDistance)
        {
            if (station == null || !station.gameObject.activeInHierarchy || player == null) return;
            var distance = Vector3.Distance(player.position, station.position);
            if (distance <= closestDistance) { closestDistance = distance; closest = station; }
        }

        private string GetNearbyChickenId()
        {
            var chicken = GetNearbyChicken();
            if (chicken == null) return null;
            foreach (var pair in chickenVisuals) if (pair.Value == chicken) return pair.Key;
            return null;
        }

        private void EnsureChickenVisuals()
        {
            if (coopRoot == null || state == null) return;
            foreach (var record in state.Animals.Chickens)
            {
                if (record == null || chickenVisuals.ContainsKey(record.Id)) continue;
                if (record.Id == "chicken_a") CreateChicken(record.Id, "Chicken_Pip", new Vector3(-0.90f, 0.17f, 0.70f), new Color(0.92f, 0.80f, 0.52f));
                else if (record.Id == "chicken_b") CreateChicken(record.Id, "Chicken_Clover", new Vector3(0.65f, 0.17f, 0.85f), new Color(0.78f, 0.42f, 0.22f));
                else if (record.Id == "chicken_c") CreateChicken(record.Id, "Chicken_Maple", new Vector3(0.1f, 0.17f, -0.85f), new Color(0.56f, 0.30f, 0.18f));
            }
        }

        private string TraitDisplayName(string traitId) => traitId switch
        {
            "steady_layer" => FarmLocalization.Get("animals.trait.steady.name", "Steady Layer"),
            "generous_layer" => FarmLocalization.Get("animals.trait.generous.name", "Generous Layer"),
            "speckled_layer" => FarmLocalization.Get("animals.trait.speckled.name", "Speckled Layer"),
            _ => FarmLocalization.Get("animals.trait.kind.name", "Kind Companion")
        };

        private string TraitBenefitText(FarmAnimalRecord chicken) => chicken.TraitId switch
        {
            "steady_layer" => FarmLocalization.Get("animals.trait.steady.benefit", "At 2 Affection, Pip's daily care improves egg quality."),
            "generous_layer" => FarmLocalization.Get("animals.trait.generous.benefit", "At 3 Affection, Clover's daily care adds an extra egg."),
            "speckled_layer" => FarmLocalization.Get("animals.trait.speckled.benefit", "At 2 Affection, Maple's daily care produces a Speckled Egg."),
            _ => string.Empty
        };

        private static FarmItemQuality UpgradeQuality(FarmItemQuality baseQuality) =>
            baseQuality == FarmItemQuality.Normal ? FarmItemQuality.Silver : FarmItemQuality.Gold;

        private static Vector3 PlaceOnGround(Vector3 position)
        {
            var lowestGroundY = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(position + (Vector3.up * 50f), Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.point.y < lowestGroundY) lowestGroundY = hit.point.y;
            if (lowestGroundY < float.MaxValue) position.y = lowestGroundY;
            return position;
        }
    }
}
