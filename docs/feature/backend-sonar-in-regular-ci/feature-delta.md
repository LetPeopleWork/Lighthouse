# Feature: backend-sonar-in-regular-ci

> Single narrative file (per nWave layout). DISCUSS wave only so far.
> Infrastructure-only feature (Decision 4 = No / escape valve): no user-facing
> behavior, JTBD/journey/persona phases skipped by design. Stories carry
> `job_id: infrastructure-only` with rationale.

---

## Wave: DISCUSS / [REF] Persona ID

`lighthouse-maintainer` — core contributor who builds, finalizes, and releases.
Values low-ceremony automation: the right outcome should be the cheap default.
This CI change serves that persona by making the backend quality gate a normal,
always-on part of CI instead of a separate scheduled job that drifts out of sight.

No end-user persona applies — this is pipeline plumbing.

## Wave: DISCUSS / [REF] JTBD one-liner

N/A — infrastructure-only. **Rationale:** the change is internal CI mechanics
(when/where SonarCloud backend analysis runs). No user-visible product behavior
changes; no job in `docs/product/jobs.yaml` is served or altered.

## Wave: DISCUSS / [REF] Pre-requisites

- Existing workflows: `ci.yml` (orchestrator), `ci_backend.yml` (reusable backend
  verify, already accepts `run_sonar`), `ci_sonar_gates.yml` (gate verifier),
  `nightly.yml` (scheduled full backend scan — to be removed).
- `SONAR_TOKEN` secret already wired into both backend and gate workflows.
- No code, schema, or contract changes. YAML-only.

## Wave: DISCUSS / [REF] Current vs Target (problem statement)

**Current state:**
- Backend SonarCloud scan runs on **PRs only** (`run_sonar: github.event_name == 'pull_request'`).
- Pushes to `main` **do not scan**; `ci_sonar_gates.yml` instead polls the SonarCloud
  *Quality Gate API* (`backend-status-check`) to assert the *last* analysis on `main`
  is still green.
- A separate **`nightly.yml`** runs a full backend scan daily at 00:00 to refresh
  that last-analysis state.

**Problem:** the backend gate on `main` is asserted against a *stale* prior analysis,
not the code being pushed. Freshness depends on the nightly cron. This is exactly the
"silent drift, fixed by a periodic clean-up instead of a gate at the moment of change"
anti-pattern the maintainer persona is allergic to.

**Target state:**
- Backend SonarCloud scan runs as part of **regular CI** every time the backend job
  runs (PRs **and** pushes to `main`) — `run_sonar` always true.
- `ci_sonar_gates.yml` always checks against the **freshly created** quality-gate
  report (`report-task.txt`) — the API status-check branch is removed, since a fresh
  scan report now always exists.
- `nightly.yml` is **deleted** — its only purpose (a periodic full scan to keep the
  last analysis green) is now redundant.

## Wave: DISCUSS / [REF] Locked decisions

- **[D1]** Backend Sonar scan runs on every backend CI run (PR + push to main), not
  PR-only. Rationale: gate must evaluate the code being pushed, not a prior analysis.
- **[D2]** `ci_sonar_gates.yml` `backend-status-check` input + "Verify Backend Quality
  Gate (no scan, status check)" step are **removed**. Since the scan always runs, the
  gate always has a fresh `report-task.txt` to wait on via `sonarqube-quality-gate-action`.
- **[D3]** `backend-ran` in the orchestrator becomes `needs.backend.result == 'success'`
  (drop the `&& event == 'pull_request'` qualifier).
- **[D4]** `nightly.yml` is deleted, not just disabled. Manual full runs remain available
  via `ci.yml`'s existing `workflow_dispatch`.
- **[D5]** Accepted tradeoff: pushes to `main` now run coverage collection + sonarscanner,
  adding a few minutes per main push. This is the explicit "always run" intent ("go back to").

## Wave: DISCUSS / [REF] User stories

### Story 1 — Backend Sonar scan always runs in regular CI `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` Changes when the SonarCloud backend analysis executes
within CI; no user-facing surface. Internal pipeline behavior only.

As a Lighthouse maintainer, the backend SonarCloud analysis runs on every backend CI
run — PRs and pushes to `main` — so the quality gate always reflects the code just pushed.

### Elevator Pitch
Before: backend Sonar scans only on PRs; `main` pushes assert a stale prior analysis via API.
After: every CI run on a backend change runs the backend scan; the run's SonarCloud
analysis appears on the PR/commit in SonarCloud and the gate evaluates *that* analysis.
Decision enabled: maintainer trusts the green/red backend gate on `main` as reflecting
HEAD, with no dependency on a nightly cron to refresh it.

**Acceptance criteria**
- AC1.1 — In `ci.yml`, the `backend` job passes `run_sonar: true` (no event conditional).
- AC1.2 — `ci_backend.yml` runs the begin/end sonarscanner steps and the with-coverage
  test path on both `pull_request` and `push` to `main`.
- AC1.3 — A backend push to `main` produces a fresh SonarCloud analysis (a
  `sonar-report-backend` artifact with `report-task.txt` is uploaded).

