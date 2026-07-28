# Animal Enrichment

## Purpose

The chicken loop now contains a real optional care decision. A Wildflower can be sold, crafted into tea, gifted to a neighbor, or offered to the chickens. The latter is an enrichment action that improves the day's egg quality.

## Live behavior

- A Treat Bowl is present at the starter chicken coop.
- Bring a Wildflower and press **F** at the bowl.
- Each coop accepts one treat per farm day.
- A treat raises the current egg quality by one tier: Normal to Silver, or Silver to Gold. A Gold streak remains Gold.
- The existing feed, care streak, egg collection, persistent daily IDs, and host-routed `AnimalCare` intent remain the authority path.

## Validation

`META23_TREAT_SMOKE` injected the persisted state into the animal authority boundary, offered one Wildflower, verified Silver quality from a Normal day, verified removal from inventory, and rejected a duplicate treat. Unity Console audit reported no errors.

## Next expansion

This is coop-wide enrichment, not individual animal simulation. The next animal meta will add stable per-animal records, distinct needs and product traits, and an expansion choice that remains co-op-safe.
