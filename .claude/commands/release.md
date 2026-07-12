---
description: Cut a Lighthouse release end to end — sync docs + screenshots, draft release notes, check the clients for a new version, trigger the signed standalone GitHub release, and announce on Slack. Invoke this whenever the user says "let's release", "do a release", "ship a release", or "/release".
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
   If it's red or running, say so and stop (offer `/clean-ci` to diagnose). If there are unpushed/unreleased commits since `$LAST`, summarize them — that's the release content.
4. Show the user the plan (the phases below) and the commit list, and ask whether to proceed.

## Phase 1 — docs + screenshots

Invoke the **`update-docs`** skill. It identifies stale/missing docs + `@screenshot` E2Es, gets the user's scope approval, runs the screenshot suite against a clean backend, and reports which `docs/assets/**.png` actually changed. Let it run to completion and surface its diff. Commit the doc/screenshot changes here (or fold them into the Phase 3 commit) — do not push yet.

## Phase 2 — release notes

Invoke the **`release-notes`** skill. It drafts the new `# Lighthouse <version>` block at the top of `docs/releasenotes/releasenotes.md` from the ADO items tagged "Release Notes" + the commits since the last tag, attributes community reporters, and adds first-time contributors to `docs/contributions/contributions.md`. Review the draft WITH the user; the heading version may be `vNext` or a date-based placeholder — that's fine, the real tag comes from CI in Phase 5 and you reconcile it there.

## Phase 3 — commit + push, wait for green

Commit the docs + screenshots + release-notes changes (conventional message, e.g. `docs(release): notes + screenshots for <version>`), push to `main`, then watch the resulting `Build And Deploy Lighthouse` run until the gates are green (mirror `/clean-ci`'s watch loop; if anything fails, stop and offer `/clean-ci`). **This green run is the candidate the release is cut from** — note its `databaseId`.

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
2. **If there are pending changesets** (or unreleased client commits), a new client version is warranted. The clients publish via a **manual version bump, then auto-publish on `main`** (there is NO `Release`-environment approval gate — earlier drafts of this doc wrongly said there was one):
   1. Land the feature commit(s) **with** their `.changeset/*.md` on `main` (the pre-commit `pnpm ci` hook enforces a changeset is present).
   2. Run `pnpm release:version` in `/storage/repos/lighthouse-clients` — consumes the changesets, bumps `package.json` versions + writes CHANGELOGs, deletes the changeset files. This commit touches `package.json` with no changeset, so the pre-commit hook blocks it: commit with `SKIP_SIMPLE_GIT_HOOKS=1` (documented escape in `docs/release-model.md`), message `chore(release): version packages — <summary>`, and push to `main`.
   3. On that push, the `Client CI` `release` job **auto-publishes** the bumped packages to npm (no human approval) and runs smoke tests. Confirm publication:
      ```bash
      npm view @letpeoplework/lighthouse-client version   # etc. per package
      gh run list --repo LetPeopleWork/lighthouse-clients --workflow "Client CI" --branch main --limit 1 \
        --json databaseId,status,conclusion,url
      ```
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
   - **Helm chart** (`chart/Chart.yaml`): bump `appVersion` to the new app calver (the tag without the leading `v`, e.g. `26.7.12.1`) and bump the chart `version` (SemVer, e.g. `0.1.5` → `0.1.6`). Also refresh any pinned chart/app version in the k8s quick-start docs snippets (grep `docs/` for the previous chart + appVersion strings — last release touched `docs/` k8s quick-start). The chart is published from this bump, so it must track every app release; the chart lagging the app is a release defect.
   - Commit both together (`docs(release): pin <real tag> + chart <newchartver>`) and push to `main`.

## Phase 6 — announce on Slack

Invoke the **`release-social`** skill (Slack only — LinkedIn was removed). Pass the real tag as its argument so it targets the right block. Walk the user through the draft and, on their explicit pick, post to `#general`.

## Phase 7 — final report

Summarize:
- Released version (real tag) + GitHub release URL + asset count.
- Docs/screenshots: which images changed.
- Release notes: headline count + contributors (and any first-timers added).
- Helm chart: new `version` + `appVersion` (must match the app calver).
- Clients: published a new version / told user to trigger it / unchanged.
- Slack: posted (permalink) / saved draft only.
- Any follow-ups (e.g. ADO items to close, a clients `Release` approval still pending).

## Guardrails

- Never approve the `Release` environment (server or clients) without an explicit user go-ahead in the same turn — it publishes a public release.
- Never cut a release on a red or in-progress pipeline. Green-or-stop.
- Don't hand-create the git tag or the GitHub release — the `ci_release.yml` job owns tagging, signing, and asset upload. Your lever is the environment approval.
- Don't bump client package versions by hand — Changesets + the clients' release job own that.
- Keep the user in the loop at every gate; this command drives the motion, the user makes the calls.
