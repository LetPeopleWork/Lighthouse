# ADR-058: The forward day↔date blackout translation is two pure functions on `BlackoutDaysExtensions`, threaded through the DTO/projection assembly layer — never inside the forecast models

## Status

Accepted — 2026-06-05 (DESIGN wave, feature `blackout-day-forecast-shift`, ADO Epic 4974). The pivotal DC-2 decision is confirmed by the user: **A1** (pass `IReadOnlyList<BlackoutPeriod>` as a method/ctor parameter). A2 (pre-bound delegate) rejected.

## Context

Epic 4974's remaining gap (DISCUSS feature-delta, D1) is the **forward day↔date working-day translation**. The Monte Carlo still produces *days*; turning days into a calendar date — and a target date into a working-day count — is currently blackout-blind. Six call sites do raw calendar arithmetic:

| Surface | Current code (blackout-blind) | Direction | Story |
|---|---|---|---|
| "When" percentile dates | `WhenForecastDto.GetFutureDate` → `Today.AddDays(days)` | days→date | US-01 |
| How-Many "by a date" + likelihood-by-date | `ForecastController` lines ~57/80/93/103 → `(target - Today).Days` | date→days | US-02 |
| How-Many target date | `HowManyForecast.TargetDate` → `CreationTime.AddDays(days)` | days→date | US-03 |
| Feature/Delivery percentile dates | `Delivery.cs:102` `Today.AddDays(...)`, `DeliveryWithLikelihoodDto` | days→date | US-03 |
| Feature likelihood-by-date | `Feature.GetLikelhoodForDate` → `(date - Today).Days` | date→days | US-03 |
| Forecast write-back date | `WriteBackTriggerService:226` → `Today.AddDays(daysToCompletion)` | days→date | US-04 |

### Locked constraints inherited from DISCUSS

- **D1** scope = forward translation only; config + historical-throughput stripping + backtest are shipped and LOCKED.
- **D3** a computed date landing ON a blackout day rolls forward to the next non-blackout day.
- **D4** Monte Carlo, `Trials`, percentile math, `GetProbability`/`GetLikelihood` day-values are UNTOUCHED. Only date projection (days→date) and target conversion (date→working-days) change.
- **D5** How-Many "by a date" forecasts for the working-day count.
- **D6** no-blackout teams must be byte-identical to today (regression guard).
- **D7** reuse `BlackoutDaysExtensions` (`GetBlackoutDayIndices`, `IsBlackoutDay`).
- **D9** blackout periods are GLOBAL (`blackoutPeriodRepository.GetAll()` is unscoped) — the projector/counter need no scope resolution; a single fetch can be threaded everywhere.

### The decisive grounding read

Every one of the six call sites lives in one of two layer kinds:

- **Persisted EF entities / pure DTOs with no DI**: `WhenForecastDto`, `HowManyForecast`, `Feature`, `Delivery`. These compute dates via `static AddDays`. They have no constructor-injected services and must not acquire one — a domain entity depending on `IRepository<BlackoutPeriod>` inverts the dependency arrow (Models → Repositories) the brief's ports-and-adapters invariant forbids and the existing ArchUnitNET suite guards.
- **DI-aware assembly/service layer**: `ForecastController` (already injects `ITeamMetricsService`, which already owns `IRepository<BlackoutPeriod>` and calls `GetAll()`), `DeliveryWithLikelihoodDto.FromDelivery` (a static projection invoked from `DeliveriesController`), `WriteBackTriggerService` (a DI service). This is the only layer that can legitimately *fetch* the global periods.

The shipped historical-stripping precedent (`TeamMetricsService.GetBlackoutAwareThroughputForTeam`, line 162) already establishes the house pattern: **fetch `blackoutPeriodRepository.GetAll().ToList()` once in the service layer, then pass the materialised list into a pure `BlackoutDaysExtensions` helper.** The translation should mirror this exactly.

## Decision

**Option A (chosen): two new pure functions extending `BlackoutDaysExtensions`, threaded into the call sites by the DTO/projection assembly layer that fetches the global periods once.**

Add to the existing static `BlackoutDaysExtensions`:

