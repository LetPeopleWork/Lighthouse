# Bug #5586 — RCA context for DELIVER

ADO Bug #5586, child of Epic #5459. Title: *"GetLikelihood reports 100% on no evidence, and
over-reports for target dates between buckets"*. Deferred deliberately by Epic 5459 DDD-7 so the
multi-team work would not move single-team numbers without their own tests.

Subject: `Lighthouse.Backend/Lighthouse.Backend/Models/Forecast/ForecastBase.cs:70-95`.

## The two defects

**Defect 1 — no evidence reported as total confidence.** `trialCounter == 0` falls to `return 100`.
The predicate conflates "no trial finished by the target" with "no trials exist"; it was written to
avoid the division by zero and `100` was picked as the fallback.

**Defect 2 — targets between buckets over-report.** `trialCounter += simulation.Value` runs *before*
`if (simulation.Key >= daysToTargetDate) break`, so the first bucket at-or-after the target is always
credited. Confirmed by execution, not by reading:

| fixture | call | actual | correct |
|---|---|---|---|
| `{3:1,4:2,5:3,6:2,7:1,9:1}` | `GetLikelihood(8)` | 100 | 90 |
| `{3:1,4:2,5:3,6:2,7:1,9:1}` | `GetLikelihood(0)` / `(2)` | 10 / 10 | 0 / 0 |
| `{5:5000,40:5000}` | `GetLikelihood(4)` | 50 | 0 |
| `{5:5000,40:5000}` | `GetLikelihood(6)` | 100 | 50 |
| `{0:0}` sentinel, `{}` empty | any | 100 | Defect 1 |

The generalisation, which is the load-bearing fact:

> `GetLikelihood(t)` returns `CDF(min{k ∈ keys : k ≥ t})`, not `CDF(t)` — correct **only** when `t` is
> itself a bucket key or `t > maxKey`.

Why it survived to now: single-team histograms are dense (`ForecastService.cs:120-127` increments the
day by 1), so `t` is usually a key. `CompletionHistogram.DistributeByLargestRemainder`
(`CompletionHistogram.cs:66-69`) emits only days with `trials > 0`, so every multi-team
`AggregatedWhenForecast` and every `DeliveryCompletionForecast` histogram is **sparse** — and the
delivery target is an arbitrary working-day count. That is where the error is unbounded.

## Root causes

- **A — the unit test that owns the method never uses a non-key target.** `WhenForecastTest.cs:34-40`
  parametrises 3, 4, 5, 6, 7 (every one a key of its own fixture) and 12 (beyond `maxKey` 9). Both
  branches agree across 100 % of the asserted domain. Mutation testing cannot cover the gap either:
  add-then-break versus break-then-add is not a mutation operator.
- **B — every consumer that noticed compensated outside the primitive.** `Feature.cs:118-121`,
  `Delivery.cs:72-75`, `AggregatedWhenForecast.cs:17-19`, `DeliveryJointForecastTest.cs:99-102` — four
  guards, no fix, and nothing stopping a new caller from depending on the branch. One already does:
  `ForecastController.cs:104-107`.
- **C — a day-domain operation sits on a direction-agnostic base.** `HowManyForecast.cs:7,12` passes a
  **descending** comparer and its key is an item count, so a naive `key <= t` fix inverts it
  (verified: keys materialise `[8,5,3]`, `GetLikelihood(0..8) ≡ 70`). `GetLikelihood` is never called
  on a `HowManyForecast` in production, so the inversion is latent — which is exactly why a wrong fix
  would go unnoticed.

## Approved fix

Express the threshold through the injected comparer rather than a literal `<=`. ASC gives `k ≤ t`;
DESC gives `k ≥ N`, which corrects `HowManyForecast` for free and matches what its `GetProbability`
already means.

```csharp
if (TotalTrials == 0) { return 0; }

var keyOrder = comparer ?? Comparer<int>.Default;   // protected ForecastBase() leaves it null
var trialCounter = 0;

foreach (var simulation in SimulationResult)
{
    if (keyOrder.Compare(simulation.Key, threshold) > 0) { break; }
    trialCounter += simulation.Value;
}

return 100 / (double)TotalTrials * trialCounter;
```

`comparer ?? Comparer<int>.Default` is required: the `protected ForecastBase()` ctor at
`ForecastBase.cs:8` leaves the field null and the `SortedDictionary` at `:44` silently falls back to
the same default. The `daysToTargetDate < 0` guard becomes redundant under ascending order and was
wrong under descending; dropping it keeps `ForecastServiceTest.cs:159` green.

