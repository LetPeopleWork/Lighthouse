# ADR-066: Aging-Chart CT↔WIA Reference Lines — Swap the Line *Source* Between Two Server-Fetched Percentile Arrays, Not a Second Line Set

## Status
Accepted (user-confirmed, 2026-06-09). Decision unchanged from the prior pass; **mechanism revised** for the ADR-065 server-side compute override (both percentile sets are now server-fetched, not CT-fetched + FE-derived).

## Context

Story #5257 US-02/US-03 add a switch on the Work Item Aging chart that flips its horizontal reference lines between **Cycle Time percentiles** (default, today's behaviour) and **Work Item Age percentiles** — **mutually exclusive**, one set at a time (DISCUSS D2). The independent pace-band overlay chip (`aging-pace-percentiles`, ADR-020) is orthogonal and untouched.

Today the chart renders CT reference lines from the `percentileValues` prop unconditionally inside `<ChartsContainer>` via `ChartsReferenceLine`, gated per-percentile by `useChartVisibility`'s `visiblePercentiles` map (`WorkItemAgingChart.tsx:566–581`). The horizontal axis, dots, SLE line, vertical grid, and pace-band overlay are all independent of which percentile *set* is shown.

**Mechanism change from the prior pass**: ADR-065 now computes the WIA percentiles **server-side** on a new endpoint (`workItemAgePercentiles`, Team + Portfolio) returning `PercentileValue[]`. The chart therefore swaps between **two server-fetched `IPercentileValue[]` arrays** — CT from `cycleTimePercentiles` and WIA from the new `workItemAgePercentiles` — **not** between a fetched CT array and an FE-derived one. The chart stays purely presentational either way; only the *origin* of the WIA array moves from "derived at `BaseMetricsView`" to "fetched into `MetricsData` ctx and passed down".

## Decision

**Swap the line *source*, not the line *renderer*.** The chart gains one optional `workItemAgePercentileValues?: IPercentileValue[]` prop (parallel to the existing `percentileValues`) and one local `percentileSource: "cycleTime" | "workItemAge"` state defaulting to `"cycleTime"`. A single derived `activePercentiles = percentileSource === "workItemAge" ? workItemAgePercentileValues : percentileValues` feeds **the existing single `ChartsReferenceLine` render block** and the existing `useChartVisibility({ percentiles: activePercentiles })`. There is exactly **one** set of reference lines on the canvas at any time, by construction — mutual exclusivity is structural, not a runtime invariant to police.

**Both arrays are server-fetched**: `percentileValues` (CT) comes from the existing `cycleTimePercentiles` read; `workItemAgePercentileValues` (WIA) comes from the new `workItemAgePercentiles` read (ADR-065), fetched in parallel into the `MetricsData` ctx by `useMetricsData` and passed down through `BaseMetricsView` as a prop. `WorkItemAgingChart` computes nothing — it receives both arrays and a source toggle.

The toggle control is a small switch (label: CT ↔ WIA, with distinct axis/legend labels so the two populations are never conflated — cross-story invariant) rendered alongside the existing legend/visibility controls. Flipping it swaps `percentileSource` only — **no network call** (both arrays already loaded), no remount, no effect on dots, SLE, vertical grid, or the pace-band chip.

## Alternatives Considered

### Alternative — Render two reference-line sets and toggle visibility per set
Keep both CT and WIA `ChartsReferenceLine` blocks mounted; show/hide each via a boolean.
- **Rejected because**: mutual exclusivity (D2) would become a runtime invariant that two booleans must never both be true / both false — a state-machine bug waiting to happen, and a visual-overlap risk if it breaks. Swapping a single source makes the invariant impossible to violate and keeps a single visibility map (`useChartVisibility` needs no change to its single-`percentiles` contract).

### Alternative — A new dedicated WIA chart / widget
- **Rejected because**: D2 explicitly wants the *same dots* benchmarked against either population on the *one* aging chart ("see which dots have drifted past the bulk of the current work" — journey). A second chart loses the shared dot context and duplicates the entire scatter render path. The whole point is to contrast on one canvas.

### Alternative — Extend `useChartVisibility` with a source dimension
Push the CT/WIA selection into the visibility hook.
- **Rejected because**: source-selection and per-percentile visibility are orthogonal concerns; conflating them bloats a shared hook used elsewhere. A single local `percentileSource` state at the chart keeps the blast radius to this one chart, mirroring how `showPaceBands` is held locally.

### Alternative — Re-fetch the WIA array only when the toggle is flipped to WIA (lazy)
- **Rejected because**: it would add a per-flip network round-trip and a loading state on a control meant to feel instant (KPI-2 <200 ms). Fetching the WIA array in parallel with the other metrics reads (one extra cached request per scope load) keeps the toggle a pure client-side swap. The WIA endpoint is cheap (snapshot + `BuildPercentiles`).

## Consequences

- **Positive**: one render path, one visibility map, mutual exclusivity by construction; `useChartVisibility` unchanged. `WorkItemAgingChart` stays presentational — it receives both percentile arrays as props and a source toggle, computes nothing (the server now produces both arrays). Empty-WIP (D6): `workItemAgePercentileValues` is the 50/70/85/95 `0`-valued set (or the chart treats it as no-lines), so flipping to WIA renders no meaningful lines and never crashes — same graceful path the chart already has for empty/zero `percentileValues`.
- **Positive**: backwards-compatible — `workItemAgePercentileValues` is optional; absent ⇒ the toggle has nothing to swap to (or is hidden), and the chart renders byte-identical to today. Guarded by a snapshot/behavioural test.
- **Negative**: one extra parallel fetch per scope load (the WIA array) plus one new local state + one new optional prop on an already-large component. Acceptable — the fetch is cached and cheap; the state mirrors the existing `showPaceBands` local-state idiom.
- **Negative**: the toggle's default-CT round-trip must restore prior per-percentile visibility cleanly; covered by a Vitest round-trip test (US-02 AC2).
