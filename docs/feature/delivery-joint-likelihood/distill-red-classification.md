# DISTILL — RED classification (Story #5587, slice-01)

Run:

```
dotnet test Lighthouse.Backend.Tests --filter "FullyQualifiedName~ComonotonicCompletionDistributionTest|FullyQualifiedName~DeliveryCompletionForecastTest|FullyQualifiedName~DeliveryJointForecastTest|FullyQualifiedName~FeatureMissingForecastRowTest|FullyQualifiedName~DeliveryJointForecastIntegrationTest|FullyQualifiedName~DeliveryUnknownForecastDtoTest|FullyQualifiedName~DeliveryGrainSeamArchUnitTest"
```

Result 2026-07-29, with every `[Ignore]` temporarily stripped: **54 tests — 33 failed, 21 passed,
0 broken.** Build: 0 warnings. Full suite with the ignores in place: **3918 passed, 0 failed,
33 skipped**.

Every failure was classified before hand-off. `MISSING_FUNCTIONALITY` is the only correct RED; zero
tests failed on import, fixture or setup errors, and every scaffold failure reaches the scaffold body
(so the seam and the signature are right) rather than failing to resolve.

## Failing — 33 (all MISSING_FUNCTIONALITY)

### `ComonotonicCompletionDistributionTest` — 11, fixture-level `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `Min_*` (all 11) | MISSING_FUNCTIONALITY | `InvalidOperationException: __SCAFFOLD__ ComonotonicCompletionDistribution.Min is not implemented yet` — the scaffold is reached on every one |

The fixture that matters: `Min_TwoIdenticalContributors_ReturnsThatHistogramUnchanged`. Minimum leaves
an identical pair alone; `JointCompletionDistribution.Combine` on the same input squares it. A test
that passes for both operators is not coverage of `Min`.

### `DeliveryCompletionForecastTest` — 10, fixture-level `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `ContributingRows_*` (5) | MISSING_FUNCTIONALITY | `__SCAFFOLD__ DeliveryCompletionForecast.ContributingRows is not implemented yet` |
| `Build_*` (5) | MISSING_FUNCTIONALITY | `__SCAFFOLD__ DeliveryCompletionForecast.Build is not implemented yet` |

### `DeliveryJointForecastTest` — 6 of 12, per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `CalculateMetrics_EveryFeatureCannotBeForecast_ReportsUnknownRatherThanZeroPercent` | MISSING_FUNCTIONALITY | expected `null`, but was `0.0` — DDD-6 in full view: `likelihood >= 0` rejects every candidate and the "no governing feature" branch reports 0 % |
| `CalculateMetrics_DeliveryFinishedBetweenForecastRuns_MovesEveryPercentileDateToToday` | MISSING_FUNCTIONALITY | expected all dates `2026-07-29`, but was `2026-09-17` ×3 — DDD-9: the persisted rows still carry their full trials |
| `CalculateMetrics_TwoFeaturesOnSeparateTeams_HeadlineAndPercentileDatesComeFromTheJointHistogram` | MISSING_FUNCTIONALITY | expected `81.0 ± 0.001`, but was `90.0`; expected 85th `2026-08-18`, but was `2026-08-08` — the governing feature answering for the delivery, badge **and** chips |
| `CalculateMetrics_ContributingPairHasNoForecastRow_ReportsUnknownRatherThanASilentCertainty` | MISSING_FUNCTIONALITY | expected `null`, but was `90.0` with three percentile dates — C1/DDD-7: Beta's work silently assumed done |
| `CalculateMetrics_ContributingPairHasNoForecastRowAndNoTeamNavigation_StillReportsUnknown` | MISSING_FUNCTIONALITY | expected `null`, but was `90.0` — guard 4, the pair-grain backstop |
| `CalculateMetrics_DeliveryWithoutADate_KeepsReportingHundredPercentAndPublishesTheJointDates` | MISSING_FUNCTIONALITY | likelihood half already `100`; expected 85th `2026-08-18`, but was `2026-08-08` — the dates half is what is missing |

