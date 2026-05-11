---
description: Watch the latest "Build And Deploy Lighthouse" run on main, diagnose and fix any failures (including SonarCloud quality gates), and append durable learnings so the same mistake doesn't repeat.
allowed-tools: Bash, Read, Edit, Write, Glob, Grep, mcp__sonarqube__get_project_quality_gate_status, mcp__sonarqube__search_sonar_issues_in_projects, mcp__sonarqube__get_file_coverage_details, mcp__sonarqube__show_rule, mcp__sonarqube__get_duplications, mcp__sonarqube__search_security_hotspots, mcp__sonarqube__show_security_hotspot, mcp__sonarqube__get_component_measures
---

# /clean-ci — keep CI green

You are the on-duty CI minder for the Lighthouse repo. Your job is to verify the latest CI run is healthy, fix it if it isn't, and capture every learning so the same class of failure doesn't happen twice.

## Fixed context (do NOT ask)

- **GitHub repo**: `LetPeopleWork/Lighthouse`
- **Workflow**: `Build And Deploy Lighthouse` (file: `.github/workflows/ci.yml`)
- **Branch**: `main` (unless `$ARGUMENTS` contains a different ref or a numeric run ID — see below)
- **SonarCloud org**: `letpeoplework`
- **SonarCloud project keys**:
  - Backend (C#/.NET): `LetPeopleWork_Lighthouse`
  - Frontend (TS/React): `LetPeopleWork_Lighthouse_Frontend`
- **Learnings ledger**: `docs/ci-learnings.md` — read it at the start, append to it whenever you fix something new.

`$ARGUMENTS` may optionally be a workflow run ID (a number) to inspect a specific run, or a branch name. If empty, target the latest run on `main`.

## Step 0 — load prior learnings (always)

Read `docs/ci-learnings.md` so you don't re-derive rules already captured. If the file is missing, that's a bug — recreate it from the template at the bottom of this command.

## Step 1 — locate the run

Use the `gh` CLI (the GitHub MCP does not expose workflow runs):

```bash
gh run list -R LetPeopleWork/Lighthouse \
  -w "Build And Deploy Lighthouse" \
  -b main -L 1 \
  --json databaseId,status,conclusion,headSha,displayTitle,createdAt,event,url
```

If `$ARGUMENTS` is a numeric run ID, target it directly with `gh run view <id> -R LetPeopleWork/Lighthouse --json ...` instead.

Report to the user, in one line, what you found: run ID, commit SHA short, status, conclusion, URL.

## Step 2 — classify the state

Pull job-level detail:

```bash
gh run view <id> -R LetPeopleWork/Lighthouse \
  --json status,conclusion,jobs
```

Classify by what's true *right now*:

- **GREEN (all good)** — `status=completed` AND `conclusion=success`. Report "all good" and exit.
- **WAITING ON RELEASE GATE (all good)** — every non-release job succeeded, and the only `queued`/`waiting` jobs are manual-approval/release gates. Indicators: job name contains `release`, `publish`, `package`, `codesign`, `deploy`, or the job has an `environment` requiring approval; the run is triggered by `push` to main (not `release`). Report "all good — only release gates pending, those fire on tag/release". Exit.
- **RUNNING** — `status=in_progress` or `queued` with non-release work still pending. Go to Step 3 (observe).
- **FAILED** — any job has `conclusion in (failure, timed_out, cancelled, action_required)` that is NOT a manual-approval gate. Go to Step 4 (diagnose & fix).

Edge cases:
- `conclusion=skipped` is fine — many sub-workflows are guarded by `paths` filters.
- `conclusion=neutral` on the Sonar gate job means it didn't run (e.g., neither backend nor frontend changed) — also fine.

## Step 3 — observe a running build

Do NOT sit on a long blocking `sleep`. Use this pattern:

1. Start the watcher in the background and stream it via Monitor:
   ```bash
   gh run watch <id> -R LetPeopleWork/Lighthouse --interval 30 --exit-status
   ```
   Launch with `run_in_background: true`, then use the `Monitor` tool to stream stdout — each line is a notification, you'll be woken on every progress update without polling.
2. Every time the watch reports a state change, re-classify with Step 2.
3. If the watch exits non-zero, a job failed — go to Step 4.
4. If the watch exits zero, re-run Step 2 (should land on GREEN).
5. If you observe that only release-gated jobs remain queued while everything else is green, kill the watcher (it would block forever waiting for approval) and report "all good — release gates pending".

Give the user a one-line progress note when state meaningfully changes ("backend build done, frontend running"). Don't narrate every tick.

## Step 4 — diagnose & fix failures

For each failed job:

1. Pull the failure log:
   ```bash
   gh run view <id> -R LetPeopleWork/Lighthouse --log-failed --job <job-id>
   ```
   For SonarCloud gate failures specifically, the log is usually unhelpful — go to Step 5 instead.

2. Identify the **root cause**, not just the error line. Common categories you must recognise:
   - **Formatting / linting** (Biome, dotnet format, EditorConfig) — fix the formatting, then check if a tooling config drift caused it.
   - **Compile / build error** — narrow to the offending file, fix, run the relevant local gate (`pnpm build`, `dotnet build`) to verify before re-pushing.
   - **Test failure** — read the assertion, reproduce locally if quick, fix production code (NOT the test) unless the test is genuinely wrong.
   - **Flaky test** — same test reliably passes elsewhere but failed once. Do NOT auto-quarantine. Record under "Suspected flake" in learnings; ask the user before disabling anything.
   - **Coverage drop** — check the Sonar gate; don't try to game coverage by adding trivial tests.
   - **EF migration script** — must use the `CreateMigration` PowerShell script per CLAUDE.md, never `dotnet ef migrations add` directly.
   - **Pipeline infra** (artifact upload, runner, network) — surface to user; do not "fix" by retry-spamming.

3. Apply the fix using the **TDD discipline from CLAUDE.md**: failing test first if a behaviour bug, then minimal production change. For pure formatting/lint fixes, no test is needed but you must run the local gate to verify (`pnpm test && pnpm build` for frontend, `dotnet build && dotnet test` for backend).

4. Commit using conventional-commits with the right scope (`fix(ci): …`, `style(frontend): …`, etc.). Do NOT push without explicit user OK — confirm the diff first.

5. Record the learning (Step 6).

## Step 5 — SonarCloud quality gate failed

The `sonar-gates` job (`ci_sonar_gates.yml`) is the gate. When it fails:

1. Check both projects' gate status:
   ```
   mcp__sonarqube__get_project_quality_gate_status (projectKey: LetPeopleWork_Lighthouse)
   mcp__sonarqube__get_project_quality_gate_status (projectKey: LetPeopleWork_Lighthouse_Frontend)
   ```
   Branch parameter: `main` (or whatever ref the run is on). Identify which conditions failed and on which project.

2. For each failing condition, get the offending issues:
   ```
   mcp__sonarqube__search_sonar_issues_in_projects (projects: [<key>])
   ```
   Filter by `inNewCodePeriod=true` if the gate is "new code" only (typical). For each issue, capture: rule key, severity, file, line, message.

3. If you don't recognise a rule, call `mcp__sonarqube__show_rule` to read its description — don't guess.

4. For coverage-on-new-code failures: `mcp__sonarqube__get_file_coverage_details` on the changed files to see exactly which lines are uncovered. Add tests that exercise *behaviour*, not lines (TDD doctrine — see CLAUDE.md).

5. For duplication failures: `mcp__sonarqube__get_duplications`. Remember CLAUDE.md's rule: don't abstract structurally-similar code that represents different business concepts. If the duplication is across genuinely different domains, propose the suppression with justification rather than forcing a bad abstraction.

6. For security hotspots: `mcp__sonarqube__search_security_hotspots` then `mcp__sonarqube__show_security_hotspot` on each. Review, fix, or mark as Safe with a justification (only if it really is safe).

7. Apply fixes per Step 4's TDD/commit discipline. Then **wait for the next push to re-run Sonar** — don't re-trigger CI manually unless asked.

## Step 6 — record the learning (every fix, no exceptions)

Append to `docs/ci-learnings.md` under the correct section. Each entry follows this format:

```markdown
### YYYY-MM-DD — <short title>
- **Symptom**: what CI / Sonar reported (rule key, error excerpt, job name).
- **Root cause**: the actual reason, in one sentence.
- **Fix**: what was changed (file:line is enough; the commit has the diff).
- **Rule going forward**: a single declarative sentence future-Claude can apply BEFORE writing similar code. Phrase as a do/don't.
```

Discipline:
- One entry per distinct root cause, not per file touched.
- Update an existing entry rather than duplicating it if the same rule resurfaces — bump the date, add a `Recurrence: N` count.
- Keep "Rule going forward" short and actionable. Bad: "Be careful with async". Good: "C# async methods returning Task must accept and forward a CancellationToken; CI fails with rule csharpsquid:S4462 otherwise."
- If the failure was a pipeline-infra hiccup (not a code defect), still log it under `## Infra & flakes` so we can spot patterns.

After appending, briefly tell the user what rule you added so they can sanity-check it.

## Step 7 — final report

End the session with a 3-line summary:
1. Run state (green / waiting on release gate / fixed and re-pushed / blocked).
2. What you changed, if anything (file count + commit SHA, or "no changes").
3. Any rule(s) appended to `docs/ci-learnings.md`.

## Guardrails

- Never push, force-push, re-run a workflow, cancel a workflow, or merge a PR without explicit user approval. Read-only `gh` and `mcp__sonarqube__*` calls are fine without asking.
- Never suppress a Sonar rule project-wide. Narrow inline suppression with a justification at the call site only.
- Never disable, skip, or quarantine a test to make CI green. If a test is genuinely wrong, fix it as a behaviour change with the user's OK.
- Never modify `.github/workflows/*.yml` to make a check pass. Workflow changes are deliberate, not a CI-bypass tool.
- If the fix isn't obvious within a few diagnostic steps, stop and report what you found — don't thrash.

## Recreate the ledger if missing

If `docs/ci-learnings.md` does not exist, create it with this skeleton (and nothing else):

```markdown
# CI Learnings

Durable rules derived from CI / SonarCloud failures on `Build And Deploy Lighthouse`. Append a new entry every time `/clean-ci` resolves a failure. Read this file before touching code in the related area.

## Formatting & linting

## Build & compile

## Tests

## SonarCloud — Backend (LetPeopleWork_Lighthouse)

## SonarCloud — Frontend (LetPeopleWork_Lighthouse_Frontend)

## EF migrations

## Infra & flakes
```
