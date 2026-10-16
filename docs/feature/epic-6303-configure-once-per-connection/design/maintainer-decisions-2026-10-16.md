# Maintainer decisions, 2026-10-16 (collected before AFK mode)

These were decided by the maintainer in conversation, with the sketches shown below. DESIGN applies the
architecture ones; DISTILL copies the UI ones into `feature-delta.md` as its maintainer-decision block and
pins them in tests. Nothing here is open.

## DESIGN

- **P-1 … P-4, P-6 … P-11, P-13, P-14**: accepted as the architect recommended.
- **CA-6**: a non-directional Azure DevOps relation type (e.g. `System.LinkTypes.Related`), or a base name
  without its end, reads nothing and warns. #6312 stays in scope.
- **P-12**: ids that point at a deleted Additional Field at upgrade count as empty, so those owners follow
  the connection default. No default exists on release day, so they read the tracker's own value again — a
  visible fix that the release note must mention.
- **P-5 (amends DISCUSS D12 / AC-F.\*)**: no on-screen notice after a save; the page navigates to `/` as in
  Story 6307. The reused "update now?" dialog carries the follower counts (UI-3), the save writes one
  Information log entry naming the followers, and their refreshes show in Task Manager → Activity.

## UI (for DISTILL)

- **UI-1 — defaults section**: its own section, "Defaults for Teams and Portfolios", below Additional Fields
  on the connection edit page. Sub-headings *Teams* and *Portfolios*, one row per setting, listing only the
  settings the tracker honours; hidden on Linear and ServiceNow. **No follower count and no "Show who" on
  the page** (see UI-4).

  ```
  ┌ Defaults for Teams and Portfolios ──────────────────┐
  │ Teams and Portfolios on this connection use these   │
  │ unless they set their own.                          │
  │ Teams                                               │
  │  Parent Override Field   [ Caused by          ▾ ]   │
  │  Estimation              [ None ▾ ] (expands)       │
  │ Portfolios                                          │
  │  Parent Override Field   [ None               ▾ ]   │
  │  Dependency Field        [ depends on         ▾ ]   │
  │  Size Estimate Field     [ None               ▾ ]   │
  │  Feature Owner Field     [ None               ▾ ]   │
  │  Estimation              [ None ▾ ] (expands)       │
  └─────────────────────────────────────────────────────┘
  ```

- **UI-2 — owner settings**: the first dropdown entry is `Connection default (<value>)` (FormSelectField),
  with the helper text "Set on the <tracker> connection." Picking any other entry, None included, makes the
  value the owner's own.

  ```
  Parent Override Field
  [ Connection default (Caused by)   ▾ ]
    ├ Connection default (Caused by)
    ├ None
    ├ Caused by
    └ Epic Link
  ```

- **UI-3 — save dialog**: the 6307 `UpdateQuestion` is reused. If nobody follows a changed default, there is
  no dialog and the save works as it does today. A change that refreshes nothing (an Estimation-only change)
  gets a plain Save. Names are not listed in the dialog; they go to the log.

  ```
  ┌ Update 3 Teams and 0 Portfolios? ──────────────┐
  │ 3 Teams follow the Parent Override Field       │
  │ default. Their parents change only once they   │
  │ have been updated. Save only: they change at   │
  │ their next scheduled update.                   │
  │        Cancel   Save only   Save and update    │
  └────────────────────────────────────────────────┘

  ┌ Change the Estimation default? ────────────────┐
  │ 4 Teams follow it; they show the new           │
  │ estimation the next time their pages load.     │
  │                          Cancel   Save         │
  └────────────────────────────────────────────────┘
  ```

- **UI-4 — slice 07 dropped completely.** There is no "who follows / who differs" view, no "make these
  follow" action, and no follower count on the page. Owners hand-set to the same value as the default stay
  *Own*; an admin switches each one on its own settings page. Story #6304 is now slices 05a, 05b and 06.
- **UI-5 — Estimation default**: the owner's existing Estimation editor is reused inline (field, unit,
  non-numeric flag, and categories when non-numeric), stays collapsed until a field is chosen, once for
  Teams and once for Portfolios. The owner page shows `Connection default (Story Points, Points)`.
- **UI-6 — copy, accepted** (configurable terms render in the user's words; wording may be polished in
  DELIVER without changing meaning):

  ```
  Refresh History (Portfolio row):
    ⚠ The Dependency Field names "depends on", and no Feature carries that link.
      Links seen: is blocked by, relates to.
    ⚠ The Dependency Field names "Blocks", which points both ways.
      Name one end: "blocks" or "is blocked by".
    ⚠ The Parent Override Field points at an Azure DevOps relation type,
      which only the Dependency Field reads.

  Connection save refused (field still in use):
    ✕ "Caused by" cannot be removed. It is the Teams' Parent Override Field
      default, and Ledger uses it as its own. Nothing was saved.
  ```

## DEVOPS

- **Usage-data event: none.** N/A, because a connection default is set once at setup; an event would count
  configuration saves, not whether the feature helps anyone. `UsageDataEventName` is unchanged.

## UI-7 — Azure DevOps relation-type warnings (approved during DELIVER, 2026-10-16)

The Refresh History copy for the two Azure DevOps findings that UI-6 did not cover. It follows the approved Jira
"points both ways" wording:

```
⚠ The Dependency Field names "System.LinkTypes.Related", which has no direction.
  Name a relation type with two ends, such as "System.LinkTypes.Dependency-Reverse".

⚠ The Dependency Field names "Contoso.DependsOn" without its end.
  Name one end: "Contoso.DependsOn-Forward" or "Contoso.DependsOn-Reverse".
```

The quoted names are the ones the admin typed (and, for the second, the two ends built from it). The example type
in the first warning is a literal suggestion and stays as written.
