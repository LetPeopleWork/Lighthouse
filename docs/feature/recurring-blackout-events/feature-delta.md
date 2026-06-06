# Feature Delta — recurring-blackout-events

> Epic **4577 "Recurring Blackout Events"** (ADO; State: Planned; Tags: Premium, Productboard, Release Notes; via Productboard). DISCUSS wave (Luna / nw-product-owner).
> **Sibling of the SHIPPED #4974 "Blackout Days in Future"** (`blackout-day-forecast-shift`). The one-off `BlackoutPeriod` (Start/End date range) CRUD, the premium+SystemAdmin guard pattern, the `BlackoutPeriodsSettings.tsx` settings UI, and the `BlackoutDaysExtensions` evaluation seam are shipped and **LOCKED**. This feature adds **recurrence**.
> Density: **lean** (Tier-1 [REF] only). Expansion mode: **ask-intelligent** — no Tier-2 expansion trigger fired (single bounded context = blackout configuration; 2 personas; no regulatory/compliance surface; AC are numerically unambiguous; WS = D/no-skeleton brownfield) → strict lean output, no expansion menu emitted.

---

## Wave: DISCUSS / [REF] Pre-requisites & Brownfield Baseline

This feature **extends shipped, locked infrastructure** (Decision D2). The one-off blackout-period stack landed and shipped; #4974's forward day↔date forecast shift consumes it. Recurring rules add a new entity that feeds the **same** blackout-day evaluation.

**Shipped & LOCKED (reuse, do not modify):**

| Surface | File | Reuse for recurring rules |
|---|---|---|
| One-off model | `Models/BlackoutPeriod.cs` (`{ int Id; DateOnly Start; DateOnly End; string Description; }`, `IEntity`) | Shape template for the new `RecurringBlackoutRule` entity |
| One-off DTO + service | `Models/BlackoutPeriodDto.cs`, `Services/Implementation/BlackoutPeriodService.cs` (CRUD + `ValidateDateRange`), `IBlackoutPeriodService` | CRUD + validation pattern to mirror |
| One-off controller | `API/BlackoutPeriodsController.cs` (`api/v1` + `api/latest/blackout-periods`); GET open, POST/PUT/DELETE `[LicenseGuard(RequirePremium=true)]` + `[RbacGuard(SystemAdmin)]` | Guard + route pattern to mirror on the new controller |
| Evaluation seam | `Services/Implementation/BlackoutDaysExtensions.cs` — `IsBlackoutDay`, `GetBlackoutDayIndices`, `HasOverlapWithDateRange`, `AnnotateBlackoutDays`, and the #4974 `ProjectWorkingDays`/`CountWorkingDays` (take `IEnumerable<BlackoutPeriod>`) | **The unified-evaluation seam (D4): recurring rules must feed the SAME indices/IsBlackoutDay results** |
| Forecasting consumers | `ForecastController`, `DeliveriesController`, `WriteBackTriggerService`, `TeamMetricsService` | Already blackout-aware via #4974; widen the day SOURCE to include recurring days |
| FE settings | `pages/Settings/System/BlackoutPeriodsSettings.tsx`, `models/BlackoutPeriod.ts`, `services/Api/BlackoutPeriodService.ts` | Settings-section + dialog + `data-testid` pattern to mirror |
| Chart overlays | `BlackoutOverlay.tsx` / `PbcBlackoutOverlay.tsx` / `TimeBlackoutOverlay.tsx` | Consume recurring days unchanged (D4) |

**New in this feature:** a `RecurringBlackoutRule` entity (weekday set + every-X-weeks interval + start date + optional open-ended end), its CRUD endpoint family, a recurring-rules settings section, and the **rule → concrete-days expansion** that unifies into the existing evaluation. The expansion is the only genuinely new logic.

---

## Wave: DISCUSS / [REF] Persona

**Primary: `config-admin`** (existing SSOT persona; the system-admin who authors rules). PRIMARY actor for every story. Adds the recurring-rule vocabulary via the new job.

**Secondary / beneficiary: `delivery-forecaster`** (existing SSOT persona) — gains accurate forecasts that skip recurring non-working days (e.g. weekends) without hand-maintaining one-off periods. Realises the downstream value through the existing job `job-forecast-skip-known-nonworking-days`; no bespoke build for this persona.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

> **`job-config-admin-define-recurring-blackout-rule`** (NEW, config-admin) — When the same non-working days repeat on a predictable cadence (every weekend, or a team off-site every 4th Friday) and today I must hand-enter each one as a separate one-off blackout period forever, I want to define the pattern ONCE as a recurring rule — which weekdays, how often, when it starts, optionally when it ends or open-ended to run forever — so every matching day is automatically treated as a non-working day in forecasts and on the charts, without maintaining an ever-growing list.

> **`job-forecast-skip-known-nonworking-days`** (EXISTING, delivery-forecaster, downstream value) — the recurring rule's concrete days flow into the shipped #4974 day↔date shift, so the forecaster's percentile dates step over recurring weekends/off-sites and never land on one, with zero new forecaster setup.

