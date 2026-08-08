---
description: Cut a Lighthouse release end to end — sync docs + screenshots, refresh the architecture overview, draft release notes, check the clients for a new version, trigger the signed standalone GitHub release, and announce on Slack. Invoke this whenever the user says "let's release", "do a release", "ship a release", or "/release".
allowed-tools: Bash, Read, Edit, Write, Glob, Grep, AskUserQuestion, Skill, Task
---

# /release — cut a Lighthouse release, end to end

You are the release conductor. Walk the user through the whole motion below, in order, **pausing at each human-decision gate** (doc/screenshot review, release-notes review, clients trigger, the `Release` environment approval, the Slack post). Reuse the existing commands rather than re-deriving their work — invoke them with the **Skill** tool (`update-docs`, `release-notes`, `release-social`); if nested-skill invocation isn't available in this session, fall back to reading and following the steps in the corresponding `.claude/commands/<name>.md` file.

This command does NOT write code or bump versions itself. The Lighthouse version is **calver** (`vYY.M.D.<build>`) assigned by CI (`ci_version.yml`) at release time; the signed standalones and the GitHub release are produced by the `release` job in the `Build And Deploy Lighthouse` pipeline, gated on the `Release` GitHub environment. Your job is to get everything ready, drive the gate, and announce.

Repos:
- Server: `/storage/repos/Lighthouse` (this repo) — `LetPeopleWork/Lighthouse`, trunk-based on `main`.
- Clients: `/storage/repos/lighthouse-clients` — `LetPeopleWork/lighthouse-clients`, trunk-based on `main`, Changesets-based.

## Phase 0 — pre-flight

1. Confirm this repo is on `main` with a clean working tree (`git -C /storage/repos/Lighthouse status`). If dirty, stop and ask the user to resolve.
2. Last released version + what's new since:
   ```bash
   gh release view --repo LetPeopleWork/Lighthouse --json tagName,publishedAt -q '"\(.tagName)  \(.publishedAt)"'
   LAST=$(gh release view --repo LetPeopleWork/Lighthouse --json tagName -q .tagName)
   git -C /storage/repos/Lighthouse log --oneline "$LAST"..HEAD
   ```
3. Confirm the **latest `main` run is green** (a release can only be cut on a green pipeline):
   ```bash
   gh run list --repo LetPeopleWork/Lighthouse --workflow "Build And Deploy Lighthouse" --branch main --limit 1 \
     --json databaseId,headSha,status,conclusion
   ```
   **Do NOT read the run-level `status` as the verdict.** A healthy release candidate sits at `status: waiting, conclusion: ""` — the `Release` environment parks the three `Package * Standalone` jobs, and that IS the gate you are here to approve. Judge by the job list instead:
   ```bash
   gh run view $RUN --repo LetPeopleWork/Lighthouse --json jobs \
     --jq '.jobs[] | "\(.name): \(.status)/\(.conclusion)"'
   ```
   Green candidate = every verify job (`frontend`, `backend`, `verifysqlite`, `verifypostgres`, `verifyauth`, `docker`, `packageapp`) plus `sonar-gates` `completed/success`, with only the standalone package jobs `waiting`. Stop and offer `/clean-ci` only if a job actually concluded `failure`, or if jobs are still `in_progress`.
4. Map the version to its run. `<build>` in `vYY.M.D.<build>` is **the count of `ci.yml` runs on `main` created today (UTC), including that run** — so a version names ONE already-existing run, which may not be the newest. Read it off the run's artifact names rather than counting by hand:
   ```bash
   gh api repos/LetPeopleWork/Lighthouse/actions/runs/$RUN/artifacts --jq '.artifacts[].name' | grep -m1 Lighthouse-linux
   ```
   Docs-only pushes are path-filtered out of `ci.yml`, so they never consume a build number — and a docs-only HEAD sitting on top of the candidate run is fine, `Deploy Documentation` already shipped it.
5. If there are unpushed/unreleased commits since `$LAST`, summarize them — that's the release content.
6. Show the user the plan (the phases below) and the commit list, and ask whether to proceed.

## Phase 1 — docs + screenshots

