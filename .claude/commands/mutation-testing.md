---
description: Run feature-scoped mutation testing for Lighthouse on either stack (Stryker.NET for the backend, StrykerJS for the frontend), interpret the score, and record the run under docs/feature/<feature>/mutation/.
allowed-tools: Bash, Read, Edit, Write, Glob, Grep
---

# /mutation-testing — prove the tests would catch the bug

Mutation testing is the `per-feature` gate in `CLAUDE.md`: after a feature or bug fix is delivered,
before finalization. **Minimum kill rate: 80 % on each stack you touched.** A stack with no changed
files does not need a run — say so explicitly rather than skipping silently.

The point is not the number. The point is the **survivor list**: every survivor is a mutation of
production code that no test noticed. Triage each one; either write the test, or record why the
survivor is acceptable.

## Fixed context (do NOT ask)

- Backend runs from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, frontend from `Lighthouse.Frontend/`.
- Configs are **committed** under `docs/feature/<feature>/mutation/`, named `stryker.<id>.backend.json`
  and `stryker.<id>.frontend.json` where `<id>` is the ADO work item (e.g. `5611`, `5621`).
- Results are written to `docs/feature/<feature>/mutation/results.md`.
- Reference pair to copy from: `docs/feature/servicenow-multi-table-work-item-types/mutation/`.

---

## Before you start

1. **Nothing else may touch the build output.** Stryker builds the solution and reruns the suite
   hundreds of times. Running `dotnet test`, `dotnet build` or a second Stryker against the same
   `bin/` **crashes the test host** mid-run (`The active test run was aborted. Reason: Test host
   process crashed`) and the failures look like real test failures. Finish one, then start the other.
2. **The tree must be green first.** A red suite makes every mutant "killed" for the wrong reason.
3. `rm -rf StrykerOutput` (backend) so you cannot misread a previous run's report.

---

## Backend — Stryker.NET

Run **from `Lighthouse.Backend/Lighthouse.Backend.Tests/`**:

```bash
dotnet stryker --config-file ../../docs/feature/<feature>/mutation/stryker.<id>.backend.json
```

### Config shape

```json
{
  "stryker-config": {
    "project": "Lighthouse.Backend.csproj",
    "solution": "../Lighthouse.sln",
    "test-projects": ["Lighthouse.Backend.Tests.csproj"],
    "mutate": [
      "**/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapper.cs"
    ],
    "thresholds": { "high": 80, "low": 70, "break": 0 },
    "coverage-analysis": "perTestInIsolation",
    "test-case-filter": "(FullyQualifiedName~ServiceNow)&FullyQualifiedName!~IntegrationTest",
    "reporters": ["progress", "json", "cleartext"],
    "concurrency": 4,
    "additional-timeout": 30000,
    "verbosity": "info"
  }
}
```

- **`mutate` takes whole-file globs, one entry per changed file.** Stryker.NET **ignores line ranges**
  (`file.cs:20-40`, `{20..40}`) — they silently widen to the whole file. Only the frontend supports
  ranges. Consequence: if you changed 27 lines of a 1100-line file, mutating it buries your change's
  score under untouched code. Either accept that and report per-file, or exclude the file and say in
  `results.md` that you did, and why the change is covered another way.
- **`test-case-filter` must cover every changed file's tests**, not just the headline ones. A file
  whose tests are filtered out reports as survivors across the board.
- `solution` is relative to the **run directory**, hence `../Lighthouse.sln`.

### Reading the log — the number that matters

Stryker prints a create-count for the whole project before scoping:

```
[INF] 13586 mutants created                 ← whole backend. NOT your scope. Ignore it.
[INF] 13207 total mutants are skipped for the above mentioned reasons
[INF] 365   total mutants will be tested    ← this is your scope. Sanity-check it.
```

If "will be tested" is in the thousands, the `mutate` globs did not match — check the path shape
(they are `**/`-prefixed and relative to the project, not the run directory) before letting it run
for an hour.

Reports land in `StrykerOutput/<timestamp>/reports/` (`mutation-report.html`, `mutation-report.json`).

---

## Frontend — StrykerJS

