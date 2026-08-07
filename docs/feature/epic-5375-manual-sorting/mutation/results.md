# Mutation testing — 5689 (Manual sorting: hand ordering ownership to Lighthouse)

Run 2026-08-07 against `main` @ `5f055dc30` + the quality-gate working tree. Gate is 80 % kill rate on
both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **85.42 %** | 96 | 82 | 14 | 0 | 2 m 10 s |
| Frontend (StrykerJS) | **84.78 %** | 46 | 39 | 7 | 0 | 1 m 36 s |

Configs: `stryker.5689.backend.json`, `stryker.5689.frontend.json`, `vitest.stryker.mutation.5689.ts`.

The vitest config sits beside the others but is **not committed** — `.gitignore:445` (`**/vitest.stryker*.ts`)
excludes it repo-wide, as it does slice 01's. Recreate it by copying the neighbouring `.5689` file's
shape and narrowing `test.include` to the specs covering the mutated files; StrykerJS resolves it
relative to `Lighthouse.Frontend/`, so it has to be copied there for the run and removed afterwards.

Both stacks were run twice. The first backend run scored **73.96 %** and the first frontend run
**57.14 %**; the sections below record what the survivors exposed and what was written to close them.
No survivor was closed by loosening a test.

## Backend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| `Services/Implementation/FeatureOrdering.cs` | 9 | 9 | 0 |
| `Services/Implementation/FeaturePositionMap.cs` | 2 | 2 | 0 |
| `Services/Implementation/FeatureOrderingPolicyProvider.cs` | 7 | 6 | 1 |
| `Services/Implementation/FeatureRankSeeder.cs` | 8 | 4 | 4 |
| `Models/ManualRankComparer.cs` | 13 | 13 | 0 |
| `Services/Implementation/AppSettingService.cs` | 52 | 46 | 6 |
| `API/DTO/PortfolioDto.cs` | 4 | 1 | 3 |
| `.../Update/FeatureOrderingPolicyChangedForecastTriggerHandler.cs` | 1 | 1 | 0 |

### Closed by this pass

| survivor | what it exposed | now pinned by |
| --- | --- | --- |
| `ManualRankComparer` — 8 mutants with **no coverage at all** | The comparer had no test of its own. Its null branches were reachable only through an integration path that never passes a null, so "never-placed sorts last" and "a null Feature does not throw" were both unasserted — the second is the bug the source-order ladder already shipped once (`1eccb8ef0`). | `ManualRankComparerTest` — 7 focused cases over places, never-placed, null Features and antisymmetry |
| `FeatureRankSeeder:44` — `MaxAsync(...) ?? 0` → `0` | **A real coverage hole in AC-5.3.** No test ever ran the seed with some Features already placed, so the "latecomers append after the last place" path was never executed. Every existing scenario either seeded from scratch or found nothing unplaced. | `A_feature_that_arrived_while_the_tracker_had_the_order_back_is_placed_last_when_it_is_taken_over_again` |
| `AppSettingService:110` — the `PublishAsync` call removed | Nothing asserted the policy change is announced. Without the event the places move and every forecast date stays where it was — the one failure indistinguishable from success on this feature. | `SetFeatureOrderingPolicy_AnnouncesTheChange` (both directions) |
| `FeatureOrderingPolicyChangedForecastTriggerHandler:27` — `TriggerUpdate` removed | The handler had no test. | `FeatureOrderingPolicyChangedForecastTriggerHandlerTest` — 3 cases including the no-Portfolios instance |
| — (not a survivor, found while triaging) | The seed-then-record ordering that D6 rests on was asserted nowhere. | `SetFeatureOrderingPolicy_TakingTheOrderOver_SeedsBeforeItRecordsTheChoice` |

**Config trap worth carrying forward**: the first run reported all 13 `ManualRankComparer` mutants as
`NoCoverage` *after* its test was written, because `test-case-filter` did not match
`ManualRankComparerTest`. The filter must name every changed file's tests, not just the headline ones —
a file whose tests are filtered out reports as uncovered across the board and looks like missing tests.

