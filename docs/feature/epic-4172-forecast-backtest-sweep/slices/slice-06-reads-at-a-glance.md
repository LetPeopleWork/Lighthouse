# Slice 06 — The reality check reads at a glance (split: 06a, 06b)

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: Story #6094 (child of Epic #4172) · **Story**: US-06
**Estimate**: ~12.5h in total (06a ~6h, 06b ~6.5h) · **Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Depends on**: slices 04 + 05 (delivered locally, up to `655ab5a52`) · **Decisions**: `feature-delta.md`,
*US-06 amendment (2026-09-27)*, 6094-D11..D20 (locked), 6094-D21..D25 (DISCUSS defaults)

**Reference class**: the shipped reality-check dialog of slices 04 + 05. Nothing about what is checked or
how it is graded changes; only what the dialog shows first, and how much each cell says out loud.

## Goal

Maria Santos opens the reality check and can tell, before scrolling, whether her sampling window is fine
and how each confidence level held against the rate it should hold at; every forecast in the table reads
as "✓ 17" on its grade colour, with how far off it was one hover or one keyboard focus away.

## Why split

One story, twelve scenarios, over a day: past the 3-7 band as one slice. Split by what the user sees, not
by layer. **06a** makes the table and the trigger compact and is low-risk; **06b** replaces the prose with
the summary and carries most of the new copy and the most uncertainty (the bar, the badge's states, the
tooltip holding a link). Each ships on its own: after 06a the old prose still sits above a compact table;
after 06b the prose is gone.

## 06a — the trigger, the loading state, the log, the compact table (~6h)

**IN**
- Trigger below the Backtesting inputs, right-aligned, info icon with the explanation (6094-D11).
- Spinner + "Crunching the numbers…" on first run and "Run again" (6094-D12).
- Backend: one Information log entry per check, Team id + filter override (6094-D13). No contract change.
- Period header: *"Forecast Horizon: 1 week (21.09.2026 – 27.09.2026) – 14 {Work Items} completed"*,
  centred, numeric local dates (6094-D16).
- Cells: glyph + forecast only; tooltip on hover and focus = accessible name, more / fewer / exact /
  actual-0 wordings; "—" for a level left out of a check (6094-D17, D24).
- Visible caption removed; scroll region keeps an accessible name (6094-D18).
- Legend: two titled rows; credit line removed from under it (6094-D19). *(Until 06b lands, the credit
  is absent from view — acceptable only if 06b follows in the same push; otherwise keep the credit line
  until 06b. DELIVER's call at push time.)*

**ACs**: AC-6.1 .. AC-6.7, plus AC-6.13, AC-6.14, AC-6.16. **Scenarios**: A-1 .. A-6.

## 06b — the summary (~6.5h)

**IN**
- Headline "Backtested {N} scenarios · {M} forecasts", its "N of T" and N = 0 forms (6094-D14.1, D20, D21).
- Headline info tooltip: denominator explanation, could-not-run reasons, tick and *accurate* explained,
  Brown credited beside our additions, link to *The Full Monte*; hover, focus, click; Escape closes it
  first (6094-D15, D25).
- Window badge, one wording per verdict state, tone, tooltip with the set that held up (6094-D14.2, D22).
- One row per level: label, bar to held share, tick at the percentile, "75% (12 of 16) · 4 accurate",
  text alternative; "Not tested — no check could run" (6094-D14.3, D23).
- Delete the removed prose and every composer that loses its last caller, with their tests (6094-D14.4).
- Walking skeleton: replace `realityCheckLevelLine` / `realityCheckDialogDenominator` with summary
  locators in `TeamDetailPage`; run locally before commit.

**ACs**: AC-6.8 .. AC-6.12, plus AC-6.13 .. AC-6.16. **Scenarios**: B-1 .. B-6.

## OUT of scope (both)

- Grading rule, bands, rounding, the six fills, sweep, ladder, horizons, rule A, DES-16 thresholds, any
  response field.
- Any per-window score, order or highlight, in the badge or anywhere (I-a); any write (I-b).
- The docs-page link in the tooltip (FINALIZE); a new usage event; CLI / MCP; the one-pager (slice 03).

## Learning hypotheses

**06a disproves, if it fails**: that "✓ 17" on a grade colour, with the rest on hover / focus, is enough to
scan a period for trouble. Signal: in dogfooding, the maintainer still has to open tooltips to find which
cells missed badly.

**06b disproves, if it fails**: that a bar against its tick reads as "held too often / too rarely" without
the "should be about" words, and that the badge answers the window question without the region sentence.
Signal: the maintainer cannot state each level's finding and the window standing from the summary alone
in under 15 s on three real Teams (KPI *Reads at a glance*); record the result here.

**Confirms, if both succeed**: the words above the table fall from ~210 to ≤ 50, and the dialog is
screenshot-ready for the docs page and launch post.

## Production / demo data

Dogfood on three real Teams of the dev instance (write the finding here). E2E on demo scenario 0's first
Team. Worked examples: Ocean Explorer 8 weeks, actual 42, 30-day row ✗ 48 / ✓ 40 / ✓ 36 / ✓ 31, the 95th
tooltip "+26%"; Coastal Survey 1 week, 3 vs 4 → "−33%"; Harbour Pilots 0 vs 0 and 0 vs 2; Kelp Farm N = 0.

## Dependencies

- The maintainer's review of the *Copy to confirm* table in `feature-delta.md` — 06a needs rows 1, 2,
  16-19b, 21-24; 06b needs the rest. Neither blocks DESIGN.
- DESIGN: log placement, cell keyboard model, interactive tooltip vs popover, the bar primitive, the
  numeric date formatter (*Open for DESIGN — US-06*).
- No DEVOPS change (N/A, stated in the checklist).
