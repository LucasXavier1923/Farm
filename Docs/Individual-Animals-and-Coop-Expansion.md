# Individual Animals and Coop Expansion

## Purpose

Meta 24 evolves the chicken yard from one anonymous daily reward into a small, shared husbandry system. It uses the existing host-authoritative `AnimalCare` route, persists entirely in the shared farm save, and gives co-op players a reason to divide the day between feed, personal care, material gathering, and collection.

## Individual records

`FarmAnimalRecords` is serialized in `FarmSaveData` version 29. A record has a stable ID, display name, Affection, last individual-care day, trait ID, and favorite item ID. Old saves normalize to Pip and Clover automatically.

| Animal | Trait | Unlock condition | Gameplay reward |
| --- | --- | --- | --- |
| Pip | Steady Layer | 2 Affection and care today | Improves the day's regular egg quality by one tier. |
| Clover | Generous Layer | 3 Affection and care today | Adds one regular egg to that day's collection. |
| Maple | Speckled Layer | Coop expansion, then 2 Affection and care today | Adds one Speckled Egg, a distinct higher-value market item. |

Personal care is available once per animal per game day only after the flock has been fed. The host records a persistent per-animal/day action ID before applying Affection, so duplicate local input and replayed peer commands are rejected.

## Coop expansion

The blue `Coop Expansion Plan` marker is a mechanics station, not a decorative build. While nearby, select Wood or Stone in the hotbar and press `F`.

- 12 Wood
- 8 Stone
- Contributions are shared, host-validated `AnimalCare` commands.
- Completion persists, adds Maple to the flock, and enables the Speckled Egg production path.

The expansion does not consume money or bypass materials. That keeps wood/mining, storage, farming energy, and animal work connected rather than giving the player a cosmetic unlock.

## Daily loop

1. Collect or supply Animal Feed, then feed the flock.
2. Walk to Pip, Clover, or Maple and press `F` for one personal-care action each.
3. Use Wildflower at the Treat Bowl if the extra quality tier is worth more than tea, a gift, or sale value.
4. Collect the nest. Pip can raise quality, Clover can add volume, and Maple can add a separate sellable product.
5. Contribute Wood/Stone when building toward Maple.

## Verification

`META24_ANIMAL_RECORDS_SMOKE` passed in Unity. It covered feed-before-care, duplicate-care rejection, Pip/Clover Affection persistence, all shared expansion contributions, Maple creation, Clover's extra regular egg, Maple's Speckled Egg, and save/restore.

## Localization

All player-facing text belongs to `Assets/Resources/FarmLocalization/en.json`. Gameplay uses keys in the `animals.*` namespace and `item.speckled_egg.name`; no player-facing string requires a code edit for translation.
