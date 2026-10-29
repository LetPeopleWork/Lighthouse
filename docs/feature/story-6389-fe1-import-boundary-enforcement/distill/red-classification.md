# Story 6389 — expected RED classification (DISTILL, 2026-10-29)

All 17 scenarios in `Lighthouse.Frontend/src/layerBoundaries.enforcement.test.ts` are `it.skip` today. Numbers refer to the scenario list in `feature-delta.md`.

## What DELIVER must do first (applies to every scenario that calls Fallow)

1. Install `fallow` (exact pin) as a dev dependency. Until then every scenario that reaches `findLeaks` fails at `nodeRequire.resolve("fallow/bin/fallow")` with `Cannot find module`: **BROKEN** (setup failure), not RED. Only #7 and #8 (comparison-level, no Fallow call) are unaffected.
2. Create `Lighthouse.Frontend/.fallowrc.json`. Without it Fallow exits 2, and `findLeaks` throws quoting its output: **BROKEN**, which is the intended loud failure, not an empty result. Write the file in the same step as the install.

Neither is an implementation gap in a production module; there is none. The only "implementation" is configuration, so RED means "config has no rule yet".

## Per scenario, once the package is installed but `.fallowrc.json` has zones and no rules

| # | Scenario | Expected failure when unskipped | Class |
|---|---|---|---|
| 1 | components: the twelve listed | Before install: module not found. After install with no rules: Fallow reports 0 leaks, so `stale` holds all 12 entries. | BROKEN until install; then RED (MISSING_FUNCTIONALITY) |
| 2 | whole frontend: the twenty-one listed | Components-only rule: `stale` holds the 9 non-component entries. | BROKEN until install; RED until FE1-2 adds the other four rules |
| 3 | new component to page leak refused | Without the rule: `newcomers` is `[]`, expected one entry. | BROKEN, then RED |
| 4, 5 | `.test.ts` / `.test.tsx` importer not reported | The control import is a real forbidden import, so with no rule the result is `[]`, not the control entry: RED. With the rule it fails if `--production` stops excluding test files. | BROKEN before install; RED until the components rule exists |
| 6 | the same page imported three ways is reported once | With no rule the result is `[]`, missing both the control and the file pair: RED. Pins Fallow's one-edge-per-file-pair behaviour. | BROKEN, then RED |
| 7 | same entry found twice, listed once | No Fallow call. Fails today only if the comparison is set-based; with the counted comparison it passes on arrival. | GREEN-on-arrival (guards the comparison) |
| 8 | listed twice, found once | Same as #7. | GREEN-on-arrival |
| 9 | fixed leak still listed refused | The control entry is found and listed, so only the fixed entry is stale. With no rule the control is missing and `stale` holds two entries: RED. With the rule it proves the comparison reports stale entries. | BROKEN, then RED until the rule exists |
| 10, 11 | allowed directions report nothing | The control import must be the only entry; with no rule the result is `[]`: RED. With the rule it proves the rule is silent for allowed directions. | BROKEN, then RED until the rule exists |
| 12 to 16 | each remaining rule refuses a leak | Without the rule: `newcomers` is `[]`. Each goes RED to GREEN when its `from` rule is added to `.fallowrc.json`. | BROKEN until install; RED (MISSING_FUNCTIONALITY) until FE1-2 |
| 17 | a check that cannot run throws | With a missing config Fallow exits 2 and prints JSON to stdout; `findLeaks` must throw quoting it. Fails only if the exit status is ignored. | BROKEN before install; GREEN-on-arrival after (guards `findLeaks`) |

## What proves what

- The stale-entry scenario (#9) proves the comparison: a listed leak that is gone is reported, by name.
- The control import in #4, #5, #9, #10 and #11, together with the exit-status check (#17), prove Fallow ran with the rule applied. A run that applied no rule or failed to start cannot produce the expected one-entry result.
- #7 and #8 prove the comparison counts entries.

## Honest caveats

- #7, #8 and #17 are guards: they cannot be made RED without production code, because there is none. They protect against regressions (a set-based comparison, an ignored exit status), not against a missing feature. Order in DELIVER: the positive scenario for the rule first (#3), then these.
- The `it.skip` bodies have never run against the repo's Fallow: the file type-checks and passes Biome, and the JSON shape, the exit codes (0, 1, 2) and the one-violation-per-file-pair behaviour were checked by hand on the trial binary (3.36.0). DELIVER's first unskip is also the first execution of `findLeaks`; a problem in that helper shows as BROKEN, and is a test bug to fix before any RED is trusted.
