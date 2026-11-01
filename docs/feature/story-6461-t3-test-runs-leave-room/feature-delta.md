# Feature: story-6461-t3-test-runs-leave-room

Story #6461 · T3 Test runs leave room for parallel sessions: local worker caps · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-43-t3-test-runs-leave-room.md`](../epic-6264-modular-monolith/slices/slice-43-t3-test-runs-leave-room.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → Test-infrastructure track → T3.
- [REF] Story Map → Pick-up groups → G0b (order T3 → T1 → T2 with FE7 → T4) and the dependency graph
  (`T3 (independent)`).
- [REF] Cross-Cutting Impact Checklist (answered for this Story below).
- [REF] Outcome KPIs + guardrails: no KPI row is touched. Guardrail: CI test behaviour and duration unchanged.
- [REF] Wave Decisions and [REF] Risks / Open questions: nothing specific to T3.
- [REF] Improvement log: nothing since 2026-11-01 that changes T3.
- ADR-270: T3 touches no bounded context (test tooling only, Platform-level). No new term, concept or edge;
  no domain event published or consumed.

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Neither stack caps its workers locally; each parallel session adds a full set (re-count below). |
| 2 Persona | PASS | `lighthouse-maintainer` and the parallel Claude sessions on the 12-core, 31 GB development machine; the CI build as the thing that must not change. |
| 3 Domain examples | PASS | Vitest's 11 thread workers; NUnit's one-fixture-worker-per-core; the resident `VBCSCompiler`; 2026-11-01 a worktree session's backend run alongside this one. |
| 4 UAT G/W/T | PASS | The brief's three AC; DISTILL writes the scenarios. |
| 5 AC derived from UAT | PASS | 1:1 with the brief's AC. |
| 6 Right-sized | PASS | Size S; likely one or two slices (one per stack). |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | None (brief and graph agree). |
| 9 Outcome measurable | PASS | Wall time and peak memory per stack at both worker settings, same machine. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-01)

