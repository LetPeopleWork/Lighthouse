# ADR-014: Throughput Chart Toggle Delivery — Run Chart Client-Side, PBC Backend `?view=` (Split by Payload Shape)

**Status**: Accepted (2026-05-20)
**Date**: 2026-05-20
**Feature**: filter-forecast-throughput (Epic 4896) — slice 03
**Decider**: Morgan (Solution Architect), interaction mode PROPOSE

---

## Context

DISCUSS-wave decision D1 locked the user-visible behaviour: the Throughput Run Chart and the Throughput PBC each render a per-view `Show: Raw | Filtered` toggle (visible only when the team has a non-empty filter configured AND the tenant is premium). Default is **Raw** to preserve today's behaviour (cross-cutting invariant #1). Flipping to Filtered re-renders the chart with items matched by the team's rule set removed; flipping back restores the Raw view.

The implementation choice DISCUSS deferred to DESIGN: **does the toggle apply the filter client-side, or does the backend produce a filtered payload on demand?**

Two reference points narrow the answer:

- The **Run Chart** endpoint `GET /api/teamMetrics/{teamId}/throughput` returns `RunChartData`, whose shape is `Dictionary<int, List<WorkItemBase>> WorkItemsPerUnitOfTime` — the **full work item objects per day** (Type, State, Tags, ParentReferenceId, AdditionalFieldValues — every field the D9 rule schema can evaluate against).
- The **PBC** endpoint `GET /api/teamMetrics/{teamId}/throughput/pbc` returns `ProcessBehaviourChart`, whose `DataPoints[]` is `ProcessBehaviourChartDataPoint(string XValue, double YValue, IEnumerable<SpecialCauseType> SpecialCauses, int[] WorkItemIds, bool IsBlackout = false)`. The payload exposes ONLY the work-item IDs per data point — no Type, no State, no Tags, no rule-evaluable field.

This asymmetry is structural, not an oversight. PBC is a statistical chart (average, UNPL, LNPL); the data points are aggregate counts. The IDs are present for hover-tooltips, not for downstream filtering.

---

## Decision

**Split by payload shape**:

- **Run Chart**: client-side filter. The existing endpoint returns sufficient per-item granularity; flipping the toggle applies the rule set in the browser against the cached payload. No backend round-trip. No API change.
- **PBC**: backend filter via a new query parameter `?view=raw|filtered` on `GET /api/teamMetrics/{teamId}/metrics/throughput/pbc`. Default `view=raw`. `view=filtered` triggers `ThroughputFilterMode.ApplyFilter` on the call into `ITeamMetricsService.GetThroughputProcessBehaviourChart` (DDD-4 seam), which applies the filter on the source closed-items list BEFORE the PBC statistical computation runs. The response also gains a `FilterApplied: bool` field so the chip renders correctly.

Both chart frontend components share the same toggle visual contract (header control, two button-like options, "Show:" prefix). The two surfaces differ only in their `onToggleChange` handler:

- Run Chart: in-memory filter-and-reduce over the cached `WorkItemsPerUnitOfTime` payload.
- PBC: refetch with the new `?view=` value (an `axios` GET call, ≤200ms round-trip on a normal-sized team).

For the **client-side Run Chart filter**, slice 03 implements a minimal TypeScript port of the D9 field-key evaluator: `evaluateCondition(workItem: IWorkItem, condition: IDeliveryRuleCondition) → boolean` and `matchesAllConditions(workItem, conditions) → boolean`. The TS implementation is ≈40 LOC and parallels the C# `RuleEvaluator<WorkItem>` with `WorkItemFieldProvider`. A Vitest "operator parity" test fixes inputs on both sides and asserts the same boolean for each of the (operator × field-type) combinations — this catches drift between FE and BE evaluator implementations.

---

## Alternatives Considered

### Option A — Both charts backend-filtered via `?view=`

Add `?view=raw|filtered` to BOTH endpoints. The FE toggle issues a refetch on every flip for both charts.

**Rejected.** For the Run Chart this adds:

- A network round-trip on every toggle flip (poor UX — the filter is a "tell me a different story" action, expected to be instant).
- Backend code that duplicates the rule-evaluation work already cached client-side.
- A cache-key explosion: `Throughput_<dates>_<viewMode>` doubles the metrics cache pressure when the actual filtered data is reconstructible client-side.

The Run Chart's payload already carries the data; the FE filter is essentially free. Asking the BE to do it is asking the BE to do work the FE can do in zero ms.

### Option B — Both charts client-side filtered

Add a backend extension to the PBC endpoint that includes per-data-point Type/State/Tags/ParentReferenceId so the FE can filter the PBC items client-side too.

