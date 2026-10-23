# Slice 30 — S12a Metrics controllers → read-side query ports per capability

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Move the non-HTTP logic out of `TeamMetricsController` (661 lines, 11 dependencies) and `PortfolioMetricsController` (675) into read-side query ports per capability (not one per controller), so it is unit-testable without a host.

## IN

- View selection (`Get*ForView`), blackout-day annotation (`GetBlackoutDayIndicesArray`, `AnnotateBlackoutDays`), `ReconcileReconstructedCountWithSnapshot`.
- Follow the existing ports `IPercentilesOverTimeSeriesQuery`, `IProcessBehaviorSeriesQuery`, `IRefinementViewQuery`.
- **Team first**, then Portfolio moves onto the shared ports and its private copies are deleted.
- Decide explicitly what lives in query ports vs `I*MetricsService`, so there are never two read layers. Describe the convention in ARCHITECTURE.md §6.
- Move per-behaviour assertions down to unit tests. Each endpoint keeps **one** HTTP test (routing, authorisation, response shape). Each `WebApplicationFactory` case costs about 1 s of set-up, so this also trims CI time.

## OUT

- Other controllers (S12b). DTO derivation (S12c).

## Hypothesis

The controller logic is a few capabilities shared by Team and Portfolio. **Disproved if** most logic is controller-specific. Then the ports are per controller, and the Story records why.

## Acceptance criteria

- [ ] Both controllers free of view selection, blackout annotation and reconciliation. S107 suppressions removed.
- [ ] Portfolio's private copies deleted. One read layer, per the §6 convention.
- [ ] Routes, DTO shapes and serialized responses unchanged (response snapshots, E2E). Same `RbacGuard`s.
- [ ] Behaviour cases are unit tests. One HTTP test per endpoint. Mutation score held.

## Dependencies

- Soft: S07b, S02.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one capability per slice, Team before Portfolio).
