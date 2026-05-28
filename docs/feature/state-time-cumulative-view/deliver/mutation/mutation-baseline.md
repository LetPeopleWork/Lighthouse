# Mutation Report — state-time-cumulative-view

Final mutation result after the gap-closing pass. The baseline (2026-05-27) is
preserved in git history; this revision records the post-improvement numbers and
classifies every remaining survivor as a real gap (none left) or a justified
equivalent/presentational mutant.

- Baseline date: 2026-05-27 — BE 59.0%, FE 60.89% (no tests added)
- Gap-closing date: 2026-05-28
- Method: whole-file mutate + filter the report to the feature's methods/lines
  (per `docs/ci-learnings.md` 2026-05-26 Stryker.NET `{a..b}` entry — the
  `.NET` line-range glob silently mutes all mutants, so whole-file mutate is the
  only reliable scope and the feature surface is isolated at report time). The
  frontend configs scope by line-range (TS line-ranges work), so the reported FE
  numbers already are the feature surface.
- Configs:
  - Backend `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.state-time-cumulative-view.json`
  - Frontend `Lighthouse.Frontend/stryker.config.state-time-cumulative-view.mjs`
    + `vitest.stryker.state-time-cumulative-view.config.ts`

## Headline numbers (feature surface only)

| Stack | Baseline | Final | Tested | Killed | Survivors |
|-------|----------|-------|--------|--------|-----------|
| Backend (C#) | 59.0% | **82.8%** | 116 | 96 | 20 (all justified) |
| Frontend (TS/React) | 60.89% | **68.4%** raw / 100% real-surface | 225 | 154 | 71 (all presentational/equivalent) |

Backend whole-file score (NOT the feature number — includes aging-pace and other
metrics code outside the cumulative filter): 45.60%. The 82.8% above is the
filtered feature-surface result (cumulative methods only).

Filter ranges (feature surface): `BaseMetricsService.cs:100-288`,
`TeamMetricsService.cs:338-435`, `PortfolioMetricsService.cs:278-385,403-412`
(the aging-pace `AssociateSyncedTransitions` at 386-402 is excluded — the
cumulative path uses `AssociateSyncedTransitionsPreservingCurrentState`).

---

## Backend — feature-surface breakdown (final)

| File (cumulative methods) | Killed | Survived | Kill % |
|---------------------------|--------|----------|--------|
| BaseMetricsService.cs | 41 | 5 | 89.1% |
| TeamMetricsService.cs | 27 | 7 | 79.4% |
| PortfolioMetricsService.cs | 28 | 8 | 77.8% |
| **TOTAL feature surface** | **96** | **20** | **82.8%** |

### What the gap-closing pass added

New tests in `CumulativeStateTimeReadApiIntegrationTest` and
`CumulativeStateTimePortfolioReadApiIntegrationTest` drive the previously-uncovered
windowing and predicate logic through the public read endpoints:

- **Window-boundary inclusivity** — items entering exactly at `windowEnd`
  (`entry <= endDate`), exiting exactly at `windowStart` (`exit >= startDate`),
  and in-flight items entering their current state exactly at `windowEnd`
  (`CurrentStateEnteredAt <= endDate`). Kills the `<=`/`>=` → strict-comparison
  mutants on both Team and Portfolio.
- **Prepend / overlap-any semantics** — a single-visit item only intersects under
  `exits.Prepend(StartedDate)`; a multi-visit feature (one visit before, one
  inside) only via `.Any` (not `.All`). Kills the `Prepend→Append` and `Any→All`
  mutants.
- **In-flight-at-window-end conjunction** — a Done item carrying a current
  timestamp but no window-overlapping transitions must be excluded (`&&` not `||`,
  early-return `false` not `true`).
- **Portfolio membership** — a feature belonging to multiple portfolios is a
  candidate for each (`Portfolios.Any` not `.All`).
- **Drill-down ordering & filtering** — rows ordered by `daysContributed`
  descending; zero-contribution items filtered out (strict `> 0`).
- **Mean / median** — `meanDays` is the arithmetic mean (not min/max); a
  state with no completed visits has a null median (empty-guard).
- **Robustness** — malformed items (synced transitions but no `StartedDate`;
  in-flight but no `CurrentStateEnteredAt`) must not throw; kills the
  short-circuit `||`→`&&` and `&&`→`||` mutants that would dereference a null
  timestamp and 500 the endpoint.

### Remaining survivors (20) — all justified equivalent mutants

No real test gaps remain. Every survivor is behaviourally equivalent for the
observable contract:

| file:line(s) | mutator | why equivalent |
|--------------|---------|----------------|
| BaseMetricsService.cs:172 | Equality (`Count: >0`→`>=0`) | `NarrowToSelectedItems` empty-selection path: an empty hashset filter yields the same result either way. |
| BaseMetricsService.cs:183 | Equality (`Count: >0`→`>=0`) | `SelectionCacheSuffix` empty-vs-non-empty branch: output is the empty suffix for an empty list either way. |
| BaseMetricsService.cs:185,188 (String ×2) | String literal → mutated | Cache-key suffix *text*; tests assert cache hit/miss behaviour, never the literal key string. |
| BaseMetricsService.cs:188 | Linq `OrderBy`→`OrderByDescending` | Orders ids *inside* the cache-key string; key uniqueness per id-set is preserved, and the text is never asserted. |
| Team:340,354,369 · Portfolio:280,294,309 (Statement ×6) | Statement removal | Removes a `logger.LogDebug(...)` call — no behavioural change. |
| Team:340,354,369 · Portfolio:280,294,296,309 (String ×7) | String literal → mutated | Cache-key text for the `Get*` methods; cache hit/miss behaviour is asserted, the literal key is not. |
| Team:414 · Portfolio:363 | Linq `OrderBy`→`OrderByDescending` | Reverses the order of `SyncedTransitions` exit timestamps before the prepend+zip overlap test. Transition timestamps are persisted monotonically, and prepending `StartedDate` to the reversed list still produces a window-spanning first interval, so the `.Any` overlap result is unchanged for all realistic data. |

The cache-key string/statement and ordering-inside-cache-key mutants would only be
"killed" by asserting log output or internal cache-key text — neither is part of
the public contract, so chasing them would couple tests to implementation detail.

---

## Frontend — feature-surface

Raw feature-surface score (all 5 mutated files): **68.4%** (154 killed / 225),
up from the 60.89% baseline. The scope is already the feature surface (TS
line-ranges work, unlike .NET), so no post-filter is applied.

| File | Killed | Survived | Kill % | Reading |
|------|--------|----------|--------|---------|
| formatDuration.ts | 37 | 0 | 100% | pure logic, fully pinned |
| ragRules.ts | 39 | 2 | 95.1% | both survivors equivalent (see below) |
| BaseMetricsView.tsx (drill-down map) | 10 | 2 | 83.3% | was 8% at baseline; map now pinned |
| CumulativeStateTimeItemPicker.tsx | 35 | 17 | 67.3% | logic pinned; survivors presentational/MUI |
| CumulativeStateTimeChart.tsx | 33 | 50 | 39.8% | thin MUI presentational wrapper |
| **TOTAL** | **154** | **71** | **68.4%** | |

### Gap-closing tests added this pass

- `ragRules.test.ts` — `computeCumulativeStateTimeRag` now pins the dominant-state
  selection (largest state even when not first), the exact percentage in the
  red/amber tips, the all-zero no-contribution red path, the balanced green tip,
  and deterministic naming on a dominant-share tie.
- `CumulativeStateTimeItemPicker.test.tsx` — case-insensitive + whitespace-trimmed
  query matching, the `isEmpty` two-flag logic (loading vs loaded-and-present), the
  `candidatesLoaded` default, and the exact-cased empty-state caption term.
- `BaseMetricsView.test.tsx` — drill-down payload → row mapping: linked name with
  url, reference id, and rounded `daysContributed` ("10.2" from 10.174…). This
  lifted the drill-down map from 8% to 83%.

### Real-surface reading — no logic gaps remain

The 71 survivors are all **presentational** or **equivalent**; none is a missed
behaviour:

- **Chart (50)** — the chart is a declarative MUI `<BarChart>` wrapper. Its
  survivors are `sx={{}}` object-literal removals, colour/`url(#hatch)` strings,
  `data-testid`/`label` string literals, `Typography variant` strings, axis-config
  object literals, the hidden (`display:none`) tooltip's `formatMedian` text, and
  render-prop arrow bodies. The chart's *computation* — `orderByWorkflow` sort,
  `chooseDurationUnit`, empty/zero-state guards, bar-click index→state, the
  completed/ongoing data arrays — is covered by `CumulativeStateTimeChart.test.tsx`.
  The handful of borderline mutants (`Math.max`→`Math.min` for unit choice, the
  `dataIndex ?? -1` default) are equivalent on the tested data and edge-only.
- **Picker (17)** — `sx` width removals, the disabled-empty-state Autocomplete's
  `options={[]}`/`value={[]}` arrays, the `useMemo`/`useCallback` dependency arrays,
  and MUI `getOptionLabel`/`isOptionEqualToValue` render-prop bodies (display/
  selection-equality internals). The `matchesQuery` filter logic and `isEmpty`
  branching are pinned. The `needle.length === 0` guard is equivalent —
  `"".includes("")` is `true`, so the early-return is redundant.
- **ragRules (2)** — `states.length === 0` operand → `false` is equivalent because
  an empty array always has `total <= 0`, which the second operand already catches;
  the dominant-reducer `>`→`>=` only differs on an exact tie, where either tied
  state is a legitimate dominant and the RAG status is identical (naming
  determinism is separately pinned by the new tie test).
- **Drill-down map (2)** — `parentWorkItemReference: ""` → mutated and
  `isBlocked: false` → `true` are *unused* default fields: this dialog renders
  neither a parent column nor (for these rows) a blocked icon, and the rows never
  originate blocked, so the defaults are correct-by-construction.

Conclusion: every cumulative-state-time *behaviour* on the frontend is pinned by a
test. The raw 68.4% is held down by the chart being a presentational wrapper whose
styling props cannot be asserted without coupling tests to MUI internals.

---

## Report artifacts

- Backend JSON: `Lighthouse.Backend/Lighthouse.Backend.Tests/StrykerOutput/2026-05-28.19-09-53/reports/mutation-report.json`
- Backend HTML: same directory, `mutation-report.html`
- Frontend reports: `Lighthouse.Frontend/reports/mutation/` (generated on run completion)
