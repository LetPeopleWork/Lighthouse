# Mutation Baseline — state-time-cumulative-view

BASELINE ONLY. No tests or production code were added or modified. Survivors are
listed, not killed. A human decides what to address.

- Date: 2026-05-27
- Method: whole-file mutate + filter the report to the feature's methods/lines
  (per `docs/ci-learnings.md` 2026-05-26 Stryker.NET `{a..b}` entry — the
  `.NET` line-range glob silently mutes all mutants, so whole-file mutate is the
  only reliable scope and the feature surface is isolated at report time).
- Configs used as-is (not edited):
  - Backend `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.state-time-cumulative-view.json`
  - Frontend `Lighthouse.Frontend/stryker.config.state-time-cumulative-view.mjs`
    + `vitest.stryker.state-time-cumulative-view.config.ts`

## Headline numbers (feature surface only)

| Stack | Feature-surface kill rate | Survivors (Survived + NoCoverage) |
|-------|---------------------------|-----------------------------------|
| Backend (C#) | **59.0%** (69 killed incl. timeout / 117 tested) | 47 |
| Frontend (TS/React) | **see Frontend section** | see Frontend section |

Backend whole-file score (NOT the feature number — includes aging-pace and other
metrics code outside the cumulative filter): 42.21% (332 killed / 589 tested).
The 59.0% above is the filtered feature-surface result.

---

## Backend — feature-surface breakdown

Filtered to the cumulative-state-time methods only. Mutants outside these line
ranges (aging-pace, percentiles, other metrics) are excluded — they are NOT this
feature and the cumulative test filter does not cover them.

| Bucket | Killed (incl. timeout) | Survived | NoCoverage | Tested | Kill % |
|--------|------------------------|----------|------------|--------|--------|
| BaseMetricsService (core cumulative methods) | 34 | 8 | 0 | 42 | 81.0% |
| BaseMetricsService (CumulativeMean/Median — SHARED w/ aging-pace) | 2 | 2 | 0 | 4 | 50.0% |
| TeamMetricsService (cumulative methods) | 18 | 16 | 0 | 34 | 52.9% |
| PortfolioMetricsService (cumulative methods) | 15 | 21 | 1 | 37 | 40.5% |
| **TOTAL feature surface** | **69** | **47** | **1** | **117** | **59.0%** |

Note: `CompileError` mutants (12 + 4 + 5 + 8 = 29 across these files) are excluded
from the denominator per Stryker convention (a compile-error mutant is untestable,
not a survivor). They are not counted as gaps.

### Key structural observation

`IntersectsWindow`, `IsInFlightAtWindowEnd`, `ResolveCumulativeStateTimeCandidates`,
`AssociateSyncedTransitionsPreservingCurrentState`, and the cache-key wiring inside
the `Get*` methods are **duplicated near-identically between TeamMetricsService and
PortfolioMetricsService**. The same survivor shows up twice (once per service). So
the ~37 Team+Portfolio survivors collapse to roughly half that many *distinct* test
gaps — fixing the window/in-flight logic coverage once conceptually closes both.

### Backend survivor list (`file:line` · mutator · method · hint)

#### BaseMetricsService.cs — core cumulative (8 survivors)

| file:line | mutator | method | hint |
|-----------|---------|--------|------|
| BaseMetricsService.cs:148 | Equality (`> 0` → `>= 0`) | ComputeCumulativeStateTimeItems | likely real gap — boundary: items contributing exactly 0 days should be filtered out; no test pins the strict `>`. |
| BaseMetricsService.cs:172 | Equality (`Count: > 0` → `>= 0`) | NarrowToSelectedItems | likely equivalent — `{ Count: >= 0 }` pattern vs `> 0`; empty-list path behaves the same downstream (filter on empty hashset). |
| BaseMetricsService.cs:183 | Equality (`Count: > 0` → `>= 0`) | SelectionCacheSuffix | likely equivalent — same empty-vs-nonempty cache-suffix branch; output identical for empty. |
| BaseMetricsService.cs:185 | String (`""` → `"Stryker was here!"`) | SelectionCacheSuffix | likely equivalent — only the empty-suffix return value; cache key text is never asserted by tests. |
| BaseMetricsService.cs:188 | String (suffix template → `""`) | SelectionCacheSuffix | likely equivalent — cache-key string content not asserted (cache hit/miss behaviour is, the literal text is not). |
| BaseMetricsService.cs:188 | Linq (`OrderBy` → `OrderByDescending`) | SelectionCacheSuffix | likely equivalent — ID ordering inside the cache-key string only; key uniqueness preserved either way, text not asserted. |
| BaseMetricsService.cs:219 | Logical (`&&` → `\|\|` on `entryIntoState.HasValue && !IsNullOrEmpty(FromState)`) | CompletedVisits | likely real gap — controls whether a completed visit is emitted; a test exercising a transition with null FromState or no entry would catch this. |
| BaseMetricsService.cs:230 | Logical (`&&` → `\|\|` on Doing + CurrentStateEnteredAt.HasValue) | IsInFlight | likely real gap — in-flight predicate; a Doing item with null CurrentStateEnteredAt (or a non-Doing item with a timestamp) would distinguish. |

#### BaseMetricsService.cs — CumulativeMean/Median (SHARED with aging-pace, 2 survivors)

| file:line | mutator | method | hint |
|-----------|---------|--------|------|
| BaseMetricsService.cs:276 | Linq (`Average()` → `Min()`) | CumulativeMean [SHARED] | likely real gap BUT shared surface — mean-vs-min on completed visit days; out of strict cumulative-only scope (also feeds aging-pace). Flag for human: is mean asserted anywhere? |
| BaseMetricsService.cs:282 | Block removal (`{}`) | CumulativeMedian [SHARED] | likely real gap BUT shared surface — empty-guard block removed; shared helper. Verify median value is asserted, not just non-null. |

#### TeamMetricsService.cs (16 survivors)

| file:line | mutator | method | hint |
|-----------|---------|--------|------|
| TeamMetricsService.cs:340 | Statement removal | GetCumulativeStateTimeForTeam | likely equivalent — guard/validation statement; behaviour unchanged for tested inputs. |
| TeamMetricsService.cs:340 | String (`""`) | GetCumulativeStateTimeForTeam | likely equivalent — cache-key text, not asserted. |
| TeamMetricsService.cs:354 | Statement removal | GetCumulativeStateTimeItemsForTeam | likely equivalent — guard statement. |
| TeamMetricsService.cs:354 | String (`""`) | GetCumulativeStateTimeItemsForTeam | likely equivalent — cache-key text. |
| TeamMetricsService.cs:359 | Linq (`OrderByDescending` → `OrderBy`) | GetCumulativeStateTimeItemsForTeam | likely real gap — drill-down item ordering (descending by days) is user-visible; no test pins ordering direction. |
| TeamMetricsService.cs:369 | Statement removal | GetCumulativeStateTimeCandidatesForTeam | likely equivalent — guard statement. |
| TeamMetricsService.cs:369 | String (`""`) | GetCumulativeStateTimeCandidatesForTeam | likely equivalent — cache-key text. |
| TeamMetricsService.cs:371 | String (`$""`) | GetCumulativeStateTimeCandidatesForTeam | likely equivalent — interpolated cache-key text. |
| TeamMetricsService.cs:409 | Logical (`\|\|` → `&&` on the early-out guard) | IntersectsWindow | likely real gap — window-intersection guard; an item with StartedDate but zero transitions (or vice-versa) would distinguish. |
| TeamMetricsService.cs:411 | Boolean (`false` → `true`) | IntersectsWindow | likely real gap — the no-data early-return value; a non-intersecting item with no transitions should be excluded. |
| TeamMetricsService.cs:414 | Linq (`OrderBy` → `OrderByDescending`) | IntersectsWindow | likely real gap — transition ordering feeds the zip-overlap; reversing could change intersection result for multi-transition items. |
| TeamMetricsService.cs:419 | Linq (`Prepend` → `Append`) | IntersectsWindow | likely real gap — prepend StartedDate as first entry boundary; append misplaces it, changing the entry/exit pairing. |
| TeamMetricsService.cs:421 | Equality (`<=` → `<` on `entry <= endDate`) | IntersectsWindow | likely real gap — window boundary inclusivity; an item entering exactly at endDate. |
| TeamMetricsService.cs:421 | Equality (`>=` → `>` on `exit >= startDate`) | IntersectsWindow | likely real gap — window boundary inclusivity; an item exiting exactly at startDate. |
| TeamMetricsService.cs:427 | Logical (`&&` → `\|\|`) | IsInFlightAtWindowEnd | likely real gap — in-flight-at-window-end predicate; needs a case with one conjunct false. |
| TeamMetricsService.cs:429 | Equality (`<=` → `<` on `CurrentStateEnteredAt <= endDate`) | IsInFlightAtWindowEnd | likely real gap — boundary: item entering its state exactly at endDate. |

#### PortfolioMetricsService.cs (21 survivors — note Team duplicates)

| file:line | mutator | method | hint |
|-----------|---------|--------|------|
| PortfolioMetricsService.cs:280 | Statement removal | GetCumulativeStateTimeForPortfolio | likely equivalent — guard statement (mirrors Team:340). |
| PortfolioMetricsService.cs:280 | String (`""`) | GetCumulativeStateTimeForPortfolio | likely equivalent — cache-key text. |
| PortfolioMetricsService.cs:294 | Statement removal | GetCumulativeStateTimeItemsForPortfolio | likely equivalent — guard statement. |
| PortfolioMetricsService.cs:294 | String (`""`) | GetCumulativeStateTimeItemsForPortfolio | likely equivalent — cache-key text. |
| PortfolioMetricsService.cs:296 | String (`$""`) | GetCumulativeStateTimeItemsForPortfolio | likely equivalent — interpolated cache-key text. |
| PortfolioMetricsService.cs:299 | Linq (`OrderByDescending` → `OrderBy`) | GetCumulativeStateTimeItemsForPortfolio | likely real gap — drill-down ordering (mirrors Team:359), user-visible, unpinned. |
| PortfolioMetricsService.cs:309 | Statement removal | GetCumulativeStateTimeCandidatesForPortfolio | likely equivalent — guard statement. |
| PortfolioMetricsService.cs:309 | String (`""`) | GetCumulativeStateTimeCandidatesForPortfolio | likely equivalent — cache-key text. |
| PortfolioMetricsService.cs:311 | String (`$""`) | GetCumulativeStateTimeCandidatesForPortfolio | likely equivalent — interpolated cache-key text. |
| PortfolioMetricsService.cs:320 | Linq (`Any()` → `All()` on `f.Portfolios.Any`) | ResolveCumulativeStateTimeCandidates | likely real gap — feature-to-portfolio membership filter; a feature belonging to multiple portfolios would distinguish Any vs All. |
| PortfolioMetricsService.cs:358 | Logical (`\|\|` → `&&`) | IntersectsWindow | likely real gap — mirrors Team:409. |
| PortfolioMetricsService.cs:360 | Boolean (`false` → `true`) | IntersectsWindow | likely real gap — mirrors Team:411. |
| PortfolioMetricsService.cs:363 | Linq (`OrderBy` → `OrderByDescending`) | IntersectsWindow | likely real gap — mirrors Team:414. |
| PortfolioMetricsService.cs:368 | Linq (`Any()` → `All()`) | IntersectsWindow | likely real gap — overlap any-of reduction; All would require every entry/exit pair to overlap. (Team's equivalent was killed.) |
| PortfolioMetricsService.cs:368 | Linq (`Prepend` → `Append`) | IntersectsWindow | likely real gap — mirrors Team:419. |
| PortfolioMetricsService.cs:370 | Logical (`&&` → `\|\|`) | IntersectsWindow | likely real gap — overlap predicate `entry <= endDate && exit >= startDate`; OR widens intersection. |
| PortfolioMetricsService.cs:370 | Equality (`<=` → `<`) | IntersectsWindow | likely real gap — mirrors Team:421 (endDate boundary). |
| PortfolioMetricsService.cs:370 | Equality (`>=` → `>`) | IntersectsWindow | likely real gap — mirrors Team:421 (startDate boundary). |
| PortfolioMetricsService.cs:376 | Logical (`&&` → `\|\|`, first conjunct) | IsInFlightAtWindowEnd | likely real gap — mirrors Team:427. |
| PortfolioMetricsService.cs:376 | Logical (`&&` → `\|\|`, second conjunct) | IsInFlightAtWindowEnd | likely real gap — three-conjunct predicate; needs cases isolating each conjunct. |
| PortfolioMetricsService.cs:378 | Equality (`<=` → `<`) | IsInFlightAtWindowEnd | likely real gap — mirrors Team:429 (endDate boundary). |

### Backend reading

- The **core domain math (BaseMetricsService cumulative methods) is well-tested
  at 81%**. Its survivors are mostly cache-key-string and ordering-inside-cache-key
  noise (equivalent) plus two genuine predicate-logic gaps (`CompletedVisits` line
  219, `IsInFlight` line 230).
- The **gap concentration is the windowing logic in the Team/Portfolio adapters**
  (`IntersectsWindow`, `IsInFlightAtWindowEnd`, `ResolveCumulativeStateTimeCandidates`).
  These are boundary-inclusivity (`<=`/`>=`), conjunction-vs-disjunction, and
  ordering mutants that survive because the candidate-resolution tests likely use
  windows comfortably inside/outside the range rather than exactly on the boundary.
  Because the logic is duplicated, the distinct gap is roughly half the 37 raw
  Team+Portfolio survivors.
- The cache-key-string survivors (statement/`""`/`$""` mutants on the `Get*`
  methods) are almost certainly equivalent for kill purposes — tests assert cache
  hit/miss behaviour, not the literal key text. Not worth chasing.

---

## Frontend — feature-surface

Frontend Stryker (TS/React) was still running when the backend report was
finalized (no coverage analysis + concurrency 1 + per-mutant vitest fork startup
makes it slow: 225 mutants, ~40m+ estimated). This section is completed once that
run finishes. The frontend configs scope by line-range (`file.ts:start-end`), so
the whole reported score IS the feature surface — no post-hoc filtering needed.

_Status at backend hand-off: ~20% (46/225 tested, 22 survived)._

<!-- FRONTEND_RESULTS_PLACEHOLDER -->

---

## Report artifacts

- Backend JSON: `Lighthouse.Backend/Lighthouse.Backend.Tests/StrykerOutput/2026-05-27.21-13-38/reports/mutation-report.json`
- Backend HTML: `Lighthouse.Backend/Lighthouse.Backend.Tests/StrykerOutput/2026-05-27.21-13-38/reports/mutation-report.html`
- Frontend reports: `Lighthouse.Frontend/reports/mutation/` (generated on run completion)
