# Slice 01: Upgrade 17 → 18 inside the chart, keeping the old copy

**Goal:** `helm upgrade` from chart 0.1.17 to the new chart brings a bundled Postgres 17 database across
to 18 with every row intact, and `helm rollback` opens the untouched 17 copy again.

## IN scope
- Detect the data's major on the PVC; no-op when it matches the image or the volume is empty.
- Carry 17 data to 18 with the 17 copy left unchanged (D2); upgrade once, not on every start (AC-1.5).
- On-volume layout that the 0.1.17 chart's fixed PGDATA still finds on rollback (AC-2.1); a re-upgrade
  after rollback starts from the 17 data, never a stale 18 copy (AC-2.2).
- Works applied via `helm upgrade` and via rendered manifests (D7, AC-1.7).
- Kind acceptance scenarios for AC-1.1..1.7, AC-2.1, AC-2.2; helm-unittest for the render shape and the
  standalone gate (AC-1.6).

## OUT of scope
- Refusal paths (space, major gap, newer data) — slice 02.
- Docs rewrite and chart version note — slice 02.
- Compose, external databases, automatic cleanup of the old copy.

## Learning hypothesis
- **Disproves** "an in-chart upgrade can carry a real Lighthouse database 17 → 18 and keep rollback
  working" if the restored real backup loses rows, or rollback cannot open the 17 copy → stop, keep D5's
  pin to 17 and revisit the approach.
- **Confirms** it if the real-backup dogfood round-trips upgrade → rollback → upgrade with matching row
  counts.

## Acceptance criteria
AC-1.1 … AC-1.8, AC-2.1, AC-2.2 in `../feature-delta.md`. AC-1.8 is the `NOTES.txt` line for an operator
whose `--reuse-values` upgrade kept the old image.

## Dependencies
Chart 0.1.17 pullable from the published repo (the "before" state). DESIGN picks the mechanism.

## Effort
About 1 day. Reference class: epic-5306 slice-01 walking skeleton (kind install scenario) plus the
encryption-key chart guard (0.1.13).

## Dogfood
kind cluster: install 0.1.17, restore a real Lighthouse backup into it, upgrade, check teams/portfolios/
forecasts in the UI, roll back, check again, upgrade again.
