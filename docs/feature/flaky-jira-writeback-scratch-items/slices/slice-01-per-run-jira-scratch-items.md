# Slice 01 — Per-run Jira scratch items

**Goal**: Give `JiraWriteBackTest` its own throwaway Epic + Story per fixture run (created in `[OneTimeSetUp]`, deleted in `[OneTimeTearDown]`) so full-suite runs stop colliding on the shared `LGHTHSDMO-1`/`LGHTHSDMO-16` items.

**Classification**: `@infrastructure` — `job_id: infrastructure-only`. Test-suite reliability; no production code, no user surface.

## IN scope
- Add `[OneTimeSetUp]` to `JiraWriteBackTest` that creates a fresh Epic + Story (+ 2nd Story if a test needs two distinct items) via Jira REST `POST /rest/api/2/issue`, tracking created keys in a list.
- Replace fixed constants `EpicId = "LGHTHSDMO-1"` / `StoryId = "LGHTHSDMO-16"` with the created keys.
- Add `[OneTimeTearDown]` that hard-deletes each created issue via `DELETE /rest/api/2/issue/{key}`, best-effort (try/catch, log via `TestContext.Progress.WriteLine`), then clears the list.
- Skip-safe: if `JiraLighthouseIntegrationTestToken` unset, setup returns early; non-integration test still passes.
- Update `docs/ci-learnings.md` `JiraWriteBackTest` flakiness note.

## OUT scope
- Any `JiraWorkTrackingConnector` production change.
- ADO write-back test (already fixed by `2964383c`).
- Other live-Jira flakiness classes (rate-limit / Jira incident).
- Parallelism/config changes.

## Learning hypothesis
- **Disproves if it fails**: "the ADO per-run scratch-item pattern ports cleanly to Jira." Failure mode = Jira PAT can't delete (→ transition fallback) OR created Epic/Story lack the `Delivery Date`/`Age` custom fields the tests write (→ adjust field set or seed fields).
- **Confirms if it succeeds**: full suite runs green twice back-to-back with distinct scratch keys and zero board residue.

## Acceptance criteria
- AC1–AC6 from `feature-delta.md` Story A. Key gate: two consecutive full-suite runs green with distinct scratch keys, no `Lighthouse WriteBack scratch *` residue on the board.

## Dependencies
- Reference: ADO commit `2964383c`.
- `JiraLighthouseIntegrationTestToken`, `LGHTHSDMO` project.
- DESIGN-gated: (1) PAT delete permission; (2) custom-field availability on created issues.

## Effort estimate
≤½ day. Reference class: ADO `2964383c` = 161-line test-only diff, one file.

## Pre-slice SPIKE
Low-cost DESIGN check (not a full spike): one manual `POST` + `DELETE` against `LGHTHSDMO` with the integration token to confirm create/delete permission and that a new Epic/Story exposes `customfield_10205`/`customfield_10206`. If delete is denied → switch D3 to transition-to-terminal.

## Taste tests
- Ships <4 new components: ✅ one test file edited.
- No new abstraction shipped first: ✅ direct REST calls in the test, like ADO.
- Disproves a pre-commitment: ✅ "ADO pattern ports to Jira."
- Not synthetic-only: ✅ hits real Jira Cloud (`LGHTHSDMO`).
- Not a scale-duplicate of another slice: ✅ only slice.
