# Current Development Goal — Gameplay Depth and World Authoring

Updated: 27 July 2026.

## Live presentation map

The companion FigJam system map is [Live Systems Map](Live-Systems-Map.md). It is the visual handoff view for Lucas and the co-publisher; the Markdown documents remain the implementation source of truth.

## Continuous development rule

The active roadmap is now a continuous, evidence-based gameplay loop. Each completed meta establishes the next one. A candidate must add a meaningful player activity, reward that activity, give the farm a choice, and connect to an existing system; decorative work alone is not a meta. The loop continues until the project owner explicitly stops it.

The active roadmap is the gameplay-depth and world-authoring tranche, Metas 11–16. The project remains a 3D co-op-first farm prototype. It must not become a single-player-only foundation: menus do not pause world time, and future Steam peers will use the already accepted host-authoritative boundary.

## Accepted foundation — Metas 7–10

Metas 7–10 are preserved as accepted source foundations, not the active delivery target.

- **Meta 7: Co-op ready.** Host/peer intent routing, shared snapshots, sender validation, loopback verification, and a local Steam P2P adapter exist. A real 2–4-account Steam session remains a live validation gate.
- **Meta 8: Global economy foundation.** The asynchronous Unity contract, mock, Supabase migrations/RPCs, Edge Function boundary, RLS, and idempotency design exist. A real Supabase project deployment and concurrent transaction test remain live validation gates.
- **Meta 9: Steam integration.** Steamworks.NET lobby, invite, presence, and P2P code is integrated. The configured App ID remains `0`, so no live Steam claim may be made.
- **Meta 10: World & presentation.** `Assets/_Project/Scenes/FarmWorld.unity` is the editable playable copy of the prototype. Final decorative world authoring is deliberately deferred until the mechanics kit is solid.

## Active tranche

### Meta 11 — A Day on the Farm — complete

The day rhythm, prompts, weather/day presentation, sleep flow, and non-pausing menu behavior are in the prototype. `Docs/Daily-Farm-Loop.md` records the player-facing loop and validation.

### Meta 12 — Meaningful Farming — complete

The existing season, greenhouse, quality, pest, and crop-affinity systems are now complemented by an authoritative Compost decision. Compost is a drag-to-hotbar item; it is validated asynchronously before inventory and tile visuals change, gives +1 yield and quality score, persists per crop cycle, and is consumed after harvest. The backend smoke covered prepare → compost → plant → water → harvest with the expected yield, and FarmWorld started with the feature available. See `Docs/Meaningful-Farming.md`.

### Meta 13 — Animals & Care — complete

The starter chicken loop now has auditable daily feed, care streak, quality production, reward collection, persistence, and a dedicated host-routed `AnimalCare` intent. The FarmWorld smoke test exercised a peer-shaped host request through feed, care, egg collection, and duplicate rejection. See `Docs/Animals-Loop.md`.

### Meta 14 — Crafting, Production & Building — complete

Recipes and the workbench processor now have a host-routed `Production` intent while persistent building remains host-managed through its explicit permissions. FarmWorld validation completed a peer-shaped processing start/collect, crafted a scarecrow kit, and placed a persistent buildable through the placement validator. See `Docs/Crafting-Production-Building.md`.

### Meta 15 — Progression & Specialization — complete

The former changeable focus is now a strict profile specialization: every track reaches Level 2, one committed track can reach Level 3, and the host routes `Progression` requests. A smoke test verified the cap, the committed Level 3 track, and rejected a later change. See `Docs/Progression-Specialization.md`.

### Meta 16 — World Authoring Kit — complete

`FarmWorldAuthoringKit` is attached to `Farm_Test_System` in FarmWorld with five named scene reservations: spawn, fields, production, animals, and north expansion. Its validation passed with no messages. See `Docs/World-Authoring-Kit.md`.

### Meta 17 â€” Community & Relationship Loop â€” complete

Daily orders now have deterministic community requesters and build shared farm Favor. Every delivery retains its gold reward, adds Favor, and can unlock milestone gold at 2, 5, and 9 Favor. The state persists, UI identifies the requester and next bond, and the system is intentionally farm-shared for co-op while future personal relationships are reserved for authoritative player profiles. The smoke test delivered six orders, verified both milestones and persistence. See `Docs/Community-and-Favors.md`.

### Meta 18 â€” Exploration, Foraging & Discovery â€” next

Deterministic daily forage opportunities now vary by weather and season, are collected through the host-only pickup path, record collection discoveries, and persist safely. Spring rain favors Forest Mushroom, while other seasons select from Wildflower, wood, stone, and mushroom routes. The smoke test covered deterministic generation, duplicate rejection, discovery, and save/load. See `Docs/Exploration-Foraging-and-Discovery.md`.

### Meta 19 â€” Meals & Home Comfort â€” complete

