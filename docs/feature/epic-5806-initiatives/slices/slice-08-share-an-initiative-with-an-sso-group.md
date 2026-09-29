# Slice 08 — Share an Initiative with an SSO group

**Feature**: epic-5806-initiatives · **Epic**: #6119 "Initiative access and on-track status" ·
**Story**: US-08 · **ADO**: #6122 · **Estimate**: ~3h (≤1 day) · **Order**: 8 of 10

## Goal

A System Admin maps an identity-provider group to Viewer or Admin on an Initiative, and everyone in that
group gets it on their next request, exactly as group mappings already work for Teams and Portfolios.

## IN scope

- Settings → Access → Group Mappings offers the Initiative scope for Viewer and Admin.
- Group-derived Initiative grants merge with direct ones; the higher role wins.
- `docs/settings/rbac.md`: the group-mapping section names the new scope.

## OUT of scope

- Initiative Admins managing group mappings (D24 keeps them with the System Admin).

## Learning hypothesis

**Disproves, if it fails**: that the existing claim-computed mapping extends to a new scope with no new
mechanism. If it needs one, the mapping code is less general than the docs claim.

## Acceptance criteria

AC-8.1 to AC-8.4 in `feature-delta.md`.

## Dependencies

Slices 07 (scope, Viewer) and 09 (Admin role). D24 is settled.

## Effort

~3h.

## Dogfood / production data note

With a test identity-provider group, map it to Viewer on one Initiative, sign in as a member, then remove
the mapping and refresh. The Initiative must appear and disappear without any per-user row being
created.
