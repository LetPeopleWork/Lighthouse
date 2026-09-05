# ADR-188: One Pace-Band Ladder, Read by Both the Chart's Geometry and the Dialog's Cell Value

**Status**: Accepted (2026-09-05 — Morgan, DESIGN wave, interaction mode PROPOSE). No code implements it yet; `story-5884-work-item-age-bands` slice 01 is the first commit that will.
**Date**: 2026-09-05
**Feature**: story-5884-work-item-age-bands (ADO User Story #5884)
**Decider**: Morgan (Solution Architect)

---

## Context

`aging-pace-percentiles` (ADR-019, ADR-020, ADR-053) shipped per-state pace bands as coloured zones
painted behind the dots of the Work Item Aging chart. The classification and the geometry of those
zones live together in one exported function, `computePaceBandRects`
(`Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx:92-163`). It does four
things in one pass:

1. Indexes the per-state percentile lists by lower-cased state name, keeping only non-empty lists.
2. Walks the workflow order and **carries the previous state's percentiles forward** into any state
   that has none of its own; a state to the left of every populated state gets nothing.
3. Sorts each state's percentiles by value ascending and turns them into a ladder of upper
   boundaries, with the axis maximum as the final boundary.
4. Projects each `(lower, upper]` slice through the chart's x and y scales into a `<rect>`, drops
   zero-height slices, and colours each slice by its position in the ladder — with the top slice
   forced to the reddest colour even when fewer than four percentiles came back.

Story #5884 needs the *same* judgement as a value on a row in the work item dialog: which zone would
the chart have painted under this item's dot. Its DISCUSS wave locked that requirement as an identity,
not a resemblance — same half-open `(lower, upper]` boundaries, same carry-forward, same
case-insensitive state matching — and made "zero reports of a dialog band contradicting the coloured
zone under the same dot" the feature's correctness KPI.

So the question this ADR settles is where that rule lives once two surfaces need it.

Two facts bound the answer. First, `computePaceBandRects` is already exported and is already covered
by sixteen direct unit tests and six DOM tests inside its own describe block in
`WorkItemAgingChart.test.tsx`, with two further DOM tests touching the overlay elsewhere in the file.
They cover the awkward cases — the floor band, the top band, carry-forward into an empty state, a
leading empty state with nothing to its left, coinciding percentiles that collapse a band to zero
height, input order independence, and fewer than four percentiles. Second, neither dialog can see the
chart's scales. The View Data dialog is rendered by `WidgetShell` from a payload built in
`BaseMetricsView`, outside the chart component altogether; the dot-click dialog is rendered by the
chart component but outside `<ChartsContainer>`, so it has the props and not the coordinate system.

## Decision

**Split `computePaceBandRects` into a pure ladder resolver and a geometry projector, both living in a
new pure module, and have the dialog's classifier read the same ladder.**

Three exported functions, no component imports, no React:

```
resolvePaceBandLadders({ perStatePercentileValues, doingStates })
    -> ladders keyed by workflow position, each carrying the state name and its
       value-ascending percentile list after carry-forward; states with nothing
       to carry are absent

classifyPaceBand(ageInDays, stateName, ladders)
    -> the rank of the first boundary the age does not exceed, or the top rank
       when it exceeds all of them; absent when the state has no ladder or is
       not in the workflow order

paceBandColorForRank(rank, boundaryCount)
    -> the fill the chart paints for that rank, top-rank clamp included
```

`computePaceBandRects` keeps its name, its signature and its exported status, and becomes the
projection of `resolvePaceBandLadders`' output through the scales. `PACE_BAND_COLORS_LOW_TO_HIGH`
moves into the same module and is re-exported from the chart so existing importers and tests are
unaffected.

Three consequences follow directly from the shape rather than from discipline:

- The half-open `(lower, upper]` boundary is not a rule the dialog reproduces. It is the arithmetic of
  `classifyPaceBand` selecting the first boundary that the age does not exceed, which is the same
  boundary that owns the rect the age would sit inside. An age equal to a percentile lands below it in
  both consumers because there is only one comparison.
- Carry-forward is not reproduced either. There is one resolver, and both consumers receive its output.
- The colour is not reproduced. The top-rank clamp — which paints the top band reddest even when the
  ladder is short, and which a hand-written copy is very likely to get wrong — is one function called
  by the chart's fill and by the dialog's cell.

**What this buys is bounded, and the bound is worth stating rather than glossing.** The geometry takes
two inputs the classifier does not have and should not have: the axis minimum and maximum, read from
the chart's y scale. So the invariant is not "the two can never disagree" in the abstract. It is: for a
monotone y scale and an age within `[axisMin, axisMax]`, the rank `classifyPaceBand` returns is the
rank named by the rect that contains that age. An age outside the domain cannot arise in practice —
`getMaxYAxisHeight` (`utils/charts/chartAxisUtils.ts:19-43`) takes the maximum of the percentile
values, the service level expectation and **the plotted ages**, then adds ten percent — but if one ever
did, the classifier's answer is the correct one and the chart's clipping is the artefact. A band is a
property of the item and the ladder. The viewport is not part of it.

**Rank is recovered from the rect's key, never from its position in the output array.** The geometry
drops zero-height rects, so the index is not the rank. Each rect already carries a key naming its
boundary, and that is what the agreement test reads.

**A collapsed rect cannot orphan a classification.** A rect collapses when two consecutive boundaries
are equal, and when they are, the classifier — which selects the *first* boundary an age does not
exceed — always returns the lower of the two, which is the rect that survives. The higher rank is
unreachable: an age would have to be at once no greater than a boundary and greater than the identical
one beneath it. Under the identity scales the agreement test uses, zero pixel height and equal values
are the same condition, so the invariant is total there. Under real scales two distinct boundaries can
round to the same pixel; the resulting band is thinner than a pixel and invisible, and the classifier's
rank is then the honest answer about a zone the user cannot see. This is the shipped tied-percentile
case, and it is settled here rather than left to whoever writes the test.

**The band's label is derived from the boundary's percentile number, not from a fixed five-entry
array.** Rank 0 reads `Below {p₀}th`, a middle rank reads `{pᵢ₋₁}th-{pᵢ}th`, the top rank reads
`Above {pₗₐₛₜ}th`, and an absent rank reads `No history`. On the shipped fixed 50/70/85/95 percentiles
this produces exactly the six strings the DISCUSS wave locked.

The honest reason for the derivation is not that ladder lengths vary today — they do not. Both metrics
services hard-code `[50, 70, 85, 95]`, and the terminal state's clone of the cycle-time percentiles
comes from a builder that is equally literal, so no production response can carry a ladder that is not
four long. The variability the geometry already tolerates lives only in its own test fixtures. The
derivation is written for the change that is already named and already deferred — configurable
percentiles — under which a positional five-label array would relabel a three-boundary ladder's top
band `70th-85th` when it means `Above 85th`, silently, while the derivation needs no edit at all.

The colour deliberately does not follow the label past five ranks. `paceBandColorForRank` saturates
every rank above the fourth to the same red, so a six-boundary ladder would render `85th-95th`,
`95th-99th` and `Above 99th` in three identically coloured cells. That is the existing chart behaviour
and it is kept: past five bands the palette stops carrying the distinction and the label carries it.

**The agreement between the two consumers is a test, not a promise.** A property test walks a fixture
table of percentile sets — carry-forward, leading empty state, tied percentiles, short ladders,
case-mismatched state names — and for each `(state, age)` inside the axis domain asserts that the rank
`classifyPaceBand` returns is the rank named by the key of the rect `computePaceBandRects` produces
containing that age, under identity scales. It holds even if a later change reimplements one side,
which is the only thing prose in an ADR cannot do.

## Alternatives Considered

**Option A — A sibling classifier in the dialog that restates the rule.**

- Pros: `computePaceBandRects` is not touched, so no shipped chart behaviour is at risk and
  ADR-020's rendering commitments cannot be disturbed by accident.
- Cons: two copies of a rule whose sole requirement is that they never disagree, with nothing in the
  type system or the compiler able to see both at once. The three things that would drift are exactly
  the three that are hard to get right — carry-forward, the low-side half-open boundary, and the
  top-rank colour clamp. Worse, the drift is silent in the direction that matters: someone tuning the
  chart's carry-forward has no reason to learn the dialog exists, and the contradiction appears to a
  user who clicked a dot and read a row that disagreed with it. And the agreement test would have to be
  written anyway, since a restated rule needs a cross-check more than a shared one does — so this
  option pays the same test cost while also paying the maintenance cost.
- **Rejected.** The regression risk it avoids is bounded and instrumented — twenty-two existing tests
  over the overlay fail loudly on a botched extraction, on the first `pnpm test` run. The drift risk it
  accepts is unbounded and silent.

**Option B — The chart computes a band for every in-flight item and hands the dialog a lookup.**

- Pros: exactly one caller of the rule; the dialog receives finished answers.
- Cons: only one of the two dialogs is rendered by the chart, and even that one sits outside
  `<ChartsContainer>` and so cannot read the scales. The View Data dialog is rendered by `WidgetShell`
  from a payload built in `BaseMetricsView`, outside the chart component entirely. Satisfying it would
  mean either lifting the computation out of the chart — which is this ADR's decision, reached by a
  longer route — or wiring a second path through a component that has been kept deliberately ignorant
  of percentiles.
- **Rejected** as either equivalent to the decision or worse than it.

**Option C — Extract only the resolver; leave the palette and the colour clamp in the chart and import
them from there.**

- Pros: strictly smaller. It avoids relocating `PACE_BAND_COLORS_LOW_TO_HIGH` and the re-export shim
  that exists only so twenty-two test imports keep working. The palette is a constant array, not a
  component, so importing it directly would give the dialog the same single source.
- Cons: it leaves the dialog importing a chart component for a value, which is the coupling the module
  boundary exists to forbid — a pure module the dialog depends on cannot later grow a `useTheme` or a
  hook, whereas a component can and eventually will. It also splits the rule in half: the ladder would
  be shared and the colour would not, so the top-rank clamp — the single most copy-prone piece — stays
  reachable only through a component.
- **Rejected**, but narrowly, and it is the cheapest fallback if the re-export shim turns out to cost
  more than expected.

**Option D — Move the whole overlay, geometry included, into the shared module.**

- Pros: nothing band-shaped left in the component.
- Cons: the geometry is not shared. It exists to serve one chart's coordinate system and reads scales
  that only exist inside `ChartsContainer`, which is ADR-020's central commitment. Relocating it would
  move code without removing a duplicate and would put a chart-coupled function in a module the dialog
  imports.
- **Rejected** as motion without benefit.

## Consequences

**Positive**

- The classification, the carry-forward, the boundary convention, the palette and the top-rank clamp
  each exist once. The dialog cannot disagree with the chart about any of them without a change that
  makes the chart disagree with itself.
- A future change to the band rule — configurable percentiles being the obvious candidate, deferred
  since `aging-pace-percentiles` — lands in one function and reaches both surfaces.
- The label survives a change in ladder length instead of silently mislabelling it.
- The extraction is guarded on entry: twenty-two existing tests over `computePaceBandRects` and the
  overlay DOM pin the behaviour that must not change.

**Negative**

- One shipped exported function is refactored to serve a feature that does not change its behaviour.
  The refactor commit is separate from the feature commits, per the repo's commit convention, so a
  bisect can tell the two apart.
- The dialog now imports a chart-flavoured utility module. It is pure and React-free, but the
  dependency is real and is the price of the palette and the colour clamp being shared rather than
  copied.

**Neutral**

- `computePaceBandRects` keeps its name and signature, so no test file and no importer is renamed.
- The module is frontend-only and adds no dependency, no endpoint, no persistence and no premium gate.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| For an age inside the axis domain, a dialog cell's band is the rank named by the key of the chart rect containing that age, for every state and age in a shared fixture table | Vitest property test running `classifyPaceBand` and `computePaceBandRects` over one fixture set under identity scales, reading rank off the rect key rather than its array index |
| A classification never lands on a rank the geometry collapsed | Vitest over a tied-percentile fixture asserting the returned rank is the surviving lower boundary, which is what the reader sees at that height |
| The band rule is not restated anywhere: the chart, the dialog and the descriptor factory all reach it through the shared module | The module is the only definition of the ladder type; the chart and the dialog take that type, never the raw per-state percentile list |
| The shared module stays pure and free of React and of component imports | Vitest importing it in isolation without a render; Biome import rules on the module's directory |
| The dialog's band colour is the chart's fill for the same rank, including the short-ladder top clamp | Vitest asserting the cell colour equals `paceBandColorForRank` for each rank, and that the chart's rect fill for the top rank matches it on a three-boundary ladder |
| `computePaceBandRects` renders exactly as today for every existing case | The sixteen existing unit tests and six overlay DOM tests in the pace-band block of `WorkItemAgingChart.test.tsx`, plus the two elsewhere in the file, all unmodified by the refactor commit |

## Cross-feature impact

- `aging-pace-percentiles` (ADR-019, ADR-020, ADR-053): the overlay's rendering, its position inside
  `ChartsContainer`, its prop-absent parity and its palette are all unchanged. ADR-020's decision is
  about where the overlay *renders*; this ADR is about where the rule it renders *lives*. Neither
  supersedes the other.
- ADR-018, ADR-021 and ADR-024 — the standing "no shared per-state aggregation service" chain — do
  **not** reach this decision. That chain governs backend C# aggregation over
  `WorkItemStateTransition` rows, and it rejected a shared service because its two would-be consumers
  have deliberately different item-membership semantics that a common signature would hide. Here the
  two consumers are required to be identical, and a disagreement between them is the defect the
  feature exists to avoid. Same principle — share only where the semantics are the same — opposite
  verdict, because the premise is inverted.
