# Bug 6575 — only a System Admin deletes a Team or Portfolio

Finalized 2026-11-07. Workspace: `docs/feature/bug-6575-delete-guard-scoped-admins/`.

## Summary

With RBAC on, Team Admins, Portfolio Admins and their API keys could delete their own unit through the API, against
`docs/settings/rbac.md`. Both delete endpoints now use a dedicated `CanDeleteUnit` requirement, so they are refused with 403 and
the row survives. Nobody gained access.

## Business context

Found while designing broad admin rights in Epic 6577: an Admin on all would have reached this on every unit. Deleting a Team
also affects Portfolios that use it or are owned by it, which that Team Admin may not administer.

## Key decisions

- A dedicated requirement instead of the existing `SystemAdmin` one, which skips the enforcement gate and would have let a
  System Admin delete with Premium lapsed.
- Delete does not share `CanCreateTeam`, because sharing a rule between two actions is how this gap appeared.
- Fixed ahead of Epic 6577, on its own, because the gap was live.

## Work completed

One step, 01-01, through RED, GREEN and COMMIT. Regression tests failed on the old code with 204. The tests that used delete as
a write probe moved to `PUT`.

## Lessons learned

- The capability matrix existed only as prose, so a policy change reached the UI and docs but not two guards. The new tests pin
  the delete row of the matrix at the API; the other rows are still not pinned there.
- A hidden button hides a gap from manual testing. An API-only path needs an API-level test.
- A test using a destructive action as its write probe can quietly assert the opposite of the policy.

## Issues encountered

None in delivery. Stryker.NET cannot scope to lines, so the whole-file score (68.15 %) is older code with its integration suites
excluded; the changed lines are covered, see `mutation/results.md`.

## Finalization checklist

- Docs prose: N/A, because `rbac.md` already describes this behaviour.
- Screenshots: N/A, because nothing a user sees changed.
- Demo data: N/A, because no data shape changed.
- Website assets: N/A, because nothing visible changed.
- Lighthouse-Clients: no change needed. `lh team delete` and `lh portfolio delete` now get 403 for scoped admins.
- Usage data: N/A, because a bug fix adds no feature to measure.
- ADRs and `ARCHITECTURE.md`: no concept changed.
- Release note: written at `/release`, from the Release Notes tag on the Bug.

## Follow-up

The host accepting both a session and an API key is set up in five test files and should become one shared helper. Not yet an
ADO item.
