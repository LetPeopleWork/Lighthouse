<!-- markdownlint-disable MD024 -->
# Epic 5074 — Blocked Items — Enhancement Backlog (pre-DISCUSS)

Feature-id: `epic-5074-blocked-items` | Date: 2026-07-07 | Status: raw backlog, input to `/nw-discuss`

These are NET-NEW enhancements on top of the shipped rule-based blocked foundation (slices 01–04).
They are **not** slice-04/05 of the original carpaccio — they extend the delivered surface
(`BlockedOverviewWidget`, blocked-over-time chart, `BlockedCountSnapshot` history). Collected here so
DISCUSS can run JTBD + AC on the set as a cohesive follow-up epic/slice group.

Grounding docs: [feature-delta.md](./feature-delta.md), ADR-069 (`BlockedCountSnapshot` + over-time
endpoint), ADR-068 (`WorkItemBlockedTransition`). Overview widget = non-premium
(`BlockedOverviewWidget.tsx`, no premium gate).

## Backlog Items

### B1 — Chart-bar click-through to a blocked-items dialog

- **Idea**: Click a bar in the blocked-items-over-time chart → open a `WorkItemsDialog` listing the items
  that were blocked at that point in time.
- **User value**: The chart shows *how many* were blocked; the practitioner's next question is always
  *which ones*. Closes the loop from trend to action.
- **Known constraint (schema decision)**: `BlockedCountSnapshot` (ADR-069) persists only a **count**, not
  membership. "Which items were blocked at past date T" is not currently reconstructable. Options for
  DISCUSS/DESIGN to weigh:
  - **(a) Current-only click-through** — dialog only for the latest/live point using computed
    `IsBlocked`; historical bars non-interactive. Zero schema change, ships immediately.
  - **(b) Persist membership** — add an item-id set per snapshot (amend ADR-069, expand-only migration).
    Enables true historical membership; storage + backfill cost.
  - **(c) Reconstruct from transitions** — derive membership at T from `WorkItemBlockedTransition`
    (ADR-068) enter/leave intervals. No new storage, but only valid back to when transition capture
    started; heavier query.
- User previously flagged this one as "design mini-loop now" candidate.

### B2 — RAG status on the blocked overview widget

- **Idea**: Show a Red/Amber/Green health indicator on the `BlockedOverviewWidget` driven by the blocked
  count (and/or blocked duration) against configurable thresholds.
- **User value**: At-a-glance health signal on the dashboard without reading the number.
- **Open questions for DISCUSS**: what drives the color — absolute blocked count, % of WIP blocked, or
  max/avg blocked age? Threshold configuration surface (per-team/portfolio settings vs sensible
  defaults)? Reuse of any existing RAG/threshold idiom in the app.

### B3 — Previous-period trend indicator on the overview widget

- **Idea**: On the overview widget, compare the current blocked count against the blocked count on the
  **last day of the previous period**, and show the delta / direction (up/down/flat).
- **User value**: Is blocking getting better or worse? Cheap, high-signal.
- **Data availability**: History already exists in `BlockedCountSnapshot` (ADR-069) — no new capture
  needed; only a lookback query + delta render.
- **Open questions for DISCUSS**: what defines "the period" (dashboard date range vs a fixed cadence)?
  Trend arrow only vs numeric delta vs sparkline?

## Notes / out of scope for this backlog

- Original slice 05 (#5269, Jira predefined flagged field, MoSCoW *Could*) is a separate pending item,
  not part of this enhancement set.
- Deferred bug (logged in `deliver/review-slices-01-04.md`): blocked rule-set validation is a silent
  no-op (case-sensitive deserialize). Bug, not an enhancement — track separately.
