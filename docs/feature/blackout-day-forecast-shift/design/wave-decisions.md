# DESIGN Wave Decisions — blackout-day-forecast-shift

Feature: blackout-day-forecast-shift (ADO Epic 4974 "Blackout Days in Future")
Architect: Morgan (Solution Architect), interaction mode = PROPOSE
Date: 2026-06-05
Status: ACCEPTED — placement locked; DC-2 confirmed by the user (2026-06-05) as **A1**. Locked DISCUSS decisions D1–D9 inherited.

## Key Decisions

| # | Decision | Recommendation | ADR / source |
|---|---|---|---|
| DC-1 | Where the two translation primitives live | **Two pure functions on the existing static `BlackoutDaysExtensions`** (`ProjectWorkingDays`, `CountWorkingDays`) — Option A. NOT a new injectable service (Option C, rejected), NOT logic inside the forecast models (Option B, rejected) | ADR-058 |
| DC-2 | **LOCKED (user-confirmed 2026-06-05)** — how the no-DI models receive the periods | **A1: pass `IReadOnlyList<BlackoutPeriod>` as a method/ctor parameter.** A2 (pre-bound delegate) rejected. Upholds Models ↛ Repositories | ADR-058 |
| DC-3 | Where the global periods are fetched | **Once per inbound request** in the DI assembly layer (`ForecastController`, `DeliveriesController`→`FromDelivery`, `WriteBackTriggerService`), passed inward (D9 single fetch, no N+1) | ADR-058 |
| DC-4 | Orthogonality vs the shipped historical stripping | **Disjoint by construction** — stripping acts on the past SAMPLE (feeds the days value), projection acts on the future DATE; opposite sides of "today", never the same day. Pinned by the US-04 AC3 compose-guard test | ADR-058 / US-04 AC3 |
| DC-5 | Premium gating | **None** for US-01/02/03 (no gate on `BlackoutPeriod`/throughput stripping — verified); US-04 inherits the existing `CanUsePremiumFeatures()` write-back gate unchanged | ADR-058 |

## Architecture Summary

Ports-and-adapters, unchanged. This feature adds the forward day↔date working-day translation as **two pure functions extending the existing static `BlackoutDaysExtensions`** (single home with the shipped blackout math, D7): `ProjectWorkingDays(periods, start, workingDayCount)` (days→date, skips blackout days, rolls a landing date forward per D3) and `CountWorkingDays(periods, start, target)` (date→working-days). Both are pure — the clock and the period list are passed in, so D6 byte-identical falls out of the math (empty list ⇒ `AddDays` / `(t−start).Days`).

The **global** blackout set (`blackoutPeriodRepository.GetAll()`, unscoped, D9) is fetched **once per inbound request** in the DI-aware assembly layer and threaded inward as a materialised `IReadOnlyList<BlackoutPeriod>` — mirroring the shipped `GetBlackoutAwareThroughputForTeam` fetch-once pattern (no N+1). The persisted EF entities / pure DTOs that compute dates (`WhenForecastDto`, `HowManyForecast`, `Feature`, `Delivery`) receive the periods as a **parameter** (recommended A1) and acquire NO repository/service dependency — upholding the brief's Models ↛ Repositories invariant.

The Monte Carlo (`ForecastService`, `ForecastBase.GetProbability`/`GetLikelihood`, `Trials`, percentile math) is **untouched** (D4); only its date inputs (date→days) and date outputs (days→date) are wrapped at the assembly layer. The shipped historical-throughput stripping is orthogonal by construction (past sample vs future date) — pinned by the US-04 AC3 compose-guard test.

No new endpoint, no new DTO field, no EF migration, no new library, no new DI registration, no new RBAC surface.

C4: Container + Component diagrams in `docs/product/architecture/brief.md` → `## Application Architecture — blackout-day-forecast-shift`. ADR: 058.

## Reuse Analysis (verdicts)

