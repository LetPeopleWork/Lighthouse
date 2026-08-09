# Mutation testing — 5502 (Event-driven write-back collection)

Run 2026-08-09 against the slice-01 working tree. Gate is 80 % kill rate on every stack with changed
files.

| stack | score | scored | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **81.62 %** | 136 | 111 | 20 | 5 | 0 | ~6 m |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — | — |

**Frontend is N/A, not skipped**: slice 01 changes no file under `Lighthouse.Frontend/`. It has no
endpoint, no component and no copy — the whole slice lives behind a background refresh.

Config: `stryker.5502.backend.json`. No `vitest.stryker.mutation.ts` — see above.

## Two things about the number, for whoever runs this next

**The denominator is not the "will be tested" count.** Stryker reports `121 total mutants will be
tested`, but scores against 136 — the 15 `NoCoverage` mutants count against you without ever being
run. Chasing survivors alone will stall short of the gate; the `NoCoverage` list is where the untested
paths actually are, and it is not in the cleartext output. Read it out of `mutation-report.json`.

**Whole-file globs in `mutate` do scope correctly** when written as `**/Services/.../File.cs`. 14 324
mutants are created project-wide and then skipped down to 121; the create-count in the log is not the
scope. An earlier feature recorded these globs as ignored — that is not the behaviour here.

## Scope

Four files are mutated: the three write-back services and `UpdateServiceBase`. The three updaters
(`PortfolioUpdater`, `ForecastUpdater`, `TeamUpdater`) are **excluded** — each changed two to eight
lines inside a much larger body of licence gating, refresh logging and cleanup that this slice never
touched. Their changed lines are covered end to end by the ten acceptance scenarios in
`API/Integration/QuietWriteBack/`, which drive the real updaters through the real update queue.

`test-case-filter` matches 202 tests — the same set as the targeted suite.

| file | killed | survived | no coverage |
| --- | --- | --- | --- |
| `WriteBackTriggerService.cs` | 63 | 0 | 1 |
| `WriteBackService.cs` | 31 | 3 | 0 |
| `UpdateServiceBase.cs` | 14 | 14 | 4 |
| `WriteBackCollector.cs` | 3 | 3 | 0 |

## Closed by this pass

Four rounds: 58.82 % → 69.12 % → 74.26 % → **81.62 %**. Sixteen tests, each pinning a promise that was
previously unpinned rather than padding the score.

| Mutant that survived | What it exposed | Test that now pins it |
| --- | --- | --- |
| `mappings.Count == 0 \|\| !CanUsePremiumFeatures()` → `&&`, both call sites | **The premium-licence gate was not tested.** All three `NotPremiumLicense_ResolvesNothing` tests asserted an empty plan against a Team or Portfolio with **no items seeded**, so they passed whether the gate fired or not | The three tests now seed an item the resolver would otherwise resolve |
| the `mappings.Count == 0` half of the same condition | A connection with no write-back mapping still queried the work-item repository | `ResolveWriteBackForTeam_NoMappings_NeverAsksTheRepositoryForWorkItems` |
| `cycleTime > 0` → `cycleTime < 0` in `ResolveFeatureValue` | **The Feature cycle-time arm had no test at all.** The Team path had one; the Feature path fell through to `null` unnoticed | `ResolveFeatureWriteBackForPortfolio_WorkItemAgeCycleTime_WritesCycleTimeForDoneFeatures` |
| `TargetValueType == FormattedText && !IsNullOrEmpty(DateFormat)` → `\|\|` | A `Date` mapping carrying a format would have honoured it | `ResolveForecastWriteBackForPortfolio_DateTargetCarryingAFormat_StillWritesTheIsoDate` + its `FormattedText` twin |
| `changedItems.Count > 1` → `>= 1`, and its negation | The duplicate-reference warning (ADR-143 §5 / D-A6) was never asserted — only that the write did not throw | `WriteFieldsToWorkItems_TwoItemsShareAReference_WarnsNamingTheReference` + `..._ReferenceIsUnique_DoesNotWarn` |
| `return;` removal in `PersistWrittenValues` | A wholly-refused write still hit the database | `WriteFieldsToWorkItems_TheTrackerRefusedEveryField_SavesNothing` |
| D-A7-R persistence | The written value reaching the local copy was only covered end to end | `WriteFieldsToWorkItems_TheTrackerAcceptedTheWrite_StoresTheWrittenValueLocally` |
| **no coverage** — the unresolved-mapping guard, both call sites | A mapping that outlived the field it pointed at: never exercised | `Resolve{WriteBackForTeam,FeatureWriteBackForPortfolio}_MappingWhoseFieldNeverResolved_IsSkippedWithAWarning` |
| **no coverage** — `changedItem == null` skip | An update naming an item the instance no longer holds: never exercised | `WriteFieldsToWorkItems_UpdateNamesAnItemLighthouseDoesNotHold_NeverReachesTheConnector` |
| **no coverage** — the flush's own catch | `A_flush_that_throws_…` makes the *connector* throw, which `WriteBackService` handles internally, so `FlushWriteBack`'s catch never fired | `TriggerUpdate_TheWriteBackFlushThrows_LogsItAndStillFinishes` |
| `"No mapped value changed …"` / `"… with 0 updates — skipping"` logs | AC-04.3's silence is indistinguishable from broken write-back except through these lines | asserted in the two matching `WriteBackServiceTest` cases |
| `isForecast ? "forecast" : "feature"` | A Portfolio refresh runs two passes over the same items; the log is where an operator tells them apart | `Resolve{Feature,Forecast}WriteBackForPortfolio_SaysWhichPassItRan` |
| resolution-failure error messages | The exception tests counted error logs without reading one, so the message was free to change — and a swallowed failure leaves no other trace | `AssertSingleErrorLoggedContaining` in both exception tests |

## Accepted survivors

**`UpdateServiceBase.cs` — 14 survived + 4 no-coverage, none in code this slice added.** Every one is a
log statement or string in `ExecuteAsync`, `TryUpdating`, `UpdateAll` or `DelayStart`, plus the two
`await` removals (`DelayStart`, `Task.Delay`) that only a timing-sensitive test could kill. The member
this slice added, `FlushWriteBack`, now has **no** surviving and **no** uncovered mutants.

The file is kept in scope rather than excluded even though it costs ~13 points, because excluding it
would raise the number by hiding the state of a file the slice touched. The honest reading is: this
slice's own code is comprehensively covered; the background loop it sits in is not, and never was.

**`WriteBackCollector.cs` — 3, all equivalent or log-only.**

- `if (pending.Count == 0) return [];` block removal — with an empty staging area the loop body never
  runs and the method returns an empty list either way. Equivalent apart from one Debug line.
- `connectionsById.Clear()` removal — scope-lifetime hygiene. The map is unreachable once the scope
  ends, and a stale entry cannot be selected because selection is driven by the staged updates.
- the Debug flush line.

**`WriteBackService.cs` — 3.** The two `stopwatch.Stop()` removals are equivalent for every assertion
that does not read wall-clock time, and `"Write-back failed …"`'s string is the unhandled-exception
message on a path already asserted by its behaviour (every item comes back `Success = false`).

**`WriteBackTriggerService.cs` — 1 no-coverage.** The `ArgumentOutOfRangeException` message in
`GetPercentileFromSource`'s default arm. Unreachable through the public API: the switch is only
entered for a source already in `ForecastSources`.

## Not mutated

`PortfolioUpdater.cs`, `ForecastUpdater.cs`, `TeamUpdater.cs` — see Scope.
