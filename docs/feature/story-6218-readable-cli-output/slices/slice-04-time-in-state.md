# Slice 04 — Time in State as a table, with its drill-down

**Story** #6218 / US-04 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~3h · **Sketch** `discuss/cli-sketches.md` §4

## Goal

`--metrics cumulativeStateTime` prints one row per state in workflow order, and `--state <name>` adds
the web's drill-down list.

## IN scope

- `Time in State across n {Work Items}` (chosen wording) over State · Total days · {Work Items} ·
  Completed · Ongoing · Mean · Median, in `workflowOrder`; `—` for a null median.
- `--state` → `{Work Items} contributing to {state}` with ID · Name · Type · State · Days Contributed
  (the web dialog's words).
- `--item-ids` → the heading counts the subset.
- The candidates list not printed under `--pretty`.
- The metrics headline (slice 02) gains its Time in State block.

## OUT of scope

- Any change to the item-picker semantics.

## Learning hypothesis

**Disproves that the state rows on the wire are enough to reproduce the web chart's order and
figures** — if `workflowOrder` or the per-state counts are missing or disagree with the chart's
tooltips on the dev instance.

## Acceptance criteria

AC-04.1 … AC-04.5. Risk carrier: **AC-04.5**, production data against the web chart.

## Dependencies

Slices 01, 02.

## Reference class

The only fully typed answer of the metrics group (`CumulativeStateTime*` types) — the cheapest renderer.

## Pre-slice SPIKE

None.
