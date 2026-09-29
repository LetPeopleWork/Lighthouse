# Slice 07 — Share one Initiative with someone who cannot open its Portfolios

**Feature**: epic-5806-initiatives · **Epic**: #6119 "Initiative access and on-track status" ·
**Story**: US-07 · **ADO**: #6120 · **Estimate**: ~7h (≤1 day) · **Order**: 6 of 10

## Goal

A System Admin grants a user Viewer on one Initiative, and that user sees exactly that Initiative on the
Initiatives page, with its full date and status, without being given any Portfolio.

## IN scope

- A new Initiative scope beside System, Team and Portfolio (decision D21), with the Viewer role, granted
  to users under Settings → Access → Users. The scope picker lists Initiatives by name and tracker type.
- Enforcement on the Initiatives page read: a row is visible through an Initiative grant, or through
  Portfolio read (D23). Numbers cover all Features; Features in unreadable Portfolios are only
  counted (D22).
- An empty state for a reader who can see no Initiative.
- All checks through `IRbacAdministrationService`; UI through `useRbac()`. Expand-only migration via
  `CreateMigration`. Grant keyed per D25; nothing changes without RBAC enforced (D26).
- `docs/settings/rbac.md`: the scope and the Viewer row of the capability matrix.

## OUT of scope

- The Admin role (slice 09). SSO group mappings (slice 08). The grant list's "not in current data" state
  (slice 10).
- Anything an Initiative grant would show outside the Initiatives page.

## Learning hypothesis

**Disproves, if it fails**: that a scope whose entity is created and removed by tracker refresh can sit
in the RBAC model beside Teams and Portfolios, which the product creates and deletes itself, without
special cases leaking into every check.

## Acceptance criteria

AC-7.1 to AC-7.6 in `feature-delta.md`.

## Dependencies

Slice 03 (Epic #5806). D23 and D25 are settled.

## Effort

~7h: scope and grant storage ~3h, enforcement on the read ~2h, grant UI ~1h, E2E ~1h.

## Dogfood / production data note

On the dev instance with RBAC on, create a user with only a Viewer grant on one real Initiative that
spans two Portfolios. Sign in as them and check what else anywhere in the product became visible. The
answer must be: nothing.
