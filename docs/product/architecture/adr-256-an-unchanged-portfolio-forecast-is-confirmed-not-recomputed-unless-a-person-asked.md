# ADR-256: An unchanged Portfolio forecast is confirmed, not recomputed, unless a person asked

**Status**: Proposed (DESIGN, 2026-10-23; recommendation taken in PROPOSE mode; revised after the review gate, which
returned "approved with notes"; amended at the DISTILL review gate).
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

*Amended at the DISTILL review gate (2026-10-23): the forget writes a token rather than null (2, 4, 5), every start
forgets (6), the promise of a press is split between "Update forecast" and the refresh buttons (5), the port takes the
instance day as a `DateOnly` and an error while fingerprinting means compute (2, 4), the Feature read is narrowed
(Consequences), and no controller gains a parameter (Consequences).*

1. **Fingerprint at the simulation's input boundary.** A pure `ForecastInputFingerprint` renders exactly what the run
   is about to be handed:
   - today (the instance day) and the simulation limits;
   - the in-scope Features in forecast order;
   - the Teams of those Features;
   - every row of Feature, Team and remaining items;
   - per Team, the throughput as handed, its chip status and its Feature WIP;
   - per Feature, `ForecastWaits.Of(referenceId)`.

   It is rendered culture-invariantly, sorted ordinally wherever order carries no meaning, and digested with SHA-256
   as 128-bit hex. A guard test lists every member. Of the simulation limits, every public property is a member except
   `CoresSeen` and `MostRunsAtOnce`, which shape how the run is spread over the machine, not what it computes.
2. **Compare where the inputs already are; act in the shell.** `ForecastService.UpdateForecastsForPortfolio(portfolio,
   today, ct)`, with `today` a `DateOnly` from `ILighthouseClock.Today` (the instance day), computes the fingerprint
   after gathering and before simulating.
   - When it equals `Portfolio.ForecastInputFingerprint`, the method returns `ConfirmedUnchanged` and writes nothing.
     `ForecastUpdater` then stamps the in-scope forecasts' `CreationTime` as confirmed now, with one set-based update.
     It writes a `RefreshLog` row with `Skipped = true` and logs `forecast=skipped after Nms (inputs unchanged)`. It
     stages write-back and publishes `PortfolioForecastsUpdated` exactly as a computed run does.
   - Otherwise the method simulates, saves the forecasts and returns `Computed`. `ForecastUpdater` stages write-back,
     publishes, and only then records the fingerprint, **last**, with a CAS against the value read at gathering
     (null-safe: a run that read nothing recorded requires the column to still be null).
   - If fingerprinting throws, the run simulates and records nothing: a computed round, not a failed one.
3. **The fingerprint is written only by set-based updates.** EF ignores the column on entity updates, so a stale
   Portfolio save cannot write an old value back. Because it is recorded last, the stored fingerprint is never newer
   than the stored forecasts.
4. **Fail towards forecasting.** Nothing recorded (null) means compute, and so does a forget token. A failed or
   cancelled compute never records a fingerprint. A failed or cancelled run also forgets, best-effort, with an
   uncancelled token.
5. **A forget writes a token, and "Update forecast" always computes.** A forget writes `"forgotten:"` plus a new Guid,
   never null, so it changes the stored value whatever was there: a run that read null, an older fingerprint or an
   earlier token loses its CAS to any later forget. Each user entry point forgets every Portfolio it touches, in the
   database, before it queues anything: the two "Update forecast" endpoints, Team refresh, Portfolio refresh, and both
   refresh-alls. Nothing about the origin travels through the queue, the updaters or `TeamDataRefreshed`.
   - The two "Update forecast" buttons queue the forecast itself, so the forecast after the press computes, on any
     replica and through any coalescing path: a guarantee.
   - The four refresh buttons queue a refresh. A forecast of that Portfolio that runs between the press and the
     refreshed data uses the forget up, and the forecast the refresh causes may then be confirmed. That is correct
     whenever the fingerprint is, so it is best effort, not a guarantee. Docs say: "Update forecast always recomputes;
     a refresh you start recomputes unless a forecast of that Portfolio ran in between."
6. **Every start forgets.** An `ISeeder` forgets every Portfolio's fingerprint in one set-based update at every start
   and after a database restore, so the first round after an upgrade, a rollback, a roll-forward or a restore always
   computes. Another build may have computed forecasts in between without recording what from.

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
- **Writing null on a forget**: a run that read null (just after an upgrade or an earlier press) would record over a
  second press, because its CAS still matches.
- **A release-note step after a roll-forward** ("press refresh all"): relies on a person reading it; the start-up
  forget closes the same gap.

## Consequences

**Positive**
- A background round that changed nothing the forecast reads saves the simulation, which is the minutes. It still pays
  for the input gathering, the hash, the stamp, and the write-back and publishing every round already does.
- Stored forecasts are never older than their fingerprint. When in doubt, the forecast computes.
- Dates stop jittering between no-change rounds, because the simulation is unseeded.

**Negative / accepted**
- A skip is possible only within one instance day, because the first round of each day computes, and only after the
  first round since the app started.
- A skip still loads the Portfolio's Features with their histograms. The read is narrowed at query level to the
  Features of the Portfolio's Teams (it used to read every Feature and filter in memory). If a skip still costs more
  than a third of a computed run locally, or 5 s on the reporter's instance, the check moves to a lighter read
  (measured in DELIVER; a Debug line times gather and publish apart).
- A Feature's `lastUpdated` now means "confirmed current at", not "computed at". The clients' wording is checked before
  slice 02 ships.
- The refresh buttons promise a recompute only on a best-effort basis (Decision 5).
- `IForecastService.UpdateForecastsForPortfolio` changes signature and return type, so its test doubles follow. No
  controller gains a parameter: two already take `IPortfolioRepository`, and three change their existing
  `IRepository<Portfolio>` parameter to it. One expand-only migration adds `Portfolio.ForecastInputFingerprint` and
  `RefreshLog.Skipped` (database default `false`). `Program.cs` gains one seeder registration.

## Enforcement

- A guard test over `ForecastInputFingerprint.RegisteredMembers`, with one case per member and dependency cases
  (`IgnoreDependencies`, `CanBeForecast`, a changed reference target, the premium licence). A reflection guard checks
  that every public property of `ForecastSimulationLimits` except `CoresSeen` and `MostRunsAtOnce` is a member.
  "Too sensitive" cases check that what the simulation is not handed (cores, names, stored forecast dates and times)
  leaves the fingerprint alone.
- Stability tests:
  - a golden fingerprint for a fixed input;
  - equal fingerprints after a fresh `DbContext` read on SQLite and on Postgres;
  - a stale Portfolio `Update` leaves the column unchanged;
  - a forget during a run wins over that run, including a run that read nothing recorded or an earlier forget;
  - starting the app forgets every Portfolio;
  - a refresh row written without `Skipped` reads `false` on both providers.
- Acceptance tests:
  - each AC-1.2 change computes on the next round;
  - every user entry point makes the next forecast compute; "Update forecast" pressed while a forecast runs ends in a
    computed forecast; refresh-all with forecasts under way is pinned as best effort;
  - after a restart the next quiet round computes;
  - a skip leaves every forecast row and histogram byte-equal (slice 01), or changes only `CreationTime` (slice 02).
