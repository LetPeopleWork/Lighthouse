# Slice 03: An already-upgraded volume moves on to the next major the same way

**Goal:** a volume this chart has already upgraded once (`pgdata/` kept, `pgdata-N/` live) is carried to
the next major by the next chart release with a plain `helm upgrade`. Once the new copy is in place, the
copy before last is removed, so the volume keeps about twice the data and one step of rollback. Proved on
kind with a 16 → 17 → 18 chain, because no image of Postgres 19 exists yet.

## IN scope
- The upgrade takes its source from the copy the database last ran on, which may be `pgdata-N/`, and
  builds `pgdata-(N+1)/` beside it (AC-4.1).
- After the new copy is committed, every copy older than the source is removed. The removal leaves the
  placeholder in `pgdata/`, logs one line, and an interruption is finished on the next start (AC-4.2,
  AC-4.6). The first upgrade removes nothing (AC-4.11).
- Rollbacks:
  - one chart back starts on `pgdata-N/` with the warning (AC-4.3);
  - two charts back fails loudly or is refused, and never starts an empty or out-of-date database (AC-4.3);
  - re-upgrade after a rollback redoes the copy from `pgdata-N/` (AC-4.4);
  - pin-back to N starts on `pgdata-N/`, to N−1 is refused (AC-4.5).
- The cleanup command shares the removal: remove every copy older than the live one, in the same order.
  A cleaned-up volume moves on (AC-4.6, AC-4.7).
- Refusals measured against the live copy, including out-of-date newer copies (AC-4.8). No re-upgrade on
  restart (AC-4.9).
- Docs: rollback, cleanup, cost and one major per chart release (AC-4.10).
- ADR-213's known-limitation section, marked resolved once this slice is delivered.

## OUT of scope
- Upgrading two or more majors in one start. Each chart release still moves exactly one major.
- Removing the source copy (the one-chart-back rollback) automatically, and growing the volume.
- Compose and binary installs; external databases.

## Learning hypothesis
- **Disproves** "the upgrade step can take its source from a copy it made itself, drop the copy before
  last safely, and rollback, cleanup and refusals still hold" if any of these happens → the chart must not
  move its default past 18 until the volume model is rethought:
  - the 17 → 18 step of the chain loses rows;
  - a rollback opens the wrong copy or an empty one;
  - an interrupted removal leaves a volume the next start cannot serve;
  - a refusal changes the volume.
- **Confirms** it if the chain round-trips with matching row counts: upgrade → removal → rollback →
  re-upgrade → cleanup → restart.

## Acceptance criteria
AC-4.1 … AC-4.11 in `../feature-delta.md`.

## Dependencies
Slices 01 and 02, as delivered (HEAD `93b7efe7c`). `postgres:16-trixie` for the chain, and
`postgres:15-bookworm` with `postgres:16-bookworm` for the gap-from-a-copy refusal (`postgres:15-trixie`
does not exist; checked 2026-10-02).

## Effort
About 1–1½ days. Reference class: slice 02. The decision logic is rewritten around one new idea (the live
copy), one removal function is shared with the cleanup, and the harness gains one chain fixture.

## Dogfood
On a local kind cluster:
1. Install chart 0.1.17 with `postgresql.image=postgres:16-trixie` and load the demo data.
2. Upgrade to the chart with `postgresql.image=postgres:17-trixie` and `postgresql.upgrade.image=postgres:16-trixie`,
   and write a row.
3. Upgrade to the chart's defaults (18), and read the removal line.
4. Check teams and portfolios in the UI.
5. Roll back one revision, then upgrade again.
6. Roll back to 0.1.17 and watch it fail loudly, then upgrade again.
7. Run the cleanup command copied from the docs, and restart.
