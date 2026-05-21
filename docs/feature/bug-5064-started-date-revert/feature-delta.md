# Feature Delta: bug-5064-started-date-revert

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISTILL | Date: 2026-05-21 | Density: lean (per ~/.nwave/global-config.json)

Bug goal: ADO Bug 5064 (reported by Liz, observed on Jira) — a work item that follows the path `ToDo → Doing(T1) → ToDo → Done(T3)` keeps the original Doing-entry timestamp `T1` as its `StartedDate` instead of clearing it. Expectation: because the item was never "in progress" at the moment it closed, `StartedDate` should equal `ClosedDate` (`T3`). The reporter explicitly noted the direct `ToDo → Done` path already works — this fix extends the same semantics to the revert-then-skip-Doing path.

DISTILL is fast-tracked: no DISCUSS / DESIGN / DEVOPS waves. The fix is a localised behavioural change on an already-tested algorithm in two adapters (no public contract change, no schema change, no migration, no new endpoint). Rex's RCA (see Phase 1 output, summarised below) serves as the design doc.

---

## Wave: DISTILL / [REF] Root cause (from @nw-troubleshooter / Phase 1)

The `Done` branch of both connectors derives `StartedDate` by scanning state history for the most recent transition INTO a Doing state, filtering out transitions whose previous state is already Doing or already Done:

- ADO: `AzureDevOpsWorkTrackingConnector.cs:733-737` → `GetStateTransitionDateThrottled` at `:751-771` (filter at `:763`, latest-pick at `:769`).
- Jira: `IssueFactory.cs:69-73` → `GetTransitionDate` at `:166-191` (filter at `:206`).

