# Slice 01 — Always-scan backend, single gate path, drop nightly

**Goal:** Backend SonarCloud analysis runs on every backend CI run and the gate always
verifies the fresh report; the nightly scan is removed.

## IN scope
- `ci.yml`: `backend` job `run_sonar: true`; `sonar-gates` `backend-ran: needs.backend.result == 'success'`; remove `backend-status-check` arg + stale comments.
- `ci_sonar_gates.yml`: remove `backend-status-check` input, its `if:` term, and the API status-check step.
- `nightly.yml`: delete.
- Fix stale "nightly" comments in `ci.yml` / `ci_backend.yml`.

## OUT scope
- Frontend Sonar flow, gate threshold definitions, test-selection logic, full-integration coverage baseline.

## Learning hypothesis
- Confirms if green: a backend push to `main` runs the scan and passes the gate on the
  fresh report with no nightly dependency.
- Disproves if it fails: assumption that the existing `run_sonar` plumbing in
  `ci_backend.yml` works identically on `push` as on `pull_request` (e.g. artifact
  upload/download or `fetch-depth` differences).

## Acceptance criteria
- All Story 1/2/3 ACs in `feature-delta.md`.
- Production data: a real push to `main` (not synthetic) is observed green in Actions,
  with the backend analysis visible in SonarCloud and the gate waiting on the fresh report.

## Dependencies
- None. `SONAR_TOKEN` already wired.

## Effort estimate
- ~1 hour (YAML edits + one CI observation cycle). Single thin slice; ships in one day.

## Reference class
- Prior Sonar/CI wiring changes in this repo (PR #5362-era CI edits).

## Pre-slice SPIKE
- Not needed — mechanism already exists (`run_sonar` path), low uncertainty.
