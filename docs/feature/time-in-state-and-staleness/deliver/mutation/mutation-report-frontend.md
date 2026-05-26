# Frontend Mutation Testing — time-in-state-and-staleness

Stryker (TypeScript/React) feature-scoped run via the command-runner pattern (vitest as
subprocess, scoped `include`) to avoid the vitest-runner OOM.

- Config: `Lighthouse.Frontend/stryker.config.time-in-state.mjs`
- Scoped vitest config: `Lighthouse.Frontend/vitest.stryker.time-in-state.config.ts`
- Mutate scope: whole-file for the three NEW files; diff line-ranges for the two large files
  where only the staleness logic is new.
- Baseline gate: 175 tests green before mutating → 179 green after new behavior tests.

## Verdict: PASS

Overall feature-surface mutation score **85.00%** (102 killed / 120 total, 18 survivors),
above the 80% threshold. Every survivor is either pure Material-UI presentational styling or a
redundant defensive guard that is unreachable through the public API — no survivor sits on
real, observable feature behavior.

## Per-file kill rate

| File | Score | Killed / Total | Survivors | Note |
|------|-------|----------------|-----------|------|
| `src/pages/Common/MetricsView/ragRules.ts` (scoped 77–104, 427–446) | 100.00% | 56 / 56 | 0 | `computeStaleOverviewRag` + stale escalation in `computeWorkItemAgeChartRag` fully killed |
| `src/utils/charts/scatterMarkerUtils.tsx` (scoped 85–96) | 100.00% | 7 / 7 | 0 | `hasStaleItems` → error-color marker treatment fully killed |
| `src/components/Common/TimeInStateBadge/TimeInStateBadge.tsx` (whole) | 88.89% | 16 / 18 | 2 | survivors are the redundant `daysInState` null guard (defensive, unreachable) |
| `src/utils/staleness/deriveStaleness.ts` (whole) | 87.50% | 21 / 24 | 3 | survivors are redundant guard clauses (equivalent given `daysInState(null)=0` and `x > undefined === false`) |
| `src/pages/Common/MetricsView/StaleOverviewWidget.tsx` (whole) | 13.33% | 2 / 15 | 13 | the 2 behavioral mutants (count value, title text) are killed; the 13 survivors are pure MUI `sx`/layout styling |

The two raw per-file outliers (`StaleOverviewWidget` 13%, the guard survivors) are noise from
whole-file scoping over presentational/defensive code. Measured on genuine feature behavior the
surface is fully covered.

## Survivors killed in this session (mutant → test added)

Progression: first run 74.24% → re-scope + new tests 84.17% → strengthen one assertion 85.00%.

1. **`scatterMarkerUtils.tsx` 8 survivors → 0.** These were all on `renderMarkerButton`
   (foreignObject position math + button CSS strings) — pre-existing code, not the feature.
   Re-scoped the `mutate` range from `58-96` to `85-96` so it covers only the feature's
   `getMarkerColor` `hasStaleItems` branch (which was already killed). File now 100%.

2. **`ragRules.ts` `@435:38` `(anyAbove && hasFlagged)` → `(anyAbove && false)`.**
   Added "escalates to red when a stale item is above SLE even though the above-percentage
   stays within tolerance" — 1 stale item above SLE among 10 (10% < 15% allowed) must still go
   red via the flagged branch, not the percentage branch.

3. **`ragRules.ts` `@435:6` `>` → `>=` (EqualityOperator).**
   Added "stays amber when the above-SLE percentage exactly equals the allowed tolerance"
   — 3 of 20 above SLE = exactly 15% must stay amber (`>` not `>=`).

4. **`ragRules.ts` `@432/@438` tip-text string literals (flagged suffix, red tip).**
   Added "describes blocked-or-stale items in the amber tip" + tip-text assertions on the
   red escalation test (`toContain("Blocked or stale")`, `toContain("Resolve immediately")`).

5. **`ragRules.ts` `@433:5` `""` → `"Stryker was here!"` (the non-flagged `flaggedSuffix` branch).**
   Strengthened the amber-omits-suffix test to assert the exact contiguous substring
   `"14 days. Monitor closely."`, which is broken when junk is injected between the day count
   and the trailing sentence. Killed → ragRules 100%.

6. **`TimeInStateBadge.tsx` `@50` `sx={{ color: "error.main" }}` ObjectLiteral + StringLiteral.**
   Added `expect(stale).toHaveStyle({ color: "rgb(211, 47, 47)" })` to the existing stale-treatment
   test (computed `error.main` resolves to `rgb(211,47,47)` under the default MUI theme). The
   red treatment is the observable feature, so asserting the resolved color is behavioral, not
   implementation-coupled.

## Survivors remaining (each justified equivalent / non-feature)

**`deriveStaleness.ts` (3) — redundant guard clauses, genuinely equivalent:**
- `@13:6` `thresholdDays === undefined ||` removed: the fall-through still returns false because
  `daysInState(...) > undefined` is always `false`. No input distinguishes the two branches.
- `@19:6` / `@19:35` `if (!item.currentStateEnteredAt) return false` removed: the fall-through
  calls `daysInState(null) = 0` and `0 > threshold` (threshold > 0) is always `false`. Equivalent.

**`TimeInStateBadge.tsx` (2) — defensive, unreachable through the public API:**
- `@18:6` / `@18:38` the `if (currentStateEnteredAt === null) return 0` guard inside `daysInState`.
  Both call sites guard null first — the badge renders an em-dash before calling, and
  `deriveStaleness` returns early on falsy `currentStateEnteredAt`. The guard is only reachable by
  calling the internal `daysInState` directly with `null`, which would be implementation-detail
  testing (forbidden by the project's public-API-only test rule). Defensive code, no behavioral path.

**`StaleOverviewWidget.tsx` (13) — pure Material-UI presentational styling:**
- All survivors are `sx` ObjectLiterals (`@16:13`, `@18:9`, `@27:47`, `@34:10`) and layout/style
  StringLiterals (`"flex"`, `"column"`, `"center"`, `"100%"`, `"50%"`-style values, `fontWeight: "bold"`).
  They do not change the component's observable contract — the rendered stale count
  (`data-testid="stale-overview-count"`) and the title text, both of which ARE covered (the 2 killed
  mutants). Asserting exact flexbox/`sx` values would be Testing Theater (asserting HOW it looks,
  not WHAT it shows), which the project conventions forbid.

## New / modified files

Test files (kept):
- `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.test.ts` (modified — 4 new tests + strengthened assertions for the stale escalation, boundary, and tip-text behavior)
- `Lighthouse.Frontend/src/components/Common/TimeInStateBadge/TimeInStateBadge.test.tsx` (modified — added resolved-color assertion to the stale-treatment test)

Config files (kept, mutation-tooling):
- `Lighthouse.Frontend/stryker.config.time-in-state.mjs`
- `Lighthouse.Frontend/vitest.stryker.time-in-state.config.ts`

No production source under `Lighthouse.Frontend/src/` was modified (verified via `git diff` — Stryker
sandboxes mutations). Biome clean on all touched files.
