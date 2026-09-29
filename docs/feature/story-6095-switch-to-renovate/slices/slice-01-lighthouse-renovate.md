# Slice 01 — lighthouse-renovate

**Goal:** the Lighthouse repo gets its dependency updates from Renovate under the same auto-merge rules
Dependabot has today, with a 7-day release age and a hand-merged toolchain PR.

**Stories:** US-01, US-02 (see `../feature-delta.md`).

## IN
- `renovate.json` at the repo root: `config:recommended`, `Europe/Zurich`, Dependency Dashboard,
  `minimumReleaseAge: 7 days`, `platformAutomerge: true`, auto-merge for every update type and for
  vulnerability alerts, the carried-over MUI ignores, a `deps` commit form, and merge commits.
- A toolchain package rule: `.nvmrc` + both `packageManager` + both `engines.node` in one group, `automerge: false`.
- Keeping the SHA-pinned Actions updatable (C1, mechanism chosen in DESIGN).
- Deleting `.github/dependabot.yml`, `.github/workflows/dependabot-automerge.yaml` and the Dependabot paragraph in `.github/actions/README.md`, and resolving the `dependabot[bot]` guards (C2).
- Repo settings: Dependabot security updates off, alerts on. Delete `DEPENDABOT_GITHUB_TOKEN` and the Dependabot secret set.

## OUT
Grouping or schedules, lock-file maintenance, MUI pin changes, lighthouse-platform.

## Learning hypothesis
- Disproves "Renovate is a drop-in replacement here" if bare-SHA Actions can't be updated, if a
  lockfile comes back written by the wrong pnpm, or if a main-only job changes behaviour after a Renovate merge.
- Confirms it when the first run's PRs merge on green checks and main parks at Release as before.

## Acceptance
AC-01.1 … AC-01.10 and AC-02.1 … AC-02.3, all checked against the first live Renovate run on production dependencies.

## Dependencies
The Renovate app's repository selection includes `LetPeopleWork/Lighthouse` (maintainer action).

## Effort / reference class
About half a day plus a one-week observation, like #6088 (one config file, one live run).
Optional pre-slice spike: run the Renovate CLI locally (`--platform=local` or a dry run) to see how it reads bare SHA pins before committing.
