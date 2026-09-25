# Story #6088 — Automatic dependency updates for lighthouse-clients

## Wave: DISCUSS / [REF] Persona and Job

- Persona: `lighthouse-maintainer`
- Job: `job-maintainer-keep-shipped-clients-patched-without-toil` (docs/product/jobs.yaml)
- One-liner: when a dependency of the clients ships a fix, get it proposed and merged without touching it — but only once the version has been public for 7 days.

## Wave: DISCUSS / [REF] Locked Decisions

- **D1 — Renovate, hosted app.** Already used by lighthouse-platform; the repo was added to the org installation on 2026-09-25.
- **D2 — 7-day minimum release age for every update**, so a compromised release has time to be caught and pulled before it lands. User: "wait for 5-7 days after updating something to prevent supply chain attacks".
- **D3 — Patch and minor updates auto-merge once CI is green; majors open as PRs and wait.** Renovate does the merge itself (`platformAutomerge: false`): the repo has no branch protection, so GitHub's native auto-merge has nothing to wait on, while Renovate checks the PR's status itself.
- **D4 — Security fixes open immediately and never auto-merge.** They skip the age rule by Renovate's default, so a human decides.
- **D5 — No changesets on update PRs.** User: "we could skip the changeset, this is not a hard requirement. if needed it can easily be done by hand". Updates ship with the next release of the affected package.
- **D6 — Release workflow unchanged.** A Renovate merge to main queues a release run that waits at the Release approval gate like any other push. User: "it's a gate anyway that needs approval from a human. so it's a no-op".

## Wave: DISCUSS / [REF] Out of Scope

Explicitly dropped by the user ("I just want automatic pr's for dependency updates. dont make this more complicated than it is"): branch ruleset, gating the release job on npm state, SHA/digest pinning, building the mcp-http image from the lockfile, image smoke test in `verify`, pnpm-level age setting for transitive dependencies.

## Wave: DISCUSS / [REF] User Stories

### US-01 — Updates arrive as PRs and merge themselves once green

Elevator Pitch
Before: dependencies in lighthouse-clients only move when the maintainer bumps them by hand.
After: open the repo's PR list → sees `renovate[bot]` PRs for patch/minor updates at least 7 days old, which merge themselves after `Client CI / verify` passes.
Decision enabled: none needed for routine updates — the maintainer only looks at what did not merge.

- AC-01.1 `renovate.json` exists on main and passes `renovate-config-validator`.
- AC-01.2 No PR is opened for a version published less than 7 days ago (it appears as pending on the Dependency Dashboard).
- AC-01.3 A patch/minor PR with a green `verify` check is merged by Renovate without human action.
- AC-01.4 A patch/minor PR with a red `verify` check stays open.

### US-02 — Majors and security fixes wait for the maintainer

Elevator Pitch
Before: no signal at all that a major version or a security fix is available.
After: open the PR list → sees major-update PRs and `security`-labelled PRs that stay open until merged by hand.
Decision enabled: whether and when to take a breaking upgrade or an urgent fix.

- AC-02.1 A major-update PR is never auto-merged, even when green.
- AC-02.2 A vulnerability-alert PR carries the `security` label and is never auto-merged.

## Wave: DISCUSS / [REF] Outcome KPIs

- KPI-1: open patch/minor Renovate PRs older than 2 days with green CI — target 0 (`gh pr list -R LetPeopleWork/lighthouse-clients --author app/renovate`).
- KPI-2: auto-merged major updates — target 0.

## Wave: DISCUSS / [REF] Checklist

- RBAC impact: N/A — no Lighthouse server or UI change.
- Lighthouse-Clients CLI/MCP versioning: no automatic version bump; runtime updates ship with the next release, or add a changeset by hand when a fix must go out sooner (documented in lighthouse-clients `docs/release-model.md`).
- Website marketing surface: N/A — internal tooling, nothing user-facing.

## Wave: DELIVER / [REF] Delivered

- lighthouse-clients `renovate.json` (validated with `renovate-config-validator`) and a "Dependency Updates" section in `docs/release-model.md`.
- DESIGN, DEVOPS and DISTILL skipped at the user's instruction: a single config file with no code to design or acceptance-test beyond observing the first Renovate PRs.
- Verification: observe the first Renovate run — Dependency Dashboard issue created, first patch/minor PR merged by Renovate after green CI (AC-01.3). If Renovate does not merge on its own, fall back to GitHub-native auto-merge: a ruleset on main requiring `verify`, "Allow auto-merge" in the repo settings, and `platformAutomerge: true` (without the required check, native auto-merge has nothing to wait for).
