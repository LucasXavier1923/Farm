# Fishing Loop - First Pass

> Superseded by Meta 25 for the implemented authority, species, weather, season, and cooking details. See `Docs/Fishing-Depth-and-Pond-Ecology.md`.

Fishing is a relaxed pond activity beside the farm. It deliberately shares the
same day, weather, inventory, pickup persistence, and session-authority
boundaries as farming and mining.

## Current implementation

- The Starter Pond supports three catches per farm day.
- Fish only bite in Morning or Dusk; rain gives two fish and shortens the wait.
- Press `F` near the pond or click its water surface, then wait briefly for the
  bobber to resolve the catch.
- Each catch uses a daily pickup identity and therefore persists through save
  and load. Fish are normal sellable inventory goods at the market crate.
- A peer emits an intent and waits for host confirmation instead of modifying
  its own inventory.

## Deferred polish

- Rod tool attachment and animation, fish species/rarity tables, a tension
  minigame, bait, ponds beyond the starter pond, cooking, and final sell-price
  balance.
