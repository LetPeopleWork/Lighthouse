# Options (raw) — Story #6094

**Wave**: DIVERGE, light pass · Phase 3 · 2026-09-26. Generation only; nothing here is scored.
Competitive grounding reused from `../competitive-research.md` (no new research dispatched).

## HMW

> How might we let a forecaster see how often, and by how much, each confidence level's number has matched what their Team delivered, so they can say how far to trust the number they quote?

## SCAMPER + Crazy 8s (generated)

| Lens | Option | Kept? |
|---|---|---|
| A — Adapt | **The maintainer's direction** (Brown's table in a dialog) | curated #1 |
| E — Eliminate | Graded grid with no numbers; a cell opens its detail | curated #2 |
| C — Combine | Sentence stays inline on the Forecast tab; the table lives in a dialog | curated #3 |
| S — Substitute | Stated-versus-observed chart per confidence level | curated #4 |
| R — Reverse | Text leads, table follows as its evidence | curated #5 |
| M — Modify | Grade against what the level promised; misses stated in Work Items | curated #6 |
| P — Put to other use | The graded table as the prospect-facing one-pager | merged: slice 03 already owns the export; same content as #1 |
| Crazy 8s | Four period cards, one per horizon, actual in each card's header | merged into #1 as a layout of the same table (same mechanism, assumption, cost) |
| Crazy 8s | "Quote card": the tool names the level to quote | removed: names a winning level; the maintainer declined a per-Team confidence default and D4 says the human chooses |
| Crazy 8s | Sparkline per level of signed error across the 4 periods | merged into #4 (same aggregate-by-level mechanism) |

## Curated 6

### Option 1: The Full Monte table in a dialog (maintainer's direction, verbatim)

**Core idea**: The reality check opens in a popup dialog instead of sitting inline in the Forecast Backtesting group. A results table modelled on Nick Brown's "The Full Monte": for the Team, every period under test (each horizon [7,14,28,56] ending today and reaching back by its length), the forecast at each confidence level (50/70/85/95) for each history window, and the actual delivered in that period. One Team only. Each forecast cell graded on Nick's scale: correct / incorrect × within <10%, 10-25%, >25% of the actual; coloured with the product's forecast colours. A neat result in text summarising what the grading says.
**Key mechanism**: one dense graded table; colour carries correctness and margin; a text summary.
**Key assumption**: the forecaster reads a 64-80 cell table and wants to see every number.
**SCAMPER origin**: Adapt (Brown's published format).
**Closest competitor**: Brown's Full Monte article; FlowViz "MCS Backtesting" hidden page.

### Option 2: Graded grid, numbers on demand

**Core idea**: The dialog shows only the grid of graded cells (colour plus a glyph, no numbers). Selecting a cell shows its forecast, the actual, the dates and the margin.
**Key mechanism**: pattern first, number by interaction.
**Key assumption**: the pattern of grades answers the question; the numbers are needed only to check one cell.
**SCAMPER origin**: Eliminate (remove the numbers from the default view).
**Closest competitor**: parameter-sweep plots in hyperparameter tooling (`../competitive-research.md` §3.3); no agile tool found.

### Option 3: Sentence inline, table in a dialog

**Core idea**: The shipped sentence stays on the Forecast tab after "Run reality check"; the band panels go; an "Open the full results" control opens the graded table in a dialog.
**Key mechanism**: two surfaces — a short answer where the button is, the evidence one click away.
**Key assumption**: most forecasters stop at the sentence; the table serves the sceptic.
**SCAMPER origin**: Combine (shipped verdict + new table).
**Closest competitor**: the shipped reality check itself (sentence plus evidence toggle, slices 01+02).

### Option 4: Stated versus observed, per confidence level

**Core idea**: The dialog shows one small chart: for each confidence level, the share of checks it held in against the share it promised (50/70/85/95), with the typical margin beside each point; the per-period table sits below.
**Key mechanism**: aggregate across checks by level; a reliability-diagram reading.
**Key assumption**: the forecaster's question is per level ("can I quote the 85?"), not per check.
**SCAMPER origin**: Substitute (a calibration chart for the table as the primary view).
**Closest competitor**: weather-verification reliability diagrams; Prophet `coverage` (`../competitive-research.md` §3.1).

### Option 5: The answer in words, the table as its evidence

**Core idea**: The dialog opens on three or four sentences, one per confidence level: how often it held and how close it usually was ("Your 85% forecast held in 14 of 16 checks, within 10% in 9 of them"). The graded table follows underneath as the evidence for each sentence.
**Key mechanism**: server facts rendered as per-level prose; table as supporting detail.
**Key assumption**: the forecaster wants a sentence to repeat to a stakeholder, then the proof.
**SCAMPER origin**: Reverse (the maintainer's "result in text" leads; the table follows).
**Closest competitor**: ProKanban case-study write-ups (prose over a table).

### Option 6: Grade against the promise, misses in Work Items (premise challenge)

**Core idea**: The same table, but each cell's grade reads against what its level promised and states the miss in Work Items; a percentage appears only when the actual is large enough to carry one. A 50% forecast that did not hold is shown as an expected miss, not an error; a 95% forecast that held by more than 25% is shown as cautious, not correct.
**Key mechanism**: change the grading rule, not the layout.
**Key assumption**: percent-of-actual on small periods and "incorrect" on the 50% level both mislead.
**SCAMPER origin**: Modify.
**Closest competitor**: none found; nearest is Brown's own caveat that he declined the Brier score for accessibility.

## Diversity test

| Pair-check | Mechanism | Assumption | Cost profile |
|---|---|---|---|
| 1 dense table | all numbers visible | reader wants every number | one dialog + grading rule |
| 2 grid + drill | interaction reveals numbers | pattern is the answer | + selection state, detail pane |
| 3 two surfaces | inline sentence + dialog | most stop at the sentence | smallest change to shipped UI |
| 4 per-level chart | aggregation by level | question is per level | + chart, + aggregate rule |
| 5 prose-led | per-level sentences lead | reader wants a quotable line | + per-level copy, same table |
| 6 grading rule | different grade semantics | Brown's scale misleads here | rule + copy, layout-neutral |

All six differ on at least two of three. Narrowest pair: **1 vs 5** (same content, different lead); they differ in assumption and in the copy cost, noted rather than hidden.
