# Slice 02: System Admin User and Group Management

**Feature**: rbac-enhancements
**Delivery Sequence**: 2 of 4
**Estimated Effort**: 1-2 days

## Learning Hypothesis

System Admins can perform all routine RBAC housekeeping — removing departed users, managing the user table — entirely from within Lighthouse, with no external tooling.

## Stories in This Slice

| Story | Priority | What it delivers |
|---|---|---|
| US-04 | P2 | Remove departed users with confirmation dialog |
| US-05 | P2 | Access/System Admins tabs gated by isRbacEnabled |

## Design Decisions Resolved in This Slice

- Q6: Remove departed users in-app with hard delete + confirmation
- Q19: Access tab in team/portfolio detail and System Admins tab in Settings visible only when isRbacEnabled

## Acceptance Gate

This slice is complete when:
1. A System Admin can remove a departed user from the user table and all their role assignments are deleted
2. In a non-RBAC deployment, no "System Admins" tab appears in Settings and no "Access" tab appears in team/portfolio detail
3. In an RBAC-enabled deployment, both tabs are visible

## Dependencies

- Slice 01 complete (System Admin must exist)
- Backend: user removal endpoint — **new endpoint required** (design in DESIGN wave)