### `FeatureMissingForecastRowTest` — 3 of 6, per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `TeamsWithoutForecast_ContributingPairHasNoForecastRow_NamesThatTeam` | MISSING_FUNCTIONALITY | expected `["Beta"]`, actual sequence has 0 elements — the second DDD-8 clause does not exist yet |
| `CanBeForecast_ContributingPairHasNoForecastRow_IsFalse` | MISSING_FUNCTIONALITY | expected `False`, but was `True` |
| `GetLikelhoodForDate_ContributingPairHasNoForecastRow_IsUnknownRatherThanAlphasNumberAlone` | MISSING_FUNCTIONALITY | expected `null`, but was `100.0` — the feature answers with Alpha's distribution alone |

### `DeliveryUnknownForecastDtoTest` — 1 of 10 (the one added this wave), per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `FromDelivery_ContributingPairHasNoForecastRow_ReportsUnknownAndNamesThatTeam` | MISSING_FUNCTIONALITY | expected `null`, but was `100.0`; `TeamsWithoutForecast` expected `["Team Pulsar"]`, actual list has 0 elements |

### `DeliveryJointForecastIntegrationTest` — 2 of 4, per-test `[Ignore]`

| Test | Classification | Evidence |
|---|---|---|
| `GetDelivery_TwoFeaturesOnSeparateTeams_LikelihoodIsTheJointAcrossEveryFeature` | MISSING_FUNCTIONALITY | expected `81.0 ± 0.01`, but was `90.0` — **reached the HTTP port and deserialised the real DTO, so the wiring is proven and only the maths is missing** |
| `GetDelivery_TwoFeaturesOnSeparateTeams_PercentileDatesComeFromTheJointHistogram` | MISSING_FUNCTIONALITY | expected `2026-08-18`, but was `2026-08-08` |

## Passing — 21 (regression guards and anchors, must stay green)

| Test | Why it is green today and must stay green |
|---|---|
| `CalculateMetrics_DeliveryWithoutFeatures_ReportsZeroPercentAndNoDates` | AC-01.11. Same values, narrower reason after the change — guard 1 no longer swallows the all-un-forecastable case |
| `CalculateMetrics_OneFeatureCannotBeForecast_ReportsUnknownAndNoDates` | AC-01.10 / ADR-112 D8. The short-circuit is preserved exactly |
| `CalculateMetrics_EveryFeatureWasAlreadyFinishedAtTheLastForecastRun_ReportsHundredPercentForToday` | Guard 3 on the `{0: 0}` sentinel shape; today's path already agrees here |
| `CalculateMetrics_LateAndEarlyFeatureOnSeparateTeams_PercentileDatesAreNeverEarlierThanTheLatestFeature` | **AC-01.9**, the ADO #5435 regression re-asserted directly rather than through the deleted tie-break. The single most important guard in this set |
| `CalculateMetrics_OverdueDelivery_ReportsZeroPercentAndStillSaysWhenItWillLand` | A 0 % delivery must still publish dates, or `DeliveryMetricSnapshotRecordingHandler` reads the empty distribution as "no forecast" and the deliveries most in trouble silently stop reporting |
| `CalculateMetrics_ThreeWayFixture_HeadlineIsSeventyTwoAndEqualsTheGoverningBreakdownRow` | **GRAIN ANCHOR** — see the note below. Green under old code and new code by construction |
| `TeamsWithoutForecast_PairWithNoRemainingWorkAndNoForecastRow_IsNotNamed` | The exemption keys off remaining work, not off the absence of a forecast (AC-01.7) |
| `TeamsWithoutForecast_ContributingPairWithNoForecastRowAndNoTeamNavigation_IsNotNamed` | The dangling-name guard: DDD-8's new clause must drop unnameable teams, which is exactly why guard 4 is retained |
| `TeamsWithoutForecast_EveryContributingPairHasARow_StaysEmpty` | The negative case for both DDD-8 clauses |
| `Delivery_DoesNotReachForACompletionCombinatorDirectly` | ArchUnitNET. Green today because `Delivery` has no such dependency; it must still be green after DELIVER moves the combination into `DeliveryCompletionForecast` |
| `GetDelivery_JointRollup_LeavesTheDeliveryPayloadShapeUnchanged` | **AC-01.12**, the contract guard. 18 keys, byte-compatible with the CLI/MCP clients |
| `FromDelivery_JointRollup_AttachesNothingToTheChangeTracker` | The delivery read path is read-only over the entity graph; the carriers leave `Team`/`Feature` unset |
| `DeliveryUnknownForecastDtoTest` (9 pre-existing) | ADR-112 D8 at delivery grain, unchanged by this slice |

