# DISTILL — slice 02, metrics headline (US-02)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/metricsHeadlineView.test.ts` | 12 | 3 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 02 | 2 | — |

Fixtures: `test-support/metricsAnswers.ts` (`gravitysMetrics`, `oceanExplorersMetrics`, keyed by client
read). Every run passes explicit `--start-date` / `--end-date`: the default range is end − 30 days, which
is 31 inclusive days, so the sketch's "(30 days)" only holds with explicit dates.

## Scenarios

- Team headline: heading, six headline lines in order, the percentile table, at most 30 lines (KPI-3;
  today's generic view prints about 1,324 lines for the same facts).
- Over-time lines; an empty series reads as a sentence; a refused predictability read is reported in
  place.
- Error/edge: one section in an unknown shape → `<Metric>  shown only with --json (unknown shape)` and the
  rest renders (M2); no WIP limit; a server that flags no Work Item as blocked; `1 day` and `—`; no work
  distribution; terms renamed; Team name and terms refused.
- Portfolio headline: `Ocean Explorer · Thu 9 Jul 2026 – Tue 6 Oct 2026 (90 days)`, Features in Progress
  against the System WIP Limit, total throughput per day.
- Guards: `--json` and `--toon` SHA-256 of the composite, captured on unchanged code; the reads are
  exactly the 15 metric reads; `--metrics percentilesOverTime --json` hash.

## DELIVER order

Un-skip top to bottom, then the slice-02 rows of `prettyForms.test.ts`. A changed hash in a guard means
the composite moved: fix the code, never re-capture.

## Not pinned (open)

The headline's Time in State line once slice 04 lands.
