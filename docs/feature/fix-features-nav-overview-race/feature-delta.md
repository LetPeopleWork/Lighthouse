# fix-features-nav-overview-race — Bug #6112

Bugfix flow: RCA (`rca.md`) → maintainer approval → DELIVER (`deliver/roadmap.json`, `deliver/execution-log.json`).
No DISCUSS / DESIGN / DEVOPS / DISTILL waves; the defect is in E2E test code, not in the product.

## Wave: DELIVER / [REF] Implementation summary

`ManualSortingSwitch.spec.ts` flaked on both SQLite and Postgres. Opening the Features page returned as soon as
the nav link was clicked. The Features view loads on demand and the Overview stays painted meanwhile, and the
Features row locator matched any data grid, so the "before" read picked up the Overview's grids. The page
object now waits for the Features view's own help text and only reads the grid that has a position column. Both
manual-sorting specs reset the Feature ordering switch to its shipped default (off) before and after each test,
so the Switch spec really clicks it. A Playwright reporter writes the names of tests that only passed on retry
to the GitHub Actions job summary, so a flake hidden by `retries: 2` becomes visible.

## Wave: DELIVER / [REF] Files modified

- Tests — `Lighthouse.EndToEndTests/tests/models/app/LighthousePage.ts`: `goToFeatures` waits for the Features help text.
- Tests — `Lighthouse.EndToEndTests/tests/models/features/FeaturesPage.ts`: `featureRows` and the position heading are scoped to the position-column grid.
- Tests — `Lighthouse.EndToEndTests/tests/helpers/api/optionalFeatures.ts`: exported `switchFeatureOrdering`.
- Tests — `Lighthouse.EndToEndTests/tests/specs/features/ManualSortingSwitch.spec.ts`, `ManualSortingMove.spec.ts`: switch off in `beforeEach` / `afterEach`.
- CI — `Lighthouse.EndToEndTests/tests/reporters/FlakySummaryReporter.ts` (new) and `playwright.config.ts`: registered next to `html`.
- Production — none.

## Wave: DELIVER / [REF] Regression evidence

- 01-01: the Features chunk held back 2 s → the "before" read was `["Project Apollo","Team Zenith","Demo Data CSV Connector"]`; the features request held back 2 s → `[]`. That matches the CI signatures. Both pass after the fix, and ManualSortingSwitch passed with `--repeat-each=10`. The delay was a one-off and is not committed.
- 01-02: running Move then Switch with an "off on entry" check failed with `Expected: false, Received: true`. After the fix the pair passes in both orders and with `--repeat-each=5`, and the switch is off afterwards.
- 01-03: a flaky-by-construction throwaway run wrote no summary before the reporter existed. After it, the summary names the test and its attempt count. An all-pass run gives one line, and with no `GITHUB_STEP_SUMMARY` set nothing is written. The real config still produces the html report.

## Wave: DELIVER / [REF] Quality gates

- Refactor pass: N/A, because the diffs are small page-object, spec and reporter edits with nothing left to restructure.
- Adversarial review (nw-software-crafter-reviewer): NEEDS_REVISION. The blocker said `titlePath().slice(3)` drops titles of top-level tests; a probe disproved it (`toplevel.spec.ts › @premium top level flaky title - passed on attempt 2`). Its HIGH and MEDIUM points were already met: RED evidence is in the execution log, and `workers: 1`. No revision was made.
- Mutation testing: N/A, because no production code changed. Stryker's per-feature gate mutates production sources, and the only TypeScript here is E2E test and reporter code outside both Stryker configs.
- DES integrity: `des-verify-integrity` reports that all 3 steps have complete DES traces.
- E2E package: `tsc --noEmit` and `biome check ./tests` are clean.
