# Feature: story-6070-toolchain-single-source

> ADO Story #6070 — "Pin the Node version in one place so local matches CI".
> Scope widened in DISCUSS (user decision, 2026-09-25) to the pnpm version too: same bug class,
> same files. Infrastructure-only (Decision 4 = No / escape valve): no product surface changes;
> stories carry `job_id: infrastructure-only` with rationale.

---

## Wave: DISCUSS / [REF] Persona ID

`lighthouse-maintainer` (`docs/product/personas/lighthouse-maintainer.yaml`) — builds, verifies and
releases Lighthouse; trunk-based on `main`; runs a newer Node locally (v26.8.2, system package, no
version manager) than CI does (24). Wants "green locally" to mean "green in CI".

No end-user persona applies.

## Wave: DISCUSS / [REF] JTBD one-liner

N/A — infrastructure-only. **Rationale:** the change decides which Node and pnpm binaries build and
test Lighthouse. No product behaviour, API, or UI changes; no job in `docs/product/jobs.yaml` is
served or altered. The maintainer-side job, stated for traceability: *when I verify a change
locally, I want the same toolchain CI uses, so a local green is evidence of a CI green.*

## Wave: DISCUSS / [REF] Pre-requisites

- `.github/workflows/ci_workflow-scripts.yml` already runs `node --test .github/scripts/*.test.mjs`
  as the first job of `ci.yml` — a new `*.test.mjs` there is picked up with no wiring.
- `actions/setup-node` supports `node-version-file`; `pnpm/action-setup` supports
  `package_json_file` (reads `packageManager` when `version:` is omitted).
- `ci.yml` already triggers on `features/**` pushes (used for the probe run).
- No code, schema, or API contract changes. YAML, Dockerfile, `package.json`, one test script.

## Wave: DISCUSS / [REF] Current vs Target (problem statement)

**Node — current** (verified 2026-09-25):
- No `.nvmrc`, no `.node-version`, no `engines` in either `package.json`.
- 12 literal pins under `.github/`: `node-version: '24'` in 10 workflows + `build-frontend/action.yml`,
  and unquoted `node-version: 24` in `ci_docker.yml`.
- `Dockerfile:24` — `FROM --platform=$BUILDPLATFORM node:24-bookworm-slim AS node-builder`, tracked by
  Dependabot's `docker` ecosystem, which can propose a Node major on its own; nothing tracks the
  workflow pins.
- **`ci.yml`'s `push.paths` filter does not list `.nvmrc`** — a commit touching only `.nvmrc` would
  start no CI run at all. (Not in the ADO story; found in DISCUSS.)

**pnpm — current** (verified 2026-09-25; not in the ADO story, added by user decision):
- `Lighthouse.Frontend/package.json` `packageManager: pnpm@10.33.2`; `Lighthouse.EndToEndTests` has none.
- 6 × `pnpm/action-setup` with literal `version: 10.33.2` (`ci_sonar_gates`, `ci_sbom`,
  3 × `ci_package-*-standalone`, `build-frontend/action.yml`).
- 4 × `corepack prepare pnpm@latest --activate` (`ci_e2e`, `ci_verifyauth`, `ci_verifysqlite`,
  `ci_verifypostgres`) — resolves to pnpm 11.x today.
- `Dockerfile:28` — `corepack prepare pnpm@10.12.1` — a third, older version.
- Node 25+ no longer ships corepack, so every corepack-based install breaks on the next Node major.

**Incidents this caused:** Bug #6068 (`node --test <dir>` passed on local Node 26, failed on CI Node 24);
`docs/ci-learnings.md` 2026-06-16 (corepack → pnpm 11 in CI vs pnpm 10.33.2 locally →
`ERR_PNPM_LOCKFILE_CONFIG_MISMATCH`); 2026-09-14 (Dependabot vs CI pnpm versions disagree).

**Target:**
- `.nvmrc` (value `24`) is the only place the Node version is written for CI, Docker, and local use.
- `Lighthouse.Frontend/package.json` `packageManager` is the only place the pnpm version is written;
  `Lighthouse.EndToEndTests/package.json` carries the same value, enforced equal.
