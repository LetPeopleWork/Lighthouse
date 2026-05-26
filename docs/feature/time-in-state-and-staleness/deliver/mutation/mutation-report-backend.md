# Backend Mutation Testing — time-in-state-and-staleness

Tool: Stryker.NET 4.14.2 (local manifest, `Lighthouse.Backend/.config/dotnet-tools.json`).
Config: `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.time-in-state.json`.
Diff range scoped: `4f2e1774` .. `aa325466`.

## Method

Stryker.NET 4.14.2's glob line-range suffix (`file.cs{a..b}`) silently filtered
out **all** mutants in this environment (verified with isolated probes — even a
known-good single-line range yielded zero tested mutants). To get reliable
results the four target files were mutated **whole**, then survivors were
filtered to the feature line ranges during analysis. Kill rate is reported over
the feature surface only (mutants whose line falls inside the diff's new/changed
ranges).

Feature line ranges analysed:
- `WorkItemStateTransitionMapper.cs`: 1–21 (entirely new)
- `LinearWorkTrackingConnector.cs`: 33, 340, 346–361, 417, 421–438, 440–510, 554–614, 638–641, 687–702, 733, 743–756
- `CsvWorkTrackingConnector.cs`: 17–20, 185, 201–233, 328–331
- `LinearResponses.cs`: new lines are auto-property DTOs only (no mutable statements/operators) — Stryker emits no behavioral mutants there; exercised transitively through the connector mapping tests.

Baseline gate before mutating: target suites green (79 tests). After adding
behavior tests: 85 tests green.

## Results (feature surface)

| File | Killed | Survived | Total | Kill % |
|------|-------:|---------:|------:|-------:|
| WorkItemStateTransitionMapper.cs | 2 | 0 | 2 | 100.0% |
| LinearWorkTrackingConnector.cs | 69 | 11 | 80 | 86.2% |
| CsvWorkTrackingConnector.cs | 17 | 2 | 19 | 89.5% |
| **Overall feature surface** | **88** | **13** | **101** | **87.1%** |

(Killed counts include 1 timeout mutant, treated as killed.)

## Survivors killed (mutant → test added)

| Mutant | Behavior gap | Test added |
|--------|--------------|------------|
| Csv L228 `FromState = string.Empty` → string mutation | Synthesized transition's `FromState` was never asserted | `GetWorkItemsForTeam_StateEnteredDateConfigured...` now asserts `FromState Is.Empty` |
| Csv L220 block removal (null state-entered date) | No Doing-row with an empty state-since column | Added fixture row `ITEM-005` (Doing, empty `StateSince`); test asserts its `SyncedTransitions Is.Empty` |
| Linear L351 `&&` → `||` (issue history filter) | History node with one null endpoint never sent | `GetWorkItemsForTeam_HistoryNodeMissingOneEndpoint_DropsThatTransitionAndKeepsTheComplete` |
| Linear L423 `?? []` on `projectNode.History?.Nodes` | Project with `history.nodes: null` never sent | `GetFeaturesForProject_HistoryNodesNull_YieldsFeatureWithoutTransitions` |
| Linear L426 `?? []` on `node.Entries` | Project history node with `entries: null` never sent | `GetFeaturesForProject_HistoryNodeEntriesNull_YieldsFeatureWithoutTransitions` |
| Linear L428 `&&` → `||` (project status filter) | Status entry with one null endpoint never sent | `GetFeaturesForProject_StatusEntryMissingOneEndpoint_DropsThatTransition` |
| Linear L485/L486 project downgrade fallback (statement + `?? []` left) | No project-history-rejection path | `GetFeaturesForProject_ProjectHistoryQueryRejected_ReQueriesWithoutHistoryAndYieldsEmptySyncedTransitions` |
| Linear L691 issue history-fragment fields | Issue query's history connection shape unasserted | `GetWorkItemsForTeam_HistorySupplied_RequestsHistoryConnectionWithExpectedFields` (asserts `history(first:`, `fromState`, `toState`) |

