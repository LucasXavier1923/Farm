# Meta 3 - From Resources to Production

## Purpose

Meta 3 turns raw gathering into a durable production loop. The current scene
is only a disposable 3D test harness: no environmental composition is part of
the deliverable. All value is in the gameplay rules, data, persistence, UI
feedback, and future host-authority seams.

## Player loop

`Find renewable node -> equip the right tool -> perform several validated hits
-> receive materials -> start a timed process -> collect output -> use output
in crafting or building.`

## Mechanics contract

### Resource nodes

- A node definition declares its ID, required tool, hit count, yield, energy
  cost, and respawn policy.
- Node state is durable: current durability and depletion time/day survive
  save/load and future host snapshots.
- A wrong tool, full inventory, or peer request never mutates node durability
  or awards items locally.
- A successful final hit grants its full yield exactly once, then depletes the
  node. Respawn is evaluated from shared game time, not real time or a menu.

### Timed processing

- A processing recipe declares input, output, duration in *game minutes*, and
  the processor category that accepts it.
- Starting a job atomically consumes the input and records its completion game
  time. The UI cannot create an item merely by closing, opening, or waiting in
  a menu.
- Finishing a job adds the output exactly once; a full inventory retains a
  collectable completed job rather than deleting output.
- Jobs survive save/load and future host snapshots. A peer can request an
  action but may not finalize it locally.

## First test content

The initial generic nodes will prove the system with **Stone Outcrop** and
**Fallen Timber**. The first processor will prove the timing/transaction rules
with a compact material-conversion recipe. These names and visuals are test
content; the systems must not depend on their scene placement or final art.

## Acceptance checks

1. Correct and wrong-tool node interactions are distinguishable and safe.
2. Partial durability, final yield, depletion, and respawn remain correct after
   save/load.
3. Processing advances while inventory, crafting, storage, or another menu is
   open.
4. A completed job cannot duplicate output, and inventory-full collection is
   recoverable.
5. The existing crafting/building system can consume at least one output from
   the processor.
6. Every player-facing string is localized English data; no scenario dressing
   is introduced in the current scene.
7. Play Mode validation and the Unity Console are checked before handoff.