- `engines.node` in both `package.json` files, **strict**: `pnpm install` refuses a wrong Node.
- A guard test fails CI if any literal Node/pnpm version reappears or the copies disagree.
- The maintainer's machine switches to the `.nvmrc` version automatically inside the repo.

## Wave: DISCUSS / [REF] Locked decisions

- **[D1] `.nvmrc` = `24`** (floating major). Matches today's behaviour (`'24'` and `node:24-…` both
  float to the newest 24.x). Format constrained to a bare `N[.N[.N]]` so it is also a valid Docker
  tag prefix — no `lts/*`, no `v` prefix. The upgrade to 26 is out of scope.
- **[D2] Dockerfile takes `ARG NODE_VERSION` with no default** (user decision).
  `FROM --platform=$BUILDPLATFORM node:${NODE_VERSION}-bookworm-slim`; `ci_docker.yml` passes
  `--build-arg NODE_VERSION=$(cat .nvmrc)`. A build without the arg fails at `FROM`, loudly.
  Consequence, accepted: Dependabot cannot resolve a templated `FROM`, so it stops proposing Node
  image bumps — that removes the independent drift channel the story names. The `aspnet`/`sdk`
  stages stay Dependabot-tracked. Local `docker build` needs the arg.
- **[D3] `engines.node` is strict** (user decision). Both `package.json` get `"node": "24.x"`
  (derived from `.nvmrc`), and pnpm `engineStrict` is enabled for both projects, so
  `pnpm install` on Node 26 fails with `ERR_PNPM_UNSUPPORTED_ENGINE`.
  Precondition: the maintainer's local version manager (D4) lands **before** strict engines, or
  their own machine stops building.
- **[D4] Local version manager = `fnm`**, set up on the maintainer's machine as part of this story
  (user request: "ensure I have nvm ready locally"). `fnm` reads `.nvmrc`, supports fish natively,
  and switches on `cd` (`--use-on-cd`); `nvm` itself is bash-only. The system Node 26 stays the
  default outside directories that carry a `.nvmrc`. The setup must also work in
  **non-interactive** fish shells, because Claude Code and scripts run `pnpm` there.
- **[D5] SSOT proven by a throwaway `features/6070-*` probe branch** (user decision; one-off exception
  to trunk-based). The probe sets `.nvmrc` (or `packageManager`) to a specific *older* patch; every
  CI job log must print exactly that patch. `'24'` read from a file looks identical to a `'24'`
  literal, so only a distinguishable value proves anything. The Docker job runs only on `main`, so
  its evidence is the `main` push log plus a local `docker build --target node-builder`.
- **[D6] pnpm in scope** (user decision). `packageManager` is the pnpm source of truth; workflows use
  `pnpm/action-setup` with `package_json_file:` and no `version:`.
- **[D7] No corepack anywhere.** The four `corepack prepare pnpm@latest` workflows move to
  `pnpm/action-setup`; the Dockerfile installs pnpm without corepack. Rationale: Node 25+ dropped
  corepack, so leaving it in makes the Node upgrade this story prepares for a multi-file breakage.
- **[D8] One pnpm version for the repo.** `Lighthouse.EndToEndTests/package.json` gains
  `packageManager` equal to the frontend's; the guard enforces equality. See Changed Assumptions —
  this reverses a documented ledger rule.
- **[D9] Guard test** `.github/scripts/toolchain-pins.test.mjs`, run by the existing
  `ci_workflow-scripts` job. It fails, naming file and line, when: any `node-version:` key or
  `pnpm/action-setup` `version:` key exists under `.github/`; any `corepack` or `pnpm@<ver>` literal
  exists under `.github/` or in `Dockerfile`; the Dockerfile's node stage does not use
  `${NODE_VERSION}`; `engines.node` in either `package.json` disagrees with `.nvmrc`'s major; the two
  `packageManager` values differ; `.nvmrc` is missing or malformed; `ci.yml` `push.paths` lacks
  `.nvmrc`. This turns "no literal remains" from a one-off grep into a standing rule.
- **[D10] `.nvmrc` added to `ci.yml` `push.paths`** — required for the story's own AC
  ("editing `.nvmrc` alone changes every CI job").
