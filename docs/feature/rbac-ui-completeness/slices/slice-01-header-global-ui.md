# Slice 01 — Header & Global UI Gating

**Feature**: rbac-ui-completeness
**Stories**: US-01 (Update All), US-04 (API Keys tab), US-05 (License import/clear)
**Estimate**: ≤4 hours
**Walking skeleton**: yes — smallest, highest-visibility change

## Goal (one sentence)

Hide the global System-Admin-only surfaces from non-System-Admins in the app header and System Settings.

## IN scope

- `<UpdateAllButton />` rendered conditionally on `rbac.isSystemAdmin` in both desktop and mobile header layouts (`components/App/Header/Header.tsx` lines 74 and 149)
- `Settings.tsx:85` — add `"40"` (API Keys) to `systemAdminTabValues`
- `LicenseStatusPopover.tsx` — gate the Add/Update License and Clear License buttons on `rbac.isSystemAdmin`; status display stays visible
- Vitest assertions for each gate
- E2E assertion added to existing `@rbac` test: as TeamReader, no `update-all-button` testId in DOM; no `api-keys-tab` testId; no Add/Clear License buttons visible in popover

## OUT scope

- License status text / expiry / tooltip — visible to all (already the case)
- BlockedPage license upload — exempt (pre-bootstrap flow for first-time deployment)
- Per-team Refresh / Update Data button (already gated via `showWriteControls`)
- Any backend change

## Learning hypothesis

Disproves: that the Update All / API Keys / License buttons are a meaningful surface for non-System-Admins.
Confirms: that the existing `useRbac()` hook is the right one-line gate for global controls and that no special state needs to be added.

## Acceptance criteria (from feature-delta US-01, US-04, US-05)

- [ ] `<UpdateAllButton />` not in DOM when `rbac.isSystemAdmin === false`
- [ ] `<UpdateAllButton />` in DOM when `rbac.isSystemAdmin === true` (incl. non-RBAC deployments via `PERMISSIVE_SUMMARY`)
- [ ] Settings page rendered as TeamReader shows tabs `system-info-tab` only (no `api-keys-tab`, `configuration-tab`, etc.)
- [ ] Settings page rendered as SystemAdmin shows all tabs incl. `api-keys-tab`
- [ ] LicensePopover as TeamReader: status row visible, no `Add License` / `Update License` / `Clear License` buttons
- [ ] LicensePopover as SystemAdmin: all buttons present (unchanged)

## Dependencies

None. Pure additive frontend change. `useRbac()` already exists.

## Pre-slice spike

Not required.

## Production-data acceptance

The E2E run uses real Keycloak users + real Lighthouse instance — no synthetic data.

## Dogfood

Maintainer runs `pnpm dev` locally, logs in as `teamreader@user.com` (existing seeded user), confirms the three surfaces are absent. Same-day.

## Effort estimate

~2 hours implementation + 1 hour tests + ~1 hour E2E wiring. Walking skeleton; ships independently.

## Reference class

Comparable to rbac-enhancements step 03-04 (commit `bf5dcb05` — hide Log Level for non-sysadmin). One file edit + one or two tests.
