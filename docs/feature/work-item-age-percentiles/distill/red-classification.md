# RED Classification — work-item-age-percentiles (Story #5257)

DISTILL pre-DELIVER fail-for-the-right-reason gate. Each scaffold below was verified RED by temporarily removing the class-level `[Ignore]`, building (0 warnings, `TreatWarningsAsErrors`), and running the fixture: every failure is a clean NUnit assertion (`MISSING_FUNCTIONALITY`) with **zero raw exceptions** (verified `grep -c JsonReaderException` = 0 across both fixtures). The missing endpoint resolves to the SPA fallback (`app.UseSpa` returns `index.html`, HTTP 200 + HTML), so each body-parsing test guards with `AssertIsPercentileJsonArray` to turn the non-JSON body into a clean assertion rather than a parse exception. The `[Ignore]` is restored; DELIVER un-skips one at a time.

Stack note: C#/.NET 8 (net10.0 TFM) + NUnit + WebApplicationFactory — NOT the Python/Hypothesis pilot. No `__SCAFFOLD__` stubs, no `assert_state_delta` Universe; RED = compiles clean + real assertion fails because the endpoint is unwired (per `docs/architecture/atdd-infrastructure-policy.md`).

## Team — `WorkItemAgePercentilesReadApiIntegrationTest.cs` (US-01)

| Test | Classification | RED reason (observed when un-skipped) |
|------|----------------|---------------------------------------|
| `…TeamWithInProgressItemsOfKnownAges_ReturnsExactPercentilesOfThoseAges` | MISSING_FUNCTIONALITY | `AssertIsPercentileJsonArray` fails: Expected True But was False (SPA HTML, endpoint unwired) |
| `…PopulationIsTheWipSet_ClosedItemsExcluded` | MISSING_FUNCTIONALITY | same JSON-array guard fails |
| `…ResponseShapeIsByteCompatibleWithCycleTimePercentiles` | MISSING_FUNCTIONALITY | guard fails on the WIA body before shape comparison |
| `…SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles` | MISSING_FUNCTIONALITY | guard fails (both bodies SPA HTML) |
| `…TeamWithNoInProgressItems_ReturnsGracefulZeroValuedSet` | MISSING_FUNCTIONALITY | guard fails before zero-set assertion |
| `…TeamWithSingleInProgressItem_ComputesOverThatOneValue` | MISSING_FUNCTIONALITY | guard fails before single-value assertion |
| `…NonPremiumCaller_StillReceivesPercentiles` | MISSING_FUNCTIONALITY | guard fails: a real percentile array is not yet served |
| `…AnonymousCaller_IsRejected` | MISSING_FUNCTIONALITY | Expected OK in {Unauthorized,Forbidden,NotFound} But was OK — the SPA fallback serves 200 to anyone; the real guarded route does not exist yet |
| `…StartDateAfterEndDate_ReturnsBadRequest` | MISSING_FUNCTIONALITY | Expected BadRequest But was OK — the 400 guard lives on the not-yet-created action |

## Portfolio — `WorkItemAgePercentilesPortfolioReadApiIntegrationTest.cs` (US-03)

| Test | Classification | RED reason |
|------|----------------|-----------|
| `…PortfolioWithInProgressFeaturesOfKnownAges_ReturnsExactPercentilesOfThoseAges` | MISSING_FUNCTIONALITY | JSON-array guard fails (SPA HTML) |
| `…SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles` | MISSING_FUNCTIONALITY | guard fails (both bodies SPA HTML) |
| `…PortfolioWithNoInProgressFeatures_ReturnsGracefulZeroValuedSet` | MISSING_FUNCTIONALITY | guard fails before zero-set assertion |
| `…AnonymousCaller_IsRejected` | MISSING_FUNCTIONALITY | Expected denied-status But was OK (SPA fallback) |

## Gate verdict

13/13 = MISSING_FUNCTIONALITY (correct RED). Zero IMPORT_ERROR / FIXTURE_BROKEN / SETUP_FAILURE / WRONG_ASSERTION. Handoff to DELIVER unblocked for the backend acceptance layer. The full `dotnet test` build-then-run was executed locally here; no deferral to the orchestrator is needed for the backend slice.