### Accepted survivors

| survivor | why it cannot be killed meaningfully |
| --- | --- |
| `FeatureRankSeeder:27` — first `return` removed | Equivalent. Falling through reaches the second early return via an empty result set, with the same observable. |
| `FeatureRankSeeder:36` — `&&` → `\|\|` | Equivalent. `unplaced` is derived from exactly the null-ranked set, so widening the predicate to "or unplaced" selects the same rows. The guard is defence-in-depth against a concurrent writer, not a filter this run can distinguish. |
| `FeatureRankSeeder:41` — second `return` removed | Reachable only when another writer placed every candidate between the two reads. That is a genuine race; a deterministic test cannot enter it. |
| `FeatureRankSeeder:51` — `SaveChangesAsync` removed | Equivalent **through a shared context**: `AppSettingService` calls the policy provider next, whose `repository.Save()` runs on the same scoped `LighthouseAppContext` and flushes the tracked rank changes. The explicit save is kept because relying on a later caller to flush is not a boundary worth having. |
| `FeatureOrderingPolicyProvider:35` — `repository.Update(existing)` removed | Equivalent. The entity was loaded through the same context and is already tracked, so EF writes it either way. |
| `AppSettingService` ×6, `PortfolioDto` ×3 | Not this change. Survey-nudge cadence, refresh settings, install timestamp and the percentile block are pre-existing code pulled in by whole-file globs (Stryker.NET ignores line ranges). Their coverage is a separate question from slice 02. |

## Frontend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| `pages/Settings/System/FeatureOrderingSettings.tsx` | 19 | 19 | 0 |
| `models/FeatureOrdering.ts` | 6 | 6 | 0 |
| `hooks/useFeatureOrdering.ts` | 14 | 9 | 5 |
| `components/Common/FeatureListDataGrid/FeatureListDataGrid.tsx` | 4 | 2 | 2 |
| `services/Api/SettingsService.ts` | 3 | 3 | 0 |

### Closed by this pass

| survivor | what it exposed | now pinned by |
| --- | --- | --- |
| `SettingsService.ts:47-50` — the route string and both bodies | **The two new service methods had no test at all.** The URL could become `""` and nothing noticed. | `SettingsService.test.ts` — reads both policies back, rejects one the client does not know, and pins the `PUT` body |
| `models/FeatureOrdering.ts:7-14` — the zod enum members and the schema object | The parse boundary was never exercised. Now covered through the service, including the rejection case. | as above |
| `FeatureListDataGrid.tsx` — the position column's label | **AC-5.4's actual observable was unasserted**: nothing checked that the grid renders the heading the ordering seam hands it. The column factory was tested, the wiring was not. | `should head the position column with %s's label` — both policies, asserted on the rendered column header |
| `useFeatureOrdering.ts` — the catch body | The documented failure mode ("an instance that cannot answer follows the tracker") had no test. | `follows the tracker when the instance cannot say who owns the order` |
| `FeatureOrderingSettings.tsx:33-38` — `isSaving` | Nothing stopped a second flip while the first was in flight. | `cannot be flipped again while the first flip is still in flight` |
| `useFeatureOrdering.ts` — `isLoading` | Not a missing test: **dead API surface.** No caller read it. Deleted rather than tested. | — |

### Accepted survivors

| survivor | why it cannot be killed meaningfully |
| --- | --- |
| `useFeatureOrdering.ts:29,32` — the catch body emptied / its literal blanked | Equivalent. The fallback sets `SourceOrder`, which is already the initial state, so an emptied catch is observationally identical. The assignment stays because it states the intent where a future default change would otherwise break it silently. |
| `useFeatureOrdering.ts:34,38` — `useCallback` / `useEffect` dependency arrays emptied | Standard StrykerJS React survivors. Emptying them changes re-render bookkeeping, not any output these tests can observe. |
| `FeatureListDataGrid.tsx:62,65` — the empty `else` branch of the two column spreads replaced with a junk column | Killing this means asserting the grid's exact column set, which breaks on every future column for no user-visible gain. The columns that matter are asserted by field and by header. |

