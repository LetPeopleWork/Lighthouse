# Feature: flaky-jira-writeback-scratch-items

ADO: User Story #5542 — "Port per-run scratch-item fix (commit 2964383c) to JiraWriteBackTest"
Waves completed: DISCUSS, DESIGN, DISTILL, DELIVER | Density: lean | Classification: **infrastructure-only** (Decision 4 = No)

---

## Wave: DELIVER / [REF] Implementation summary

Ported the ADO per-run scratch-item pattern to `JiraWriteBackTest`. `[OneTimeSetUp]` creates a fresh Epic + Story in `LGHTHSDMO` via `POST /rest/api/2/issue` (test-local `HttpClient`, Basic auth from env vars); `[OneTimeTearDown]` hard-deletes them best-effort. Fixed constants `EpicId`/`StoryId` are now instance fields set per run — the fixture no longer mutates the shared demo items `LGHTHSDMO-1`/`-16`. Delivered **directly** (single test file, no production code) rather than via the DES roadmap/crafter pipeline — deviation noted below.

## Wave: DELIVER / [REF] Files modified

- `Lighthouse.Backend/.../Jira/JiraWriteBackTest.cs` — `[OneTimeSetUp]`/`[OneTimeTearDown]` + `CreateScratchIssue`/`CreateScratchClient` helpers; `const` → instance `EpicId`/`StoryId`; connection-builder now uses shared consts.

## Wave: DELIVER / [REF] Demo evidence (live, token present)

- **Run 1**: `dotnet test --filter ~JiraWriteBackTest` → `Passed! Failed: 0, Passed: 16` (13 s).
- **Run 2 (AC6)**: back-to-back → `Passed! Failed: 0, Passed: 16` (12 s). Distinct scratch keys per run (`DateTime.UtcNow:O` summary + auto-increment keys).
- **AC4**: `JiraLighthouseIntegrationTestToken` unset → `WriteFieldsToWorkItems_EmptyUpdates` `Passed: 1` (66 ms), no Jira call, no NRE.
- **KPI-2 residue**: `POST /rest/api/2/search/jql summary ~ "Lighthouse WriteBack scratch"` → 0 issues after runs.
- **Empirical unknown-resolution**: create Epic/Story `201`; write `customfield_10205`/`_10206` on fresh issues `204`; `DELETE` `204` (integration user has delete permission — Q1 resolved, no fallback). `LGHTHSDMO` = next-gen/team-managed → no Epic-Name requirement (Q3), createmeta unnecessary (D8 simplified).

## Wave: DELIVER / [REF] DoD check

1. Constants removed → keys from setup — ✅ AC1. 2. Per-run Epic+Story — ✅ AC2. 3. Teardown deletes, logged — ✅ AC3. 4. Token-absent skip-safe — ✅ AC4. 5. Existing assertions green vs fresh issues — ✅ AC5 (16/16). 6. Two consecutive runs green, distinct keys — ✅ AC6. 7. `dotnet build` 0 warnings — ✅. 8. SonarQube new issues — ⏳ CI-verified (no obvious new smell; consts dedupe literals). 9. `ci-learnings.md` note — ⏳ HELD pending user confirmation (docs-after-code).

## Wave: DELIVER / [REF] Quality gates

- Refactor L1–L6: N/A (single small fixture change, already clean). Adversarial review: not auto-dispatched (per standing rule + review-before-closeout). **Mutation: N/A — no production code changed** (mutation targets production; test-only delta has nothing to mutate). Integrity (DES): N/A — direct delivery, not DES-monitored (deviation).

## Wave: DELIVER / [WHY] Deviation from DES pipeline

`/nw-deliver` prescribes roadmap → crafter subagent (DES markers) → mutation → integrity. Skipped: (a) single test file, no production surface — roadmap/crafter dispatch adds ceremony, no value; (b) standing instruction "don't spawn agents unless asked"; (c) mutation/integrity target production TDD, absent here. Trade-off: no DES audit-log for this delivery. Given the infrastructure-only classification and full live AC verification above, acceptable. Flag if DES audit trail is required — I can re-run through the instrumented pipeline.

---

## Wave: DISTILL / [REF] Reconciliation

