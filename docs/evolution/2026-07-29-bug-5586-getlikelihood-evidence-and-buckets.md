# Bug #5586 — `GetLikelihood` reported confidence the simulation did not support

**Shipped** 2026-07-29 · commits `202be9980` … `1f72ce659` · CI green on run 30483742448 ·
Epic #5459's last open child.

## What was wrong

`ForecastBase.GetLikelihood(int)` carried two independent defects, both deferred deliberately by
Epic 5459's DDD-7 so the multi-team work would not move single-team numbers without its own tests.

**Defect 1 — no evidence reported as total confidence.** The method ended `return 100` when
`trialCounter == 0`. Not one simulated trial finishing by the target is the strongest possible
statement that the date is unreachable; it answered with the maximum.

**Defect 2 — targets between buckets over-reported.** The loop banked a bucket's mass *before*
testing the break, so the answer was `CDF(min{k ∈ keys : k ≥ t})` rather than `CDF(t)`. The
generalisation is the load-bearing part:

> `GetLikelihood(t)` was correct **only** when `t` was itself a bucket key, or `t > maxKey`.

Measured, not reasoned: `{5:5000, 40:5000}` at day 6 returned **100** where 50 is the truth;
`{3:1,4:2,5:3,6:2,7:1,9:1}` at day 8 returned 100 against 90, and at day 0 or 2 returned 10 against 0.

## Why it survived this long

Single-team histograms are dense — `ForecastService` increments the simulated day by 1 — so `t` is
almost always a key and the two branches agree. But `CompletionHistogram.DistributeByLargestRemainder`
emits only days with `trials > 0`, so **every multi-team `AggregatedWhenForecast` and every
`DeliveryCompletionForecast` histogram is sparse**, and the delivery target is an arbitrary
working-day count. That is precisely where the error was unbounded, and it is the code path Epic 5459
had just built.

Two deeper causes, both worth remembering:

- **The unit test that owned the method never used a non-key target.** `WhenForecastTest`
  parametrised 3, 4, 5, 6, 7 — every one a key of its own fixture — plus 12, beyond `maxKey`. Both
  the buggy and the correct implementation agree across 100 % of that domain, so the suite was
  structurally incapable of failing. Mutation testing could not have helped either: add-then-break
  versus break-then-add is not a mutation operator.
- **Every consumer that noticed compensated outside the primitive.** `Feature`, `Delivery`,
  `AggregatedWhenForecast` and a delivery test each carried a guard-plus-comment explaining that they
  existed to avoid this branch. Four independent compensations, no fix — and nothing stopping a fifth
  caller from depending on it. One already did: `ForecastController.RunManualForecastAsync` was
  unguarded, so a team with no throughput was told it had a 100 % chance.

## The fix

The threshold is expressed through the injected `IComparer`, not a literal `key <= threshold`:

```csharp
if (TotalTrials == 0) { return 0; }

var trialCounter = 0;
foreach (var simulation in SimulationResult)
{
    if (KeyOrder.Compare(simulation.Key, threshold) > 0) { break; }
    trialCounter += simulation.Value;
}
return 100 / (double)TotalTrials * trialCounter;
```

`HowManyForecast` passes a **descending** comparer over an item-*count* key, where the honest reading
is at-least-N. `Compare(k, t) <= 0` means `k ≤ t` ascending and `k ≥ t` descending, so one expression
serves both — and a "simplification" to the literal inverts HowMany silently. It is never called on a
`HowManyForecast` in production, so the inversion would have stayed latent; a test now pins it.

`KeyOrder` coalesces to `Comparer<int>.Default` because the `protected ForecastBase()` constructor
leaves the field null while the `SortedDictionary` silently applies that same default — the two must
agree, since the comparer decides the order the break walks.

Separately, `ManualForecastDto.Likelihood` became `double?` so the manual forecast can say "cannot
forecast" rather than fabricate a number. Falling to 0 would have been a different lie, and the
frontend's `likelihood > 0` render gate would have hidden the newly-common legitimate 0 % as well.

## Behaviour changes users will see

Delivery and feature likelihoods **move downward on real data** — that is the point. Persisted
`DeliveryMetricSnapshot.LikelihoodPercentage` rows carry pre-fix numbers and are forward-only
(ADR-048/049/050), so `DeliveryPredictabilityChart` steps at deploy. The maintainer accepted this
silently: no backfill, no chart annotation, no release-note copy.

## Gates

Backend 4136/4136 · frontend 3807/3807 · `dotnet build` and `pnpm build` zero warnings · Biome clean ·
5 Playwright specs green against a local instance · mutation **91.43 % backend / 91.30 % frontend**,
with all 9 `GetLikelihood` mutants killed including every flip of the break predicate. Adversarial
review APPROVED with one non-blocking caution (`TotalTrials` is public-settable and could disagree
with the histogram it describes — pre-existing).

## Things learned the expensive way

- **The E2E that looked most at risk was not.** `TeamsDetail.spec.ts` asserts the manual forecast is
  `> 0` against demo throughput at +14 days, on the one unguarded controller path. It could only be
  settled by running it. It passes — day 14 sits above `minKey`.
- **Stryker.NET silently ignores line-span `mutate` patterns.** Scoping to
  `ForecastBase.cs{72..94}` reported a clean **100 %** in which every tested mutant came from the one
  entry that had no span. Whole-file entries plus survivor triage is the only trustworthy shape.
- **StrykerJS can only run in-place in this repo** — its sandbox preprocessor calls
  `ts.parseConfigFileTextToJson`, which the installed TypeScript no longer exports. In-place mutates
  the real tree, and the default `disableTypeChecks` prepended `// @ts-nocheck` to 661 source files
  before a run OOMed. Set `disableTypeChecks: false`. Full detail in the feature's `mutation/results.md`.
- **The RCA's test inventory was one short.** `ForecastControllerTest` also asserted the defect via a
  bare `new WhenForecast()` stub; found by the crafter, not the analysis.
- **`forecastSchemas.ts` had `likelihood: z.number()`.** Nobody listed it. A null would have failed
  zod parse and surfaced as a generic API error rather than the cannot-forecast state — the
  shared-contract grep CLAUDE.md mandates is what caught it.

## Follow-ups left open

- **ADO #5586 not transitioned** — left Active at the maintainer's instruction.
- **Lighthouse-Clients** is not in this working tree. If its CLI/MCP surface deserialises the manual
  forecast's `likelihood` as a non-nullable number, the `double?` change is breaking and needs a
  version bump; if it forwards payloads verbatim per ADR-112, it is unaffected.
- **`ForecastBase.TotalTrials` is public-settable** and can disagree with `SimulationResult`'s actual
  mass. Pre-existing; the new zero-evidence guard reads it. Worth making internal or invariant-guarded
  when something else touches that file.
- **`ForecastController` asks `whenForecast.TotalTrials > 0`** — mild Feature Envy. The same predicate
  recurs in `Feature.cs` and `DeliveryCompletionForecast.cs`; a `ForecastBase.HasTrials` is only
  coherent applied at all three.
