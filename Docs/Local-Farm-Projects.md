# Meta 8 - Local Farm Projects

Projects are persistent Journal goals, not daily UI timers. `Village Regular`
requires five delivered village orders and awards $150 once. Its progress is
part of `FarmJournalProgress`, which is cloned into `FarmSaveData` and shared
world snapshots. A peer may request a delivery through the existing session
seam but cannot award or claim the project locally.
