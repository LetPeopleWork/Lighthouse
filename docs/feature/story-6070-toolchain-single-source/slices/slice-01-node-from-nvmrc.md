# Slice 01 — Node from `.nvmrc` everywhere

**Goal:** Every CI job, the Docker frontend stage and the maintainer's shell take Node from `.nvmrc`,
and CI rejects any literal that comes back.

## IN scope
- Local first: `fnm` installed and wired into fish (`--use-on-cd`), verified in interactive and
  non-interactive shells (Stories 2, AC2.1–2.2). Installing needs `sudo`, so the maintainer runs it.
- `.nvmrc` = `24`; `.nvmrc` added to `ci.yml` `push.paths`.
- 12 `node-version:` → `node-version-file: '.nvmrc'` (11 workflows + `build-frontend/action.yml`).
- `Dockerfile` `ARG NODE_VERSION` (no default) + `ci_docker.yml` `--build-arg`.
- `engines.node: "24.x"` + engine-strict in both pnpm projects.
- Guard test, Node rules of D9.
- Probe branch `features/6070-node-probe`.

## OUT scope
- pnpm (slice 02), Node 26, `dotnet-version`.

## Learning hypothesis
- Confirms if green: `node-version-file` resolves identically in reusable workflows and in the
  composite action (checkout path assumptions hold), and a templated `FROM` builds on both platforms.
- Disproves if it fails: that every job checks out the repo *before* `setup-node` — a job that sets
  up Node before checkout has no `.nvmrc` to read. Also disproves that fish's `--use-on-cd` hook fires
  in non-interactive shells.

## Acceptance criteria
- Story 1 (AC1.1–1.7), Story 2 (AC2.1–2.6), Story 4 Node rules.
- Production data: the real probe run and the real first `main` run, URLs recorded.

## Dependencies
- None upstream. D4 (fnm) before D3 (strict engines) within the slice.

## Effort estimate
- ~3–4 h including two CI observation cycles.

## Reference class
- `backend-sonar-in-regular-ci` (single YAML slice, one CI observation); Bug #6068 fix cycle.

## Pre-slice SPIKE
- Not needed. Step order already checked in DISCUSS (2026-09-25): every `ci_*.yml` runs
  `actions/checkout` before `actions/setup-node`, so the checkout half of the hypothesis is expected
  to hold. The composite action's callers still need checking.