Invoke the **`update-docs`** skill. It identifies stale/missing docs + `@screenshot` E2Es, gets the user's scope approval, runs the screenshot suite against a clean backend, and reports which `docs/assets/**.png` actually changed. Let it run to completion and surface its diff. Commit the doc/screenshot changes here (or fold them into the Phase 3 commit) — do not push yet.

## Phase 1b — architecture overview

`ARCHITECTURE.md` (repo root) describes the **general concepts** — modules, seams, cross-cutting mechanisms, topologies, load-bearing constraints. It is supposed to be updated in the same change that moves a concept, but a release is the backstop: nothing ships describing an architecture the code no longer has.

1. See what architectural decisions landed since the last release:
   ```bash
   git -C /storage/repos/Lighthouse log "$LAST"..HEAD --diff-filter=A --name-only --format= -- 'docs/product/architecture/adr-*.md' | sort -u
   git -C /storage/repos/Lighthouse log "$LAST"..HEAD --oneline -- ARCHITECTURE.md
   ```
   No new ADRs **and** no architecture-shaped commits (a new module folder, a new port/adapter pair, a new inbound surface, a new provider/topology) → say "architecture overview unchanged" and move on.
2. For each new ADR, read its **Status** and **Relationship to prior ADRs** header lines. Two kinds matter more than the rest:
   - an ADR that **amends or supersedes ADR-027** — it changed a load-bearing constraint, so §2 is wrong until you fix it;
   - an ADR that **supersedes an ADR the overview cites by number** — the citation now points at a dead decision.
3. Then check the handful of facts that rot silently, **against the code, not against the ADRs** (an accepted ADR is not evidence that the code does it):
   - the ADR range in the header bullet and in the closing line of the ADR index vs. the highest `adr-NNN-*.md` on disk;
   - the module table vs. the namespace patterns in `ModuleBoundariesArchUnitTest.cs`, and the enforced-rules list vs. the tests in `Lighthouse.Backend.Tests/Architecture/`;
   - the authentication schemes vs. `SmartAuthSchemeSelector`, and the adapter choices vs. the registrations in `Program.cs`;
   - the connector list vs. `Services.*.WorkTrackingConnectors`;
   - the topology table vs. `chart/` and the health endpoints;
   - the CI paragraph vs. `.github/workflows/`;
   - framework majors (`TargetFramework`, React/MUI/Vite in `Lighthouse.Frontend/package.json`).
4. Update the affected sections **and** add the load-bearing new ADRs to the ADR-index table at the end. Keep feature-level detail out — that belongs in `brief.md` and the ADRs. Commit separately from the screenshots (`docs(architecture): …`), or fold into Phase 3; do not push yet.

## Phase 2 — release notes

Invoke the **`release-notes`** skill. It drafts the new `# Lighthouse <version>` block at the top of `docs/releasenotes/releasenotes.md` from the ADO items tagged "Release Notes" + the commits since the last tag, attributes community reporters, and adds first-time contributors to `docs/contributions/contributions.md`. Review the draft WITH the user; the heading version may be `vNext` or a date-based placeholder — that's fine, the real tag comes from CI in Phase 5 and you reconcile it there.

## Phase 3 — commit + push, wait for green