Run **from `Lighthouse.Frontend/`**:

```bash
pnpm exec stryker run docs/feature/<feature>/mutation/stryker.<id>.frontend.json
```

### Config shape

```json
{
  "packageManager": "pnpm",
  "testRunner": "vitest",
  "plugins": ["@stryker-mutator/vitest-runner"],
  "vitest": { "configFile": "vitest.stryker.mutation.ts" },
  "reporters": ["clear-text", "progress", "json"],
  "coverageAnalysis": "off",
  "concurrency": 2,
  "timeoutMS": 120000,
  "inPlace": true,
  "disableTypeChecks": false,
  "ignorePatterns": ["dist", "coverage", "playwright-report", "reports"],
  "mutate": ["src/models/Common/DataRetrievalSchemaDefaults.ts:56-66"],
  "thresholds": { "high": 90, "low": 80, "break": 0 },
  "jsonReporter": { "fileName": "stryker-<id>-frontend.json" },
  "tempDirName": ".stryker-tmp-<id>"
}
```

Three frontend-specific traps:

- **`inPlace: true` mutates your working tree.** StrykerJS restores on a clean exit; a crash or a
  Ctrl-C can leave mutants in `src/`. **Always `git status` after a frontend run** and revert anything
  unexpected before committing.
- **`disableTypeChecks` must stay `false`.** Left on, Stryker writes `// @ts-nocheck` into every file
  it touches — a previous run put it into 661 files.
- **Vitest needs its own narrowed config.** Stryker reruns the suite per mutant, and sweeping all
  ~282 spec files OOMs the node heap. `vitest.stryker.mutation.ts` copies the normal vitest setup but
  narrows `test.include` to just the specs covering the mutated files. Copy the one at
  `docs/feature/servicenow-multi-table-work-item-types/mutation/vitest.stryker.mutation.ts` and swap
  the `include` list. It carries `// @ts-nocheck` at the top on purpose — it is not a source file.

`mutate` **does** support line ranges here (`file.ts:56-66`), so scope tightly to the changed lines.

---

## Long runs

Runs take minutes to an hour. Background them **with output redirected to a log file**, not just to a
job handle — background job handles get reaped and you lose the whole run's output:

```bash
dotnet stryker --config-file <cfg> > /tmp/claude-1000/stryker-<id>.log 2>&1
```

then poll with `tail`. Note `setsid` and heredocs into interpreters are blocked by the shell
allowlist; write a script file if you need one.

---

## Triage and recording

For every survivor decide: **write a test**, or **justify it**. Common acceptable survivors are
equivalent mutants (the mutation cannot change observable behaviour), log-message string mutations,
and defensive guards unreachable through the public API. Everything else is a missing test.

Then write `docs/feature/<feature>/mutation/results.md`:

```markdown
# Mutation testing — <id> (<title>)

Run <date> against `main` @ `<sha>`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **88.44 %** | 504 | 454 | 45 | 5 | 8 m 51 s |
| Frontend (StrykerJS 9.6.1) | **85.71 %** | 14 | 12 | 2 | 0 | 17 s |

Configs: `stryker.<id>.backend.json`, `stryker.<id>.frontend.json`, `vitest.stryker.mutation.ts`.

## Backend
Per-file table of tested/survived, then:
### Closed by this pass   — survivors that produced new tests, with the scenario each now pins
### Accepted survivors    — each with the reason it cannot be killed meaningfully
### Not mutated           — files excluded from `mutate`, and why (e.g. 27 changed lines in a 1100-line file)
```

Commit the configs, the vitest config and `results.md` together with the feature — they are the
evidence the gate was met, and the next feature copies them rather than rediscovering the traps.

## Success criteria

- [ ] Every stack with changed files has a run; stacks without are explicitly declared N/A
- [ ] "total mutants will be tested" sanity-checked against the changed-file set
- [ ] Kill rate ≥ 80 % per stack, or a written justification for falling short
- [ ] Every survivor either killed by a new test or listed with a reason
- [ ] `git status` clean after any frontend run
- [ ] Configs + `results.md` committed under `docs/feature/<feature>/mutation/`
