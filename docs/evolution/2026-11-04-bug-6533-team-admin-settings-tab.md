# Bug #6533: Team Admins and Portfolio Admins cannot load the Settings tab when RBAC is on

**Pushed to main 2026-11-04, not yet released** · ADO Bug #6533 · workspace
`docs/feature/bug-6533-team-admin-settings-tab/` (RCA, roadmap, execution log, mutation record) · commits
`fb8a93446..531d67a96` · remaining guard work is Bug #6534

## What users get

- **A Team Admin or Portfolio Admin can open and save the Settings tab** of their Team or Portfolio. It used to stay
  blank, with no error, on every release with RBAC.
- **A Portfolio Admin's save keeps an Owning Team they cannot read.** The form shows it as empty, and the stored owner
  stays.
- **Clearing an Owning Team now works** when that Team works none of the Portfolio's Features. It used to do nothing,
  silently, with or without RBAC.

## How it happened

1. The tab loads settings and connections together, so one refused request leaves the form empty.
2. The connection list is System Admin only, so a scoped admin gets a 403.
3. ADR-006 locked the list and said to repoint the frontend at a new `/summary`. Only the backend half shipped,
   bundled with three other security stories.
4. That commit reversed `e0aef89bb`, the real fix from earlier the same day. Both first shipped in `v26.5.14.2`, so
   the fix never reached a release, while the docs written for it stayed and said it was fixed.

## What changed

- `/summary` carries six fields, the non-secret ones the settings forms read, pinned by an exact-key test.
- The Team and Portfolio Settings tabs read `/summary` for every role. No role branch, and the 403 is never caught:
  an empty list would let a save write connection id 0.
- `UpdatePortfolio` keeps a stored owner the caller cannot read, found by its id. `SyncTeams` sets the navigation and
  the foreign key together.
- `docs/settings/rbac.md`, the old release note and ADR-006 are corrected.

## Lessons

- **Finishing the data load opened a path nobody had used.** Fixing the tab made a save reachable for a role that
  could never save before, and that save met a read filter the write side did not know about. When a fix makes a
  screen reachable, trace its writes too.
- **A mock hid a missing `Include`.** The first owner fix read a navigation the save path does not load, and passed
  because the mock supplied it. Only a test through the real API and `DbContext` showed what happened, including that
  the owner survived by accident, and that a legitimate clear silently did nothing.
- **A mutation score with no survivors can mean nothing was tested.** All 13 mutants of the key method were compile
  errors, because of an `is not { } x` guard. Read the mutant statuses, not only the survivors.
- **A review finding is a claim to check.** One review blocker led to a gate reorder that bought nothing and was
  reverted; another review had the failure mode half wrong. Both were settled by reading the whole path or measuring.

## Verification

- Backend: 12,375 passed, 2 skipped, 2 failed on the rebased code; the 2 are the Licensing tests that need a gitignored
  fixture.
- Frontend: 9,412 of 9,413 passed; the failure was a timeout under load in an unrelated file, which passes alone.
- Mutation: frontend 85.71 %. Backend changed lines 11 of 15, and the other 4 are equivalent mutants; see
  `mutation/results.md`.
- The `@rbac` E2E check was never run locally. Its first run is in CI, agreed by the maintainer.
