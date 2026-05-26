# Epic 4144: More Detailed State Info — Slice Catalog

**ADO**: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144
**Status**: Planned, MVP forecast 2026-06-03
**Tags**: Community, Productboard

Tracks the carpaccio split of Epic 4144 into shippable features. The MVP release ships three features together (see below); post-MVP features ship independently afterwards. Each feature has its own DISCUSS-onwards lifecycle.

## MVP release bundle

The Epic 4144 MVP ships **all three** features below together. The MVP gate does not lift until each has reached Done.

| # | Slice | Feature ID | Primary persona | Status | Role in MVP |
|---|---|---|---|---|---|
| A+B1+D | Capture transitions + per-item live badge + Team/Portfolio threshold | `time-in-state-and-staleness` | flow-coach | DISCUSS complete | Data foundation + triage signal (per-item) |
| F | Pace-percentiles bands on Work Item Aging chart (ActionableAgile-style) | `aging-pace-percentiles` | flow-coach + PM | PRE-DISCUSS stub | Competitive-parity story; chart-glance pace recognition |
| B3 | Cumulative time-per-state across timeframe (incl. ongoing items) | `state-time-cumulative-view` | Delivery Lead / RTE | PRE-DISCUSS stub | Leadership / retro view; "where do we spend our time?" |

**Why these three together**: A+B1+D alone ships per-item triage but doesn't answer "where does my team spend its time?" (B3) or "are my in-flight items pacing OK vs history?" (F). The product call (user-driven, 2026-05-24) is that an MVP without F and B3 would not close the perceived gap with ActionableAgile and would not give leadership the aggregate view it needs alongside the per-item view.

## Post-MVP features

Ship independently after the MVP bundle. No dependency between these and the MVP closure.

| # | Slice | Feature ID | Primary persona | Status | Notes |
|---|---|---|---|---|---|
| ~~B2~~ | ~~Per-item historical state breakdown (outlier deep-dive)~~ | ~~`work-item-state-history-view`~~ | Product Owner | **Absorbed into B3 (2026-05-26)** | Distribution lens ("where did this item's time go, per state") now ships in `state-time-cumulative-view` via the US-05 item-picker single-item selection (D15). Only the *chronology* lens (ordered transition timeline / re-entries with dates) was dropped — revisit as a follow-up if telemetry/users ask. |
| C | Detailed CFD using actual workflow states | TBD `detailed-cfd` | flow-coach | Planned | Replaces Simplified CFD as an option (not default initially) |

> Blocked-time history was originally scoped here as slice E. It has since been promoted to its own Epic — **#5074** — because it needs a different capture mechanism. See `docs/feature/epic-5074-blocked-items/README.md`.

## Cross-cutting design decisions (apply to all features)

- **State transitions: source-of-truth first.** Jira and ADO already read transitions to derive Started/Closed; extend that mechanism to capture all state changes. Linear: investigate at DESIGN of `time-in-state-and-staleness` (GraphQL `IssueHistory` is the candidate). CSV + any connector that can't expose history: sync-side delta fallback (compare to last-known state on every sync; resolution = sync cadence).
- **Shared data foundation.** All features consume the `WorkItemStateTransition` data built in `time-in-state-and-staleness`. F and B3 are downstream consumers — neither rebuilds the capture.
- **Views of "time in state", one foundation.** B1 (live, per-item, current state) and B3 (cumulative across items in a timeframe, plus a per-item/subset distribution lens via the B3 item picker) are different UX layers on the same transition data — captured once in A, consumed many times. B2's per-item distribution breakdown was absorbed into B3 on 2026-05-26 (D15); only its chronology/timeline lens remains un-built (deferred, not committed).

## When this Epic is "done"

- **MVP done**: all three MVP-bundle features have their ADO Stories Done AND each feature has been archived to `docs/evolution/`.
- **Epic done**: MVP done AND the remaining post-MVP feature (C) has shipped and been archived. (B2 was absorbed into B3 on 2026-05-26 — D15 — so it is no longer a separate ship; its dropped chronology lens, if ever revived, would be a new feature, not a B2 reopening.)

## Why a carpaccio split with an MVP bundle (not one mega-feature, not three separate ships)

Each feature keeps a distinct persona and JTBD — that's the carpaccio principle preserved. But three feature-deltas with their own DISCUSS / DESIGN / DELIVER each is cheaper to plan and review than one mega-feature, and the MVP bundle lets the release-coordination concern ("ship together") live at the Epic level instead of polluting any individual feature scope. C remains a truly independent ship afterwards (B2 was absorbed into B3 on 2026-05-26 — D15).
