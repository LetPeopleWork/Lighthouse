# Story 6655 — Outlier switch and outliers from the upper natural process limit

**Finished:** 2026-11-17 · ADO Story #6655 · nwdes feature `docs/feature/story-6655-outlier-toggle-and-unpl/`

## The problem

The Cycle Time Scatterplot set apart every closed Work Item above five times the median (ADR-282). A team saw more
than 20 outliers there while its Cycle Time Process Behaviour Chart flagged far fewer, so the two charts told the
reader different stories. There was also no way to see every item at once.

## What changed

Three slices, built in order.

- **S1 — Hide outliers switch.** A "Hide outliers" switch next to the Cycle Time selector, on whenever the chart
  opens and never stored. Off, the chart draws every closed Work Item, the axis grows to fit them and the chip goes;
  on again restores both. Switching it off sends the name-only usage event `OutlierHandlingSwitchedOff` (28).
- **S2 — outliers from the upper natural process limit.** An outlier is a closed Work Item above the upper natural
  process limit of the Cycle Time Process Behaviour Chart: the same baseline, formula and comparison as the chart,
  also for a named Cycle Time. The backend judges it through a replaceable `ICycleTimeOutlierDefinition` and serves
  the verdict on `GET .../metrics/cycleTime/outliers` for Teams and Portfolios. The scatterplot asks with a query
  keyed on owner, range and Cycle Time, sets nothing apart until the answer lands, and shows a loading chip and the
  could-not-load look. The list title and help name the limit in the reader's words.
- **S3 — no limits, no outliers.** Without usable limits (an invalid or too short baseline, fewer than two items, an
  unknown named Cycle Time) nothing is set apart and the header says so. No fallback to the median rule.

ADR-287 records the decision and supersedes decisions 1 to 3 of ADR-282.

## Key commits

| Commit | What |
|---|---|
| `f1f081c41`, `1e115e379`, `12dcc50b3` | S1: oracle, switch and usage event, extra test dropped after review |
| `d333cca5d`, `67108bdc8`, `3c8e923d7` | S2: oracle (with the adapted scatterplot tests), oracle fixes, the rule and route |
| `3d493c719`, `a67ac38b2` | S3: oracle, the no-limits text |
| `4920a869e` | Final refactor: one named default rule, no cast in the verdict query, ADR-282 marked superseded |
| `1b9b3d3ea`, `05ed932b6` | Mutation survivors killed (frontend, backend) |
| `c77308a73` | Docs: the Outliers section of the Cycle Time Scatterplot page |

## Gates

- RED observed by the host before each slice's craft (S3's backend rows were pins, since S2 built the no-limits
  answer early); GREEN observed by the host after each slice.
- Slice reviews: S1 accepted after one revision (a test outside the approved oracle removed), S2 and S3 accepted.
  The oracles of S1 and S2 were re-approved after later slices changed them.
- Whole-candidate review: accepted with four non-blocking findings (see "Left open").
- EXAMINE: E1 to E6 met.
- Mutation: backend 91.28 % first run, 92.31 % after the kill round; frontend 78.51 % first run, 90.91 % after the
  kill round (`mutation/results.md`).

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done** — `docs/metrics/flow-metrics.md`, Outliers section: the new rule and its baseline, the Hide outliers switch, the no-limits text, the loading and could-not-load looks (`c77308a73`). `docs/settings/usagedata.md` lists the new event (S1). |
| Screenshots | **Done** — `cycleScatter.png` (from the team metrics widgets test) and `cycleScatterNamedCycleTime.png` (named Cycle Times test) regenerated with `--grep` on their one test each; both show the Hide outliers switch. The outlier E2E walking skeleton (`OutlierChip.spec.ts`) passes unchanged: demo Team Zenith still shows one outlier, TZ-301, under the new rule. |
| Demo data | **N/A**, because the demo Teams already have closed Work Items and baselines; the outliers follow from them. The E2E walking skeleton runs on demo data. |
| Website assets | **Done** — the two regenerated scatterplot images under `docs/assets/features/metrics/`; the website hot-links them from `main`, so it shows them once this is pushed. |
| Clients (CLI, MCP) | **N/A**, because the clients do not read outliers; the new route is additive. |
| Context map, `ARCHITECTURE.md` | **Done** — ADR-270's Flow metrics glossary: Outlier rewritten, Upper natural process limit added (S2). `ARCHITECTURE.md`: the outlier rule bullet rewritten and ADR rows 286 and 287 added. |
| Usage-data event | **Done** — `OutlierHandlingSwitchedOff` (28), emitted when the switch is turned off, listed in `docs/settings/usagedata.md`. |
| Mutation | **Done** — see Gates. |
| Release notes | N/A; written at /release. |

## Left open, knowingly

- The metrics services take the outlier definition as an optional constructor parameter that falls back to the
  named default, because 15 test call sites build the services directly; DI injects the registered default.
- The scatterplot keys its verdict query on a module-level fetcher id, because the oracle re-renders with a new
  fetcher and the same owner and range; a per-component id would flash the loading look on remount.
- `BaseMetricsView`'s memoised outlier fetcher has 6 surviving mutants: its test file cannot render the
  scatterplot without the providers the loading suite builds.
- `XmRCalculator`'s older sigma-rule code (unchanged by this Story) has surviving mutants at exact sigma hits; worth a
  Continuous Improvement item.
- The list title falls back to a 0-day limit while no limit is known (`analysis.cut ?? 0`); it is never shown then.
- `docs/product/architecture/ADR-002.md` duplicates ADR-287 because nwdes resolves the feature's ADR by that path.
