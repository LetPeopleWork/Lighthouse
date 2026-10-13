# RED classification — story-6251-team-feature-size

Run 2026-10-13 against unmodified production code plus the two DISTILL scaffolds (`Feature.SizeFor`, which answers
-1, and `MetricsCacheKey`, which answers the names built from the id alone). Every new `[Ignore(Pending)]` and
`it.skip` was stripped, the suites were run, and the markers were put back. Build: `dotnet build Lighthouse.sln`,
0 warnings, 0 errors.

**Every pending test fails on an assertion. None fails on an error.** Backend: 91 of 91 are NUnit Failures (an
`Assert.That` that did not hold); 0 Errors. Frontend: 38 of 38 fail on `AssertionError` or
`TestingLibraryElementError` (an expected element is not there); 0 `TypeError`, import or setup failures.

## Slice 00 reproduction

The collision DESIGN suspected is real and fails on today's code for exactly the predicted reason: the second owner
asked is served the first owner's remembered answer. Team 1 (Gravity) finished 3 Work Items with cycle times 2, 3 and
4 days; Portfolio 1 (Ocean Explorer) closed 5 Features after 21 days each. In every case the owner asked first reads
its own value and the owner asked second reads the other's.

Filter `FullyQualifiedName~Slice00OwnerScopedMetricsTest`: 6 Failures, 2 passed (0.7, the guard).

| Scenario | Classification | Failure observed |
|----------|----------------|------------------|
| `…each_see_their_own_numbers_whichever_is_asked_first(TotalThroughput, Team)` | MISSING_FUNCTIONALITY | the Portfolio's Total Throughput: expected 5, but was **3** (Gravity's) |
| `…(TotalThroughput, Portfolio)` | MISSING_FUNCTIONALITY | the Team's Total Throughput: expected 3, but was **5** (Ocean Explorer's) |
| `…(CycleTimePercentiles, Team)` | MISSING_FUNCTIONALITY | the Portfolio's 50th: expected 21, but was **2** (Gravity's) |
| `…(CycleTimePercentiles, Portfolio)` | MISSING_FUNCTIONALITY | the Team's 50th: expected 2, but was **21** (Ocean Explorer's) |
| `Refreshing_one_owner_leaves_the_numbers_remembered_for_the_other_alone(Team)` | MISSING_FUNCTIONALITY | the Team refreshed as a sync ends it (`UpdateTeamMetrics`) also cleared Portfolio 1: expected its remembered 5, but was **6** |
| `…(Portfolio)` | MISSING_FUNCTIONALITY | the Portfolio refreshed as a sync announces it (`PortfolioFeaturesRefreshed`) also cleared Team 1: expected its remembered 3, but was **4** |
| `A_Team_whose_number_no_Portfolio_shares_reads_its_own_numbers` (2) — guard | (green) | Zenith, Team 2, reads 3 and 2 beside Portfolio 1 |

Control inside the same runs: the owner asked first, the owner that was refreshed, and a Team whose number no
Portfolio has always read the right number, so the seeding, the windows and the reads are sound and only the shared
name is wrong.

## Backend, slices 00 to 02

Filter `FullyQualifiedName~TeamFeatureSize|FullyQualifiedName~FeatureSizeForTeamTest|FullyQualifiedName~MetricsCacheKeyTest`:
91 Failures, 27 passed (the guards, which are not ignored), 0 Errors.

| Test (cases) | Classification | Failure observed |
|--------------|----------------|------------------|
| `MetricsCacheKeyTest.A_Team_and_a_Portfolio_with_the_same_id_never_share_a_remembered_metric` (21), calling `For<Team>` and `For<Portfolio>` | MISSING_FUNCTIONALITY | e.g. `expected: not equal to "1_ArrivalsInfo_…"`: Team 1 and Portfolio 1 get the same name |
| `MetricsCacheKeyTest.Clearing_one_owner_reaches_none_of_the_other_owner_s_metrics` (21) | MISSING_FUNCTIONALITY | Portfolio 1's name starts with Team 1's prefix `1_` |
| `FeatureSizeForTeamTest`, 5 methods (8 cases) | MISSING_FUNCTIONALITY | `Expected: 8 / 4 / 4 / 3 / 0 / 4 / 0, But was: -1`; the adding-up test `Expected: 19, But was: -4` |
| `Slice01TeamFeatureSizeTest`, 14 methods on the new routes (23 cases) | MISSING_FUNCTIONALITY | each Team read of `allFeaturesForSizeChart` / `sizePercentiles`: `Expected: OK, But was: NotFound`, as the route does not exist yet. The refused-window cases: `Expected: BadRequest, But was: NotFound`. The RBAC cases fail on the reader of the Team (`Expected: OK, But was: NotFound`). The unknown-Team cases pass their 404 half and fail on Gravity's (`Expected: OK, But was: NotFound`). The query-budget cases fail on the first answer (`Expected: OK`), before any count is compared |
| `Slice01TeamFeatureSizeTest.The_Features_a_Team_is_working_on_are_as_big_as_its_own_part_of_them` (3) | MISSING_FUNCTIONALITY | the route exists: `OE-002 should read 4` (Gravity) and `should read 3` (Zenith), `OE-007 should read 4`, but the body carries `"size":19` and `"size":10`, the whole Feature |
| `Slice02TeamFeatureSizePercentilesTest`, 8 methods (9 cases) | MISSING_FUNCTIONALITY | `featureSizePercentilesInfo` on a Team: `Expected: OK, But was: NotFound`; the refused window `Expected: BadRequest, But was: NotFound`; the RBAC case fails on the reader of the Team; the unknown-Team case on Gravity's half |
| Guards (27): `MetricsCacheKeyTest.Clearing_an_owner_reaches_every_one_of_its_own_metrics` (21), `…no_other_owner_of_the_same_kind`, `A_Team_whose_number_no_Portfolio_shares_reads_its_own_numbers` (2), `A_reader_who_cannot_read_the_Team_gets_no_Feature_sizes_from_the_Features_it_is_working_on_either`, `The_Portfolio_still_sizes_a_Feature_by_every_Team_s_Work_Items` (19), `The_Portfolio_s_percentiles_still_count_every_Team_s_Work_Items` (25/27/28/29) | (green) | pass today and must pass after |