## Not mutated

- `Models/Feature.cs`, `Models/FeatureOrderingPolicy.cs`, `Models/FeatureOrderKey.cs`,
  `Models/Events/FeatureOrderingPolicyChanged.cs`, `AppSettingKeys.cs` — data and declarations, no
  behaviour to mutate.
- `FeatureRepository.cs`, `WorkItemService.cs`, `FeaturesController.cs`, `AppSettingsController.cs` —
  slice 02 changed one call site in each. Stryker.NET ignores line ranges, so mutating them would bury
  a few lines under hundreds of untouched ones. The single seam they all now route through
  (`FeatureOrdering.cs`) scored **9/9**, and `FeatureOrderingSingleSourceArchUnitTest` fails if any of
  them reaches for a comparer directly — verified by introducing the violation and watching it go red.
- `SystemSettingsTab.tsx` — one `InputGroup` hosting the panel; the panel itself is at 100 %.

---

# Mutation testing — 5690 (Manual sorting: moving a Feature up the order)

Run 2026-08-07 against `main` @ `f59df9eac` + the quality-gate working tree. Gate is 80 % kill rate on
both stacks.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **82.76 %** | 85 | 72 | 13 | 2 | 0 | 5 m 28 s |
| Frontend (StrykerJS) | **85.55 %** | 173 | 148 | 23 | 2 | 0 | 4 m 31 s |

Configs: `stryker.5690.backend.json`, `stryker.5690.frontend.json`, `vitest.stryker.mutation.5690.ts`.

Both stacks failed their first run and were fixed by writing tests, not by narrowing scope: the backend
opened at **78.16 %** and the frontend at **65.90 %**.

## Backend

| file | tested | survived |
| --- | --- | --- |
| `FeaturesController.cs` | 47 | 9 |
| `FeatureRankingService.cs` | 22 | 4 |
| `FeatureMoveAuthorization.cs` | 12 | 2 |
| `FeatureRankChangedForecastTriggerHandler.cs` | 3 | 0 |
| `FeatureMoveVerdict.cs` | 1 | 0 |

### Closed by this pass

- **`TryReadTheTarget`'s two rejections had no coverage at all.** No test sent a body that was not an
  object, and none named a target that was not a number — so both `return false` guards could be
  inverted and nothing noticed. Now `A_move_whose_body_is_not_a_command_at_all_is_refused` and
  `A_move_naming_a_target_that_is_not_a_feature_id_is_refused`. Required a harness addition
  (`MoveFeatureWithBody`) because the old helper could only send a well-formed object.
- **A move of a Feature that is not there.** `A_move_of_a_feature_that_does_not_exist_changes_nothing` —
  the ordinary case is a row deleted between the list rendering and the menu being used.
- **The forecast trigger's own guards**, via a new `FeatureRankChangedForecastTriggerHandlerTest`: a
  Feature two Portfolios share refreshes both, a Feature in none refreshes nothing, and a Feature that
  is gone by the time the handler reads it refreshes nothing **and does not throw** — that last one
  matters because the dispatcher swallows a throwing handler, so the failure would look exactly like a
  forecast that quietly went stale.

### Accepted survivors

- **`FeatureRankingService` L44/L45 — the service's own "Feature not found" guard.** Unreachable through
  the driving port: the controller refuses a missing Feature before the service is called. It is not
  dead code — it covers the window between the controller's read and the transaction's re-read — but
  staging that race needs two concurrent connections, which is the same single-threaded-harness limit
  already declared for the renumber's concurrency.
- **`FeatureRankingService` L101 — `index + 1` → `index - 1`.** Genuinely equivalent, and it is the
  strongest evidence this epic has that INV-O2/DDD-4 hold: nothing reads a rank value, the position
  column is a computed ordinal, so numbering from 0 or from −1 changes no observable.
- **`FeatureRankingService` L106 — dropping the skip-if-unchanged `continue`.** Writes every row instead
  of the changed ones. Same result, more UPDATEs.
