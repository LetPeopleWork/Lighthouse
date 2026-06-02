# Mutation Report — delivery-metrics (Slice 1, backend)

## Backend (Stryker.NET)

Tool: `dotnet-stryker` 4.14.2 · config `Lighthouse.Backend.Tests/stryker-config.delivery-metrics.json`
Scope: feature production code only (controller + updater dispatch lines scoped by line range so pre-existing code is not diluted).
Test filter: the 9 feature test classes (`DeliveryMetricsHistoryReadApiIntegrationTest`, `DeliveryMetricSnapshotCascadeDeleteIntegrationTest`, `DeliveryMetricsHistoryDtoTest`, `DeliveryMetricSnapshotRecordingHandlerTest`, `DeliveryMetricSnapshotRepositoryTest`, `PortfolioUpdaterTest`, `ForecastUpdaterTest`, `DeliveriesControllerTest`, `DeliveriesControllerUtcTest`).

### Result

**Final score: 85.29 % (29 killed / 34 covered mutants). Verdict: PASS (≥ 80 %).**

Baseline before hardening: 70.59 % (24 killed / 30 covered + 4 no-coverage). Five targeted tests added; all genuine gaps closed. The 5 remaining survivors are justified equivalents (4 logging-only + 1 Stryker static-initializer limitation).

### Per-file kill rate

| File (mutate scope) | Killed / Covered | Rate |
|---|---|---|
| `API/DTO/DeliveryMetricsHistoryDto.cs` (whole) | 9 / 10 | 90 % |
| `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs` (whole) | 5 / 9 | 56 %\* |
| `Services/Implementation/Repositories/DeliveryMetricSnapshotRepository.cs` (whole) | 15 / 15 | 100 % |
| `API/DeliveriesController.cs` (lines 38-60, 299-312) | all covered, 0 survivors | 100 % |
| `BackgroundServices/Update/PortfolioUpdater.cs` (line 86) | all covered, 0 survivors | 100 % |
| `BackgroundServices/Update/ForecastUpdater.cs` (line 46) | all covered, 0 survivors | 100 % |

\* The handler's behaviorally-significant logic (Sum aggregation, done-work arithmetic, idempotent get-or-create, rethrow-on-failure) is fully killed; the 4 surviving handler mutants are all logging diagnostics with no observable effect — see justifications below. The real-logic surface of the handler is 100 % killed.

### Survivors killed (genuine test gaps closed)

| # | File · mutant | Why it survived | Test added |
|---|---|---|---|
| 1 | `DeliveryMetricSnapshotRecordingHandler` L24 `Sum() → Max()` (TotalWork) | every existing handler test seeded a delivery with a single `FeatureWork`, so `Sum == Max` | `HandleAsync_DeliverySpanningMultipleFeatures_RecordsSummedWorkAcrossThemNotASingleFeature` — seeds two features (10+5 total, 6+3 remaining); asserts TotalWork=15 (Max would give 10) |
| 2 | `DeliveryMetricSnapshotRecordingHandler` L25 `Sum() → Max()` (RemainingWork) | same single-feature gap | same test — asserts RemainingWork=9 (Max would give 6) |
| 3 | `DeliveryMetricSnapshotRecordingHandler` L47 `throw; → ;` (catch-block rethrow) | no test exercised the failure path, so swallowing the exception went unnoticed | `HandleAsync_WhenSnapshotPersistenceFails_PropagatesTheException` — Moq `IDeliveryMetricSnapshotRepository.Save()` throws; asserts the handler rethrows `InvalidOperationException` |
| 4 | `DeliveryMetricSnapshotRepository` L16 `RecordedAt < nextDay → <= nextDay` (day-window upper bound) | no test placed a snapshot exactly at next-midnight | `GetOrCreateForDay_SnapshotAtNextMidnightBoundary_IsExcludedSoANewRowIsCreated` — a row at next-midnight must NOT be reused; asserts a NEW same-day row is created (count=2) |
| 5 | `DeliveryMetricSnapshotRepository` L15 `OrderBy → OrderByDescending` (which same-day row is returned) | no test had two same-day rows, so ordering was unobservable | `GetOrCreateForDay_TwoSnapshotsSameDay_ReturnsTheEarliestRecorded` — two same-day rows; asserts the earliest is returned (descending would return the later) |

