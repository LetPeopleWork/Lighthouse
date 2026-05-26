# Mutation Testing — aging-pace-percentiles REVIEW-ROUND changes

Scope: the NEW/CHANGED logic added AFTER the original DELIVER mutation run (commit `7a989fc9`),
introduced by the review-round commits `91134c0d` → `718bc8b7`. The original run validated the
state then; this run re-validates the surfaces the review rounds rewrote.

Step-ID: `review-mutation`.

## Verdict: PASS (both stacks ≥ 80% on the changed surface)

| Stack | Surface | Killed / Total | Score |
|-------|---------|----------------|-------|
| Frontend | `WorkItemAgingChart.tsx` (`computePaceBandRects` + `PaceBandOverlay` + `paceBandColorForPosition` + palette) | 53 / 53 | **100.0%** |
| Frontend | `useShowPaceBands.ts` (localStorage toggle hook) | 14 / 17 | **82.35%** |
| Frontend | feature-surface overall | 67 / 70 | **95.71%** |
| Backend | `CsvWorkTrackingConnector.cs` interpolation methods (lines 207–276) | 22 / 22 | **100.0%** |

Did NOT hit the Stryker.NET `{a..b}` glob gotcha: backend was mutated **whole-file** (no
line-range glob), then survivors filtered to the interpolation methods during analysis — the
mutant count was healthy (113 tested for the whole CSV file, 22 on the interpolation surface),
not the near-zero count that signals the muted-glob bug.

## Frontend

- Config: `Lighthouse.Frontend/stryker.config.aging-pace-percentiles.mjs`
  - mutate scope re-pointed to the moved logic (TS line-ranges work fine for the TS runner):
    - `WorkItemAgingChart.tsx:58-200` — `PACE_BAND_COLORS_LOW_TO_HIGH`, `paceBandColorForPosition`,
      `computePaceBandRects` (carry-forward inherit, floor→axisMin / top→axisMax full-height bands,
      top-always-red, zero-height suppression), `PaceBandOverlay` (opacity, scale domain).
    - `useShowPaceBands.ts:10-29` — default-off, restore-on-mount, persist-on-toggle.
  - `PercentileLegend.tsx` dropped from scope: the review-round pace-chip removal left no
    pace-specific logic in that file; it is now plain percentile + SLE chips covered transitively.
- Scoped vitest config: `Lighthouse.Frontend/vitest.stryker.aging-pace-percentiles.config.ts`
  (includes `WorkItemAgingChart.test.tsx` + `useShowPaceBands.test.ts`).
- Baseline: 81 scoped tests green before mutating → 83 after the two kill-tests.

### Survivors killed this session (mutant → test added)

Progression: first run 92.86% (65/70) → after kill-tests 95.71% (67/70).

1. **`WorkItemAgingChart.tsx:85` ArithmeticOperator — `PACE_BAND_COLORS_LOW_TO_HIGH.length - 1`
   → `... + 1`** (the clamp in `paceBandColorForPosition`). Survived because no test had more
   than five band positions, so `Math.min(position, lastIndex)` never engaged. Added
   *"clamps the colour of bands beyond the palette to the reddest non-top colour"* — feeds six
   percentiles (seven boundaries) so the band below the top has position 5; the original clamps it
   to palette index 4 (`errorColor`), the mutant indexes 5 (`undefined`). Kills the mutant and took
   `WorkItemAgingChart.tsx` from 98.11% → **100%**.

2. **`useShowPaceBands.ts:16` ConditionalExpression — `setShowPaceBands(stored === "true")`
   → `setShowPaceBands(true)`**. Survived because no test mounted with a stored value other than
   `"true"`. Added *"treats any stored value other than the literal true as disabled"* — stores
   `"false"`, asserts the hook restores `false`. The mutant would force `true`. Took the hook from
   76.47% → **82.35%**.

### Survivors remaining (each a justified equivalent mutant)

All three remaining survivors are in `useShowPaceBands.ts` and are genuinely equivalent — killing
them would require asserting React-internal effect/callback scheduling, i.e. framework-behavior
testing, which the project conventions forbid.

- **`:15` ConditionalExpression `stored !== null` → `true`.** When nothing is stored, the mutated
  guard runs `setShowPaceBands(null === "true")` = `setShowPaceBands(false)`. The hook's default
  state is already `false`, so the call is a no-op and produces no observable change. Equivalent.
