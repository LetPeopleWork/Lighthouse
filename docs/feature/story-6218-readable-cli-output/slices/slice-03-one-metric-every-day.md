# Slice 03 — One metric, every day, as a table

**Story** #6218 / US-03 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~6h · **Sketch** `discuss/cli-sketches.md` §3

## Goal

`--metrics <name>` prints that metric's sentence and its every-day table, for each of the ten metrics
other than Time in State.

## IN scope

- `throughput`, `arrivals` — sentence + Date · Work Items closed/started.
- `wip` — in-progress items (ID · Name · State · {Work Item Age} · {Blocked} since) + daily WIP.
- `cycleTime` — percentile sentence (named cycle time with `--definition-id`) + closed items.
- `workItemAge` — percentile sentence + per day oldest item and count.
- `totalWorkItemAge`, `blocked` — sentence + daily table.
- `predictabilityScore` — score, the web's explanation sentence verbatim, the four chances.
- `percentilesOverTime`, `processBehaviorOverTime` — per recorded day; empty → the web's sentence.
- Several names → each in the order given, complete.

## OUT of scope

- `cumulativeStateTime` (slice 04). Changing the 30-day percentile horizon.

## Learning hypothesis

**Disproves that one daily-table shape serves every over-time series.** If more than two metrics need
their own layout, the slice is not thin and the remainder is split out before DELIVER continues.

## Acceptance criteria

AC-03.1 … AC-03.6. Risk carriers: **AC-03.1** as one table-driven test over all ten names; **AC-03.6**
production data — the dev instance's recorded percentile days against the web widget.

## Dependencies

Slices 01, 02 (heading and range wording).

## Reference class

Slice 02.

## Pre-slice SPIKE

None.
