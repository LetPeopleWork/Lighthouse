# One dependency bot everywhere — Story 6095

Delivered 2026-09-29 in three slices across two repositories. Repository tooling only: nothing in a running
Lighthouse changes, and no release is needed for it to take effect.

Lighthouse took its updates from Dependabot, while lighthouse-platform and lighthouse-clients used Renovate.
That meant two tools, two rule sets and two auto-merge policies that differed for no stated reason. Lighthouse
now runs on the same hosted Renovate app as the other two. Every update proposed there merges itself once the
required checks are green, and the MUI packages that had been held back for months are current again.

## What shipped

| Slice | What it changed |
| --- | --- |
| 01 — Lighthouse on Renovate | `renovate.json` replaces `.github/dependabot.yml` and the PAT-driven auto-merge workflow. An update is proposed once its release is 7 days old (a security fix at once), and every update merges itself, as a merge commit, once the 8 required checks of the `main` ruleset pass. Three kinds wait for a maintainer: the Node and pnpm toolchain group, a .NET runtime image major, and chart value updates. A one-time conversion pinned all 141 `uses:` refs to `@<sha> # <version>`. Two new guards run in the required `Verify Workflow Scripts` check: `action-pins` (every `uses:` is in the form Renovate can update) and `dependency-bot` (one bot, no `if:` that depends on who pushed or opened the change, a readable `renovate.json`, and a reason on every hold). Both read YAML through the shared `scanned-yaml-files.mjs`. The `dependabot[bot]` guards came out of 8 workflows, and `.github/actions/README.md` describes Renovate. Repository settings: Dependabot security updates off, alerts on, the 12 Dependabot secrets deleted, `Verify Workflow Scripts` made the 8th required check, and the dashboard off because Issues are off. |
| 02 — lighthouse-clients merges everything on green | Majors and vulnerability PRs auto-merge there too once `verify` passes, and `docs/release-model.md` says so (lighthouse-clients `b42d986`). |
| 03 — the MUI holds lifted | `@mui/x-charts` and `@mui/x-date-pickers` 9.0.x → 9.14.0, `@mui/icons-material` 7 → 9.4.0. `ProcessBehaviourChart.tsx` took the library's `MarkElementProps` type, and two icons moved to the `*Outlined` names that draw the same glyph. All three holds left `renovate.json`. Merged as PR #1802 after the maintainer's check on the dev instance. The `@screenshot` run rewrote 51 images, and the maintainer reviewed them and kept none. |

Close-out: `2462de738` shares the scanned-YAML read loop between the guards; `70329439f` makes both guards
skip the text inside YAML block scalars; `c94a912e7` replaces an internal reference in a comment; `120284c4e`
closes the mutation gaps.

Live proof, 2026-09-29: Renovate PRs #1800, #1801 and #1803–#1806 merged themselves on green. #1800 moved
`actions/upload-artifact` from `# v7.0.0` to `# v7.0.1` and kept it SHA-pinned. Its `main` run, pushed as
`renovate[bot]`, ran every job and waited at the `Package and Sign` approval ahead of `Release`, the same gate
where the maintainer's own merge waits. lighthouse-clients #29 merged itself.

Architecture: ADR-212 *(Renovate is the one dependency bot across the LetPeopleWork repositories, and a green
update merges itself)*. The permanent record is [`ARCHITECTURE.md`](../../ARCHITECTURE.md) §15. The delivery
history is in [`docs/feature/story-6095-switch-to-renovate/`](../feature/story-6095-switch-to-renovate/).

## Decisions worth keeping

- **Everything auto-merges on the required checks, majors and security fixes included**, in Lighthouse and in
  clients. It is the policy Lighthouse had run on under Dependabot since 2024. This reversed #6088's "majors
  and security fixes wait for a human" for clients. The merge gate is the ruleset, so what a PR must pass is
  decided in one place.
- **Three exceptions, each with its reason in `renovate.json`.** Each one needs a change a green PR cannot
  carry: toolchain moves need CI, the Docker image and the lockfile format to move together; a .NET image major
  needs the TargetFramework change; a chart value needs a regenerated chart README and a new chart version.
