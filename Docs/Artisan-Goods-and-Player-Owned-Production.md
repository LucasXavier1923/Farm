# Artisan Goods and Player-Owned Production

## Purpose

The workbench now supports deliberate artisan production rather than only Stone refinement. A player chooses a raw good in the hotbar, spends it immediately, waits for the shared queue, then decides whether to collect or let the Farm Shed route completed goods into shared storage next morning.

## First Artisan Choices

| Raw input | Conversion | Time | Base value comparison |
| --- | --- | --- | --- |
| 2 Chicken Eggs | 1 Egg Preserve | 30 game minutes | $48 raw -> $70 artisan |
| 3 Pumpkins | 1 Pumpkin Jam | 35 game minutes | $45 raw -> $80 artisan |
| 2 Pond Fish | 1 Smoked Fish | 35 game minutes | $36 raw -> $68 artisan |

The margin pays for time, queue capacity, and the opportunity cost of using goods for cooking, orders, gifts, or immediate market sale. It is not passive: every job consumes its chosen input at queue time and the three-job workbench is serialized.

## Quality and Authority

The processor selects the highest single quality tier with sufficient input quantity. That tier is consumed exactly and inherited by the artisan output. A Silver Egg Preserve and Gold Pumpkin Jam therefore preserve the value of animal care, crop rotation, compost, seasonal planning, and mastery.

- `FarmProcessingJobSaveData` now stores `OutputQuality` in save version 32.
- Output collection and Farm Shed morning storage use the same quality-aware inventory APIs.
- A peer sends the selected recipe ID through the existing `Production` intent. The host validates the recipe, inventory quantity/quality, queue capacity, completion time, and output capacity.
- Raw Stone refinement remains available by selecting Stone.

## Player Flow

1. Select Stone, Chicken Eggs, Pumpkin, or Pond Fish in the hotbar.
2. Stand at the workbench; its prompt shows the selected conversion and current queue.
3. Press **F**. The host queues the job only after consuming valid shared input.
4. Return when the job completes, or use a Farm Shed to route the completed product into storage during the shared morning routine.

## Validation

`META32_ARTISAN_SMOKE` passed. It verified the three product definitions, exact quality-tier input consumption, serialized queue persistence through save/load, collection, and Silver Egg Preserve / Gold Pumpkin Jam output quality.