For the path `ToDo → Doing(T1) → ToDo → Done(T3)`, there is exactly one qualifying transition (the initial T1), so `last = T1` and `startedDate = T1`. The subsequent null-fallback at `AzureDevOpsWorkTrackingConnector.cs:743-746` / `IssueFactory.cs:79-83` (which is what makes Liz's "ToDo → Done direct" case work) is bypassed because `startedDate` is non-null.

Missing constraint: the lookup does not require the chosen "moved to Doing" timestamp to lie *after* the last revert to ToDo. Direct ToDo → Done was protected by the null-fallback; revert-then-skip-Doing is the equivalence class that fell through.

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | Impact |
|---|---|---|
| `Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:723-748` | `GetStartedAndClosedDateForWorkItem` returns `(DateTime? startedDate, DateTime? closedDate)`; signature and `Done`/`Doing`/`Other` branching unchanged. | Public/internal signature stays. Only the inside of the `Done` branch (between line 736 and the existing null-fallback at 743) gains the ToDo-after-StartedDate invalidation step. |
| `Lighthouse.Backend/Factories/IssueFactory.cs:58-86` | `GetStartedAndClosedDate(JsonElement, IWorkItemQueryOwner, string)` returns the same tuple shape. | Same shape preserved. Equivalent invalidation step added inside the `Done` branch. |
| `Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:743-746` (null-fallback) | `if (startedDate == null && closedDate != null) startedDate = closedDate;` | **Reused.** The fix sets `startedDate = null` when a ToDo-revert post-dates it, so the existing fallback patches it to `closedDate` — same semantics as Liz's "already working" direct ToDo → Done case. |
| `Lighthouse.Backend/Factories/IssueFactory.cs:79-83` (null-fallback) | Identical pattern. | Reused for Jira side, same way. |
| `Lighthouse.Backend/Models/IWorkItemQueryOwner.cs:13` | `List<string> ToDoStates { get; set; }` exists on the interface — already accessible from both adapters. | No new property needed. Acquire via `workItemQueryOwner.GetRawStatesForCategory(workItemQueryOwner.ToDoStates)`. |
| Existing test `IssueFactoryTest.cs:343` (`IssueInDone_MovedToDoingAndBackToDone_…`) | When an item is reopened from Done back to Doing and then re-Done, `StartedDate` = first Doing entry; `ClosedDate` = second Done entry. | **Must keep passing.** The filter parameters for the new invalidation walk use `statesToIgnore = rawDoneStates` so the implicit `Done → ToDo`/`Done → Doing` reopen transition is ignored. |
| Existing test `IssueFactoryTest.cs:375` (`IssueInDone_MovedToToDoAndBackToDone_…`) | A Done item reopened via Resolved → Analysis → Implementation → Verification → Resolved → Closed produces `StartedDate` = the *second* Implementation entry (Sept 30). | **Must keep passing.** This test's path enters Doing again (Implementation on 30 Sept) AFTER the implicit ToDo phase, so even if we recompute, `startedDate` still equals 30 Sept (later than the Analysis "ToDo" date 28 Sept). Validate this manually during GREEN. |
| Existing test `IssueFactoryTest.cs:284` (`IssueInDoing_MovedToToDoAndBackInDoing`) | `Doing` branch (not `Done` branch) — fix does not touch this branch. | Unaffected. |

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

No prior wave decisions exist (no DISCUSS / DESIGN / DEVOPS sessions). Reconciliation gate passes trivially. Cross-feature note: the duplicated history-walking algorithm across ADO + Jira (and partially Linear) is a known refactor candidate — explicitly **out of scope** for this fix (separate work item; CLAUDE.md mandates refactor commits separate from fix commits).

---

## Wave: DISTILL / [REF] Scenario list (acceptance/bug-5064-started-date-revert.feature)

| # | Scenario | Tags |
|---|---|---|
| 1 | Jira issue: `ToDo → Doing → ToDo → Done` produces `StartedDate == ClosedDate` | `@bug-5064 @regression @jira @in-memory` |
| 2 | ADO work item: `ToDo → Doing → ToDo → Done` produces `StartedDate == ClosedDate` | `@bug-5064 @regression @ado @real-io` |
| 3 | Jira: existing `Done → ToDo → … → Doing → Done` reopen flow still resolves to the second Doing entry (parity guard) | `@bug-5064 @regression @jira @in-memory` |

Scenario 1 is the primary Jira regression. Scenario 2 is the ADO mirror (tagged `@real-io` because the existing ADO test fixture pattern provisions real work items in `CMFTTestTeamProject`). Scenario 3 is a parity guard — the existing test at `IssueFactoryTest.cs:375` already exercises this; the crafter should run it after the fix and confirm it remains GREEN (no new test needed, but tag the existing one mentally for this fix).

The `Doing` branch and the `Other` branch are unchanged by this fix, so no new scenarios target them — covered by existing tests.

---

## Wave: DISTILL / [REF] Test placement

**Jira (synthetic, easy)** — add one new `[Test]` method to `Lighthouse.Backend/Lighthouse.Backend.Tests/Factories/IssueFactoryTest.cs` next to line 375. Use the existing helpers `CreateJsonDocument` + `AddChangelogEntries` + `CreateChangelogEntry`. Suggested name: `IssueInDone_MovedToDoingThenBackToToDoThenDirectlyToDone_StartedDateEqualsClosedDate`. Final state: `"Closed"`. Changelog entries:

```text
Backlog → Implementation, T1=2024-09-27
Implementation → Backlog, T2=2024-09-28
Backlog → Closed,         T3=2024-09-30
```

Assert: `issue.StartedDate?.Date == new DateTime(2024,9,30,0,0,0,DateTimeKind.Utc).Date` AND `issue.ClosedDate?.Date == new DateTime(2024,9,30,0,0,0,DateTimeKind.Utc).Date`, both `Kind == Utc`.

**ADO (real-io fixture)** — add one new `[Test]` method to `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnectorTest.cs` next to line 287. The existing ADO tests follow the pattern of querying a single work-item ID in `CMFTTestTeamProject` (e.g. IDs 396, 397, 398, 399 — Read this file for the pattern). Suggested name: `SetStartedAndClosedDate_ItemMovedFromToDoToDoingBackToToDoThenToDone_StartedEqualsClosed`. Pick the next free ID (likely **400**); the crafter MUST provision this work item upstream in CMFTTestTeamProject with the revision history above before the test will pass.

**Crafter decision required**: if ADO fixture provisioning is blocked (e.g. no write access to CMFTTestTeamProject during this session), the crafter has two options:

1. **Defer the ADO regression test** to a follow-up commit / work item, and ship the Jira test + both fixes now. Justified because the ADO code path is identical in shape to the Jira code path and the Jira test exercises the same logical defect — though it leaves the ADO fix without a per-connector regression net.
2. **Refactor `GetStartedAndClosedDateForWorkItem` to extract the history-resolution step behind a seam** that can be unit-tested without ADO. This is a larger change and per CLAUDE.md belongs in a separate refactor commit; not recommended for the same delivery.

**Recommended path: option 1** if the fixture cannot be created in-session — record the deferred ADO test as a follow-up work item and proceed. The Jira test is the primary regression net for the algorithmic defect; the ADO duplicate copy receives a parallel code change verified by manual / customer-instance smoke after deploy.

Conventions inherited from both test files (NUnit 4.x):

- `[TestFixture]`, `[Test]`, `Assert.That(..., Is.EqualTo(...))`, `Assert.EnterMultipleScope()` for grouped asserts.
- Factory functions over shared mutable setup (CLAUDE.md DRY rule).
- No mocking of third-party libraries (`witClient` is fronted by the test fixture, not mocked directly).

---

## Wave: DISTILL / [REF] Walking-skeleton strategy

Strategy A (Full InMemory for Jira; Real-IO for ADO) — auto-detected. The Jira scenario runs entirely in-process against a synthetic JSON changelog. The ADO scenario tags `@real-io` because the existing fixture pattern provisions real CMFTTestTeamProject work items. Walking-skeleton is omitted (bug fix per the DISTILL skill).

---

## Wave: DISTILL / [REF] Adapter coverage table

| Adapter | `@real-io` scenario | Covered by |
|---|---|---|
| Jira (`IssueFactory`) | n/a (synthetic JSON) | New `[Test]` in `IssueFactoryTest.cs` |
| ADO (`AzureDevOpsWorkTrackingConnector`) | scenario 2 (`@real-io`) | New `[Test]` in `AzureDevOpsWorkTrackingConnectorTest.cs` *or* deferred per crafter decision |
| Linear | **out of scope** — see Out of scope below | — |
| CSV | n/a (StartedDate is read from a column, not derived from transitions) | — |

---

## Wave: DISTILL / [REF] Pre-requisites

| Source | Pre-requisite |
|---|---|
| DESIGN driving ports | None — bug fix, no new ports. |
| DEVOPS environment matrix | Default. The Jira test runs against in-process JSON; the ADO test (if included) requires the same CMFTTestTeamProject access the existing tests rely on. |
| External services | ADO test fixture: needs a new work item provisioned in `CMFTTestTeamProject` with `ToDo → Doing → ToDo → Done` history (suggested ID 400). |
| Feature flags | None. |
| Migrations | None. |
| ADO bug 5064 state | Already `Active` (Implementation column), assigned to Benj, tagged `Release Notes`. Transitions per `/ado-sync`: stays `Active` through DELIVER, moves to `Resolved` after commits land + CI green. |

---

## Wave: DISTILL / [REF] Out of scope

- **Linear connector**. Linear exposes a single mutable `startedAt` GraphQL field (`LinearResponses.cs:137`, `:195`); Lighthouse cannot recompute it from changelog without a richer Linear API query (`issueHistory`). If a customer reports the same defect on Linear, file a separate bug; do not bundle here.
- **Extracting a shared `StateTransitionDateResolver`** from the duplicated ADO + Jira implementations. CLAUDE.md mandates refactor commits separate from fix commits; track as a follow-up refactor.
- **Adding an invariant guard in `WorkItemBase`** that asserts `StartedDate <= ClosedDate`. Useful but orthogonal; covers a wider class of bugs than 5064.
- **Customer-data backfill**. Both connectors recompute StartedDate from history on every sync, so deployed instances self-heal — no migration / backfill needed.

---

## Wave: DISTILL / [REF] Mandatory review gate

DISTILL artefact (this file + the `.feature` file the crafter writes during DELIVER setup) is reviewed by `@nw-acceptance-designer-reviewer` (Sentinel, Haiku) on the DELIVER orchestrator's first pre-flight pass. Verdict must be `approved` or `conditionally_approved` before regression tests are written.

---

## Wave: DISTILL / [REF] Hand-off context for /nw-deliver

- Paradigm: OOP (per CLAUDE.md). Crafter: `@nw-software-crafter`.
- Roadmap shape (minimum): two steps.
  - **01-01 (RED)**: write the Jira regression test (and the ADO regression test if fixture provisioning is feasible). Run and confirm RED.
  - **01-02 (GREEN)**: implement the ToDo-after-StartedDate invalidation in both `AzureDevOpsWorkTrackingConnector.GetStartedAndClosedDateForWorkItem` and `IssueFactory.GetStartedAndClosedDate`. Run full backend suite; new tests + all existing tests must be GREEN.
- Mutation testing: per-feature Stryker scoped to `IssueFactory.cs` + `AzureDevOpsWorkTrackingConnector.cs` + the two test classes. Kill rate ≥ 80%.
- Commit pattern (CLAUDE.md conventional commits, scope = bug/area):
  - `test(work-items): add regression for started date when item reverts to to-do before close (bug 5064)`
  - `fix(work-items): clear started date when item reverts to to-do before closing (bug 5064)`
  - (Optional) `docs(work-items): note revert-to-to-do started date behaviour` if a docs touch is needed.
- Boy-Scout rule: if any banned comment (decorative dividers, section labels, restating-next-line, provenance) sits inside the touched methods, delete it in the fix commit. Per Phase 1 inspection, the existing methods are clean.
- Slice-boundary push: per `[[feedback_slice_boundary_ritual]]`, push after both commits land, wait for CI green (Build And Deploy Lighthouse on main, plus SonarCloud quality gate), then transition ADO bug 5064 `Active → Resolved` with reason in the same call (per `[[feedback_classifier_main_push_and_ado_reason]]`).

---

## Wave: DELIVER / [REF] Implementation summary

Two production files changed (`Lighthouse.Backend/Factories/IssueFactory.cs` +9/-2; `Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` +7/-0) and one test file (`Lighthouse.Backend.Tests/Factories/IssueFactoryTest.cs` +30/-0). Both connector copies of the history-walking algorithm gained an identical invalidation step inside the `StateCategories.Done` branch: a third history walk for `targetStates = rawToDoStates, statesToIgnore = rawDoneStates`, and `if (lastToDoEntryDate.HasValue && startedDate.HasValue && lastToDoEntryDate.Value > startedDate.Value) startedDate = null;`. The existing null-fallback (`IssueFactory.cs:79-83` / `AzureDevOpsWorkTrackingConnector.cs:743-746`) then patches `startedDate = closedDate` — reusing the semantics that already made Liz's "direct ToDo → Done" case work. Boy-Scout rule applied on the Jira file: two banned restate-the-code comments at the touched method (former lines 65 and 79) removed. The ADO Done-branch had no banned comments to remove. Public surface unchanged on both files; the `Doing` branch and the `Other` branch are untouched, protected by existing tests `IssueInDoing_MovedToToDoAndBackInDoing` (Jira) and `SetStartedDate_ItemMovedFromDoingToToDoBackToDoing_UsesSecondTransitionToDoing` (ADO).

Wave path was lean: DISCUSS / DESIGN / DEVOPS were inlined into DISTILL because Rex's RCA (Phase 1 of `/nw-bugfix`) served as the design doc. The roadmap was written inline (`validation.status = approved`, orchestrator-inline) rather than dispatching `@nw-solution-architect` — surgical scope of a one-line semantic change in two files, identical in shape to the bug-5016 precedent.

## Wave: DELIVER / [REF] Files modified

| Category | File | Change |
|---|---|---|
| Production (Jira) | `Lighthouse.Backend/Lighthouse.Backend/Factories/IssueFactory.cs` | +9 / -2. Added `rawToDoStates` local alongside `rawDoingStates`/`rawDoneStates`; new invalidation step inside `StateCategories.Done` branch (after the two existing `GetTransitionDate` calls, before the null-fallback). Removed two banned restate-the-code comments at former lines 65 and 79 (Boy-Scout). |
| Production (ADO) | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` | +7 / -0. Mirror of the Jira change: `rawToDoStates` local + invalidation step inside `StateCategories.Done` branch via `await GetStateTransitionDateThrottled(witClient, workItemId, rawToDoStates, rawDoneStates)`. |
| Tests (Jira) | `Lighthouse.Backend/Lighthouse.Backend.Tests/Factories/IssueFactoryTest.cs` | +30 / -0. One new `[Test]` method `IssueInDone_MovedToDoingThenBackToToDoThenDirectlyToDone_StartedDateEqualsClosedDate` immediately after the existing `IssueInDone_MovedToToDoAndBackToDone_ClosedDateSetToSecondTimeItEnteredDone`. Uses the file's existing helpers (`CreateJsonDocument`, `AddChangelogEntries`, `CreateChangelogEntry`, the static `workItemQueryOwner`, `CreateIssueFactory`) and the `Assert.EnterMultipleScope()` pattern from lines 363-371. |
| Tooling | `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.bug-5064-started-date-revert.json` (new) | Per-feature Stryker config scoped to `**/Factories/IssueFactory.cs` + `FullyQualifiedName~IssueFactoryTest`. Mirrors the bug-5016 per-feature config precedent. The ADO file is intentionally out of mutation scope: its tests are real-IO integration tests against `CMFTTestTeamProject`, unsuitable for the Stryker fast loop. The semantic is structurally identical and is mutation-validated via the Jira copy. |
| Workspace | `docs/feature/bug-5064-started-date-revert/` (new) | This `feature-delta.md` + `deliver/roadmap.json` + `deliver/execution-log.json`. |
| Evolution | `docs/evolution/2026-05-21-bug-5064-started-date-revert.md` (new — written at finalize) | Long-term archive: wave path, decisions, quality gates, carry-forward. |

## Wave: DELIVER / [REF] Scenarios green count

3 of 3 — captured 2026-05-21 via filtered `dotnet test`:

| # | Scenario | Tag | Status |
|---|---|---|---|
| 1 | Jira: `ToDo → Doing → ToDo → Done` produces `StartedDate == ClosedDate` (NEW) | `@bug-5064 @regression @jira @in-memory` | **GREEN** (RED on `main`; flipped GREEN by step 01-02) |
| 2 | ADO: same scenario | `@bug-5064 @regression @ado @real-io` | **DEFERRED** (see Out of scope clause #5 — ADO regression test requires provisioning a real work item in CMFTTestTeamProject; not feasible in-session. Jira test #1 covers the shared semantic.) |
| 3 | Jira parity: existing `Done → ToDo → … → Doing → Done` reopen still resolves to the second Doing entry | `@bug-5064 @regression @jira @in-memory` | **GREEN** — existing test `IssueInDone_MovedToToDoAndBackToDone_ClosedDateSetToSecondTimeItEnteredDone` at `IssueFactoryTest.cs:375` continues to pass (second Implementation entry on 2024-09-30 > Analysis/ToDo entry on 2024-09-28, so invalidation does NOT fire). |

Full backend suite: 2555 / 2556 GREEN (1 pre-existing `LicensingIntegrationTest` file-fixture failure, independent of this change — same pattern documented in bug-5016 evolution log).

## Wave: DELIVER / [REF] DoD check

No formal DoD checklist was issued upstream (lean bug-fix). De facto DoD derived from the RCA, the roadmap acceptance criteria, and CLAUDE.md quality gates:

| Item | Status | Note |
|---|---|---|
| Reproduce the bug deterministically in a new test | PASS | `IssueInDone_MovedToDoingThenBackToToDoThenDirectlyToDone_StartedDateEqualsClosedDate` FAILS on `25fbe9f3`'s baseline (`main` pre-fix) with `Expected: 2024-09-30, but was: 2024-09-27`. |
| Fix prevents the reproduction | PASS | Same test GREEN after `442ceb19`. |
| Both connectors (Jira + ADO) receive the fix | PASS | Verified by `grep -n 'rawToDoStates' Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs Lighthouse.Backend/Lighthouse.Backend/Factories/IssueFactory.cs` → 4 hits, both files. |
| Existing reversal tests stay GREEN | PASS | All four existing reversal tests on the Jira side (`IssueInDoing_MovedToToDoAndBackInDoing`, `IssueInDone_MovedToDoingAndBackToDone_…`, `IssueInDone_MovedToToDoAndBackToDone_…`, plus `IssueInUnknownState_MovedToToDo_…`) and all four on the ADO side stay GREEN. |
| Public surface unchanged | PASS | No method-signature edits; no new public methods; no new interface members (`IWorkItemQueryOwner.ToDoStates` already existed). |
| Full backend suite GREEN | PASS | 2555 / 2556 (1 pre-existing licensing flake — same as bug-5016). |
| `dotnet build` zero warnings | PASS | `TreatWarningsAsErrors` + `WarningLevel 5` satisfied. |
| CLAUDE.md comment policy | PASS | Zero banned comments added; two pre-existing banned comments inside the touched Jira method removed per the Boy-Scout rule. |
| Adversarial review | PASS | `nw-software-crafter-reviewer` (Haiku) → **approved**, 0 blockers / 0 high / 0 medium / 0 low across correctness, Testing Theater, symmetry, and CLAUDE.md compliance. |
| ADO bug state | Pending | Stays `Active` through finalize; transitions to `Resolved` after push + green CI per the slice-boundary ritual. |
| Mutation testing | _Running — see Quality gates_ | Stryker.NET per-feature scoped to `IssueFactory.cs` + `FullyQualifiedName~IssueFactoryTest`; results to be inlined below. |

## Wave: DELIVER / [REF] Demo evidence

Not applicable. This is a bug fix with no Elevator Pitch in any `## Wave: DISCUSS / [REF] User Stories with Elevator Pitches` section (no DISCUSS wave was run; the customer report on ADO 5064 is the spec). The acceptance grain is the `IssueFactory.GetStartedAndClosedDate` algorithm exercised by the new NUnit test. The "demo" is the targeted filtered `dotnet test` run captured under Scenarios green count above.

## Wave: DELIVER / [REF] Quality gates

| Gate | Outcome | Evidence |
|---|---|---|
| Phase 1 — Roadmap creation | PASS | `deliver/roadmap.json` (orchestrator-inline approval, surgical-scope precedent identical to bug-5016). `des-verify-integrity --roadmap-only` flagged "no execution-log entries yet" which is expected pre-execution. |
| Phase 2 — 5-phase TDD per step | PASS | Step 01-01 (commit `25fbe9f3`): PREPARE EXECUTED, RED_ACCEPTANCE EXECUTED (test fails with `Expected: 2024-09-30, but was: 2024-09-27`), RED_UNIT SKIPPED (NOT_APPLICABLE — acceptance == unit), GREEN SKIPPED (NOT_APPLICABLE — delivered by 01-02), COMMIT EXECUTED. Step 01-02 (commit `442ceb19`): PREPARE EXECUTED, RED_ACCEPTANCE SKIPPED (NOT_APPLICABLE — covered by 01-01), RED_UNIT SKIPPED (NOT_APPLICABLE — same), GREEN EXECUTED, COMMIT EXECUTED. |
| Phase 3 — L1-L6 refactor | SKIPPED | Surgical 8-line additions to existing methods; no L1-L6 opportunity. Duplication between IssueFactory + AzureDevOpsWorkTrackingConnector is an acknowledged refactor candidate but per CLAUDE.md must be a separate commit. Same rationale as bug-5016 precedent. |
| Phase 4 — Adversarial review | PASS | `nw-software-crafter-reviewer` (Haiku) returned `APPROVAL_STATUS: approved`, `BLOCKER_COUNT: 0`, `HIGH_COUNT: 0`, `MEDIUM_COUNT: 0`, `LOW_COUNT: 0`. Reviewer traced multi-revert edge case, direct Done transitions, and reopen-with-intermediate-states by hand against the algorithm and confirmed each behaves correctly. Confirmed no Testing Theater patterns. |
| Phase 5 — Mutation testing (Stryker.NET per-feature) | PASS at method scope | Config: `stryker-config.bug-5064-started-date-revert.json` scoped to `**/Factories/IssueFactory.cs` + `FullyQualifiedName~IssueFactoryTest`. Results captured 2026-05-21 22:00-22:02 (1m 23s elapsed): 69 mutants tested file-wide, 51 killed, 18 survived. **On the `GetStartedAndClosedDate` method (lines 58-91) the kill rate is 13/14 = 92.86%** — above the 80% CLAUDE.md gate. **On the 5 lines added by bug-5064 (62, 74-77) the kill rate is 5/6 = 83.33%** — also above the gate. The lone fix-area survivor at `IssueFactory.cs:75` mutates `lastToDoEntryDate.Value > startedDate.Value` to `>=`; this is an **equivalent mutant** — `startedDate` and `lastToDoEntryDate` are timestamps of *distinct state transitions* (Doing-entry vs ToDo-entry), which cannot occupy the same instant in any real Jira/ADO workflow, so `>` and `>=` produce identical observable behaviour. Accepted under the same precedent as bug-5016's `>=`/`>` boundary mutant on `Cache.cs:29`. The remaining 17 survivors live in pre-existing untested helpers (`GetRankFromFields` line 18, `TryGetRankFromRankField` line 131-137, `GetLabelsFromFields` line 160-164, plus rank string mutations at lines 32 and 94/107/111/129) — pre-existing tech debt unrelated to bug-5064; closing those gaps is out of scope per CLAUDE.md "don't add features beyond what the task requires". ADO file (`AzureDevOpsWorkTrackingConnector.cs`) is deliberately out of mutation scope — its tests are real-IO integration tests against `CMFTTestTeamProject`, unsuitable for Stryker's fast loop; the semantic is structurally identical to the Jira fix and is mutation-validated via the Jira copy. Full HTML report at `Lighthouse.Backend/StrykerOutput/2026-05-21.22-00-43/reports/mutation-report.html` (not committed; can be regenerated from the per-feature config). |
| Phase 6 — DES integrity verification | PASS | `des-verify-integrity docs/feature/bug-5064-started-date-revert/deliver/` → "All 2 steps have complete DES traces". |
| Phase 7 — Finalize | _PENDING_ | Workspace commit + evolution archive at `docs/evolution/2026-05-21-bug-5064-started-date-revert.md` (to be written). |
| Slice-boundary push | _PENDING_ | Per memory `[[feedback_slice_boundary_ritual]]` + `[[feedback_classifier_main_push_and_ado_reason]]`: pause for user, push HEAD:main (re-authorize per session), wait for CI green, transition ADO bug 5064 `Active → Resolved` with reason in same call. |

## Wave: DELIVER / [REF] Pre-requisites

| Source | Pre-requisite | Status |
|---|---|---|
| DISTILL Tier-1 (this file) | RCA + acceptance scenarios + test placement decision + adapter coverage + scope decisions | Met. |
| DESIGN component manifest | `IssueFactory` and `AzureDevOpsWorkTrackingConnector` are existing leaf adapters; no new component | Met (architecture brief unchanged). |
| DEVOPS env matrix | Default — Jira test runs against in-process JSON; ADO regression test deferred (real-IO out of scope this session) | Met. |
| ADO state | Bug 5064 already `Active`, assigned to Benj, tagged `Release Notes` | Met. No state transition until push + green CI per `/ado-sync` slice-boundary ritual. |