- Read DISCUSS (D1–D4) + DESIGN (D5–D8) decisions. **Reconciliation passed — 0 contradictions.** DESIGN refines DISCUSS without conflict: D7 confirms D3 (hard-delete + transition fallback), D5/D6/D8 add mechanism detail, D4 skip-safe preserved.
- Language detected: **C# / .NET 10**, test framework **NUnit 4.6 + Moq** (project marker: `*.csproj`; existing fixture uses NUnit `[Test]`/`[Category]`). Polyglot matrix row C# = FsCheck/xUnit is NOT used — project convention (NUnit) wins per the language-convention frame.

## Wave: DISTILL / [REF] Verification strategy (no new AT artifacts)

This feature has **no new production surface** — it modifies the lifecycle of an existing integration-test fixture (`JiraWriteBackTest`). The acceptance test *is* that fixture. Consequences:

- **No new `.feature` file / step-defs**: there is no business-language journey to author; the "SUT" is the test's own setup/teardown. Writing meta-ATs that assert "the fixture created a scratch item" would test-the-test — rejected as ceremony.
- **No RED scaffold (Mandate 7 N/A)**: Mandate 7 scaffolds *production* modules imported by new ATs. No production module is added; nothing to stub. Not a silent skip — structurally inapplicable.
- **No PBT / state-delta**: this is a **layer-3 real-I/O** integration test (real Jira Cloud). Per Mandate 9/11, layer 3+ is example-only; sad paths enumerated, never PBT-generated. The existing fixture is already example-based — correct.
- **Register Outcomes: SKIPPED** (correct) per D-6 gate-scoping — test-methodology-only, no new typed contract in `docs/product/outcomes/registry.yaml`.

## Wave: DISTILL / [REF] AC → verification mapping

| AC | Verification mechanism | Layer |
|---|---|---|
| AC1 (no fixed constants; keys from setup) | Structural — code review + compile (`const` → instance field). DELIVER RED = grep shows `LGHTHSDMO-1/16` gone. | static |
| AC2 (setup creates Epic+Story via REST) | Live run — `[OneTimeSetUp]` executes `POST /rest/api/2/issue` ×2; run with token. | 3 (real-io) |
| AC3 (teardown deletes best-effort, logged) | Live run + board query — after run, no `Lighthouse WriteBack scratch *` residue (KPI-2). Teardown try/catch verified by review. | 3 (real-io) |
| AC4 (token-absent skip-safe) | **Existing non-integration test** `WriteFieldsToWorkItems_EmptyUpdates_ReturnsEmptyResult` runs WITHOUT token → exercises the early-return `[OneTimeSetUp]` path; must stay green with no NRE from empty keys. | 1 (no-io) |
| AC5 (existing assertions pass vs created issues) | Live run — full `JiraWriteBackTest` green against fresh Epic/Story (write + read-back of `customfield_10205`/`_10206`). | 3 (real-io) |
| AC6 (two consecutive suite runs green, distinct keys) | **This is the fail-for-right-reason / acceptance gate** — run full backend suite twice; both green, scratch keys differ. | 3 (real-io) |

## Wave: DISTILL / [REF] Adapter coverage

| Driven adapter | Real-I/O coverage | Covered by |
|---|---|---|
| Jira REST create (`POST /issue`) | YES | AC2 live run |
| Jira REST delete (`DELETE /issue/{key}`) | YES | AC3 live run + residue query |
| Jira REST createmeta (`GET /issue/createmeta`) | YES | AC2 (required-field discovery, DESIGN D8) |
| Jira REST write-back + read-back (existing) | YES | AC5 (unchanged existing coverage) |

Zero MISSING rows. All exercised against real Jira Cloud (`LGHTHSDMO`).

## Wave: DISTILL / [REF] Pre-DELIVER fail-for-right-reason gate

- Genuine-RED equivalent for this feature = **AC6**: the modified fixture, run twice full-suite, is green with distinct keys and no residue. Wrong-reason failures to watch in DELIVER: (a) `DELETE` 403 → not a test bug, it's DESIGN-Q1 → switch to transition fallback (D7); (b) Epic create 400 missing required field → DESIGN-Q2/D8 → populate from `createmeta`; (c) custom-field write rejected on fresh issue → DESIGN-Q2 → confirm field context. None of these are "import/fixture-broken" false REDs; each maps to a locked design fallback.
- DELIVER PREPARE reads this mapping in place of a `red-classification.md` (no scaffolds to classify).

## Wave: DISTILL / [REF] Final Wave Review Gate

