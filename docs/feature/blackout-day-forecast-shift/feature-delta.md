# Feature Delta — blackout-day-forecast-shift

> Epic **4974 "Blackout Days in Future"** (ADO, reported by Chris). DISCUSS wave (Luna / nw-product-owner).
> Sibling **#4577 "Recurring Blackout Events"** is OUT of scope (recurring rules; this feature handles explicit date-range blackout periods only).
> Density: **lean** (Tier-1 [REF] only). No Tier-2 expansion trigger fired (single bounded context, 2 personas, no compliance/AC-ambiguity/WS-D) → strict lean output.

---

## Wave: DISCUSS / [REF] Pre-requisites & Partial-Implementation Baseline

**This epic is partially shipped.** A March-2026 slab landed on `main` (commits `bfa0203e`, `fa6078b6`, `31b9c9cb`, `b58c2ff3`, `77da3d8c`). Scope decision (user, 2026-06-05): **treat the shipped halves as LOCKED and correct**; this feature delivers only the missing **forward date-shift / working-day translation** layer.

**Already shipped (LOCKED — do not modify, only build on):**
- `BlackoutPeriod` model — explicit `Start`/`End` (`DateOnly`) + `Description` date ranges (non-recurring) and full CRUD (`BlackoutPeriodsController` / `BlackoutPeriodService` / `BlackoutPeriodRepository` / `BlackoutPeriodDto`).
- **Blackout-aware historical throughput** — `TeamMetricsService.GetForecastThroughputStatus` → `ComputeBlackoutAwareThroughput` strips blackout days from the throughput sample (default forecast path). Fixes the epic's "weekend adds zero-throughput days that dilute history" half.
- Backtest samples blackout-aware throughput (`ForecastController.RunBacktest` → `GetBlackoutAwareThroughputForTeam`). The epic's "backtesting should account for blackout days in the observed range" is satisfied here.
- Reusable helpers `BlackoutDaysExtensions` (`GetBlackoutDayIndices`, `IsBlackoutDay`, `HasOverlapWithDateRange`) — **reuse these for the date↔days translation; do not re-derive.**
- Chart blackout overlays (FE `BlackoutOverlay.tsx`) + docs + screenshots.

**Remaining gap = the forward day→date and date→days translation (this feature):** the forecast still produces *days*; turning days into a calendar date (and a target date into a working-day count) is currently blackout-blind. Verified: **zero** blackout references in `Models/Forecast/*`, `WhenForecastDto`, `Delivery.cs`, `Feature.cs`.

| Surface | Current code (blackout-blind) | Direction |
|---|---|---|
| "When" percentile dates | `WhenForecastDto.GetFutureDate` → `Today.AddDays(days)` | days → date |
| "How Many" target date | `HowManyForecast.TargetDate` → `CreationTime.AddDays(days)` | days → date |
| Feature/Delivery percentile dates | `Delivery.cs:102`, `DeliveryWithLikelihoodDto.ExpectedDate` | days → date |
| Likelihood **by a date** | `ForecastController` / `Feature.GetLikelhoodForDate` → `(target - Today).Days` | date → days |
| How-Many **by a date** | `ForecastController` → `HowMany(throughput, calendarDays)` | date → days |
| Write-back date | `WriteBackTriggerService:219` → `Today.AddDays(daysToCompletion)` | days → date |

---

## Wave: DISCUSS / [REF] Persona

**Primary: `delivery-forecaster`** (existing SSOT persona). Owns the leadership conversation about WHEN features ship; cares whether percentile dates are HONEST and defensible. Adds blackout vocabulary below.

**Secondary: `product-owner`** (existing) — reads the same delivery surfaces (US-03) to judge on-track/at-risk; no bespoke build.

---

## Wave: DISCUSS / [REF] JTBD One-Liner

> **`job-forecast-skip-known-nonworking-days`** — When I forecast a delivery and known non-working days (a weekend, a company shutdown) fall between now and the forecast date, I want Lighthouse to translate the forecast's working-days into a calendar date that steps over those days (and never lands on one), so a weekend simply passing doesn't make my forecast look worse and the dates I present are ones work can actually finish on.

- **Functional**: forecast *days* stay identical; the day→date projection skips configured `BlackoutPeriod`s and rolls a landing date forward; target→days counts only working days for likelihood/how-many-by-date.
- **Emotional**: move from "I shared 70% on Friday, it's 55% Monday and nothing changed — I look unreliable" to "the number holds across a known weekend because the model treats it as predictable non-working time."
- **Social**: present percentile dates that never land on a day nothing ships, so stakeholders stop discounting Lighthouse dates.
- **Forces** — *Push*: a passing weekend hits the forecast twice (dilutes history AND shrinks days-remaining); P85 lands on a Saturday. *Pull*: forecast days unchanged, just translated to honest calendar dates; keys off blackout periods already configured. *Anxiety*: "will this silently change the Monte Carlo numbers?" (No — only the date projection). "Will it double-count with the shipped historical stripping?" (Guarded by compose tests). *Habit*: forecasters mentally add "minus the weekend" or hand-shift dates in a spreadsheet.
- **Opportunity**: importance 4, current_satisfaction 1 (forward shift absent), gap 3.

---

## Wave: DISCUSS / [REF] Locked Decisions