**Rejected.** PBC payload size would balloon — typical PBC has 90-180 data points × however many items per day. Multiplying each `int[] WorkItemIds` by the full WorkItemBase shape (8+ fields) inflates the response from a few KB to potentially hundreds of KB. Worse: the PBC's statistical fields (Average, UpperNaturalProcessLimit, LowerNaturalProcessLimit) are computed BEFORE the WorkItemIds are aggregated — to filter client-side, the FE would have to re-run the PBC statistical computation in TypeScript, which is a non-trivial port and a duplication-of-knowledge risk in the heart of the SPC math.

The cost (payload bloat + duplicated SPC math in TS) is much larger than the cost (one network round-trip on toggle flip) of letting the BE do the work.

### Option C — One endpoint, one shape, one rule (always backend OR always client)

**Rejected as a category.** The two endpoints have structurally different payloads because they serve structurally different chart types. Forcing them onto the same delivery mechanism subordinates a real engineering signal to a cosmetic uniformity preference. The split decision is the lower-cost option by every measure (payload size, network round-trips, code complexity, cache pressure).

---

## Consequences

### Positive

- **Run Chart toggle is instant** — no network round-trip, no spinner, no jank. Aligned with the journey emotional arc ("equipped — one click apart").
- **PBC toggle is correct** — the statistical fields are re-computed on the filtered source list, not approximated client-side. The Average / UNPL / LNPL match what they would be if the filter were the team's permanent setting.
- **Default behaviour invariant (D1 / cross-cutting #1) is upheld** by both endpoints' defaults — Run Chart unchanged, PBC `view=raw` default.
- **Cache friendliness** — the PBC cache key on `TeamMetricsService` includes the new `ThroughputFilterMode` enum value, so filtered and unfiltered series cache independently.
- **The FE evaluator port is small and bounded** — ≈40 LOC, three operators, one field provider analogue. The Vitest operator-parity test catches any drift between the FE and BE evaluators.

### Negative

- **Two slightly different toggle implementations** — the Run Chart toggle is an in-memory `useMemo` filter; the PBC toggle is a `useQuery` refetch. The mental model differs slightly per chart. Mitigated by a shared `<FilteredThroughputChip>` and a shared header layout component (DRY on the visual, intentional divergence on the data-fetch path).
- **The FE evaluator is a duplication of the BE evaluator's semantics** — operator semantics live in both places. Strictly this is "duplication of knowledge" per CLAUDE.md DRY rules. Mitigated by:
  - The Vitest operator-parity test asserting identical input → identical boolean (catches semantic drift).
  - The FE evaluator's file header carrying a single-line comment pointing at the C# `RuleEvaluator<T>` (no decorative banner, no provenance label — just a tight cross-reference that survives the no-comments default per CLAUDE.md because it is the rare "non-obvious WHY" that prevents a future maintainer from "fixing" the FE evaluator and silently desynchronising.)
- **PBC payload-shape decision is now load-bearing** — if a future PBC change adds Type/State/Tags to `ProcessBehaviourChartDataPoint`, this ADR's split rationale shifts. That change should explicitly reconsider this ADR.

### Sensitivity / trade-off points

- **Sensitivity**: an unusually large Run Chart payload (e.g. a team configured with `ThroughputHistory = 365` and thousands of items) could make the initial fetch heavy. This is already today's behaviour — the chart payload structure is unchanged. The toggle does not amplify the cost.
- **Trade-off**: PBC toggle has a network round-trip; users may flip back and forth multiple times in a meeting. Backend cache (keyed on team + dates + mode) eliminates re-computation cost after the first flip in each direction; only network latency remains, typically <200ms.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Run Chart endpoint payload carries per-item granularity sufficient for client-side rule evaluation | Backend integration test asserts `GET /api/teamMetrics/{teamId}/throughput` response contains `Type`, `State`, `Tags`, `ParentReferenceId`, `AdditionalFieldValues` on every item — fails if a future refactor strips fields. |
| PBC `?view=filtered` invokes `ThroughputFilterMode.ApplyFilter` (and `?view=raw` or absent invokes `RespectTeamSetting`) | Backend integration test parameterised over the three cases (omitted, `raw`, `filtered`); asserts the resulting Average / UNPL / LNPL differ between `raw` and `filtered` when a non-trivial filter is configured. |
| Frontend rule evaluator stays in lockstep with the backend evaluator | Vitest operator-parity test: parameterised over (`operator` × `fieldType`) combinations, runs the same input through the TS `evaluateCondition` and asserts the expected boolean (the expected booleans are derived from the C# evaluator's documented semantics — equals/notEquals are case-insensitive, contains is case-insensitive substring, tags-field semantics apply per-tag). |
| FE evaluator is invoked ONLY from the throughput Run Chart toggle (not used elsewhere) | Vitest "import-graph" test or Biome custom rule asserting `evaluateCondition.ts` is imported only by the Run Chart widget. Prevents the FE evaluator from accidentally becoming the source of truth for other rule-based features. |
