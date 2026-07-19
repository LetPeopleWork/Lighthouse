# Mutation Report — Bug 5522 (Blocked Items Trend Interpolation)

**Date:** 2026-07-19 · **Tool:** Stryker.NET 4.14.1 · **Scope:** feature-changed backend files only
**Config:** `Lighthouse.Backend/stryker-config.json` · **Raw JSON:** `bugfix-5522-mutation-report.json` (this dir, run 2)

## Verdict: ACCEPTED — 77.30% overall; 92.3% on new code (100% excluding provably-equivalent mutants)

Two runs. Run 1: 75.51% (296k/85s). Run 2 (after adding 7 mutation-killing tests): **77.30%** (303k/78s).
381 mutants tested (mutate-glob restricted to the 4 changed production files; 10 970 out-of-scope filtered).
Test filter excluded 2 pre-existing env-failing tests (`ValidLicenseLoaded_*`, fail on clean base).

## Per-file breakdown (run 2)

| File | Run 1 | Run 2 | Killed | Survived | NoCov |
|---|---|---|---|---|---|
| **BlockedCountSeriesBuilder.cs (NEW)** | 61.1% | **88.9%** | 16 | 2 | 0 |
| **BlockedCountSnapshotRepository.cs** | 75.0% | **100%** | 8 | 0 | 0 |
| TeamMetricsController.cs | 81.1% | 80.2% | 150 | 35 | 2 |
| PortfolioMetricsController.cs | 75.9% | 72.1% | 129 | 41 | 9 |

**New-code aggregate: 24/26 = 92.3%.** The 2 builder survivors are provably equivalent
(`AsEnumerable→Reverse`, `OrderBy→OrderByDescending` immediately before a key-unique `ToDictionary` —
ordering is unobservable; verified by hand-applying both mutations: all 3506 tests stay green).
Excluding equivalents: **100% of killable new-code mutants dead.**

## Why overall stays below 80% — accepted debt

The 76 remaining survivors are all in **pre-existing controller code** untouched by this fix:
- Team L455 / Portfolio L540, L558: statement/string mutants in log & drill-down paths
- Team L531 / Portfolio L513: `&&→||` in `blockedItemsAtDate` transition reconstruction
- Portfolio L529/L544: blackout `OrderBy` + comparison mutants
- Plus ~60 more across the two controllers' legacy methods (they are large files; mutants spawn file-wide)

Reaching 80% overall requires ~11 additional kills from legacy controller code — explicitly deferred
(user decision 2026-07-19): hardening pre-existing code is out of scope for a bug fix and belongs in a
dedicated test-hardening task. The 80% gate is judged against the *feature delta*, which passes at 92.3%.

## Tests added in run 2 (mutation-targeted)
- `Services/Implementation/BlockedCountSeriesBuilderTests.cs` (4 tests, real repo over EF InMemory)
- `Services/Implementation/Repositories/BlockedCountSnapshotRepositoryTests.cs` (3 tests, direct repo)
Each mapped to specific survivor IDs; kill effectiveness verified empirically (mutation applied by hand → test fails).

## Post-run safety
Working tree verified clean after both runs (Stryker.NET mutates in-memory assemblies; artifacts gitignored).