Not part of the RED run because they are not pending: `ClosedInRangeCharacterisationTest` (8 cases) is green on
today's code by design. It pins what "closed in the range" means, so the refactor that puts the two copies of that
rule together cannot move an item.

The "someone who cannot read the Team" and "a Team that does not exist" halves pass on today's code only because the
route is missing (404 either way). The half that asks for an existing Team the reader may read is what fails, and it
goes green only when the route answers.

The query-budget mechanism was tried on two existing Team routes (`throughputInfo`, `cycleTimePercentiles`) with a
throwaway test, deleted afterwards: refreshed, asked once, counted; equal non-zero counts for 2 and 20 Features in
three runs out of three.

## Frontend

`pnpm exec cross-env TZ=Europe/Zurich vitest run` on the four files with pending tests: 38 failed, 249 passed. The
two new chip guards on a Team (1.38) are among the passes: they hold today and must keep holding.

| File › test | Classification | Failure observed |
|-------------|----------------|------------------|
| `FeatureSizeScatterPlotChart.teamShare.test.tsx` › Team size axis (both modes), localised copy | MISSING_FUNCTIONALITY | axis read `Size (Child Items)` |
| same › bubble of two / of one, accessible name | MISSING_FUNCTIONALITY | bubble titles read `… with size 5 child items`; no button named `View 2 Features with 5 of this Team's Work Items` |
| same › empty state on Team and Portfolio (×2 each) | MISSING_FUNCTIONALITY | no `Features Size` heading and no `No Features to show.` (today: `No data available`) |
| `BaseMetricsView.loading.test.tsx` › a Portfolio's Portfolio & Features opens with every frame / adds and removes none / opens with Work Item distribution and Feature Size framed | MISSING_FUNCTIONALITY | `featureSize` absent at first sight, then appears once loaded |
| same › Feature Size framed from first render / ready and handed an empty list (Team and Portfolio) | MISSING_FUNCTIONALITY | `featureSize` frame absent |
| same › Team could-not-load, plots the Team's own answer, handed no estimate, Team info text | MISSING_FUNCTIONALITY | no `widget-shell-featureSize` on a Team |
| same › Team Feature Size Percentiles offered, could not be loaded, no data (answered in the test with no percentiles), Act / Observe at 8 / Observe / Sustain at 5 / Sustain, Sustain with only a finished Feature of 9 and with no Feature at all, Team info text | MISSING_FUNCTIONALITY | no `widget-shell-featureSizePercentiles` on a Team |
| `categoryMetadata.test.ts` › Team Portfolio & Features order, Team Flow Overview order, Team fetch keys | MISSING_FUNCTIONALITY | `featureSize`, `featureSizePercentiles`, `featureSizeData`, `featureSizePercentilesInfo` absent for a Team |
| `useMetricsData.test.ts` › a Team service asked for its sizes, never for the PBC or estimate it carries as trap spies | MISSING_FUNCTIONALITY | the Team service has no `getAllFeaturesForSizeChart` (asserted on its keys before anything is called) |
| same › a service missing the PBC or the estimate is still asked for its sizes (×2) | MISSING_FUNCTIONALITY | `getFeatureSizePercentilesInfo` never called |

Four dashboard tests were also run once on a Portfolio in a throwaway copy, deleted afterwards, where both widgets
already exist: "plots the Team's own answer", "Feature Size Percentiles could not be loaded", "no data, not zeros"
with its answers given in the test, and both "reads Sustain" cases. All pass, so their RED on a Team is the missing
gate and not the test.

No IMPORT_ERROR / FIXTURE_BROKEN / SETUP_FAILURE / WRONG_ASSERTION.
