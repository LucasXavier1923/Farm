# Localization

The game uses English as its source language. Player-facing text must never require a code change to add another language.

## Runtime data

Language files live in `Assets/Resources/FarmLocalization/`.

- `en.json` is the required fallback table.
- Add one JSON file per language, such as `pt-BR.json`, `es.json`, or `de.json`.
- `LanguageCode` must be unique and use a standard language tag.
- Every new language reuses the same `Key` values from `en.json`; only `Value` changes.

```json
{
  "LanguageCode": "es",
  "Entries": [
    { "Key": "item.pumpkin.name", "Value": "Calabaza" },
    { "Key": "season.spring", "Value": "Primavera" }
  ]
}
```

Missing entries safely fall back to English. A language can therefore ship progressively without broken UI.

## File ownership

Keep localization work in these places only:

| Concern | Owner file/location | Never edit for a translation |
| --- | --- | --- |
| Source-language copy and stable keys | `Assets/Resources/FarmLocalization/en.json` | C# gameplay scripts |
| A shipped language | `Assets/Resources/FarmLocalization/<language-tag>.json` | `en.json` keys |
| Translation instructions and review | `Docs/Localization.md` | Prefabs or scenes |
| Runtime lookup and fallback | `FarmLocalization.cs` | Individual UI screens |

English is the canonical source language. Portuguese, Spanish, German, and any later language must be introduced as a data table, never as a second set of hard-coded UI strings.

## Key namespaces

Use the feature that owns the text as the prefix. This keeps the table navigable as the game grows.

- `hud.*` — HUD, menus, inventory, storage, sleep, and controls.
- `prompt.*` and `interaction.*` — nearby-object and world interaction prompts.
- `tile.*`, `tool.*`, and `tutorial.*` — farming actions and feedback.
- `item.*`, `crop.*`, `recipe.*`, and `buildable.*` — authored content names and descriptions.
- `backend.*`, `commerce.*`, and `sleep.*` — confirmed-action and session responses.
- `weather.*`, `season.*`, `clock.*`, and `quality.*` — world simulation labels.

Keys are API: do not rename or translate them after a language has shipped. Add a new key instead when the meaning changes.

`building.*` and `catalog.*` own construction mode, placement validation, grid guidance, and project browsing.

## Rules for gameplay code

Use explicit keys for every new dynamic message:

```csharp
FarmLocalization.Get("ui.inventory.empty", "EMPTY");
FarmLocalization.Format("feedback.harvested", amount, cropName);
```

`Format` uses invariant formatting and supports `{0}`, `{1}`, and so on. Do not concatenate translated sentence fragments: keep each complete sentence in one entry.

Static labels created by the current HUD are also passed through `FarmLocalization`, with the English label as a safe fallback. New UI should still use descriptive explicit keys rather than relying on that legacy convenience.

## Content definitions

The following data objects already resolve their names from the active language table:

- `ItemDefinition.LocalizedName` -> `item.<id>.name`
- `CropDefinition.LocalizedName` -> `crop.<id>.name`
- `CraftingRecipe.LocalizedName` / `LocalizedDescription` -> `recipe.<id>.name` and `recipe.<id>.description`
- `FarmBuildableDefinition.LocalizedName` / `LocalizedDescription` -> `buildable.<id>.name` and `buildable.<id>.description`

Keep the serialized `DisplayName` and `Description` fields as an editor fallback only. New player-facing systems must use the localized properties above.

## Language selection

`FarmLocalization.SetLanguage("pt-BR")` activates an installed table and persists the selection locally. `FarmLocalization.AvailableLanguageCodes` can populate a future language selector. Switching language emits `FarmLocalization.LanguageChanged`; any screen that supports live language switching should rebuild or refresh in response to that event.

## Translation workflow

1. Copy `en.json` to a new language-code file.
2. Translate only `Value`; do not modify `Key`, placeholders, item IDs, or line-break markers.
3. Launch the game with that language selected and review text overflow in each modal.
4. Add missing entries to `en.json` first, then mirror them in translation files.

This keeps translators working in data files and prevents localization from becoming a source-code editing task.

## Review checklist

Before accepting a translation:

1. Keep placeholders (`{0}`, `{1}`), currency symbols, and input names (`F`, `Esc`, `I`) intact unless the platform convention requires otherwise.
2. Preserve explicit line breaks (`\n`) and do not translate IDs such as `pumpkin_seed`.
3. Test the language in every modal: inventory, storage, market, orders, sleep, journal, mastery, crafting, collections, mailbox, settings, and building catalog.
4. Treat English fallbacks as a developer safety net only. Add the missing key to `en.json`, then mirror it in each shipped language.

## Current source-table coverage

`en.json` is the source table for the actively localized vertical slice. It includes content names, calendar and weather, item qualities, backend and commerce responses, sleep/readiness, cultivation feedback, inventory and storage, shop and orders, tooltips, pest forecasts, save prompts, and all in-world interaction prompts. The HUD's active labels, controls, sleep window, journal, filters, and storage UI are also keyed under `hud.*`.

Construction is covered under `building.*`: placement and move failures, reclaim feedback, fence continuation, the build launcher, snapped/valid status, grid state, and all placement instructions. Inventory-to-hotbar drag feedback and both storage capacity headings are likewise data-driven under `hud.*`. Context passed between UI views uses the localized `storage.backpack` and `storage.chest` values, so it remains consistent with the visible language.

When a new feature needs text, add its English key to `en.json` in the feature namespace first (for example, `fishing.*` or `animals.*`). Gameplay code may pass an English fallback for resilience during development, but that fallback is not a translation source. Translators only need the JSON tables and this document; they never need to edit C#.

Legacy screens are being migrated incrementally. Any visible player-facing label touched during feature work must be converted to a key in the same change; do not add new untranslated literals to C#.

## Escaped-text safety

`FarmLocalization` normalizes a final escaped-text layer while it loads every JSON table. This prevents UI from showing literal `\\n` or `\\u2022` when an external translation/export tool has escaped a JSON value twice. It also repairs the small set of common legacy UTF-8 punctuation artifacts in the current English table.

Translation files should still use normal JSON escapes exactly once: write `\n` for a line break and `\u2022` (or the actual `•` character) for a bullet. The runtime normalizer is a safety net, not a replacement for valid localization data. `LOCALIZATION_ESCAPE_SMOKE` verifies the shop, week, and tooltip strings render real line breaks and bullets with no literal escape sequences.
