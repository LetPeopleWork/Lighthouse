# Bug 6575 — Team Admins and Portfolio Admins can delete their own unit through the API

## Wave: DELIVER / [REF] Maintainer decisions

- Root cause in `rca.md` confirmed (2026-11-07).
- Fix: a dedicated `CanDeleteUnit` requirement, not the existing `SystemAdmin` one, which skips the enforcement gate.
- Fixed on nWave 3.22.2, before the v4 upgrade.
- Rigor profile `thorough`: crafter Opus, reviewer Sonnet, double review.
- It goes ahead of Epic 6577, whose Admin on all would otherwise reach every unit.

## Wave: DELIVER / [REF] Implementation summary

With RBAC on, only a System Admin can delete a Team or a Portfolio. Both delete endpoints use a dedicated `CanDeleteUnit`
requirement, so scoped admins and their API keys get 403 and the row survives. Nobody gains access: the enforcement gate still
refuses a System Admin when Premium has lapsed.

## Wave: DELIVER / [REF] Commits

- `fix(rbac): only a System Admin can delete a Team or Portfolio`
- `test(rbac): BE_1_4 proves the guard passed without relying on one incidental status`

## Wave: DELIVER / [REF] Quality gates

- Regression tests failed on the old code with 204, then passed.
- Full backend suite under the standard filter: 12,806 passed, 0 failed, 2 skipped. Run before the review revision, which changed
  one test assertion and one comment; those two classes and `dotnet build` were re-run after it.
- Refactor: an L1 tidy of the new test file. The production change is about 20 lines.
- Double review on Sonnet, one on authorization and one on tests: no blocker, high or testing theatre. The medium finding is below.
- Mutation: see `mutation/results.md`.
- DES integrity: all steps have complete traces.

## Wave: DELIVER / [REF] Follow-up

The host that accepts both a session and an API key is now set up in five test files. It is the same knowledge, so it should
become one shared helper taking the licence mock as a parameter. Not done here, to keep the fix small.