- **[D1] Scope = forward date-shift only.** Config + historical-throughput stripping + backtest are shipped and treated as correct/locked. This feature adds the day↔date working-day translation only. (User, 2026-06-05.)
- **[D2] Trigger = configured `BlackoutPeriod`s; no new toggle.** If a team/portfolio has blackout periods, its forecast dates shift automatically. No new per-team enable setting, no new RBAC surface. (User, 2026-06-05.)
- **[D3] A computed date that lands ON a blackout day rolls forward** to the next non-blackout day (epic: "forecast dates avoid landing on weekends"). (User, 2026-06-05.)
- **[D4] Monte Carlo output is unchanged.** `GetProbability` / `GetLikelihood` day-values, the simulation, `Trials`, and the percentile math are NOT touched. Only date projection (`days→date`) and target conversion (`date→workingDays`) change. The epic's "forecasting itself stays the same" is honoured literally.
- **[D5] How-Many "by a date" uses the working-day count** between today and the target (epic's resolved TBD: "jump over those days → run how-many for the working-days count").
- **[D6] No-blackout teams are byte-identical to today.** When no `BlackoutPeriod` overlaps the window, every date and count must equal the pre-feature value (regression guard).
- **[D7] Reuse `BlackoutDaysExtensions`.** The date↔days math builds on the existing `GetBlackoutDayIndices`/`IsBlackoutDay` helpers; DESIGN decides where the forward-projection domain service lives (ports-and-adapters).
- **[D9] Blackout periods are GLOBAL.** `blackoutPeriodRepository.GetAll()` is unscoped — every team/feature/delivery uses the same global set, so the projector/counter need no scope resolution. A multi-team feature still forecasts each team independently (existing logic unchanged); each team's date projection just reads the global periods. (User, 2026-06-05.)
- **[D8] Manual-review gate (user instruction).** Every slice that touches forecasting / forecast-result code gets a per-slice change briefing AND manual user review BEFORE commit. Acceptance + unit tests are non-negotiable; mutation ≥80% on new code.

---

## Wave: DISCUSS / [REF] User Stories (with Elevator Pitches + embedded AC)

### US-01 — "When" percentile dates step over future blackout days
`job_id: job-forecast-skip-known-nonworking-days`

As a delivery-forecaster running a manual "When" forecast for a team with known non-working days ahead, I want the percentile dates to skip those days so the date I commit reflects working effort and lands on a day work can finish.

#### Elevator Pitch
Before: A manual "When" forecast adds raw calendar days, so P85 can land on a Saturday and a passed weekend makes the date drift later though nothing changed.
After: run a manual **When** forecast (Team → Forecast → "When", enter remaining items) for a team with a future `BlackoutPeriod` → sees `P50/70/85/95 dates each stepped past the blackout span, none landing on a blackout day`.
Decision enabled: the forecaster commits a percentile date to stakeholders knowing it counts working days and falls on a working day.

**AC** (verify the After end-to-end):
- AC1: Given the When forecast returns 10 days at P85 and exactly 2 configured blackout days fall within the next 10 calendar days, then the P85 `ExpectedDate` is **12** calendar days from today.
- AC2: Given a percentile date computes onto a date inside a `BlackoutPeriod`, then it is rolled forward to the next non-blackout day (D3).
- AC3: Given a team with no overlapping `BlackoutPeriod`, then `ExpectedDate == Today + days` (unchanged, D6).
- AC4: `forecast.GetProbability(p)` (the days value) is identical with and without blackout periods (D4).

### US-02 — Likelihood & How-Many "by a date" count working days
`job_id: job-forecast-skip-known-nonworking-days`

As a delivery-forecaster asking "how likely / how many by `<date>`", I want known non-working days in the window excluded so the answer isn't distorted by days nothing ships.

#### Elevator Pitch
Before: "how likely by `<date>`?" / "how many by `<date>`?" counts raw calendar days to the target, scoring a weekend in the window as delivery opportunity.
After: run a manual forecast with a **target date spanning a future blackout period** → sees `likelihood and the How-Many item counts computed against the working-day count (blackout days excluded)`.
Decision enabled: the forecaster trusts a by-date likelihood/quantity that isn't inflated or deflated by known non-working days.

**AC**:
- AC1: Given a target date 12 calendar days out with 2 blackout days in between, then likelihood = `GetLikelihood(10)` and how-many = `HowMany(throughput, 10)`.
- AC2: Given no blackout days in the window, then the working-day count equals the calendar-day count (unchanged, D6).
- AC3: Given the target date is today or in the past, then existing guard behaviour (`timeToTargetDate <= 0`) is unchanged.

### US-03 — Feature & Delivery forecast dates and likelihood reflect blackouts
`job_id: job-forecast-skip-known-nonworking-days`

As a delivery-forecaster / product-owner reading a Portfolio → Delivery, I want feature percentile dates and the delivery likelihood to already account for known non-working days so on-track/at-risk reads are honest on the portfolio surface too.

#### Elevator Pitch
Before: portfolio/delivery feature percentile dates and the delivery likelihood-by-target add/count raw calendar days — the same weekend distortion on the portfolio surface.
After: open **Portfolio → Delivery** whose features have future blackout periods → sees `feature percentile dates stepped over the blackouts and the delivery likelihood computed on working days`.
Decision enabled: forecaster/PO reads delivery status (and decides scope cuts) on dates that already account for known non-working days.

**AC**:
- AC1: Feature percentile dates (`HowManyForecast.TargetDate`, `Delivery` expected dates, `DeliveryWithLikelihoodDto.ExpectedDate`) step over blackout periods and roll a landing date forward (D3).
- AC2: `Feature.GetLikelhoodForDate(date)` counts working days between today and `date`.
- AC3: Features/deliveries with no overlapping blackout period are unchanged (D6).

### US-04 — Forecast write-back date reflects the shift
`job_id: job-forecast-skip-known-nonworking-days`

As a delivery-forecaster who writes the forecasted completion date back to Jira/ADO, I want the written date to be the blackout-shifted date so the tool of record shows the same honest date Lighthouse does.

#### Elevator Pitch
Before: forecast write-back writes the raw calendar date (`Today.AddDays(daysToCompletion)`), re-introducing the weekend distortion in the work-tracking tool.
After: trigger a forecast write-back for a team with future blackout periods → sees `the blackout-shifted working-day date written to the work item field in Jira/ADO`.
Decision enabled: stakeholders reading the date in Jira/ADO see the same blackout-aware date as in Lighthouse.

**AC**:
- AC1: The written date equals the US-01 shifted date for the same percentile/team.
- AC2: A team with no blackout periods writes back the same date as today (D6).
- AC3 (compose guard, cross-slice): regression tests assert the shipped historical-throughput stripping and the new forward-shift **do not double-count** a blackout day (a blackout day is never both removed from the sample AND added twice to the projection in a way that changes the days value).

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Forecast date stability across a known weekend | 100% of Fri-vs-Mon pairs unchanged (no flow change, weekend configured as blackout) | Integration test pinning the clock to Fri then Mon; assert P50/70/85/95 calendar dates equal |
| Percentile dates landing on a blackout day | 0 | Assertion across all forecast surfaces in integration tests |
| No-blackout regression | 100% byte-identical dates/counts vs pre-feature | Golden tests for teams without blackout periods (D6) |
| Mutation kill rate on new shift code | ≥ 80% | Stryker.NET feature-scoped run |

---

## Wave: DISCUSS / [REF] Cross-Cutting Impact Checklist

- **RBAC** — **N/A, because** the shift changes WHAT date/count a forecast yields, not WHO may access it. No new endpoint (keys off existing `BlackoutPeriod`s, D2); no `IRbacAdministrationService` interaction; forecast endpoints keep their current `TeamRead`/`PortfolioWrite` guards; `useRbac()` gating untouched. (DESIGN to confirm whether `BlackoutPeriod` itself is premium-gated and whether the shift inherits that gate — no premium gate observed in `ComputeBlackoutAwareThroughput`.)
- **Lighthouse-Clients (CLI + MCP)** — **N/A (transparent), because** the change alters the *value* of existing date fields (`ExpectedDate`, write-back date) on existing endpoints, not the contract. No new/changed endpoint, no new field → clients render whatever date the server sends; no `FEATURE_REQUIRES_SERVER_NEWER_THAN` gate needed. Dates simply become more accurate.
- **Website** — **N/A for a marketing change**, because no new screen/capability is surfaced; the blackout-config feature already has public docs. **BUT** strong release-notes story (stakeholder-trust angle: "a passing weekend no longer makes your forecast look worse") — flag for `/release-notes`, tag the epic.

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy C — brownfield, no walking skeleton.** The day↔date translation is an isolated layer on top of shipped config/throughput; each slice is an end-to-end thin vertical on an existing surface. No new integration point. (Decision 2 = "No / isolated".)

---

## Wave: DISCUSS / [REF] Driving Ports (inbound surfaces touched)

- `POST /api/{v1|latest}/forecast/manual/{id}` — When + likelihood-by-date + how-many-by-date (US-01, US-02)
- `POST /api/{v1|latest}/forecast/itemprediction/{id}` — how-many-by-date (US-02)
- Portfolio/Delivery read surfaces (`DeliveryWithLikelihoodDto`, delivery metrics) (US-03)
- Forecast write-back to work-tracking system (`WriteBackTriggerService`) (US-04)
- No new endpoint. No new FE screen — existing date displays render the shifted values.

---

## Wave: DISCUSS / [REF] Out of Scope

- Recurring blackout *rules* (every weekend / every holiday) → **#4577**.
- Any change to the Monte Carlo simulation, `Trials`, percentile math, or the day-values (D4).
- Re-verifying or modifying the shipped historical-throughput stripping or backtest (D1 — locked).
- A per-team enable toggle / new settings field / new RBAC surface (D2).
- New FE charts or blackout visualisations beyond the existing overlays.
- Time-zone redesign (use the existing `DateTime.UtcNow.Date` convention already in the forecast path).

---

## Wave: DISCUSS / [REF] Definition of Done

1. All 4 stories' AC pass via acceptance (port-to-port) tests.
2. Unit tests cover the day→date shift, the date→working-days count, the roll-forward-on-landing rule, and the no-blackout regression (D6).
3. Compose guard test: historical-strip × forward-shift no double-count (US-04 AC3).
4. Mutation ≥80% on new shift code.
5. `dotnet build` zero warnings; `dotnet test` green; SonarCloud new-violations = 0 (consult `docs/ci-learnings.md` first).
6. Each forecasting/forecast-result code change **briefed to and manually reviewed by the user before commit** (D8).
7. ADO Epic 4974 children created/transitioned; pause before push.
8. Release-notes flag recorded (cross-cutting).
9. SSOT updated: job in `jobs.yaml`, journey yaml, persona job-ref.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | User story format + persona | ✅ | 4 stories, `delivery-forecaster`/`product-owner` |
| 2 | Job traceability | ✅ | all → `job-forecast-skip-known-nonworking-days` |
| 3 | Elevator pitch (real entry point + observable output) | ✅ | per story, real endpoints/UI actions |
| 4 | Testable AC | ✅ | numeric AC (10→12 days etc.), no ambiguity |
| 5 | Outcome KPIs w/ targets | ✅ | KPI table above |
| 6 | Dependencies known | ✅ | shipped config/throughput (locked); `BlackoutDaysExtensions` reuse |
| 7 | Technical notes / cross-cutting | ✅ | RBAC/clients/website all explicit; partial-impl baseline; D1–D8 |
| 8 | Sized / sliceable | ✅ | 4 thin slices, 1 surface each (see slice briefs) |
| 9 | Out-of-scope explicit | ✅ | section above |

Requirements completeness: **0.97**.

---

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone: **(A) days→date projection** · **(B) date→working-days conversion** · **(C) consistency across surfaces**.

Thinnest-first (Decision: one surface end-to-end first). One slice = one story = one demoable surface. Each slice carries the D8 manual-review gate.

| Slice | Story | Surface | Direction | Learning hypothesis |
|---|---|---|---|---|
| 01 | US-01 | Manual "When" percentile dates | days→date | Disproves "a single shared working-day projector serves the date path without touching the Monte Carlo days" if P-dates don't move by exactly the blackout-day count |
| 02 | US-02 | Likelihood + How-Many by a date | date→days | Disproves "working-day counting composes with the existing `timeToTargetDate<=0` guard" if a spanning target mis-scores |
| 03 | US-03 | Feature & Delivery dates + likelihood | both | Disproves "the same projector/counter drop into the feature & delivery paths unchanged" if portfolio dates diverge from manual |
| 04 | US-04 | Forecast write-back date + compose guard | days→date | Disproves "historical-strip and forward-shift compose without double-counting" if the days value moves |

Slice briefs: `docs/feature/blackout-day-forecast-shift/slices/slice-0{1..4}-*.md`.

Prioritisation rationale: Slice 01 first = it births the shared projector on the smallest surface (highest learning leverage, smallest review surface for D8). 02 adds the inverse direction on the same controller. 03 fans the two proven primitives across feature/delivery. 04 (write-back + compose guard) last — lowest visibility, depends on the projector being settled.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

- Feature type: **Backend** (core forecasting; dates computed server-side, FE renders unchanged).
- JTBD: **Yes** (default) — user-facing forecaster value.
- UX research depth: **Lightweight** — no new screens; existing date displays show shifted values.
- Walking skeleton: **No** (Strategy C — isolated brownfield layer).
- Primary need: forecast dates that step over known non-working days and never land on one, without altering the Monte Carlo days.
- Constraints: D4 (no Monte Carlo change), D6 (no-blackout regression byte-identical), D8 (per-slice briefing + manual review before commit).
- Upstream changes: none (no DISCOVER/DIVERGE artifacts existed; SSOT bootstrapped with new job + journey).

**Handoff → DESIGN (nw-solution-architect)** + **DEVOPS (KPIs only)**. DESIGN to place the working-day projector domain service (ports-and-adapters, reading the global blackout periods per D9), confirm premium-gating inheritance, and resolve the write-back composition.

---

## Wave: DESIGN / [REF] DDD List

Architect: Morgan · Mode: PROPOSE · Date: 2026-06-05 · Density: lean (Tier-1 [REF]).

- **DDD-1** — The day↔date blackout translation is **two pure functions on the existing static `BlackoutDaysExtensions`** (`ProjectWorkingDays`, `CountWorkingDays`), NOT a new injectable service and NOT logic inside the forecast models. (ADR-058, Option A.)
- **DDD-2** — The global blackout periods (D9, `GetAll()`) are fetched **once per inbound request** in the **DI-aware assembly layer** (`ForecastController`, `DeliveryWithLikelihoodDto.FromDelivery`/`DeliveriesController`, `WriteBackTriggerService`) and **passed inward** as a materialised `IReadOnlyList<BlackoutPeriod>`. Models never fetch. (Mirrors the shipped `GetBlackoutAwareThroughputForTeam` fetch-once pattern.)
- **DDD-3** — Forecast models/DTOs (`WhenForecastDto`, `HowManyForecast`, `Feature`, `Delivery`) receive the periods as a **method/ctor parameter** (recommended A1) — they acquire NO repository/service dependency (upholds Models ↛ Repositories). The **pivotal decision** the human confirms is A1 (pass `IReadOnlyList<BlackoutPeriod>`) vs A2 (pass a pre-bound projection delegate).
- **DDD-4** — D6 byte-identical is a **property of the math, not a branch**: empty period list ⇒ `ProjectWorkingDays == AddDays` and `CountWorkingDays == (t-d).Days`.
- **DDD-5** — D4 untouched: the Monte Carlo, `GetProbability`, `GetLikelihood`, `HowMany` are not edited; only their date *inputs* (date→days) and date *outputs* (days→date) are wrapped at the assembly layer.
- **DDD-6** — Historical-strip (SAMPLE/past) and forward-shift (DATE/future) are orthogonal by construction — opposite sides of "today", never the same day. Pinned by the US-04 AC3 compose-guard test. (ADR-058 orthogonality argument.)
- **DDD-7** — No premium gate for US-01/02/03 (none on `BlackoutPeriod`/throughput stripping); US-04 inherits the existing `CanUsePremiumFeatures()` write-back gate unchanged.

## Wave: DESIGN / [REF] Component Decomposition

| Component | File | Change Type | Change (story) |
|---|---|---|---|
| `BlackoutDaysExtensions` | `Services/Implementation/BlackoutDaysExtensions.cs` | **EXTEND** | Add pure `ProjectWorkingDays(periods, start, workingDayCount)` (D3 roll-forward) + `CountWorkingDays(periods, start, target)`; reuse `GetBlackoutDayIndices`/`IsBlackoutDay` (US-01..04) |
| `WhenForecastDto` | `API/DTO/WhenForecastDto.cs` | **EXTEND** | `GetFutureDate` → `periods.ProjectWorkingDays(Today, days)`; ctor/`CreateForecastDtos` take periods (US-01) |
| `DtoExtensions.CreateForecastDtos` | `API/DTO/DtoExtensions.cs` | **EXTEND** | Thread `periods` into the `WhenForecastDto` factory (US-01/03) |
| `ForecastController` | `API/ForecastController.cs` | **EXTEND** | Fetch periods once; `(target-Today).Days` → `CountWorkingDays` at lines ~57/80/93/103; pass periods to When DTOs (US-01/02) |
| `HowManyForecast` | `Models/Forecast/HowManyForecast.cs` | **EXTEND** | `TargetDate` projected over periods passed in (US-03) |
| `Feature` | `Models/Feature.cs` | **EXTEND** | `GetLikelhoodForDate(date, periods)` → `CountWorkingDays` (US-03) |
| `Delivery` | `Models/Delivery.cs` | **EXTEND** | `CalculateMetrics(periods, percentiles)`; `ToWhenPercentile` projects over periods; line 102 `AddDays` → `ProjectWorkingDays` (US-03) |
| `DeliveryWithLikelihoodDto` | `API/DTO/DeliveryWithLikelihoodDto.cs` | **EXTEND** | `FromDelivery(delivery, periods)`; thread periods into metrics + feature likelihoods (US-03) |
| `DeliveriesController` (caller of `FromDelivery`) | `API/DeliveriesController.cs` | **EXTEND** | Fetch periods once, pass to `FromDelivery` (US-03) |
| `WriteBackTriggerService` | `Services/Implementation/WriteBackTriggerService.cs` | **EXTEND** | Inject `IRepository<BlackoutPeriod>`; fetch once per `TriggerWriteBackForTeam`; line 226 `AddDays` → `ProjectWorkingDays` (US-04) |
| ArchUnitNET suite + NUnit tests | `Lighthouse.Backend.Tests/...` | **EXTEND** | Purity rule, Models ↛ Repositories rule, single-home rule, D3/D4/D6 + compose-guard tests (ADR-058) |
| *(no CREATE NEW production component)* | — | — | The translation reuses the existing helper class + existing assembly seams (D7) |

## Wave: DESIGN / [REF] Driving Ports

No new inbound surface. Existing endpoints carry shifted values (per DISCUSS Driving Ports):
- `POST /api/{v1|latest}/forecast/manual/{id}` (US-01/02), `POST .../forecast/itemprediction/{id}` (US-02), Portfolio/Delivery reads via `DeliveriesController`→`DeliveryWithLikelihoodDto` (US-03), write-back trigger (US-04). Existing `TeamRead`/`PortfolioRead`/`PortfolioWrite` RBAC guards unchanged.

## Wave: DESIGN / [REF] Driven Ports + Adapters

- **Reused as-is**: `IRepository<BlackoutPeriod>` (the only outbound dependency; `GetAll()` is global, D9). Already injected in `TeamMetricsService`; **newly injected in `WriteBackTriggerService`** (US-04). `ForecastController`/`DeliveriesController` source periods via repository (directly or via a thin `ITeamMetricsService` accessor).
- **No new driven port, no new adapter.** The translation primitives are pure (no I/O, clock is a parameter) → no probe contract needed (no external substrate is touched; the period set is in-process data from the existing repo).

## Wave: DESIGN / [REF] Technology Choices

ASP.NET Core .NET 8 (OOP, ports-and-adapters); NUnit 4.6 + Moq + EF InMemory + WebApplicationFactory; Stryker.NET ≥80% (D8). TngTech.ArchUnitNET (existing suite, Apache 2.0). **No new library, no new endpoint, no EF migration, no DI registration** (Option A statics). Reuse `BlackoutDaysExtensions` (D7).

## Wave: DESIGN / [REF] Decisions table

| # | Decision | Verdict | Source |
|---|---|---|---|
| DC-1 | Where the two translation primitives live | Pure statics on `BlackoutDaysExtensions` (Option A) — not a service (C), not in models (B) | ADR-058 |
| DC-2 | How periods reach the no-DI models | **LOCKED — A1** (pass `IReadOnlyList<BlackoutPeriod>` param). User-confirmed 2026-06-05. | ADR-058 |
| DC-3 | Where periods are fetched | Once per request in the DI assembly layer; passed inward (D9 single fetch, no N+1) | ADR-058 |
| DC-4 | Orthogonality vs shipped stripping | Disjoint by construction (past sample vs future date); compose-guard test pins it | ADR-058 / US-04 AC3 |
| DC-5 | Premium gating | None for US-01/02/03; US-04 inherits existing write-back gate | ADR-058 |

## Wave: DESIGN / [REF] Reuse Analysis

| Overlapping component | Verdict | Justification |
|---|---|---|
| `BlackoutDaysExtensions` (`GetBlackoutDayIndices`/`IsBlackoutDay`) | **EXTEND** | D7 mandates reuse; the two new pure functions belong beside the shipped blackout math (single home) |
| `IRepository<BlackoutPeriod>` / `GetAll()` | **REUSE AS-IS** | Global set (D9); fetch-once pattern already proven in `GetBlackoutAwareThroughputForTeam` |
| `WhenForecastDto` / `DtoExtensions.CreateForecastDtos` | **EXTEND** | Existing When-DTO factory; thread periods through rather than fork a parallel DTO |
| `Feature.GetLikelhoodForDate` / `Delivery.CalculateMetrics` / `DeliveryWithLikelihoodDto.FromDelivery` | **EXTEND** | Existing projection path; add a periods parameter (A1) — no parallel projection |
| `ForecastController` / `DeliveriesController` / `WriteBackTriggerService` | **EXTEND** | Existing assembly seams; add the fetch + thread, no new controller/service |
| `ForecastService` / `ForecastBase` / Monte Carlo | **REUSE AS-IS (untouched)** | D4 — day-values and simulation must not change |
| `TeamMetricsService` blackout-aware throughput | **REUSE AS-IS (untouched)** | D1 — shipped/locked; orthogonal (DDD-6) |
| ArchUnitNET suite + NUnit test classes | **EXTEND** | Add ADR-058 rules/tests to the existing suite (no new test project) |
| *new `IWorkingDayProjector` service* | **CREATE NEW — REJECTED** | Over-engineered for pure functions with no collaborators (ADR-058 Option C); would fork the blackout math across two homes |

Default = EXTEND honoured everywhere; the only CREATE-NEW candidate (a projector service) is explicitly rejected.

## Wave: DESIGN / [REF] Open Questions

- **OQ-1 — RESOLVED (user, 2026-06-05): A1** (pass `IReadOnlyList<BlackoutPeriod>` parameter). Sets the shared-contract signatures for all four slices. A2 (pre-bound delegate) rejected.
- **OQ-2 (DELIVER, low risk)**: in `ForecastController`/`DeliveriesController`, source periods via `IRepository<BlackoutPeriod>` directly or via a thin `ITeamMetricsService` accessor? Either honours DDD-2; pick whichever keeps the controller ctor leaner. Non-blocking.

---

## Wave: DISTILL / [REF] Scenario list with tags

Designer: Quinn (nw-acceptance-designer) · Date: 2026-06-05 · Density: lean (Tier-1 [REF]). Stack: NUnit 4.6 + Moq + EF-InMemory(Sqlite) + `WebApplicationFactory<Program>` (NO `.feature`/SpecFlow, NO FsCheck, NO PBT — per `docs/architecture/atdd-infrastructure-policy.md`). Skip marker: `[Ignore("pending DELIVER — Epic 4974 US-0X ...")]`. 14 story scenarios + 2 ArchUnit guards. Error/edge ratio = 8/14 ≈ 57% (≥40% target met: no-blackout regression ×4, past-target guard ×1, roll-forward boundary ×2, compose-guard ×1).

| # | Scenario | Story | Tags | Today |
|---|---|---|---|---|
| 1 | When percentile date steps over a 2-day future blackout (`Today+12`) | US-01 | `@US-01 @real-io @driving_port` | RED |
| 2 | Percentile date landing on a blackout day rolls forward (D3) | US-01 | `@US-01 @real-io @error` | RED |
| 2b | Percentile date landing on the FIRST day of a 4-day consecutive blackout rolls forward past the WHOLE span (D3, multi-day, +4 not +1) | US-01 | `@US-01 @real-io @error` | RED |
| 3 | No-blackout team ⇒ date == `Today+days` (D6 byte-identical) | US-01 | `@US-01 @real-io @error` | GREEN guard |
| 4 | By-date how-many scored on the working-day count (10, not 12) | US-02 | `@US-02 @real-io @driving_port` | RED |
| 5 | By-date likelihood scored on the working-day count | US-02 | `@US-02 @real-io` | GREEN guard |
| 6 | No-blackout window ⇒ working-day count == calendar-day count (D6) | US-02 | `@US-02 @real-io @error` | GREEN guard |
| 7 | Target date today/past ⇒ existing `timeToTargetDate<=0` guard unchanged | US-02 | `@US-02 @real-io @error` | GREEN guard |
| 8 | Feature percentile date steps over blackouts on the delivery read surface | US-03 | `@US-03 @real-io @driving_port` | RED |
| 9 | No-blackout feature/delivery ⇒ date unchanged (D6) | US-03 | `@US-03 @real-io @error` | GREEN guard |
| 10 | Multi-team feature still forecast per-team, worst-case date shifted (D9) | US-03 | `@US-03 @real-io @error` | RED |
| 11 | Write-back writes the blackout-shifted date | US-04 | `@US-04 @error` | RED |
| 12 | No-blackout team writes back the unchanged date (D6, AC2) | US-04 | `@US-04 @error` | GREEN guard |
| 13 | Historical × forward-shift compose guard — days unchanged, date shifted once (AC3) | US-04 | `@US-04 @error @property` | RED |
| A1 | `Models.Forecast.*` ↛ repositories (ADR-058 A1) | ADR-058 | `@arch-unit` | GREEN guard |
| A2 | `Feature`/`Delivery` ↛ repositories (Models ↛ Repositories invariant) | ADR-058 | `@arch-unit` | GREEN guard |

## Wave: DISTILL / [REF] WS strategy

**No walking skeleton** (inherited from DISCUSS WS Strategy C — brownfield, isolated day↔date translation layer on top of shipped config/throughput). Each slice is an end-to-end thin vertical on an existing endpoint; no new integration point to prove. The four `@driving_port` scenarios (one per inbound surface) close the end-to-end loop through the production composition root in lieu of a skeleton.

## Wave: DISTILL / [REF] Architecture-of-Reference port treatment

| Port | Class | Treatment | Mechanism |
|---|---|---|---|
| HTTP API (`/forecast/manual`, `/deliveries/portfolio`) | Driving | Real adapter | `WebApplicationFactory<Program>` + `WithTestAuthentication` (`AsTeamViewer` / `AsPortfolioViewer`) |
| EF repositories (`IRepository<BlackoutPeriod|Team|Portfolio|Feature>`) | Driven internal | Real adapter | Real EF Sqlite via the test factory; seeded through repos |
| `IForecastService` (Monte Carlo) | Driven non-deterministic | Fake/capture | `Mock<IForecastService>` wired via `RemoveAll`+`AddScoped`; days value pinned (mass on day 10) — isolates the date transform from the simulation (D4) |
| `ITeamMetricsService` (settings-coupled construction) | Driven internal (settings-dependent) | Fake (for the When/HowMany controller path only) | `Mock<ITeamMetricsService>.GetForecastThroughputStatus` returns a valid empty status |
| `IWriteBackService` (Jira/ADO field write) | Driven external | Fake/capture | `Mock<IWriteBackService>` captures `WriteBackFieldUpdate.Value` (US-04) |

New policy specialization recorded this run: `IForecastService` as a determinism fake (appended to `atdd-infrastructure-policy.md`). All other treatments inherit the existing project policy.

## Wave: DISTILL / [REF] Driving-port coverage

| Inbound surface | Story | Covering scenario(s) |
|---|---|---|
| `POST /api/latest/forecast/manual/{id}` (When) | US-01 | #1, #2, #3 |
| `POST /api/latest/forecast/manual/{id}` (by-date how-many + likelihood) | US-02 | #4, #5, #6, #7 |
| `GET /api/latest/deliveries/portfolio/{id}` (delivery read) | US-03 | #8, #9, #10 |
| Forecast write-back (`WriteBackTriggerService` → `IWriteBackService` seam) | US-04 | #11, #12, #13 |

Every endpoint named in DISCUSS/DESIGN Driving Ports has ≥1 integration scenario. `/forecast/itemprediction/{id}` shares the day→date / how-many path with `/forecast/manual`; it is covered transitively by the same `WhenForecastDto`/`HowManyForecast` projection under test (US-02) — no bespoke endpoint behaviour differs for the shift.

## Wave: DISTILL / [REF] Test placement

| File | Dir | Precedent |
|---|---|---|
| `BlackoutForecastShiftTestBase.cs` | `Tests/API/Integration/` | `ForecastFilterTeamSettingsIntegrationTest` (own-factory `WithWebHostBuilder` + `RemoveAll`/`AddScoped` + DB seed) |
| `BlackoutForecastShiftTeamForecastIntegrationTest.cs` (US-01) | `Tests/API/Integration/` | `ForecastFilterTeamForecastIntegrationTest` (manual When forecast) |
| `BlackoutForecastShiftItemPredictionIntegrationTest.cs` (US-02) | `Tests/API/Integration/` | `ForecastFilterTeamForecastIntegrationTest` (HowMany by-date) |
| `BlackoutForecastShiftDeliveryIntegrationTest.cs` (US-03) | `Tests/API/Integration/` | `DeliveriesControllerIntegrationTest` (delivery create+read via `IntegrationTestBase`) |
| `BlackoutForecastShiftWriteBackTest.cs` (US-04) | `Tests/Services/Implementation/` | service-level Moq test (write-back has no honest port-to-port observable) |
| `BlackoutForecastShiftSeamArchUnitTest.cs` (ADR-058) | `Tests/Architecture/` | `ForecastFilterSeamArchUnitTest` (ArchUnitNET seam guard) |

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports (no new endpoint; shifted values on existing routes) — `feature-delta.md` DESIGN Driving Ports.
- ADR-058 (A1 locked): translation is two pure functions on `BlackoutDaysExtensions`, periods threaded as `IReadOnlyList<BlackoutPeriod>` parameter; Models ↛ Repositories.
- Shipped+locked: `BlackoutPeriod` CRUD + repo (`IRepository<BlackoutPeriod>.GetAll()` global, D9), `BlackoutDaysExtensions` helpers, blackout-aware historical throughput.
- DEVOPS: no separate env matrix; defaults (clean EF Sqlite per `[SetUp]`). KPI contract `OUT-blackout-forecast-shift` appended to `kpi-contracts.yaml`.

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DISCUSS#D3 | A computed date landing on a blackout day rolls forward to the next non-blackout day | n/a | Scenario #2 asserts single-day roll-forward (`Today+11`); scenario #2b asserts a landing on the FIRST day of a 4-day consecutive blackout rolls forward past the WHOLE span (`Today+14`, +4 not +1) — guards multi-day shutdowns; a missed roll-forward ships a date work cannot finish on |
| DISCUSS#D4 | Monte Carlo days value (`GetProbability`) unchanged with/without periods | DDD-5 | Determinism fake pins the days value; compose-guard #13 asserts `GetProbability(85)` unchanged — proves the date transform never touches the simulation |
| DISCUSS#D6 | No-blackout teams byte-identical to pre-feature | DDD-4 | Four regression guards (#3, #6, #9, #12) GREEN today and after — a regression here silently changes every existing customer's dates |
| DISCUSS#D9 | Blackout periods are GLOBAL; multi-team feature still forecasts per-team | n/a | Scenario #10 seeds a 2-team feature and asserts the worst-case team's date is shifted, not the forecasting fanned differently |
| DESIGN#DC-2 | A1 — periods threaded as `IReadOnlyList<BlackoutPeriod>` parameter, never a repo dependency in a model | ADR-058 | ArchUnit guards A1/A2 enforce Models ↛ Repositories; a model gaining `IRepository<BlackoutPeriod>` reds the guard |
| DESIGN#DC-4 | Historical-strip × forward-shift are orthogonal — no double-count | ADR-058 / US-04 AC3 | Compose-guard #13 pins it empirically (both blackouts configured ⇒ days unchanged, date shifted exactly once) |
| DISCUSS#D8 | Per-slice manual-review gate before any forecasting-code commit | n/a | DISTILL wrote ONLY test code; no production forecasting change. DELIVER un-ignores one slice at a time behind the review gate |

---

## Wave: DELIVER / [WHY] Upstream Issues

### UI-1 — Item-creation prediction history is NOT blackout-aware (asymmetry surfaced in Slice 02)

**Found:** 2026-06-06, during Slice 02 (US-02) D8 review (user-raised).

**Issue.** The shipped blackout-aware historical stripping (`ComputeBlackoutAwareThroughput` → `GetBlackoutAwareThroughputForTeam`) is applied **only to completed-item throughput** — the `When` path, the manual how-many-by-date path (via `GetForecastThroughputStatus`), backtest, and the predictability score. The **item-creation prediction** endpoint (`POST /forecast/itemprediction/{id}` → `ForecastService.PredictWorkItemCreation` → `TeamMetricsService.GetCreatedItemsForTeam` → `GenerateCreationRunChart`) samples **raw calendar creation dates** with blackout days included as zero-creation days. DESIGN (DDD/DC tables, DISTILL driving-port coverage) assumed `/itemprediction` "shares the day→date / how-many path … no bespoke endpoint behaviour differs" — but it overlooked that the *history sample* on that path is not blackout-aware.

**Consequence avoided.** Roadmap step 02-02 initially applied `CountWorkingDays` to *both* the manual how-many path and the item-creation path. The item-creation path would then run a **working-day horizon over a calendar-day (blackout-diluted) creation rate** → systematic **under-prediction**, and it sits outside the US-02 acceptance test (which exercises `/forecast/manual`, not `/itemprediction`).

**Resolution (user decision, 2026-06-06).** Revert the `RunItemCreationPrediction` change; `/itemprediction` keeps calendar-day horizon (byte-identical to pre-feature, internally consistent: calendar rate × calendar horizon). `CountWorkingDays` stays only on the tested, consistent manual how-many path.

**Deferred follow-up (out of scope here).** Making item-creation truly blackout-aware requires stripping blackout days from `GetCreatedItemsForTeam` / `GenerateCreationRunChart` — a change inside the **D1-locked** throughput-stripping area — plus a new acceptance test hitting `/forecast/itemprediction/{id}`. Track as a separate scoped item (candidate ADO Story under Epic 4974 or sibling #4577) if item-creation by-date accuracy around shutdowns is wanted.

### UI-2 — Backtest forecast horizon was calendar days while its sample was blackout-aware (RESOLVED in-feature, Slice 05)

**Found:** 2026-06-06, during the manual-verification review (user-raised: "is backtesting also affected by the blackout days?").

**Issue.** `ForecastController.RunBacktest` sampled a **blackout-aware** historical throughput (`GetBlackoutAwareThroughputForTeam`, working-day rate) but simulated `HowMany` over a **raw calendar-day** horizon (`forecastDays = EndDate − StartDate`). For a backtest window containing blackout days, the forecast side drew calendar-many days at the working-day rate while the actual side (`GetThroughputForTeam`) naturally had zeros on those days → the forecast systematically **over-predicted** vs actual. Same asymmetry class as UI-1, on the opposite (backtest) surface.

**Resolution (user authorized 2026-06-06, Slice 05 / step 05-01).** `forecastDays` now = `blackoutPeriods.CountWorkingDays(periodStart, periodEnd)` — the working-day span of the window — so the horizon matches the blackout-aware sample. The ≥14-day window validation stays on calendar days (it guards window size); the Monte Carlo and the shipped historical-stripping are untouched (D4). No-blackout windows are byte-identical (D6).

**Scope deviation note.** Backtest was declared D1-LOCKED in DISCUSS. This change touches it deliberately, on explicit user request after manual verification surfaced the inconsistency. It does NOT modify the locked historical-stripping itself — only the horizon value the controller feeds to `HowMany`.
