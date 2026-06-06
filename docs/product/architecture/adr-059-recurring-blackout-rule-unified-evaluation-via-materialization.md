# ADR-059: Recurring blackout rules reach the unified evaluation by materializing into synthetic `BlackoutPeriod` day-instances behind the existing `GetAll()` fetch seam — not by generalizing every consumer signature

## Status

Accepted — 2026-06-06 (DESIGN wave, feature `recurring-blackout-events`, ADO Epic 4577). Interaction mode = PROPOSE. The pivotal decision (where the union of one-off + recurring days is assembled, and in what shape it reaches the downstream seams) is surfaced for user confirmation below. Cross-references **ADR-058** (the shipped #4974 day↔date shift this feature feeds) and **ADR-060** (the `RecurringBlackoutRule` entity + weekday storage).

## Context

Epic 4577 adds a `RecurringBlackoutRule` entity (weekday set + every-X-weeks interval + concrete start + optional open-ended end) alongside the shipped one-off `BlackoutPeriod`. Locked decision **D4** requires *unified evaluation*: a recurring-rule day must be **indistinguishable downstream** from a one-off blackout day, so that

- the shipped #4974 forward day↔date shift (`BlackoutDaysExtensions.ProjectWorkingDays` / `CountWorkingDays`, ADR-058 A1),
- the historical-throughput stripping (`TeamMetricsService.FilterBlackoutDaysFromRunChart`),
- the chart overlays (`AnnotateBlackoutDays` → `blackoutDayLabels`),

all consume recurring-rule days **with no per-surface change** (D7: Monte Carlo and the shift logic itself are untouched — recurring rules only *widen the blackout-day set* those consumers already read).

### The decisive grounding read

The blackout-day set is fetched and threaded inward at **13 call sites** across 8 controllers/services, every one of them written as a variant of:

```
var blackoutPeriods = blackoutPeriodRepository.GetAll().ToList();   // → IReadOnlyList<BlackoutPeriod> / IEnumerable<BlackoutPeriod>
```

(`ForecastController` ×2, `DeliveriesController`, `DeliveryRulesController`, `FeaturesController`, `TeamMetricsController` ×3, `PortfolioMetricsController` ×5, `TeamController`, `TeamsController`, `WriteBackTriggerService`, `TeamMetricsService` ×2, `DeliveryMetricSnapshotRecordingHandler`.)

Every one of these then passes the list into the **pure** `BlackoutDaysExtensions` helpers, whose entire contract is expressed over `BlackoutPeriod`'s `Start`/`End` `DateOnly` range:

- `GetBlackoutDayIndices(periods, start, end)` / `IsBlackoutDay(periods, date)` / `HasOverlapWithDateRange` / `AnnotateBlackoutDays` take `IEnumerable<BlackoutPeriod>`;
- `ProjectWorkingDays(periods, start, n)` / `CountWorkingDays(periods, start, target)` take `IReadOnlyList<BlackoutPeriod>`.

A `BlackoutPeriod` with `Start == End == d` is, to every one of these helpers, exactly "day `d` is a blackout day." This is the lever: **a recurring rule occurrence is representable as a single-day `BlackoutPeriod`.** If the recurring days are materialized into such instances and unioned into the list *before* it reaches any helper, the entire downstream — all 13 seams, the #4974 shift, the stripping, the overlays — is **byte-for-byte unchanged**. D4 (indistinguishable) and D7 (shift untouched) fall out by construction rather than by 13 careful edits.

The remaining design question is **where** the union is assembled and **what shape** it takes — which is the pivotal decision.

## Decision

**Option C (chosen, recommended): materialize recurring-rule occurrences into synthetic single-day `BlackoutPeriod` instances and assemble the union behind the existing service fetch seam, so the downstream `IReadOnlyList<BlackoutPeriod>` shape is unchanged.**

Concretely:

1. **Expansion (pure)** — recurring days come from a pure function (ADR-060 §expansion): `RecurringBlackoutRule.ExpandToBlackoutDays(windowStart, windowEnd) → IEnumerable<BlackoutPeriod>` yielding one single-day `BlackoutPeriod { Start = End = matchedDay, Description = <rule summary> }` per matching day in the window. No I/O, the window is passed in (every consumer already has one — forecast horizon / chart date range; open-ended rules are *bounded by the consumer's window*, never expanded to infinity).

2. **Union behind the fetch seam** — promote the existing `IBlackoutPeriodService.GetAll()` (today a thin pass-through over `IRepository<BlackoutPeriod>`) into the **single assembly point** that returns one-off periods **unioned with** materialized recurring days. A new method:

   ```
   IReadOnlyList<BlackoutPeriod> IBlackoutPeriodService.GetEffectiveBlackoutDays(DateTime windowStart, DateTime windowEnd)
   ```

   fetches `blackoutPeriodRepository.GetAll()` **and** `recurringBlackoutRuleRepository.GetAll()` once, expands each rule over `[windowStart, windowEnd]`, and returns the concatenated `IReadOnlyList<BlackoutPeriod>`. The 13 consumers migrate from `blackoutPeriodRepository.GetAll().ToList()` to `blackoutPeriodService.GetEffectiveBlackoutDays(windowStart, windowEnd)` — a **same-shape swap** (still `IReadOnlyList<BlackoutPeriod>`), threading the window each consumer already owns. No helper signature changes; no DTO change; no model change.

3. **The window is always bounded by the consumer.** A forecast has a horizon; a chart has a date range; a delivery/feature projection has a target/percentile horizon; write-back has a days-to-completion. Open-ended rules (`End == null`) are expanded only across the *consumer's* window, so the synthetic-period count is O(window-days), never unbounded. ADR-060 pins the anchoring math.

This keeps `BlackoutPeriod` as the **single lingua franca** of blackout-day evaluation (the shipped helpers, stripping, overlays never learn that "recurring" exists), and confines all new logic to (a) the pure expansion (ADR-060) and (b) one unifying service method.

### The pivotal decision the human must confirm

**Where is the one-off + recurring union assembled, and in what shape does it reach the 13 downstream seams?** Three shapes, all honour the no-DI-in-models invariant and D4:

- **Option A — materialize + union *at each consumer***: each of the 13 fetch points additionally fetches the rules and expands them locally. Smallest conceptual change to the helpers (zero) but **duplicates the union + the window plumbing 13 times** — every consumer must learn to fetch a second repo and call the expansion. High repetition, 13 edit sites, drift risk (a missed site silently omits recurring days — the exact US-03 failure mode). **Rejected** as a DRY/consistency violation.

- **Option B — generalize the seam behind an abstraction** (`IBlackoutDaySource` / `IsBlackoutDay(date)` interface that both one-off and recurring implement; promote the `BlackoutDaysExtensions` statics per ADR-058's explicit "if recurring rules arise, then promote behind an interface" hint). Conceptually cleanest, but **touches every helper signature and every one of the 13 seams** (`IEnumerable<BlackoutPeriod>` → `IBlackoutDaySource` everywhere), re-opens the shipped #4974 A1 contract (`ProjectWorkingDays`/`CountWorkingDays` parameter types), and forces re-verification of the whole #4974 surface. Large blast radius for a feature whose locked constraint (D7) is "don't touch the shift." **Rejected for v1** — the YAGNI threshold ADR-058 named ("*then* promote") is real, but materialization clears the bar at a fraction of the blast radius; promote behind an interface only if a future rule type cannot be expressed as day-instances.

- **Option C (recommended, chosen) — materialize + union once behind the `IBlackoutPeriodService` fetch seam**, returning the unchanged `IReadOnlyList<BlackoutPeriod>` shape. One new union method, 13 same-shape call-site swaps, **zero helper/DTO/model signature changes**, the #4974 A1 contract untouched. The "single fetch, pass materialised list inward" house pattern (ADR-058 DDD-2, `GetBlackoutAwareThroughputForTeam` line 162) is preserved — the union just happens to fetch *two* repos instead of one before materializing.

**Recommendation: Option C.** It is the smallest blast radius that still keeps the union in exactly one place (the failure mode of Option A is a missed seam; the failure mode of Option B is re-touching the shipped shift). **This is the single decision most needing human confirmation** — specifically whether the union lives behind `IBlackoutPeriodService.GetEffectiveBlackoutDays(window)` (chosen) versus generalizing the seam behind an `IBlackoutDaySource` interface (Option B) for a cleaner long-term model at a larger immediate cost. Mirrors how ADR-058 surfaced its A1/A2 choice.

### Who fetches, and the window contract (mirrors ADR-058 D9 fetch-once)

| Consumer (current fetch) | Window it already owns | After ADR-059 |
|---|---|---|
| `ForecastController.RunManualForecastAsync` / by-date | today → forecast horizon / target date | `GetEffectiveBlackoutDays(today, horizon)` |
| `DeliveriesController` → `DeliveryWithLikelihoodDto.FromDelivery` | today → delivery date / percentile horizon | `GetEffectiveBlackoutDays(today, deliveryDate)` |
| `FeaturesController` / `DeliveryRulesController` / `PortfolioMetricsController` / `TeamMetricsController` (feature/portfolio reads) | today → percentile horizon | `GetEffectiveBlackoutDays(today, horizon)` |
| `TeamMetricsService.GetBlackoutAwareThroughputForTeam` / chart-annotation reads | chart `startDate`→`endDate` (often historical) | `GetEffectiveBlackoutDays(startDate, endDate)` |
| `DeliveryMetricSnapshotRecordingHandler` | snapshot horizon | `GetEffectiveBlackoutDays(today, horizon)` |
| `WriteBackTriggerService` | today → days-to-completion | `GetEffectiveBlackoutDays(today, today.AddDays(daysToCompletion))` |

One fetch of each repo per inbound request; expansion is O(window-days) per rule; no N+1 (D6/#4974 D9: the set is global). The historical-strip cache key note from ADR-058 (cache keyed on date-range, not on the rule/period set revision) carries over unchanged — adding a rule mid-window does not retroactively re-strip already-cached throughput; **acceptable and out of scope** (rules, like periods, are planned in advance).

## Alternatives Considered

(Options A and B above are the considered alternatives; both rejected with rationale. A = per-consumer duplication, 13 edit sites + drift; B = generalize the seam, large blast radius re-touching the shipped #4974 shift, deferred per ADR-058's own YAGNI threshold.)

### Option D — a new injectable `IRecurrenceExpander` / `IBlackoutDayProvider` domain service

Introduce a DI service that owns expansion + union. **Rejected (over-engineered, same stance as ADR-058 Option C).** The expansion is a *pure* function with no collaborators to mock (window + rule in, days out); the union is one `Concat`. `IBlackoutPeriodService` already exists, already wraps the repo, and is already the natural home for "give me the effective blackout days." A new interface + impl + DI registration buys nothing over one method on the service that already exists. Promote to a service only if expansion ever acquires hidden I/O or a clock that must be swappable (it does not — the clock is the caller's window).

## Consequences

**Positive**
- **D4 indistinguishable + D7 shift-untouched fall out by construction.** A recurring day IS a single-day `BlackoutPeriod` once materialized — `IsBlackoutDay`/`GetBlackoutDayIndices`/`ProjectWorkingDays`/`CountWorkingDays`/`AnnotateBlackoutDays` cannot tell the difference because there is no difference. AC US-01/AC3 (recurring day ≡ one-off day) is a direct equality assertion on the helper outputs.
- **Zero change to the shipped #4974 A1 contract** (`WhenForecastDto` ctor, `Feature.GetLikelhoodForDate`, `Delivery.CalculateMetrics`, `FromDelivery`, `ProjectWorkingDays`/`CountWorkingDays` signatures). The new feature does not re-open ADR-058's shared contracts.
- **Union in exactly one place** (`GetEffectiveBlackoutDays`). A new forecasting surface added later inherits recurring-rule awareness for free by calling the service method instead of the raw repo — and an ArchUnitNET/grep rule forbids the raw `blackoutPeriodRepository.GetAll()` on the forecast/chart path, catching the Option-A drift failure mode statically.
- **No-rule + no-period regression is byte-identical** (inherits #4974 D6): with no rules, `GetEffectiveBlackoutDays` returns exactly the one-off periods; with neither, the empty list ⇒ identity math.
- **Bounded synthetic-period count**: O(window-days) per open-ended rule, expanded only across the consumer's existing window — no unbounded materialization.

**Negative / accepted**
- **13 same-shape call-site swaps** (`blackoutPeriodRepository.GetAll().ToList()` → `blackoutPeriodService.GetEffectiveBlackoutDays(window)`). Bounded, mechanical, same return shape; each site already owns its window. Grep-first per CLAUDE.md; an ArchUnitNET rule pins that the forecast/chart path no longer calls the raw repo.
- **`IBlackoutPeriodService` gains a dependency on the recurring-rule repo.** It is a DI service (not a model), so this respects the ports-and-adapters arrow; it already depends on `IRepository<BlackoutPeriod>`.
- **Synthetic periods carry a non-persisted `Id` (0).** They never round-trip through the repo (expansion is read-only, in-memory); they exist only inside a single request's evaluation. A guard test asserts expansion output is never `Add()`-ed to the repo.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The one-off + recurring union exists in exactly one place (`IBlackoutPeriodService.GetEffectiveBlackoutDays`) — no forecast/chart consumer calls `blackoutPeriodRepository.GetAll()` directly after this feature | NUnit/grep + ArchUnitNET test: classes on the forecast/chart path depend on `IBlackoutPeriodService.GetEffectiveBlackoutDays`, not on `IRepository<BlackoutPeriod>.GetAll()` for the eval path |
| A recurring-rule day and a one-off `BlackoutPeriod` day produce identical `IsBlackoutDay`/`GetBlackoutDayIndices`/`ProjectWorkingDays` results (D4, US-01 AC3) | NUnit parity test: expand a rule to a day-set, build the same day-set as one-off periods, assert every helper returns equal results |
| Expansion is pure (no `IRepository<>`, `DbContext`, `HttpClient`, `ILogger`, `DateTime.UtcNow`/`Today` — window/clock passed in) | NUnit static-inspection test on the expansion function |
| Synthetic (expanded) periods never persist | NUnit: expansion output has `Id == 0` and `GetEffectiveBlackoutDays` performs no `Add`/`Save` |
| The shipped #4974 A1 contract signatures are unchanged | grep test: `ProjectWorkingDays`/`CountWorkingDays`/`FromDelivery`/`GetLikelhoodForDate` parameter lists match the ADR-058 shape |
| No-rule regression byte-identical (inherits #4974 D6) | NUnit golden test: no rules ⇒ `GetEffectiveBlackoutDays(w) ≡ blackoutPeriodRepository.GetAll()` |

## Clients consistency verdict

A **NEW endpoint family** `api/{v1|latest}/recurring-blackout-rules` is introduced (ADR-060 / the new controller). Per CLAUDE.md the CLI/MCP clients, **if they surface blackout-period CRUD**, need a matching wrapped method that is **version-gated** (`FEATURE_REQUIRES_SERVER_NEWER_THAN`, pinned strictly newer than the last released Lighthouse version; dev/unparseable versions never blocked). If the clients do not currently wrap one-off blackout-period CRUD, the recurring-rule client method is **deferred — recorded explicitly in the clients repo, not silently skipped** (matches the feature-delta cross-cutting checklist). The *evaluation-path* changes (recurring days flowing into existing forecast/delivery/chart endpoints) change only the **value** of existing fields — no client gate, exactly as #4974.

## Premium-gating verdict

`BlackoutPeriod` CRUD and `ComputeBlackoutAwareThroughput` carry **no premium gate** on the read/eval path (verified in ADR-058: `GetBlackoutAwareThroughputForTeam` does not reference `ILicenseService`). Recurring rules therefore **inherit no premium gate on `GetEffectiveBlackoutDays`** — once configured, rules act on every forecast/chart surface for every viewer, exactly like one-off periods. Only the **writes** (POST/PUT/DELETE on the new controller) are gated `[LicenseGuard(RequirePremium=true)]` + `[RbacGuard(SystemAdmin)]` (D5). GET (list rules) is open. No new permission.