Commit whatever Phases 1–2 produced — docs, screenshots, the architecture overview, release notes (conventional message, e.g. `docs(release): notes + screenshots for <version>`), push to `main`, then watch the resulting `Build And Deploy Lighthouse` run until the gates are green (mirror `/clean-ci`'s watch loop; if anything fails, stop and offer `/clean-ci`). **This green run is the candidate the release is cut from** — note its `databaseId`.

## Phase 4 — clients: new version if changed since last release

1. Detect changes since the clients' last release = pending changesets:
   ```bash
   ls /storage/repos/lighthouse-clients/.changeset/*.md 2>/dev/null | grep -viE '/(README|config)\b'
   ```
   Also show what changed:
   ```bash
   CLAST=$(gh release view --repo LetPeopleWork/lighthouse-clients --json tagName -q .tagName 2>/dev/null)
   git -C /storage/repos/lighthouse-clients log --oneline "${CLAST:-HEAD~20}"..HEAD
   ```
2. **If there are pending changesets** (or unreleased client commits), a new client version is warranted. The clients publish via a **manual version bump, then a gated publish on `main`**. The `Client CI` `release` job runs only `changeset publish` — it has NO `changeset version` step and there is no version-PR bot, so it ships whatever is already in each `package.json`; skip the bump and publish silently no-ops (a calver GitHub release tag is still cut, which is exactly what "the release ran but didn't update the packages" looks like):
   1. Land the feature commit(s) **with** their `.changeset/*.md` on `main` (the pre-commit `pnpm ci` hook enforces a changeset is present).
   2. Run `pnpm release:version` in `/storage/repos/lighthouse-clients` — consumes the changesets, bumps `package.json` versions + writes CHANGELOGs, deletes the changeset files. This commit touches `package.json` with no changeset, so the pre-commit hook blocks it: commit with `SKIP_SIMPLE_GIT_HOOKS=1` (documented escape in `docs/release-model.md`), message `chore(release): version packages — <summary>`, and push to `main`.
   3. Approve the **`Release` environment on that new run** — the clients repo has one, same as the server. **Order matters**: the gate is per-run, so a run started on the pre-bump commit would publish the already-published versions. Push the bump FIRST, approve the gate on the run it triggers; the stale pre-bump run just sits in `waiting` and can be ignored.
      ```bash
      CRUN=$(gh run list --repo LetPeopleWork/lighthouse-clients --workflow "Client CI" --branch main --limit 1 --json databaseId -q '.[0].databaseId')
      gh api repos/LetPeopleWork/lighthouse-clients/actions/runs/$CRUN/pending_deployments \
        --jq '.[] | {env: .environment.name, env_id: .environment.id, current_user_can_approve}'
      gh api -X POST repos/LetPeopleWork/lighthouse-clients/actions/runs/$CRUN/pending_deployments \
        -f state=approved -F 'environment_ids[]=<env_id>' > /dev/null
      ```
      Redirect the approval response to `/dev/null` — its shape differs from the server repo's and a `--jq` filter errors noisily even when the approval succeeded. Verify by the published version, not by the command's output:
      ```bash
      npm view @letpeoplework/lighthouse-client version   # etc. per package
      gh run list --repo LetPeopleWork/lighthouse-clients --workflow "Client CI" --branch main --limit 1 \
        --json databaseId,status,conclusion,url
      ```
      `updateInternalDependencies: patch` means dependents (`mcp-http`, `mcp-stdio`) get patch bumps when `client`/`mcp-core` take a minor.
   `release:version` needs the `GITHUB_TOKEN_CHANGESET` credential (the changelog-github plugin); if it's not in the shell, have the user run the bump. Don't block the server release on this — it proceeds in parallel.
3. **If there are no pending changesets**, say "clients unchanged since last release — nothing to publish" and move on.

## Phase 5 — cut the release (signed standalones + GitHub release)

The `release` job on the Phase-3 green run tags the commit, signs + cosigns the `latest` docker image, gathers the **signed** standalones already built by the pipeline (Windows NSIS + MSI, macOS DMG/app, Linux AppImage) + the SBOM, and publishes a prerelease GitHub release. It waits on the **`Release` environment**.

1. Find the pending `Release` deployment on the candidate run:
   ```bash
   RUN=<databaseId from Phase 3>
   gh api repos/LetPeopleWork/Lighthouse/actions/runs/$RUN/pending_deployments \
     --jq '.[] | {env: .environment.name, env_id: .environment.id, current_user_can_approve}'
   ```
2. **Approving is a deliberate, human-owned gate.** Confirm with the user (`AskUserQuestion`) before approving. If they approve and the token may approve (`current_user_can_approve: true`):
   ```bash
   gh api -X POST repos/LetPeopleWork/Lighthouse/actions/runs/$RUN/pending_deployments \
     -f state=approved -f comment="cut release" -F 'environment_ids[]=<env_id>'
   ```
   If the token cannot approve, direct the user to the run's web UI to click **Review deployments → Release → Approve**:
   ```bash
   gh run view $RUN --repo LetPeopleWork/Lighthouse --json url -q .url
   ```
3. Wait for the release job to finish, then confirm the GitHub release and the new tag:
   ```bash
   gh release view --repo LetPeopleWork/Lighthouse --json tagName,isPrerelease,assets -q '.tagName, (.assets | length)'
   ```
   Capture the **real tag** (e.g. `v26.6.6.3`). Sanity-check the asset list includes the win/msi/dmg/app/AppImage + `.sig` files and the SBOM.
4. **Reconcile the version + Helm chart** (one dedicated commit, pushed *after* the GitHub release is live so the real calver tag is known):
   - **Release-notes heading**: if `docs/releasenotes/releasenotes.md` opens with `# Lighthouse vNext` (or a guessed date), edit it to `# Lighthouse <real tag>`. (Skip if it already matches.)
   - **Helm chart** — the chart lagging the app is a release defect, so it tracks every app release. The app calver appears in **four** places; miss one and the publish either fails or ships an inconsistent chart:
     1. `chart/Chart.yaml` — `appVersion` to the new calver (tag minus the leading `v`, e.g. `26.7.12.1`) **and** `version` to the next SemVer (e.g. `0.1.8` → `0.1.9`).
     2. `chart/values-enterprise.yaml` — `image.tag`, same calver. (`chart/values.yaml` leaves the tag empty and falls back to `appVersion` per ADR-083 — don't pin it.)
     3. `chart/README.md` — **helm-docs-GENERATED from `README.md.gotmpl`; never hand-edit it.** The `validate` job regenerates it and fails on any diff (`config-ref drift`). Regenerate with the pinned binary:
        ```bash
        helm-docs --chart-search-root chart --skip-version-footer -s file --ignore-non-descriptions
        ```
     4. `docs/Installation/kubernetes.md` — the quick-start snippets pin both versions; grep it for the previous chart + appVersion strings.
   - Commit all of it together (`docs(release): pin <real tag> + chart <newchartver>`) and push to `main`.
   - **The chart has its OWN `Release` gate.** Pushing the bump is not the publish. The `chart/**` path triggers the `Helm Chart` workflow (`ci_chart.yml`); its `publish` job runs `validate` + `install-smoke` + `detect-publish`, then waits on the same `Release` environment. Approve it the same way as Phase 5.1–5.2, with `--workflow "Helm Chart"`, and confirm the chart landed:
     ```bash
     gh run list --repo LetPeopleWork/Lighthouse --workflow "Helm Chart" --branch main --limit 1 \
       --json databaseId,status,conclusion,url
     ```
     `detect-publish` skips the whole publish when the chart `version` already appears in `docs/charts/index.yaml` — a run that goes green *without* a `publish` job means you forgot to bump `Chart.yaml:version`.

## Phase 6 — announce on Slack

Invoke the **`release-social`** skill (Slack only — LinkedIn was removed). Pass the real tag as its argument so it targets the right block. Walk the user through the draft and, on their explicit pick, post to `#general`.

## Phase 7 — final report

Summarize:
- Released version (real tag) + GitHub release URL + asset count.
- Docs/screenshots: which images changed.
- Architecture overview: unchanged, or which sections moved and which ADRs drove it.
- Release notes: headline count + contributors (and any first-timers added).
- Helm chart: new `version` + `appVersion` (must match the app calver), and whether the `Helm Chart` run's `publish` job actually ran and landed the `.tgz` in `docs/charts/`.
- Clients: published a new version / told user to trigger it / unchanged.
- Slack: posted (permalink) / saved draft only.
- Any follow-ups (e.g. ADO items to close, a clients `Release` approval still pending).

## Guardrails

- Never approve the `Release` environment (server, clients, or chart — three separate approvals, three separate go-aheads) without an explicit user go-ahead in the same turn — it publishes a public release.
- Never cut a release on a pipeline with a `failure` job or with jobs still `in_progress`. A run parked at `waiting` on the `Release` gate with every other job green is NOT "in progress" — it is the candidate (Phase 0.3).
- Don't hand-create the git tag or the GitHub release — the `ci_release.yml` job owns tagging, signing, and asset upload. Your lever is the environment approval.
- Don't bump client package versions by hand — Changesets + the clients' release job own that.
- Don't let `ARCHITECTURE.md` go out describing an architecture the release doesn't have. A new ADR that amends ADR-027 is the loudest signal, but the check is against the code — an ADR can be accepted and unbuilt, and code can move without an ADR.
- Keep the user in the loop at every gate; this command drives the motion, the user makes the calls.
