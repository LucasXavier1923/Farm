# Meta 15 — Progression & Specialization

The prototype now uses a strict specialization cap rather than a freely changeable focus.

- Every mastery track can progress through Level 2.
- Until a specialization is chosen, all tracks stop at 35 XP, one point before Level 3.
- Choosing a Level 2 track commits the profile to that specialization.
- Only the committed track can reach Level 3 and receives the existing 25% experience bonus.
- A committed specialization cannot be changed by the current prototype profile.

This creates clear cultivation, harvesting/production, and commerce roles in a four-player farm while keeping every normal activity available to a solo tester. Player profiles remain part of the local hosted save; the host `Progression` intent provides the shared-session transition boundary.

Older prototype saves that already have multiple Level 3 tracks are normalized when a specialization is committed: non-selected tracks are capped at 35 XP. This is an intentional migration toward the strict model, not a deletion of inventory, money, world, or building data.
