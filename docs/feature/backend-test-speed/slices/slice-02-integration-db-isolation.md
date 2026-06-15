# Slice 02 — Integration (WAF) tests parallel via per-fixture DB isolation

**Goal (one sentence)**: Replace `IntegrationTestBase`'s shared `static` `WebApplicationFactory` + global `EnsureDeleted`/`EnsureCreated` reset with the Slice-01-chosen per-fixture database isolation, remove the base-level `[NonParallelizable]`, and let the ~37 `API/Integration` fixtures run in parallel.

**Owner story**: US-02. **The headline lever** — this cluster is the bulk of the serial wall-clock.

**Estimated effort**: 1–1.5 days.

**Learning hypothesis**:
- Confirms: With per-fixture isolated DB state, the integration suite runs 3× consecutively green under `ParallelScope.Fixtures` and local + CI backend wall-clock drops materially (toward ≤ 7 min CI).
- Disproves: "Integration serial cost is intrinsic." If a hidden shared-singleton collision survives DB isolation (e.g. a process-global cache à la the 2026-05-17 `VssConnection` learning), the green-3× gate fails — isolate that singleton per-host or annotate just that fixture; do NOT re-blanket the base.

## IN scope

1. **`IntegrationTestBase` isolation refactor** (`TestHelpers/IntegrationTestBase.cs`):
   - Remove `[NonParallelizable]` from the base (line 8).
   - Replace `SharedFactoryLazy` (single static WAF + single DB) with the Slice-01 strategy — leading candidate: each fixture owns a `WebApplicationFactory` configured with a **unique in-memory EF database name** (the base already has an `ownsFactory:true` constructor path), dropping the global `EnsureDeleted`/`EnsureCreated` whole-DB reset in favour of per-fixture isolation.
   - Preserve the `[SetUp]`/`[TearDown]` seeding contract (`SeedDatabase`, `DatabaseContext`) so derived fixtures need zero changes.
2. **TDD shape**: RED — a test asserting two `IntegrationTestBase`-derived fixtures hold disjoint DB state concurrently (fails today); GREEN — introduce per-fixture isolation; REFACTOR — tidy seams.
3. **Verify + measure**: 3 consecutive local green runs of the integration suite; record before/after local + CI wall-clock; append to the re-baseline table.

## OUT scope

- The `Services/Implementation` shared-mock cluster (Slice-03).
- The inherently-serial allowlist + guard (Slice-04).
- `ParallelScope.Children`; project-splitting; SQLite-in-file.

## Acceptance criteria

- AC-02.1..02.5 from `feature-delta.md` US-02.

## Dependencies

- Slice-01 strategy recommendation merged.

## Reference class

Test-infra isolation refactor; same problem/fix class as #5020's CS-P `AuthenticationMethodSchema` (shared static keyed by identity → carry the discriminator, isolate per-host). Prior art: `slice-be-parallel-enable.md`.

## Risk and mitigations

- **Per-fixture WAF setup cost erodes the parallelism gain** → Slice-01 measured the break-even; if WAF-per-fixture is too costly, use shared-WAF + per-test unique DB name instead (the strategy decision lives in Slice-01).
- **Hidden process-global singleton collision** → isolate per-host or `[NonParallelizable]` that single fixture (added to the Slice-04 allowlist with justification); bounded, do not re-blanket the base.
- **Mutation kill-rate slip on any production seam** → AC-02.5 checks Stryker ≥ 80 %.

## Taste tests

- Ship 4+ new components? Borderline — one base refactor + DI/DB-naming seam is one coherent change. Pass.
- Depends on a new abstraction? Only if Slice-01 picks a per-fixture-DB helper; single-purpose. Acceptable.
- Disproves something? Yes (intrinsic-serial-cost claim). Pass.
- Synthetic data only? No — real `dotnet test` + real CI. Pass.
- Duplicate-except-scale? No (distinct cluster from Slice-03). Pass.
