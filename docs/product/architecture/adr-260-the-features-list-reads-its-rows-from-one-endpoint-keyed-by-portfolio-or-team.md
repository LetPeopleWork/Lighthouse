# ADR-260: The Features list reads its rows from one endpoint keyed by Portfolio or Team

**Status**: Proposed (DESIGN, 2026-10-23, PROPOSE mode; revised after the architecture review).
**Feature**: `epic-6400-fast-with-many-features` (Story #6408, slice 04)
**Relies on**: [ADR-258](./adr-258-reads-that-show-no-forecast-dates-never-load-forecast-histograms.md) (histogram-free
dependency graph, in-progress by reference id), ADR-135 (global positions), the RBAC guard attribute
(`RbacGuard(PortfolioRead | TeamRead, ScopeIdRouteKey)`, as on `PortfolioController` and `TeamMetricsController`).

## Context

A Portfolio's Features tab sends the Portfolio's Feature ids in the query string to `GET /features/ids` (full
`FeatureDto` per row), then `GET /features/references` for the parent column, and one
`GET /teams/{id}/metrics/featuresInProgress` per involved Team. The Team tab has the same shape. Each `FeatureDto`
carries, per contributing Team, a start and a completion forecast at four percentiles, which the grid never reads
(only the Delivery timeline does). The list shows loading placeholders forever when it fails or is empty.
Lighthouse-Clients read `GET /features` and `FeatureDto`; those must not change (C6, D20).

Today `/features/ids` and `/features/references` carry no `RbacGuard`: they are `[Authorize]` only and **filter** the
result to what the caller may read, so an unreadable owner yields fewer rows, never a 403. The per-Team
`featuresInProgress` route is guarded `TeamRead` and answers 403 for an unreadable Team.

## Decision

1. **Two additive routes on one new thin controller**: `GET api/latest/portfolios/{portfolioId}/feature-list`
   (guarded `PortfolioRead`) and `GET api/latest/teams/{teamId}/feature-list` (guarded `TeamRead`), each also under
   `api/v1` as every controller is. No id list travels in the URL. The guard is new for this read: an owner the caller
   may not read answers **403** (the owner's own detail route already answers 403, so the tab is never reachable for
   it), while everything inside a readable owner is **filtered** exactly as `/features/ids` filters today.
2. **One response**: `features` (rows), `parents` (reference id, name, url of each parent the caller may read), and
   `activeWork` (per readable involved Team, the Feature references it is working on today; an unreadable involved Team
   is left out, not refused).
3. **A row is a field subset of `FeatureDto`, same names and shapes**, without `teamForecasts`. The frontend parses it
   with the existing `Feature` model unchanged. Row values come from the same domain properties (`Feature.Forecast`,
   `WhenWorkBegins`, `TeamsWithoutForecast`, positions, move verdicts, dependency verdicts) as `FeatureDto`.
4. **Same order and readability as today**: Features in forecast order; a Team list holds the Team's Features in
   Portfolios the caller may read; unreadable blockers are withheld, unreadable Portfolios never named.
5. **Server path**: controller → driving port `IFeatureListService` → the owner's Features read in the database by
   owner, the histogram-free dependency graph, the position map, move verdicts, and `TeamMetricsService`'s in-progress
   references. The dependency-reading code now private to `FeaturesController` moves to one collaborator both use. The
   controller depends on no repository. `IFeatureListService` and that collaborator only read, so they take a narrow
   read-only interface (provisional `IFeatureListReader`: the owner read, the histogram-free graph, the reference-id
   read, the parent read) that `FeatureRepository` implements and the composition root registers as the same scoped
   instance. This follows the house pattern of `ReportRepository` serving `IReportReader` beside `IReportWriter` and
   `ISecretCustodyReader` narrowing `ISecretCustodyService`; the new components are never handed the write-capable
   `IFeatureRepository`.
6. **Which histograms the list still reads.** `Feature.Forecast` is the aggregate of **every per-Team completion
   forecast** (`AggregatedWhenForecast(Forecasts)`); there is no Feature-level completion row to read instead. So the
   list keeps all completion histograms. It drops only the per-Team **start** histograms: `WhenWorkBegins` reads the
   one Feature-level start forecast (the start row with no Team). The K2 saving therefore comes from scoping the read
   to one owner, dropping per-Team start histograms and dropping the `teamForecasts` payload, not from dropping
   completion histograms.
7. **`GET /features`, `/features/ids`, `/features/references` and `FeatureDto` are unchanged**, and Lighthouse-Clients
   stay on them.

## Alternatives considered

- **One endpoint with an owner discriminator** (`/features/list?owner=portfolio&id=…`): the RBAC guard keys on a route
  value per owner kind; a discriminator needs custom guarding. Rejected.
- **No guard, filter only (as `/features/ids`)**: an unreadable owner would answer an empty list, which reads as "no
  Features". Rejected; 403 tells the caller the truth and matches the owner's detail route.
- **The same routes returning full `FeatureDto`**: removes the round trips but keeps per-Team start histograms and the
  payload the grid ignores. Rejected for K2.
- **A precomputed read model of percentiles** (no histogram read at all): the larger win, but a schema and a write
  path, and read models wait for Epic 6264's data ownership. Recorded as the follow-up if K2 is missed.
- **GraphQL or a field-selection parameter**: a new technology or a second contract style for one screen. Rejected.

## Consequences

**Positive**
- One request per open instead of 2 + T; no instance-wide read; a smaller payload; parents and in-progress Teams
  arrive with the rows, so the grid re-renders once.

**Negative / accepted**
- A second read path to Feature rows exists beside `/features/ids`. A parity test keeps them equal until the clients
  move (a later Epic) and the old one can be retired.
- Completion histograms are still read per open. If K2 is missed on the scaled set, the read-model follow-up is next.

## Enforcement

- Parity: for every Feature on the dev backup and the scaled set, each row field equals the same field of `FeatureDto`
  from `/features/ids`; parents equal `/features/references`; `activeWork` equals the per-Team `featuresInProgress`
  references.
- RBAC, RBAC on: an owner the caller may not read → 403 on the new route; a readable owner holding blockers in an
  unreadable Portfolio and an unreadable involved Team → 200 with those blockers withheld, the Portfolio unnamed and
  that Team absent from `activeWork` (filtered, as today). RBAC off: every route answers 200 with everything.
- ArchUnitNET: the new controller depends on no `IRepository<>` or `I*Repository`.
