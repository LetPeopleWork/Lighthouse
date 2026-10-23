# Slice 05: Forecast rounds finish sooner (US-05)

**Goal:** a forecast round reads only what the simulation and Story 6354's fingerprint use, so a skipped round costs
at most 15% of the baseline computed round and a computed one at least 30% less, with every output identical.

**Job:** `job-operator-keep-lighthouse-usable-while-it-forecasts`. **Moves:** **K4**; K3 in part.

## IN scope

- The gather stops loading what it does not use (forecast histograms of unrelated Features, every Portfolio via
  `GetById`); new persistence-adapter reader methods only (D28).
- The save writes what changed; how histograms are replaced is DESIGN's.
- Identity checks: percentiles, histogram rows and the input fingerprint, same data and seed, SQLite and PostgreSQL.

## OUT of scope

- Forecasting less often, coalescing rounds (follow-up Epic).
- Making Forecast its own aggregate or removing the Feature → Forecast navigation (Epic 6264's S13).
- Any change to what a round computes (C12).

## Learning hypothesis

- **Disproves** "the gather dominates a skipped round" (A14) if a skipped round still takes over 15% of the
  baseline computed round.
- **Confirms** it if both K4 figures hold and the round split shows the gather shrank.

## Acceptance criteria

US-05 AC-5.1 to AC-5.5 in `feature-delta.md`.

## Production data (D25)

Dev DB backup: the same rounds skip and compute as before (fingerprint unchanged), and every Feature's forecast is
identical.

## Looks and sketch

- Loading / failure look: **N/A, because** no screen's fetch changes; Refresh History reads durations it already
  shows.
- UI sketch at DISTILL: **N/A, because** nothing a user sees changes.

## Dependencies

Task 01a (round split baseline). Swaps freely with 02 and 03. Best after slice 01, so the gain is read off the request
pool.

## Effort

About 1.5 days. Reference class: Story 6354's narrowed Feature read and its guard test on the fingerprint members.

## Dogfood

Scaled instance: let two hourly rounds run, one with nothing changed and one after closing Work Items on Team Atlas;
read both in Settings → System Info → Refresh History against the baseline.
