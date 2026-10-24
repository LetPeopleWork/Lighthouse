# Upstream issues — Epic 6400 DISTILL (2026-10-24)

None blocks DELIVER. None contradicts a maintainer decision. Each names the wave it goes back to. Items marked
**Closed** were settled by the maintainer's decisions of 2026-10-24 or by the revision round that followed.

## For the maintainer (open questions, defaults applied)

1. **Closed: Q-M2, a Feature stored twice under one reference id.** Settled by maintainer decision 13: no 500, the
   first in forecast order answers, and one Warning names the id per refresh. Pinned by
   `Slice03FeaturesListReadsScenarios.A_Feature_stored_twice_under_one_id_answers_once_with_the_first_in_forecast_order_and_the_operator_is_warned_once_per_refresh`
   (two reads with no refresh between them leave exactly one Warning). Today the request answers 500.
2. **Q-M4 — the computed-round target (DESIGN, K4).** Still provisional at "≥ 30% shorter". It is a harness reading
   and no acceptance test pins it.

## For DESIGN

3. **Closed: banner wording when one thread runs forecast rounds.** Settled as singular, "Forecast rounds run on 1
   dedicated thread; …", and "N dedicated threads" otherwise (DDD-7). The test works the count out as
   `max(1, cores ÷ 2)` and expects the singular or the plural form exactly, followed by "; each forecast uses up to N
   of M cores". The banner reads the machine's cores and has no input for them yet, so a run checks the form that
   machine has: the singular only on a host of three cores or fewer. When slice 01 gives the banner the lane as an
   input (DDD-7, DDD-23), DELIVER can turn the test into cases for one and for several threads.
4. **Who may call `getLicenseStatus` (slice 02).**
   - DESIGN says "only the shared query calls `getLicenseStatus`". Today two more modules read the licence for other
     reasons: `SurveyNudge.tsx` and `SystemSettingsTab.tsx`.
   - The enforcement test (`hooks/licenseStatusOneAsker.enforcement.test.ts`) exempts them by name and requires
     exactly one asker among the rest.
   - If DESIGN meant every reader, the exemption list is the one line to change.
5. **The detector hand-off of the band (flag F1, slice 01).** DESIGN leaves the mechanism to DELIVER.
   - Pinned: what reaches the server and what the server refuses (`Story6404FeaturesHeldUsageEventTests`), and the
     pure band function (`featuresHeldOf.test.ts`).
   - Not pinned: how a detail page hands its owner's count to the page-opening detector.
   - DELIVER adds a component test once the mechanism exists. It is listed under Known gaps.
6. **Closed: the lane stopping with the host (DDD-23, slice 01).** The outcome is now an acceptance scenario,
   `When_Lighthouse_stops_in_the_middle_of_a_round_the_round_ends_and_every_Feature_keeps_the_forecast_it_had`: the test
   puts back the queue drain the test host drops, stops the host while a round is held, and reads the Feature from a
   fresh host on the same database. Only the joining of the lane's threads stays a unit test on `ForecastLane`.

## For DEVOPS

7. **Closed: the DEVOPS "Wave Decisions Summary" described three bands.** It now lists the six of maintainer
   decision 2, as the tests and `kpi-contracts.yaml` do. Decision 2 itself now says the band travels as its name and
   0–5 are its internal values.
8. **The E2E wait on `data-widget-status="ready"` is deferred to DELIVER slice 04.**
   - The Features-tab page object cannot wait on an attribute the list does not carry yet, so changing it now would
     turn green E2E red.
   - No new E2E spec. The existing walking skeleton covers the flow.

## For DELIVER (existing tests that change with the behaviour)

9. `StartupBannerForecastCoresLineTest` pins today's "Forecasts use up to {n} of {cores} cores" line.
   - Slice 01 replaces that line, so DELIVER retires the class in the slice that changes the banner.
   - `StartupBannerForecastLaneLineTest` takes over.
10. `PortfolioFeatureList.test.tsx` and `TeamFeatureList.test.tsx` drive today's three-request list
    (`getFeaturesByIds`, `getFeaturesInProgress`, `useParentWorkItems`). Slice 04 rewrites what they arrange.
    - Their row, column and dialog claims stay.
    - Their service setup moves to `getFeatureList`, through `tests/FeatureListTestKit.tsx`.
11. `OverviewDashboard.test.tsx` and `EditTeam.test.tsx` mock `useLicenseRestrictions` without a `status`. They keep
    working, because `status` is optional until slice 02 makes it required, and then those mocks gain
    `status: "ready"`.

## For DESIGN (raised by revision round 1)

12. **Decision 15's "Save on a new Team's edit page" does not match the page.**
    - `/teams/new` opens the create wizard (`CreateTeamWizard`), and its Create button does not read the licence at
      all today. That is the path most new Teams take, and DESIGN's entry-point list (CF-3, DDD-11) leaves it out.
    - `EditTeam.tsx:41-43` gates only the copied-Team path (`/teams/new?cloneFrom=…`). That page autosaves and has no
      Save button: a refused save simply does not happen, with nothing on screen.
    - Pinned so far, at the seam that exists: the copied Team is not saved while the check loads or has failed, and
      the reason text appears on the page when it failed (`pages/Teams/Edit/EditTeam.licenseCheck.test.tsx`). Where
      the reason sits is left to DESIGN.
    - Asked of DESIGN: is the wizard's Create a create entry point under decision 15, and where does the
      could-not-check reason show on the copied-Team page? Each answer adds or moves one scenario.
