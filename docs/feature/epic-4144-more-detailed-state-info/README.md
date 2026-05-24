# Epic 4144: More Detailed State Info — Slice Catalog

**ADO**: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144
**Status**: Planned, forecast 2026-06-03
**Tags**: Community, Productboard

Tracks the carpaccio split of Epic 4144 into shippable features. Each row below points to its own DISCUSS-onwards lifecycle when ready. The Epic stays open until every slice has a corresponding ADO Story marked Done.

## Slice map

| # | Slice | Feature ID | Primary persona | Status | Notes |
|---|---|---|---|---|---|
| A+B1+D | Capture transitions + per-item live badge + Team/Portfolio threshold | `time-in-state-and-staleness` | flow-coach | DISCUSS (in progress) | First customer-visible release |
| F | Pace-percentiles bands on Work Item Aging chart (ActionableAgile-style) | TBD `aging-pace-percentiles` | flow-coach + PM | Planned (next) | Strongest differentiated UX; needs distribution statistics |
| B2 | Per-item historical state breakdown (outlier deep-dive) | TBD `work-item-state-history-view` | Product Owner | Planned | Reuses transition data captured by A |
| B3 | Cumulative time-per-state across timeframe | TBD `state-time-cumulative-view` | Delivery Lead / RTE | Planned | Aggregation view; powers leadership conversations |
| C | Detailed CFD using actual workflow states | TBD `detailed-cfd` | flow-coach | Planned | Replaces Simplified CFD as an option (not default initially) |

> Blocked-time history was originally scoped here as slice E. It has since been promoted to its own Epic — **#5074** — because it needs a different capture mechanism. See `docs/feature/epic-5074-blocked-items/README.md`.

## Cross-cutting design decisions (apply to all slices)

- **State transitions: source-of-truth first.** Jira and ADO already read transitions to derive Started/Closed; extend that mechanism to capture all state changes. Linear: investigate at DESIGN of `time-in-state-and-staleness` (GraphQL `IssueHistory` is the candidate). CSV + any connector that can't expose history: sync-side delta fallback (compare to last-known state on every sync; resolution = sync cadence).
- **Shared data foundation.** All slices consume the `WorkItemStateTransition` data built in `time-in-state-and-staleness`.
- **Three views of "time in state", one foundation.** B1 (live, per-item, current state), B2 (historical, per-item breakdown), B3 (cumulative across items in a timeframe) are different UX layers on the same transition data — captured once in A, consumed many times.

## When this Epic is "done"

When every slice above has a corresponding ADO Story marked Done AND the resulting feature has been archived to `docs/evolution/`.

## Why a carpaccio split (not one mega-feature)

Each slice targets a distinct persona / decision and can ship independently. Shipping A+B1+D first proves the data foundation and the flow-coach persona's value loop. F, B2, B3, C then each become focused, shippable improvements without re-litigating the foundation.