- **Functional**: new entity (weekdays + interval + start + optional end) → CRUD via a new endpoint → **expands to concrete days feeding the SAME `IsBlackoutDay`/`GetBlackoutDayIndices`** the one-off periods feed (D4). Covers "weekends forever" (Sat+Sun, every 1 wk, no end) AND "off-site every 4th Friday" (Fri, every 4 wks, bounded).
- **Emotional**: from "I dread the blackout screen — I keep hand-adding weekends and it never ends" → "I described the weekend once; Lighthouse keeps it correct forever."
- **Social**: be the admin whose forecasts are honest about recurring non-working time without anyone babysitting a list.
- **Forces** — *Push*: excluding weekends today = a one-off period per weekend forever, so nobody does it and Saturdays count as working days. *Pull*: calendar-style recurrence everyone understands, entered once, reusing the proven CRUD/guard/settings pattern and plugging into already-shipped blackout-aware forecasting. *Anxiety*: "will a recurring day behave differently downstream?" (No — D4 unified eval). "Will empty end break anything?" (No — empty = forever). "Will it change the Monte Carlo?" (No — only adds days to the set #4974 already consumes). *Habit*: admins already manage one-off periods on this exact screen; recurrence is the natural "make it repeat" extension.
- **Opportunity**: importance 4, current_satisfaction 1, gap 3 (weekends effectively un-excludable today; high-leverage, kept thin by reuse).

---

## Wave: DISCUSS / [REF] Locked Decisions

- **[D1] Feature type = Cross-cutting.** New admin settings UI + backend recurrence model + forecasting/chart integration (every surface that today reads blackout days). (User, 2026-06-06.)
- **[D2] Walking skeleton = No / extend existing.** Reuse the shipped one-off `BlackoutPeriod` CRUD pattern, `BlackoutPeriodsSettings.tsx`, the premium+SystemAdmin guard pattern, and the `BlackoutDaysExtensions` evaluation seam. Slice thin vertical increments onto proven infrastructure. (User, 2026-06-06.)
- **[D3] v1 scope = both core use cases.** Weekday selection + "every X weeks" interval cadence + start date + OPTIONAL open-ended end (empty = forever). Must cover BOTH "exclude weekends forever" AND "off-site every 4th Friday". Monthly / nth-weekday-of-month / broader RRULE recurrence is OUT of v1. (User, 2026-06-06.)
- **[D4] Model relationship = separate entity, unified evaluation.** A new `RecurringBlackoutRule` lives ALONGSIDE the one-off `BlackoutPeriod`; forecasting and charts evaluate BOTH through a unified blackout-day check so a recurring-rule day is indistinguishable from a one-off blackout day downstream. One-off and recurring coexist as distinct concepts in the settings UI. Entity-vs-materialization is a DESIGN/ADR concern; the "separate entity + unified evaluation" product constraint is locked. (User, 2026-06-06.)
- **[D5] Auth = Premium + SystemAdmin (create/edit/delete); GET open.** Identical to one-off blackout-period CRUD. Reuses `[LicenseGuard(RequirePremium=true)]` + `[RbacGuard(SystemAdmin)]`; RBAC through `IRbacAdministrationService`; UI gating via the existing premium pattern (`isPremium`, `LicenseTooltip`, `useRbac()`). (Luna, derived from epic "only for sys admins, like regular blackout day creation".)
- **[D6] Rules are GLOBAL.** Inherits #4974 D9 — every team/feature/delivery uses the same global rule set; no per-team scoping in v1 (explicit out-of-scope). (Luna, by parity with shipped one-off periods.)
- **[D7] Monte Carlo unchanged.** Recurring rules only widen the blackout-day SET the shipped #4974 day↔date shift already consumes; `Trials`, the simulation, percentile math, `GetProbability`/`GetLikelihood` day-values are untouched (inherits #4974 D4). (Luna, by parity.)

---

## Wave: DISCUSS / [REF] User Stories (with Elevator Pitches + embedded AC)

<!-- markdownlint-disable MD024 -->

### US-01 — Create an "exclude weekends forever" recurring rule
`job_id: job-config-admin-define-recurring-blackout-rule`

As a config-admin who wants weekends excluded from every forecast, I want to define a single open-ended weekly rule (Sat+Sun, every 1 week, start today, no end) instead of hand-entering a one-off period for every weekend forever, so the forecast automatically treats every Saturday and Sunday as non-working — permanently.

#### Elevator Pitch
Before: excluding weekends means creating a separate one-off `BlackoutPeriod` for every single weekend forever, so nobody does it and the forecast counts Saturdays/Sundays as working days.
After: in **System settings → Recurring Blackout Rules → Add**, the admin picks Sat + Sun, leaves interval "every 1 week", sets start = today, leaves end empty (forever), saves → sees `the rule listed as "Every Sat, Sun — weekly — from 2026-06-06 — no end"`, and a **"When" forecast for a team now shows `P50/70/85/95 dates each stepped over the next Saturday and Sunday, none landing on a weekend`**.
Decision enabled: the admin sets weekend exclusion up once and trusts every future forecast skips weekends without ever revisiting the screen.

**AC** (verify the After end-to-end):
- AC1: A rule (Sat+Sun, every 1 week, start today, no end) created via `POST /recurring-blackout-rules` appears in `GET` with a human-readable summary.
- AC2: Given that rule and no one-off periods, a "When" forecast returning 10 working days at P85 with exactly 2 weekend days in the next 10 calendar days yields a P85 `ExpectedDate` **12** calendar days out, and no percentile date lands on a Sat/Sun (rolls forward, inherits #4974 D3).
- AC3: A recurring-rule day and a one-off `BlackoutPeriod` day produce **identical** `IsBlackoutDay`/`GetBlackoutDayIndices` results (unified evaluation, D4).
- AC4: `POST` by a non-premium OR non-SystemAdmin caller returns 403; `GET` succeeds for a viewer (D5).

### US-02 — Create an "off-site every 4th Friday" interval rule with a bounded end
`job_id: job-config-admin-define-recurring-blackout-rule`

As a config-admin whose team has a recurring off-site, I want a rule that repeats on an interval ("every 4 weeks, Friday, from 2026-06-12 to 2026-12-31") so exactly the off-site Fridays — and no other Fridays, and nothing outside the window — become non-working days.

#### Elevator Pitch
Before: a recurring off-site can only be modelled as a one-off period per occurrence, and there is no way to say "every 4th Friday this year".
After: the admin adds a rule (Friday, **every 4 weeks**, start 2026-06-12, **end 2026-12-31**) → sees `2026-06-12, 2026-07-10, 2026-08-07, … marked as non-working days while 2026-06-19 (an off-week Friday) stays a working day`, and a forecast window spanning one off-site Friday `steps that single Friday over (+1 working day)`.
Decision enabled: the admin models a bounded recurring off-site precisely, so forecasts for the rest of the year skip exactly those Fridays and no others.

**AC**:
- AC1: A rule (Fri, every 4 weeks, start 2026-06-12, end 2026-12-31) marks 2026-06-12, 2026-07-10, 2026-08-07 … as blackout days and marks NO off-week Friday (2026-06-19 is a working day).
- AC2: A date before `start` (2026-06-05) and after `end` (2027-01-08) is NOT a blackout day for this rule (bounded window).
- AC3: A "When" forecast whose window spans exactly one off-site Friday shifts the percentile date by +1 working day and lands on a working day.
- AC4: "every 1 week" with selected weekdays reproduces the US-01 weekly behaviour (interval default, no regression).

### US-03 — Recurring-rule days are honoured across every forecasting & chart surface
`job_id: job-forecast-skip-known-nonworking-days`

As a delivery-forecaster / config-admin reading a Portfolio → Delivery, a by-date likelihood, a written-back date, or a metrics chart, I want recurring-rule days treated exactly like one-off blackout days everywhere, so the unified-evaluation guarantee holds product-wide — not just on the manual "When" forecast.

#### Elevator Pitch
Before: even if the manual "When" forecast skipped recurring weekends, the feature/delivery dates, by-date likelihood, write-back date, and chart overlays might still treat a recurring weekend differently from a one-off blackout — an inconsistent product.
After: open **Portfolio → Delivery** whose features span a recurring weekend → sees `feature percentile dates stepped over the recurring weekend and rolled forward, the delivery likelihood computed on working days, the write-back date blackout-shifted, and chart overlays annotating the recurring days` — all identical to one-off blackout behaviour.
Decision enabled: the forecaster reads delivery status and the PO judges on-track/at-risk on dates that already account for recurring non-working days, with no surface-specific surprises.

**AC**:
- AC1: Feature/Delivery percentile dates (`HowManyForecast.TargetDate`, `Delivery` expected dates, `DeliveryWithLikelihoodDto.ExpectedDate`) step over recurring-rule days and roll forward (inherits #4974 D3) — identical to one-off behaviour.
- AC2: `Feature.GetLikelhoodForDate(date)` counts working days excluding recurring-rule days in the window.
- AC3: Forecast write-back writes the recurring-blackout-shifted date to Jira/ADO.
- AC4: Chart blackout overlays annotate recurring-rule days the same as one-off blackout days.
- AC5 (regression): with no recurring rules and no one-off periods, every surface is byte-identical to pre-feature (inherits #4974 D6).

### US-04 — Edit, delete, and validate recurring rules
`job_id: job-config-admin-define-recurring-blackout-rule`

As a config-admin maintaining the rule list, I want to edit and delete recurring rules and be stopped at the form with clear messages when a rule is invalid (no weekday, interval < 1, end before start), so the recurring-rules screen is a complete, trustworthy management surface like the one-off periods table.

#### Elevator Pitch
Before: a freshly-added recurring rule cannot be corrected or removed, and an invalid rule (no weekday, end before start) could be saved into a meaningless state.
After: the admin opens a rule's **Edit** dialog (pre-filled), changes Sat+Sun → Fri only, saves → `the forecast immediately reflects only Fridays`; clicking **Delete** with confirmation `removes the rule and reverts the stepped-over dates`; saving a rule with zero weekdays shows `"Select at least one weekday for the rule to repeat on."`
Decision enabled: the admin self-services rule corrections and removals and is prevented from saving a rule that would silently do nothing.

**AC**:
- AC1: Editing a rule (Sat+Sun → Fri only) via `PUT /recurring-blackout-rules/{id}` updates it; the forecast reflects the new matching days.
- AC2: Deleting a rule via `DELETE /recurring-blackout-rules/{id}` removes its days; a previously-stepped-over forecast date reverts; a viewer sees no edit/delete controls.
- AC3: Saving a rule with zero weekdays is rejected with "Select at least one weekday for the rule to repeat on."
- AC4: Saving a rule with interval < 1 is rejected with "Repeat interval must be at least 1 week."
- AC5: Saving a rule whose end date precedes its start is rejected with "End date must be on or after the start date."
- AC6: `PUT`/`DELETE` by a non-premium or non-SystemAdmin caller returns 403; an unknown id returns 404.

---

## Wave: DISCUSS / [REF] Outcome KPIs

### Objective
By release + 60 days, config-admins express recurring non-working days as reusable rules (not hand-entered one-off periods), and forecasts skip those days automatically — so "exclude weekends" goes from impractical to one click.

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|-----|-----------|-------------|----------|-------------|------|
| 1 | Premium config-admins | Define a recurring rule covering weekends instead of hand-entering one-off weekend periods | ≥ 60% of instances that today maintain ≥ 4 weekend one-off periods replace them with ≥ 1 recurring rule | ~0 (no recurrence exists) | Count of `RecurringBlackoutRule` rows vs one-off weekend `BlackoutPeriod` rows per instance (telemetry-gated — see note) | Leading (adoption) |
| 2 | Premium config-admins | Stop hand-maintaining one-off weekend periods after defining a weekends rule | Net new one-off weekend periods per instance drops by ≥ 70% in the 60 days after a weekends rule exists | current one-off-weekend creation rate | one-off `BlackoutPeriod` create events (telemetry-gated) | Leading (behaviour change) |
| 3 | Delivery-forecasters (downstream) | Present percentile dates that never land on a recurring non-working day | 0 percentile dates land on a recurring-rule day across all forecast surfaces | n/a (capability absent) | Integration-test assertion across surfaces (US-03) — product invariant, measurable today without telemetry | Guardrail (correctness) |
| 4 | Engineering | Ship recurring-day evaluation that is indistinguishable from one-off blackout days | Mutation kill rate ≥ 80% on new recurrence-expansion + endpoint code | n/a | Stryker.NET (BE) + Stryker (FE), feature-scoped | Guardrail (quality) |

### Metric Hierarchy
- **North Star**: recurring-rule adoption replacing hand-entered weekend periods (KPI 1).
- **Leading indicators**: drop in net-new one-off weekend periods after a rule exists (KPI 2).
- **Guardrail metrics**: 0 percentile dates on recurring non-working days (KPI 3, measurable today via tests); mutation ≥ 80% (KPI 4); no regression for instances with no rules (inherits #4974 D6).

### Measurement note
KPIs 1 & 2 require usage telemetry. Per MEMORY (`project_self_hosted_telemetry_gap`), self-hosted Lighthouse instances do **not** phone home; cross-instance KPIs are blocked on Epic 5015 (opt-in telemetry, no timeline). Until then, KPIs 1 & 2 are tracked **qualitatively** (Productboard feedback, support signal) and instrumented in code so they activate when telemetry lands. KPIs 3 & 4 are enforced now via the test suite and gate the feature regardless of telemetry.

---

## Wave: DISCUSS / [REF] Cross-Cutting Impact Checklist

- **RBAC** — Recurring-rule **create/edit/delete is gated by Premium + SystemAdmin**, identical to one-off blackout-period CRUD: the new endpoints carry `[LicenseGuard(RequirePremium=true)]` + `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`. RBAC flows through `IRbacAdministrationService` (no NEW permission — reuses the existing `SystemAdmin` requirement); UI gating derives from the established premium pattern (`isPremium` prop, `LicenseTooltip`, `useRbac()`). **GET (read the rule list) is open**, matching the existing `BlackoutPeriodsController` GET. (D5.)
- **Lighthouse-Clients (CLI + MCP)** — A **NEW endpoint family** `api/{v1|latest}/recurring-blackout-rules` is introduced. **Decision:** if the CLI/MCP clients surface blackout-period configuration, they need a matching wrapped method for recurring rules; that method MUST be **version-gated** — an old Lighthouse server returns an opaque 404 for an endpoint it lacks, so the client pre-checks server version and fails with a clear "upgrade Lighthouse" error. Pin **strictly newer than the LAST RELEASED Lighthouse version** and record that baseline in the clients' `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry (bump it to the current latest release when wrapping); dev/unparseable versions must never be blocked. **If the clients do not currently wrap one-off blackout-period CRUD, the recurring-rule client method is deferred** — record that decision explicitly in the clients repo at release (do not silently skip). DESIGN/DELIVER to confirm whether the clients touch blackout config at all.
- **Website** — This is a **Premium feature** (epic tagged Premium + Release Notes). **Decision:** surface it on the public website's premium-feature list and in release notes — "Exclude weekends and recurring off-sites from forecasts automatically, set up once." Tag the epic for `/release-notes`. (Not N/A — a marketed premium capability per the epic tags.)

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy D — brownfield, no walking skeleton; extend existing infrastructure** (Decision D2). There is no new end-to-end integration spine to prove — recurring rules plug into the shipped one-off CRUD pattern and the unified `BlackoutDaysExtensions` evaluation seam already consumed by forecasting and charts. Each slice is a thin vertical increment on proven infrastructure. (Of the A/B/C/D walking-skeleton options: **A** greenfield-skeleton = no; **B** thin-skeleton-then-thicken = no; **C** isolated-brownfield-layer = partial; **D** extend-existing-with-no-new-skeleton = **chosen**.)

Slice 01 still ships an observable end-to-end behaviour (create a weekends-forever rule → it lists AND a forecast date steps over Saturday/Sunday): the shared unified-evaluation "infra" rides inside a value-producing slice — **no slice contains only `@infrastructure` stories.**

---

## Wave: DISCUSS / [REF] Driving Ports (inbound surfaces touched)

- `POST /api/{v1|latest}/recurring-blackout-rules` — create a rule (US-01, US-02).
- `GET /api/{v1|latest}/recurring-blackout-rules` — list rules (open; US-01).
- `PUT /api/{v1|latest}/recurring-blackout-rules/{id}` — edit a rule (US-04).
- `DELETE /api/{v1|latest}/recurring-blackout-rules/{id}` — delete a rule (US-04).
- **Settings UI action**: a "Recurring Blackout Rules" section in the System settings page (Add / Edit / Delete dialogs, mirroring `BlackoutPeriodsSettings.tsx`) (US-01, US-02, US-04).
- **No new forecast/chart endpoint**: recurring days flow into existing #4974 surfaces (`forecast/manual`, delivery reads, write-back, chart overlays) which render the wider blackout-day set (US-03).

---

## Wave: DISCUSS / [REF] Pre-requisites

- Shipped + LOCKED: one-off `BlackoutPeriod` model/DTO/service/controller/repository; the premium+SystemAdmin guard pattern; `BlackoutPeriodsSettings.tsx`; `BlackoutDaysExtensions` evaluation seam; the #4974 day↔date forecast shift (`ProjectWorkingDays`/`CountWorkingDays`) across all forecasting consumers.
- New endpoint → client version-gate decision (cross-cutting, above).
- EF migration for the new `RecurringBlackoutRule` table (use the existing `CreateMigration` PowerShell script, per CLAUDE.md — not `dotnet ef migrations add` directly). DESIGN/DELIVER concern; flagged here as a prerequisite.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Monthly / nth-weekday-of-month / broader RRULE recurrence** (e.g. "first Monday of each month") → v1 is **weekdays + every-X-weeks only** (D3).
- **Per-team / per-portfolio scoping** of recurring rules → rules are **GLOBAL** like one-off periods (D6, inherits #4974 D9).
- **Timezone handling / holiday-calendar import / public-holiday presets.**
- **Any change to the Monte Carlo simulation or the shipped #4974 day↔date shift logic itself** — recurring rules only widen the blackout-day set the shift already consumes (D7).
- **New forecasting charts or blackout visualisations** beyond the existing overlays.
- **Migrating existing one-off weekend periods into recurring rules** (no data migration; admins replace them manually if they choose).

---

## Wave: DISCUSS / [REF] Definition of Done

1. All 4 stories' AC pass via acceptance (port-to-port) tests through the new endpoints + the existing forecast/delivery surfaces.
2. Unit tests cover rule → concrete-days expansion (weekly-forever, every-X-weeks anchoring, bounded [start,end], open-ended end) and the unified-evaluation parity (recurring day ≡ one-off blackout day).
3. Validation tests: weekday-required, interval ≥ 1, end ≥ start; premium+SystemAdmin guard on POST/PUT/DELETE; GET open; 404 on unknown id.
4. Downstream-coherence tests (US-03): feature/delivery dates, by-date likelihood, write-back, chart overlays honour recurring days identically to one-off; no-rule regression byte-identical (inherits #4974 D6).
5. Mutation ≥ 80% on new recurrence-expansion + endpoint code (Stryker.NET BE; Stryker FE).
6. EF migration generated via `CreateMigration` (all providers); `dotnet build` zero warnings; `dotnet test` green; `pnpm build`/Biome clean; SonarCloud new-violations = 0 (consult `docs/ci-learnings.md` first).
7. ADO Epic 4577 child stories created/transitioned; pause before push.
8. Release-notes + website premium-surface flag recorded (cross-cutting).
9. SSOT updated: job in `jobs.yaml`, journey `recurring-blackout-events.yaml`, persona job-ref on `config-admin`.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem statement clear, domain language + persona | PASS | 4 stories; `config-admin` primary, `delivery-forecaster` beneficiary; problem framed in admin pain (hand-entering weekends forever) |
| 2 | User/persona specific characteristics | PASS | config-admin = SystemAdmin authoring global config on the settings screen; forecaster = downstream beneficiary |
| 3 | 3+ domain examples with real data | PASS | "Every Sat+Sun, weekly, from 2026-06-06, no end"; "Fri, every 4 weeks, 2026-06-12→2026-12-31" marking 06-12/07-10/08-07; off-week 2026-06-19 stays working; edit Sat+Sun→Fri |
| 4 | UAT in Given/When/Then (3–7 per story) | PASS | 4 stories, 4–6 numeric AC each; expressible as G/W/T (DISTILL to formalise) |
| 5 | AC derived from UAT / Elevator Pitch | PASS | Each story's AC verify its Elevator Pitch end-to-end (rule listed + forecast steps over the day) |
| 6 | Right-sized (1–3 days, 3–7 scenarios) | PASS | 4 slices, ~0.5–1 day each; one surface/outcome per slice (see slice briefs) |
| 7 | Technical notes / cross-cutting constraints | PASS | RBAC/clients/website all explicit; brownfield baseline; D1–D7; EF migration prerequisite |
| 8 | Dependencies resolved or tracked | PASS | Shipped one-off CRUD + guard + settings + `BlackoutDaysExtensions` + #4974 shift (locked); new-endpoint client gate tracked; Slices ordered 01→04 |
| 9 | Outcome KPIs with measurable targets | PASS | KPI table (adoption/behaviour/correctness/quality) with targets + methods + telemetry-gap note |

**DoR Status: PASSED** (9/9). Requirements completeness: **0.96** (KPIs 1–2 telemetry-gated, tracked qualitatively until Epic 5015 — only soft point).

---

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone (config-admin authoring journey): **(A) define a rule** · **(B) express the cadence** · **(C) it acts everywhere blackout days act** · **(D) manage the rule list**.

| Activity | A · Define | B · Cadence | C · Acts everywhere | D · Manage |
|---|---|---|---|---|
| Walking skeleton (Slice 01) | Create weekends-forever rule (entity+POST+GET+UI) | every 1 week (default) | Manual "When" forecast steps over weekend (unified eval) | — |
| Release 1 (Slice 02) | — | every-X-weeks + bounded end | One off-site Friday stepped over | — |
| Release 2 (Slice 03) | — | — | Feature/Delivery/likelihood/write-back/charts all honour recurring days | — |
| Release 3 (Slice 04) | edit/delete | — | edited rule re-expands; deleted rule reverts | edit + delete + validation |

Thinnest-first. One slice = one story = one demoable surface/outcome.

| Slice | Story | Surface | Learning hypothesis (one line) |
|---|---|---|---|
| 01 | US-01 | Recurring-rules create + GET + minimal UI + expansion + unified eval | Disproves "a recurring rule expands to days feeding the SAME unified blackout-day eval (D4) so the #4974 shift consumes them for free" if a forecast doesn't step over the configured weekend |
| 02 | US-02 | Recurring-rules create (interval + bounded end) | Disproves "every-X-weeks anchors on the start week so exactly the intended Fridays match" if the wrong Fridays / out-of-window days are marked |
| 03 | US-03 | Feature/Delivery/likelihood/write-back/chart surfaces | Disproves "recurring days drop into the existing union-of-blackout-days seam unchanged" if any surface treats a recurring day differently from a one-off blackout day |
| 04 | US-04 | Recurring-rules edit/delete + form validation | Disproves "the lifecycle reuses the one-off edit/delete/validation UX 1:1" if recurrence-specific validations (weekday-required, interval≥1) don't fit the existing pattern |

Slice briefs: `docs/feature/recurring-blackout-events/slices/slice-0{1..4}-*.md`.

### Priority Rationale
Slice 01 first = it births the entity + endpoint + the **unified-evaluation seam** (highest learning leverage; proves D4 on the smallest surface while delivering observable value — a forecast stepping over a weekend). Slice 02 adds interval/bounding on the same endpoint (second epic use case, smallest delta). Slice 03 fans the proven recurring-day source across all #4974 consumers (high value, depends on the seam being settled). Slice 04 (edit/delete/validation) last — lowest forecasting risk, completes the management surface, depends on the entity existing. Ordering = Value × Urgency / Effort with Walking-Skeleton (Slice 01) > riskiest assumption (D4 unified eval, also Slice 01) > highest value (Slice 03) > completeness (Slice 04).

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Feature type**: Cross-cutting (D1) — settings UI + recurrence model + forecasting/chart integration.
- **JTBD**: Yes (Decision 4 = YES) — every story carries a real `job_id`: US-01/02/04 → `job-config-admin-define-recurring-blackout-rule` (NEW); US-03 → `job-forecast-skip-known-nonworking-days` (existing downstream).
- **UX research depth**: Lightweight — reuses the proven `BlackoutPeriodsSettings.tsx` settings pattern; the only new UI is a recurrence form (weekday checkboxes + interval + dates) and a rule-summary list row. Platform = web (React + ASP.NET Core); web UX skills consulted on-demand.
- **Walking skeleton**: No — Strategy D (extend existing, D2).
- **Primary need**: define recurring non-working days once (weekends forever; off-site every 4th Friday) so forecasts and charts skip them automatically, without an ever-growing one-off list.
- **Constraints**: D4 (unified evaluation — recurring day ≡ one-off day downstream), D5 (premium+SystemAdmin; GET open), D6 (global), D7 (Monte Carlo unchanged), D3 (no monthly/RRULE in v1).
- **Upstream changes**: no DISCOVER/DIVERGE artifacts existed for this epic (sibling of shipped #4974). SSOT bootstrapped: new job in `jobs.yaml`, new journey `recurring-blackout-events.yaml`, `config-admin` persona job-ref added. **Risk recorded**: no formal DIVERGE recommendation — mitigated by inheriting the validated #4974 decisions and the locked user decisions D1–D4.
- **Expansion trigger**: none fired (single bounded context, 2 personas, no compliance, unambiguous numeric AC, brownfield no-skeleton) → strict lean; ask-intelligent menu NOT emitted.

**Handoff → DESIGN (nw-solution-architect)** + **DEVOPS (KPIs only, telemetry-gated)** + **DISTILL (journey YAML + integration points + KPIs)**. DESIGN to decide: the `RecurringBlackoutRule` entity shape + EF migration; rule→concrete-days **expansion vs materialization** (the entity-vs-materialization detail deferred per D4) and where the unified-evaluation union lives (likely a `BlackoutDaysExtensions` extension or an assembly-layer union into the existing `IReadOnlyList<BlackoutPeriod>` shape); the new controller/service/repository; and whether recurring rules inherit a premium gate on the evaluation path (none observed on one-off throughput stripping — confirm).

---

## Wave: DESIGN / [REF] Prior-Wave Consultation Checklist

- ✓ `docs/feature/recurring-blackout-events/feature-delta.md` (DISCUSS): D1–D7, cross-cutting checklist, 4 slices, DoR 9/9 read and designed within.
- ✓ `docs/product/journeys/recurring-blackout-events.yaml`: journey, surfaces, error paths, D1–D6 read; error-path messages reused verbatim in validation (ADR-060 §5).
- ✓ `docs/product/architecture/adr-058-…`: the A1 contract, the `BlackoutDaysExtensions` seam, the fetch-once-pass-inward pattern, and ADR-058's explicit "promote behind an interface if recurring rules arise; YAGNI until then" hint — all consulted; ADR-059 chose materialization (clears the YAGNI bar at a fraction of B's blast radius).
- ✓ `docs/product/architecture/brief.md` `## Application Architecture`: house style followed; appended `## Application Architecture — recurring-blackout-events` delta (earlier sections untouched).
- ✓ Code surfaces grounded (read, not assumed): `BlackoutPeriod`/`BlackoutPeriodDto`/`BlackoutPeriodService`/`IBlackoutPeriodService`/`BlackoutPeriodsController`/`BlackoutDaysExtensions`/`BlackoutPeriodRepository`/`TeamMetricsService` (fetch-once); the #4974 seams `ForecastController`, `DeliveriesController`+`DeliveryWithLikelihoodDto.FromDelivery`, `WriteBackTriggerService`, `Feature.GetLikelhoodForDate`, `Delivery.CalculateMetrics`, `WhenForecastDto`; `LighthouseAppContext` (the `StateMappings` JSON-converter+`ValueComparer` precedent + `BlackoutPeriod` key config + `DbSet`); `Program.cs` DI; FE `BlackoutPeriodsSettings.tsx`/`BlackoutPeriod.ts`/`BlackoutPeriodService.ts`/`BlackoutOverlay.tsx`. **Decisive finding**: the blackout-day set is fetched at **~13 call sites** (`blackoutPeriodRepository.GetAll().ToList()`), all threading into `BlackoutPeriod`-typed pure helpers — the lever for ADR-059's materialization-behind-one-seam.
- ⊘ `nwave-ai outcomes check-delta` — nWave tooling not present in this repo (Lighthouse); **attempted, skipped-unavailable**, not blocking (per task note).

## Wave: DESIGN / [REF] Domain-Driven Design

- **DDD-1 — Materialization, not signature change (ADR-059).** Recurring days reach evaluation as synthetic single-day `BlackoutPeriod` instances. **Verdict: chosen** (Option C of the pivotal decision) — smallest blast radius that keeps the union in one place; D4/D7 fall out by construction.
- **DDD-2 — Union behind the fetch seam (ADR-059).** `IBlackoutPeriodService.GetEffectiveBlackoutDays(window) → IReadOnlyList<BlackoutPeriod>` is the single assembly point; the 13 eval sites do a same-shape swap. **Verdict: chosen** — mirrors #4974 fetch-once-pass-inward (ADR-058 DDD-2).
- **DDD-3 — Separate entity, value-typed weekday set (ADR-060).** `RecurringBlackoutRule` mirrors `BlackoutPeriod`'s stack; `Weekdays:List<DayOfWeek>` JSON-converted + `ValueComparer` (the `StateMappings` idiom). **Verdict: chosen** — D4 locks separate entity; bitmask/child-table rejected.
- **DDD-4 — Pure interval-anchored expansion (ADR-060).** `ExpandToBlackoutDays(rule, window)` = weekday match ∧ week-index-modulo anchor ∧ `[Start,End]∩window`. **Verdict: chosen** — interval-1 ⇒ weekly by `% 1`; anchoring worked against every US-02 AC.
- **DDD-5 — Models acquire no repo/service dependency.** Entity is a persistence projection; expansion is a pure extension with the window passed in. **Verdict: upheld** — ArchUnitNET `Models.* ↛ Services.*`.
- **DDD-6 — No-rule regression byte-identical (inherits #4974 D6).** No rules ⇒ `GetEffectiveBlackoutDays ≡ GetAll()`. **Verdict: upheld** — golden test.

## Wave: DESIGN / [REF] Component Decomposition

| Component | Path | EXTEND / CREATE NEW |
|---|---|---|
| `RecurringBlackoutRule` entity | `Lighthouse.Backend/Lighthouse.Backend/Models/RecurringBlackoutRule.cs` | CREATE NEW (D4 separate entity) |
| `RecurringBlackoutRuleDto` | `Models/RecurringBlackoutRuleDto.cs` | CREATE NEW |
| Expansion (pure) | `Services/Implementation/RecurringBlackoutRuleExtensions.cs` | CREATE NEW |
| Rule service + interface | `Services/Implementation/RecurringBlackoutRuleService.cs`, `Services/Interfaces/IRecurringBlackoutRuleService.cs` | CREATE NEW |
| Rule repository | `Services/Implementation/Repositories/RecurringBlackoutRuleRepository.cs` | CREATE NEW |
| Rule controller | `API/RecurringBlackoutRulesController.cs` | CREATE NEW |
| Union seam | `Services/Interfaces/IBlackoutPeriodService.cs` + `Services/Implementation/BlackoutPeriodService.cs` (`GetEffectiveBlackoutDays`, inject rule repo) | EXTEND |
| EF context | `Data/LighthouseAppContext.cs` (`DbSet<RecurringBlackoutRule>` + weekday converter/comparer + key) | EXTEND |
| DI | `Program.cs` (register `IRepository<RecurringBlackoutRule>`) | EXTEND |
| Eval fetch sites (×13) | `ForecastController`, `DeliveriesController`, `FeaturesController`, `DeliveryRulesController`, `TeamMetricsController`, `PortfolioMetricsController`, `TeamController`, `TeamsController`, `WriteBackTriggerService`, `TeamMetricsService`, `DeliveryMetricSnapshotRecordingHandler` | EXTEND (same-shape swap) |
| FE rule model + Zod | `Lighthouse.Frontend/src/models/RecurringBlackoutRule.ts` | CREATE NEW |
| FE API service | `src/services/Api/RecurringBlackoutRuleService.ts` | CREATE NEW |
| FE settings section | `src/pages/Settings/System/BlackoutSettings.tsx` (VF-2: merged one-off + recurring into one section/grid; was planned as a sibling `RecurringBlackoutRulesSettings.tsx`) | CREATE NEW |
| EF migration | via `CreateMigration` PowerShell script | CREATE NEW (new table) |

## Wave: DESIGN / [REF] Driving Ports

- `POST /api/{v1|latest}/recurring-blackout-rules` — create (Premium+SystemAdmin) — US-01/02.
- `GET /api/{v1|latest}/recurring-blackout-rules` — list (open) — US-01.
- `PUT /api/{v1|latest}/recurring-blackout-rules/{id}` — edit (Premium+SystemAdmin) — US-04.
- `DELETE /api/{v1|latest}/recurring-blackout-rules/{id}` — delete (Premium+SystemAdmin) — US-04.
- Settings UI section action (Add/Edit/Delete dialogs, weekday checkboxes + interval + start/optional-end + summary) — US-01/02/04.
- No new forecast/chart endpoint (US-03 rides existing #4974 surfaces via `GetEffectiveBlackoutDays`).

## Wave: DESIGN / [REF] Driven Ports + Adapters

- `IRepository<RecurringBlackoutRule>` → `RecurringBlackoutRuleRepository : RepositoryBase<RecurringBlackoutRule>` (EF Core 8; `GetAll()` global, D6). Newly injected into `BlackoutPeriodService` for the union.
- REUSE: `IRepository<BlackoutPeriod>` (existing), `IRbacAdministrationService` (RBAC), `ILicenseService` (premium guard via attribute). **No new external integration / foreign substrate ⇒ no probe contract and no contract tests owed at the platform-architect handoff** (the union is a pure in-process function over the two existing repos).

## Wave: DESIGN / [REF] Technology Choices

- **Backend**: C# .NET 8 ASP.NET Core, EF Core 8 (OSS, MIT/Apache — already the stack). Weekday set via EF `ValueConverter` + `ValueComparer` (built-in; the `StateMappings` precedent). No new library.
- **Frontend**: React 18 + TS, MUI (existing), Zod at the trust boundary (existing). No new library.
- **Migration**: existing `CreateMigration` PowerShell script (CLAUDE.md). No new tooling.

## Wave: DESIGN / [REF] Decisions

| # | Decision | Choice | ADR |
|---|---|---|---|
| 1 | Unified-evaluation mechanism | **Materialize** recurring days into synthetic single-day `BlackoutPeriod`s; union behind `IBlackoutPeriodService.GetEffectiveBlackoutDays(window)` (Option C) | ADR-059 |
| 2 | Entity + weekday storage | Mirror `BlackoutPeriod` stack; `Weekdays:List<DayOfWeek>` JSON-converted + `ValueComparer`; `Start:DateOnly`, `End:DateOnly?` | ADR-060 |
| 3 | Expansion algorithm + home | Pure `RecurringBlackoutRuleExtensions.ExpandToBlackoutDays(rule, window)`; week-index-modulo anchor (Monday of start week) | ADR-060 |
| 4 | New controller/service/repo | `RecurringBlackoutRulesController` + `IRecurringBlackoutRuleService` + `IRepository<RecurringBlackoutRule>`, mirroring the one-off stack; `GetEffectiveBlackoutDays` exposes the unified eval path | ADR-059/060 |
| 5 | Premium gating on eval path | **None** — recurring rules act whenever configured for every viewer (inherits one-off / #4974 verdict); only writes gated | ADR-059 |
| 6 | Frontend shape | ~~Sibling component `RecurringBlackoutRulesSettings.tsx` (second section)~~ **→ REVERSED by VF-2 (DELIVER, user verification 2026-06-06): ONE unified `BlackoutSettings.tsx` section with a single merged grid** (Schedule \| Description \| Actions; one row per entry, Schedule = date-range for one-off OR recurrence-summary for recurring) and two Add buttons. One-off & recurring stay *distinct concepts* (two Add buttons, two dialogs, date-range vs summary text) but share one box — less real estate, easier to manage. See "Verification-Feedback Overrides" below. | brief delta |

## Wave: DESIGN / [REF] Reuse Analysis

| Overlapping component | Verdict | Justification |
|---|---|---|
| `BlackoutPeriod` model/DTO/service-CRUD/controller/repo | REUSE AS-IS | Untouched; recurring is a separate entity (D4) |
| `BlackoutDaysExtensions` (all helpers) | REUSE AS-IS | Materialized recurring days are ordinary `BlackoutPeriod` input — no signature change (D7) |
| #4974 A1 seams (`WhenForecastDto`, `Feature.GetLikelhoodForDate`, `Delivery.CalculateMetrics`, `FromDelivery`, `ProjectWorkingDays`/`CountWorkingDays`) | REUSE AS-IS | Contract untouched; they receive the unified list |
| Monte Carlo (`ForecastService`/`ForecastBase`) | REUSE AS-IS | D7 — only the day SET widens |
| Chart overlays (`Blackout`/`PbcBlackout`/`TimeBlackout`Overlay.tsx) | REUSE AS-IS | Consume server-derived `blackoutDayLabels`; transparent to the union |
| `IBlackoutPeriodService` + `BlackoutPeriodService` | EXTEND | Add `GetEffectiveBlackoutDays`; the union's one home |
| `LighthouseAppContext`, `Program.cs` | EXTEND | `DbSet` + converter + DI registration |
| 13 eval fetch sites | EXTEND | Same-shape swap to the union method |
| `BlackoutPeriodsSettings.tsx` | MERGED (VF-2) | Merged with `RecurringBlackoutRulesSettings.tsx` into the unified `BlackoutSettings.tsx`; both originals deleted |
| **New entity stack (7 BE files + 3 FE files + migration)** | **CREATE NEW** | **Evidence**: D4 locks a *separate entity*; a recurring rule (weekdays + interval + open-endedness) cannot be expressed by `BlackoutPeriod`'s date range, so storage reuse is impossible. Each new file is the recurring twin of a shipped one-off file — the *pattern* is reused even though the *type* is new (D2 satisfied). |

## Wave: DESIGN / [REF] Open Questions

- **OQ-1 (the single decision needing user confirmation)** — confirm ADR-059 Option C (materialize + union behind `GetEffectiveBlackoutDays`, `IReadOnlyList<BlackoutPeriod>` shape unchanged) over Option B (generalize the seam behind an `IBlackoutDaySource` interface, cleaner long-term model but re-touches every helper + the shipped #4974 shift). Recommendation: **Option C**.
- **OQ-2** — do the CLI/MCP clients currently wrap one-off blackout-period CRUD? If yes, add a version-gated recurring-rule method; if no, defer-and-record explicitly. (DELIVER confirms against the clients repo.)

## Wave: DESIGN / [REF] Wave Decisions Summary

- **Pattern**: Ports-and-adapters (hexagonal) — unchanged; no new style.
- **Paradigm**: **OOP** (C# backend), functional-leaning React frontend.
- **Key components**: NEW `RecurringBlackoutRule` entity + DTO + pure `ExpandToBlackoutDays` + service/repo/controller + FE unified `BlackoutSettings.tsx` settings section (VF-2 merge); EXTENDED `IBlackoutPeriodService.GetEffectiveBlackoutDays` (the union seam) + 13 same-shape fetch-site swaps + `LighthouseAppContext` + `Program.cs`.
- **Reuse**: everything the recurring days flow *into* is REUSE AS-IS (helpers, #4974 shift, Monte Carlo, overlays); the union seam and EF/DI are EXTEND; the new entity stack is CREATE NEW (D4 separate entity, evidence above).
- **Tech stack**: existing — .NET 8 / EF Core 8 / React 18 / MUI / Zod; EF migration via `CreateMigration`. No new library/integration.
- **Constraints honoured**: D1 (cross-cutting), D2 (extend, no skeleton), D3 (weekdays + every-X-weeks + start + optional end), D4 (separate entity, unified eval via materialization), D5 (writes Premium+SystemAdmin, GET open, no new permission), D6 (global), D7 (Monte Carlo + shift untouched).
- **Upstream changes**: none — every AC is satisfiable within the locked DISCUSS decisions; no user story / AC needs to change. No `## Changed Assumptions`, no `design/upstream-changes.md`.
- **ADRs**: ADR-059 (unified evaluation via materialization), ADR-060 (entity + weekday storage + expansion). Cross-ref ADR-058.
- **External integrations / contract tests**: none — no foreign substrate; nothing owed at the platform-architect handoff.
- **ADO children**: #5221 (US-01), #5222 (US-02), #5223 (US-03), #5224 (US-04) — already exist.

---

## Wave: DISTILL / [REF] Reconciliation Gate

**Reconciliation passed — 0 contradictions.** DISCUSS decisions D1–D7 were checked against
DESIGN (ADR-059 Option C, ADR-060, OQ-1 confirmed). DESIGN explicitly recorded "no upstream
changes." Each DISCUSS decision has a consistent DESIGN realisation: D3↔ADR-060 entity shape;
D4↔ADR-059 materialization + separate entity; D5↔both ADRs' premium verdict (writes
Premium+SystemAdmin, GET open); D6↔`GetAll()` global; D7↔"shift untouched, only widens set".
No `--policy=fresh`; `--policy=inherit` applied.

## Wave: DISTILL / [REF] Scenario list with tags

Density: **lean** (Tier-1 [REF] only). Tier A acceptance only — no Tier B state-machine PBT
(this is a C#/.NET project; the Hypothesis/`RuleBasedStateMachine` pilot does not apply here per
`docs/architecture/atdd-infrastructure-policy.md`). 26 backend integration scenarios authored
(17 original + 9 added to close Sentinel's US-03 per-surface HIGH findings);
**10/26 = 38% error/edge** plus **5/26 = 19% regression/parity guards**; the 8 explicit
`@error` scenarios remain (≥40% of the non-guard, behaviour-asserting set: 8/21 = 38%, and
10/26 counting the two new regression guards as edge). US-03 now traces a dedicated test to
every AC: AC1/AC2 via #2/#4/#5/#6 + #18–#20 (delivery), AC3 via #21–#23 (write-back), AC4 via
#24–#26 (chart overlays), AC5 via #7 + #23 + #26.

| # | Scenario | Story | Tags |
|---|----------|-------|------|
| 1 | Create weekends-forever rule as premium SystemAdmin → 201 + listed with human-readable summary | US-01 | @walking_skeleton @real-io @US-01 |
| 2 | "When" forecast with weekends-forever rule → no percentile date lands on a weekend | US-01 | @real-io @US-01 |
| 3 | Create every-4th-Friday bounded rule → 201 + listed | US-02 | @real-io @US-02 |
| 4 | "When" forecast with off-site Friday rule → no percentile date lands on a Friday | US-02 | @real-io @US-02 |
| 5 | Interval-1 rule reproduces plain weekly behaviour (no regression) | US-02 | @real-io @US-02 |
| 6 | Recurring-rule day shifts percentile date identically to an equivalent one-off period (parity) | US-03 | @real-io @US-03 |
| 7 | No rules and no one-off periods → percentile date unshifted (regression, inherits #4974 D6) | US-03 | @real-io @regression @US-03 |
| 8 | GET as non-premium user does NOT return 403 (open read) | US-04 | @error @auth @US-04 |
| 9 | POST as non-premium user → 403 | US-04 | @error @auth @US-04 |
| 10 | PUT as non-premium user → 403 | US-04 | @error @auth @US-04 |
| 11 | DELETE as non-premium user → 403 | US-04 | @error @auth @US-04 |
| 12 | Edit rule (Sat+Sun → Fri) → list reflects the new weekday | US-04 | @real-io @US-04 |
| 13 | Delete rule → removed from the list | US-04 | @real-io @US-04 |
| 14 | Delete unknown id → 404 | US-04 | @error @US-04 |
| 15 | Create with zero weekdays → 400 "Select at least one weekday for the rule to repeat on." | US-04 | @error @US-04 |
| 16 | Create with interval < 1 → 400 "Repeat interval must be at least 1 week." | US-04 | @error @US-04 |
| 17 | Create with end before start → 400 "End date must be on or after the start date." | US-04 | @error @US-04 |
| 18 | Delivery feature percentile date steps over recurring-rule days (AC1/AC2) | US-03 | @real-io @US-03 |
| 19 | Delivery feature percentile date identical to equivalent one-off period (parity, AC2) | US-03 | @real-io @US-03 |
| 20 | Delivery likelihood (`GetLikelhoodForDate`) computed on the working-day count excluding recurring days (AC2) | US-03 | @real-io @US-03 |
| 21 | Forecast write-back persists the recurring-blackout-shifted date (AC3) | US-03 | @real-io @US-03 |
| 22 | Write-back date identical to equivalent one-off period (parity, AC3) | US-03 | @real-io @US-03 |
| 23 | Write-back with no rules and no periods → unchanged date (regression, AC5) | US-03 | @real-io @regression @US-03 |
| 24 | Chart throughput PBC annotates the recurring-rule data point as blackout (AC4) | US-03 | @real-io @US-03 |
| 25 | Chart `IsBlackout` annotation identical to equivalent one-off period (parity, AC4) | US-03 | @real-io @US-03 |
| 26 | Chart with no rules and no periods → no data point annotated (regression, AC5) | US-03 | @real-io @regression @US-03 |

## Wave: DISTILL / [REF] WS Strategy

Inherits the **Architecture of Reference** + the project ATDD Infrastructure Policy
(`docs/architecture/atdd-infrastructure-policy.md`, `--policy=inherit`):

- **Driving (HTTP API)** = REAL `WebApplicationFactory<Program>` over real HTTP (`TestWebApplicationFactory` + `WithTestAuthentication`; `AsSystemAdmin`/`AsViewer`/`AsTeamViewer`). Routes `/api/latest/recurring-blackout-rules`.
- **Driven internal (EF `LighthouseAppContext` + `IRepository<T>`)** = REAL EF via the test factory; `EnsureDeleted`+`EnsureCreated` per `[SetUp]`. The new `IRepository<RecurringBlackoutRule>` is covered by the **existing** EF policy row — no new policy row needed (confirmed: it is a `RepositoryBase<RecurringBlackoutRule>` over the same context, same mechanism).
- **Driven external / non-deterministic** = FAKE: `Mock<ILicenseService>` (`CanUsePremiumFeatures()→true`), `Mock<IForecastService>` (deterministic single-key Monte Carlo mass), `Mock<ITeamMetricsService>` — all injected via `RemoveAll`+`AddScoped`, exactly as #4974's `BlackoutForecastShiftTestBase`.

Walking-skeleton scenario (#1, `@walking_skeleton`): create a weekends-forever rule as a premium
SystemAdmin and see it listed — the demo proof a stakeholder confirms ("yes, I set up weekend
exclusion once"); scenario #2 closes the loop by showing the forecast then steps over the
weekend (the unified-eval payoff). Per DISCUSS WS Strategy D (brownfield, extend existing), there
is no new integration spine — recurring days ride the shipped #4974 surfaces.

**No new atdd-policy rows** (expected none; confirmed).

## Wave: DISTILL / [REF] Adapter coverage table

| Driven adapter | @real-io scenario | Covered by |
|---|---|---|
| EF `IRepository<RecurringBlackoutRule>` (new, real) | YES | #1/#3/#12/#13 — POST/PUT/DELETE round-trip through real EF; GET reads it back; #18–#26 seed the rule via POST |
| EF `IRepository<BlackoutPeriod>` (existing, real) | YES | #6/#19/#22/#25 — one-off period seeded + removed via the real repo for the parity comparison |
| `IBlackoutPeriodService.GetEffectiveBlackoutDays` union seam (extended) | YES | #2/#4/#5/#6/#7 (forecast); #18–#20 (delivery read path `DeliveryWithLikelihoodDto.FromDelivery` + `Feature.GetLikelhoodForDate`); #21–#23 (`WriteBackTriggerService`); #24–#26 (`TeamMetricsController.AnnotateBlackoutDays` → `ProcessBehaviourChart.IsBlackout`) — every #4974 eval site exercised end-to-end |
| `IWriteBackService` (fake) | n/a (fake) | `Mock<IWriteBackService>` captures the written `WriteBackFieldUpdate.Value` for the shifted date (#21–#23) |
| `ITeamMetricsService` (fake) | n/a (fake) | `Mock<ITeamMetricsService>` returns a deterministic 3-day PBC so `IsBlackout` annotation is assertable (#24–#26) |
| `ILicenseService` (fake) | n/a (fake) | premium toggled via `Mock<ILicenseService>` (#1–#7, #12–#26 happy/parity) / default non-premium (#8–#11) |
| `IForecastService` (fake) | n/a (fake) | deterministic 10-working-day mass pins the date shift (#2/#4/#5/#6/#7); delivery/write-back surfaces (#18–#23) pin the forecast directly on the seeded `Feature` via `SetFeatureForecasts` (no service call), mirroring #4974 |

## Wave: DISTILL / [REF] Driving-adapter coverage

Every NEW endpoint exercised over HTTP at least once:

| Endpoint | Method | Exercised by |
|---|---|---|
| `/api/latest/recurring-blackout-rules` | POST | #1, #3, #5, #6, #9, #15, #16, #17 |
| `/api/latest/recurring-blackout-rules` | GET | #1, #3, #8, #12, #13 |
| `/api/latest/recurring-blackout-rules/{id}` | PUT | #10, #12 |
| `/api/latest/recurring-blackout-rules/{id}` | DELETE | #11, #13, #14 |
| `/api/latest/forecast/manual/{teamId}` (existing #4974 surface) | POST | #2, #4, #5, #6, #7 |
| `/api/latest/deliveries/portfolio/{portfolioId}` (existing #4974 surface) | POST + GET | #18, #19, #20 — feature/delivery percentile dates + likelihood (US-03 AC1/AC2) |
| `IWriteBackTriggerService.TriggerForecastWriteBackForPortfolio` (resolved from the real DI container; the write-back surface has no HTTP endpoint — same service seam as the #4974 `BlackoutForecastShiftWriteBackTest`, but seeded via the rule POST) | service seam | #21, #22, #23 — shifted write-back date (US-03 AC3) |
| `/api/latest/teams/{teamId}/metrics/throughput/pbc` (existing #4974 surface) | GET | #24, #25, #26 — chart `IsBlackout` overlay annotation (US-03 AC4) |

## Wave: DISTILL / [REF] Scaffolds

**No production scaffolds created.** Per the C#/statically-typed RED-readiness strategy, the
new endpoints are driven over **raw HTTP** with anonymous/inline JSON bodies — the Tests project
references NO not-yet-existing C# types (`RecurringBlackoutRule`, its DTO, service, controller).
The project therefore compiles today (verified: 0 warnings, 0 errors) and each new-endpoint call
fails with route-missing = MISSING_FUNCTIONALITY = correct RED. DELIVER creates the production
types in its RED/GREEN cycle (ADR-025). The `__SCAFFOLD__` Python-pilot convention does not apply
to this repo (per the ATDD Infrastructure Policy header).

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/` — the established home for
port-to-port `WebApplicationFactory` integration tests, and the exact location of the SHIPPED
sibling #4974 suite (`BlackoutForecastShift*IntegrationTest.cs`,
`BlackoutPeriodsControllerAuthorizationTests.cs`). New files:

- `RecurringBlackoutRulesTestBase.cs` — combined deterministic-forecast + premium-license base (merges #4974's `BlackoutForecastShiftTestBase` forecast pinning with the `Mock<ILicenseService>` premium enablement).
- `RecurringBlackoutRulesWeekendsForeverIntegrationTest.cs` (US-01) — 2 scenarios.
- `RecurringBlackoutRulesIntervalRuleIntegrationTest.cs` (US-02) — 3 scenarios.
- `RecurringBlackoutRulesDownstreamParityIntegrationTest.cs` (US-03) — 2 scenarios (manual "When" parity + no-rule regression).
- `RecurringBlackoutRulesDeliveryIntegrationTest.cs` (US-03 AC1/AC2) — 3 scenarios; mirrors #4974 `BlackoutForecastShiftDeliveryIntegrationTest.cs` (feature/delivery percentile dates + `GetLikelhoodForDate` via `/deliveries/portfolio/{id}`).
- `RecurringBlackoutRulesWriteBackIntegrationTest.cs` (US-03 AC3) — 3 scenarios; mirrors #4974 `BlackoutForecastShiftWriteBackTest.cs` (shifted write-back date; same `IWriteBackTriggerService` seam resolved from the real container, rule seeded via POST + `Mock<IWriteBackService>` capture).
- `RecurringBlackoutRulesChartOverlayIntegrationTest.cs` (US-03 AC4) — 3 scenarios; mirrors #4974 `TeamMetricsControllerTest.GetThroughputPbc_With/NoBlackoutPeriods` over HTTP (`/teams/{id}/metrics/throughput/pbc` → `ProcessBehaviourChart.IsBlackout`).
- `RecurringBlackoutRulesAuthorizationTests.cs` (US-04, extends `IntegrationTestBase`, mirrors `BlackoutPeriodsControllerAuthorizationTests`) — 4 scenarios.
- `RecurringBlackoutRulesLifecycleIntegrationTest.cs` (US-04) — 6 scenarios.

All tests carry `[Category("recurring-blackout-events")]` and `[Ignore("pending DELIVER — US-0N")]`;
they were verified RED before being ignored (see `distill/red-classification.md`). DELIVER
un-ignores one at a time for Outside-In TDD.

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports: the four `recurring-blackout-rules` endpoints + the extended `IBlackoutPeriodService.GetEffectiveBlackoutDays` union seam (ADR-059) + the pure `RecurringBlackoutRuleExtensions.ExpandToBlackoutDays` anchoring (ADR-060).
- DEVOPS: no new environment matrix — the SQLite test-host (with Postgres lockstep in CI) recorded in the ATDD policy is reused as-is.
- The new `IRepository<RecurringBlackoutRule>` must be DI-registered (DESIGN component decomposition) before the integration tests can seed/read it — a DELIVER GREEN-phase obligation.

## Wave: DISTILL / [REF] DELIVER obligations (carried, not authored here)

These are intentionally NOT authored in DISTILL — they need the production types and are
DELIVER's RED-phase job (ADR-025), or are unit/component-level work:

- **Unit tests for `ExpandToBlackoutDays`** — the interval-anchoring math (US-02 worked examples: 06-12/07-10/08-07 match, 06-19 does not; out-of-window excluded; interval-1 ≡ weekly), purity (no I/O), and the synthetic single-day `BlackoutPeriod { Start==End }` shape (ADR-060 enforcement table). DELIVER authors these against the real production types.
- **Unit parity test for the union** — recurring-day-set ≡ one-off-period-set across `IsBlackoutDay`/`GetBlackoutDayIndices`/`ProjectWorkingDays` (ADR-059 enforcement table); the integration parity test (#6) is the user-observable proxy, the unit test is the helper-level proof.
- **ArchUnitNET seam test** — NOT authored here: it would reference the not-yet-existing `RecurringBlackoutRule`/`RecurringBlackoutRuleExtensions` namespaces and break compilation. Recorded as a DELIVER close-out obligation. Template: the #4974 `Architecture/BlackoutForecastShiftSeamArchUnitTest.cs`. Rules to encode (ADR-059/060): `Models.RecurringBlackoutRule ↛ Services.*`; the forecast/chart path depends on `GetEffectiveBlackoutDays`, not the raw `IRepository<BlackoutPeriod>.GetAll()`; expansion output is never `Add()`-ed to the repo.
- **EF round-trip / `ValueComparer`-present test** — the weekday `List<DayOfWeek>` JSON-converter + `ValueComparer` (the StateMappings trap, ADR-060 §2). DELIVER, against the real `LighthouseAppContext`.
- **Frontend `RecurringBlackoutRulesSettings.tsx` Vitest/RTL tests** — the component does not exist yet; per the project convention its tests live beside it and are DELIVER's Outside-In responsibility (same as the sibling one-off `BlackoutPeriodsSettings` tests). NOT scaffolded now.
- **Mutation gate** — Stryker.NET (BE) + Stryker (FE) ≥ 80% on the new recurrence-expansion + endpoint code (DoD item 5).
- **Outcomes registry** — `nwave-ai outcomes register` is **absent in this repo** (not the Python pilot). Skipped-unavailable, non-blocking.

### [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DISCUSS#D4 | Separate entity + unified evaluation — a recurring-rule day is indistinguishable downstream from a one-off blackout day | DDD-1, DDD-2 | Scenario #6 asserts the recurring-rule forecast date equals the one-off-period forecast date over the same days — the user-observable proxy for D4 parity |
| DISCUSS#D5 | Writes gated Premium + SystemAdmin; GET open | n/a | Scenarios #8–#11 pin the 403-on-write / open-GET guard; happy paths run as premium SystemAdmin |
| DISCUSS#D7 | Monte Carlo + #4974 shift untouched — recurring rules only widen the blackout-day set | DDD-1 | Forecast scenarios reuse the shipped #4974 deterministic-forecast harness verbatim; the only new input is the recurring rule POSTed before the forecast |
| DESIGN#ADR-059 | Union assembled once behind `IBlackoutPeriodService.GetEffectiveBlackoutDays(window)`; 13 same-shape call-site swaps | DDD-2 | Scenarios #2/#4/#5/#6/#7 exercise the union via the existing forecast endpoint; the ArchUnit seam test (DELIVER obligation) pins "no raw repo on the eval path" |
| DESIGN#ADR-060 | Validation messages match journey error paths verbatim | DDD-4 | Scenarios #15/#16/#17 assert the exact strings ("Select at least one weekday…", "Repeat interval must be at least 1 week.", "End date must be on or after the start date.") |
| #4974 D6 | No-rule regression byte-identical to pre-feature | DDD-6 | Scenario #7 asserts the unshifted `Today + 10 working days` date with no rules and no periods |

---

## Wave: DELIVER / [REF] Verification-Feedback Overrides (2026-06-06)

After the 16 DELIVER steps shipped, the user manually verified the feature ("looks really great") and raised two UI revisions, folded in before the quality gates. Both are recorded here as the source of truth for the deviations from DESIGN Decision 6.

- **VF-1 — throughput tooltip qualifiers as a list (polish).** On the team-view Throughput tile, when a forecast filter AND a blackout overlap are both active, the tooltip previously chained two parentheticals ("… (Blackout days within window — excluded from forecast) (Forecast filter active — some throughput items excluded)"). Now the base label renders first and the active qualifiers render as a bulleted **list** beneath it (`ThroughputQuickSetting.tsx`). The IconButton `aria-label` keeps a flat, ordered string (base → blackout → filter) so the existing accessible-name assertions still hold. Pre-existing surface (#4974 + forecast-filter), not recurring-specific.
- **VF-2 — merged settings section (REVERSES DESIGN Decision 6 / the D4 "sibling, distinct sections" UI framing).** The System-settings page previously showed TWO boxes ("Blackout Periods" + "Recurring Blackout Rules"). The user asked for ONE box with less real estate that is easier to manage. Delivered as a single `BlackoutSettings.tsx` section titled **"Blackout Periods & Recurring Rules"** with **two Add buttons** (Add Blackout Period / Add Recurring Rule) and **one merged grid**: columns **Schedule | Description | Actions**, one row per entry, where Schedule shows the `start → end` range for a one-off period or the recurrence summary for a recurring rule. The two prior components (`BlackoutPeriodsSettings.tsx`, `RecurringBlackoutRulesSettings.tsx`) and their tests were merged into `BlackoutSettings.tsx` + `BlackoutSettings.test.tsx`; `SystemSettingsTab.tsx` renders the single section; the E2E `SystemConfigurationPage.blackoutPeriodsSection` getter was retargeted. **Rationale for the reversal**: D4's product constraint is "separate entity, *unified evaluation*, one-off & recurring are distinct concepts" — VF-2 keeps them distinct (two Add buttons, two dialogs, date-range vs recurrence-summary text) while sharing the Description + Actions columns and one box, which is the lower-real-estate management surface the user wanted. The original "two sections would entangle two form shapes" worry does not apply: the two Add/Edit *dialogs* remain separate; only the read grid and the section frame are unified. Verified live (screenshot regeneration deferred to a clean-backend `/update-docs` run pending user confirmation).
