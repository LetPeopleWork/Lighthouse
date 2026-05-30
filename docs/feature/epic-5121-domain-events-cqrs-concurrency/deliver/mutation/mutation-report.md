# Epic 5121 — Mutation Testing Report (Domain Events / CQRS-lite / Concurrency)

Date: 2026-05-30
Scope: the epic's NEW, self-contained logic only (domain-event dispatcher + reaction
handlers, the concurrency-conflict MVC exception filter, and the frontend auto-save /
optimistic-concurrency hook + supporting UI). Big shared files (`LighthouseAppContext.cs`,
`WorkItemService.cs`, `RepositoryBase.cs`) are NOT whole-file mutated — see the big-file
coverage note below.

## Result summary

| Stack | Scoped surface | Before added tests | After added tests | ≥80% met? |
|-------|----------------|--------------------|--------------------|-----------|
| Backend | 6 files (dispatcher + 4 handlers + filter) | 61.90% (13/21) | **85.71%** (18/21) | yes |
| Frontend | 3 files (hook + 2 components) | 16.23% (25/154) | **80.52%** (124/154) | yes |

## Tooling note (mirrored the project's per-feature Stryker pattern)

- **Stryker.NET 4.14.2** — the documented `file.cs{a..b}` line-range glob silently mutes
  ALL mutants (ci-learnings 2026-05-26). All backend files are therefore **whole-file
  mutated**; the chosen files are small and self-contained so whole-file scoping is exact.
  Test execution is scoped via `test-case-filter` (the `time-in-state.json` pattern) so the
  run stays ~2 min.
- **Stryker JS (frontend)** — TS `file.ts:start-end` line-ranges work; used to scope the
  NEW concurrency logic inside three otherwise-larger files.

### Configs created

- `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.epic-5121.json`
- `Lighthouse.Frontend/stryker.config.epic-5121.mjs`
- `Lighthouse.Frontend/vitest.stryker.epic-5121.config.ts`

## Backend — per-file kill rate (after added test)

Run: 20 in-scope mutants (1 NoCoverage statement promoted to killed by the new filter test),
21 reported after the filter test added coverage; final 18 killed / 3 survived = **85.71%**.

| File | Verdict |
|------|---------|
| `DomainEvents/DomainEventDispatcher.cs` | all behavioral mutants killed; 1 log-message string survives (presentational) |
| `DomainEvents/PortfolioFeaturesRefreshedMetricsInvalidationHandler.cs` | 100% killed |
| `DomainEvents/TeamDeletedRefreshLogCleanupHandler.cs` | 100% killed |
| `BackgroundServices/Update/TeamDataRefreshedForecastTriggerHandler.cs` | 100% killed |
| `BackgroundServices/Update/TeamDeletedForecastRetriggerHandler.cs` | 100% killed |
| `API/Filters/ConcurrencyConflictExceptionFilter.cs` | all behavioral mutants killed by the new unit test; 2 log-statement/string mutants survive (presentational) |

### Backend test added

- `Lighthouse.Backend.Tests/API/Filters/ConcurrencyConflictExceptionFilterTest.cs` — 2 tests
  driving the filter's public `OnException(ExceptionContext)` directly (the filter had no
  dedicated unit test; it was only covered transitively by `TeamConcurrencyTokenIntegrationTest`).
  This killed the real survivors: the non-concurrency early-return guard (was NoCoverage), the
  `ConflictObjectResult` + 409 status, the `ProblemDetails` Title/Detail/Type, the
  `Extensions["code"]` key, and `ExceptionHandled = true`. The expected `code` value is asserted
  against the production `internal const ConcurrencyConflictExceptionFilter.ConflictCode`
  (visible via `InternalsVisibleTo`), not a hardcoded oracle.

### Backend surviving mutants — classification (3, all presentational)

