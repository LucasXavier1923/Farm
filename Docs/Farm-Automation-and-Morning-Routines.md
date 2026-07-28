# Farm Automation and Morning Routines

## Intent

Automation should reclaim routine work without turning the farm into an unattended resource generator. The farmer still chooses materials and starts every refinement; the shared farm then carries out the work over game time and reports the result in a readable morning routine.

## Shared Workshop Queue

The workbench now has a persistent queue of up to three Stone-to-Refined-Stone jobs.

- Press **F** at the workbench to collect a completed job first; otherwise it queues another refinement when there is space.
- Each job consumes 2 Stone at the moment it is deliberately queued.
- Jobs are serialized, not parallel: each one starts after the previous completion timestamp. Skipping time, save/load, and co-op snapshots cannot make the queue produce out of order.
- A full queue rejects further input and preserves the player's materials.
- The same host-routed `Production` intent validates a peer's request, so this is ready for the later Steam transport instead of relying on client-owned timers.

## Farm Shed Morning Collection

Placing the existing Farm Shed creates a practical logistics benefit in addition to its storage capacity. At the host's morning update, completed workshop jobs are routed into shared Storage when there is room. The morning feedback reports the number of processed items stored. If storage is full, the completed job remains safely in the queue for a player to collect later.

This gives a built structure a mechanical purpose and preserves choice: players may keep refinements in the queue, retrieve them manually before morning, or plan materials into storage for later buildings, projects, and market requests.

## Persistence and Co-op Safety

`FarmProcessingJobSaveData` already persists recipe, output, quantity, and absolute game-time completion. Meta 29 adds no client-only rewards or real-world timers. The host executes shed collection only after the existing `TryBeginMorningAutomation` guard, which prevents duplicate runs in the same shared day.

## Validation

`META29_AUTOMATION_SMOKE` passed. It verified the three-job capacity, no material loss on a rejected fourth request, sequential completion timestamps, save/restore of the queued jobs, partial and final storage collection, and a total of three Refined Stone outputs.
