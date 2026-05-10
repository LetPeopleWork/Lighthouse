# Slice 03: Scoped Team/Portfolio Admin Self-Service

**Feature**: rbac-enhancements
**Delivery Sequence**: 3 of 4
**Estimated Effort**: 3-4 days

## Learning Hypothesis

Team and Portfolio Admins can manage all aspects of their own scope — membership, settings, group mappings, and CRUD operations — without any System Admin involvement, and SSO group-based rights produce identical behaviour to individual assignments.

## Stories in This Slice

| Story | Priority | What it delivers |
|---|---|---|
| US-06 | P2 | Settings and Access tabs visible to Team/Portfolio Admins for their own scope |
| US-07 | P2 | Clone, Delete, Reload, Update All gated by scoped admin rights |
| US-08 | P1 (bug) | Scoped group mapping calls correct endpoint; no "Failed to load" error |
| US-10 | P2 | Create Team/Portfolio button not blocked by connections check for non-system-admins |
| US-12 | P2 | E2E scenarios 5-7: scoped permissions + group-vs-individual parity |

## Design Decisions Resolved in This Slice

- Q9: Update All shown only where user has write access
- Q10: Reload hidden from Viewers
- Q13: Create Team/Portfolio button not disabled by connections check for non-system-admins
- Q14: ScopedGroupMappingManager uses scoped endpoint (bug fix)
- Q16: Clone/Delete require Admin rights for that scope
- Q17: Settings sub-page visible to Team/Portfolio Admins

## Acceptance Gate

This slice is complete when:
1. Jordan (TeamAdmin for Team Alpha) can manage Team Alpha's membership, settings, and group mappings without involving Alex (System Admin)
2. E2E scenario 7 passes: group-based rights produce identical behaviour to individual rights
3. No "Failed to load team access groups" errors for Team Admins

## Dependencies

- Slice 01 complete (System Admin must exist; bootstrap story is the foundation)
- Slice 02 complete (tab visibility gating from US-05 must be in place)
- Backend: scoped group mappings endpoint for teams/portfolios — **confirm or add in DESIGN wave**
- Test environment: 4 test user accounts with Keycloak group membership management