| File:Line | Mutant | Classification |
|-----------|--------|----------------|
| `DomainEventDispatcher.cs:30` | `LogError(...)` message string → `""` | **Presentational** — log-message text. The dispatcher test verifies `LogLevel.Error` is logged exactly once on a handler throw; asserting the literal message text would be over-coupling (banned by CLAUDE.md comment/assert discipline). |
| `ConcurrencyConflictExceptionFilter.cs:28` | `LogInformation(...)` statement removed → `;` | **Presentational** — removing the info log has no observable HTTP-contract effect. |
| `ConcurrencyConflictExceptionFilter.cs:28` | `LogInformation(...)` message string → `""` | **Presentational** — log-message text. |

## Frontend — per-file kill rate (after added tests)

| File | After | Survived | Note |
|------|-------|----------|------|
| `components/Common/ValidationActions/SaveStateIndicator.tsx` | **100.0%** | 0 | conflict branch + reload affordance fully covered by existing tests |
| `components/Common/Connection/ModifyConnectionSettings.tsx` | **78.6%** | 6 | `resolveValidationErrorMessage` 403 branch now killed; remaining survivors are the re-throw-into-unhandled-rejection class (below) |
| `hooks/useModifySettings.ts` | **78.8%** | 24 | was 1.77% — the epic's flagship auto-save / token-chaining / conflict logic had near-zero behavioral coverage before this run |
| **Scoped total** | **80.52%** | 30 | ≥80% met |

### Frontend tests added

All through the hook's / component's public API (driving port), no internal-structure assertions.

- `src/hooks/useModifySettings.test.ts` — added a `describe("auto-save with optimistic
  concurrency")` block (14 tests, several parametrized): debounced auto-save fires once;
  no-save without interaction / when `canSave=false`; saving→saved transition + token advance
  from the save result; in-flight queueing flushes the pending edit **with the refreshed token**;
  409 enters `conflict` and **blocks further auto-saves**; non-409 maps to `error`;
  `reloadAfterConflict` fetches fresh settings, clears the conflict, returns to `idle`, and
  **reselects the matching work-tracking system**; `retry` re-dispatches the last payload;
  `autoRefreshOnSave` triggers `additionalFetch` and flags `refreshFailed` on rejection;
  `reloadDependentData`; token preserved when a save resolves without a fresh token;
  parametrized null-clears for each nullable field.
- `src/components/Common/Connection/ModifyConnectionSettings.test.tsx` — 2 tests: 403 ApiError
  with empty server message renders the plan-limit fallback copy; non-ApiError validation
  failure renders the generic connection-failed copy.

### Frontend surviving mutants — classification (30)

**(a) Real-but-impractical-to-kill — re-throw escapes the test assertion channel (4)**
`ModifyConnectionSettings.tsx` L428–429 (`error_ instanceof ApiError`, `error_.code === 409`)
and the L50 `&&`/`true` conditional on the 403 branch. The save handler is invoked
fire-and-forget as `onSave()` (ValidationActions.tsx:63), so a non-409 / mutated-guard path
**re-throws** into an unhandled promise rejection that vitest reports as a side-channel "error",
NOT a test assertion failure — so Stryker does not count it as a kill. Killing these would
require restructuring production error-propagation (return a Result instead of re-throwing) or
globally intercepting unhandled rejections in the harness; both destabilize the suite for no
behavioral gain. The 409 happy-path (conflict copy shown) and the 403/fallback message branches
ARE covered. **Recommend**: leave as-is; revisit if `handleSave` is ever changed to a Result-return.

**(b) Stale-request guards `isLatest()` (8)** — `useModifySettings.ts` L179/181/199/207/209
(`isLatest() → true`, related conditionals/booleans). `isLatest()` only diverges from `true`
when an **earlier** overlapping save resolves **after** a newer one bumped `requestSeqRef`.
The hook serializes saves via `isSavingRef` + a pending-payload queue, so producing a
deterministic stale-resolution interleaving in a unit test is not reliably reproducible.
**Equivalent under the hook's own serialization** in all observable single-threaded test flows.

**(c) Monotonic-counter direction (2)** — L177/L240 `requestSeqRef.current += 1 → -= 1`.
The counter is only ever compared for **equality/inequality** (`seq === requestSeqRef.current`);
increment direction is irrelevant to uniqueness. **Equivalent**.

