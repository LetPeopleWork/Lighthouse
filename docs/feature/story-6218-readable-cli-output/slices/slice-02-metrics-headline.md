# Slice 02 — The metrics headline fits one screen

**Story** #6218 / US-02 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~6h · **Sketch** `discuss/cli-sketches.md` §2

## Goal

`lh metrics team|portfolio --id n` without `--metrics` prints the dashboard's headline numbers, a
percentile table and one line per over-time metric — the whole answer on one screen.

## IN scope

- Heading `{name} · {start} – {end} ({n} days)`.
- Headline lines in the dashboard's words with D6 applied: `{Work Items|Features} in Progress` (with
  `System {WIP} Limit: n …` when set), `Total {Throughput}` and `Total Arrivals` with `n / day`,
  `{Blocked} {Work Items}` (count of in-progress items flagged blocked; omitted on servers that send no
  flag), `Total {Work Item Age}` (latest day), `Predictability Score` (one decimal).
- Percentile · {Cycle Time} · {Work Item Age} table, highest first.
- One line per over-time metric: first → last recorded day and the count recorded (D7); the web's empty
  sentence when none.
- A refused section prints its refusal in place; the rest renders (today's per-section behaviour).
- `workDistribution` not printed (D8).
- `--pretty`-only read of the Team/Portfolio (name, WIP limit).

## OUT of scope

- Per-day tables (slice 03). Time in State (slice 04; the headline names it only after 04 ships).

## Learning hypothesis

**Disproves that the dashboard's headline fits ≤ 30 lines of a terminal** if, on demo data, the
headline plus the over-time lines cannot be held there. Then the default view must drop a metric —
a product call to bring back to the maintainer, not to make in DELIVER.

## Acceptance criteria

AC-02.1 … AC-02.7. Risk carriers: **AC-02.5** (≤ 30 lines, KPI-3 — baseline counted at slice start),
**AC-02.4** (a refused section does not hide the others), **AC-02.7** (production data on the dev
instance against the web dashboard).

## Dependencies

Slice 01 (resolver, table helper, dates).

## Reference class

Slice 01 — same seam; the payload is assembled already by `buildMetricsPayload`, so the work is the
renderer only.

## Pre-slice SPIKE

None.
