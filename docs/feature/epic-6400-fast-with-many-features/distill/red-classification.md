# RED classification — Epic 6400

2026-10-24. Every pending test was un-skipped once (not committed) and run: the backend classes with
`dotnet test --no-build --filter "(FullyQualifiedName~FastWithManyFeatures|FullyQualifiedName~Story6404FeaturesHeld|FullyQualifiedName~StartupBannerForecastLaneLineTest)&TestCategory!=Integration&…"`,
the frontend files with `pnpm vitest run <files>`. Then the skips were restored, the backend rebuilt (0 warnings) and
both suites re-run green with the skips in place.

- Backend: **67 of 67** pending cases failed; the **18** active cases passed.
- Frontend: **53 of 53** pending cases failed; the **10** active cases passed.
- None failed on setup, seeding, routing or types.

Revision round 1 (same day) changed or added cases in seven backend and five frontend files. Every pending case in
those files was un-skipped again and run: backend 42 failed and 11 passed, and each of the 11 is an active guard;
frontend, every pending case failed except one, which passed and is now an active guard (see below). The skips were
then restored, and the full sets re-run: backend 18 passed, 64 skipped; frontend 12 passed, 51 skipped.

Classes:

- **MISSING_FUNCTIONALITY**: the assertion fires because the behaviour is not built.
- **MISSING_ENDPOINT**: the route answers 404 because the action does not exist yet. This is the same kind of RED,
  named apart so DELIVER sees which slice adds a route.
- **SCAFFOLD**: the call reaches a scaffold body that throws.

Paths: backend under `Lighthouse.Backend/Lighthouse.Backend.Tests/`, frontend under `Lighthouse.Frontend/src/`.

## Harness defects found and fixed during the run (not RED reasons)

1. **`ManyServiceProvidersCreatedWarning` after 20 hosts.** A materialisation interceptor is a singleton
   interceptor, so a new instance per test made Entity Framework build a new internal service provider per test.
   Past twenty it throws in SetUp. `TestHelpers/CapturedMaterialisations.cs` now registers one shared interceptor,
   and each host has its own counter, found through the context's application service provider.
2. **"Which round was last" was ambiguous.** Both rounds of the Slice 05 skip scenario run at the same pinned
   instant, so ordering by `ExecutedAt` returned the computed round. The scenario now takes the latest row by id.
   With that fixed, the skip itself is confirmed today, and the scenario fails only on the histograms it reads (5).
3. **Two 404 scenarios passed vacuously.** They answered 404 because the route does not exist. Each now first proves
   the route answers 200 for an owner that exists, so today they fail for MISSING_ENDPOINT.
4. **The G1 golden constant was a guess.** It was replaced with the recorded value `400 runs [5:280]`: runs that
   never finish leave no histogram row.

## Revision round 1: what changed in the RED run

- **Became an active guard after its un-skipped run passed:** the Get Started Add Team step is disabled and says
  nothing while the licence is checked. Today the check's `canCreateTeam` is already false then, and the stepper has no
  tooltip, so the step passes; it guards that no reason is shown while loading.
- **Moved from active to pending:** the size note at exactly 100. It now waits for the size to be asked for and to
  arrive before it reads "no note", and today the page never asks (`getSize` called 0 times).