### The grain anchor, stated plainly

`CalculateMetrics_ThreeWayFixture_HeadlineIsSeventyTwoAndEqualsTheGoverningBreakdownRow` passes under
today's code **and** under the new maths. That is not an oversight: on the DISCUSS fixture Checkout
governs entirely and Reporting carries slack, so the correct joint (.720) **coincides** with today's
governing-feature answer — the AC-01.4 equality corner, and the reason slice 03's copy must not promise
"always lower". It is kept because it still fails loudly on a wrong *grain* (68.4 for a feature-CDF
product, 51.84 for a team term taken off `feature.Forecast`). The old-vs-new discrimination lives in
`CalculateMetrics_TwoFeaturesOnSeparateTeams_...` and in the two `DeliveryCompletionForecastTest`
fixtures, not here. Same standing as Story #5569's
`FeatureForecast_ConstantThroughputTeams_MatchesTheSlowestTeam`.

## Regression guards that DELIVER must REPAIR, not treat as a wrong-reason RED

Two pre-existing tests in `Lighthouse.Backend.Tests/Models/DeliveryTest.cs` build `FeatureWork` with no
`Team` and forecasts with no `TeamId`:

- `CalculateMetrics_MultipleFeaturesTiedOnLikelihood_WhenDistributionReflectsLatestCompletingFeature`
- `CalculateMetrics_FeatureWithZeroLikelihood_StillGovernsAndReportsForecastDates`

Under the new enumeration (`FeatureWork.Where(RemainingWorkItems > 0)` LEFT JOIN `Forecasts`, matched
on `f.Team?.Id ?? f.TeamId`) those pairs join to nothing, so guard 4 fires and both tests will go red
with `LikelihoodPercentage = null`. **That is a fixture defect, not a behaviour regression** — the
repair is to wire a real `Team` on the `FeatureWork` and the matching `TeamId` on the forecast
(`CreateForecastCompletingInDays` also needs the team). Both are already covered by direct replacements
in `DeliveryJointForecastTest` (`..._PercentileDatesAreNeverEarlierThanTheLatestFeature` and
`..._OverdueDelivery_ReportsZeroPercentAndStillSaysWhenItWillLand`), so the first one — which asserts
the *governing feature's* dates and is the behaviour AC-01.9 deletes — can also simply be removed once
its replacement is green. DISTILL deliberately did not touch them: they pass today, and separating the
fixture repair from the change that forces it would make the DELIVER diff harder to read.

## State at hand-off

The 33 RED tests carry `[Ignore("RED until Story #5587 ...")]` so the committed tree stays green —
`ComonotonicCompletionDistributionTest` and `DeliveryCompletionForecastTest` at fixture level (every
test in them is RED), the other 13 per test. **DELIVER's first act is to remove those attributes.** The
RED they encode is the entry gate, not a backlog item.

Scaffold detection: `grep -rn "__SCAFFOLD__" Lighthouse.Backend/Lighthouse.Backend/` returns 9 hits
across three files today and must return **zero** when DELIVER is done.

---

# DISTILL — RED classification (Story #5587, slice-02)

Run:

```
dotnet test Lighthouse.Backend.Tests --filter "FullyQualifiedName~DeliverySufficiencyDtoTest"
```

Result 2026-07-29, with every `[Ignore]` temporarily stripped: **9 tests — 3 failed, 6 passed,
0 broken.** With the ignores in place: 6 passed, 3 skipped. Backend build: 0 warnings. Full suite
after this slice: **3924 passed, 0 failed, 37 skipped** (33 from slice-01, 3 from this slice, 1 from
a concurrent slice-01 edit landing while this wave ran).