| Brief says | Today |
|---|---|
| `vitest.config.ts`: `pool: "threads"`, `maxWorkers: undefined` → 11 workers on 12 cores | **Confirmed.** Vitest `^5.0.5`; `pool: "threads"`, `isolate: true`, `maxWorkers: undefined`, `fileParallelism: true`. `nproc` = 12. |
| NUnit `[assembly: Parallelizable(ParallelScope.Fixtures)]`, default `LevelOfParallelism` | **Confirmed**, `Lighthouse.Backend.Tests/GlobalUsings.cs:3`. No `LevelOfParallelism` or `NumberOfTestWorkers` anywhere, so one fixture worker per core (12). |
| runsettings | `Lighthouse.Backend.Tests/lighthouse.runsettings` exists (TZ pin only), wired through `<RunSettingsFilePath>` so a bare `dotnet test` reads it. A natural home for a worker setting. |
| CI uses the full runner | CI passes `RunConfiguration.MaxCpuCount=0` (process-level; there is one test assembly, so it does not change NUnit's worker count). `ubuntu-latest` runners have 4 cores, so "full count" in CI = 4. GitHub sets `CI=true`. |
| `VBCSCompiler` resident at about 5.3 GB | **2.4 GB** at the time of the re-count (after a build ~6 min earlier); it varies with what was built. |
| "Reduce memory usage on CI" comment beside `bail: 1` | **Still there** (`vitest.config.ts`, CI spread block). |
| Docs for the variable and `dotnet build-server shutdown` | **None yet**: no mention in CLAUDE.md, `.claude/` or `docs/`. |
| Frontend test files (FE7 brief: 577) | **576** tracked `*.test.ts(x)` files. |
| `BackendTestParallelizationGuardTest` | 7 allow-listed serial fixtures. T3 does not change parallel scope, only the worker count. |

## Wave: PICK-UP / [REF] Cross-cutting re-check

- RBAC: N/A, because no code path or authorization rule changes; only test-runner configuration.
- Lighthouse-Clients: N/A, because no route, DTO or serialized response changes.
- Website: N/A, because nothing user-visible changes.
- Usage-data event: provisionally N/A, because it is behaviour-free test tooling nobody uses in the product;
  DEVOPS confirms.
- UI sketch: N/A, because nothing a user sees changes.
- Terminology: N/A, because no user-facing words change.

## Wave: PICK-UP / Open questions for DESIGN

1. NUnit: an assembly attribute cannot read an environment variable. Options: runsettings
   `NUnit.NumberOfTestWorkers` (does `dotnet test` expand env vars in runsettings?), a CLI pass-through, or an
   `MSBuild` property that writes the runsettings value. DESIGN picks one that a bare `dotnet test` honours.
2. Does "local default" mean half the cores (6), or the measured knee? The measurement decides.
3. The backend peak memory is dominated by T1's leak (up to 18 GB). The backend default chosen now is
   re-measured after T1 lands; this item records that as a follow-up rather than waiting.
4. Playwright (`Lighthouse.EndToEndTests`) is outside the brief ("both stacks" = Vitest + NUnit). Confirm
   at DESIGN that it stays out.

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free: no test added, removed or changed in what it asserts; CI worker count and duration
  unchanged (shown in the CI logs).
- [ ] Ratchet in the same change, if one applies (for example a test pinning that CI uses the full count),
  or "N/A, because …" stated at DESIGN.
- [ ] Moves separate from edits (N/A expected: configuration only; stated at DESIGN).
- [ ] Re-count before designing (done above).
- [ ] Mutation last, on frozen code: N/A expected (configuration, no production or helper logic); stated at
  DESIGN.
- [ ] Architecture docs in the same change: CLAUDE.md quality gates and/or `/run-dev`; ARCHITECTURE.md
  only if it describes test running.
- [ ] Payoff proved: wall time and peak memory at both settings, both stacks, same machine, quiet slot.
- [ ] No user-facing release notes.
- [ ] Cross-cutting re-check answered (above).
- [ ] Terminology: N/A (no user-facing names).

## Wave: DESIGN / [REF] Scope and evidence

Propose mode. Test tooling only, behaviour-free. Platform-level; ADR-270: no bounded context touched, no new term,
concept, edge or domain event. C4 is N/A, because no runtime topology changes (nothing deployed or shipped changes).
UI sketch, loading look, failure look: N/A, because nothing a user sees changes.

Who reads the worker count, before and after:

```
BEFORE                                          AFTER
vitest.config.ts  maxWorkers: undefined         vitest.config.ts -> testWorkers.ts (env, CI, cores)
   -> Vitest default = cores-1 (11 here, 3 CI)     -> CI unset: undefined (same as before)
                                                   -> local: LIGHTHOUSE_TEST_WORKERS or 50% of cores
Tests.csproj  (nothing)                         Tests.csproj  VSTestCLIRunSettings property
   -> NUnit default = cores (12 here, 4 CI)        -> `dotnet test` hands NUnit.NumberOfTestWorkers=N to the adapter
                                                   -> CI unset: property not set (same as before)
```

Evidence gathered in this DESIGN (2026-11-01, `dotnet test --no-build`, filter
`BackendTestParallelizationGuardTest|ForecastControllerCalendarDayAnchorTest`, 4 tests, ~5 s; no full run):

| Question | Result |
|---|---|
| Does a `dotnet test` pass-through reach the adapter? | Yes. `-- NUnit.NumberOfTestWorkers=2 NUnit.Verbosity=5` logs `Setting: NumberOfTestWorkers = 2`. Unset logs `= ` and `-1` (adapter default, which means one worker per core). |
| Does the TZ pin survive? | Yes. `TestHost_RunsUnderThePinnedInstanceTimeZone` passed with the setting applied. The runsettings file keeps being read. |
| Does the **MSBuild property** `VSTestCLIRunSettings` (not a CLI argument) reach the adapter? | Yes. Setting it from the environment (MSBuild exposes env vars as properties, the same evaluation path as a csproj assignment) with `NUnit.NumberOfTestWorkers=3;NUnit.Verbosity=5` logs `NumberOfTestWorkers = 3` on a bare `dotnet test`. `/usr/share/dotnet/sdk/10.0.114/Microsoft.TestPlatform.targets` passes `$(VSTestCLIRunSettings)` to the VSTest task. |
| Does a CLI `--` argument replace the property? | Yes: CLI arguments become global MSBuild properties, which beat a csproj assignment. So the CI step `-- RunConfiguration.MaxCpuCount=0` drops the csproj value on its own, and the `CI` condition is a second guard. A contributor passing their own `-- ...` also loses the cap (documented). |
| Do runsettings files expand env vars? | No evidence that they do (the adapter reads literal values); not relied on. |
| Candidate MSBuild expression | Evaluated in a scratch project: unset -> `NUnit.NumberOfTestWorkers=6` (12 cores); `CI=true` -> empty (property not set); `CI=true` + override 2 -> `=2`; override 4 -> `=4`. |
| Vitest 5.0.5 `maxWorkers` | Accepts a number or a string ending in `%` (`resolveMaxWorkers`: `round(pct/100 * availableParallelism)`, clamped to 1..cores); other strings go through `Number()` (garbage becomes `NaN`). `undefined` means `cores - 1` in a run, `cores / 2` in watch. Vitest also reads `VITEST_MAX_WORKERS` itself and it beats `maxWorkers` in the config; the CLI `--maxWorkers` beats both. |
| Stryker | Frontend: none of the 46 `vitest.stryker.*` configs imports `vitest.config.ts` (they are standalone), so they keep Vitest's default. Backend: Stryker configs set their own `concurrency` (2, 4 or 6); how Stryker.NET's own VSTest runner treats `VSTestCLIRunSettings` is **not verified** (see open questions). |
| Playwright | `Lighthouse.EndToEndTests/playwright.config.ts` already pins `workers: 1`. |

## Wave: DESIGN / [REF] Decisions

### D1. One variable, `LIGHTHOUSE_TEST_WORKERS`, and how the local default is computed

| Option | Local default | Trade-offs |
|---|---|---|
| A. Fraction of cores (recommended) | `max(1, floor(cores * 0.5))`, a named constant per stack | Scales with the machine (6 on 12 cores, 2 on 4). Same rule on both stacks. The fraction is the single number the measurements pick. |
| B. Fixed number | e.g. 6 | Simplest to document, but 6 is the whole runner on a 6-core laptop and a quarter on a 24-core box. |
| C. Cores minus a reserve | `cores - 4` | Models "leave room" literally, but the room needed grows with the session count, not the core count; hard to explain. |

**Recommended: A.** Placeholder `0.5` until the measurement protocol picks the fraction (per stack if they differ).

Semantics, identical on both stacks:

| Situation | Worker count |
|---|---|
| `LIGHTHOUSE_TEST_WORKERS=N` set (any environment) | N. An explicit value always wins, including in CI. |
| Unset and `CI` is `true` | Nothing configured: the tool's own default, so CI is **byte-for-byte what it is today** (Vitest `cores-1`, NUnit `cores`; 3 and 4 on `ubuntu-latest`). |
| Unset, not CI | The local default (fraction of cores). |

"Full count in CI" in the brief is read as "unchanged", not "an explicit 4": Vitest's default on a 4-core runner is 3, so
writing 4 would change CI.

### D2. Vitest: `maxWorkers` from the variable

| Option | Trade-offs |
|---|---|
| A. Small pure resolver `src/tests/testWorkers.ts`, imported by `vitest.config.ts` (recommended) | Same pattern as the existing `./src/tests/ExpectedTestFailure.ts` import. Type-checked by `tsc -b` and Biome (the config file itself is neither) and unit-testable. One new file plus its test. |
| B. Inline expression in `vitest.config.ts` | No new file, but the CI/override/default branches are untestable and unchecked. |
| C. Set `VITEST_MAX_WORKERS` from `package.json` scripts (`cross-env`) | Uses Vitest's own variable, but cannot express "CI unset" cleanly, only covers `pnpm test`, and still needs a default. Rejected. |

**Recommended: A.** The resolver takes `(env, availableParallelism)` and returns a number or `undefined`; it
returns an integer (not a `"50%"` string) so the logged count and the test are exact. An invalid value (not a positive
integer) throws with a message naming the variable, instead of Vitest silently turning it into `NaN`.
The 576 test files keep running with `fileParallelism: true`, `pool: "threads"`, `isolate: true` unchanged.

### D3. NUnit: how the count reaches the adapter

| Option | Works for bare `dotnet test` | Env-driven | CI unchanged | Trade-offs |
|---|---|---|---|---|
| A. `VSTestCLIRunSettings` MSBuild property in the csproj, built from the variable and `CI` (recommended) | Yes (proved above) | Yes | Yes: property unset in CI, and CI's `--` argument overrides it anyway | One `PropertyGroup` in the csproj. Not honoured by IDE runners (they do not evaluate this property): they stay uncapped, documented. A contributor's own `-- ...` arguments replace it. |
| B. Fixed `<NUnit><NumberOfTestWorkers>` in `lighthouse.runsettings` | Yes | No (fixed number, no env expansion evidence) | No: applies in CI too unless the CI command changes | Covers IDEs, but a CI workflow edit is needed and the count is hard-coded. Rejected. |
| C. A second `ci.runsettings`, `RunSettingsFilePath` chosen by `CI` | Yes | Only two values, no override | Yes | Duplicates the TZ pin in two files (they must stay equal), and has no override. Rejected. |
| D. `LevelOfParallelism` attribute read at startup | n/a | An attribute cannot read the environment; a `[SetUpFixture]` is too late for worker creation | n/a | Rejected. |
| E. CLI pass-through only (document `-- NUnit.NumberOfTestWorkers=6`) | Only if typed every time | No | Yes | No default; fails the brief. Rejected as the mechanism; still the way to override for one run. |

**Recommended: A.** Proposed csproj fragment (expression verified in a scratch project; DELIVER re-verifies in the real one
with `dotnet msbuild -getProperty:VSTestCLIRunSettings` for the four cases):

```xml
<!-- Caps NUnit's workers on a local run so parallel sessions on one machine do not each take every core.
     CI leaves the property unset and keeps NUnit's default of one worker per core. An explicit
     LIGHTHOUSE_TEST_WORKERS wins everywhere. Arguments after `dotnet test --` replace this property. -->
<PropertyGroup Condition="'$(LIGHTHOUSE_TEST_WORKERS)' != '' Or '$(CI)' != 'true'">
  <LighthouseTestWorkers>$(LIGHTHOUSE_TEST_WORKERS)</LighthouseTestWorkers>
  <LighthouseTestWorkers Condition="'$(LighthouseTestWorkers)' == ''">$([System.Math]::Max(1, $([System.Math]::Floor($([MSBuild]::Multiply($([System.Environment]::ProcessorCount), 0.5))))))</LighthouseTestWorkers>
  <VSTestCLIRunSettings>NUnit.NumberOfTestWorkers=$(LighthouseTestWorkers)</VSTestCLIRunSettings>
</PropertyGroup>
```

The `[assembly: Parallelizable(ParallelScope.Fixtures)]` scope and the 7 allow-listed serial fixtures are untouched (the
parallelization guard keeps passing); only the number of workers changes. A non-integer override would reach NUnit
as-is; the adapter's reaction is to be checked in DELIVER (open question 3) and, if unfriendly, validated with an MSBuild
`Error` condition.

### D4. Ratchet

| Stack | Decision |
|---|---|
| Frontend | **Yes**, a unit test of the resolver (`src/tests/testWorkers.test.ts`): CI and unset -> `undefined`; CI and override -> the override; local unset -> `floor(cores * fraction)`, never below 1; invalid override -> throws. This pins "CI behaviour is the tool default" so a later edit cannot cap CI silently. |
| Backend | **N/A, because** the logic is a declarative MSBuild condition, and no NUnit test can see the engine setting reliably (a worker-id probe would be flaky and test the framework). The csproj comment says why the condition exists. The four-case `dotnet msbuild -getProperty` check is run in DELIVER and its output pasted here as the evidence. |
| ADR-268 | **N/A, because** it governs offender lists that shrink; there is no offender list here. |

### D5. Measurement protocol

See the next section. Wall time and peak memory, per stack, at full count and at the candidate defaults, alternating runs.

### D6. Documentation

| Where | What |
|---|---|
| `CLAUDE.md` > Quality Gates (recommended home; the file contributors and agents read first) | One short block: the variable, the local default and CI rule, the two overrides (`LIGHTHOUSE_TEST_WORKERS=N pnpm test`, `LIGHTHOUSE_TEST_WORKERS=N dotnet test ...`), the limitation that arguments after `--` and IDE runners bypass the cap, and `dotnet build-server shutdown` (releases the resident Roslyn compiler server; it is started again by the next build). |
| `.claude/commands/run-dev.md` | **N/A, because** it is about starting the dev server and key rings, not test runs; a duplicate would drift. CLAUDE.md is the single place. |
| `docs/ci-learnings.md` | **N/A, because** it holds only rules no tool can enforce; this is configuration. |
| `ARCHITECTURE.md`, `docs/product/architecture/brief.md` | **N/A, because** neither describes test running or worker counts (checked by search). |
| `vitest.config.ts` | Delete the "Reduce memory usage on CI" comment beside `bail: 1`; `bail: 1` stays as is (CI behaviour unchanged). It is fail-fast, not a memory saver. |

### D7. ADR

Not warranted: a worker-count knob for a test runner is neither hard to reverse nor structural. The CLAUDE.md note and
the csproj comment carry the reasoning.

## Wave: DESIGN / [REF] Component decomposition

| File | Change |
|---|---|
| `Lighthouse.Frontend/src/tests/testWorkers.ts` | NEW: pure resolver `(env, cores) -> number | undefined`, plus the default-fraction constant. |
| `Lighthouse.Frontend/src/tests/testWorkers.test.ts` | NEW: the ratchet (D4). |
| `Lighthouse.Frontend/vitest.config.ts` | MODIFIED: `maxWorkers: resolveTestWorkers(process.env, availableParallelism())`; remove the misleading comment. |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj` | MODIFIED: one `PropertyGroup` (D3). |
| `CLAUDE.md` | MODIFIED: Quality Gates block (D6). |
| `.github/workflows/*`, `lighthouse.runsettings`, `GlobalUsings.cs`, `package.json` scripts, Stryker configs, Playwright | UNCHANGED. |

## Wave: DESIGN / [REF] Technology choices

Only what is installed: Vitest 5.0.5 (`maxWorkers`), `node:os` `availableParallelism()` (the same call Vitest uses),
MSBuild property functions, NUnit3TestAdapter 6.3.1's `NUnit.NumberOfTestWorkers` setting, SDK 10.0.114's
`VSTestCLIRunSettings`. No new package, tool, script or workflow.

## Wave: DESIGN / [REF] Reuse Analysis

| Need | Existing | Decision |
|---|---|---|
| Frontend worker setting | `vitest.config.ts` `maxWorkers: undefined` | EXTEND |
| Frontend logic home | `src/tests/` helper precedent (`ExpectedTestFailure.ts` imported by the config) | **CREATE NEW** `testWorkers.ts` + test: no existing module is type-checked, unit-tested and importable from the config; inlining (D2-B) leaves the CI rule unpinned. |
| Backend worker setting | `RunSettingsFilePath` property in the test csproj, `lighthouse.runsettings` | EXTEND the csproj (D3-A). The runsettings file stays TZ-only because it cannot take an env value. |
| CI detection | `CI` env var (GitHub sets it; the frontend action also sets it explicitly) | REUSE, no new switch |
| Docs | CLAUDE.md Quality Gates | EXTEND |
| Measurement script | none in the repo for resource sampling (`Scripts/test-timings` measures per-test times from CI output) | Keep the sampler in this workspace (`measurement/`), not in `Scripts/`: a one-off. CREATE NEW there only, justified: nothing existing samples process-tree memory. |

## Wave: DESIGN / [REF] Measurement protocol

Run later by the main session in a quiet slot (no other session, no `dotnet`/`node` busy; record `uptime` and
`free -m` first). Not run in DESIGN.

- **Settings**: full (the CI-equivalent unset behaviour: Vitest 11 workers, NUnit 12), half (6), quarter (3). Candidate
  fractions are chosen from the three; add one more point only if the knee is between them.
- **Rounds**: n = 3 per setting per stack, order alternated (full, half, quarter, full, half, quarter, ...) so thermal and cache
  drift hit every setting alike. Drop nothing; report median and range.
- **Commands**: frontend `LIGHTHOUSE_TEST_WORKERS=<n> pnpm test` (full = unset with `CI=true`, which is the CI behaviour);
  backend: build once, `dotnet build-server shutdown` before each run so the compiler server is not counted, then
  `dotnet test --no-build --filter "TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration"`
  with `LIGHTHOUSE_TEST_WORKERS=<n>` (full = `CI=true` unset).
- **Capture per run**: wall time; peak sum of RSS over the run's process tree (all `node`/vitest workers; `testhost` and
  its children) sampled at 1 Hz with `ps`; minimum `MemAvailable` from `/proc/meminfo` at the same cadence; failures
  (a different failure count between settings invalidates the run). `/usr/bin/time -v` is not enough (it reports one process).
- **Report** in this file: one table per stack, median wall time, median peak tree RSS, minimum available memory, and
  the chosen fraction with the rule: the smallest worker count whose median wall time is within about 25% of full, or
  the knee if there is one (the exact tolerance is the maintainer's call at the review).
- **Follow-ups recorded, not done**: the backend peak is dominated by the testhost leak (T1: 2 -> 18 GB in 8 minutes),
  so the backend fraction is **re-measured after T1** and adjusted if the leak's removal moves the knee. The frontend
  number stays after FE7 only if FE7 changes the file count markedly.
- **Estimated duration**: 2 stacks x 3 settings x 3 runs = 18 runs. Full-count durations are unmeasured today; assuming
  about 3-6 minutes for the frontend and 4-8 for the backend (smaller worker counts run longer), plus about a minute
  between runs: **roughly 1.5 to 2 hours**, plus about 15 minutes for one dry run of the sampler. n can be cut to 2
  for the quarter setting to save about 20 minutes.

## Wave: DESIGN / [REF] Slices and commit plan

One slice, three steps, each under a day (the whole thing is a half-day of work plus the measurement slot):

| Step | Delivers | Commit |
|---|---|---|
| 01-01 | Frontend: resolver + its test + `vitest.config.ts` wiring + comment removal | `chore(test-infra): cap local vitest workers, leave CI on the default` |
| 01-02 | Backend: csproj `PropertyGroup`, four-case `getProperty` evidence | `chore(test-infra): cap local NUnit workers through the test project` |
| 01-03 | CLAUDE.md block; later the measured fraction replaces the placeholder in both stacks and the results table lands here | `docs(test-infra): document the worker cap and build-server shutdown`, then `chore(test-infra): set the measured local worker fraction` |

Steps 01-01 and 01-02 are independent. No moves; configuration only, so "moves separate from edits" is N/A.
Mutation testing: N/A, because there is no production or shared-helper logic; the resolver is test tooling that its
own test pins branch by branch. If the maintainer prefers a Stryker pass on `testWorkers.ts` (one file, a handful of
mutants) it is cheap; flagged below. Order within DELIVER: placeholder `0.5` ships first, the measured value is a
one-constant follow-up in both stacks (the fraction lives in two languages, a duplicated number each place
points at the other).

## Wave: DESIGN / [REF] Answers to the pick-up open questions

1. **NUnit mechanism**: the `VSTestCLIRunSettings` property from the csproj (D3-A). Runsettings do not expand env vars
   (no evidence they do), a CLI pass-through has no default.
2. **Half the cores or the knee**: ship half (a fraction constant) as the placeholder; the measurement protocol may change
   the fraction, once, in one commit.
3. **Backend peak dominated by T1**: recorded as a follow-up of the protocol (re-measure after T1).
4. **Playwright stays out**: confirmed. It is not part of "both stacks" and already runs with `workers: 1`.

## Wave: DESIGN / Open questions for DISTILL / DELIVER

1. DISTILL: the acceptance check for "shown by the worker count in the logs". Vitest prints no worker count, so the
   frontend evidence is the resolver test plus (optionally) one `console.info` line when an override or the local
   default applies; the backend evidence is `dotnet msbuild -getProperty` plus the adapter's `Setting: NumberOfTestWorkers`
   line, visible only with `-v n` and `NUnit.Verbosity=5` set through the property (a `--` argument would replace it).
   Decide whether the echo line is wanted; recommended: no echo, evidence by test and command output.
2. DELIVER: confirm Stryker.NET (both its own `concurrency` and its VSTest runner) is not altered by the property; a
   dry run on the smallest existing backend Stryker config before and after. Unverified here.
3. DELIVER: behaviour of a non-numeric `LIGHTHOUSE_TEST_WORKERS` through the NUnit adapter; add an MSBuild `Error` if
   it fails obscurely.
4. DELIVER: confirm no other workflow runs the backend test project with `CI` unset (all `ci_*` workflows run on GitHub,
   which sets it); a grep of `dotnet test` and `vitest` call sites in `.github/` and `.githooks/` (the hooks run no tests).
5. Maintainer: Stryker pass on `testWorkers.ts` (default: N/A, as above).
6. Peer review: the platform-architect-reviewer could not be invoked from this run (no Task tool); the lead session
   dispatches it, with `model` set per the rigor profile.

## Wave: DESIGN / [REF] Changed Assumptions

- The brief and the inherited baseline say "CI uses the full count" and "full count = 4 on `ubuntu-latest`". The real
  CI behaviour is Vitest `cores-1` = 3 and NUnit `cores` = 4. The design keeps CI **unchanged by configuring nothing**, which
  is the only way to make that literally true.
- The brief offers "runsettings `NumberOfTestWorkers`, or an assembly-level `LevelOfParallelism` read from the
  variable". Neither is workable (a runsettings file cannot take the env value; an attribute cannot read it). The
  csproj `VSTestCLIRunSettings` property replaces both.
- The brief's acceptance criterion "shown by the worker count in the logs" is not satisfiable as written for Vitest
  (nothing logs it); see open question 1.
