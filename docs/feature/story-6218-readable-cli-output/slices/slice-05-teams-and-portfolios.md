# Slice 05 — Teams and Portfolios as the Overview's tables

**Story** #6218 / US-05 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~5h · **Sketch** `discuss/cli-sketches.md` §5

## Goal

`lh team|portfolio list` prints the Overview's table; `get` prints the page header and its
quick-settings lines.

## IN scope

- `{Teams}` / `{Portfolios}` tables: Name `[id: n]` · {Features} (`1 Feature` / `n Features`) · Tags ·
  Last Updated (`DataOverviewTable.tsx`).
- `portfolio list` hint to `lh delivery list --portfolio-id <id>`; no Delivery reads (D10).
- `team get`: `Name [id: n]`, `Last Updated on …`, {SLE}, System {WIP} Limit, {Feature} {WIP},
  {Throughput} dates `(rolling)`/`(fixed dates)` (chosen, C15), {Portfolios}, {Features}, Tags, Work
  Item Types; `Not set` as the web says it.
- `portfolio get`: the same header, {SLE}, System {WIP} Limit, `{Feature} {WIP}: n {Teams}`, {Teams},
  {Features}, Tags.

## OUT of scope

- The `Deliveries` column (D10). The `Actions` column.

## Learning hypothesis

**Disproves that the list answer carries what the Overview's columns show** (`remainingFeatures`,
`tags`, `lastUpdated`) — if the list endpoint's records are thinner than the web model assumes, the CLI
would need a read per row, which this slice refuses; the column then shows `—` and the gap is reported.

## Acceptance criteria

AC-05.1 … AC-05.6. Risk carriers: **AC-05.3** (no N+1 reads), **AC-05.4** (D5 fallbacks), **AC-05.6**
(production data against the Overview).

## Dependencies

Slice 01.

## Reference class

Slice 01's heading and table, applied to two untyped answers each.

## Pre-slice SPIKE

None.