- **`:18` ArrayDeclaration `useEffect(..., [])` → `[..., "Stryker was here"]`.** React always runs
  an effect on first mount regardless of the dependency array; the hook is mounted once per test and
  never re-rendered with changed deps, so the restore-on-mount behavior is identical. Equivalent
  under the mount-once contract.
- **`:26` ArrayDeclaration `useCallback(..., [])` → `[..., "Stryker was here"]`.** The toggle
  callback closes over only the stable `setShowPaceBands` setter, so its identity change is
  unobservable to every assertion. Equivalent.

## Backend

- Config: `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.aging-pace-percentiles.json`
  (unchanged — it already mutates `CsvWorkTrackingConnector.cs` whole-file and filters tests to
  `CsvWorkTrackingConnectorTest` + `DemoDataStateEnteredSeamIntegrationTest`).
- Interpolation methods analysed (lines 207–276): `BuildStateEnteredTransitions`,
  `BuildInterpolatedDoneJourney` (even-split tick math + `Zip`/`Skip(1)` pairing + final
  ClosedDate transition), `ShouldSynthesizeStateJourney`, `MappedDoneState`.
- Baseline: 56 CSV-connector tests green before mutating → 58 after the two kill-tests
  (the strengthened journey-timestamp test reuses the existing test slot).

### Survivors killed this session (mutant → test added)

Progression on the interpolation surface: first run 90.9% (20/22, 2 survivors) → after kill-tests
**100% (22/22)**.

1. **`:213` Block-removal in `BuildStateEnteredTransitions` — drops the
   `if (string.IsNullOrWhiteSpace(stateEnteredDateColumn)) return [];` guard.** Survived because the
   existing unconfigured-column test had the demo synthesis flag OFF, so the fall-through path also
   returned empty. Added
   *`GetWorkItemsForTeam_StateEnteredColumnUnconfigured_NeverSynthesizesJourney_EvenWhenSynthesisFlagOn`*
   — synthesis flag ON, Done item with dates, but no StateEnteredDate column configured. With the
   guard the result is empty (transition history unsupported); without it the mutant synthesizes a
   full interpolated journey. Asserting empty kills the mutant and pins the contract.

2. **`:268` Linq mutation `SingleOrDefault()` → `Single()` in `ShouldSynthesizeStateJourney`.**
   Survived because every test connection declared the `Synthesize State Journey For Demo` option.
   Added *`GetWorkItemsForTeam_SynthesisFlagOptionAbsentEntirely_DefaultsToNoSynthesis`* — removes
   the option from the connection entirely. `SingleOrDefault` returns null → `bool.TryParse(null)` is
   false → no synthesis (correct); `Single` throws `InvalidOperationException`. Asserting the item
   gets empty transitions kills the mutant.

Defense in depth: also strengthened
`GetWorkItemsForTeam_DemoJourneySynthesisEnabledForCompletedItem_SynthesizesInterpolatedExitChainEndingAtClosedDate`
to pin the two intermediate interpolated timestamps (Jan 14 and Jan 18 for a 12-day span over
three Doing states = even 4-day splits), so the index-scaled tick math
(`started + TimeSpan.FromTicks(totalSpan.Ticks * index / stateCount)`) is asserted exactly rather
than only by monotonic ordering.

No accepted survivors remain on the backend interpolation surface.

## New / modified files

Test files (kept):
- `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.test.tsx` (+1 palette-clamp test)
- `Lighthouse.Frontend/src/hooks/useShowPaceBands.test.ts` (+1 non-"true" stored-value test)
- `Lighthouse.Backend/.../Csv/CsvWorkTrackingConnectorTest.cs` (+2 guard/option tests, +2 timestamp asserts)

Config files (kept, mutation-tooling):
- `Lighthouse.Frontend/stryker.config.aging-pace-percentiles.mjs` (re-pointed mutate scope)
- `Lighthouse.Frontend/vitest.stryker.aging-pace-percentiles.config.ts` (re-pointed include)
- backend config unchanged.

No production source was modified (Stryker sandboxes mutations; both source trees verified clean
after the runs). No frontend dependency touched (the `qs` audit gate is untouched). Biome clean,
`pnpm test` (3041) / `pnpm build` green; `dotnet build` zero-warning / `dotnet test` green for the
changed surface (one unrelated live-Jira write-back integration test flaked under parallel load and
passes in isolation).
