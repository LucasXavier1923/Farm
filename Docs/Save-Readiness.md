# Meta 10 - Save and Release Readiness

The farm save remains version 22. Meta 7 planner state is presentation-only;
Meta 8 delivery-project progress belongs to the existing cloned
`FarmJournalProgress`; Meta 9 permission policy is runtime policy only. None
adds hidden client-only progression fields.

Release checks cover:

- legacy save restoration followed by a current version snapshot;
- deep-copied Journal and placed-object state in a world snapshot;
- Planner and project UI leave `FarmSessionTime` running;
- host/peer mutation boundary rejects peer management operations.