## Maintainer decisions (2026-07-29)

1. **Both defects fixed together**, plus the `ForecastController` guard.
2. **Snapshot step accepted silently.** Persisted `DeliveryMetricSnapshot.LikelihoodPercentage` rows
   carry pre-fix numbers and are forward-only (ADR-048/049/050), so `DeliveryPredictabilityChart` will
   show a step at deploy. No chart annotation, no release-note copy, no backfill.
3. **Manual forecast reports "cannot forecast", not 0 %.** `ManualForecastDto.Likelihood` becomes
   `double?` — the same nullable-carrier move ADR-112 made for delivery likelihood. Frontend renders
   null as "cannot forecast" and shows a genuine 0 % honestly, replacing the current
   `manualForecastResult.likelihood > 0` gate at `ManualForecaster.tsx:407`, which would otherwise
   hide the newly-common legitimate zero. CLI/MCP clients need their version bump per the
   Lighthouse-Clients rule.

## Files

**Production**
1. `Models/Forecast/ForecastBase.cs:70-95` — the fix; rename the parameter off `daysToTargetDate`.
2. `Models/Delivery.cs:72-75` — delete the `// Stryker disable once all: equivalent while Bug #5586
   stands` block. `DeliveryJointForecastTest.cs:96-118` then kills that mutant with no new test.
   **Leave `Delivery.cs:64-67` alone** — guard 2's annotation is not #5586-dependent.
3. `API/ForecastController.cs:104-107` — skip the assignment when `whenForecast.TotalTrials == 0`.
4. `API/DTO/ManualForecastDto.cs:9` — `double` → `double?`.
5. Frontend: `models/Forecasts/ManualForecast.ts`, `pages/Teams/Detail/ManualForecaster.tsx:407,416`.
6. Stale comments to trim, not expand: `Models/Feature.cs:118-119`,
   `Models/Forecast/AggregatedWhenForecast.cs:17-19`.

**Tests — two assert the defect and must change**
7. `Tests/Models/Forecast/AggregatedWhenForecastTest.cs:204` — `GetLikelihood(0) Is.EqualTo(100)` →
   `Is.Zero`. Keep the percentile half at `:199-202` untouched; that invariant is legitimate.
8. `Tests/Services/Implementation/Forecast/ForecastServiceTest.cs:162-172`
   `When_FixedThroughput_TargetDateToday_ReturnLikelihood` — `minKey` is 13, so 35 items cannot finish
   in zero days. Rename and assert `Is.Zero`.
9. `Tests/Models/FeatureTest.cs:29-34` — the XML doc claiming both branches "agree by accident"
   becomes false.

**Tests — must add (this is the prevention, do not drop it)**
10. `Tests/Models/Forecast/WhenForecastTest.cs:34-40` — non-key targets: `(8, 90)`, `(2, 0)`, `(0, 0)`.
    Without these the fix is unpinned and root cause A stays open.
11. New `HowManyForecast` likelihood test pinning at-least-N semantics (e.g. `{3:10,5:20,8:70}`,
    `GetLikelihood(5) == 90`) so a future "simplification" to `key <= t` fails loudly.

**Must re-run, cannot be settled by reading**
12. `Lighthouse.EndToEndTests/tests/specs/teams/TeamsDetail.spec.ts:26-30` — `howMany=20`, target
    +14 days, `expect(likelihood).toBeGreaterThan(0)`. Runs the unguarded controller path against demo
    throughput; if day 14 sits below `minKey` it passes today only because of Defect 2.

**Verified green under the fix, no action** — `DeliveryCompletionForecastTest` (`:73,94-96,142,231`),
`DeliveryJointForecastTest`, `FeatureUnknownForecastTest`, `FeatureMissingForecastRowTest`,
`InstanceDayAnchorEntityTest` (`:55,58`), `MultiTeamJointForecastDeliveryIntegrationTest` (`:40,44`),
`RecurringBlackoutRulesDeliveryIntegrationTest:94`, `ForecastServiceTest:159,206`, and the whole
frontend Vitest surface (all likelihood values there are hand-written DTO fixtures).

## Verification note

Dense single-team forecasts barely move — `GetLikelihood(30)` was already exact at 66.49. Verify
against a **sparse multi-team** fixture, or the smoke test will look identical either way.
