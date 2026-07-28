# Land Stewardship and Regenerative Gathering

## Purpose

Mining is a renewable farm activity rather than an infinite static resource. A depleted quarry outcrop can be prepared during rain so that its next return produces more building material. This creates a small but meaningful connection between crop waste, weather, gathering, construction, refining, workshop recipes, and shared farm requests.

## Player Loop

1. Mine a quarry outcrop three times. Its stone reward is granted on the third hit and the outcrop rests until the following day.
2. On a rainy day, stand near a resting outcrop with **Compost** in the shared inventory and press **F**.
3. The host validates the request, consumes one Compost, and marks the specific outcrop as prepared.
4. When that outcrop returns the next day, its next completed mining cycle yields **3 Stone** instead of **2 Stone**.
5. The preparation is then consumed. A player cannot stack preparations or renew an active node.

The temporary ground ring is functional feedback, not scenery: blue means a resting outcrop can be renewed if conditions are met; green means its next return has already been prepared.

## Authority and Persistence

- `FarmGameState` owns per-node hits, respawn day, and the `Stewarded` flag in `FarmResourceNodeSaveData`.
- `TryStewardResourceNode` verifies depletion, rain from the deterministic global weather seed, the one-Compost cost, and duplicate requests before mutating state.
- A peer emits the `Stewardship` session intent; `FarmHostIntentRouter` validates it on the host using the same state method.
- The farm save format is now version 31. Older saves simply begin with unprepared nodes.
- A returned node retains its preparation until its reward cycle completes, then clears it while entering its next rest cycle.

## Design Boundaries

This is deliberately a light strategic choice, not a real-time chore. It is optional, requires a rainy window, and rewards players who connect farming output to their material plans. It does not alter local time, pause the farm, or add client-owned rewards.

## Validation

`META28_STEWARDSHIP_SMOKE` passed with a deterministic rainy seed. It verified depletion, Compost consumption, save/restore, the three-Stone enhanced return, state reset after reward, and refusal without rain while preserving Compost.