- Mandatory 4-reviewer parallel gate (Eclipse/Architect/Forge/Sentinel) is **deferred to user discretion** for this feature: single test file, zero new scenarios/scaffolds, infrastructure-only. Sentinel's structural-correctness domain (Gherkin antipatterns, hexagonal boundary, scaffold integrity) has no surface here — there are no scenarios or scaffolds to review. Per standing "don't spawn agents unless asked," not auto-dispatched. User may invoke `/nw-review` if desired.
- Deliverable type: `application` → no plugin/skill reviewer routing.

## Wave: DISTILL / [REF] Pre-requisites

- DELIVER needs a valid `JiraLighthouseIntegrationTestToken` + `JiraLighthouseIntegrationTestUsername` to run AC2/AC3/AC5/AC6 live. Without them, only AC1 (static) + AC4 (no-io) are locally verifiable; AC2/3/5/6 defer to CI/an environment with the token.

---

## Wave: DESIGN / [REF] DDD list

- **[D5] Test owns its own `HttpClient`; no production connector reuse** — Verdict: LOCKED. `JiraWorkTrackingConnector.GetJiraRestClientAsync` is `private` and Jira has no SDK client (unlike ADO's `WorkItemTrackingHttpClient`). `[OneTimeSetUp]` builds a plain `HttpClient` with `BaseAddress = https://letpeoplework.atlassian.net/` and `Authorization: Basic base64(username:apiToken)`, reading the same env vars the fixture already uses (`JiraLighthouseIntegrationTestUsername`, `JiraLighthouseIntegrationTestToken`). Mirrors ADO `2964383c`, which built its own `VssConnection` rather than touching production internals. Confirmed by reading the connector: `RoutesViaAtlassianCloudGateway` is false for `AuthenticationMethodKeys.JiraCloud` (only `JiraScopedToken`/`JiraOAuth` route via `api.atlassian.com`), so site base URL + Basic auth is exactly what production uses for this connection.
- **[D6] Scratch set = 1 Epic + 1 Story (no second Story)** — Verdict: LOCKED. Unlike ADO (which needed `secondStoryId`), the Jira `WriteFieldsToWorkItems_MultipleUpdates_SucceedsForAll` test uses `EpicId` + `StoryId`. `[OneTimeSetUp]` creates exactly one Epic (replaces `EpicId = "LGHTHSDMO-1"`) and one Story (replaces `StoryId = "LGHTHSDMO-16"`) via `POST /rest/api/2/issue`, project `LGHTHSDMO`.
- **[D7] Teardown = hard delete `DELETE /rest/api/2/issue/{key}?deleteSubtasks=true`** — Verdict: LOCKED (confirms DISCUSS D3). Best-effort try/catch, log via `TestContext.Progress.WriteLine`, keys tracked in a `List<string>` cleared after teardown. **Fallback** if the integration user lacks "Delete issues" on LGHTHSDMO: transition scratch issues to a terminal status via `POST /rest/api/2/issue/{key}/transitions` (the ADO approach). Hard-delete-vs-fallback is a DELIVER-time runtime verification (Open question Q1).
- **[D8] Required-field discovery via `createmeta`** — Verdict: LOCKED. Company-managed "Epic" issue types can require an "Epic Name" field on create. DELIVER queries `GET /rest/api/2/issue/createmeta?projectKeys=LGHTHSDMO&issuetypeNames=Epic,Story&expand=projects.issuetypes.fields` once to learn required create fields and populates them (minimum `project`, `issuetype`, `summary`, plus any `required:true` field). Robust to project configuration.

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change |
|---|---|---|
| `JiraWriteBackTest` fixture | `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Jira/JiraWriteBackTest.cs` | EXTEND — add `[OneTimeSetUp]`/`[OneTimeTearDown]`, private `CreateScratchIssue`/`DeleteScratchIssue` helpers + a test-local `HttpClient` builder; convert `EpicId`/`StoryId` from `const` to instance fields set in setup. |
| `docs/ci-learnings.md` | `docs/ci-learnings.md` | EXTEND — update/close the `JiraWriteBackTest` shared-scratch flakiness note. |

No production-code file changes. `JiraWorkTrackingConnector` untouched.

## Wave: DESIGN / [REF] Driving & driven ports

- Driving: NUnit fixture lifecycle hooks (`[OneTimeSetUp]`/`[OneTimeTearDown]`).
- Driven: Jira Cloud REST — `POST /rest/api/2/issue` (create), `DELETE /rest/api/2/issue/{key}` (teardown), `GET /rest/api/2/issue/createmeta` (required-field discovery). Read-back path uses the connector as today. Adapter = test-local `HttpClient` with Basic auth.

## Wave: DESIGN / [REF] Technology choices

- .NET 10 / NUnit 4.6, `System.Net.Http.HttpClient` + `System.Text.Json` for REST payloads (no new package). Basic-auth header via `AuthenticationHeaderValue("Basic", base64(user:token))`.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `JiraWorkTrackingConnector.GetJiraRestClientAsync` | `.../Jira/JiraWorkTrackingConnector.cs:1276` | Builds an authed Jira `HttpClient` | CREATE NEW (test-local) | Method is `private`; exposing it (internal + `InternalsVisibleTo`) widens production surface for a test-only need. ADO precedent (`2964383c`) built its own client. ~10 LOC to build a Basic-auth client from env vars. |
| ADO `AzureDevOpsWriteBackTest` scratch pattern | `.../AzureDevOps/AzureDevOpsWriteBackTest.cs` | Per-run create/teardown lifecycle | EXTEND (port pattern) | Same design ported to Jira REST; no shared base class — DRY-of-knowledge does not apply to two distinct vendor REST shapes. |

## Wave: DESIGN / [REF] Decisions table

| ID | Decision |
|---|---|
| D5 | Test-owned `HttpClient`, Basic auth, site base URL; no connector reuse. |
| D6 | Scratch set = 1 Epic + 1 Story. |
| D7 | Hard-delete teardown; transition fallback on permission denial. |
| D8 | `createmeta` drives required create fields (Epic Name etc.). |

## Wave: DESIGN / [REF] Open questions (deferred to DELIVER)

- **Q1 (DISCUSS unknown #1 — delete permission)**: does the integration user hold "Delete issues" on `LGHTHSDMO`? Runtime-only; not knowable from code. DELIVER's first run resolves it — if `DELETE` returns 403, switch D7 to the transition fallback. Bounded, non-blocking.
- **Q2 (DISCUSS unknown #2 — custom fields)**: LOW risk, resolved by analysis. `customfield_10205` (Delivery Date) / `customfield_10206` (Age) are project/instance field definitions; a fresh Epic/Story of the same types in `LGHTHSDMO` inherits the same field contexts and edit-screen config that let `LGHTHSDMO-1`/`-16` accept these writes today. DELIVER confirms on first green run (AC5). No design change needed.
- **Q3**: is `LGHTHSDMO` company-managed or team-managed? Determines whether Epic create needs an Epic-Name field — handled generically by D8 (`createmeta`), so not blocking.

## Wave: DESIGN / [REF] SSOT & gate notes (no silent N/A)

- **`docs/product/architecture/brief.md`**: NOT updated. Rationale — brief.md is the production-architecture SSOT; this feature changes only test-fixture lifecycle and adds no production component, port, or ADR-worthy decision. A test-infra entry would pollute the production SSOT.
- **ADR**: none. No production architectural decision; D5–D8 are test-scoped and captured here.
- **Outcome Collision Check**: SKIPPED (correct) per D-6 gate-scoping — test-methodology-only, no new typed contract surface in `docs/product/outcomes/registry.yaml`.
- **C4 diagrams**: N/A — no new container/component topology; a single test fixture calling Jira REST adds no node.
- **Paradigm**: OOP (unchanged, project default). No CLAUDE.md write.

---

## Wave: DISCUSS / [REF] Persona

- **Persona ID**: `lighthouse-maintainer` — the developer running the full backend suite locally and in CI.
- Not an end-user surface. No production behavior changes. This feature exists so the maintainer's `dotnet test` run is deterministic.

## Wave: DISCUSS / [REF] JTBD one-liner

N/A — **infrastructure-only escape valve** (Decision 4 = No). Pure internal test-infrastructure change; no user-visible behavior, so no `job_id` traces to `docs/product/jobs.yaml`. Rationale carried per-story below.

## Wave: DISCUSS / [REF] Pre-requisites

- Reference implementation exists: ADO commit `2964383c` (`AzureDevOpsWriteBackTest` per-run scratch items). This feature ports that proven pattern to Jira.
- Jira integration test token env var `JiraLighthouseIntegrationTestToken` (already used by the fixture).
- Jira Cloud project `LGHTHSDMO` on `letpeoplework.atlassian.net`.

## Wave: DISCUSS / [REF] Locked Decisions

- **[D1] Infrastructure-only classification** — Verdict: LOCKED. `JiraWriteBackTest` is `[Category("JiraIntegration")]` test code only; no production code, no user surface. Every story uses `job_id: infrastructure-only`. (confirmed 2026-07-23)
- **[D2] Port the ADO per-run scratch-item pattern** — Verdict: LOCKED. Add `[OneTimeSetUp]` that creates a fresh Epic + Story per fixture run via the Jira REST API; replace the fixed constants `EpicId = "LGHTHSDMO-1"` / `StoryId = "LGHTHSDMO-16"` with the created keys.
- **[D3] Teardown = hard delete** — Verdict: LOCKED. Jira PAT can delete issues via `DELETE /rest/api/2/issue/{key}`, unlike the ADO PAT (VS403145) which forced a state-transition. `[OneTimeTearDown]` hard-deletes each created issue, best-effort (try/catch, log via `TestContext.Progress`). **DESIGN RISK** (now DESIGN Q1): if the integration PAT lacks delete permission, fall back to transitioning the scratch issues to a terminal state.
- **[D4] Skip-safe when no token** — Verdict: LOCKED. Mirror ADO: if `JiraLighthouseIntegrationTestToken` is unset (fork PRs), `[OneTimeSetUp]` returns early without touching Jira; the `[Category("Integration")]` tests are already excluded by category filter, and the non-integration test (`WriteFieldsToWorkItems_EmptyUpdates_ReturnsEmptyResult`) still runs.

## Wave: DISCUSS / [REF] User Stories

### Story A — Deterministic per-run Jira scratch issues

`job_id: infrastructure-only`
`infrastructure_rationale`: This is test-suite reliability infrastructure. The change lives entirely in `JiraWriteBackTest.cs`; no application code path, API, or UI is touched. No end-user job applies — the beneficiary is the maintainer running CI/`dotnet test`. Analogous to ADO commit `2964383c` which was likewise a test-only fix.

**As** the Lighthouse maintainer, **I want** each `JiraWriteBackTest` fixture run to create and destroy its own Jira Epic + Story, **so that** parallel/full-suite runs no longer collide on the shared `LGHTHSDMO-1` / `LGHTHSDMO-16` items and the suite stops being flaky.

`@infrastructure` — no user-visible output; observable outcome is a deterministic test run.

**Observable outcome** (in place of Elevator Pitch, per infra-only rule):
Before: `dotnet test` on the full backend suite intermittently shows one `JiraWriteBackTest` failure that passes when the test is run alone; every crafter step during Story #5524 had to note it as a known-unrelated failure.
After: run `dotnet test --filter "FullyQualifiedName~JiraWriteBackTest"` (with token) **and** the full suite → `JiraWriteBackTest` passes in both, with no shared-item collision across repeated runs.
Outcome enabled: maintainer trusts a red suite means a real regression, not scratch-item contention.

**Acceptance Criteria**
- AC1: `JiraWriteBackTest` no longer references the fixed constants `EpicId = "LGHTHSDMO-1"` / `StoryId = "LGHTHSDMO-16"`; the Epic and Story keys used by the tests come from issues created in `[OneTimeSetUp]`.
- AC2: `[OneTimeSetUp]` creates a fresh Epic and Story via the Jira REST API using the fixture's existing connection/token (scratch set = 1 Epic + 1 Story, per DESIGN D6).
- AC3: `[OneTimeTearDown]` deletes every issue created in setup, best-effort: wrapped in try/catch, failures logged via `TestContext.Progress.WriteLine` and never fail the run. Created-issue keys tracked in a list, cleared after teardown.
- AC4: When `JiraLighthouseIntegrationTestToken` is unset, `[OneTimeSetUp]` returns early without any Jira call; the non-integration test still passes and integration tests are skipped by category filter (no crash, no NRE from empty keys).
- AC5: All existing `JiraWriteBackTest` assertions still pass against the freshly-created issues (the created Epic/Story must accept the custom fields `Delivery Date` `customfield_10205` and `Age` `customfield_10206` the tests write). DELIVER confirms on first green run.
- AC6: Running the full backend suite twice back-to-back produces two green `JiraWriteBackTest` results with distinct scratch issue keys (no residue, no collision).

## Wave: DISCUSS / [REF] Definition of Done

1. Fixed constants removed; keys sourced from `[OneTimeSetUp]`-created issues. ✅ gate = AC1
2. Per-run Epic + Story created via Jira REST. ✅ gate = AC2
3. Teardown hard-deletes, best-effort, logged. ✅ gate = AC3
4. Token-absent path is skip-safe. ✅ gate = AC4
5. All existing assertions green against created issues. ✅ gate = AC5
6. Two consecutive full-suite runs both green, distinct keys. ✅ gate = AC6
7. `dotnet build` zero warnings (`TreatWarningsAsErrors`).
8. No SonarQube new issues (test file).
9. `docs/ci-learnings.md` entry for `JiraWriteBackTest` flakiness updated/closed.

## Wave: DISCUSS / [REF] Out-of-scope

- No change to `JiraWorkTrackingConnector` production code. The test issues the REST calls directly (via a test-local HTTP client) — same as ADO used `WorkItemTrackingHttpClient` directly in the test.
- No change to the ADO write-back test (already fixed).
- Not fixing the *other* live-Jira flakiness classes tracked in `ci-learnings.md` (rate-limit / Jira-Cloud incident) — only the shared-scratch-item collision.
- No parallelism-config change; the fix makes the fixture collision-free regardless of parallel settings.

## Wave: DISCUSS / [REF] WS strategy

Strategy **B** (extend existing) — brownfield, single test file, proven reference pattern. No walking skeleton; the slice IS the whole feature.

## Wave: DISCUSS / [REF] Driving ports

None (no inbound surface). Test-fixture lifecycle hooks + Jira REST API as the driven port.

## Wave: DISCUSS / [REF] Story Map

Single activity → single slice. See `slices/slice-01-per-run-jira-scratch-items.md`.

- **Backbone**: Reliable Jira write-back integration tests.
- **Walking skeleton / only slice**: slice-01 — create-per-run + delete-teardown + swap constants.
- **Prioritization**: N/A (one slice). Highest-leverage and only work item.

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** 1 story, 1 bounded context (test infra), 1 file, ~1 integration point (Jira REST), effort ≤½ day. Zero oversized signals. No split needed.

## Wave: DISCUSS / [REF] Outcome KPIs

- **KPI-1 — Suite flakiness**: `JiraWriteBackTest` failure rate in full-suite runs. Target: **0** shared-item-collision failures across ≥5 consecutive full-suite runs. Measure: local/CI `dotnet test` repetition.
- **KPI-2 — Scratch-item residue**: leftover `Lighthouse WriteBack scratch *` issues on the `LGHTHSDMO` board after a run. Target: **0** after teardown (allowing best-effort leftovers only on teardown exception). Measure: Jira board query for the scratch-title prefix.

## Wave: DISCUSS / [REF] DoR Validation

1. Story independent & valuable — ✅ standalone test fix.
2. Negotiable — ✅ teardown mechanism has documented fallback (D3/DESIGN D7).
3. Estimable — ✅ ≤½ day, reference class = ADO `2964383c` (161 LOC test-only diff).
4. Small — ✅ single file, single slice.
5. Testable — ✅ ACs verified by running the fixture (self-verifying).
6. Job traceability — ✅ `infrastructure-only` + rationale on Story A (escape valve, non-user-facing).
7. Dependencies known — ✅ reference commit, token, LGHTHSDMO project, custom-field availability (AC5 risk, DESIGN Q2 resolved LOW).
8. AC unambiguous — ✅ 6 concrete ACs.
9. No blocking unknowns — ✅ DESIGN resolved the two unknowns: Q1 (delete permission) DELIVER-verified with fallback; Q2 (custom fields) analyzed LOW risk. Both bounded, non-blocking.

## Wave: DISCUSS / [REF] Wave Decisions

### Key Decisions
- [D1] Infrastructure-only classification — no user surface.
- [D2] Port ADO `2964383c` per-run scratch pattern to Jira.
- [D3] Hard-delete teardown (Jira PAT can delete), transition fallback if permission missing.
- [D4] Token-absent early-return keeps fork PRs green.

### Requirements Summary
- Primary need: eliminate `JiraWriteBackTest` shared-scratch-item flakiness by giving each fixture run its own throwaway Jira Epic + Story, deleted in teardown.
- Walking skeleton scope: entire feature = one slice.
- Feature type: infrastructure (test tooling).

### Constraints Established
- Test-only; no production code change.
- Must remain skip-safe without an integration token.
- Created issues must accept the custom fields the tests write, or the field set adjusts (AC5).

### Upstream Changes
- None. No DISCOVER artifacts existed for this item; no assumptions changed.

## Wave: DISCUSS / [REF] Handoff

To **nw-solution-architect (DESIGN)**: resolved — see DESIGN sections above (D5–D8, Q1–Q3). To **nw-platform-architect (DEVOPS)**: KPIs only (flakiness rate, residue count).
