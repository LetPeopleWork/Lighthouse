# ADR-061: Named Cycle Time — Ordered-Boundary Duration Computed in the Metrics Layer over the Existing Transition Log (Reuse `CompletedVisits` Primitive), NOT on `WorkItemBase.CycleTime`

**Status**: Accepted (2026-06-08 — Morgan; Fork 1 confirmed by user)
**Date**: 2026-06-08
**Feature**: multiple-cycle-times (Epic 5251)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: consumes the transition log placed by ADR-015/016/017 and the ordered-state universe (`AllStates`) + `GetRawStatesForCategory` resolver (ADR-056 mapping-aware pattern). Sibling to ADR-022 (cumulative state-time algorithm), whose `CompletedVisits` transition-ordering primitive (in `BaseMetricsService`) this decision reuses. Resolves DISCUSS **D1/D2/D10** placement (locked semantics) — this ADR fixes WHERE the generalised derivation lives, not WHAT it computes.

---

## Context

DISCUSS locked (D1) that a named cycle time is `{ name, startState, endState }` with ordered-boundary semantics over `WorkTrackingSystemOptionsOwner.AllStates`, that the regular cycle time is the special case (start = first Doing, end = first Done), and that the existing `StartedDate→ClosedDate` derivation must be **generalised/reused, not parallel-engined**. D2 fixes first-crossing on re-entry; D10 fixes the half-open `[entering startState … entering endState)` window (time-to-reach, not time-in).

Code reality (verified):

- `WorkItemBase.CycleTime` (`Models/WorkItemBase.cs` L53–71) is a hot, widely-consumed `int` property: `StartedDate ?? CreatedDate → ClosedDate`, gated `StateCategory == Done`, defaulting to 1 on a bad closed date. It is read by the scatterplot DTO (`WorkItemDto.CycleTime`), `GetCycleTimePercentilesForTeam`, the cycle-time PBC (`BuildCycleTimeProcessBehaviourChart`, filters `CycleTime > 0`), and `EstimationVsCycleTime`. It is derived from **summary dates**, not the transition log — it has no notion of arbitrary boundary states.
- `BaseMetricsService.CompletedVisits(WorkItem)` (L267–284) already walks `SyncedTransitions` in `TransitionedAt` order, anchored at `StartedDate`, yielding `(state, itemId, days)` per visit. This is the exact transition-ordering primitive a named-window duration needs. `GroupTransitionsByItem` (L18–29) and `ComputeCumulativeStateTime` (L137–158) are siblings on the same data.
- The named duration is fundamentally a **transition-log** computation (first entry into startState-or-later → first subsequent entry into endState-or-later, over `AllStates` order). `WorkItemBase` does not carry the workflow state order or the mapping resolver (`GetRawStatesForCategory` lives on the owner), so it **cannot** compute a named window without being handed the owner's ordered state universe — a dependency `WorkItemBase` does not and should not have.

The fork: generalise `WorkItemBase.CycleTime` into a parameterised "duration between two ordered boundaries" (regular CT = special case on the model) **vs** add the generalised derivation as a sibling computation in the metrics layer that reuses `CompletedVisits`/the transition-ordering helper, leaving `WorkItemBase.CycleTime` untouched.

---

## Decision

**Compute the named ordered-boundary duration in the metrics layer (`BaseMetricsService`), as a new pure static helper that reuses the existing transition-ordering primitive. Do NOT touch `WorkItemBase.CycleTime`.**

### 1. New `BaseMetricsService` primitive (shared Team + Portfolio)

A new protected static helper computes, for one `WorkItem` and one resolved ordered-boundary pair, the half-open `[enter start … enter end)` duration in days (D10), using first-crossing (D2) over the owner's `AllStates` order (D1):

```
NamedCycleTimeDays(WorkItem item, IReadOnlyList<string> allStatesInOrder,
                   string startState, string endState) : int?
```

- Resolves the **start index** = first transition whose `ToState` (or `StartedDate` anchor) is `startState` **or any later state in `allStatesInOrder`** (D1 "OR any later state"); resolves the **end index** = first transition **at or after the start moment** into `endState` **or any later state** (D1). Returns `null` when the item never crosses **both** boundaries (D9 — excluded from the series).
- Duration uses the SAME inclusive day-difference convention as `WorkItemBase` (`(end.Date − start.Date).TotalDays + 1`), so a named series reads on the same axis units as the default cycle time. Returns days as `int` to match `WorkItemDto.CycleTime`.
- It reuses `CompletedVisits`/`GroupTransitionsByItem` ordering semantics (same `OrderBy(TransitionedAt)`, same `StartedDate` anchor) so re-entries collapse to first-crossing exactly as the cumulative computation already does (D2).
- The **default** cycle time stays exactly as today (`WorkItemBase.CycleTime`); the named helper is the GENERALISATION, with regular CT being the conceptual special case (start = first Doing raw state, end = first Done raw state) — but the default path is NOT re-routed through the new helper in MVP (see §3, blast-radius).

### 2. Mapping resolution via the existing resolver

`startState`/`endState` saved on the definition may be a State-Mapping name or a raw state (D3). They are resolved to raw states via the EXISTING `owner.GetRawStatesForCategory([state])` (ADR-056 pattern) before the index lookup; `allStatesInOrder = owner.AllStates` (already mapping-expanded, ordered ToDo++Doing++Done — verified L26–28). NO second resolver is written.

### 3. `WorkItemBase.CycleTime` is NOT generalised (blast-radius)