These tests took the issue-history surface from 78.8% → 86.2% and the CSV
state-since surface from 78.9% → 89.5%.

## Survivors remaining (justified)

| Mutant | Status | Justification |
|--------|--------|---------------|
| Linear L476 statement `;` + string `""` | Survived | `DowngradeHistorySupport` `logger.LogWarning(...)`. **Log-only** — no observable behavior; mutating the message text/removing the call changes nothing assertable. Equivalent. |
| Linear L610 statement `;` + string `""` | Survived | `SendQueryWithErrors` `logger.LogDebug("GraphQL Error...")`. **Log-only**. Equivalent. |
| Linear L447 `?? []` (remove right) | Survived | Second-fetch fallback in `FetchAllIssuesForTeam`. After downgrade `historyUnavailable=true`, the retry query carries no history → cannot be rejected → never returns null, so the right operand is unreachable. **Equivalent.** |
| Linear L486 `?? []` (remove right) | Survived | Same unreachable second-fetch fallback for `FetchAllProjects`. **Equivalent.** (The left-side and statement mutants on L485/486 were killed by the new project-rejection test.) |
| Linear L574 statement removal (`if (!onErrors(errors)) return;`) | Survived | Removing the early return still terminates: the null-data response yields an empty page and `historyFieldRejected` is already set, so the rejection→re-query outcome is identical. **Equivalent.** |
| Linear L581 statement removal (`if (!continueToNextPage) return;`) | NoCoverage | Pre-existing pagination short-circuit; `processResult` always returns `true` in all wired paths. Out of new-feature behavior; would need a multi-page mock that asks to stop early. **Accepted.** |
| Linear L585 boolean `HasNextPage ?? false` → `true` | NoCoverage | Pre-existing multi-page loop control. All test fixtures are single-page (`hasNextPage:false`), so the `?? false` default is the only branch reached. Out of new-feature scope. **Accepted.** |
| Linear L691 string `string.Empty` → garbage (issue fragment, `!includeHistory` branch) | Survived | Mocked handler returns canned JSON regardless of query text, so injecting garbage into the downgraded query is not observable without asserting full query equality (brittle). The history-INCLUDED fragment fields are positively asserted by the new query-shape test. Low value. **Accepted.** |
| Linear L747 string `string.Empty` → garbage (project fragment, `!includeHistory` branch) | NoCoverage | Same as L691 for the project fragment. **Accepted.** |
| Csv L330 `SingleOrDefault()` → `Single()` | Survived | The default CSV connection factory always adds exactly one `StateEnteredDateHeader` option, so `Single()` can never throw. **Equivalent.** |
| Csv L330 key literal string mutation | NoCoverage | Stryker coverage-analysis artifact for an inlined `const` (`CsvWorkTrackingOptionNames.StateEnteredDateHeader`). The behavior IS covered — `SupportsTransitionHistory_DependsOnStateEnteredDateColumnBeingConfigured("StateSince", true)` fails if the key literal is wrong — but the per-test coverage capture attributes 0 tests to this inlined mutant. **Accepted (false NoCoverage).** |

All remaining survivors are either log-only, provably equivalent, pre-existing
pagination mechanics outside the new feature, or coverage-analysis artifacts.
None represents an untested feature behavior.

## Verdict

**PASS** — backend feature surface mutation score **87.1%** (88/101), above the
80% threshold. Per-file: mapper 100%, CSV 89.5%, Linear 86.2%.

## New / modified test files

- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnectorHistoryParsingTest.cs` (6 new tests + response builders)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnectorTest.cs` (strengthened transition assertions)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Csv/team-valid-state-since.csv` (added `ITEM-005`: Doing row with empty state-since)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.time-in-state.json` (scoped Stryker config — note: whole-file mutate + analysis-time line filtering due to the 4.14.2 glob-range limitation)

`StrykerOutput/` is gitignored (verified). No production source under
`Lighthouse.Backend/Lighthouse.Backend/` was modified.
