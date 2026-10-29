# Story 6389 — expected RED classification (DISTILL, 2026-10-29)

All 12 scenarios in `Lighthouse.Frontend/src/layerBoundaries.enforcement.test.ts` are `it.skip` today.

## What DELIVER must do first (applies to every scenario)

1. Install `fallow` (exact pin) as a dev dependency. Until then every scenario that reaches `findLeaks` fails at `nodeRequire.resolve("fallow/bin/fallow")` with `Cannot find module`: **BROKEN** (setup failure), not RED.
2. Create `Lighthouse.Frontend/.fallowrc.json`. Without it Fallow either errors on the missing `-c` file or reports no violations: **BROKEN** or **RED** depending on the Fallow version, so write the file in the same step as the install.

Neither is an implementation gap in a production module; there is none. The only "implementation" is configuration, so RED means "config has no rule yet".

## Per scenario, once the package is installed but `.fallowrc.json` has zones and no rules

| # | Scenario | Expected failure when unskipped | Class |
|---|---|---|---|
| 1 | components: the twelve listed | Before install: module not found. After install with no rules: Fallow reports 0 leaks, so `stale` holds all 12 entries. | BROKEN until install; then RED (MISSING_FUNCTIONALITY) |
| 2 | new component to page leak refused | Before install: module not found. Without the rule: `newcomers` is `[]`, expected one entry. | BROKEN, then RED |
| 3 | test files not reported | Passes even with no rule (empty result), so it cannot fail for the right reason on its own. Unskip it only after #2 is green, so it proves `--production` and not an absent rule. | BROKEN before install; **GREEN-on-arrival after** (vacuous until #2 passes) |
| 4 | fixed leak still listed refused | With no rule it already reports the stale entry correctly, so it passes once Fallow runs. Its purpose is the comparison helper; keep after #2. | BROKEN before install; GREEN-on-arrival after |
| 5 | allowed directions report nothing | Also passes with no rules. Only meaningful once #2 is green (rule present, still silent for allowed directions). | BROKEN before install; GREEN-on-arrival after |
| 6 | (second row of #5) | Same as #5. | same |
| 7 | whole frontend: the twenty-one listed | Components-only rule: `stale` holds the 9 non-component entries. | BROKEN until install; RED until FE1-2 adds the other four rules |
| 8 to 12 | each remaining rule refuses a leak | Without the rule: `newcomers` is `[]`. Each goes RED to GREEN when its `from` rule is added to `.fallowrc.json`. | BROKEN until install; RED (MISSING_FUNCTIONALITY) until FE1-2 |

## Honest caveats

- #3, #4, #5 and #6 are negative or comparison-only scenarios: they cannot be made RED without production code, because there is none. They guard against regressions (a rule that over-reports, a comparison that forgets stale entries), not against a missing feature. Their order in DELIVER is therefore: the positive scenario for the rule first (#2), then these.
- The `it.skip` bodies have never run against a real Fallow: the file type-checks and passes Biome, and the JSON shape was checked by hand on the trial binary (21 `boundary_violations` with `from_path`/`to_path`, root-relative). DELIVER's first unskip is also the first execution of `findLeaks`; a problem in that helper shows as BROKEN, and is a test bug to fix before any RED is trusted.
- Fallow's exit code on violations is not inspected; stdout is parsed regardless.