1. **`ProjectWorkingDays(this IEnumerable<BlackoutPeriod> periods, DateTime startDate, int workingDayCount) → DateTime`** — advance calendar days from `startDate`, skipping any day inside a blackout period, until `workingDayCount` working days have elapsed; if the landing day is itself a blackout day, roll forward to the next non-blackout day (D3). Reuses `GetBlackoutDayIndices` / `IsBlackoutDay`.
2. **`CountWorkingDays(this IEnumerable<BlackoutPeriod> periods, DateTime startDate, DateTime targetDate) → int`** — count non-blackout days in `(startDate, targetDate]`. Reuses `GetBlackoutDayIndices`.

Both are **pure** (no I/O, no `DbContext`, no clock) — `startDate` / `targetDate` and the period list are passed in by the caller. Empty period list ⇒ identity behaviour: `ProjectWorkingDays(empty, d, n) == d.AddDays(n)` and `CountWorkingDays(empty, d, t) == (t - d).Days` (D6 byte-identical falls out of the math, not a special case).

**The day↔date translation contract:**
- days→date: `ProjectWorkingDays(periods, Today, GetProbability(p))` replaces `Today.AddDays(GetProbability(p))`.
- date→days: `CountWorkingDays(periods, Today, target)` replaces `(target - Today).Days`, then feed the result into the unchanged `GetLikelihood(...)` / `HowMany(throughput, ...)`.

**Threading the global periods (D9) — single fetch per request, passed inward:**

| Call site | Who fetches `GetAll()` | How the periods reach the pure function |
|---|---|---|
| US-01 When dates (`WhenForecastDto`) | `ForecastController.RunManualForecastAsync` (has `ITeamMetricsService`; add a thin blackout accessor or inject `IRepository<BlackoutPeriod>`) | `CreateForecastDtos(...)` / the `WhenForecastDto` ctor gains a `DateTime ProjectExpectedDate` delegate **or** an `IReadOnlyList<BlackoutPeriod>` param — see pivotal decision |
| US-02 by-date (`ForecastController` 57/80/93/103) | same controller | `CountWorkingDays(periods, Today, target)` computed in the controller before calling `GetLikelihood`/`HowMany` |
| US-03 feature/delivery (`Feature`, `Delivery`, `DeliveryWithLikelihoodDto`) | `DeliveryWithLikelihoodDto.FromDelivery` (passed the periods from `DeliveriesController`) / `Delivery.CalculateMetrics(periods, ...)` | periods passed as a parameter down the projection; `Feature.GetLikelhoodForDate(date, periods)` overload, `Delivery.ToWhenPercentile(periods, ...)` |
| US-04 write-back (`WriteBackTriggerService:226`) | inject `IRepository<BlackoutPeriod>` into `WriteBackTriggerService`, fetch once per `TriggerWriteBackForTeam` | `ProjectWorkingDays(periods, Today, daysToCompletion)` |

One `GetAll().ToList()` per inbound request/operation — no N+1 (D9: the set is global, so a single materialised list serves every team/feature in the request).

### The pivotal decision the human must confirm

**How do the pure DTOs/entities (`WhenForecastDto`, `HowManyForecast`, `Feature`, `Delivery`) receive the blackout periods without taking a DI dependency?** Two sub-shapes, both honour the no-DI-in-models invariant:

- **A1 (recommended): pass the materialised `IReadOnlyList<BlackoutPeriod>` as a method/ctor parameter** down the assembly path (e.g. `WhenForecastDto(forecast, probability, periods)`, `Feature.GetLikelhoodForDate(date, periods)`, `Delivery.CalculateMetrics(periods, percentiles)`). Explicit, greppable, trivially unit-testable with a literal list; the model still owns *no* fetch. Changes a handful of signatures (bounded blast radius — grep-first per CLAUDE.md before touching the shared `CreateForecastDtos`/`FromDelivery` contract).
- **A2: pass a pre-bound projection delegate** (`Func<int,DateTime> projectDate` / `Func<DateTime,int> countDays`) the assembly layer closes over the periods with. Keeps `BlackoutPeriod` out of the model signatures entirely; slightly more indirection, harder to grep.

**Decision: A1 (user-confirmed 2026-06-05).** It keeps the translation primitive and its inputs visible at every seam, makes the D6 regression test a one-liner (`periods = []`), and matches the shipped `FilterBlackoutDaysFromRunChart(throughput, start, end, periods)` signature style already in `TeamMetricsService`. This sets the shared-contract signatures (`WhenForecastDto` ctor, `CreateForecastDtos`, `Feature.GetLikelhoodForDate`, `Delivery.CalculateMetrics`, `FromDelivery`) the four slices build on — grep-first and extend the test factory before touching them (CLAUDE.md shared-contract rule).

