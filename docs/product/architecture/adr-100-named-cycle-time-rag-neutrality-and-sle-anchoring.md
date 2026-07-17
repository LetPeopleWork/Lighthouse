# ADR-100: Cycle Time Percentiles RAG Is NEUTRAL Under a Named Cycle Time Selection — the SLE Is Default-Window-Anchored and Single-Per-Owner; No Per-Definition SLE

**Status**: Accepted (2026-07-17 — Morgan, interaction mode PROPOSE; user-confirmed)
**Date**: 2026-07-17
**Feature**: flow-overview-named-cycle-time (ADO Story #5509)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: consumes the named-cycle-time computation (ADR-061) and read contract (ADR-062), and the validity SSOT (ADR-063). Resolves DISCUSS **D11**. Sibling to ADR-101 (this feature's trend contract).

---

## Context

Story 5509 brings the named-cycle-time selection (shipped by Epic 5251 on the Flow Metrics scatterplot) to the Flow Overview `percentiles` widget (`CycleTimePercentiles`, `categoryMetadata.ts:62`, `size: "small"`). The widget carries a RAG footer. That footer must do *something* when the user selects a named cycle time.

Code reality (verified):

- The RAG for this widget is computed by `computeCycleTimePercentilesRag(sle, cycleTimes, terms)` (`Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.ts:174`). It is **entirely SLE-anchored**: with no SLE it returns red ("Define an SLE…"); otherwise it compares the fraction of items within the SLE target against `sle.percentile` and returns green / amber / red.
- The SLE is a **single pair per owner** — `ServiceLevelExpectationProbability` + `ServiceLevelExpectationRange` on `WorkTrackingSystemOptionsOwner` (`WorkTrackingSystemOptionsOwner.cs:33-35`). There is exactly one, and it is defined by the user against the **default** started→finished cycle time (the only cycle time that existed when the SLE field was designed).
- A named cycle time is a **different, generally wider window** (ADR-061: half-open `[enter start … enter end)` over `AllStates`; e.g. the demo's `Lead Time (End to End)` = Backlog→Done is wider than the default, which starts at the first Doing state).

The problem: feeding a named window's durations into `computeCycleTimePercentilesRag` against the default-anchored SLE produces a **semantically false verdict**. A team with SLE "85% @ 12 days" (a true statement about its build cycle) would see its `Lead Time (End to End)` — legitimately 47 days at P85 — render **RED**, implying a breach where none exists. The SLE simply does not describe that window.

---

## Decision

**When a named cycle time is selected, the Cycle Time Percentiles widget's RAG is NEUTRAL (`ragStatus: "none"`) with an explaining tip. The SLE-anchored RAG is computed ONLY for the Default selection.**

### 1. Neutral RAG for named, unchanged RAG for default

- **Default selected** (`selectedDefinitionId === null`): `computeCycleTimePercentilesRag(sle, cycleTimes, terms)` exactly as today — byte-identical behaviour, including the no-SLE red.
- **Named selected**: `ragStatus: "none"`, `tipText: "SLE applies to the {cycleTime} Default window. Named cycle times have no SLE target."` (terminology-term interpolated via the existing `useTerminology()`). No green/amber/red is derived.

`ragStatus: "none"` is an existing, rendered state (`WidgetShell` already maps `"none"` — the RAG chip is suppressed and the tip shows), so this needs no new rendering primitive.

### 2. The neutrality is a property of the *selection*, computed where the selection lives

Per this feature's D13 (see the brief delta and ADR — selection state is lifted to `BaseMetricsView`), the RAG-footer value for `percentiles` is chosen at the same level that owns the selection: named ⇒ the neutral footer; default ⇒ the existing `computeCycleTimePercentilesRag` result. No named durations are ever passed into `computeCycleTimePercentilesRag`.

### 3. No SLE line in View Data under a named selection

Consistent with the RAG neutrality: the View Data dialog draws no SLE reference under a named selection (the `sle` field of the ViewData payload is omitted for named). The SLE describes only the default window; drawing it against named durations would reintroduce the same false comparison in the table.

### 4. No per-definition SLE (explicitly rejected for now)

We do **not** add an SLE to `CycleTimeDefinition`. A named window has no RAG target; it is read as a distribution, not judged against a threshold. See Alternatives.

---

## Alternatives Considered

**Chosen: neutral RAG for named, SLE-anchored RAG for default only.**

- Pros: honest — the widget never asserts a breach it cannot substantiate; zero backend change (RAG stays a pure FE computation on the default path); reuses the existing `"none"` render state; the tip *teaches* the user why (SLE ⇄ default window) rather than hiding silently. The Default experience is byte-identical, protecting the no-regression guardrail.
- Cons: a user might expect RAG on their named window and be mildly disappointed. Mitigated by the explaining tip, which converts confusion into understanding and points at the concept.

**Rejected: per-definition SLE (`CycleTimeDefinition` gains `SleProbability` + `SleRange`).**

- Would make RAG meaningful for named windows. But: reopens the Epic 5251 settings/config surface (the cycle-time editor, ADR-064), needs an EF migration on the owned collection, new save-time validation, and a UI to set it — a materially larger feature than 5509's read-only scope. No user has asked to *judge* a named window against a target; the demand is to *see* it. Deferred as a candidate follow-up, recorded here so the question is not re-litigated from scratch.

**Rejected: reuse the default SLE against named durations.**

- Simplest code (no branch). But knowingly ships the false red described in Context — a correctness defect dressed as a feature. Rejected outright.

**Rejected: hide the RAG footer entirely for named.**

- No false verdict, but also no explanation; the user is left wondering where the footer went. The neutral-with-tip option costs the same and teaches instead of vanishing.

---

## Consequences

**Positive**:
- The widget is never wrong: a named window shows a distribution and an honest "no target here", never a fabricated breach.
- Zero backend change for RAG; the default RAG path and the no-SLE behaviour are untouched (no-regression guardrail protected by construction).
- The tip creates a teaching moment about what the SLE actually anchors to.

**Negative**:
- Named windows have no at-a-glance health colour. Accepted — that is the honest state until (and unless) a per-definition SLE is designed.

**Neutral**:
- If per-definition SLE is ever built, this ADR is its natural starting point (it already frames the trade-off and the rejected alternative).

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Default selection ⇒ `computeCycleTimePercentilesRag` result byte-identical to today | Vitest golden test on the widget footer, default selection |
| Named selection ⇒ `ragStatus: "none"` + the SLE-anchoring tip; never green/amber/red | Vitest: select a named definition ⇒ assert `"none"` + tip text; assert `computeCycleTimePercentilesRag` is NOT called with named durations |
| No SLE line drawn in View Data under a named selection | Vitest on the ViewData payload: `sle` omitted when a named definition is selected |
| `CycleTimeDefinition` gains NO SLE field | Grep/type check: no `Sle*` on `CycleTimeDefinition` / its DTO |

---

## Cross-feature impact

- **Default RAG callers** (every non-named use of the percentiles widget): UNCHANGED.
- **Future named surfaces** (`cycleTimePbc`, any other SLE-judged chart that gains a named selector): inherit this rule — a named window has no SLE target; render neutral. This ADR is the reference.
- **Backend**: none — RAG is a frontend computation; this decision adds no server behaviour.
- **Lighthouse-Clients**: none — RAG is not exposed via the clients.
