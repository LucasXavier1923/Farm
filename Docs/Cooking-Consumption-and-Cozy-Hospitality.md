# Cooking, Consumption, and Cozy Hospitality

## Purpose

Artisan goods should not be valued only by their sale price. Egg Preserve, Pumpkin Jam, and Smoked Fish now have two deliberate destinations: recover farm energy with **R**, or become the daily favorite gift for a specific community contact.

## Artisan Hospitality Map

| Contact | Favorite artisan gift | Social connection |
| --- | --- | --- |
| Elara | Egg Preserve | Animal care and product quality |
| Bram | Smoked Fish | Fishing time and pond planning |
| Niko | Pumpkin Jam | Crop growth, soil rotation, and seasonal planning |

Each gift grants 3 shared Favor, uses the existing one-gift-per-contact-per-day rule, and can unlock the existing community bond rewards. This makes a finished artisan good compete with immediate money, energy recovery, storage, orders, and future production.

## Consumption

- Egg Preserve restores 16 Energy.
- Pumpkin Jam restores 26 Energy.
- Smoked Fish restores 30 Energy.

All remain normal consumable items and are used with the existing host-routed **R** action outside modal UI. The host validates inventory and energy capacity before removing the item. Quality remains valuable for the market and inventory ecosystem; the social and energy uses deliberately avoid multiplying its value again.

## Persistence and Co-op

Community gifts use the shared farm's persistent `FarmCommunityProgress`; the same records store daily gift limits, Favor, milestones, and rewards. The existing `CommunityGift` intent carries the selected artisan item to the host, so peers do not mutate inventory or Favor locally.

## Validation

`META33_HOSPITALITY_SMOKE` passed. It verified Elara's Egg Preserve gift (+3 Favor), daily duplicate rejection, exact item consumption, Pumpkin Jam energy recovery, saved Favor restoration, and Smoked Fish meal registration.