### Orthogonality vs. the shipped historical stripping (US-04 AC3)

The shipped `ComputeBlackoutAwareThroughput` and this forward shift operate on **disjoint concerns and cannot double-count by construction**:

- **Historical stripping** changes the **throughput SAMPLE**: it removes past blackout days from the observed-throughput window so they don't dilute the empirical rate the Monte Carlo draws from. Its output feeds `GetProbability` → a *days* value.
- **Forward projection** changes only the **calendar DATE** a given *days* value maps to (and the working-day count a target maps to). It never touches the sample, the simulation, or the days value (D4).

A blackout day in the **past** is acted on once (sample stripping); a blackout day in the **future** is acted on once (date projection). They live on opposite sides of "today" and never operate on the same day. The compose-guard test (US-04 AC3) pins this empirically: with both a historical and a future blackout configured, `GetProbability(p)` (the days value) is identical to the historical-strip-only run — the forward shift moves only the rendered date.

## Alternatives Considered

### Option B — compute shifted dates inside the forecast-result objects (`WhenForecast`/`HowManyForecast`/`Feature`/`Delivery`) by giving them access to the blackout periods

Give each entity a `BlackoutPeriods` field/property populated at construction, and have `TargetDate`/`GetLikelhoodForDate`/`ExpectedDate` consult it internally.

- **Rejected.** Two failure modes. (1) If the entity *fetches* the periods, a persisted EF model gains an `IRepository` dependency — a direct inversion of the ports-and-adapters arrow the brief and the ArchUnitNET suite forbid (Models ↛ Repositories/Services). (2) If the periods are *set* on the entity as state, the entity is no longer a faithful persistence projection — `HowManyForecast`/`WhenForecast` are EF-mapped, and a transient `BlackoutPeriods` field risks the same `[NotMapped]`-dropped-by-copy-ctor trap recorded in project memory for `WorkItem`. Either way the date computation becomes hidden entity state instead of an explicit, testable transformation, and the D6 regression and AC4 (`GetProbability` unchanged) become harder to prove because the date logic is entangled with the day logic in the same object.

### Option C — a new injectable `IWorkingDayProjector` domain service

Introduce `IWorkingDayProjector` / `WorkingDayProjector` with `ProjectWorkingDays` / `CountWorkingDays`, register in DI, inject at the assembly layer.

