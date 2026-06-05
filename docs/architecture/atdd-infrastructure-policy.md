# ATDD Infrastructure Policy

Per `nw-distill` § Project Infrastructure Policy. One file per project. Apply-if-exists; write-if-absent; rewrite with `--policy=fresh`. Git history is the audit trail.

Bootstrapped during the `time-in-state-and-staleness` slice-03 DISTILL run (2026-05-25). This is a C#/.NET 8 backend + React/TypeScript frontend + Playwright E2E project — NOT the Python/Hypothesis pilot. The Python-pilot artifacts (`tests/common/state_delta.<ext>`, `assert_state_delta` Universe assertions, Hypothesis/PBT harnesses, `__SCAFFOLD__` stubs) do NOT apply here; the C#/TS rows of the polyglot matrix govern (NUnit `[Ignore]` and Playwright `test.fixme` are the skip markers; backend ATs are black-box example-based via `WebApplicationFactory<Program>`). The mechanisms below were already encoded as precedent across slices 01–02 and are recorded here for the audit trail.

## Driving
| Port | Mechanism | Note |
|---|---|---|
| HTTP API (backend) | `WebApplicationFactory<Program>` (`TestWebApplicationFactory<Program>` + `WithTestAuthentication`) | Real ASP.NET host; `client.AsTeamAdmin` / `AsPortfolioAdmin` / `AsViewer` for RBAC. Routes are `/api/latest/...`. |
| Production React app (E2E) | Playwright against a locally-started app, Page Object Model only | No inline `page.locator` in specs. POMs in `tests/models/`. |

## Driven internal (real)
| Port | Mechanism | Note |
|---|---|---|
| EF `LighthouseAppContext` + repositories (`IRepository<T>`, `IWorkItemStateTransitionRepository`) | Real EF context via the test factory; `Database.EnsureDeleted()` + `EnsureCreated()` per `[SetUp]` | Mirrors `ForecastFilter*IntegrationTest`. Sqlite + Postgres lockstep in CI. |
| Seeded demo data (E2E) | `loadDemoScenario(request, scenarioId)` + `waitForBackgroundUpdates(request)` (POST `/api/latest/demo/scenarios/{id}/load`) | Deterministic seeded teams/portfolios/items with known states. PREFERRED over live connector syncs for walking-skeletons (live syncs flake on 0-WIP + overview Search hidden during active syncs). |

## Driven external / non-deterministic (fake)
| Port | Fake | Note |
|---|---|---|
| `ILicenseService` | `Mock<ILicenseService>` (`CanUsePremiumFeatures() → true`) | Injected via `RemoveAll` + `AddScoped` in the factory builder. |
| `IWorkTrackingConnector` (Jira / ADO / Linear / CSV) | `Mock<IWorkTrackingConnector>` for service-seam ATs; live connector sync only in E2E when unavoidable (deprecated for walking-skeletons in favour of demo data) | Service-seam ATs drive the REAL `WorkItemService` + real EF; only the connector boundary is faked. |
| `IForecastService` (Monte Carlo simulation) | `Mock<IForecastService>` injected via `RemoveAll` + `AddScoped` in the factory builder | Added during `blackout-day-forecast-shift` DISTILL (Epic 4974, 2026-06-05). The simulation is non-deterministic; for tests asserting a date/count *derived from* a forecast (not the simulation itself), pin the days value with a `WhenForecast` whose simulation mass sits on a single key so `GetProbability(p)` is fixed. The controller's settings-coupled `ITeamMetricsService.GetForecastThroughputStatus` is likewise stubbed when only the date/count projection is under test. |
