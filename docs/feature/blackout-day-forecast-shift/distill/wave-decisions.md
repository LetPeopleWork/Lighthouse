# DISTILL Wave Decisions — blackout-day-forecast-shift (Epic 4974)

Designer: Quinn (nw-acceptance-designer) · Date: 2026-06-05 · Density: lean (Tier-1 [REF] only).

## Reconciliation HARD GATE

Read DISCUSS + DESIGN sections of `feature-delta.md` (no separate `discuss/`/`design/`/`devops/wave-decisions.md` files exist for this feature — the deltas live in `feature-delta.md`). **Reconciliation passed — 0 contradictions.** DISCUSS D1–D9 and DESIGN DDD-1..7 / DC-1..5 are mutually consistent; DC-2 (A1) is the single locked pivotal decision and is honoured by every test (periods threaded as a parameter, never fetched by a model).

## Test-stack decisions

- **Lang/stack**: C# .NET 8 (actually net10.0 per the .csproj) — NUnit 4.6 + Moq + EF-InMemory(Sqlite) + `WebApplicationFactory<Program>`. The generic pytest-bdd/Hypothesis/`__SCAFFOLD__`/`assert_state_delta` guidance does NOT apply (per `docs/architecture/atdd-infrastructure-policy.md`). No `.feature` files, no FsCheck, no PBT.
- **Skip marker**: `[Ignore("pending DELIVER — Epic 4974 US-0X ...")]` on each story test class. Confirmed: with the marker, the suite reports the four story classes as Skipped and `main` stays green; ArchUnit guards run and pass.
- **Mandate 7 RED-ready scaffolding**: N/A for this stack/feature. DISTILL writes ONLY test code (D8 — no production forecasting code may be written before manual review). The shift changes the *value* of existing response fields, not the contract, so tests drive through TODAY's endpoints and compile against current production symbols; no production scaffold stubs are needed (and would violate D8).

## Test-level decisions (per story)

| Story | Level | Driving port / seam | Why this level |
|---|---|---|---|
| US-01 When dates | Full HTTP (`WebApplicationFactory`) | `POST /api/latest/forecast/manual/{id}` | Port-to-port; `ExpectedDate` observed in JSON. Deterministic via faked `IForecastService` + stub `ITeamMetricsService.GetForecastThroughputStatus` wired through the production composition root. |
| US-02 by-date | Full HTTP | `POST /api/latest/forecast/manual/{id}` | Same factory; `HowMany` stubbed to echo the day-count so the working-day-vs-calendar-day decision is observable. |
| US-03 feature/delivery | Full HTTP (shared `IntegrationTestBase`) | `GET /api/latest/deliveries/portfolio/{id}` | Feature forecast persisted deterministically via `SetFeatureForecasts`; real DI projection through `DeliveryWithLikelihoodDto.FromDelivery`. |
| US-04 write-back + compose guard | **Service-level** (`WriteBackTriggerService` + Moq) | `IWriteBackService.WriteFieldsToWorkItems` capture | A true port-to-port write-back is a real Jira/ADO field write (external non-deterministic boundary → fake/capture per the Architecture-of-Reference). The captured `WriteBackFieldUpdate.Value` is the lowest level that observes the written date honestly. |

## Determinism strategy (Architecture-of-Reference port treatments applied)

- **Driving (HTTP API)**: real adapter via `WebApplicationFactory<Program>` + `WithTestAuthentication` (`client.AsTeamViewer` / `AsPortfolioViewer`). Matches the project policy row.
- **Driven internal (EF repositories)**: real EF Sqlite via the test factory; `BlackoutPeriod`, `Team`, `Portfolio`, `Feature` seeded through `IRepository<T>`. Matches the policy.
- **Driven non-deterministic (Monte Carlo `IForecastService`, `ITeamMetricsService` settings-coupled construction)**: faked via `RemoveAll` + `AddScoped` so the percentile *days* value is pinned (mass on day 10 ⇒ `GetProbability(p)==10`). This is the one new policy specialization for this feature — recorded as a row in `atdd-infrastructure-policy.md`. The shift under test is a pure date transformation, so a deterministic days input is the correct isolation: it proves the *date projection* changed without re-testing the simulation (D4).

## US-04 service-level seeding caveat (DELIVER action)

`WriteBackTriggerService`'s current 4-arg ctor takes no `IRepository<BlackoutPeriod>`, so the test cannot seed a blackout the production code reads without a compile break (forbidden — must drive observable behaviour only). The future-blackout write-back tests therefore document the blackout in their method name and assert the shifted value; they are RED because the date is unshifted. DELIVER: inject `IRepository<BlackoutPeriod>` (DDD-2 fetch-once per `TriggerWriteBackForTeam`), add a mock-period seam to the fixture, un-ignore. The compose-guard test additionally asserts `feature.Forecast.GetProbability(85)` is unchanged (already true) so it pins "no double-count" once the date shift lands.

## ArchUnit decision

`BlackoutForecastShiftSeamArchUnitTest` is written to be **GREEN immediately** (a guard, not a RED feature test) — it references only existing namespaces and asserts `Models.Forecast.*` / `Feature` / `Delivery` do not depend on `Services.Interfaces.Repositories`. This upholds ADR-058 A1 (periods threaded as a parameter, never a repo dependency in a model) and must stay GREEN through DELIVER.

## Deferred / needs-user-decision

- **OQ-2 (DESIGN, non-blocking)**: whether `ForecastController`/`DeliveriesController` source periods directly via `IRepository<BlackoutPeriod>` or via a thin `ITeamMetricsService` accessor is a DELIVER implementation choice; the tests are agnostic (they drive through the HTTP boundary and never reference the seam).
- **D8 manual-review gate**: every slice that touches forecasting/forecast-result code must be briefed and manually reviewed by the user BEFORE commit. DISTILL wrote only test code; no forecasting production code was touched.