- **[D11] Honest upgrade cost:** a Node major bump edits `.nvmrc` + two `engines.node` lines, and the
  guard refuses a partial edit. It's one decision in three lines, not literally one line, and
  drifting stays impossible. A pnpm bump edits two `packageManager` lines, also guard-enforced.

## Wave: DISCUSS / [REF] User stories

### Story 1 — CI and Docker take Node from `.nvmrc` `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` Changes which Node binary CI jobs and the Docker build stage run on; no
product surface.

As the Lighthouse maintainer, every CI job and the Docker frontend build resolve Node from `.nvmrc`,
so changing that one file changes the Node every job runs on.

### Elevator Pitch
Before: Node 24 is written in 13 places, one of them Dependabot-bumpable on its own, and nothing
says which Node a developer should use.
After: push `features/6070-node-probe` with `.nvmrc` = an older 24.x patch → every job's
`Setup Node.js` log resolves to `24.<patch>` and `node --version` prints `v24.<patch>`.
Decision enabled: the maintainer can plan the Node 26 upgrade as a single reviewed edit and trust
that no job is left behind.

**Acceptance criteria**
- AC1.1 — `.nvmrc` exists at the repo root containing `24`.
- AC1.2 — All 11 workflows and `build-frontend/action.yml` use `node-version-file: '.nvmrc'`; no
  `node-version:` key remains anywhere under `.github/` (quoted or unquoted).
- AC1.3 — `Dockerfile` node stage is `node:${NODE_VERSION}-bookworm-slim` from an `ARG NODE_VERSION`
  with no default; `ci_docker.yml` passes `--build-arg NODE_VERSION=$(cat .nvmrc)`.
- AC1.4 — `docker build --target node-builder .` without the arg fails; with
  `--build-arg NODE_VERSION=$(cat .nvmrc)` it succeeds.
- AC1.5 — `ci.yml` `push.paths` includes `.nvmrc`.
- AC1.6 — Probe: a `features/6070-node-probe` push changing only `.nvmrc` to a specific older 24.x
  patch starts a CI run in which every job that sets up Node reports exactly that patch. Run URL
  recorded in this file; branch deleted afterwards.
- AC1.7 — The first `main` run after merge shows `--build-arg NODE_VERSION=24` and a
  `node:24-bookworm-slim` pull in `Build and Push Docker Image`, and is green.

### Story 2 — The repo refuses the wrong Node locally, and the maintainer's machine picks the right one `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` Local developer-toolchain enforcement; no product surface.

As the Lighthouse maintainer, entering the repo switches me to the CI Node version, and installing
dependencies on any other version fails, so a local green cannot come from a different runtime.