- **Coverage is everything Renovate finds**, not only the five ecosystems Dependabot covered: Cargo, Bundler,
  helm values, workflow `with:` versions and the pnpm override floors. A fixed `enabledManagers` list would
  silently leave out the next manager a file introduces.
- **Guard the rule, not the value.** The `dependency-bot` guard does not re-check `automerge: false` on the
  toolchain group, because that would restate the file. It enforces what must hold for any future edit: one
  bot, no actor-dependent `if:`, and a reason on every hold.
- **Dependabot alerts stay, Dependabot updates go.** Renovate reads the alerts to open vulnerability PRs.
  Leaving security updates on would have produced duplicate PRs.
- **No new observability.** The KPIs are `gh` queries run by hand at T0, T1 and T+7d, following the standing
  rule of no scheduled runs unless asked.

## Lessons

- **Renovate never updates a bare SHA pin.** A `uses: owner/repo@<sha>` with no version comment gives it
  nothing to compare against, so it skips the pin without saying so. The `# <version>` comment is what keeps a
  pinned action current, and the `action-pins` guard keeps it there.
- **The Mend org default is Silent, but a repository with its config on `main` runs anyway.** Lighthouse needed
  only `renovate.json` on `main` and the app's repository selection. There was no onboarding PR and no
  org-level switch.
- **With Issues off there is no Dependency Dashboard.** It is a GitHub issue. Lighthouse runs with
  `dependencyDashboard: false`, and pending or held-back updates are read in the Renovate log at
  developer.mend.io. Check `has_issues` before designing around the dashboard.
- **A Renovate merge pushes to `main` as `renovate[bot]`.** Under Dependabot the push came from the PAT owner,
  so the `dependabot[bot]` guards never fired. Rewriting them for Renovate would have skipped docker,
  packaging, signing and release after every dependency merge. Actor-dependent `if:` is now banned outright.
- **MUI x-charts 9.14 drops reference lines outside the drawing area**, and jsdom has no layout, so a chart
  test that asserts a reference line must give the chart a real width. The first GREEN attempt failed on
  exactly this.
- **A zero-finding review is not evidence of no defect.** The final review of the guards found nothing. A
  ten-line probe, a workflow whose `run: |` script writes YAML into a heredoc, showed both guards reading block
  scalar content as keys. Probe a line-based parser with the inputs it is most likely to misread before
  accepting a clean review.

## Quality

Class A: `node --test .github/scripts/*.test.mjs` passes 188 of 188, none skipped, in a required check.
`renovate-config-validator --strict` passes. Mutation (StrykerJS command runner, 80 % per file):
**94.20 %** overall. `action-pins` 94.57 %, `dependency-bot` 94.02 %, `scanned-yaml-files` 94.19 %. The 21
survivors were each reviewed as equivalent or unreachable. See
[`story-6095-switch-to-renovate/mutation-results.md`](story-6095-switch-to-renovate/mutation-results.md).
Frontend gates for the MUI upgrade were green on PR #1802's required checks. The backend is untouched.

## What stays open

- **The pnpm 12 toolchain PR:** still to be observed. It should open as one grouped PR with no auto-merge
  and wait for the maintainer, with the toolchain guard green on it.
- **The KPI checks at T+7d (2026-10-06):** KPI-1 to KPI-4 in `docs/product/kpi-contracts.yaml`, and the
  operational KPI-5 to KPI-7 in the feature delta. At T0, KPI-1 and KPI-3 both read 0.
- **ADO Bug #6112:** `verifypostgres` flakiness seen during the live runs, not caused by this story.
- **Internal-reference comments in workflow files:** ADR-083/080/084 in `ci_chart.yml`, "Guard A1/A2, ADR-105"
  in the macOS packaging step names, and "Guard A4, ADR-105" in `ci_generate-update-feed.yml`. The maintainer
  has not decided whether to rewrite them.
- **`Scripts/validate-dependabot.js`:** unreferenced, and it now exits 2 because the file it validates is gone.
- **The OSV Scanner claim in the CRA technical file:** no workflow runs OSV Scanner. It predates this story.
- **ADO #6095:** Resolved on push, Closed on green CI.
