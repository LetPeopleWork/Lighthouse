# Slice 09 — An Initiative's Admin decides who can see it

**Feature**: epic-5806-initiatives · **Epic**: #6119 "Initiative access and on-track status" ·
**Story**: US-09 · **ADO**: #6121 · **Estimate**: ~6h (≤1 day) · **Order**: 7 of 10

## Goal

A System Admin makes someone Admin of an Initiative, and that person manages the Initiative's audience
from its row on the Initiatives page, without asking the System Admin. The same grant is the write right
slice 05 checks for the target date.

## IN scope

- The Admin role on the Initiative scope, granted by a System Admin to users.
- A row action "Access" on the Initiatives page, shown only to a System Admin or that Initiative's
  Admin (through `useRbac()`), to add, change and remove the Initiative's Viewers and Admins (users only,
  D24).
- The API refuses anyone else, through `IRbacAdministrationService`, mirroring how a Portfolio Admin
  manages their Portfolio's members today.
- `docs/settings/rbac.md`: Admin row of the capability matrix, and "Manage an Initiative's access".

## OUT of scope

- The target date itself (slice 05, which depends on this slice).
- Group mappings (slice 08). Any read of Portfolios, Teams or Features through the Admin grant (D22).

## Learning hypothesis

**Disproves, if it fails**: that the people who own an Initiative will manage its audience themselves.
If every change still goes through the System Admin in dogfooding, the Admin role is only a write right
for the target date, and its access-management half can be dropped.

## Acceptance criteria

AC-9.1 to AC-9.5 in `feature-delta.md`.

## Dependencies

Slice 07. D24 is settled.

## Effort

~6h: role and check ~2h, row action and panel ~3h, E2E ~1h.

## Dogfood / production data note

Make a colleague Admin of one dev-instance Initiative and ask them to share it with someone else without
instructions. Note where they looked for the control first.