- `BlackoutDaysExtensions` (`GetBlackoutDayIndices`/`IsBlackoutDay`) — **EXTEND** (D7; the two new pure functions belong beside the shipped blackout math).
- `IRepository<BlackoutPeriod>` / `GetAll()` — **REUSE AS-IS** (global set, D9; fetch-once pattern proven in `GetBlackoutAwareThroughputForTeam`).
- `WhenForecastDto` / `DtoExtensions.CreateForecastDtos` — **EXTEND** (thread periods through the existing When-DTO factory; no parallel DTO).
- `Feature.GetLikelhoodForDate` / `Delivery.CalculateMetrics` / `DeliveryWithLikelihoodDto.FromDelivery` — **EXTEND** (add a periods parameter, A1; no parallel projection).
- `ForecastController` / `DeliveriesController` / `WriteBackTriggerService` — **EXTEND** (existing assembly seams; add fetch + thread; inject `IRepository<BlackoutPeriod>` into write-back).
- `ForecastService` / `ForecastBase` / Monte Carlo — **REUSE AS-IS (untouched)** (D4).
- `TeamMetricsService` blackout-aware throughput — **REUSE AS-IS (untouched)** (D1; orthogonal, DC-4).
- ArchUnitNET suite + NUnit test classes — **EXTEND** (add ADR-058 rules/tests; no new test project).
- New `IWorkingDayProjector` service — **CREATE NEW — REJECTED** (over-engineered for pure functions with no collaborators; would fork the blackout math across two homes — ADR-058 Option C).

## Tech Stack

ASP.NET Core .NET 8 (OOP, ports-and-adapters/hexagonal). NUnit 4.6 + Moq + Microsoft.EntityFrameworkCore.InMemory + WebApplicationFactory. Stryker.NET ≥80% on new shift code (D8 / KPI). TngTech.ArchUnitNET (existing suite, Apache 2.0) extended with the ADR-058 rules. No new library, no new endpoint, no EF migration, no DI registration (Option A statics).

## Constraints (inherited + design-confirmed)

- D1 scope = forward translation only; config + stripping + backtest LOCKED.
- D2 trigger = configured periods; no toggle/RBAC surface.
- D3 landing-on-blackout rolls forward.
- D4 Monte Carlo / day-values untouched.
- D5 how-many-by-date forecasts the working-day count.
- D6 no-blackout byte-identical (falls out of the empty-list math).
- D7 reuse `BlackoutDaysExtensions`.
- D8 per-slice briefing + manual user review BEFORE commit; ≥80% mutation.
- D9 global periods, single fetch threaded inward.

## Upstream Changes

None. No DISCOVER/DIVERGE artifacts. SSOT already bootstrapped in DISCUSS (`job-forecast-skip-known-nonworking-days` in `jobs.yaml`, journey yaml, persona job-ref). This DESIGN wave appends only to `feature-delta.md`, `brief.md`, ADR-058, and this file.

## Story → design-element traceability

- **US-01** (When days→date): `BlackoutDaysExtensions.ProjectWorkingDays` + `WhenForecastDto`/`CreateForecastDtos` + `ForecastController` fetch/thread.
- **US-02** (by-date date→days): `BlackoutDaysExtensions.CountWorkingDays` + `ForecastController` seams ~57/80/93/103.
- **US-03** (feature/delivery): `HowManyForecast.TargetDate`, `Feature.GetLikelhoodForDate(date, periods)`, `Delivery.CalculateMetrics(periods, …)` line 102, `DeliveryWithLikelihoodDto.FromDelivery(delivery, periods)` + `DeliveriesController`.
- **US-04** (write-back + compose guard): `WriteBackTriggerService` (inject repo, project line 226) + the historical-strip × forward-shift compose-guard test.

## Pivotal decision for the human (PROPOSE mode)

**Confirm DC-2: A1 (pass `IReadOnlyList<BlackoutPeriod>` parameter — recommended) vs A2 (pass a pre-bound projection delegate).** This sets the shared-contract signatures (`WhenForecastDto` ctor, `CreateForecastDtos`, `Feature.GetLikelhoodForDate`, `Delivery.CalculateMetrics`, `FromDelivery`) the four slices build on. Recommendation: A1 — explicit, greppable, makes the D6 regression test a one-liner (`periods = []`), and matches the shipped `FilterBlackoutDaysFromRunChart(throughput, start, end, periods)` signature style.