**No scaffolds.** Slice-02 needs none: every observable it asserts already exists on
`DeliveryWithLikelihoodDto`, so nothing had to be stubbed for the tests to compile, and
`grep -rn "__SCAFFOLD__" Lighthouse.Backend/Lighthouse.Backend/` still returns slice-01's 9 hits
across three files and no more. `DeliveryMetricsProjection` was deliberately **not** given its
`HasSufficientData` field in this wave — see the grain note below.

## Failing — 3 (all MISSING_FUNCTIONALITY)

| Test | Classification | Evidence |
|---|---|---|
| `FromDelivery_ThinHistoryOnAFeatureThatIsNotTheLeastLikely_ReportsInsufficientData` | MISSING_FUNCTIONALITY | expected `False`, but was `True` — AC-02.4's visible delta in full view: the least-likely feature has ample history, so the delivery shows no warning at all while publishing a number that rests on the other feature's thin history. The two `FeatureLikelihoods` assertions in the same scope PASS, which is the point: the per-feature marginals are unchanged |
| `FromDelivery_EveryFeatureIsFinished_ReportsSufficientDataRatherThanTheSentinelDefault` | MISSING_FUNCTIONALITY | expected `True`, but was `False` — the landmine, stated at its sharpest. A finished delivery reads the flag off the `{0: 0}` sentinel whose `bool` was never assigned and reports "not enough data" on work that is DONE. The `LikelihoodPercentage == 100` half of the same scope passes |
| `FromDelivery_UnforecastableDeliveryWithThinHistoryElsewhere_ReportsBothSignals` | MISSING_FUNCTIONALITY | expected `False`, but was `True` — AC-02.5. The un-forecastable feature drops out of the ranking (`null >= 0` is `false` in C#) and the second-least-likely feature answers for sufficiency, so the thin history hides behind the unknown state instead of composing with it. The `LikelihoodPercentage is null` half passes |

## Passing — 6 (regression guards, must stay green)

| Test | Why it is green today and must stay green |
|---|---|
| `FromDelivery_FinishedFeatureAlongsideAWellSupportedOne_StillReportsSufficientData` | **AC-02.2, the fixture the exemption exists for.** Green today only by accident — a finished feature sorts to likelihood 100 and is therefore never the least likely one. Drop the remaining-work exemption from the new AND and this goes false, which would put a "not enough data" indicator on every delivery containing a completed feature |
| `FromDelivery_FinishedFeatureAlongsideAThinOne_StillReportsInsufficientData` | The exemption must not OVER-exempt. An implementation that returns true whenever any feature is finished passes the row above and fails here |
| `FromDelivery_EveryContributingFeatureHasAmpleHistory_KeepsReportingSufficientData` | The negative control. AND can only flip `true → false` (D6 point 4), so a well-supported delivery must keep reading true |
| `FromDelivery_ThinHistoryOnTheLeastLikelyFeature_KeepsReportingInsufficientData` | The direction-of-change guard: the one case today's rule already gets right must not regress. AND never newly HIDES a warning |
| `FromDelivery_DeliveryWithoutFeatures_ReportsSufficientData` | The other empty-AND case, and the only one today's `?? featureLikelihoods.All(...)` fallback reaches. Deleting `GetLeastLikelyFeature` deletes that fallback, so this pins the VALUE rather than the expression |
| `FromDelivery_StaleDoneRowInsideALiveFeature_IsStillCountedByTheFeatureGrainAnd` | DDD-2's named nuance. Feature grain (what AC-02.1 words, and what `f.Forecasts.All(...)` computes) includes a stale done row inside a live feature; row grain would exclude it and report true. Pinned so a future reader does not "unify" the two grains without noticing they are different sets |

## Why every slice-02 assertion is at the DTO grain

DDD-2 routes the value through a new `DeliveryMetricsProjection.HasSufficientData` field. DISTILL wrote
**no** test against that field and did **not** add it, for a reason worth recording rather than
leaving as an omission.

AC-02.1 through AC-02.6 all word their subject as `DeliveryWithLikelihoodDto.HasSufficientData`, and
the DTO is where the value is genuinely computed *today* — so every one of the nine fixtures above
discriminates the old rule from the new one. The projection field is an internal carrier with no
behaviour and no wire surface, chosen so `FromDelivery` can reach `delivery.Features`. Adding it in
DISTILL would have forced a default: `= true` makes every entity-grain "sufficient" assertion pass
**by default value** rather than by computation, and `= false` does the same for every "insufficient"
one. Either way half the suite would be green for the wrong reason. Pinning the route rather than the
answer is also an AST-shape test, which slice-01's DT-10 already refused for the deleted selectors.

DELIVER adds the field as part of the implementation. Nothing here constrains it not to.

## Regression guards that DELIVER must REPAIR, not treat as a wrong-reason RED

**`DeliveryUnknownForecastDtoTest.FromDelivery_ForecastableFeatureIsSufficientButAnUnknownOneIsNot_TheGoverningFeatureStillAnswers`**
(`:119-130`) asserts `HasSufficientData` is **True** for a delivery holding a forecastable feature with
ample history (remaining 5) and an un-forecastable feature with thin history (remaining 3). Its own
comment says what it pins: "the all-features fallback must not take over while a feature that can be
forecast is still there to govern the delivery." That precedence rule is exactly what D6 deletes. Under
the new AND both features have remaining work, so the answer becomes `False` and the test flips.

**It is not a regression — it is the AC-02.4 delta, asserted from the other side.** DELIVER should
invert it and rename it, not repair the fixture. DISTILL deliberately left it untouched: it passes
today, and separating it from the change that forces it would make the DELIVER diff harder to read.

Two neighbours in the same file were checked and are **safe**:
`FromDelivery_EveryFeatureUnforecastableAndInsufficient_...` (one `false` contributor ⇒ AND is false,
unchanged) and `FromDelivery_AForecastableFeatureGovernsSufficiency_EvenWhenAnotherIsUnknown` (also
false under both rules — though its NAME becomes wrong once nothing governs anything).
`DeliveryWithLikelihoodDtoTest.Should_Mirror_Insufficient_Data_From_Governing_Feature` is likewise
value-stable and name-drifted: both its features have remaining work and one is insufficient, so the
AND is false either way.

## The `DeliveryMetricSnapshot` `hasForecast` interaction — NOT in slice-02's scope

DESIGN deferred question 10 asks whether the recorder should skip a row rather than record a null when
a guard returns an empty `WhenDistribution`. **That question belongs to slice-01, not here, and
slice-01 already answered it** (DT-7: no test, no change — the recorder handles `(null, [])` today
because that is exactly what an ADR-112 D8 delivery produces; DDD-7 makes the shape more frequent, not
new).

Slice-02 introduces **no** new empty-`WhenDistribution` path: it changes one boolean and nothing else.
`DeliveryMetricSnapshotRecordingHandler` (`:46-63`) writes `TargetDateAtSnapshot`, `TotalWork`,
`DoneWork`, `RemainingWork`, `EstimatedItemCount`, `LikelihoodPercentage`, `WhenDistributionJson` and
`FeatureBreakdownJson`. There is no sufficiency column on the snapshot at all, so this slice is
invisible to the recorder. No test, and nothing deferred.

## State at hand-off

The 3 RED tests carry `[Ignore("RED until Story #5587 slice-02 ...")]` per test — the fixture also
holds 6 guards that must run, so a fixture-level ignore would silence them. DELIVER's first act for
this slice is removing those three attributes.

---

# DISTILL — RED classification (Story #5587, slice-03)

Runs:

```
cd Lighthouse.Frontend && pnpm exec vitest run \
  src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.likelihoodCopy.test.tsx

cd Lighthouse.EndToEndTests && TZ=Europe/Zurich pnpm exec playwright test \
  tests/specs/portfolios/DeliveryJointLikelihood.spec.ts
```

Result 2026-07-29, with `describe.skip` temporarily un-skipped: **10 tests — 5 failed, 5 passed.**
With the skip in place: 5 passed, 5 skipped. Full frontend suite after this slice: **281 files,
3779 passed, 11 skipped** (5 here + 6 from slice-04). `pnpm build` green, `pnpm biome check ./src`
clean — the one remaining `info` is a pre-existing `noUselessFragments` in
`components/Common/FeatureListDataGrid/columns.test.tsx`, untouched by this wave.

## Failing — 5 (all MISSING_FUNCTIONALITY)

| Test | Classification | Evidence |
|---|---|---|
| `labels the header with the joint framing, the renamable plural term and the delivery date (AC-03.1, AC-03.8)` | MISSING_FUNCTIONALITY | `Unable to find an element with the text: All Features by 1/31/2025: 72%` — the chip still reads `Likelihood: 72%` |
| `explains on the header what ALL means (AC-03.1)` | MISSING_FUNCTIONALITY | `Unable to find an element with the title: P(ALL of these land by the date)` |
| `frames the breakdown column as the per-feature probability and says what it ignores (AC-03.2)` | MISSING_FUNCTIONALITY | `Unable to find an element with the text: /Likelihood \(each on its own\)/` |
| `builds the header from the renamed vocabulary rather than a literal (AC-03.3)` | MISSING_FUNCTIONALITY | `Unable to find an element with the text: All Epics by 1/31/2025: 72%` under a `getTerm` mock returning "Epics" |
| `keeps the full label reachable under a long renamed term (deferred question 8)` | MISSING_FUNCTIONALITY | same, under "Programme Increment Epics" |

## Passing — 5 (guards, must stay green)

| Test | Why it is green today and must stay green |
|---|---|
| `never claims the header is lower than every row (AC-03.4, D1 constraint B)` | **Vacuous today and labelled as such in the test body.** It is deliberately OUTSIDE the skipped block: it cannot be RED, because it constrains copy that does not exist yet — but it is running the moment DELIVER writes the label, and fails on the first draft that over-promises. Equality is legitimate (D5) and is exactly what the three-way fixture renders |
| `keeps the cannot-forecast label and its team-naming tooltip, without the joint framing (AC-03.5)` | The `CANNOT_FORECAST_SHORT` branch and its `cannotForecastReason` tooltip are untouched. The `queryByText(/^All /)` half proves the new framing does not leak into a non-numeric state |
| `keeps the not-enough-data label, without the joint framing (AC-03.5, AC-02.6)` | Carries **AC-02.6** as well: slice-02 flips this flag on more deliveries but reuses this exact rendering — no new indicator, no new colour |
| `keeps the per-row chip's own cannot-forecast tooltip alongside the column header (AC-03.6)` | The coexistence assertion. `FeatureLikelihoodChip` wraps its unforecastable chip in an MUI `Tooltip`, so the reason is the element's accessible label; the new column-header affordance must not clobber it |
| `keeps the header chip's size and ForecastLevel colour (AC-03.7)` | `MuiChip-sizeSmall` plus the `ForecastLevel(72).color` background. **Position is NOT asserted** — jsdom has no layout, and a DOM-order assertion would pin markup rather than appearance |

## The E2E — run live, all three steps observed RED

`DeliveryJointLikelihood.spec.ts` was run **un-skipped against a live Lighthouse** (backend on
`localhost:5169` with the frontend built into `wwwroot`, demo scenario 0, `TZ=Europe/Zurich`), not
merely typechecked:

| Step | Observed |
|---|---|
| the header says it covers all features | `Locator resolved to <div class="MuiChip-root MuiChip-filled …">` **123×**; `Received string: "Likelihood: 0%"` against `/^All .+ by .+: \d+%$/`. The locator works; only the copy is missing |
| the header explains what ALL means | `getByTitle('P(ALL of these land by the date)')` — not attached |
| the breakdown column says it ignores the other features | `locator('[role="columnheader"][data-field="likelihood"]')` resolved and read `"Likelihood"` |

Steps 2 and 3 are unreachable once step 1 fails, so they were run in isolation through a temporary
probe spec (since deleted) rather than assumed. **Two POM defects were found and fixed by that run,
either of which would have produced a wrong-reason RED:**

1. `DeliveryItem.getTargetDate()` matches a `Target Date:` prefix that `DeliverySection` **does not
   render** — it renders `Delivery Date:`. The getter returns `null` on this page and always has. The
   spec's AC-03.8 assertion would have failed on a broken helper rather than on missing copy. A new
   `getDeliveryDate()` was added (verified live: `"8/12/2026"`); `getTargetDate()` was left in place
   and flagged, because its only caller reads `name` and `scope` off `getDetails()`.
2. `page.getByText(/^Likelihood/)` matches **2** elements (the header chip and the column header) —
   a strict-mode break waiting to happen. Replaced with a `likelihoodColumnHeader` POM getter keyed on
   `data-field`, which resolves to exactly 1 and survives the relabel.

`getForecastChipLabel()` / `forecastChip` are scoped to `.MuiChip-filled`, because the same
`AccordionSummary` also renders one **outlined** chip per completion-date percentile.

**What the E2E deliberately does not assert**: the joint maths (demo throughput moves; the number is
pinned in `DeliveryJointForecastIntegrationTest`), a renamed-vocabulary permutation, or a
long-terminology permutation. One thin walking skeleton, per the project's standing E2E rule.

## What could not be tested, and why

- **Visual truncation under a long renamed term** (slice-03's learning hypothesis / deferred
  question 8). jsdom has no layout, so the Vitest case asserts only that the full string is rendered
  and reachable — a green there does **not** mean the copy fits. The E2E has no seam for applying a
  terminology override to the instance. This stays a **manual check before the copy is final**, and
  DESIGN already says what happens if it fails: a chip/label restructure comes back to DESIGN, it is
  not solved by shortening copy that is locked.
- **Chip position** (AC-03.7). Size and colour are asserted; position is not, because any assertion
  available in jsdom pins markup order rather than rendered position.

## Existing tests DELIVER must update — not wrong-reason REDs

| Site | What breaks | Why |
|---|---|---|
| `DeliverySection.test.tsx:130, 241, 242, 376, 377` | `getByText("Likelihood: 75%")`, `queryByText("Likelihood: >95%")` etc. | The header chip's numeric branch is relabelled. The `>95%` and `100%` assertions still matter — they are the never-present-a-forecast-as-certainty guards — so they must be **rewritten against the new label**, not deleted |
| `DeliveryItem.getLikelihood()` (E2E POM) | regex `/Likelihood:\s*>?(\d+)%/` stops matching | Superseded by `getForecastChipLabel()`. Left in place by DISTILL because it works today |
| `PortfolioDetail.spec.ts:87` — `expect(featureLikelihoods).toContain(details.likelihood)` | breaks **twice** | (a) `details.likelihood` becomes `null` after the relabel; (b) more fundamentally, it asserts the delivery number EQUALS one of its feature rows. After slice-01 the delivery is `<=` every row and equality is the exception, not the rule (D5). **This is a latent slice-01 break that slice-01's own classification did not catch**, because it lives in an E2E rather than in the backend suite |
| `DeliveriesChips.tsx` (portfolio overview table) + `DeliveriesChips.test.tsx` | not broken, but **left inconsistent** | It renders the same delivery number under the old `Likelihood: NN%` label. D1's copy is scoped to the delivery header chip, so after slice-03 the overview and the detail describe the same number differently. See upstream note S3-1 |

## State at hand-off

The 5 RED Vitest cases sit in a `describe.skip` block; the E2E scenario is `test.skip(...)` per the
`OAuthConnection.spec.ts` precedent. DELIVER un-skips them one at a time.

---

# DISTILL — RED classification (Story #5587, slice-04)

Run:

```
cd Lighthouse.Frontend && pnpm exec vitest run \
  src/utils/forecast/deliveryJointLikelihoodDocs.enforcement.test.ts
```

Result 2026-07-29, with `describe.skip` temporarily un-skipped: **8 tests — 6 failed, 2 passed.**
With the skip in place: 2 passed, 6 skipped.

## Be honest about what slice-04 supports

Slice-04 is a prose slice. **No test can judge whether an explanation explains**, and the slice's real
gates are human: the maintainer walking the worked example end to end against the running demo
instance and arriving at the displayed rounded percentage (slice-04 gate 1), and the DIVIO/Diataxis
review that keeps the page *explanation* rather than tutorial.

What IS mechanical, and worth having, is **drift**. The release notes and the concept page are both
versioned in this repo (`docs/releasenotes/releasenotes.md` has a `# Lighthouse vNext` section;
`docs/concepts/howlighthouseforecasts.md` is hot-linked live from `@main` via jsDelivr), so a
`readFileSync`-plus-regex enforcement test can fail if the section is never written, is later deleted,
or makes the one claim D1 constraint B forbids. That idiom is not invented here — it is exactly
`src/utils/forecast/formatLikelihood.enforcement.test.ts`, which is why the new file sits beside it.
(Not under `src/docs`: a `src/docs` path in this project has a history of Biome reformatting the whole
docs tree.)

## Failing — 6 (all MISSING_FUNCTIONALITY)

| Test | Classification | Evidence |
|---|---|---|
| `names all three visible consequences in the release notes (AC-04.1)` | MISSING_FUNCTIONALITY | vNext section does not match `/governing/i` (nor `/percentile/`, nor `/backfill/`) |
| `calls the sufficiency change out separately (AC-04.2)` | MISSING_FUNCTIONALITY | vNext section does not match `/not enough data/i` |
| `adds a delivery-level worked example to the concept page (AC-04.3)` | MISSING_FUNCTIONALITY | concept page has no `^#{2,4} .*deliver` heading |
| `teaches the per-team-per-feature grain (AC-04.4)` | MISSING_FUNCTIONALITY | delivery-grain section is empty (`expected '' to match /twice|double|each team/i`) |
| `restates the independence assumption at delivery grain (AC-04.5)` | MISSING_FUNCTIONALITY | same, `/independen|shared people/i` |
| `shows the equality case (AC-04.6)` | MISSING_FUNCTIONALITY | same, `/0\.72|72\s?%/` |

## The check that could not fail — caught, and fixed

**AC-04.4, AC-04.5 and AC-04.6 were first written page-wide and observed PASSING against the unchanged
concept page.** The page already teaches independence ("treats the teams as independent", "if the same
people work in both teams"), already says a finished team's column is 1.00 everywhere, and already
contains "about 72%" — all at **feature** grain. A page-wide keyword check for those ideas is green
today and can never go red, which is worse than no check at all.

The fix was to scope every content assertion to the **delivery-grain section itself**, extracted from
its heading to the next heading of the same or higher level. All three then fail with
`expected '' to match …` — the section does not exist. Recorded here because the first draft looked
like coverage and was not.

## Passing — 2 (guards, must stay green)

| Test | Why it is green today and must stay green |
|---|---|
| `never claims the delivery is always lower than every feature (AC-04.6, D1 constraint B)` | **Vacuous today, labelled as such, deliberately outside the skipped block.** It runs against the first draft DELIVER writes. `docs/` is hot-linked from `@main` via jsDelivr, so a false "always lower than every Feature" is live on letpeople.work the moment it merges — this is the one prose error worth a machine check |
| `adds no in-app banner or dismissible notice to the delivery surface (AC-04.7, D3)` | Source scan of `DeliverySection.tsx` for `Alert` / `Snackbar` / `dismiss`. D3 chose docs-only; the failure this catches is a well-meant "why did my number change?" banner appearing in DELIVER |

## What slice-04 cannot test, stated rather than papered over

- **Whether the prose is good.** The keyword checks pass on a section that says the right words badly.
  They are drift guards, not a quality gate.
- **Whether the worked example reproduces.** Slice-04's own learning hypothesis is that a reader
  following the page arrives at the number on screen. That is the maintainer's manual walkthrough
  (gate 1) and an outcome KPI; a test that recomputed the number in TypeScript would be asserting the
  docs against a second implementation, which is not the same claim.
- **AC-04.7's trend-annotation half.** `DeliverySection.tsx` is greppable; "no annotation was added to
  the predictability chart" is a diff-review item.
- **Screenshots.** `Screenshots.spec.ts` captures `features/delivery_detail.png` (`:308`) and
  `features/portfoliodetail.png` (`:247`), both of which show the delivery header chip and therefore
  both of which **must be regenerated** once slice-03 lands. Standing trap: an `@screenshot` test keeps
  the OLD PNG when the pixel diff is under 0.5 %, so `rm` the old file first. This is a DELIVER task,
  not a test — recorded here so it is not discovered at release time.

## State at hand-off

6 RED cases in a `describe.skip` block, 2 guards running. DELIVER un-skips as the prose lands.