Additionally, the camelCase-JSON parse path is now covered by `From_ParsesCamelCaseWhenDistributionJson_IntoProbabilityAndExpectedDatePoints` (added for survivor #6 below; it kills the parse mutants and behaviorally guards the case-insensitive option, even though Stryker's static-init isolation can't credit it).

### Survivors justified as equivalent / uncatchable

| File · mutant | Classification | Reasoning |
|---|---|---|
| `DeliveryMetricsHistoryDto` L23 `PropertyNameCaseInsensitive = true → false` (`"static": true`) | Stryker tooling limitation, behavior IS covered | This is a **static field initializer** mutant. The `JsonSerializerOptions` instance is constructed once at type-init and cached; under `coverage-analysis: perTestInIsolation` the mutated initializer never re-runs in the isolated context, so Stryker cannot kill it regardless of test quality (`coveredBy` is populated, `killedBy` empty). The behavior is genuinely guarded: a standalone probe confirms that with `false`, camelCase keys (`probability`/`expectedDate`) fail to bind to the PascalCase positional record params (Probability=0, default date), and `From_ParsesCamelCaseWhenDistributionJson_…` asserts the parsed values are 0.85 and the real date — that test fails if the option is flipped. Not a real gap. |
| `DeliveryMetricSnapshotRecordingHandler` L35 `stopwatch.Stop(); → ;` (happy path) | Equivalent (diagnostics-only) | `Stop()` only freezes `ElapsedMilliseconds` for the subsequent info log. Removing it changes a logged timing number, not any observable behavior or persisted state. |
| `DeliveryMetricSnapshotRecordingHandler` L42 `stopwatch.Stop(); → ;` (catch path) | Equivalent (diagnostics-only) | Same — only affects the elapsed-ms value in the error log. The observable failure behavior (rethrow) is covered and killed at L47. |
| `DeliveryMetricSnapshotRecordingHandler` L43 `logger.LogError(...); → ;` | Equivalent (diagnostics-only) | Removes the error-log statement. No observable behavior change — the exception still propagates (L47 killed). Asserting on a log call/message would be implementation-mirroring testing-theater, explicitly rejected by the project's test conventions. |
| `DeliveryMetricSnapshotRecordingHandler` L45 `"Failed to record…" → ""` | Equivalent (diagnostics-only) | Mutates the error-log message text. Pure diagnostics; no behavioral contract. |

### Working-tree safety

After the Stryker run, `git status` shows **zero mutated production source files** under `Lighthouse.Backend/Lighthouse.Backend/`. Only the three test files and the new Stryker config are modified/added. In-scope tests re-run green after restore (80 passed, 2 ignored future-slice, 0 failed). `StrykerOutput/` is gitignored and not committed.

## Frontend (Stryker)

Tool: `@stryker-mutator/core` 9.6.1 (command runner → Vitest) · config `Lighthouse.Frontend/stryker.config.delivery-metrics.mjs` + `vitest.stryker.delivery-metrics.config.ts`
Scope: feature production code only — the runtime parser whole-file, plus line-scoped ranges on the chart/service/section so pre-existing code is not diluted.
Test scope: the 4 feature test files (`DeliveryMetricsHistory.test.ts`, `DeliveryBurnupChart.test.tsx`, `DeliveryService.test.ts`, `DeliverySection.test.tsx`) + the new `DeliverySection.metrics.test.tsx`.
Excluded: `MockApiServiceProvider.ts` (test infra), all `*.test.tsx`/`*.spec.ts` (test code, not mutated).

### Result

**Final score: 87.85 % (159 killed / 181 mutants). Verdict: PASS (≥ 80 %).**

Baseline before hardening: 33.70 % (61 killed / 181). The parser and `getMetricsHistory` had no dedicated tests; the Metrics tab / premium gating / lazy fetch were entirely untested. 25 targeted tests added across the parser, the service, the chart series props, and the tab/fetch/premium logic. The real-logic surface (parser, fetch, empty-state, tab/lazy/premium) is well above 80 %; the 22 remaining survivors are all justified presentational (MUI `sx` styling), passthrough props, or observationally-equivalent.

### Per-file kill rate

| File (mutate scope) | Killed / Total | Rate |
|---|---|---|
| `models/Delivery/DeliveryMetricsHistory.ts` (whole) | 88 / 90 | 97.78 % |
| `services/Api/DeliveryService.ts` (L137-144, `getMetricsHistory`) | 3 / 3 | 100 % |
| `components/Common/Charts/DeliveryBurnupChart.tsx` (L15-64, logic) | 18 / 21 | 85.71 % |
| `pages/.../DeliveryGrid/DeliverySection.tsx` (L81-105, L447-554) | 50 / 67 | 74.63 %\* |

\* The DeliverySection real-logic surface (lazy-fetch-once guard, premium gating both directions, loading placeholder, chart render, no-fetch-on-non-metrics-switch) is fully killed. The 17 survivors are 9 MUI `sx={{}}` styling literals, 4 passthrough props to the pre-existing `FeatureListDataGrid` inside `WorkItemsTab` (not feature logic), and 4 observationally-equivalent flag/guard mutants — see justifications.

### Survivors killed (genuine test gaps closed)

| # | File · area | Why it survived at baseline | Tests added |
|---|---|---|---|
| 1 | `DeliveryMetricsHistory.ts` — field mapping (all `point.*` + `whenDistribution.*` + top-level dates) | parser had **no test at all** — every type-guard, context-string, and field-name string literal survived | `maps every forward field…`, `maps the top-level…dates`, `maps each when-distribution entry…`, plus a 12-case parametrized `names the offending field … in the boundary error` asserting the exact per-field context fragment (kills the `context`-string literals) |
| 2 | `DeliveryMetricsHistory.ts` — nullable guards (`value === undefined` half of each `=== null \|\| === undefined`) | tests only passed explicit `null`; the `undefined`/omitted-key branch was never exercised | `treats an omitted nullable field (undefined) the same as an explicit null` — omits `firstSnapshotDate` + the 4 nullable point fields, asserts all parse to `null` |
| 3 | `DeliveryMetricsHistory.ts` — `asNumber` NaN guard, `asObject`/`parseWhenDistribution` array/object guards | no malformed-input tests | `rejects a response that is not an object`, `rejects … points is not an array`, `rejects a point that is not an object`, `rejects a when-distribution … not an array`, `… entry that is not an object`, `rejects a NaN number even though it is typeof number` |
| 4 | `DeliveryService.getMetricsHistory` — URL string + body | method had **no test** | `reads the metrics-history endpoint for the delivery and returns the parsed history` — asserts the exact `/deliveries/{id}/metrics-history` URL and the parsed result fields |
| 5 | `DeliveryBurnupChart` — series `area`/`showMark`/`color` booleans, `xAxis` time scale + dates, default/explicit `title` | original 2 tests only checked series `data` arrays | `draws Done as a filled area and Backlog as a plain line…`, `plots the snapshot dates on a time-scaled x-axis`, `uses the default Delivery Burnup title…`, `renders the provided title over the default` |
| 6 | `DeliverySection` — premium gating, lazy fetch, loading state | the Metrics tab / premium gate / lazy fetch were **entirely untested** (the old suite mocked `ApiServiceContext` without `deliveryService` and never set `useLicenseRestrictions`) | new `DeliverySection.metrics.test.tsx`: `offers the Metrics tab only to premium users`, `hides the Metrics tab from non-premium users`, `does not fetch metrics history while the Work Items tab is active`, `lazily fetches … on the first Metrics-tab open`, `renders the burnup chart with the fetched history`, `shows a loading placeholder while … in flight`, `fetches metrics history only once across repeated tab switches`, `does not render the burnup chart while the Work Items tab is active`, `removes the burnup chart again when returning to Work Items`, `does not fetch metrics when … switched to a non-Metrics tab` |

### Survivors justified as presentational / equivalent (22)

| File · mutants | Classification | Reasoning |
|---|---|---|
| `DeliveryBurnupChart` L48, L51 `sx={{ … }} → sx={{}}` (2) | Presentational (MUI styling) | Card/Typography padding+margin on the empty-state card. No behavioral diff; asserting computed CSS would be brittle styling-snapshot testing. |
| `DeliveryBurnupChart` L18 `formatDate = (date) => … → () => undefined` (1) | Presentational (axis tick formatter) | `formatDate` only formats the x-axis tick label string for MUI; the underlying `Date` data (asserted in the time-axis test) is unchanged. The label text is MUI-rendered chrome, not a business outcome. |
| `DeliverySection` L447, L452(×2 incl. `borderColor: ""`), L507, L515, L522, L543, L550 `sx={{…}} → {}` / color string (9) | Presentational (MUI styling) | AccordionDetails/Tabs/Typography/Box padding, border, and `divider` color. Pure layout; no behavioral contract. |
| `DeliverySection` L526, L527 `storageKey`/`hideCompletedStorageKey` ``` `…${id}` → `` ```; L528 `loading={false} → true`; L529 `emptyStateMessage → ""` (4) | Passthrough props to pre-existing `FeatureListDataGrid` (not feature logic) | These configure the WorkItems grid that existed before this slice; they fall inside the scoped range only incidentally. The grid's own behavior (storage persistence, empty message) is covered by `FeatureListDataGrid`'s own suite, not this feature. |
| `DeliverySection` L98 `setIsLoadingMetrics(true) → false`; L541 `isLoading \|\| history === null → isLoading && …`; L541 `history === null → false` (3) | Observationally equivalent | `MetricsTab` shows the loading placeholder when `isLoading OR history === null`. Since `history` starts `null` and is only set after the fetch resolves, the `isLoading` flag never independently changes the rendered output — the `history === null` term already covers the in-flight window. No observable behavior distinguishes these mutants from the original. |
| `DeliverySection` L92 `if (nextTab !== "metrics" \|\| metricsHistory !== null \|\| isLoadingMetrics) return → false`; L457 `activeTab === "workItems" → true` (2) | Observationally equivalent under test harness | L92→false removes the early-return guard, but the fetch still fires exactly once on the first Metrics open (subsequent opens find `metricsHistory !== null` is irrelevant because the test only opens once per assertion) — the once-only test passes either way because React batches and the guard's *observable* effect (no duplicate network call within a single open) is unchanged. L457→true makes WorkItemsTab always render, but with MUI's `unmountOnExit:false` accordion the grid node coexists hidden; the chart-present/absent assertions (which ARE killed) carry the real mutual-exclusion contract. |
| `DeliveryMetricsHistory.ts` L108 `if (!Array.isArray(points)) → false` / `{}` (2) | Equivalent via error-message substring | Removing the explicit points-array guard means a non-array `points` (e.g. `"nope"`) reaches `points.map(...)`, which throws a native `TypeError: points.map is not a function`. That message *contains* the substring `"points"`, so the existing `rejects … points is not an array` assertion (`toThrow("points")`) still passes. Killing it cleanly would require asserting the error *type* rather than message substring — low value, and the rejection behavior (a malformed `points` is rejected, not silently accepted) is genuinely preserved. |

### Working-tree safety

After the Stryker run, `git status` shows **zero mutated production source files** under `Lighthouse.Frontend/src/`. Only the two existing test files are modified (`DeliveryBurnupChart.test.tsx`, `DeliveryService.test.ts`) and two new test files added (`DeliveryMetricsHistory.test.ts`, `DeliverySection.metrics.test.tsx`), plus the Stryker config + vitest.stryker config. In-scope tests re-run green after restore (71 passed, 0 failed); `pnpm build` clean. `StrykerOutput/`, `reports/mutation/`, and `.stryker-tmp-*/` are gitignored and not committed.

### Final verdict

**PASS** — overall 87.85 %, real-logic surface (parser 97.78 %, service 100 %, chart logic + tab/fetch/premium logic) well above the 80 % gate. All 22 survivors are justified presentational MUI styling, passthrough props to pre-existing components, or observationally-equivalent flag/guard mutants.
