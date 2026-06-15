# Slice 03 — Service / mock-state fixtures parallelized

**Goal (one sentence)**: Fix the ~10 `Services/Implementation` fixtures that are `[NonParallelizable]` only because they share mutable Moq / in-memory-DB / domain-event-dispatcher state across methods, by making that state per-test, then remove their tags.

**Owner story**: US-03.

**Estimated effort**: ~1 day (per-fixture; some are trivial `[SetUp]` moves, the Gold tests may need dispatcher isolation).

**Learning hypothesis**:
- Confirms: The remaining service-cluster tags are avoidable — moving shared mocks/DB into per-test setup (or scoping a unique in-memory DB) lets them run parallel, green 3× consecutively.
- Disproves: "The rest of the tags are all inherent." Any fixture that still flakes after per-test isolation is genuinely serial → move it to the Slice-04 allowlist with a justification, don't force it.

## IN scope

Per Slice-01's `fix`-verdict list in the `service-mock` cluster, expected to include:
- `BackgroundServices/Update/TeamUpdaterTest`, `PortfolioUpdaterTest` — shared `Mock<...>` across methods → construct mocks in `[SetUp]` (per-test) or scope per test.
- `TeamMetricsServiceTests`, `PortfolioMetricsServiceTests` — shared in-memory DB context → unique/scoped DB per test.
- `Seeding/TerminologySeederTests` — shared in-memory DB → scope per test.
- `DomainEvents/*GoldTest` (`TeamDataRefreshedGoldTest`, `DomainEventDispatcherGoldTest`, `WorkItemDomainEventsGoldTest`), `DeliveryMetricSnapshotRecordingHandlerTest` — shared dispatcher/handler state → per-test dispatcher instance or scoped registration.
- `LighthouseReleaseServiceIntegrationTest` — confirm cluster + apply the matching fix.

For each: remove `[NonParallelizable]` only after per-test isolation is real and the fixture is green 3× consecutively.

## OUT scope

- Integration WAF cluster (Slice-02).
- Inherently-serial residue: `API/Security/**` (rate-limiting, CORS-env, API-key scopes, group-snapshot) and `LighthouseAppContextConcurrencyTest` — these stay tagged and go on the Slice-04 allowlist.
- Any production-behaviour change (D8 — test-isolation only).

## Acceptance criteria

- AC-03.1..03.3 from `feature-delta.md` US-03.

## Dependencies

- Slice-02 merged (clean parallel baseline so residual flakes are attributable to this cluster, not the integration base).

## Reference class

Per-fixture test-isolation hygiene; the `[SetUp]`-fresh-mocks and scoped-DB patterns are standard and already used elsewhere in the suite.

## Taste tests

- Ship 4+ new components? No — mechanical per-fixture isolation. Pass.
- Depends on a new abstraction? No. Pass.
- Disproves something? Yes (the "all remaining tags are inherent" claim). Pass.
- Synthetic data only? No — real `dotnet test`. Pass.
- Duplicate-except-scale of Slice-02? No — different root cause (shared mocks/dispatcher vs shared WAF/DB). Pass.
