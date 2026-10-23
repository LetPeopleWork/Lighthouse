# ADR-256: An unchanged Portfolio forecast is confirmed, not recomputed, unless a person asked

**Status**: Proposed (DESIGN, 2026-10-23; recommendation taken in PROPOSE mode; revised after the review gate, which
returned "approved with notes").
**Feature**: `story-6354-skip-unchanged-forecasts` (ADO Story #6354)
**Relates to**: ADR-197 (forecasts have no periodic refresh of their own), ADR-249 (a cancelled forecast keeps its
rows), the `FetchFingerprint` convention (a fingerprint stored on its owner; null means changed; a guard test lists
the members).

## Context

Every Team and Portfolio refresh triggers a forecast of every Portfolio involved. On a large instance (400 to 920
Features, 6 to 9 Teams) each forecast spends 4 to 8 minutes simulating, even when the refresh found nothing new, and
the app stalls meanwhile. Fetching nothing does not mean that nothing the forecast reads changed: a Delta refresh can
still remove items, recompute remaining work and move automatic Feature WIP, and the throughput window moves every
day. In multi-replica deployments, queue admission is shared through Redis, so any per-process memory of "who asked"
can be lost.

## Decision

1. **Fingerprint at the simulation's input boundary.** A pure `ForecastInputFingerprint` renders exactly what the run
   is about to be handed:
   - today (the instance day) and the simulation limits;
   - the in-scope Features in forecast order;
   - the Teams of those Features;
   - every row of Feature, Team and remaining items;
   - per Team, the throughput as handed, its chip status and its Feature WIP;
   - per Feature, `ForecastWaits.Of(referenceId)`.

   It is rendered culture-invariantly, sorted ordinally wherever order carries no meaning, and digested with SHA-256
   as 128-bit hex. A guard test lists every member.
2. **Compare where the inputs already are; act in the shell.** `ForecastService.UpdateForecastsForPortfolio(portfolio,
   today, ct)` computes the fingerprint after gathering and before simulating.
   - When it equals `Portfolio.ForecastInputFingerprint`, the method returns `ConfirmedUnchanged` and writes nothing.
     `ForecastUpdater` then stamps the in-scope forecasts' `CreationTime` as confirmed now, with one set-based update.
     It writes a `RefreshLog` row with `Skipped = true` and logs `forecast=skipped after Nms (inputs unchanged)`. It
     stages write-back and publishes `PortfolioForecastsUpdated` exactly as a computed run does.
   - Otherwise the method simulates, saves the forecasts and returns `Computed`. `ForecastUpdater` stages write-back,
     publishes, and only then records the fingerprint, **last**, with a CAS against the value read at gathering.
3. **The fingerprint is written only by set-based updates.** EF ignores the column on entity updates, so a stale
   Portfolio save cannot write an old value back. Because it is recorded last, the stored fingerprint is never newer
   than the stored forecasts.
4. **Fail towards forecasting.** No stored fingerprint means compute. A failed or cancelled compute never records one.
   A failed or cancelled run also clears it, best-effort, with an uncancelled token.
5. **A person always gets a computed forecast.** Each user entry point clears the stored fingerprint of every Portfolio
   it touches, in the database, before it queues anything: the two update-forecast endpoints, Team refresh, Portfolio
   refresh, and both refresh-alls. The next forecast of each of those Portfolios computes, on any replica and through
   any coalescing path. A run in flight loses its CAS. Nothing about the origin travels through the queue, the
   updaters or `TeamDataRefreshed`.

## Alternatives considered

- **A rule on fetch outcomes** ("every Team fetched 0, no settings change, forecast from today"): rejected by the
  maintainer (MD-1). A fetch of 0 still removes items, shrinks membership and moves WIP.
- **Fingerprint the upstream settings** (blackouts, filters, sizes, forecast settings): a second list of the same
  knowledge, which drifts from what the run reads.
- **Check in `ForecastUpdater` with its own read of the inputs**: twice the input cost, and a second reading path.
- **Save the fingerprint in the same `SaveChanges` as the forecasts**: a stale full-entity `Update` could write an old
  value back, and a person's clear during the run would be overwritten.
- **An in-memory fingerprint, or an in-memory "user asked" mark carried as an `UpdateOrigin`**: either is lost across
  replicas, because Redis admission parks a request on another replica. The mark is also lost when the forecast
  trigger returns early on a held or already-owed forecast. The fingerprint is also lost on every restart.
- **A confirmation that publishes nothing**: the daily snapshot upsert and Delivery publishing would stop following
  intra-day target-date changes, and their best-effort retries would wait up to a day.

## Consequences

**Positive**
- A background round that changed nothing the forecast reads saves the simulation, which is the minutes. It still pays
  for the input gathering, the hash, the stamp, and the write-back and publishing every round already does.
- Stored forecasts are never older than their fingerprint. When in doubt, the forecast computes.
- Dates stop jittering between no-change rounds, because the simulation is unseeded.

**Negative / accepted**
- A skip is possible only within one instance day, because the first round of each day computes.
- A skip still loads the Feature graph with its histograms. If that alone breaks the 5 s target on production-shaped
  data, the check moves to a lighter read (measured in DELIVER).
- A Feature's `lastUpdated` now means "confirmed current at", not "computed at". The clients' wording is checked before
  slice 02 ships.
- `IForecastService.UpdateForecastsForPortfolio` changes signature and return type, so its test doubles follow. Up to
  four controllers gain `IPortfolioRepository`; S107 is checked in DELIVER. One expand-only migration adds
  `Portfolio.ForecastInputFingerprint` and `RefreshLog.Skipped`.

## Enforcement

- A guard test over `ForecastInputFingerprint.RegisteredMembers`, with one case per member and dependency cases
  (`IgnoreDependencies`, `CanBeForecast`, a changed reference target). A reflection guard checks that every public
  property of `ForecastSimulationLimits` is a member.
- Stability tests:
  - a golden fingerprint for a fixed input;
  - equal fingerprints after a fresh `DbContext` read on SQLite and on Postgres;
  - a stale Portfolio `Update` leaves the column unchanged;
  - a clear during a run leaves the column null afterwards.
- Acceptance tests:
  - each AC-1.2 change computes on the next round;
  - every user entry point makes the next forecast compute;
  - a skip leaves every forecast row and histogram byte-equal (slice 01), or changes only `CreationTime` (slice 02).