**(d) React dependency arrays / cleanup (5)** — L227/237/251(killed)/257 `[...] → []`,
L276 `clearTimeout` arrow → `() => undefined`. Mutating a `useCallback`/`useEffect` dep array
or the debounce-cleanup changes memoization identity / cleanup timing, not the observable
save behavior the tests assert. **React-internal / equivalent** for the asserted contracts;
the debounce-fires-once behavior IS asserted and passes.

**(e) Optional-chaining on an always-defined ref (2)** — L208/234
`additionalFetchRef.current?.() → additionalFetchRef.current()`. In every test path that reaches
these lines `additionalFetch` is provided, so the `?.` short-circuit is never exercised
differently. **Equivalent for the covered configurations** (the no-additionalFetch path doesn't
reach these branches).

**(f) Residual behavioral micro-survivors (the rest)** — e.g. L75 `token === undefined → false`
in `withToken` (the queue-flush always supplies a token in the covered flow), L198
`setSaveState("saving") → ""` (the transient saving state is overwritten by saved/conflict
before any assertion can observe it under fake timers), L204/243 `hasInteractedRef = false → true`,
L263 auto-save-guard variations. These are low-value: each is either masked by a stronger
downstream state assertion or requires a contrived interleaving. Pushing past them would add
brittle timing-dependent tests with poor signal. Left documented; the epic's observable
contract (debounce → save → token advance → conflict → reload → reselect → retry) is covered.

## Big-file coverage note (NOT whole-file mutated, by design)

`Data/LighthouseAppContext.cs` and `Services/Implementation/WorkItems/WorkItemService.cs` were
deliberately excluded from mutation. They are large and .NET Stryker cannot line-range-scope
them (the 4.14.2 `{a..b}` defect), so whole-file mutation would run for hours and dilute the
score with unrelated pre-existing code. `Repositories/RepositoryBase.cs` was likewise excluded:
its only NEW member is the one-line `ApplyConcurrencyTokenForEdit` passthrough that delegates
straight to `LighthouseAppContext.ApplyConcurrencyTokenForEdit`.

Their NEW concurrency / event-emission logic is covered qualitatively by already-shipped tests:

- **Concurrency-token regeneration / advance / bypass** (`LighthouseAppContext`, `RepositoryBase`):
  `TeamConcurrencyTokenIntegrationTest`, `PortfolioConcurrencyTokenIntegrationTest`,
  `WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest`,
  `RbacGroupMappingConcurrencyTokenIntegrationTest`, `ConcurrencyTokenScopeIsolationTest` — these
  exercise read-your-writes token advance, stale-write → 409 → no-write → first-writer-preserved,
  and lone-editor-no-false-conflict, end-to-end through the real HTTP stack + the exception filter.
- **Work-item domain-event emission edge logic** (`WorkItemService`): the WorkItemService unit
  tests + the `WorkItemDomainEventsGoldTest` / `DomainEventDispatcherGoldTest` (real DI fan-out,
  survives-throwing-handler, re-drive-on-republish).

Full mutation of these two big files is **impractical** given the .NET line-range limitation and
is not recommended; the integration + gold tests are the right safety net for that surface.

## Flags / surprising findings

- **Frontend hook started at 1.77%** — the epic's most important NEW client logic (auto-save
  with optimistic-concurrency token chaining + conflict handling) had essentially no behavioral
  mutation coverage. The pre-existing `useModifySettings.test.ts` thoroughly covered the *manual*
  `handleSave` path but never drove `dispatchSave` / the auto-save effect / `reloadAfterConflict`
  / `retry`. The 14 added tests raised it to 78.8% and the scoped total to 80.52%. This is the
  headline finding — worth a reviewer's attention as a coverage-shape gap, not a production bug.
- **Re-throw-into-unhandled-rejection cluster** (frontend item (a)): a recurring hard-to-kill
  pattern wherever production re-throws inside a fire-and-forget event handler. Noted for future
  mutation runs on similar handlers.
- No equivalent-mutant cluster needed a production change; **no production code was modified**.
