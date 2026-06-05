# ADR-057: Wait-Bar Colour-Highlight on the Cumulative Chart (FE-Derived, Colour-Blind-Safe, Composing With Segments) + `computeFlowEfficiencyRag` (Inverted 40/60 Thresholds)

**Status**: Accepted (2026-06-05 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-05
**Feature**: wait-states-flow-efficiency (Story #5173)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: extends ADR-025 (cumulative chart widget) and ADR-028 (chart rendering). Depends on ADR-054 (single FE resolver). Resolves the **wait-bar rendering** and the **D10 RAG** deferred decision.

---

## Context

US-04 requires the cumulative chart's wait-state bars to be visually distinct from active-state bars, colour-blind-safe, composing with the existing completed/ongoing segment split (solid base / hatched top per ADR-022 §4 / ADR-025). US-03 requires a RAG colour on the overview tile with D10 thresholds (`act < 40` / `observe 40–60` / `sustain ≥ 60`), which DISCUSS allowed DESIGN to refine and to decide whether it reuses the `ragRules.ts` pattern.

Code reality:

- `ragRules.ts` exposes `computeCumulativeStateTimeRag(states, terms)` and ~25 sibling `compute*Rag` functions, all returning `RagResult = { ragStatus: "red" | "amber" | "green"; tipText }`. The RAG vocabulary in code is **red/amber/green**, not the D10 `act/observe/sustain` words (those are the human-facing guidance labels; the code statuses are r/a/g).
- The cumulative chart already renders one bar per Doing-category state with a per-state colour and an SVG-`<pattern>` hatched ongoing segment, with established colour-blind care (ADR-025).
- The wait-state membership is the SAME `resolveWaitRawStates(...)` resolver ADR-054 mandates for the chart number (single FE source).

A critical correctness point: **flow efficiency is "higher is better"** — the OPPOSITE polarity of `computeCumulativeStateTimeRag` (where a dominant state holding a LARGE share is BAD). So the efficiency RAG must invert: low efficiency ⇒ red, high ⇒ green. Reusing `computeCumulativeStateTimeRag` directly would invert the meaning.

---

## Decision

### 1. Wait-bar highlight is FE-derived from the single resolver (no new DTO field)

The chart marks a bar as a wait bar via `isWait(state) = resolveWaitRawStates(waitStates, stateMappings, doingStates).has(state)` — the SAME resolver the chart number uses (ADR-054 §2). A mapped wait state highlights ALL its underlying raw-state bars (D11), because the resolver expands it. No `isWaitState` flag is added to `CumulativeStateTimeStateRowDto` (ADR-054 §1). Single source: number + highlight partition the same rows by the same set.

### 2. Visual treatment: distinct colour family + non-colour reinforcement, composing with segments

- Wait bars render in a **distinct colour family** from active bars, while EACH bar still shows its solid completed-contribution base and hatched ongoing-contribution top (ADR-022 §4 segment split is preserved — the wait colouring applies to the bar's base colour; the existing ongoing-hatch `<pattern>` composes on top).
- **Colour-blind-safe (not colour-alone)**: the wait/active distinction is ALSO carried by a **legend entry** ("Wait state") AND a non-colour mark on the bar (a small icon/marker or a pattern accent on the bar label) — reusing the chart's existing colour-blind conventions (the same care ADR-025 applies to the completed/ongoing segments). At least one non-colour channel always conveys "this is a wait bar."
- No new charting dependency: the treatment uses the existing MUI-X `<BarChart>` per-series colour + the existing SVG `<pattern>` mechanism (ADR-025) — no new library, no custom SVG bars.

### 3. No wait states ⇒ no highlight, no regression

When `waitStates` is empty, `resolveWaitRawStates` returns an empty set ⇒ `isWait` is false for every bar ⇒ the chart renders EXACTLY as today (no legend wait entry, no marks). This is the D3/regression guarantee for the chart surface; asserted by a Vitest no-wait-states test.

### 4. `computeFlowEfficiencyRag` — NEW function in `ragRules.ts`, INVERTED thresholds

A NEW `computeFlowEfficiencyRag(efficiencyPercent: number | null, terms): RagResult | undefined` is added to `ragRules.ts` (the established RAG pattern — US-03 reuses it, not a bespoke mechanism):

```
null / not-configured      -> undefined      (tile renders "not configured", no RAG chip — D3)
no-data-in-scope           -> undefined      (tile renders "no data in scope", no RAG chip — D4)
efficiencyPercent <  40    -> red    ("act":      below 40% — most lead time is waiting; investigate the queues)
40 <= efficiencyPercent< 60 -> amber  ("observe":  40–60% — meaningful idle time; watch the wait states)
efficiencyPercent >= 60    -> green  ("sustain":  60% or above — healthy active ratio)
```

The thresholds confirm D10 (40 / 60) but the polarity is **inverted vs `computeCumulativeStateTimeRag`** (low efficiency is bad). The `act/observe/sustain` words appear in the `tipText` guidance; the `ragStatus` is `red/amber/green` matching every other RAG function. The 40/60 boundaries align with the cumulative chart's 40/60 threshold family (shared numeric vocabulary), only the direction differs. `widgetInfoMetadata.ts` `statusGuidance` for `flowEfficiency` documents the three bands in the act/observe/sustain language.

`computeFlowEfficiencyRag` is a SEPARATE function from `computeCumulativeStateTimeRag` (different input — a single percent vs the per-state array — and inverted polarity); it does NOT reuse or wrap it. This is correct duplication of the RAG idiom, not shared knowledge.

---

## Alternatives Considered

**RAG: Option A (chosen): new `computeFlowEfficiencyRag` with inverted 40/60 thresholds.** Reuses the `ragRules.ts` idiom and `RagResult` shape; inverts polarity because efficiency is higher-is-better; confirms D10. Matches every other tile's RAG mechanism.

**RAG: Option B: reuse `computeCumulativeStateTimeRag`.** Rejected — its polarity is inverted (dominant-state-share is bad ⇒ high share = red), so reusing it would paint HIGH efficiency red. Different input shape too (per-state array vs single percent).

**RAG: Option C: different numeric thresholds (e.g. 25/50).** Rejected for MVP — D10 locked 40/60 as the baseline aligned with common flow-efficiency guidance and the cumulative 40/60 family; tunability is out of scope (fold into per-widget RAG-config if asked).

**Highlight: Option A (chosen): FE-derived from the single resolver, distinct colour + non-colour mark, composing with segments.** No DTO change, single source with the number, colour-blind-safe, reuses existing chart mechanisms.

**Highlight: Option B: add `isWaitState` to each cumulative row.** Rejected (ADR-054 §1) — bloats the contract; the FE already holds `waitStates` + `stateMappings` and the resolver; the highlight would still need the resolver for the legend anyway.

**Highlight: Option C: colour-only distinction.** Rejected — fails the colour-blind-safe AC (D6); the chart's existing conventions already pair colour with non-colour channels.

---

## Consequences

**Positive**:

- Highlight + number share one resolver ⇒ the registry HIGH-risk "two surfaces read different lists" item is closed structurally (a mapping's raw states are identical on both surfaces by construction).
- No contract change for the highlight; reuses the existing MUI-X `<BarChart>` colour + `<pattern>` hatch — no new dependency.
- The RAG is the standard `ragRules.ts` idiom, correctly inverted; mostly unit-testable (mutation-friendly), unlike the largely-presentational highlight.

**Negative**:

- The highlight is largely presentational; mutation survivors on the rendering path are expected and justified per project convention (presentational/equivalent survivors documented), with the `isWait` predicate and `computeFlowEfficiencyRag` carrying the real testable surface.

**Neutral**:

- The wait colour family is a design-token choice (software-crafter/designer picks the exact palette within the chart's colour-blind-safe set at GREEN); the architectural requirement is "distinct + non-colour-reinforced + composes with segments."

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Wait bars use `resolveWaitRawStates(...)` (the SAME resolver as the chart number) — no second expansion | Vitest: highlight predicate and number util import the same resolver; mapped wait state highlights all underlying raw-state bars |
| Highlight composes with the completed/ongoing segment split (solid base + hatched top retained) | Vitest DOM test: a wait bar still has the ongoing `<pattern>` hatch |
| Colour-blind-safe: a non-colour channel (legend + mark/pattern) always conveys "wait" | Vitest: legend has a "Wait state" entry; wait bars carry a non-colour mark |
| No wait states ⇒ no highlight, chart unchanged (no regression) | Vitest: empty `waitStates` ⇒ no wait legend entry, no marks; existing chart tests pass unchanged |
| `computeFlowEfficiencyRag`: red < 40, amber 40–60, green ≥ 60 (INVERTED vs cumulative); `undefined` for not-configured/no-data | `ragRules.test.ts` unit test at each boundary + null cases |
| `computeFlowEfficiencyRag` does NOT wrap `computeCumulativeStateTimeRag` (separate, inverted) | Review + unit test asserting opposite polarity at the same percent |

---

## Cross-feature impact

- `state-time-cumulative-view`: the highlight is an additive presentation layer on the existing chart; the bars/segments/RAG of the cumulative chart itself are UNCHANGED (D9 — wait states are a labelling overlay only). `computeCumulativeStateTimeRag` is untouched.
- The overview tile (ADR-055) consumes `computeFlowEfficiencyRag` for its chip; the chart does NOT show the efficiency RAG (the chart shows the efficiency NUMBER per US-02; the systemic RAG lives on the tile per D5/D18).