- **Rejected (over-engineered for this scope).** The two functions are *pure* — they take all inputs as parameters and have no collaborators to mock. A new interface + implementation + DI registration + lifetime decision buys nothing over two static methods on the helper class that *already exists for exactly this domain* (`BlackoutDaysExtensions`, D7). It would also fork the blackout math across two homes (the shipped stripping uses the static extensions; the shift would use a service), eroding the single-home property. An injectable seam is the right shape when there is hidden I/O, a clock, or a swappable adapter to probe — there is none here (the clock is the caller's `Today`, passed in). If a future need to swap the working-day rule (e.g. recurring rules, #4577) arises, *then* promote the two statics behind an interface; YAGNI until then. This is the same "no new service, keep it a pure helper" stance ADR-021/ADR-018 took for per-state aggregation.

## Consequences

**Positive**
- One pure home (`BlackoutDaysExtensions`) owns the whole day↔date translation, beside the shipped stripping helpers (D7) — boundary tests (roll-forward, landing-on-blackout, empty-list identity) live in one place; strong for the ≥80% mutation gate (D8/KPI).
- D6 byte-identical falls out of the math: empty period list ⇒ `AddDays`/`(t-d).Days` identity. The regression golden test passes `periods = []`.
- D4 is provably untouched: the Monte Carlo, `GetProbability`, `GetLikelihood`, `HowMany` are not edited; only their *inputs* (date→days) and *outputs* (days→date) are wrapped at the assembly layer. AC4 is a direct assertion.
- D9 single-fetch: one `GetAll().ToList()` per request threaded inward — no N+1, mirrors the shipped `GetBlackoutAwareThroughputForTeam` fetch pattern.
- No new endpoint, no new DTO field, no EF migration, no new library, no DI registration (Option A's statics). Additive method signatures only (the A1 contract change) — bounded, grep-first.
- Clients (CLI + MCP) transparent: the *value* of existing date fields changes, not the contract — no `FEATURE_REQUIRES_SERVER_NEWER_THAN` gate (matches the feature-delta cross-cutting verdict).

**Negative / accepted**
- A1 changes a handful of shared signatures (`WhenForecastDto` ctor, `CreateForecastDtos`, `Feature.GetLikelhoodForDate`, `Delivery.CalculateMetrics`, `FromDelivery`). Bounded and explicit; mitigated by grep-first + extending the test factory before editing the contract (CLAUDE.md shared-contract rule).
- The translation must be wired at *every* of the six seams; a missed seam is an inconsistency (e.g. portfolio date diverging from manual). Guarded by US-03's cross-surface acceptance test and an ArchUnitNET rule (below).

### Operational note — blackout-period cache freshness (Forge DEVOPS review, 2026-06-05)
The shipped historical-throughput path caches on a date-range key (`BlackoutAwareThroughput_{start}_{end}` in `TeamMetricsService`), NOT on a hash of the blackout-period set. The forward shift reads `GetAll()` live per request, but the *historical* sample it builds on is cached. Consequence: adding a `BlackoutPeriod` mid-day does not retroactively re-strip already-cached throughput windows until the cache entry expires / the service restarts — a forecast computed right after the addition can still sample the un-stripped (slightly optimistic) history for the rest of that cache window. This is **acceptable and out of scope (D1, the cache is shipped/locked)**: blackout periods are planned in advance (weekends, announced shutdowns), so the staleness window is a non-issue in practice. If urgent mid-day additions ever become common, revisit the cache key (period-set revision) as a follow-up — do not widen #4974.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The day↔date translation exists in exactly one place (`BlackoutDaysExtensions.ProjectWorkingDays`/`CountWorkingDays`) — no inline `AddDays`/`(target - Today).Days` on a forecast date at any of the six seams after this feature | NUnit/grep test asserting forecast date projection references the two new helpers; ArchUnitNET test extending the existing suite |
| `ProjectWorkingDays`/`CountWorkingDays` are pure (no I/O) | NUnit static-inspection test: `BlackoutDaysExtensions` references no `IRepository<>`, `DbContext`, `HttpClient`, `ILogger`, or `DateTime.UtcNow`/`Today` inside the two new functions (clock is a parameter) |
| Forecast models (`WhenForecast`, `HowManyForecast`, `Feature`, `Delivery`) acquire NO repository/service dependency | ArchUnitNET test: `Models.Forecast.*` and `Models.{Feature,Delivery}` must not depend on `Services.Interfaces.Repositories` or `Services.Interfaces` (upholds the brief's Models ↛ Repositories invariant) |
| Monte Carlo day-values unchanged | NUnit test asserting `GetProbability(p)`/`GetLikelihood(d)` identical with and without blackout periods (US-01 AC4) |
| D3 roll-forward | NUnit boundary tests: landing on the first/last day of a period, single-day period, back-to-back periods |
| D6 byte-identical | NUnit golden test: empty period list ⇒ `ProjectWorkingDays == AddDays`, `CountWorkingDays == (t-d).Days` |
| Historical-strip × forward-shift no double-count (US-04 AC3) | NUnit compose-guard test: both a past and a future blackout configured ⇒ `GetProbability(p)` equals the historical-strip-only days value |

## Clients consistency verdict

The translation changes the *value* of existing date fields (`ExpectedDate`, write-back date) on existing endpoints — no new route, no new field. **No `FEATURE_REQUIRES_SERVER_NEWER_THAN` gate.** CLI/MCP clients render whatever date the server sends; dates simply become more accurate. (Matches the feature-delta cross-cutting checklist.)

## Premium-gating verdict

`BlackoutPeriod` CRUD and `ComputeBlackoutAwareThroughput` carry **no premium gate** (verified: `GetBlackoutAwareThroughputForTeam` does not call `ILicenseService`). The forward shift therefore **inherits no premium gate** for US-01/02/03 — it activates whenever blackout periods are configured (D2). The **write-back** path (US-04) already sits behind `licenseService.CanUsePremiumFeatures()` in `WriteBackTriggerService.TriggerWriteBackForTeam` (line 34); the shift inherits that existing gate unchanged — no new gate introduced anywhere.
