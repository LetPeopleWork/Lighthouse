# Slice 04 — Polish (Access tab load flash)

**Feature**: rbac-ui-completeness
**Stories**: US-06 (Access tab does not flash during load in non-RBAC deployments)
**Estimate**: ≤1 hour

## Goal (one sentence)

Eliminate the brief Access-tab render that happens on initial Team/Portfolio detail page load in non-RBAC deployments.

## IN scope

- `TeamDetail.tsx:120-121` — change `showAccessTab = !team || (rbac.isRbacEnabled && rbac.isTeamAdmin(team.id))` to `showAccessTab = !!team && rbac.isRbacEnabled && rbac.isTeamAdmin(team.id)`
- `PortfolioDetail.tsx:102-103` — symmetric change with `isPortfolioAdmin`
- Vitest assertion: on initial render with `isRbacEnabled === false`, the Access tab is not in the DOM (no flicker)
- No change to `showSettingsTab` — its `!entity || ...` guard is intentional to avoid Settings flicker for admins (Settings is shown to all admins regardless of RBAC; the brief render is desired UX, not a bug)

## OUT scope

- Other load-flash issues — none found in the sweep, but if any surface during testing, surface as a follow-up (do not bundle into this slice)
- Loading skeleton redesign — orthogonal to RBAC

## Learning hypothesis

Disproves: that the flash is harmless. Manual testing identified it as confusing in non-RBAC deployments; fix it.
Confirms: that the `!entity || ...` pattern is fine for write controls (where the admin will see them anyway) but wrong for RBAC-only surfaces (where they should never appear in non-RBAC mode).

## Acceptance criteria (from feature-delta US-06)

- [ ] On initial render of TeamDetail with `isRbacEnabled === false`, the "Access" tab is NOT in the DOM (vitest RTL assertion)
- [ ] On initial render of PortfolioDetail with `isRbacEnabled === false`, the "Access" tab is NOT in the DOM
- [ ] Once team/portfolio loads, behavior unchanged (Access tab still hidden in non-RBAC mode; visible to admins in RBAC mode)
- [ ] Existing E2E coverage for Access tab visibility (in `RoleBasedAccessControl.spec.ts`) continues to pass

## Dependencies

None. Smallest, most isolated change. Could merge with Slice 03 if convenient.

## Pre-slice spike

Not required.

## Production-data acceptance

E2E run with `Authorization__Enabled=false` env var would verify; the standard `@rbac` run uses `Authorization__Enabled=true` so won't catch the regression. Manual verification or a Vitest-only test is sufficient since the change is purely a render guard.

## Dogfood

Maintainer toggles `Authorization__Enabled` env var locally, refreshes a team/portfolio page, confirms no Access tab flash. Same-day.

## Effort estimate

~15 minutes implementation + 30 min Vitest test. Smallest slice; can ship alone or bundle.
