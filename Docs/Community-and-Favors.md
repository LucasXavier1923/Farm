# Community and Favors

## Purpose

The first social loop turns a routine delivery into more than a sale. Every completed daily order earns its normal gold and one shared **Favor** with the villager who requested it. This gives a co-op farm a small, visible reason to decide which orders to prioritize together.

This follows a proven cozy-farm pattern: social requests create variety, acknowledgement, and long-term progress without forcing a linear quest line. The implementation stays intentionally light until actual villagers and schedules are authored.

## Live behavior

- Each generated daily order has a deterministic requester: Elara, Bram, or Niko. A board deliberately rotates across all three, so choosing which order to complete is also a relationship choice.
- The Daily Orders window identifies the requester, current Favor, and distance to the next bond.
- Delivering an order grants its normal gold reward plus one Favor.
- A contact's shared farm bond reaches milestones at 2, 5, and 9 Favor.
- The first time each milestone is reached, the farm receives an extra 25, 50, or 100 gold respectively.
- Favor is saved in the farm state and is not awarded twice for an already completed order.

## Co-op and authority

Favor belongs to the **farm**, not a local player. This matches shared deliveries and prevents client divergence while co-op is host-authoritative. Future personal friendships, romance, gifts, and character-specific scenes must instead live in the authenticated player profile at the Supabase authority boundary. The stable contact IDs (`elara`, `bram`, `niko`) are intentionally save-safe for that future split.

## Content and localization boundary

`FarmCommunityCatalog` is the temporary data boundary for three contacts; its IDs must remain stable. Its display names and all UI/feedback copy resolve through `FarmLocalization` and `Assets/Resources/FarmLocalization/en.json`. A later content pass may replace the catalog with ScriptableObject definitions, portraits, schedules, preferences, and authored dialogue without changing the saved favor records.

## Validation

`META17_COMMUNITY_SMOKE` generated and delivered two deterministic boards, verified that one board rotates across all three contacts, reached 2 Favor and bond level 1 with each contact, awarded exactly 75 milestone gold (three 25-gold first bonds), and restored the records from a save.

## Next expansion points

1. A Community Ledger with all contacts, milestones, and recent contributions.
2. Personal profile relationships and gifts through the authoritative backend.
3. Villager schedules, festival attendance, and dialogue that react to farm reputation.
4. Reward choices (recipe, seeds, access) rather than only gold after content balancing exists.
