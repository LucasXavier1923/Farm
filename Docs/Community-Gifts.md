# Community Gifts

## Purpose

Gifts turn crafted cozy items into a deliberate social investment. The farm chooses a recipient at the mailbox, trades one useful item for a larger Favor gain, and decides whether to prioritize the villagers most relevant to its current plans.

This is a shared-farm system, not a substitute for personal friendship. It gives co-op players a visible joint decision now while preserving personal relationships, romance, and dialogue for the future authenticated player-profile layer.

## Live interaction

Open the mailbox with **F**. Under the selected letter, the **Community Gifts** desk displays all three neighbors and their current favorite gift.

| Neighbor | Favorite gift | Favor |
| --- | --- | ---: |
| Elara | Wildflower Tea | +3 |
| Bram | Mushroom Stew | +3 |
| Niko | Wildflower Tea | +3 |

- A button is enabled only when that gift exists in the shared inventory.
- A neighbor accepts one favorite gift per farm day.
- The host removes the item, applies Favor, then evaluates the usual 2/5/9 Favor bond milestones.
- Reaching a new milestone grants the same 25/50/100 gold community reward as an order.

## Authority and persistence

Peers emit a `CommunityGift` intent with contact and item IDs. The host validates the contact, exact favorite item, inventory, daily limit, and bond reward; peer input never supplies Favor or currency values. The contact's `LastGiftDay`, Favor, and milestone result persist in `FarmSaveData` version 25.

## Validation

`META20_GIFT_SMOKE` gave Elara tea on day one (+3 Favor and the 25-gold first-bond reward), rejected a duplicate and an unsuitable gift, gave tea again on day two (+3 Favor and the 50-gold second-bond reward), then restored 6 Favor and day-two gift state from save data. Unity Console audit reported no errors.

## Next connection

Meta 21 will make the existing seasonal schedule playable through festival contributions. Gifts and orders will become meaningful ways to prepare for a seasonal collective goal rather than isolated buttons.
