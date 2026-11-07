# Bug 6575 — root cause

Team Admins and Portfolio Admins, and keys scoped to them, can delete their own Team or Portfolio through the API, while
`docs/settings/rbac.md` says only System Admins can.

## Reproduction

Run on a throwaway fixture on the `F_BE_1` host, with the real `RbacGuard` and `RbacAdministrationService` and RBAC on.

| Actor | Request | Result |
|---|---|---|
| Team Admin, session | DELETE own Team | 204, row gone |
| Portfolio Admin, session | DELETE own Portfolio | 204, row gone |
| Team-scoped API key | DELETE Team | 204, row gone |
| Team Admin | DELETE another Team, or a Portfolio | 403 |
| Viewer | DELETE Team | 403 |
| System Admin | DELETE Team | 204 |
| Team Admin, Portfolio Admin | POST create | 403 |

## Why

1. `TeamController.cs:93` and `PortfolioController.cs:69` guard DELETE with `TeamWrite` and `PortfolioWrite`, which scoped
   admins hold.
2. The guards were correct when written on 2026-05-09: scoped delete was the intent then.
3. On 2026-05-12 the policy tightened in three commits within about 11 minutes: a3bef9fdb changed create in the backend,
   3c1b7a527 changed the frontend delete button, and c9b39f2e2 rewrote `rbac.md`. Nobody changed the delete guards.
4. Delete had no capability of its own. It shared the write requirement with edit, so it needed a change per endpoint, and the
   hidden button kept the gap out of sight of manual testing.
5. **Root cause: the capability matrix exists only as prose.** No test pins it at the API. BE_1_4, added the same day, uses a
   delete as its write probe and so asserts the opposite.

Deleting a Team also affects Portfolios that use or are owned by it, which that Team Admin may not administer.

Create is guarded System Admin only. Clone has no endpoint of its own: it goes through create. No other mismatch was found on
other destructive endpoints, from reading the code.

## Fix

A dedicated `CanDeleteUnit` requirement with the same logic as `CanCreateTeam`, on both delete endpoints. The existing
`SystemAdmin` requirement was rejected: it skips the enforcement gate, so a System Admin could delete with Premium lapsed where
today they are refused. The dedicated requirement only narrows access, and a separate name stops create and delete being
coupled again.

## Impact

Scoped admins and their keys lose delete through the API and CLI (`lh team delete`, `lh portfolio delete`). They have had no
button since 3c1b7a527. Keys limited to a Team or Portfolio lose it too, even when a System Admin owns them. `rbac.md` is
already correct. A release note is due at `/release`.
