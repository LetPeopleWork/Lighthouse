# Slice 01 — Triage the 54 opt-outs + spike the isolation strategy

**Goal (one sentence)**: Classify every `[NonParallelizable]` fixture by root cause and produce a measured recommendation for the integration-test isolation strategy, so Slice-02 refactors against evidence rather than a guess.

**Owner story**: US-01.

**Estimated effort**: ½ day.

**Learning hypothesis**:
- Confirms: The 54 tags fall into a small set of root-cause clusters; one shared design flaw (`IntegrationTestBase` static-WAF + global DB reset) explains the ~37-fixture majority; a per-test isolated-DB strategy beats WAF-per-fixture on setup cost.
- Disproves: "We already know which fixtures are genuinely serial" / "WAF-per-fixture isolation is obviously the right shape." If per-fixture WAF setup cost is small relative to test bodies, the cheaper strategy assumption flips — capture the real number.

## IN scope

1. **Triage table** — `docs/feature/backend-test-speed/triage.md`: one row per currently-tagged file with `path, cluster (WAF-integration | service-mock | inherently-serial), root_cause, verdict (keep|fix)`.
2. **Re-baseline** — pull the current backend CI test-step wall-clock and the serial-cluster share from a recent `Build And Deploy Lighthouse` run (the #5020 `test-timings-backend` artifact still publishes).
3. **WAF setup spike** — use the existing `FixtureSetupTimer` (instruments `OneTimeSetUp`/`SetUp`/`TearDown`) to measure per-fixture `WebApplicationFactory` construction cost; compute the break-even fixture count for WAF-per-fixture vs shared-WAF + per-test isolated DB. Reuse #5020's `spike-be-fixture-setup-findings.md` as the prior baseline.
4. **Strategy recommendation** — pick ONE isolation strategy with the measured rationale; record in `triage.md`.

## OUT scope

- Any production or test code change beyond throwaway spike scratch (this slice measures; Slice-02 changes).
- FE; real-API `[Category("Integration")]` tests; project-splitting.

## Acceptance criteria

- AC-01.1..01.5 from `feature-delta.md` US-01.

## Dependencies

- None (read-only + spike). #5020 timing artifacts + `spike-be-fixture-setup-findings.md` available.

## Reference class

Measurement/triage slice — same shape as #5020 Slice-02 (alternatives memo) and the `spike-be-parallelism` probe.

## Taste tests

- Ship 4+ new components? No (one doc + a throwaway spike). Pass.
- Depends on a new abstraction? No. Pass.
- Disproves something? Yes (the isolation-strategy guess). Pass.
- Synthetic data only? No — real CI timing + real WAF construction. Pass.
- Duplicate-except-scale of another slice? No. Pass.
