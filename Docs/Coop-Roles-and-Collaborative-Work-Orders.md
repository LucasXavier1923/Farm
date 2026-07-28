# Co-op Roles and Collaborative Work Orders

## Purpose

Meta 36 corrects an important co-op architecture problem: farm mastery describes shared farm technology, so it cannot also describe each player's identity. The new role profile belongs to a player ID and is stored in the shared authoritative farm state. This makes room for a four-player crew to divide work without making a role mandatory for ordinary farming.

The design uses the proven co-op pattern of complementary optional contributions: a group earns an extra reward when different players bring distinct strengths to the same shared objective. A player can still plant, fish, craft, or deliver any valid item regardless of their role.

## Roles

| Role | Best matching board work | Why it fits |
| --- | --- | --- |
| Cultivator | Crop order | Represents planned growing and crop output. |
| Gatherer | Fishing or Animal order | Represents field collection, fishing, and animal goods. |
| Steward | Workshop order | Represents the processing and trade side of the homestead. |

Roles are selected in the Mastery window with `Shift+1`, `Shift+2`, or `Shift+3`. Normal `1`, `2`, and `3` still operate the existing **shared farm-mastery focus**, so the two concepts stay distinct in the UI.

A player may adjust their role before their first matching delivery. After that contribution, their role is committed. This creates a meaningful identity while avoiding an irreversible choice before the player has tried the loop.

## Collaborative work-order loop

1. The order board now states the best co-op role for each order.
2. Any player may still deliver a valid order from the shared inventory/storage boundary.
3. A delivery made by the matching role records a personal contribution.
4. When Cultivator, Gatherer, and Steward each make a matching delivery on the same day, the shared farm earns a one-time `$45` Teamwork Bonus.

The bonus is intentionally modest: it rewards coordination without making a mismatched delivery feel like failure or requiring all three roles in solo play.

## Authority and identity

- `FarmPlayerRoleProfile` is keyed by the transport-level player ID. The prototype accepts at most four profiles.
- Solo and host sessions call `TrySetCoopRole` and `TryCompleteDailyOrder` locally.
- Peers send `PlayerRole` or `DailyOrder` intents. `FarmHostIntentRouter` uses `RequestedBy` as the profile key and applies the result only on the host.
- The host validates invalid roles, a fifth profile, a role change after contribution, invalid orders, duplicate deliveries, item availability, and the one-time daily teamwork bonus.
- Steam transport remains an adapter concern: it only needs to preserve the player ID already included in the intent.

## Persistence

Save version 34 adds `FarmCoopRoleProgress`, including player profiles, matching contribution counts, the daily role mask, and the last Teamwork Bonus day. Version 33 and older saves create an empty role roster; their existing farm mastery is unchanged.

## Localization

Role names, selection feedback, lock/four-player validation, order recommendations, and teamwork reward text live in `Assets/Resources/FarmLocalization/en.json` under `roles.*`. No player-facing sentence is embedded in the role profile or rule classes.

## Validation

`META36_ROLE_SMOKE` used real `FarmGameState` components to:

1. register Cultivator, Gatherer, and Steward profiles;
2. deliver matching Crop, Fishing, and Workshop orders as three different player IDs;
3. verify a single `$45` teamwork reward on the third contribution;
4. reject a role change after a contribution; and
5. save/restore the role, contribution count, role mask, and bonus day.

```text
passed=True rolesSet=True crop=True fish=True workshop=True teamworkBonus=45 roleLock=True persisted=True
```

## Deferred work

Player-owned XP and achievements can later extend these profiles once live Steam identities and secure account persistence are enabled. They must not replace the existing shared farm mastery until profile synchronization has a real transport test.