- **Precondition that depends on the feature:** the failed-update size-warning case first needs the successful update
  to warn. Today that warning does not exist, so the case stops at its precondition ("The successful update before the
  failure warns…, expected 1, was 0"). That is still MISSING_FUNCTIONALITY, and it is why this case only proves its
  claim once the 101 case is green.

## Revision round 2: what changed in the RED run

Un-skipped and run again: the three changed backend files (Slice 03, the usage-data tests, ArchUnit) and the two
frontend cases that became pending. Then the skips were restored and the full sets re-run: backend 18 passed,
67 skipped (85); frontend 10 passed, 53 skipped (63).

- **Moved from active to pending**, each because the precondition it now needs does not exist today:
  - the hook's "free licence with room left" case waits on `status === "ready"`: "expected undefined to be 'ready'";
  - the size note "while the size is on its way" case first needs the size asked for: `getSize` called 0 times.
- **New pending refusal cases `4` and `0`** (a band's internal number, not its name): 204 instead of 400 today,
  because the band is not read at all yet. MISSING_FUNCTIONALITY for slice 01. They exist for the step after: an
  enum bound with the default string converter also accepts integers, so slice 01 has to turn that off for this
  property, or these two go green for the wrong reason.
- **New pending sibling for "per refresh"** (read, refresh, read: 2 Warnings): the first read answers 500 today,
  like its sibling.
- **ArchUnit, per-loop scheduler check:** still fails today on `Task.Run(` and on the `Parallel.For` whose options
  name no scheduler.
- **Active guards that gained a precondition and stayed green:** the store-level orphan check (the round did not
  skip); the staging comparison, which now stages a start date for the in-progress Feature no Team works on (`PAY-2`)
  and requires the computed round to have staged it. A completion date is never staged for that Feature, because it
  has no forecast, so a start mapping was added to make it comparable.

## Slice 01 — Story 6404

| Test | Cases | Class | Failure seen |
|---|---|---|---|
| `Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios` (2, 4, 8 cores) · simulates on Lighthouse's own threads | 3 | MISSING_FUNCTIONALITY | "The round simulated on the threads that answer pages: #7 shared pool, background, '.NET TP Worker' …" |
| · saves its forecasts from the forecast threads too | 3 | MISSING_FUNCTIONALITY | "…saved its forecasts from the threads that answer pages: #54 shared pool…" |
| · a round stopped from the Task Manager ends and keeps the previous forecast | 3 | MISSING_FUNCTIONALITY | The new "it had simulated" precondition passed and the previous forecast was kept; the thread assertion fired ("had been simulating on the threads that answer pages") |
| · when Lighthouse stops in the middle of a round, the round ends and the previous forecast stays | 3 | MISSING_FUNCTIONALITY | The stop finished in time, but the queue drain let the held round finish and save: "The round under way when Lighthouse stopped saved a new forecast… Expected `400 runs [5:400]`" and a 7-in-10 histogram was found |
| `Story6404FeaturesHeldUsageEventTests` · a tab opening travels with its band | 7 | MISSING_FUNCTIONALITY | `features_held` absent at the collector (expected "UpTo20" … "Unknown", was null) |
| · anything but one of the six bands is refused (920, 6, -1, "920", "Lots", 4, 0) | 7 | MISSING_FUNCTIONALITY | 204 instead of 400, and the event was forwarded |
| · any other event carrying a band is refused | 2 | MISSING_FUNCTIONALITY | 204 instead of 400, and the event was forwarded |
| · the six bands travel by name and keep their internal numbers | 1 | MISSING_FUNCTIONALITY | "There is no closed list of bands…" (no `UsageDataFeaturesHeld` enum) |
| · the usage data page names every band | 1 | MISSING_FUNCTIONALITY | "The usage data page does not name the band UpTo20." |
| `StartupBannerForecastLaneLineTest` (2) | 2 | MISSING_FUNCTIONALITY | The banner still prints today's "Forecasts use up to 6 of 12 cores" line; no "Forecast rounds run on 6 dedicated threads; …" line |
| `FastWithManyFeaturesArchUnitTest` · `ForecastService` hands nothing to the shared pool | 1 | MISSING_FUNCTIONALITY | "ForecastService hands work to the threads that answer pages…": `Task.Run(` found, and a `Parallel.For` whose options name no scheduler |
| · round code never resumes off the lane; only the updater and the composition root reach the lane | 2 | MISSING_FUNCTIONALITY | "There are no forecast threads yet: no ForecastLane.cs in the production project." |
| `models/UsageData/featuresHeldOf.test.ts` | 14 | SCAFFOLD | "Not yet implemented: featuresHeldOf" |

## Slice 02 — Story 6406

| Test | Cases | Class | Failure seen |
|---|---|---|---|
| `Slice02OverviewReadsNamesAndCountsScenarios` · naming and counting reads no forecast at all | 5 | MISSING_FUNCTIONALITY | "Opening the Portfolios list read forecasts, which it never shows. Read: … IndividualSimulationResult 78 …" (same for the Teams list, one Team, one Portfolio, its settings) |
| · opening Northwind Payments reads only it and its Features | 1 | MISSING_FUNCTIONALITY | "Opening one Portfolio read other Portfolios too. Read: … Feature 26 …" |
| `hooks/useLicenseRestrictions.status.test.tsx` (loading, failed licence, failed counts, ready, free with room once ready) | 5 | MISSING_FUNCTIONALITY | `status` is undefined (expected loading / failed / ready) |
| · three askers share one read | 1 | MISSING_FUNCTIONALITY | `status` never "ready"; three licence reads today |
| · Premium never fetches the lists | 1 | MISSING_FUNCTIONALITY | `status` never "ready" (the lists are fetched today) |
| `hooks/licenseStatusOneAsker.enforcement.test.ts` | 1 | MISSING_FUNCTIONALITY | Two askers: `useLicenseRestrictions.ts` and `LicenseStatusIcon.tsx` |
| `pages/Overview/OverviewDashboard.licenseCheck.test.tsx` · loading says nothing | 1 | MISSING_FUNCTIONALITY | Hovering showed "This feature requires a premium license…" (expected no tooltip) |
| · failed says why | 1 | MISSING_FUNCTIONALITY | Hovering showed the premium tooltip instead of "Couldn't check your license. Reload the page to try again." |
| · Get Started: failed, the Add Team step and the Add Portfolio step say why | 2 | MISSING_FUNCTIONALITY | No tooltip opens on the stepper's action (no element with role `tooltip`) |
| `pages/Teams/Edit/EditTeam.licenseCheck.test.tsx` · a copied Team on a failed check says why | 1 | MISSING_FUNCTIONALITY | The could-not-check text is not on the page (the save is already refused today) |

## Slice 03 — Story 6407

| Test | Cases | Class | Failure seen |
|---|---|---|---|
| `Slice03FeaturesListReadsScenarios` · reads the histograms of the Features it shows only | 1 | MISSING_FUNCTIONALITY | "…read the forecast histograms of the whole instance. Read: … Feature 26 …" |
| · asking what Atlas works on reads one Feature | 1 | MISSING_FUNCTIONALITY | "…read other Features too. Read: … Feature 26 … IndividualSimulationResult 180 …" |
| · a Feature stored twice answers once, with one warning per refresh; and warns once more after a refresh | 2 | MISSING_FUNCTIONALITY | The first read answered 500 ("/api/latest/teams/1/metrics/featuresInProgress… answered 500"): `GetCurrentFeaturesInProgressForTeam` looks each reference up with a single-result predicate, which throws on the duplicate (the behaviour decision 13 replaces) |

## Slice 04 — Story 6408

| Test | Cases | Class | Failure seen |
|---|---|---|---|
| `Slice04OneListPerOwnerScenarios` (Portfolio list, Team list, row parity, parents and active-work parity, histogram budget, empty, withheld inside) | 7 | MISSING_ENDPOINT | `/api/latest/{portfolios\|teams}/{id}/feature-list answered 404` |
| · 403 for a Portfolio, 403 for a Team | 2 | MISSING_ENDPOINT | Expected Forbidden, was NotFound |
| · a Portfolio that does not exist is not found | 1 | MISSING_ENDPOINT | "Without a list for a Portfolio that exists, a missing one proves nothing." (the existing one answered 404) |
| `FastWithManyFeaturesArchUnitTest` · the list endpoint reads through its service | 1 | MISSING_FUNCTIONALITY | "There is no Lighthouse.Backend.API.FeatureListController yet." |
| `pages/Portfolios/Detail/PortfolioFeatureList.oneList.test.tsx` | 13 | MISSING_FUNCTIONALITY | `getFeatureList` is never called. No row appears, `data-widget-status` is absent (null), and neither the copy nor the Retry button exists. The first-load case stops at "expected null to be 'loading'", before its spinner and no-rows checks |
| `pages/Teams/Detail/TeamFeatureList.oneList.test.tsx` | 6 | MISSING_FUNCTIONALITY | Same: the list still reads `features/ids`, which the kit answers with nothing. The new failed-refresh and late-answer cases stop where they wait for the first rows or the empty message |

## Slice 05 — Story 6409

| Test | Cases | Class | Failure seen |
|---|---|---|---|
| `Slice05LighterRoundsScenarios` · a skipped round on a Portfolio that writes nothing back reads no histogram | 1 | MISSING_FUNCTIONALITY | The no-mapping precondition held and the skip happened; "The skipped round read forecast histograms it never uses … IndividualSimulationResult 5" |
| · a computed round does not read yesterday's histograms | 1 | MISSING_FUNCTIONALITY | Both new preconditions held (not skipped, stored forecast changed); "…read yesterday's histograms only to replace them … IndividualSimulationResult 5" |

## Slice 06 — Story 6410

| Test | Cases | Class | Failure seen |
|---|---|---|---|
| `Slice06PortfolioSizeScenarios` (101 over, 100 not, never updated, counting reads none) | 4 | MISSING_ENDPOINT | `/api/latest/portfolios/1/size answered 404` |
| · 403; not found | 2 | MISSING_ENDPOINT | Expected Forbidden, was NotFound; "Without a size for a Portfolio that exists, a missing one proves nothing." |
| `Slice06SizeWarningInTheLogScenarios` · 101 Features leave one warning | 1 | MISSING_FUNCTIONALITY | The exact line was absent at Warning (expected 1, was 0); all 101 stored |
| · the size warning is listed once in Recent Problems | 1 | MISSING_FUNCTIONALITY | Recent Problems held other Warnings from the same update ("No teams available for extrapolation…"), which shows the sink is wired, but no size warning |
| · a failed update leaves no size warning | 1 | MISSING_FUNCTIONALITY | Stops at its precondition: the successful update before the failure leaves no warning yet (see above) |
| `components/Common/ProjectSettings/ModifyProjectSettings.sizeNote.test.tsx` (note as an info alert, asks the size, failure, two renamed-term cases, 100 shows none once the size arrived, nothing while it is on its way once asked) | 7 | MISSING_FUNCTIONALITY | The note or the failure copy is not on the page; for 100 and for the waiting case, `getSize` was never called |

## Active guards (green today, kept green by DELIVER)

Backend (18):

- the walking skeleton;
- the G1 forecast golden;
- 5 slice 02 goldens and 3 slice 03 goldens;
- the withheld blocker (G2);
- slice 05 (G1): no orphan histogram row at the store; the tracker date after computed and skipped rounds; a skipped
  round with a write-back mapping still reads the histograms it stages from; computed and skipped rounds stage the
  same values, a Feature no Team works on included;
- 100 Features leave no size warning;
- 2 older-bundle tab openings without a band.

Frontend (10):

- 2 hook cases as today (at the limit, Premium);
- 2 Overview create actions once the licence is checked;
- 2 Get Started cases: the Add Team step says nothing while checking; enabled with room;
- 3 copied-Team cases: not saved and nothing said while checking; saves with room; an existing Team saves on a
  failed check;
- 1 size-note case: a Portfolio being created is not asked.