The default `CycleTime` property is left byte-identical. Re-parameterising it would (a) force `WorkItemBase` to depend on the owner's ordered state universe + mapping resolver (a model→settings dependency it must not have), (b) change a summary-date computation into a transition-log one for ALL its call sites (percentiles, PBC `CycleTime > 0` filter, estimation scatter), and (c) risk regressing the default scatterplot whose render-time is a named guardrail (DISCUSS guardrails). The DISCUSS "generalise/reuse, no parallel engine" requirement is satisfied by reusing the `CompletedVisits` transition-ordering primitive — the named helper is NOT a parallel engine, it is the same ordering walk parameterised by boundary states. A future refactor MAY route the default through the named helper with (startDoing, firstDone); that is out of scope and explicitly deferred.

### 4. Series + percentiles assembly

The Team/Portfolio metrics service maps the in-window closed items (`GetWorkItemsClosedInDateRange`/`GetClosedItemsForTeam`, the SAME source the default scatter uses) through `NamedCycleTimeDays`, drops `null`s (D9 exclusion), and projects to the EXISTING `WorkItemDto` shape with `CycleTime` carrying the named duration (so the scatter FE renders unchanged — see ADR-062). Percentiles reuse the existing `PercentileCalculator` over the non-null named durations, producing the same `PercentileValue` 50/70/85/95 shape as `GetCycleTimePercentilesForTeam`.

---

## Alternatives Considered

**Option A (chosen): metrics-layer helper reusing `CompletedVisits`, `WorkItemBase.CycleTime` untouched.**

- Pros: zero blast radius on the hot default property and its ~4 call-site families; the named computation lives where the transition log and the ordered state universe already are (the metrics services already hold the owner and call `GetRawStatesForCategory`); reuses the proven `CompletedVisits` ordering (D2 for free); the `int` duration drops straight into the existing `WorkItemDto.CycleTime`/scatter path. Satisfies DISCUSS "generalise/reuse, no parallel engine" via primitive reuse.
- Cons: the regular cycle time and the named cycle time are computed by two code paths in MVP (summary-date vs transition-log), rather than one unified parameterised path. Accepted: they are reconciled by the shared day-difference convention and the deferred refactor note; unifying now would impose the model→settings dependency this option exists to avoid.

**Option B: generalise `WorkItemBase.CycleTime(startBoundary, endBoundary)` on the model.**

- Pros: the most literal "regular CT = special case" — one parameterised method, model-pure single source.
- Cons: `WorkItemBase` would need the owner's ordered `AllStates` + the mapping resolver injected per call — a model→settings coupling that breaks the current summary-date self-containment; converts a summary-date computation to a transition-log one for every existing caller (percentiles, PBC, estimation), a high-blast-radius change to shipped, tested hot paths with a render-time guardrail. Rejected for blast radius; the desired "special case" relationship is preserved conceptually and reachable via the deferred unification.

**Option C: brand-new standalone `NamedCycleTimeCalculator` service, independent of `BaseMetricsService`.**

- Pros: clean separation; testable in isolation.
- Cons: duplicates the `CompletedVisits`/`GroupTransitionsByItem` transition-ordering logic that already lives in `BaseMetricsService` — a second implementation of the exact ordering semantics (D2) that must stay consistent with the cumulative computation (US-04 scopes the SAME window). Violates DISCUSS "no parallel engine" and the cross-surface-consistency risk (the named window must read identically on the scatter AND the cumulative scope). Rejected — the helper belongs next to its sibling primitives.

---

## Consequences

**Positive**:

- The default cycle time and the whole default scatter/percentile/PBC/estimation surface are untouched — no regression risk on shipped hot paths; render-time guardrail protected.
- One transition-ordering implementation (`CompletedVisits` family) backs the cumulative computation AND the named-window duration — the scatter named duration and the US-04 cumulative scope are forced to agree (the cross-surface-consistency risk is structurally mitigated, not just tested).
- Mapping resolution reuses `GetRawStatesForCategory` — one resolution rule everywhere (config, scatter, cumulative).

**Negative**:

- Two computation paths for "a cycle time" in MVP (default summary-date, named transition-log). Mitigated by the shared day-difference convention and the documented deferred unification.

**Neutral**:

- The named duration is `int` days to match `WorkItemDto.CycleTime`; sub-day precision is intentionally not introduced (parity with the default scatter).

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Named duration reuses the transition-ordering primitive (no second ordering impl) | NUnit on `NamedCycleTimeDays`; ArchUnitNET/grep: no new `OrderBy(TransitionedAt)` walk outside `BaseMetricsService` |
| `WorkItemBase.CycleTime` is NOT modified by this feature | Git-diff review gate; component-decomposition marks `WorkItemBase` NO-CHANGE |
| Mapping resolution via existing `GetRawStatesForCategory` (no second resolver) | NUnit: mapping-name boundary "Validation" → raw states expands; grep: no new resolver |
| First-crossing on re-entry (D2); half-open `[enter start … enter end)` (D10) | NUnit fixtures: PHX-211 re-open uses first crossing; end = entry moment, end-state dwell excluded |
| Item crossing neither/one boundary returns `null` (D9 exclusion) | NUnit: item never reaching endState ⇒ excluded from series |
| Named duration day-difference matches the default convention | NUnit: PHX-204 Planned(day0)→Done(day47) ⇒ 47 (parity with `WorkItemBase` convention) |

---

## Cross-feature impact

- `state-time-cumulative-view`: US-04 scoping reuses the SAME boundary resolution + transition ordering (ADR-063); no change to the unscoped cumulative endpoints/DTOs.
- `wait-states-flow-efficiency` (ADR-054/056): unaffected — flow efficiency folds over Doing-category rows; the named window is an orthogonal overlay.
- Default cycle-time scatter / percentiles / PBC / estimation: UNCHANGED.