### Story 2 — Gate always verifies the freshly created quality gate `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` Internal gate-verification mechanics; no user surface.

As a Lighthouse maintainer, the sonar-gates job always waits on the fresh backend
quality-gate report, so there is one gate code-path regardless of trigger.

### Elevator Pitch
Before: `main` pushes go through a separate API status-check branch reading the last
analysis; PRs wait on the fresh report — two divergent paths.
After: one path — `sonar-gates` downloads the `sonar-report-backend` artifact and waits
on `sonarqube-quality-gate-action` for the fresh report on every trigger.
Decision enabled: maintainer reasons about a single gate behavior; a red HEAD fails the
same way on PR and on main.

**Acceptance criteria**
- AC2.1 — `ci_sonar_gates.yml` no longer declares the `backend-status-check` input.
- AC2.2 — The "Verify Backend Quality Gate (no scan, status check)" curl/jq step is removed.
- AC2.3 — The job `if:` condition drops the `|| inputs.backend-status-check` term.
- AC2.4 — `ci.yml` `sonar-gates` call passes `backend-ran: needs.backend.result == 'success'`
  and no longer passes `backend-status-check`.
- AC2.5 — On a green backend push to `main`, the gate passes by waiting on the fresh report;
  on a red one it fails the same job.

### Story 3 — Nightly backend workflow removed `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` Deletes a scheduled CI workflow; no user surface.

As a Lighthouse maintainer, the redundant nightly backend scan is gone, so there is no
duplicate, drift-masking scheduled scan to maintain.

### Elevator Pitch
Before: `.github/workflows/nightly.yml` runs a full backend scan daily to refresh the
last-analysis status that the main-push gate relied on.
After: file deleted; refresh is no longer needed because every main push scans.
Decision enabled: maintainer has one fewer workflow to reason about; no stale-cron risk.

**Acceptance criteria**
- AC3.1 — `.github/workflows/nightly.yml` is deleted.
- AC3.2 — No remaining workflow references `nightly` (grep clean except removed file).
- AC3.3 — Stale comments in `ci.yml` / `ci_backend.yml` referencing "the nightly run does a
  full scan" / "run the nightly to re-analyse" are removed or corrected.

## Wave: DISCUSS / [REF] Definition of Done

1. Backend Sonar scan runs on PR and push-to-main (Story 1 ACs).
2. Single gate path against fresh report; status-check branch gone (Story 2 ACs).
3. `nightly.yml` deleted; no dangling references (Story 3 ACs).
4. Stale comments updated.
5. `actionlint` (or equivalent YAML/workflow lint) clean on changed workflows.
6. A push to `main` exercises the new path green end-to-end (observed in Actions).
7. `docs/ci-learnings.md` consulted; no rule re-introduced.
8. No new SonarCloud issues introduced by the YAML change (N/A — workflow files excluded
   from scan, but verify no analysis-config regression).
9. Changes committed conventionally (`ci:` scope) per repo convention.

## Wave: DISCUSS / [REF] Out-of-scope

- Frontend Sonar flow — unchanged.
- Quality-gate *definition* on SonarCloud (thresholds/conditions) — unchanged; we check
  against the existing project gate, we do not redefine it.
- Forcing full integration-test coverage on main. Nightly forced `connector_shared: true`
  (all integration tests) for a full-coverage baseline. Regular CI uses the per-connector
  diff selection. **Risk note:** the "full integration coverage" baseline nightly produced
  is dropped. The PR gate evaluates *new-code* coverage from the PR's own tests, so the
  gate stays valid; but absolute project coverage may reflect a narrower test subset on a
  given main push. If a periodic full-coverage baseline is later wanted, that is a separate
  follow-up — not this change.
- Test-selection logic in `ci_backend.yml` — unchanged.

## Wave: DISCUSS / [REF] WS strategy

N/A (Strategy: none — single thin infra change, not a walking skeleton).

## Wave: DISCUSS / [REF] Driving ports

GitHub Actions workflow triggers (`pull_request`, `push` to main, `workflow_dispatch`) —
no application-level ports touched.

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions
- [D1] Always scan backend in regular CI — gate must reflect HEAD (see: ci.yml backend job).
- [D2] Remove status-check API branch — fresh report always present (see: ci_sonar_gates.yml).
- [D4] Delete nightly.yml — redundant once main always scans (see: nightly.yml).

### Requirements Summary
- Primary need: backend SonarCloud quality gate evaluates the pushed code on every CI run,
  with a single gate path, and no scheduled nightly scan.
- Walking skeleton scope: N/A.
- Feature type: Infrastructure (CI/CD).

### Constraints Established
- YAML-only; no code/schema/contract change.
- `SONAR_TOKEN` already available to both jobs.
- Accepted: slower main pushes (coverage + scan).

### Upstream Changes
- None — no DISCOVER/DIVERGE artifacts for this feature.

## Wave: DISCUSS / [REF] Handoff

To DESIGN (trivial — likely a single ADR or direct-to-DELIVER given pure-YAML scope) +
DEVOPS. Given the change is 4 files and mechanical, recommend fast-forwarding DESIGN to a
one-line "no architectural decision; YAML edits per ACs" and proceeding to DELIVER.
