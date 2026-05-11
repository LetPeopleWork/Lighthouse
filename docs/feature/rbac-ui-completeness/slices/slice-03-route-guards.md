# Slice 03 — Route Guards (Direct URL Navigation)

**Feature**: rbac-ui-completeness
**Stories**: US-07 (Direct URL navigation blocked)
**Estimate**: ≤4 hours

## Goal (one sentence)

When a user navigates directly to an admin-only route (`/teams/new`, `/teams/edit/:id`, `/portfolios/new`, `/portfolios/edit/:id`, `/connections/new`, `/connections/edit/:id`), render a "no access" alert with a link back to Overview instead of loading the wizard.

## IN scope

- `EditTeam.tsx` (used for both `/teams/new` and `/teams/edit/:id` via `TeamEditRedirect`): top-of-component RBAC check
  - `/teams/new` → require `rbac.isSystemAdmin`
  - `/teams/edit/:id` → require `rbac.isTeamAdmin(parseInt(id))` OR `rbac.isSystemAdmin`
- `EditPortfolio.tsx` symmetric (used for `/portfolios/new` and `/portfolios/edit/:id`)
- `EditConnection.tsx`: both routes require `rbac.isSystemAdmin`
- Reusable `<RbacGate>` component (or hook) to avoid copy-paste of the same alert+redirect pattern across three pages
- Vitest tests confirming the alert renders for a Viewer / non-admin and the wizard renders for an authorized user

## OUT scope

- Backend changes — backend already 403s these endpoints; this slice is pure UX
- Admin-only routes inside Settings — already handled by tab visibility in `Settings.tsx:138-146`
- Deep-link to a tab inside TeamDetail (e.g., `/teams/42/settings`) — TeamDetail already gates the tab; deep-link falls through to the first visible tab

## Learning hypothesis

Disproves: that any user is going to type these URLs anyway (perhaps they're shared via support links or bookmarks). If usage logs show ~zero direct hits, this slice could be deferred indefinitely — but the cost is low enough that shipping it preemptively is fine.
Confirms: that a thin `RbacGate` wrapper is preferable to per-page if-statements.

## Acceptance criteria (from feature-delta US-07)

- [ ] As a Viewer/TeamReader, navigating to `/teams/new` shows the no-access alert; no form rendered
- [ ] As a TeamAdmin for Team 42, navigating to `/teams/edit/42` loads the wizard
- [ ] As a TeamAdmin for Team 42, navigating to `/teams/edit/99` (a team they don't admin) shows the alert
- [ ] As a SystemAdmin, all routes work as today
- [ ] E2E: TeamReader attempting `/teams/new` → no-access alert visible

## Dependencies

Slice 01 ships first (Slice 01 establishes the gate-via-`useRbac()` pattern that this slice reuses).

## Pre-slice spike

Optional: 30 min to verify React Router's behavior with the existing `TeamEditRedirect` helper — does `/teams/edit/:id` get the path param correctly when the EditTeam component is used for `/teams/new`? Quick read of `TeamEditRedirect`.

## Production-data acceptance

E2E uses real seeded teams + real Keycloak users.

## Dogfood

Maintainer pastes URLs in the address bar logged in as each role; confirms the right surface. Same-day.

## Effort estimate

~2 hours implementation (the `RbacGate` component is reusable — once written, three call-sites are trivial) + 1 hour tests + 1 hour E2E.

## Reference class

Comparable to the per-page `hasNoAccess` alert pattern already in `TeamDetail.tsx:340` and `PortfolioDetail.tsx:396` — reuse the visual treatment, just trigger it earlier (on missing role, not on missing entity).
