# Mutation Report — Bug 5285 "Blackout Forecast Cache" Fix

Tool: Stryker.NET 4.14.2 (backend, feature-scoped)
Config: `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.fix-blackout-forecast-cache.json`
Date: 2026-06-14

## Scope

Mutated only the 4 changed production files; tests scoped to
`BlackoutPeriodServiceTest | RecurringBlackoutRuleServiceTest | BlackoutConfigurationChangedMetricsInvalidationHandlerTest | TeamMetricsServiceTests`.

## Result

**Overall kill rate: 88.89% (56 killed / 63 tested, 7 survived) — GATE PASSED (>= 80%).**

| File | Killed | Survived | Score |
|------|-------:|---------:|------:|
| BlackoutConfigurationChangedMetricsInvalidationHandler.cs | 2 | 0 | 100.0% |
| RecurringBlackoutRuleService.cs | 32 | 1 | 97.0% |
| BlackoutPeriodService.cs | 22 | 6 | 78.6% |
| BlackoutConfigurationChanged.cs | 0 | 0 | n/a (marker record, no mutable code) |

`BlackoutConfigurationChanged.cs` is a parameterless `record … : IDomainEvent` — there is nothing to mutate, so it produces no mutants.

The handler that performs the actual cache invalidation (the heart of the fix) scores 100%.

## Surviving mutants

All 7 survivors are equivalent / presentational; none represents an untested behavioural gap after the boundary test added below.

### RecurringBlackoutRuleService.cs (1)

- L13 Null-coalescing remove-right on the `domainEventDispatcher ?? throw` constructor guard. Equivalent-class survivor: the guard fires only when a null dependency is injected, which DI never does; the repo-wide pattern leaves these defensive guards unmutated-killed. Low value, intentionally not tested.

### BlackoutPeriodService.cs (6)

- L11 / L13 / L15 Null-coalescing remove-right on the three `?? throw new ArgumentNullException` constructor guards — same equivalent/defensive class as above. (`Constructor_NullRepository_Throws`-style tests exist for the first arg in the sibling service; the remaining args follow the identical never-null DI pattern.)
- L66 / L83 String mutation of the `KeyNotFoundException` message in `Update` / `Delete` (`$"...id {id}..."` -> `$""`). Presentational: the tests assert the exception **type**, not the human-readable message text.
- L96 String mutation of the `ArgumentException` message in `ValidateDateRange` (`"Start date must be on or before end date."` -> `""`). Presentational, same reason.

These message-text and DI-guard survivors are consistent with prior feature mutation baselines in this repo (e.g. recurring-blackout-events) and are accepted as low-value.

## Test gap found and closed

The initial run scored 85.71% with two **genuine** behavioural survivors in `RecurringBlackoutRuleService.Validate`:

- L77 `dto.IntervalWeeks < 1` -> `<= 1` (would wrongly reject the valid minimum interval of 1 week)
- L82 `dto.End < dto.Start` -> `<= ` (would wrongly reject a valid single-day rule where `End == Start`)

The existing tests only exercised the *invalid* side (`IntervalWeeks = 0`, end-before-start); the valid boundary (interval == 1, end == start) was never asserted-accepted, so both mutants survived.

Added one focused test —
`RecurringBlackoutRuleServiceTest.Create_MinimumIntervalAndSingleDayRange_IsAccepted` —
asserting a rule with `IntervalWeeks = 1` and `End == Start` is persisted. This kills both equality mutants, raising the file to 97.0% and the overall score to 88.89%.

No production code was modified.
