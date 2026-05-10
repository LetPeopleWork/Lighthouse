# Slice 04: Viewer Experience Polish

**Feature**: rbac-enhancements
**Delivery Sequence**: 4 of 4
**Estimated Effort**: 1-2 days

## Learning Hypothesis

Viewers navigate Lighthouse from login to forecast review without encountering a single 403 error, broken disabled control, or admin section that does not apply to them.

## Stories in This Slice

| Story | Priority | What it delivers |
|---|---|---|
| US-09 | P2 | Viewer experience: read-only deliveries, no admin controls, gated System Settings |

## Design Decisions Resolved in This Slice

- Q7: Work Tracking Systems section hidden for non-System-Admins; teams/portfolios hidden if user has no access
- Q8: License Info visible to all (read-only); Upload button shown only to System Admins
- Q11: Deliveries visible to Viewers (read-only); Add/Edit/Delete hidden
- Q12: Quick Settings hidden from Viewers
- Q15: Log Level hidden from non-System-Admins
- Q18: Work Tracking System connections restricted to System Admins only; non-admins get 403 silently

## Acceptance Gate

This slice is complete when:
1. Morgan (Viewer for Team Alpha) can log in, review Team Alpha's forecast and deliveries, and navigate to System Settings — without triggering a single 403 error or seeing a broken control
2. Morgan sees License Info (read-only) and nothing else in System Settings

## Dependencies

- Slice 03 complete (scoped admin controls must be correctly gated first, as some viewer logic is inverse of admin logic)
- Frontend changes only: no new backend endpoints required for this slice
