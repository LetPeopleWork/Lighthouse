# Spike — CS-B integration setup/body split measurement

**Goal (one sentence)**: Instrument `JiraWriteBackTest`'s `OneTimeSetUp` and per-test `SetUp` vs body times to find out whether shared-setup refactoring (CS-B) would actually save the 15-35 % BE wall-clock the alternatives memo estimated, before committing 2-3 developer days to it.

**Owner story**: US-02 (catalog candidate CS-B; gate before opening slice-03C).

**Estimated effort**: ½ day. Wrap `[OneTimeSetUp]` / `[SetUp]` / each `[Test]` body in `Stopwatch.StartNew()`; print to stdout; run locally against real Jira; capture one report.

**Learning hypothesis**:
- Confirms: Setup ≥ 50 % of `JiraWriteBackTest`'s class wall-clock — CS-B is high-confidence, opens `slice-03C-fixture-session-sharing` with the empirical setup share as the gain ceiling.
- Disproves: Setup < 25 % — CS-B is rejected; escalate to a narrower CS-A variant ("cadence-split Jira integration only, keep ADO/Linear on per-PR") OR rely on CS-H alone if its 88 %-of-PRs default-skip already gets us to target.

## IN scope

- Add `Stopwatch` instrumentation in `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWriteBackTest.cs`:
  - `[OneTimeSetUp]` start/stop → emit `JIRA_ONE_TIME_SETUP_MS=<n>`.
  - `[SetUp]` start/stop → emit per-test `JIRA_SETUP_MS=<n>`.
  - Wrap each `[Test]` body → emit `JIRA_BODY_MS=<test>:<n>`.
  - Use `TestContext.Out.WriteLine` so output lands in the test run log.
- Run locally with valid `JiraLighthouseIntegrationTestToken` env var: `dotnet test --filter "FullyQualifiedName~JiraWriteBackTest"` once.
- Extract setup share: `setup_total / (setup_total + sum(body))`.
- Same instrumentation in `JiraWorkTrackingConnectorTest` and `JiraScopedTokenIntegrationTest` (smaller / different patterns); compute share separately.
- Produce `docs/feature/test-speed-improvements/spike-cs-b-findings.md` with:
  - Per-class setup share %.
  - Per-test body time distribution (median, p95, max).
  - Decision: CS-B GO / CS-B NO-GO / partial CS-B (e.g. only WriteBack).
  - If GO: rough effort revision based on which classes pay off.

## OUT scope

- Applying the CS-B refactor. That's the next slice (if the gate opens it).
- Instrumenting ADO `AzureDevOpsWriteBackTest` — defer unless CS-B GO and the same pattern looks applicable.
- Frontend instrumentation.
- Modifying any production code or test assertions.
- Long-term keeping the `Stopwatch` instrumentation — revert before merge if the spike's value lands purely in the findings doc.

## Acceptance criteria

- AC-SPIKE-B.1: `docs/feature/test-speed-improvements/spike-cs-b-findings.md` exists with at least:
  - `JiraWriteBackTest` setup share % (with raw numbers).
  - `JiraWorkTrackingConnectorTest` setup share %.
  - `JiraScopedTokenIntegrationTest` setup share %.
  - A GO / NO-GO / PARTIAL recommendation matching the gate criteria above.
- AC-SPIKE-B.2: If recommendation is GO, the report names the classes worth refactoring (set N), the expected aggregate saving in seconds, and the projected effort range (lower bound = pre-memo's 2 d if N=1; upper bound = pre-memo's 3 d if N=3).
- AC-SPIKE-B.3: Instrumentation is reverted before any merge into `main` (this is a local-only measurement spike).

## Dependencies

- slice-pre-integration-labelling does not block this spike, but completing it first means the spike runs against the corrected taxonomy.

## Reference class

One-file instrumentation spike. Similar to a `/nw-root-why` instrumentation pass, but ahead of a decision rather than during a bug investigation.

## Pre-slice SPIKE

This IS the spike. No nested spike.

## Taste tests

- Ship 4+ new components? **No** — one report.
- Depends on a new abstraction? **No** — `Stopwatch` is in BCL.
- Disproves something? **Yes** — the load-bearing assumption that "most of integration runtime is auth + connection setup".
- Synthetic data only? **No** — real Jira instance.
- Identical-except-for-scale duplicate of another slice? **No** — distinct mechanism from spike-fe-profile.

All taste tests pass.