Wildflower Tea and Mushroom Stew turn seasonal forage into practical energy recovery. Recipes use the existing shared production boundary; completed meals work through inventory/hotbar drag-drop and use `R` outside menus. A host-routed `Consumption` intent validates the item and energy capacity. The smoke test covered craft, energy spend, exact restoration, inventory removal, and duplicate rejection. See `Docs/Meals-and-Home-Comfort.md`.

### Meta 20 â€” Community Gifts â€” complete

The mailbox now contains a Community Gifts desk with an explicit recipient choice, favorite meal requirements, a one-gift-per-neighbor daily limit, shared Favor, milestone rewards, persistence, and host-routed peer intent. The smoke test covered valid gifts, invalid gifts, daily rejection, milestone rewards, and save/load. See `Docs/Community-Gifts.md`.

### Meta 21 â€” Seasonal Festival Contributions â€” next

The seasonal calendar now runs Harvest Share on day 6: a shared six-crop target with live prompt, host-routed contribution, $150 completion reward, shared Favor, duplicate prevention, and save persistence. The smoke test covered all six contributions, one-time reward, Favor, rejection, and restore. See `Docs/Seasonal-Festival-Contributions.md`.

### Meta 22 â€” Community Projects & Farm Landmarks â€” next

Market Route is the first persistent community project. It accepts 15 Wood, 10 Stone, and 3 Refined Stone through a host-routed shared contribution path, persists its stages, and unlocks storage-backed daily order delivery before Commerce level 3. The smoke test completed all materials, proved the logistics unlock, and restored the state. See `Docs/Community-Projects-and-Market-Route.md`.

### Meta 23 â€” Animal Enrichment â€” complete

The starter coop now includes a daily Wildflower Treat choice. It competes with tea, gifts, and selling, is host-routed through AnimalCare, persists through the existing daily action IDs, and raises egg quality by one tier. The smoke test confirmed the item cost, quality improvement, and duplicate rejection. See `Docs/Animal-Enrichment.md`.

### Meta 24 â€” Individual Animal Records & Coop Expansion â€” next

Pip and Clover now persist as individual animals with Affection, daily care, and production traits. A shared, host-routed Wood/Stone expansion adds Maple and a distinct Speckled Egg economy path. The smoke test verified duplicate rejection, traits, expansion, output variety, and save/load. See `Docs/Individual-Animals-and-Coop-Expansion.md`.

### Meta 25 - Fishing Depth and Pond Ecology - in progress

The pond now uses a host-routed Fishing intent, deterministic seasonal and weather-aware species, persistent catch IDs, a three-catch rest rhythm, collection discovery, localized condition hints, and a Fish Skewer energy route. `META25_FISHING_SMOKE` passed. See `Docs/Fishing-Depth-and-Pond-Ecology.md`.

### Meta 26 - Farm Quests and Skillful Requests - in progress

The shared board now asks for deterministic Farming, Animal Care, Fishing, and Workshop goods using a generic typed ItemId contract. Delivery is host-validated, peers use the DailyOrder intent, storage routing remains available, and `META26_ORDER_SMOKE` passed. See `Docs/Shared-Request-Board.md`.

### Meta 27 - Recipe Discovery and Meaningful Cooking - in progress

Recipe discovery is now stored in the shared farm save. Crop, forage, fish, and animal ingredients unlock localized meals; Pumpkin Roast and Farm Omelet expand the choices. Crafting and consumption remain host-routed, and `META27_RECIPE_SMOKE` passed. See `Docs/Recipe-Discovery-and-Meaningful-Cooking.md`.

### Meta 28 - Land Stewardship and Regenerative Gathering - complete

Resting quarry outcrops can now be renewed with Compost during rain. The host persists per-node preparation, and the next returned mining cycle yields an extra Stone before resetting. This connects farm waste, global weather, gathering, construction materials, recipes, and requests without a scenery-only addition. `META28_STEWARDSHIP_SMOKE` passed. See `Docs/Land-Stewardship-and-Regenerative-Gathering.md`.

### Meta 29 - Farm Automation and Morning Routines - complete

The workbench now uses a persistent three-job shared queue with serialized game-time completion. The existing Farm Shed gains a logistics purpose: at the host's morning routine it routes completed workbench output into shared storage when there is room. This reduces repetition while preserving the player's material and scheduling decisions. `META29_AUTOMATION_SMOKE` passed. See `Docs/Farm-Automation-and-Morning-Routines.md`.

### Meta 30 - Crop Care, Soil Health, and Seasonal Mastery - complete

Plots now remember their previous harvest. Alternating crops activates a visible rotation bonus that improves the next crop's quality; repeating a crop remains valid but does not gain that bonus. Rotation persists in tile saves and is evaluated by the asynchronous authoritative core alongside season, greenhouse, Compost, and Harvesting mastery. `META30_ROTATION_SMOKE` passed. See `Docs/Crop-Care-Soil-Health-and-Seasonal-Mastery.md`.

