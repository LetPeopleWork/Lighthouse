# ADR-261: A forecast round reads without histograms, and loads them only when it must

**Status**: Proposed (DESIGN, 2026-10-23, PROPOSE mode; revised after the architecture review).
**Feature**: `epic-6400-fast-with-many-features` (Story #6409, slice 05)
**Relies on**: [ADR-256](./adr-256-an-unchanged-portfolio-forecast-is-confirmed-not-recomputed-unless-a-person-asked.md)
(the fingerprint and the skip), [ADR-257](./adr-257-a-forecast-round-runs-on-its-own-threads-off-the-request-thread-pool.md)
(the round runs on the lane), [ADR-258](./adr-258-reads-that-show-no-forecast-dates-never-load-forecast-histograms.md)
(histogram-free reads), the configured cascade from a forecast to its histogram rows.

## Context

A round reads its Portfolio through `GetById` (every Feature with forecasts and histograms), then gathers every Feature
of every involved Team with completion and start forecasts **and their histograms**, tracked. Story 6354's skip
decides after this gather, so a skipped round pays nearly all of a computed round's reading. A computed round replaces
every forecast: because the old histogram rows are tracked, Entity Framework deletes each of them row by row before it
inserts the new ones, inside one transaction. On SQLite, a long write transaction makes any other writer wait up to the
10 s `busy_timeout` and then fail.

What the simulation and the fingerprint read: Features in forecast order, their Teams and remaining work, throughput
per Team, `FeatureWIP`, and `ForecastWaits` (which reads `CanBeForecast`: forecast headers, not histograms).

What the write-back reads: the forecast write-back stage resolves against `portfolio.Features` of the Portfolio
`GetById` returned (`WriteBackTriggerService`), and silently drops a Feature whose forecast percentiles are missing.
So once the round's own Portfolio read stops loading histograms, the write-back needs a read of its own.

## Decision

1. **The gather reads forecast headers, not histograms.** Features of the involved Teams load tracked, with Portfolios,
   Team work and Teams, dependency references, and forecast headers; histogram rows and start-forecast rows stay in the
   database. The fingerprint and the skip decision are taken on that.
2. **A computed round replaces forecasts without loading their histograms.** The simulation sets new forecasts as
   today; Entity Framework deletes the old forecast rows, and the database's cascade removes their histogram rows.
3. **The round's own Portfolio read is the catalogue read** (no forecasts), and the trigger's "which Teams does this
   Portfolio have" check reads Team ids only.
4. **The write-back resolves against one named full read, taken after the save.** When, and only when, the Portfolio
   has a forecast write-back mapping and the licence allows write-back, the round takes a **fresh, no-tracking read of
   the Portfolio's Features with their forecasts and histograms**, after the save and the confirm stamp (computed and
   skipped rounds alike). What it hands to the write-back stage is the **Portfolio itself from that read**: with its
   work tracking connection, the connection's write-back mapping definitions, and its forecast-loaded Features, because
   the write-back resolution reads all three and swallows any failure into an empty list, so a read missing one of
   them would drop the write-back silently. The staged values are therefore the stored values of this round, the same
   as today's. No mapping: no read. Domain-event handlers keep their own scope and read
   in full.
5. **Outputs are identical**: the same percentiles, histogram rows, fingerprints and staged write-back values on SQLite
   and PostgreSQL.

## Alternatives considered

- **Hand the write-back the tracked gathered Features**: no extra read, but the gather holds the Features of the
  involved Teams, not the Portfolio's Features (a Feature with no Team yet is missing, a Feature of another Portfolio is
  present), and on a skipped round they carry no histograms, so the write-back would drop every Feature. Rejected.
- **Keep the full tracked `GetById` for the round**: today's behaviour, but it re-imports every histogram into the
  tracked graph that this ADR exists to shrink. Rejected.
- **Set-based delete plus bulk insert**: fastest on PostgreSQL (binary `COPY`), but provider-specific; the
  general-purpose bulk library (EFCore.BulkExtensions) is under a dual commercial licence. Rejected (OSS-first, two
  providers).
- **Store percentiles and drop histograms**: changes what a forecast stores and how percentiles are read (C12). Out of
  scope.
- **Keep the full gather and only skip earlier**: the fingerprint needs the gathered inputs, so nothing earlier can
  decide. Rejected.

## Consequences

**Positive**
- A skipped round without a write-back mapping reads headers and throughput only; a computed round issues one delete
  per forecast instead of one per histogram row; the tracked graph (and the GC work behind it) shrinks; the write
  transaction is shorter.

**Negative / accepted**
- A Portfolio with a forecast write-back mapping pays one untracked histogram read per round, outside the write
  transaction.
- The round depends on the cascade being enforced by the database. On SQLite that holds only while `PRAGMA
  foreign_keys` is on for the connection; the test below pins it rather than assuming the provider's default.
- A computed round's insert of new histogram rows stays as long as it is today; K4's computed-round target depends on
  how much of the round is simulation, which this ADR cannot change.

## Enforcement

- Provider-backed tests (SQLite and PostgreSQL): after a computed round no histogram row is orphaned; the stored rows
  equal those written by the previous code for the same seed. On SQLite the test first asserts `PRAGMA foreign_keys`
  reads 1 on the round's connection.
- Write-back, both providers: for a computed and a skipped round with a forecast write-back mapping, the staged field
  values equal those of the previous code, including a Feature with no Team; without a mapping, no histogram query is
  issued.
- The fingerprint golden of Story 6354 is unchanged; the dev backup skips and computes the same rounds as before.
