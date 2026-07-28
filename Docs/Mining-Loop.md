# Mining Loop - First Pass

Mining starts as a compact renewable quarry beside the farm, not as a separate
mine map. This keeps the first exploration loop close to the core farming
vertical slice while supplying a meaningful craft material.

## Current implementation

- Four Stone Outcrops appear beside the starting plot.
- Stand within range and press `F`, or click the visible outcrop.
- Each node gives 2 Stone, costs the same energy as one harvest action, and is
  unavailable until the next farm day.
- A daily pickup ID is saved in `FarmGameState.CollectedPickupIds`; mined nodes
  therefore survive save/load and return naturally on a new day.
- The loop uses the existing shared-state pickup path and emits a peer intent
  rather than changing a peer inventory directly.

## Deliberately not in this pass

- Pickaxe animation, tool attachment, node-hit effects, copper/iron tiers,
  underground maps, monsters, and final respawn/balance rules.

Those additions should extend `FarmMiningSystem` rather than bypass its daily
pickup identity and session-authority boundary.