### Elevator Pitch
Before: `node --version` in the repo prints `v26.8.2`; `pnpm install` and `pnpm test` pass on a Node
CI never runs (Bug #6068).
After: `cd /storage/repos/Lighthouse && node --version` → `v24.x`; on Node 26,
`pnpm install` in `Lighthouse.Frontend` → `ERR_PNPM_UNSUPPORTED_ENGINE … wanted {"node":"24.x"}`.
Decision enabled: the maintainer treats a local green as evidence of a CI green, and skips the
"reproduce in a node:24 container" step `docs/ci-learnings.md` prescribes today.

**Acceptance criteria**
- AC2.1 — `fnm` is installed on the maintainer's machine; fish config activates it with
  `--use-on-cd`; `fnm install` at the repo root has fetched the `.nvmrc` version.
- AC2.2 — In the repo, both an interactive fish shell and a non-interactive one (as Claude Code's
  Bash tool runs) resolve `node --version` to `v24.x`; outside any `.nvmrc` directory it stays `v26.8.2`.
- AC2.3 — `Lighthouse.Frontend/package.json` and `Lighthouse.EndToEndTests/package.json` declare
  `"engines": { "node": "24.x" }`.
- AC2.4 — pnpm engine-strict is on for both projects: `pnpm install` under Node 26 fails with
  `ERR_PNPM_UNSUPPORTED_ENGINE`; under Node 24 it succeeds.
- AC2.5 — `pnpm test` and `pnpm build` (frontend) pass locally under the `.nvmrc` Node.
- AC2.6 — Story 2's local setup (AC2.1–AC2.2) is done and verified before AC2.4 is committed.

### Story 3 — CI, Docker and Dependabot take pnpm from `packageManager` `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` Changes which pnpm binary installs dependencies in CI and Docker; no
product surface.

As the Lighthouse maintainer, every pnpm install — CI, Docker, Dependabot, my machine — uses the
version named in `packageManager`, so no job silently runs pnpm 11 against a lockfile pnpm 10 wrote.

### Elevator Pitch
Before: three pnpm versions in play (10.33.2 pinned, `@latest` = 11.x via corepack, 10.12.1 in
Docker); lockfile mismatches have reached `main` twice (ci-learnings 2026-06-16, 2026-09-14).
After: push `features/6070-pnpm-probe` with both `packageManager` fields set to an older 10.x patch →
every job's `pnpm --version` / action-setup log prints that patch, and frozen installs are green.
Decision enabled: the maintainer bumps pnpm by editing `packageManager`, knowing Dependabot, CI and
Docker all follow.

**Acceptance criteria**
- AC3.1 — `Lighthouse.EndToEndTests/package.json` has `packageManager` equal to the frontend's.
- AC3.2 — Every `pnpm/action-setup` step sets `package_json_file:` to the project it installs and has
  no `version:` key.
- AC3.3 — The four `corepack prepare pnpm@latest` steps are replaced by `pnpm/action-setup`; no
  `corepack` string remains under `.github/`.
- AC3.4 — The Dockerfile installs the `packageManager` pnpm version without corepack and without a
  literal version.
- AC3.5 — The E2E lockfile installs `--frozen-lockfile` under the pinned version (regenerated under
  it if the current file was written by pnpm 11).
- AC3.6 — Probe: a `features/6070-pnpm-probe` push changing only the two `packageManager` values
  starts a CI run in which every job that installs pnpm reports exactly that version. Run URL
  recorded here; branch deleted afterwards.
- AC3.7 — `docs/ci-learnings.md` 2026-09-14 entry gets an appended note that its "E2E deliberately
  not pinned" rule no longer holds, and why (see Changed Assumptions).

### Story 4 — A guard keeps the single source single `@infrastructure`

`job_id: infrastructure-only`
`infrastructure_rationale:` CI-script test over repository files; no product surface.

As the Lighthouse maintainer, CI fails the moment someone writes a literal Node or pnpm version back
into a workflow, or edits `.nvmrc` without `engines`, so the single source cannot quietly split again.

### Elevator Pitch
Before: nothing stops a new workflow from adding `node-version: '24'`; the 13 pins grew one at a time.
After: `node --test .github/scripts/toolchain-pins.test.mjs` → passes on the clean tree; with a stray
`node-version: '24'` added to any workflow it fails naming that file and line.
Decision enabled: the maintainer (or a reviewer, or Dependabot's PR) sees a drift at the commit that
introduces it, not at the red build it eventually causes.

**Acceptance criteria**
- AC4.1 — The test exists and enforces every rule in [D9].
- AC4.2 — Each rule has a test case that fails on a seeded violation (a fixture tree, not the real
  repo mutated) and passes on the compliant form.
- AC4.3 — It runs in the existing `Verify Workflow Scripts` job with no workflow change.
- AC4.4 — Story 1 and Story 3 guard rules land in the same slice as the change they protect.

## Wave: DISCUSS / [REF] Story map & slices

Backbone: *declare the version* → *CI reads it* → *Docker reads it* → *local reads it* → *guard keeps it*.

| Slice | Stories | Brief |
|---|---|---|
| 01 — Node from `.nvmrc` everywhere | 1, 2, 4 (Node rules) | `slices/slice-01-node-from-nvmrc.md` |
| 02 — pnpm from `packageManager` everywhere | 3, 4 (pnpm rules) | `slices/slice-02-pnpm-from-packagemanager.md` |

Order: 01 then 02. Slice 01 is the story as filed and removes the next Bug #6068. Slice 02 depends on
it: `pnpm/action-setup` + a pinned Node is what makes dropping corepack safe. Each slice ships and
proves itself on its own probe branch.

Walking skeleton: N/A — change to an existing pipeline, no new end-to-end path.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Baseline (2026-09-25) | Target | Measured by |
|---|---|---|---|
| Literal Node version pins in repo (excluding `.nvmrc`, `engines`) | 13 | 0 | guard test (D9) |
| Literal / floating pnpm versions (excluding `packageManager`) | 11 (6 pinned, 4 `@latest`, 1 Docker) | 0 | guard test (D9) |
| Distinct pnpm versions resolved across CI jobs in one run | 3 | 1 | probe run logs (AC3.6) |
| Files to edit for a Node major bump | 13, unenforced | 3, guard-enforced | D11 |
| Red `main` builds caused by local/CI toolchain mismatch | 3 recorded (#6068, 2026-06-16, 2026-09-14) | 0 in the 90 days after release | `docs/ci-learnings.md` entries |

## Wave: DISCUSS / [REF] Definition of Done

1. All Story 1–4 ACs met, including both probe runs, with run URLs recorded here.
2. Probe branches deleted.
3. `ci.yml` green on `main` after merge, including `Build and Push Docker Image`.
4. Guard test green on the tree and proven red on seeded violations.
5. Maintainer machine: `fnm` working in interactive and non-interactive fish (AC2.1–AC2.2).
6. `docs/ci-learnings.md` consulted before editing; 2026-06-16 repro instruction and the
   2026-09-14 E2E rule updated to reflect the new state.
7. Contributor-facing docs (README / CONTRIBUTING, if either names a Node setup) say
   "use the version in `.nvmrc`", not a number.
8. Conventional commits, `ci:` / `build:` scopes; ADO #6070 kept in sync per `/ado-sync`.
9. No new SonarCloud issues (the guard script is `.mjs` under `.github/scripts/`; check whether Sonar
   analyses it and keep it clean if so).

## Wave: DISCUSS / [REF] Out-of-scope

- Upgrading to Node 26 (Active LTS only from 2026-10-28). This story makes that upgrade a three-line,
  guard-checked change.
- Upgrading pnpm to 11.
- `dotnet-version` pins (`'10'` in 4 places) — same pattern, different toolchain; `global.json` would
  be the analogue. Candidate follow-up, not raised in ADO yet.
- Pinning Docker base images by digest.
- The `tools/codesign/actions-runner/_work/` copy of the workflows — it's a runner working
  directory, not source.

## Wave: DISCUSS / [REF] WS strategy

N/A — no walking skeleton; two thin slices on an existing pipeline.

## Wave: DISCUSS / [REF] Driving ports

- GitHub Actions triggers: `push` (`main`, `features/**`), `pull_request`, `workflow_dispatch`.
- `docker buildx build` in `ci_docker.yml` (build-arg).
- Developer shell: `cd` into the repo (fnm), `pnpm install` (engine-strict).
- `node --test .github/scripts/*.test.mjs` (guard).

## Wave: DISCUSS / [REF] DoR validation

| # | Item | Evidence |
|---|---|---|
| 1 | Problem stated in domain language | Current vs Target; three recorded incidents |
| 2 | Persona identified | `lighthouse-maintainer` |
| 3 | ≥3 concrete examples | Bug #6068; ci-learnings 2026-06-16; 2026-09-14 |
| 4 | Testable ACs | every AC names a command, file or log line |
| 5 | Right-sized | 4 stories, 2 slices, ≤1 day each |
| 6 | Technical notes | D1–D11 |
| 7 | Dependencies | slice 02 after slice 01; D4 before D3 |
| 8 | Job traceability | `infrastructure-only` + rationale on every story |
| 9 | Outcome KPIs | table above |

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions
- [D2] Dockerfile `ARG NODE_VERSION`, no default; removes Dependabot's separate Node channel.
- [D3]+[D4] Strict engines, with `fnm` on the maintainer's machine landing first.
- [D5] Proof by distinguishable-value probe on throwaway `features/` branches.
- [D6]–[D8] pnpm from `packageManager`; no corepack; one pnpm version, E2E included.
- [D9] Guard test makes both single sources permanent.

### Requirements Summary
- Primary need: local Node/pnpm = CI Node/pnpm, from one declared place each, enforced.
- Walking skeleton scope: N/A.
- Feature type: Infrastructure (CI/CD + developer toolchain).

### Constraints Established
- `.nvmrc` must stay a bare numeric version (Docker tag prefix).
- Docker job is `main`-only; its proof comes from the `main` log plus a local targeted build.
- Local enforcement must not break Claude Code's non-interactive shells.

### Scope Assessment: PASS
4 stories, 1 bounded area (build toolchain), 2 slices ≤1 day each.

### Upstream Changes
- ADO #6070 AC do not mention pnpm, the `ci.yml` path filter, or local fnm setup — proposed ADO
  update pending user confirmation.

## Changed Assumptions

- **Source:** `docs/ci-learnings.md`, entry 2026-09-14, rule going forward:
  > "`Lighthouse.EndToEndTests` is deliberately not pinned — its workflows activate `pnpm@latest` and
  > it patches nothing, so pinning it would create the disagreement rather than close it."
- **New assumption:** pin E2E via `packageManager` to the frontend's version.
- **Rationale:** that rule held while E2E workflows used `pnpm@latest`, because Dependabot then also
  used a new pnpm. D7 removes `@latest` from those workflows, and the same entry records that
  Dependabot honours `packageManager`. Once both read the same field there is nothing to disagree
  with. The risk the rule protected against — a lockfile written by a newer pnpm — is covered by
  AC3.5, which regenerates under the pinned version if needed. The ledger entry is amended (AC3.7),
  not rewritten.

## Wave: DISCUSS / [REF] Handoff

To DESIGN / DEVOPS / DISTILL — see the wave recommendation in the session summary; which of those
waves run is the user's explicit call.

---

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN and DEVOPS **skipped by explicit user instruction** ("skip design and devops", 2026-09-25).
  DISCUSS locked decisions D1–D11 stand in for DESIGN. The driving port is the existing
  `Verify Workflow Scripts` job (`node --test .github/scripts/*.test.mjs`); no new entry point.
- Wave-decision reconciliation: **passed, 0 contradictions** (no DESIGN/DEVOPS decisions exist to
  contradict DISCUSS).
- Project language for this surface: plain Node ESM with `node:test`, per `generate-version.test.mjs`.
  No dependencies are available under `.github/scripts`, so YAML and Dockerfiles are read line by line.

## Wave: DISTILL / [REF] Decisions DISTILL had to make (no DESIGN wave)

Written into the fixture that defines "compliant", so DELIVER inherits them:
- **[T1] engine-strict lives in each project's `pnpm-workspace.yaml` (`engineStrict: true`)**, not
  `.npmrc`. This follows the ledger rule that pnpm settings belong in `pnpm-workspace.yaml`
  (ci-learnings 2026-06-16). AC2.4 checks this against a real `pnpm install` on Node 26; if pnpm
  10.33.2 ignores the key there, change the fixture and the rule together.
- **[T2] The Dockerfile installs pnpm with `npm install -g "pnpm@<version read from packageManager>"`**,
  not corepack (D7), and without a literal version.
- **[T3] `ARG NODE_VERSION` is declared before the first `FROM`**; only then can a `FROM` line use it.
  The guard checks this (`dockerfile-node-arg-after-from`).
- **[T4] `engines.node` must equal `<.nvmrc major>.x` exactly.** That's simple to check, and it stays
  true for a patch pin like the probe's `24.x.y`.
- **[T5] Each violation carries `family` (`node`/`pnpm`), `rule`, `file` (repo-relative), `line`
  (1-based, when it has one) and a non-empty `message`.** Rule IDs are the contract the tests assert on.

## Wave: DISTILL / [REF] Scenario list

File: `.github/scripts/toolchain-pins.test.mjs` (31 tests; 6 are the existing `generate-version` ones).

| # | Test | Slice | Kind |
|---|---|---|---|
| 1 | the repository names its Node version only in .nvmrc | 01 | walking skeleton — real repo, `@real-io` |
| 2 | the repository names its pnpm version only in packageManager | 02 | walking skeleton — real repo, `@real-io` |
| 3 | a tree reading every version from its single source passes | 01 | happy path |
| 4–17 | 14 Node drifts: `node-version-literal` ×3 (quoted, unquoted, composite action), `dockerfile-node-literal`, `dockerfile-node-arg-default`, `dockerfile-node-arg-after-from`, `docker-build-arg-missing`, `nvmrc-missing`, `nvmrc-malformed` ×2 (`lts/*`, `v24`), `nvmrc-not-in-ci-paths`, `engines-node-mismatch`, `engines-node-missing`, `engine-strict-off` | 01 | error |
| 18–25 | 8 pnpm drifts: `pnpm-action-version-literal`, `pnpm-action-no-package-json`, `corepack-used` ×2 (workflow, Dockerfile), `pnpm-version-literal` ×2 (`@latest`, Docker literal), `package-manager-missing`, `package-manager-mismatch` | 02 | error |

Every drift test asserts the violation's family, rule, file and line, and that the seeded drift
produces **exactly one** violation, so one mistake is reported once. Error paths: 22 of 25 (88%).

## Wave: DISTILL / [REF] Evidence checks outside the guard

These ACs are about CI runs, a Docker build, or the maintainer's shell, which a unit-level check
can't observe. DELIVER records the evidence here.

| AC | Check | Evidence |
|---|---|---|
| AC1.4 | `docker build --target node-builder .` fails; with `--build-arg NODE_VERSION=$(cat .nvmrc)` succeeds | _pending_ |
| AC1.6 | probe `features/6070-node-probe`, `.nvmrc` = older 24.x patch → every Node-setting job prints it | _run URL_ |
| AC1.7 | first `main` run: `--build-arg NODE_VERSION=24`, `node:24-bookworm-slim` pulled, green | _run URL_ |
| AC2.1–2.2 | `fnm` in fish; `node --version` = v24.x in repo (interactive + non-interactive), v26.8.2 outside | _pending_ |
| AC2.4 | `pnpm install` on Node 26 → `ERR_PNPM_UNSUPPORTED_ENGINE`; on Node 24 → ok (both projects) | _pending_ |
| AC2.5 | `pnpm test` + `pnpm build` green under the `.nvmrc` Node | _pending_ |
| AC3.5 | E2E `pnpm install --frozen-lockfile` under the pinned pnpm | _pending_ |
| AC3.6 | probe `features/6070-pnpm-probe`, both `packageManager` = older 10.x → every pnpm job prints it | _run URL_ |
| AC3.7 | ci-learnings 2026-09-14 amended | _commit_ |

## Wave: DISTILL / [REF] Scaffolds

- `.github/scripts/toolchain-pins.mjs` — `export const __SCAFFOLD__ = true`;
  `findToolchainPinViolations(repoRoot)` throws `AssertionError` ("RED scaffold").

## Wave: DISTILL / [REF] RED classification (pre-DELIVER gate)

With the skips stripped in a scratch copy (Node 26.8.2): **25/25 fail `ERR_ASSERTION` with the
scaffold's "RED scaffold" message**, so every failure is `MISSING_FUNCTIONALITY`. None fails on a
fixture edit (every `replace` found its needle). As committed: 31 tests, 6 pass, 25 skipped, suite
green.

## Wave: DISTILL / [REF] Test placement & adapter coverage

- Placement: `.github/scripts/`, next to `generate-version.test.mjs`. It's picked up by the existing
  glob, so no workflow edit is needed (AC4.3).
- Driving adapter: `node --test` in `Verify Workflow Scripts`, the first job of `ci.yml`. Tests 1–2
  are the end-to-end path.
- Driven adapter: the file system only, real I/O in every test (a `mkdtemp` tree or the real repo).
  No doubles.
- Policy: row appended to `docs/architecture/atdd-infrastructure-policy.md` (Driving).

## Wave: DISTILL / [REF] Deviations

- **The walking skeleton is not green at hand-off.** Tests 1–2 assert the end state of the
  repository itself, which is what DELIVER changes. They are skipped until their slice; test 3 (the
  compliant tree) is the first one DELIVER turns green.
- Gherkin `.feature` files are not produced. This surface's convention is `node:test`, and the test
  names read as the scenarios.
- Outcomes registry: skipped. The guard is a CI invariant, not a product contract.