### Meta 31 - Farm Animals, Products, and Artisan Identity - complete

The coop now supports personal favorite gifts after daily care. Each chicken gains persistent Affection; giving every active chicken a favorite creates optional Coop Harmony and improves egg quality. Pip's quality trait is now genuinely applied, while Clover and Maple retain their distinct output traits. `META31_ANIMAL_SMOKE` passed. See `Docs/Animal-Bonds-and-Product-Quality.md`.

### Meta 32 - Artisan Goods and Player-Owned Production Choices - complete

The shared workbench now turns Eggs, Pumpkins, and Pond Fish into timed artisan products while retaining their input quality. Egg Preserve, Pumpkin Jam, and Smoked Fish create an intentional raw-sale versus time-and-queue decision, work with Farm Shed morning storage, and travel through the host Production intent. `META32_ARTISAN_SMOKE` passed. See `Docs/Artisan-Goods-and-Player-Owned-Production.md`.

### Meta 33 - Cooking, Consumption, and Cozy Hospitality - complete

Egg Preserve, Pumpkin Jam, and Smoked Fish can now recover Energy or serve as the daily favorite artisan gift for Elara, Niko, and Bram. This connects animals, farming, fishing, artisan time, and shared community Favor without making sale value the only answer. `META33_HOSPITALITY_SMOKE` passed. See `Docs/Cooking-Consumption-and-Cozy-Hospitality.md`.

### Meta 34 - Exploration Routes, Risk, and Reward - complete

The farm now creates one weather-and-time-gated route per day: rainy dusk for Mushrooms, clear morning for Wildflowers, or cloudy afternoon for Wood. Routes are deterministic, host-authoritative, consume 8 Energy, and use persistent pickup IDs to prevent duplication. `META34_EXPLORATION_SMOKE` passed. See `Docs/Exploration-Routes-Risk-and-Reward.md`.

### Meta 35 - Seasonal Goals, Collections, and Long-Term Discovery - complete

The Collection Book now turns discoveries across farming, animals, fishing, forage, artisan production, and projects into optional shared breadth milestones. It communicates the next reward, claims through a host-authoritative `CollectionMilestone` intent, persists its one-time reward mask in save version 33, and never pauses the day. `META35_COLLECTION_SMOKE` passed. See `Docs/Seasonal-Goals-Collections-and-Long-Term-Discovery.md`.

### Meta 36 - Co-op Roles and Collaborative Work Orders - complete

Farm mastery remains shared technology, while roles now belong to player IDs. Cultivator, Gatherer, and Steward profiles can coordinate matching crop, nature, and workshop deliveries for one shared daily teamwork reward; any player may still perform any action. Profiles are capped at four, host-authoritative, and persisted in save version 34. `META36_ROLE_SMOKE` passed. See `Docs/Coop-Roles-and-Collaborative-Work-Orders.md`.

### Meta 37 - Farm Reputation and Meaningful Neighbor Unlocks - complete

Shared bond level 2 with each neighbor now unlocks a tangible farm benefit: Elara adds a seed to every pack, Bram grants a fourth pond catch, and Niko expands the artisan queue to four jobs. Benefits are derived from saved Favor, visible on the order board, and apply only through existing host-authoritative actions. `META37_NEIGHBOR_UNLOCK_SMOKE` passed. See `Docs/Farm-Reputation-and-Neighbor-Unlocks.md`.

### Meta 38 - Seasonal Forecasts and Preparedness Choices - complete

The Seasonal Planner now turns tomorrow's known weather into a one-day shared route preparation. Rain, clear, and cloudy forecasts respectively prepare the matching mushroom, wildflower, or wood route for +1 item and -2 Energy; doing nothing leaves the normal route unchanged. The plan is host-authoritative, save version 35 persists it, and it expires after its target day. `META38_FORECAST_SMOKE` passed. See `Docs/Seasonal-Forecasts-and-Preparedness.md`.

### Meta 39 - Farmhouse Comfort, Rest, and Meaningful Recovery - complete

The bed now supports a shared Evening Tea ritual. One Wildflower Tea prepares the next morning with three Comfort charges, each reducing one Energy cost by one. The host validates the item, the state persists in save version 36, the bed UI explains the benefit, and the system complements rather than replaces food or sleep. See `Docs/Farmhouse-Comfort-and-Rest.md`.

### Meta 40 - Evening Preparation Choice - complete

Tomorrow can receive one shared evening preparation: Comfort from Evening Tea or the existing weather-matched route plan. The options are mutually exclusive for the current evening, host-authoritative, localized, and visible at the bed. This gives co-op players a final strategic choice before they ready up, while retaining the non-pausing day rule. See `Docs/Evening-Preparation-Choice.md`.

## Paused after Meta 40

The current gameplay-depth tranche is intentionally paused here at the project owner's request. The next candidate meta must be selected after a hands-on playtest of Metas 39 and 40; no further automatic gameplay expansion is active.