- **`FeaturesController` L164 — `placeBefore = true` → `false`.** The out-parameter's default, overwritten
  on every path that returns `true`.
- **`FeatureMoveAuthorization` L25 / `FeaturesController` L253 — `ConfigureAwait(false)` → `true`.** No
  synchronization context in ASP.NET Core.
- **`FeatureMoveAuthorization` L27 / `FeaturesController` L255 — the `is { }` null fallback.** Defensive:
  `IRbacAdministrationService` never returns null, in production or in the double.
- **`FeaturesController` L87, L111, L113 ×2, L220, L258 — pre-existing code.** `GetFeatureWorkItems`, the
  blocked flag and `RequestAborted` all shipped before this slice; Stryker.NET cannot mutate a line
  range, so the whole 260-line controller comes with the ~90 lines slice 03 added. Not this slice's debt,
  and not silently excluded either.

## Frontend

| file | score | killed | survived |
| --- | --- | --- | --- |
| `useFeatureOrdering.ts` | 94.59 % | 35 | 1 |
| `FeatureMoveMenu.tsx` | 86.14 % | 87 | 14 |
| `FeatureListDataGrid.tsx` | 74.07 % | 20 | 6 |
| `columns.tsx` | 75.00 % | 6 | 2 |

### Closed by this pass — and the gap was real

The first run scored 65.90 %, and the reason was structural rather than cosmetic: **nothing rendered the
menu through the grid.** `FeatureMoveMenu.test.tsx` renders the component directly, and
`FeatureListDataGrid.test.tsx` mocks the gate as `policy-off`, which makes the menu render `null` — so
`createFeatureOrderingActionsColumn`, the neighbour resolution and the move call were exercised by
nothing at all. That is exactly the wiring D10's "one change, both surfaces" depends on.

`FeatureListDataGrid.moveActions.test.tsx` renders the real grid, the real column factory and the real
menu over an allowing gate. Ten tests, and the one that earns its keep asserts AC-3.3 end to end: a Done
Feature sits between rows 1 and 3, `hideCompleted` hides it, and Move Up on row 3 names row 1 — the
hidden row is jumped, not landed on.

`FeatureMoveMenu.test.tsx` gained six: one distinct sentence per refusal reason (a single shared string
would have passed the old assertions), the announcement's exact text and its emptiness before any move,
and the gestures that name a row being disabled when there is no row to name.

### Accepted survivors

- **`FeatureMoveMenu` L36-42, L126, L141 (11 of the 14) — styling.** The `visuallyHidden` style object,
  the caption's `sx`, and the disabled item's `opacity`. Asserting a style value would pin CSS, not
  behaviour; the `aria-disabled` attribute that actually carries the meaning is asserted.
- **`FeatureMoveMenu` L121 — the menu's `onClose`.** Closing by clicking away is MUI's behaviour.
- **`FeatureMoveMenu` L171 — the `join(", ")` separator.** Only observable with two blocking Portfolios
  at once, which needs a Feature in three.
- **`FeatureListDataGrid` L76/L78/L84 — optional chaining and the empty-list branches.** A grid with no
  rows renders no menu, so `filteredFeatures[0]?.id` and `index < 0` cannot be reached from the port.
- **`columns.tsx` L76/L77 — the column's `field` and its empty `headerName`.** Identifier and layout.
  `sortable` and `filterable` are no longer survivors: D14 makes them a decision, so they are asserted.
- **`useFeatureOrdering` L32/L84 — the empty-array defaults.** `blockingPortfolios` is `[]` in every path
  the port produces.

## Not mutated

- The three new frontend model/service surfaces (`models/FeatureOrdering.ts` additions,
  `FeatureService.moveFeature`, `models/Feature.ts` zod additions) — type declarations and a one-line
  pass-through to axios. The call's shape is asserted in `FeatureListDataGrid.moveActions.test.tsx`.
- `DataGridBase.tsx` — one optional callback added to a 373-line shipped file.
- `FeaturesView.tsx` / `PortfolioFeatureList.tsx` — each gained one prop.
