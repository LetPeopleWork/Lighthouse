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
| 3 Domain examples | PASS | Vitest's 11 thread workers (derived from its default, cores-1); NUnit's one-fixture-worker-per-core; the resident `VBCSCompiler`; 2026-11-01 a worktree session's backend run alongside this one. |
| 4 UAT G/W/T | DEFERRED (Epic catalogue rule: full G/W/T written at each Story's DISTILL, Epic DoR item 4) | The brief's three AC; DISTILL writes the scenarios. |
| 5 AC derived from UAT | PASS against the re-scoped AC1 (see brief, revised 2026-11-01) | 1:1 with the brief's AC. |
| 6 Right-sized | PASS | Size S; one slice, three steps (DESIGN). |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | None hard; T1 soft for the backend memory figure. |
| 9 Outcome measurable | PASS | Wall time and peak memory per stack at both worker settings, same machine. |

Slice composition: waived by D-6264-5 (Epic-level decision), because the Story is a single infrastructure slice.
JTBD check: infrastructure-only, no end-user job; the rationale is in the Epic's T3 entry (`infrastructure_rationale`).

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-01)

| Brief says | Today |
|---|---|
| `vitest.config.ts`: `pool: "threads"`, `maxWorkers: undefined` → 11 workers on 12 cores | **Confirmed.** Vitest `^5.0.5`; `pool: "threads"`, `isolate: true`, `maxWorkers: undefined`, `fileParallelism: true`. `nproc` = 12 (command: `nproc`). The 11 workers is DERIVED from Vitest's default (cores-1), not observed. |
| NUnit `[assembly: Parallelizable(ParallelScope.Fixtures)]`, default `LevelOfParallelism` | **Confirmed**, `Lighthouse.Backend.Tests/GlobalUsings.cs:3`. No `LevelOfParallelism` or `NumberOfTestWorkers` anywhere, so one fixture worker per core (12). |
| runsettings | `Lighthouse.Backend.Tests/lighthouse.runsettings` exists (TZ pin only), wired through `<RunSettingsFilePath>` so a bare `dotnet test` reads it. A natural home for a worker setting. |
| CI uses the full runner | CI passes `RunConfiguration.MaxCpuCount=0` (process-level; there is one test assembly, so it does not change NUnit's worker count). `ubuntu-latest` runners have 4 cores. CI uses each tool's own default (Vitest 5's cores-1 gives 3; NUnit's one per core gives 4; 4 cores is GitHub's documented size for public-repository `ubuntu-latest` runners), not an explicit "full count" of 4. GitHub sets `CI=true`. |
| `VBCSCompiler` resident at about 5.3 GB | **~2.4 GB** (`ps -eo rss,cmd | grep VBCSCompiler`, at 14:4x on 2026-11-01) at the time of the re-count (after a build ~6 min earlier); it varies with what was built. |
| "Reduce memory usage on CI" comment beside `bail: 1` | **Still there** (`vitest.config.ts`, CI spread block). |
| Docs for the variable and `dotnet build-server shutdown` | **None yet**: no mention in CLAUDE.md, `.claude/` or `docs/`. |
| Frontend test files (FE7 brief: 577) | **576** by `git grep -c` over tracked `*.test.ts(x)` at pick-up, before `testWorkers.test.ts` was added. |
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
- [ ] Moves separate from edits (N/A expected: configuration plus one new pure helper file, additive, no moves; stated at DESIGN).
- [ ] Re-count before designing (done above).
- [ ] Mutation last, on frozen code: a StrykerJS run on `src/tests/testWorkers.ts` in the final gate, kill rate >= 80%
  (the resolver is branching logic; configuration plus one new pure helper file, additive, no moves). The csproj is
  declarative: N/A. Decided in the DESIGN revision (D-R5).
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
| Stryker | Frontend: none of the 98 `vitest.stryker*` configs under `docs/feature/*/mutation/` imports `vitest.config.ts` (re-counted 2026-11-01 by glob and grep; each Stryker config selects its own via `configFile`), so they keep Vitest's default. Backend: the `stryker*.json` files in `Lighthouse.Backend.Tests/` set their own `concurrency` (values found: 2 in 1 file, 4 in 23 files, 6 in 5 files); how Stryker.NET's own VSTest runner treats `VSTestCLIRunSettings` is **not verified** (see open questions). |
| Playwright | `Lighthouse.EndToEndTests/playwright.config.ts` already pins `workers: 1`. |

## Wave: DESIGN / [REF] Decisions

### D1. One variable, `LIGHTHOUSE_TEST_WORKERS`, and how the local default is computed

| Option | Local default | Trade-offs |
|---|---|---|
| A. Fraction of cores (recommended) | `max(1, floor(cores * 0.5))`, a named constant per stack | Scales with the machine (6 on 12 cores, 2 on 4). Same rule on both stacks. The fraction is the single number the measurements pick. |
| B. Fixed number | e.g. 6 | Simplest to document, but 6 is the whole runner on a 6-core laptop and a quarter on a 24-core box. |
| C. Cores minus a reserve | `cores - 4` | Models "leave room" literally, but the room needed grows with the session count, not the core count; hard to explain. |

**Recommended: A.** Placeholder `0.5` until the measurement protocol picks the fraction (per stack if they differ).

Semantics, identical on both stacks for the variable (the tool-specific overrides that beat it are listed in D6):

| Situation | Worker count |
|---|---|
| `LIGHTHOUSE_TEST_WORKERS=N` set to a valid value (`^[1-9][0-9]*$`; any environment) | N. An explicit value always wins, including in CI, and is not clamped to the core count (D-R4). |
| Empty `LIGHTHOUSE_TEST_WORKERS=` | Treated as unset (D-R1), both stacks. |
| Any other value | Frontend: the resolver throws naming the variable. Backend: an MSBuild `<Error>` naming the variable (D-R2). |
| Unset (or empty) and `CI` equals `true`, case-insensitively (D-R3) | Nothing configured: the tool's own default, so CI is **byte-for-byte what it is today** (Vitest `cores-1`, NUnit `cores`; 3 and 4 on `ubuntu-latest`). |
| Unset, not CI | The local default (fraction of cores). |

"Full count in CI" in the brief is read as "unchanged", not "an explicit 4": Vitest's default on a 4-core runner is 3, so
writing 4 would change CI.

CI detection is case-insensitive equality with `true` on both stacks (D-R3). `vitest.config.ts`'s existing truthy
`process.env.CI &&` bail block is untouched; the two tests differ only for `CI=false`, which is pre-existing.

### D2. Vitest: `maxWorkers` from the variable

| Option | Trade-offs |
|---|---|
| A. Small pure resolver `src/tests/testWorkers.ts`, imported by `vitest.config.ts` (recommended) | Same pattern as the existing `./src/tests/ExpectedTestFailure.ts` import. Type-checked by `tsc -b` and Biome (the config file itself is neither) and unit-testable. One new file plus its test. |
| B. Inline expression in `vitest.config.ts` | No new file, but the CI/override/default branches are untestable and unchecked. |
| C. Set `VITEST_MAX_WORKERS` from `package.json` scripts (`cross-env`) | Uses Vitest's own variable, but cannot express "CI unset" cleanly, only covers `pnpm test`, and still needs a default. Rejected. |

**Recommended: A.** The resolver takes `(env, availableParallelism)` and returns a number or `undefined`; it
returns an integer (not a `"50%"` string) so the logged count and the test are exact. An invalid value (not a positive
integer; valid means `^[1-9][0-9]*$`, no trimming, no leading plus, no percentage; empty means unset) throws with a message naming the variable, instead of Vitest silently turning it into `NaN`.
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
<!-- Rejects an unusable explicit value with a message that names the variable, instead of a cryptic adapter failure. -->
<Target Name="ValidateTestWorkers" BeforeTargets="VSTest" Condition="'$(LIGHTHOUSE_TEST_WORKERS)' != '' And !$([System.Text.RegularExpressions.Regex]::IsMatch('$(LIGHTHOUSE_TEST_WORKERS)', '^[1-9][0-9]*$'))">
  <Error Text="LIGHTHOUSE_TEST_WORKERS must be a positive whole number, got '$(LIGHTHOUSE_TEST_WORKERS)'." />
</Target>
```

The `[assembly: Parallelizable(ParallelScope.Fixtures)]` scope and the 7 allow-listed serial fixtures are untouched (the
parallelization guard keeps passing); only the number of workers changes. An invalid override is rejected by the
`ValidateTestWorkers` target (D-R2; open question 3 is closed). DELIVER verifies that the target actually runs on
`dotnet test` (with and without `--no-build`) and picks the hook point that does: the `VSTest` target, or a
property-evaluation-time check if that target does not fire.

### D4. Ratchet

| Stack | Decision |
|---|---|
| Frontend | **Yes**, a unit test of the resolver (`src/tests/testWorkers.test.ts`): CI and unset -> `undefined`; CI and override -> the override; local unset -> `floor(cores * fraction)`, never below 1; empty -> unset; `CI=TRUE` counts as CI; an invalid override (not `^[1-9][0-9]*$`) -> throws. This pins "CI behaviour is the tool default" so a later edit cannot cap CI silently. |
| Backend | **N/A, because** the logic is a declarative MSBuild condition, and no NUnit test can see the engine setting reliably (a worker-id probe would be flaky and test the framework). The csproj comment says why the condition exists. A regression guard is also N/A (D-R6), because CI is structurally protected twice: both CI `dotnet test` steps pass `--` runsettings arguments, which replace `VSTestCLIRunSettings` (proven by the DESIGN experiment), and the property is not set under `CI=true`; a regression of the local cap only affects local runs and is visible in the next local run, which is accepted. The four-case `dotnet msbuild -getProperty` check is run in DELIVER and its output pasted here as the evidence. |
| ADR-268 | **N/A, because** it governs offender lists that shrink; there is no offender list here. |

### D5. Measurement protocol

See the next section. Wall time and peak memory, per stack, at full count and at the candidate defaults, alternating runs.

### D6. Documentation

| Where | What |
|---|---|
| `CLAUDE.md` > Quality Gates (recommended home; the file contributors and agents read first) | One short block: the variable, the local default and CI rule, the two overrides (`LIGHTHOUSE_TEST_WORKERS=N pnpm test`, `LIGHTHOUSE_TEST_WORKERS=N dotnet test ...`), the limitations (`VITEST_MAX_WORKERS` and the CLI `--maxWorkers` override the cap on the frontend; a contributor's own `--` arguments replace it on the backend; NUnit IDE runners bypass it while Vitest IDE runs honour it), that the fraction lives in two places (the `testWorkers.ts` constant and the csproj) and is kept in step by hand (accepted, D-R12), and `dotnet build-server shutdown` (releases the resident Roslyn compiler server; it is started again by the next build). |
| `.claude/commands/run-dev.md` | **N/A, because** it is about starting the dev server and key rings, not test runs; a duplicate would drift. CLAUDE.md is the single place. |
| `docs/ci-learnings.md` | **N/A, because** it holds only rules no tool can enforce; this is configuration. |
| `ARCHITECTURE.md`, `docs/product/architecture/brief.md` | **N/A, because** neither describes test running or worker counts (checked by search). |
| `vitest.config.ts` | Delete the "Reduce memory usage on CI" comment beside `bail: 1`; `bail: 1` stays as is (CI behaviour unchanged). It is fail-fast, not a memory saver. |

### D7. ADR

Not warranted: a worker-count knob for a test runner is neither hard to reverse nor structural. The CLAUDE.md note and
the csproj comment carry the reasoning. ADR-074 (backend test fixture parallelization isolation) was consulted: the cap
changes the worker count only, not fixture scope or isolation, so ADR-074 is not amended (D-R11).

## Wave: DESIGN / [REF] Revision after DISTILL review (2026-11-01)

Decisions taken by the lead session after the four-reviewer DISTILL gate. They are applied throughout the DESIGN and
DEVOPS sections above and below; where an earlier sentence disagrees, this list wins.

| Id | Decision |
|---|---|
| D-R1 | An empty `LIGHTHOUSE_TEST_WORKERS` means unset, on both stacks. |
| D-R2 | A valid value matches `^[1-9][0-9]*$` on both stacks (no trimming, no leading plus, no percentage). Invalid: the frontend resolver throws naming the variable; the backend fails with an MSBuild `<Error>` naming the variable (target in the D3 fragment). DELIVER verifies the target actually runs on `dotnet test` and picks the hook point that does (the `VSTest` target or a property-evaluation-time check). Open question 3 is closed by this. |
| D-R3 | CI detection is case-insensitive equality with `true` on both stacks (MSBuild's comparison already is). `vitest.config.ts`'s existing truthy `process.env.CI &&` bail block is left untouched (behaviour-free); the difference between the two tests only matters for `CI=false` and is pre-existing. |
| D-R4 | No clamp of an explicit value above the core count: kept, it is the developer's explicit choice. |
| D-R5 | Mutation: the resolver IS branching logic, so CLAUDE.md's per-feature gate applies. A StrykerJS run on `src/tests/testWorkers.ts` belongs in the final gate (on frozen code), kill rate >= 80%. The change is "configuration plus one new pure helper file (additive, no moves)", not "configuration only"; "moves separate from edits" stays N/A for that reason. The csproj stays N/A (declarative). |
| D-R6 | Backend regression guard: N/A, because CI is structurally protected twice: both CI `dotnet test` steps pass `--` runsettings arguments, which replace `VSTestCLIRunSettings` (proven by the DESIGN experiment), and the property is not set under `CI=true`. A regression of the local cap only affects local runs and is visible in the next local run; accepted. The four-case `getProperty` paste stays as the evidence. |
| D-R7 | Measurement validity, see "Protocol as run" in the measurement section. |
| D-R8 | IDE runners: Vitest IDE runs load `vitest.config.ts` and are therefore capped by the resolver; NUnit IDE runners (Rider, Visual Studio) do not evaluate the property and stay uncapped. The environment `local-uncapped-default` is renamed `local-default`. |
| D-R9 | The DELIVER call-site check also greps `package.json` scripts for every script that runs `vitest` or `dotnet test` (including `sonarreport`, which CI runs with `CI: true` through `.github/actions/build-frontend`) and `.github` for `pnpm (run )?(test|sonarreport)` wrappers (`ci_verifysqlite.yml`, `ci_verifypostgres.yml`, `ci_verifyauth.yml` run pnpm test scripts) and records each call site with its CI state. |
| D-R10 | The coexistence matrix gains `pnpm build` and `dotnet build` (see DEVOPS). Git hooks live in `.githooks` (no lefthook config exists). StrykerJS and Stryker.NET evidence re-counted (see Scope and evidence). |
| D-R11 | ADR-074 (backend test fixture parallelization isolation) was consulted: the cap changes the worker count only, not fixture scope or isolation, so ADR-074 is not amended. |
| D-R12 | The docs (step 01-03) also say: `VITEST_MAX_WORKERS` and the CLI `--maxWorkers` override the cap on the frontend; a contributor's own `--` arguments replace it on the backend; and the fraction lives in two places (the `testWorkers.ts` constant and the csproj), kept in step by hand, which is accepted. |

Provenance of the decisions (dated facts, 2026-11-01):

- In chat the maintainer chose the Platform scope.
- The maintainer then answered "yep go on" to the post-DESIGN hold, which accepted the recommendations: the full-versus-half
  measurement (12 runs), worker-count evidence by unit test plus measured thread count instead of a log line, and (then)
  skipping the optional mutation pass. That last one is now reversed by the lead session per CLAUDE.md's per-feature gate (D-R5).
- AFK entry choices: refactor plus review per slice, no push, standard rigor.

## Wave: DESIGN / [REF] Component decomposition

| File | Change |
|---|---|
| `Lighthouse.Frontend/src/tests/testWorkers.ts` | NEW: pure resolver `(env, cores) -> number | undefined`, plus the default-fraction constant. |
| `Lighthouse.Frontend/src/tests/testWorkers.test.ts` | NEW: the ratchet (D4). |
| `Lighthouse.Frontend/vitest.config.ts` | MODIFIED: `maxWorkers: resolveTestWorkers(process.env, availableParallelism())`; remove the misleading comment. |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj` | MODIFIED: one `PropertyGroup` plus the `ValidateTestWorkers` target (D3). |
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
| Frontend logic home | `src/tests/` helper precedent (`ExpectedTestFailure.ts` imported by the config) | **CREATE NEW** `testWorkers.ts` + test. `ExpectedTestFailure.ts` is a different concern (expected-failure allowance); the resolver is a separate, unit-tested pure function. Inlining (D2-B) leaves the CI rule unpinned. |
| Backend worker setting | `RunSettingsFilePath` property in the test csproj, `lighthouse.runsettings` | EXTEND the csproj (D3-A). The runsettings file stays TZ-only because it cannot take an env value. |
| CI detection | `CI` env var (GitHub sets it; the frontend action also sets it explicitly) | REUSE, no new switch |
| Docs | CLAUDE.md Quality Gates | EXTEND |
| Measurement script | none in the repo for resource sampling (`Scripts/test-timings` measures per-test times from CI output) | Keep the sampler in this workspace (`measurement/`), not in `Scripts/`: a one-off. CREATE NEW there only, justified: nothing existing samples process-tree memory. |

## Wave: DESIGN / [REF] Measurement protocol

The 18-run protocol below is the original design; the maintainer chose the reduced 12-run full-vs-half protocol, described
in "Protocol as run" at the end of this section. Run later by the main session in a quiet slot (no other session, no `dotnet`/`node` busy; record `uptime` and
`free -m` first). Not run in DESIGN.

- **Settings**: full (the CI-equivalent unset behaviour: Vitest 11 workers, NUnit 12), half (6), quarter (3). Candidate
  fractions are chosen from the three; add one more point only if the knee is between them.
- **Rounds**: n = 3 per setting per stack, order alternated (full, half, quarter, full, half, quarter, ...) so thermal and cache
  drift hit every setting alike. Drop nothing; report median and range.
- **Commands** (as designed; the run used the tools' own overrides instead, see "Protocol as run"): frontend `LIGHTHOUSE_TEST_WORKERS=<n> pnpm test` (full = unset with `CI=true`, which is the CI behaviour);
  backend: build once, `dotnet build-server shutdown` before each run so the compiler server is not counted, then
  `dotnet test --no-build --filter "TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration"`
  with `LIGHTHOUSE_TEST_WORKERS=<n>` (full = `CI=true` unset).
- **Capture per run**: wall time; peak sum of RSS over the run's process tree (all `node`/vitest workers; `testhost` and
  its children) sampled at 1 Hz with `ps`; minimum `MemAvailable` from `/proc/meminfo` at the same cadence; failures
  (a different failure count between settings invalidates the run). `/usr/bin/time -v` is not enough (it reports one process).
- **Report** in this file: one table per stack, median wall time, median peak tree RSS, minimum available memory, and
  the chosen fraction by the rule fixed before the results (see "Protocol as run", item d).
- **Follow-ups recorded, not done**: the backend peak is dominated by the testhost leak (T1: 2 -> 18 GB in 8 minutes),
  so the backend fraction is **re-measured after T1** and adjusted if the leak's removal moves the knee. The frontend
  number stays after FE7 only if FE7 changes the file count markedly.
- **Estimated duration**: 2 stacks x 3 settings x 3 runs = 18 runs. Full-count durations are unmeasured today; assuming
  about 3-6 minutes for the frontend and 4-8 for the backend (smaller worker counts run longer), plus about a minute
  between runs: **roughly 1.5 to 2 hours**, plus about 15 minutes for one dry run of the sampler. n can be cut to 2
  for the quarter setting to save about 20 minutes.

### Protocol as run (revised 2026-11-01)

The maintainer chose the reduced protocol: full (tool default) versus half (6 workers), 3 alternating rounds, both stacks,
12 runs. The quarter point is not run. Validity rules fixed before any result is read (D-R7):

- (a) Failure counts are taken post-hoc from each run's log (the Vitest summary line, the `dotnet test` "Failed:" line). A
  round whose failure counts differ between settings is voided and reported.
- (b) Peak tree RSS is an upper bound: summed RSS double-counts pages shared across processes (exact for Vitest, whose workers
  are threads in one process). Minimum `MemAvailable` is the machine-level figure. 1 Hz sampling can miss sub-second peaks.
  The sampler is Linux-only.
- (c) The quiet slot was guaranteed by the maintainer at 2026-11-01 about 14:57. The load average was not recorded per run;
  that is a stated limit of the result.
- (d) Decision rule, fixed before results: keep fraction 0.5 if half's median wall time is <= 1.25 x full's on each stack;
  otherwise raise to 0.75 untested and record the follow-up.
- (e) For later re-measurement (after T1, and once the resolver is wired, where an unset run is no longer "full"), the scripts
  set the full run explicitly. Recorded as a follow-up: the lead edits the scripts after the current run ends.
- (f) Output goes to the session scratchpad
  (`/tmp/claude-1000/-storage-repos-Lighthouse/eea95318-7f4b-496f-a4e7-06ac208044ef/scratchpad/measure/`); the results table
  lands in this file when the run completes.

## Wave: DESIGN / [REF] Slices and commit plan

One slice, three steps, each under a day (the whole thing is a half-day of work plus the measurement slot):

| Step | Delivers | Commit |
|---|---|---|
| 01-01 | Frontend: resolver + its test + `vitest.config.ts` wiring + comment removal | `chore(test-infra): cap local vitest workers, leave CI on the default` |
| 01-02 | Backend: csproj `PropertyGroup`, four-case `getProperty` evidence | `chore(test-infra): cap local NUnit workers through the test project` |
| 01-03 | CLAUDE.md block; later the measured fraction replaces the placeholder in both stacks and the results table lands here | `docs(test-infra): document the worker cap and build-server shutdown`, then `chore(test-infra): set the measured local worker fraction` |

Steps 01-01 and 01-02 are independent. No moves; configuration plus one new pure helper file (additive, no moves), so "moves separate from edits" is N/A for that reason.
Mutation testing (D-R5): the resolver is branching logic, so CLAUDE.md's per-feature gate applies: a StrykerJS run on
`src/tests/testWorkers.ts` in the final gate, on frozen code, kill rate >= 80%; the csproj is declarative and N/A. Order within DELIVER: placeholder `0.5` ships first, the measured value is a
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
3. CLOSED (D-R2): an invalid value is rejected by an MSBuild `Error` naming the variable; DELIVER only verifies the
   target runs on `dotnet test` and picks the hook point that does.
4. DELIVER: confirm no other workflow runs the backend test project with `CI` unset (all `ci_*` workflows run on GitHub,
   which sets it); a grep of `dotnet test` and `vitest` call sites in `.github/` and `.githooks/` (the hooks run no tests), plus a grep of
   `package.json` scripts and `.github` for `pnpm (run )?test` wrappers (`ci_verifysqlite.yml`, `ci_verifypostgres.yml`,
   `ci_verifyauth.yml` run pnpm test scripts); record each call site with its CI state (D-R9).
5. CLOSED (D-R5): the StrykerJS pass on `testWorkers.ts` is part of the final gate, kill rate >= 80%.
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

## Wave: DEVOPS / [REF] Platform decisions (2026-11-01)

AFK mode: the lead session took the recommended option on each, within the maintainer's "yep go on" to the DESIGN recommendations (dated provenance in the DESIGN revision section); this DEVOPS platform table itself was not put to the maintainer, and the mutation-pass reversal (D-R5) is reported to them at the next summary. Nothing is deployed or shipped differently: the product is
unchanged, only how a developer's local test run sizes its worker pool.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | N/A, because nothing deploys: no artifact, image or runtime behaviour changes. Rollback is reverting the commits; nothing is persisted. |
| CI/CD platform | GitHub Actions, existing `ci_frontend.yml` and `ci_backend.yml`, **UNCHANGED by design**: CI leaves the worker count unset, so Vitest keeps `cores-1` (3 on `ubuntu-latest`) and NUnit keeps `cores` (4). |
| Branching | Trunk-based on `main` (project rule); push directly, no branch or PR. |
| Observability, alerting, SLOs | N/A, because there is no runtime to observe; the payoff instrument is the measurement protocol (below). |
| Continuous learning / feature flags | N/A, because nothing a user sees can be rolled out gradually; the knob is an environment variable. |
| Mutation testing | Strategy stays `per-feature` (CLAUDE.md, not edited). The change is configuration plus one new pure helper file (additive, no moves). **csproj/MSBuild: N/A, because it is declarative with no logic Stryker.NET can mutate.** **`testWorkers.ts`: a StrykerJS run in the final gate, on frozen code, kill rate >= 80% (D-R5), because the resolver is branching logic.** |
| Usage-data event | **N/A, because this is behaviour-free test tooling with no product surface; nothing a user does changes, so there is no use to count.** No `UsageDataEventName` entry, no `docs/settings/usagedata.md` line. |
| Security | No secret, permission or token scope changes. The variable holds a small integer. |
| Cross-cutting (RBAC, Lighthouse-Clients, Website, UI sketch, Terminology) | All N/A as answered in PICK-UP, because no code path, route, DTO, screen or user-facing word changes. |

## Wave: DEVOPS / [REF] Environment matrix

See `environments.yaml`.

| Environment | Who | Worker count (frontend / backend) | Mechanism |
|---|---|---|---|
| local-default | Maintainer and other contributors, Linux 12-core / 31 GB here; macOS and Windows dev machines | `floor(cores * 0.5)` (placeholder, 6 here) / same rule | `testWorkers.ts` resolver; csproj `VSTestCLIRunSettings` property |
| local-override | Anyone running `LIGHTHOUSE_TEST_WORKERS=N` | N on both stacks | Same code paths; an explicit value wins in every environment |
| ci | GitHub `ubuntu-latest`, 4 cores, `CI=true` | Tool default: Vitest 5 cores-1 (3), NUnit one per core (4) (unchanged) | Nothing configured; the csproj property is unset and the resolver returns `undefined` |
| IDE runner, frontend | VS Code and other Vitest explorers | Capped like a command-line run | They load `vitest.config.ts`, so the resolver applies |
| IDE runner, backend | Rider, Visual Studio | Uncapped (tool default) | NUnit IDE runners do not evaluate `VSTestCLIRunSettings`; documented in CLAUDE.md |

## Wave: DEVOPS / [REF] CI pipeline outline

Unchanged. `ci_frontend.yml` and `ci_backend.yml` get no edit, and neither does any other file under `.github/`.

- **Why no edit**: CI detection reuses the `CI` variable GitHub already sets; "leave room for other sessions" is a
  local concern, and a 4-core runner is dedicated to one job. Writing an explicit count would change CI (Vitest
  would go from 3 to 4), which the guardrail forbids.
- **Backend CI step** passes `-- RunConfiguration.MaxCpuCount=0`; a `--` argument becomes a global MSBuild property and replaces the
  csproj `VSTestCLIRunSettings`, so the cap is dropped there on its own, and the `CI` condition is a second guard.
- **Where the change is exercised**: `ci_changes.yml` already routes a `Lighthouse.Frontend/` or `Lighthouse.Backend/` change to its
  suite, so the push runs both. The new `testWorkers.test.ts` runs in the frontend suite; the backend change is proved
  locally by `dotnet msbuild -getProperty:VSTestCLIRunSettings` in four cases (DELIVER), and by the backend suite staying green.
- **Evidence CI is unchanged**: the CI logs on the push show the same test counts and no duration step change; compare the median
  against the preceding runs on `main`.
- **Quality gates**: unchanged (`pnpm build` with Biome and `tsc -b`, `dotnet build` with `TreatWarningsAsErrors`). Git hooks run no tests.
- **DELIVER pre-push check**: grep `.github/` and `.githooks/` for `dotnet test` and `vitest` call sites to confirm none runs with `CI` unset (DESIGN open question 4); also grep `package.json` scripts and `.github` for `pnpm (run )?test` wrappers (`ci_verifysqlite.yml`, `ci_verifypostgres.yml`, `ci_verifyauth.yml` run pnpm test scripts) and record each call site with its CI state (D-R9).

## Wave: DEVOPS / [REF] Monitoring and KPI contracts

- **Outcome KPIs**: none touched (Epic guardrail only: CI test behaviour and duration unchanged).
- **Monitoring / alerting / dashboards**: N/A, because there is no running system; nothing to alert on.
- **Payoff instrument: the measurement protocol** (DESIGN, "Measurement protocol"): per stack and setting, wall time, peak
  process-tree RSS (1 Hz `ps` sampling) and minimum `MemAvailable`, n runs alternating, same machine, quiet slot, equal
  failure counts or the run is void.
- **Reduced run now in progress** (main session): full (tool default) versus half (6 workers), n = 3 alternating, both stacks,
  using the tools' own overrides (`vitest --maxWorkers`, `NUnit.NumberOfTestWorkers`). Valid before the new resolver exists,
  because the outcome depends only on the worker count, not on how it is set. The script and sampler are in
  `measurement/`; raw output goes to the session scratchpad
  (`/tmp/claude-1000/-storage-repos-Lighthouse/eea95318-7f4b-496f-a4e7-06ac208044ef/scratchpad/measure/`) and the results
  table lands in this file when the run completes. Validity rules and the decision rule are fixed in "Protocol as run"
  (DESIGN measurement section). The quarter setting and the full 18-run protocol are not run; the fraction can be revisited.
- **Follow-up, recorded not done**: re-measure the backend after T1 (testhost leak) lands.

## Wave: DEVOPS / [REF] Coexistence matrix

Things that must keep working; detail in `environments.yaml`.

| Tool | Stays working because |
|---|---|
| `ci_frontend.yml`, `ci_backend.yml` | Not edited; CI sets nothing (see above). |
| Stryker.NET configs | They set their own `concurrency` (2, 4 or 6 found); the property's effect on Stryker's VSTest runner is unverified, so DELIVER dry-runs the smallest backend Stryker config before and after (DESIGN open question 2). |
| StrykerJS (98 `vitest.stryker*` configs under `docs/feature/*/mutation/`) | None imports `vitest.config.ts`; each Stryker config selects its own via `configFile`, so they keep Vitest's default. |
| IDE runners, backend (Rider, Visual Studio) | Uncapped and documented; NUnit IDE runners never evaluate the property. |
| IDE runners, frontend (Vitest) | Capped by the resolver, because they load `vitest.config.ts`. |
| TZ runsettings pin (`lighthouse.runsettings`) | File untouched; DESIGN showed `TestHost_RunsUnderThePinnedInstanceTimeZone` passes with the setting applied. |
| `BackendTestParallelizationGuardTest` | Parallel scope and the 7 serial fixtures are untouched; only the worker count changes. |
| Playwright (`workers: 1`) | Out of scope and untouched. |
| Git hooks (`.githooks/pre-commit`, `.githooks/pre-push`; no lefthook config exists) | Run no tests; commits themselves verify Biome and the secret scan. |
| `pnpm build` | `tsc -b` type-checks the new `src/tests/testWorkers.ts` and `testWorkers.test.ts`, and `prebuild` runs `biome check --write ./src`, which may rewrite them; DELIVER checks `git status` after the build. |
| `dotnet build` | The csproj gains a property group and a validation target; the build must stay warning-free under `TreatWarningsAsErrors`. |
| Worktrees | The csproj and `vitest.config.ts` travel with the checkout; the variable is per shell. Gitignored fixtures (licence file) are unrelated. |

## Wave: DEVOPS / [REF] Pre-requisites carried from DESIGN

1. Placeholder fraction `0.5` ships first; the measured fraction is a one-constant follow-up in both stacks (two languages, each pointing at the other).
2. Re-verify the csproj expression in the real project: `dotnet msbuild -getProperty:VSTestCLIRunSettings` for unset, `CI=true`, `CI=true` plus override, override only.
3. The csproj carries the `ValidateTestWorkers` error target (D-R2): verify it fires for an invalid value (`abc`, `0`, `-1`, `1.5`, ` 6`, `+6`, `50%`) and that its hook point actually runs on `dotnet test`; use the hook point that does.
4. Stryker.NET dry run before and after (above).
5. CLAUDE.md block (variable, defaults, overrides, `VITEST_MAX_WORKERS` / `--maxWorkers` / `--` precedence, IDE limits, the two-place fraction, `dotnet build-server shutdown`) lands in step 01-03; this DEVOPS wave does not edit it.
6. Delete the misleading "Reduce memory usage on CI" comment in `vitest.config.ts`; `bail: 1` stays.
7. Peer review of DESIGN and DEVOPS is dispatched by the lead session with `model` per the rigor profile.
8. StrykerJS on `src/tests/testWorkers.ts` in the final gate on frozen code, kill rate >= 80% (D-R5).
9. Call-site check of `pnpm (run )?test` wrappers (D-R9).
10. After the current measurement run ends, the scripts set the full run explicitly (see "Protocol as run", item e).

## Wave: DEVOPS / [REF] Risks for DISTILL and DELIVER

- Do not run the reduced measurement and DELIVER's test runs at the same time; both depend on a quiet machine.
- A contributor's own `-- ...` arguments to `dotnet test` replace the cap; this is documented, not fixed.
- Local-only effect: a slower local run (fewer workers) is the intended trade; the measurement decides the fraction.

## Wave: DISTILL / [REF] Reconciliation

Reconciliation passed -- 0 contradictions. DISCUSS is covered at Epic level; DESIGN and DEVOPS agree on every point
that matters here: one variable, explicit value wins everywhere, CI configures nothing, resolver in
`src/tests/testWorkers.ts`, csproj `VSTestCLIRunSettings` for NUnit, no CI workflow edit. DESIGN's "Changed Assumptions"
(CI = tool default rather than an explicit 4; csproj property instead of runsettings/attribute; no worker count in the
Vitest log) are already accepted by the maintainer and are the basis for the scenarios below. The brief was revised
2026-11-01 and the scenarios follow the revised brief.

The four ambiguities the first DISTILL pass flagged are all decided by the DESIGN revision (D-R1..D-R4); 0 remain open:

1. "CI is `true`": case-insensitive equality with `true` on both stacks (D-R3). `TRUE` and `True` mean CI; `false`, empty and `1` do not.
2. Empty `LIGHTHOUSE_TEST_WORKERS`: treated as unset on both stacks (D-R1). Valid means `^[1-9][0-9]*$` and nothing else (D-R2),
   so `" 4"`, `"4 "`, `"+4"`, `"1.5"`, `"50%"` and `"4x"` are invalid.
3. An explicit count above the core count is accepted, no clamp (D-R4); the test pins it with `16` on 12 cores.
4. The fraction constant is named `LOCAL_WORKER_FRACTION` (also `TEST_WORKERS_VARIABLE`, `TestWorkersEnvironment`); rename freely
   in DELIVER, the test imports them. The test pins its value to 0.5 and uses literal expectations.

## Wave: DISTILL / [REF] Scenario list

Classification is kept here; none of it is written into the code (project rule). Test names in the file are the scenario titles.
The expectations are literals (6 on 12 cores, and so on), not a re-derivation of the formula, so the test cannot agree with a
wrong implementation by construction. The one live test pins `LOCAL_WORKER_FRACTION` to 0.5: when the measured fraction lands
(step 01-03), the literals in the cores table, the "share of cores" and "empty count" rows, and this pin change together, and so
does the csproj fraction.

| Scenario (describe > it) | Class | Rows | Status in file |
|---|---|---|---|
| is importable and pins the agreed fraction and variable name | scaffold sanity + fraction pin | 1 | runs, green |
| developer machine: uses the share of cores when nothing is set (6 on 12) | happy, driving port, first to un-skip | 1 | skipped |
| developer machine: empty count = not set (D-R1) | edge | 1 | skipped |
| developer machine: explicit count "1", "3" | happy | 2 | skipped |
| developer machine: explicit count "16" on 12 cores, no clamp (D-R4) | edge | 1 | skipped |
| developer machine: CI false / empty / "1" still caps | edge | 3 | skipped |
| developer machine: cores table 1->1, 2->1, 4->2, 7->3, 12->6, 24->12 | boundary | 6 | skipped |
| developer machine: whole number, >= 1, <= cores for 1..128 cores | invariant | 1 | skipped |
| build server: nothing set -> tool default (`undefined`) | happy, guardrail (CI unchanged) | 1 | skipped |
| build server: CI "TRUE" / "True" -> tool default (D-R3) | happy | 2 | skipped |
| build server: empty count = not set -> tool default | edge | 1 | skipped |
| build server: tool default whatever the core count | edge | 1 | skipped |
| build server: explicit count wins (CI "true" and "TRUE") | happy | 2 | skipped |
| invalid count: "0", "-2", "1.5", "abc", "50%", "4x", " 4", "4 ", "+4" -> throws naming the variable (D-R2) | error | 9 | skipped |
| invalid count on the build server too | error | 1 | skipped |

Counts (from the file as it stands): 33 tests counting `it.each` rows = 1 live + 32 skipped. Happy: 8 skipped plus the live
sanity test (9); error/edge/boundary: 24 of 32 skipped (error 10, edge 7, boundary 7), above the 40% target.
The invariant uses a plain loop over 1..128 cores, no fast-check (a pure function with a bounded domain does not warrant it).
The earlier separate "never below one worker on a single core" test is gone: the 1->1 row of the cores table covers it.

## Wave: DISTILL / [REF] Test placement and scaffolds

- Placement: `Lighthouse.Frontend/src/tests/testWorkers.test.ts`, next to the module (DESIGN D4; precedent
  `e2eTestIds.test.ts` and `ExpectedTestFailure.ts` live in the same folder). Explicit `describe/it/expect` imports from `vitest`.
- Scaffold: `Lighthouse.Frontend/src/tests/testWorkers.ts` exports `resolveTestWorkers(env, availableCores)`,
  `LOCAL_WORKER_FRACTION = 0.5` (placeholder until measured), `TEST_WORKERS_VARIABLE`, `TestWorkersEnvironment`, and
  `__SCAFFOLD__ = true`; the function throws `Error("Not yet implemented -- RED scaffold")`. DELIVER removes the marker and the throw.
  Not wired into `vitest.config.ts` (DELIVER step 01-01).
- Backend: no test file, no scaffold (DESIGN D4).

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | Coverage |
|---|---|
| `resolveTestWorkers(env, cores)` (called by `vitest.config.ts`) | Every DESIGN D1 row: explicit wins (also CI), CI unset -> `undefined`, local unset -> fraction, floor, minimum 1, invalid -> throws naming the variable |
| csproj `VSTestCLIRunSettings` (read by `dotnet test`) | Backend acceptance check below (command evidence, not a test) |
| CLAUDE.md block | Docs checklist below |

Driven adapters: none (no I/O), so no adapter-integration scenario applies.

## Wave: DISTILL / [REF] Backend acceptance check (step 01-02)

Run from `/storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests`, on a 12-core machine, with the fraction 0.5
in the csproj (the measured fraction changes cases 1 and 6 in step with the frontend literals). Paste the real output into
this file when run.

```
cd /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests
env -u CI -u LIGHTHOUSE_TEST_WORKERS dotnet msbuild -getProperty:VSTestCLIRunSettings        # case 1
env CI=true -u LIGHTHOUSE_TEST_WORKERS dotnet msbuild -getProperty:VSTestCLIRunSettings       # case 2
env -u CI LIGHTHOUSE_TEST_WORKERS=4 dotnet msbuild -getProperty:VSTestCLIRunSettings          # case 3
env CI=true LIGHTHOUSE_TEST_WORKERS=2 dotnet msbuild -getProperty:VSTestCLIRunSettings        # case 4
env -u CI LIGHTHOUSE_TEST_WORKERS= dotnet msbuild -getProperty:VSTestCLIRunSettings           # case 5
env CI=TRUE -u LIGHTHOUSE_TEST_WORKERS dotnet msbuild -getProperty:VSTestCLIRunSettings       # case 6
env -u CI LIGHTHOUSE_TEST_WORKERS=abc dotnet test --no-build --filter "FullyQualifiedName~BackendTestParallelizationGuardTest"   # case 7
env -u CI LIGHTHOUSE_TEST_WORKERS=0 dotnet test --no-build --filter "FullyQualifiedName~BackendTestParallelizationGuardTest"     # case 8
```

| Case | Expected output |
|---|---|
| 1 local, unset | `NUnit.NumberOfTestWorkers=6` |
| 2 `CI=true`, unset | empty (property not set; NUnit keeps one worker per core) |
| 3 local, `=4` | `NUnit.NumberOfTestWorkers=4` |
| 4 `CI=true`, `=2` | `NUnit.NumberOfTestWorkers=2` |
| 5 local, empty value | `NUnit.NumberOfTestWorkers=6` (empty = unset, same as case 1) |
| 6 `CI=TRUE`, unset | empty (same as case 2; the comparison is case-insensitive) |
| 7 local, `=abc` | exit code non-zero; output contains `LIGHTHOUSE_TEST_WORKERS must be a positive whole number`; no test runs |
| 8 local, `=0` | exit code non-zero; output contains `LIGHTHOUSE_TEST_WORKERS must be a positive whole number`; no test runs |

Cases 7 and 8 run `dotnet test` (not `getProperty`), because the check is a target. DELIVER verifies that the validation
target's hook point actually fires on `dotnet test`, with and without `--no-build`, and moves it (the `VSTest` target, or a
property-evaluation-time check) if it does not.

Same step: the parallelization guard and `TestHost_RunsUnderThePinnedInstanceTimeZone` still pass with the cap applied;
every call site in `.github`, `.githooks` and `package.json` scripts is listed with its CI state, and each one that runs with `CI` unset is a local-only script; Stryker.NET dry run on the
smallest backend config before and after (DESIGN open question 2).

## Wave: DISTILL / [REF] Docs acceptance check (step 01-03)

CLAUDE.md, under Quality Gates, one short block that:

- [ ] names `LIGHTHOUSE_TEST_WORKERS` and says it applies to both `pnpm test` and `dotnet test`;
- [ ] states the rule: unset or empty and not CI -> half the cores (or the measured fraction), at least 1; unset in CI (`CI=true`, any case) -> the tool's own default, CI unchanged; an explicit value wins everywhere;
- [ ] gives both override commands (`LIGHTHOUSE_TEST_WORKERS=N pnpm test`, `LIGHTHOUSE_TEST_WORKERS=N dotnet test ...`);
- [ ] says `VITEST_MAX_WORKERS` and the CLI `--maxWorkers` override the cap on the frontend;
- [ ] says arguments after `dotnet test --` replace the cap for that run (a contributor's own `--` arguments replace it on the backend);
- [ ] says Vitest IDE runs are capped (they load `vitest.config.ts`) while NUnit IDE runners (Rider, Visual Studio) stay uncapped;
- [ ] says the fraction lives in two places (the `testWorkers.ts` constant and the csproj), kept in step by hand;
- [ ] says an invalid value (not a positive whole number) fails fast on both stacks: the frontend resolver throws, and the backend prints "LIGHTHOUSE_TEST_WORKERS must be a positive whole number";
- [ ] mentions `dotnet build-server shutdown` (releases the resident compiler server; the next build restarts it);
- [ ] uses the same fraction number as the code (kept in step when the measured value lands);
- [ ] contains no story, decision or ADR ids;
- [ ] `vitest.config.ts`: the "Reduce memory usage on CI" comment is gone, `bail: 1` stays.

## Wave: DISTILL / [REF] Worker-count evidence

Accepted at the post-DESIGN hold (2026-11-01) instead of an echo line: the resolver unit tests above (frontend) and the
`getProperty` output (backend). Vitest prints nothing about workers and no console line is added. The sampler's
`max_threads` column is a session thread total (main, V8, libuv and wrapper threads included), not a worker count, and is
reported for information only.

## Wave: DISTILL / [REF] Acceptance criteria as Given/When/Then

The Vitest titles are the executable form; these are the brief's criteria in the Epic DoR's Given/When/Then shape.

1. **Local default.** Given a developer machine with 12 cores and neither `CI` nor `LIGHTHOUSE_TEST_WORKERS` set,
   when `pnpm test` or `dotnet test` starts, then each runs with 6 workers (the local fraction of the cores).
2. **Explicit value wins.** Given `LIGHTHOUSE_TEST_WORKERS=3` (with or without `CI=true`), when either suite starts,
   then it runs with 3 workers.
3. **CI unchanged.** Given `CI=true` and no explicit value, when either suite starts, then nothing is configured and
   each tool keeps its own default (Vitest 5: cores-1; NUnit: one per core).
4. **Invalid value refused.** Given `LIGHTHOUSE_TEST_WORKERS=abc` (or `0`, `1.5`, ` 4`), when either suite starts,
   then it fails before running a test, with a message naming `LIGHTHOUSE_TEST_WORKERS`.
5. **Empty means unset.** Given `LIGHTHOUSE_TEST_WORKERS=` (empty), when either suite starts, then it behaves as in 1
   (or 3 under `CI=true`).
6. **Payoff recorded.** Given the quiet machine, when the 12-run measurement completes, then wall time and peak memory
   for full and half workers on both stacks are in the results table of this file.
7. **Documented.** Given a contributor reading CLAUDE.md's quality gates, then they find the variable, its CI rule, the
   overrides that beat it, and `dotnet build-server shutdown`.

With these, Epic DoR item 4 for this Story is met at DISTILL (the pick-up recorded it as deferred to here).

## Wave: DISTILL / [REF] Brief-AC traceability

| Brief acceptance criterion | Closed by | Kind |
|---|---|---|
| Re-scoped AC1: a local run with nothing set is capped, an explicit value wins, CI is unchanged | Resolver tests (frontend); the backend `getProperty` output (cases 1-4 above) | tests + command evidence |
| Wall time and peak memory recorded for both settings on both stacks | The measurement results table, landing in this file in step 01-03 | a measurement, not a test |
| Documented override | Docs acceptance check below | checklist |

## Wave: DISTILL / [REF] Pre-requisites

DESIGN D1-D3 (resolver signature, csproj property), DEVOPS environment matrix (`environments.yaml`: local-default,
local-override, ci). A quiet machine for the RED check and the measurement. Outcome registry and `kpi-contracts.yaml`: skipped,
because this is behaviour-free test tooling with no product contract and no KPI row is touched.

## Wave: DISTILL / [REF] RED classification

Run in step 01-01 (2026-11-01), machine quiet, every `it.skip` removed from `testWorkers.test.ts`, scaffold still in place:
`pnpm exec vitest run src/tests/testWorkers.test.ts` -> 33 collected, 32 failed, 1 passed. The one pass is
`is importable and pins the agreed fraction and variable name`. All 32 failures are the scaffold's
`Not yet implemented -- RED scaffold` (the "throws naming the variable" cases fail with it as the received message
instead of matching `LIGHTHOUSE_TEST_WORKERS`). No import error, no type error, no collection error: right reason.

## Wave: DELIVER / [REF] Rigor profile

standard for slice 01 (crafter sonnet, reviewer haiku), agreed with the maintainer at AFK entry 2026-11-01; mutation: StrykerJS on testWorkers.ts in the final gate (>=80%), csproj N/A.

## Wave: DELIVER / [REF] Evidence step 01-02

Run 2026-11-01 from `Lighthouse.Backend/Lighthouse.Backend.Tests`, 12 cores. Change: one `PropertyGroup` and the
`ValidateTestWorkers` target in `Lighthouse.Backend.Tests.csproj`; nothing under `.github`, `lighthouse.runsettings` or
`GlobalUsings.cs` is touched.

**Hook point chosen: `BeforeTargets="VSTest"`, as proposed.** It fires on `dotnet test` both with `--no-build` and without
it (tables below), so no property-evaluation-time check was needed. Learned on the way: an XML comment may not contain a
double dash, so the csproj comment says "after the `dotnet test` double dash" instead of writing it.

**RED** (unchanged csproj): `dotnet msbuild -getProperty:VSTestCLIRunSettings` printed nothing for local-unset (case 1) and
for `LIGHTHOUSE_TEST_WORKERS=4` (case 3); `LIGHTHOUSE_TEST_WORKERS=abc dotnet test --no-build --filter
FullyQualifiedName~BackendTestParallelizationGuardTest` exited 0 and ran 2 tests (`Passed: 2`). Right reason: the feature
is absent.

**Cases 1-6 (`dotnet msbuild -getProperty:VSTestCLIRunSettings`)**

| Case | Command env | Output |
|---|---|---|
| 1 local, unset | `-u CI -u LIGHTHOUSE_TEST_WORKERS` | `NUnit.NumberOfTestWorkers=6` |
| 2 `CI=true`, unset | `CI=true`, variable unset | empty |
| 3 local, `=4` | `-u CI LIGHTHOUSE_TEST_WORKERS=4` | `NUnit.NumberOfTestWorkers=4` |
| 4 `CI=true`, `=2` | `CI=true LIGHTHOUSE_TEST_WORKERS=2` | `NUnit.NumberOfTestWorkers=2` |
| 5 local, empty | `-u CI LIGHTHOUSE_TEST_WORKERS=` | `NUnit.NumberOfTestWorkers=6` |
| 6 `CI=TRUE`, unset | `CI=TRUE`, variable unset | empty |
| extra: `CI=true`, empty | `CI=true LIGHTHOUSE_TEST_WORKERS=` | empty |

**Cases 7-8 and the extended invalid set** (`dotnet test [--no-build] --filter "FullyQualifiedName~BackendTestParallelizationGuardTest"`;
every run exit code 1, no `Passed!`/`Failed!` summary line, i.e. no test ran; message printed:
`Lighthouse.Backend.Tests.csproj(25,3): error : LIGHTHOUSE_TEST_WORKERS must be a positive whole number, got '<value>'.`)

| Value | CI | `--no-build` | Exit | Message names the variable | Tests run |
|---|---|---|---|---|---|
| `abc` (case 7) | unset | yes | 1 | yes | none |
| `abc` | unset | no (full build first) | 1 | yes | none |
| `abc` | `true` | yes | 1 | yes | none |
| `0` (case 8) | unset | yes | 1 | yes | none |
| `-1` | unset | yes | 1 | yes | none |
| `1.5` | unset | yes | 1 | yes | none |
| `' 6'` | unset | yes | 1 | yes | none |
| `+6` | unset | yes | 1 | yes | none |
| `50%` | unset | yes | 1 | yes | none |
| `0` | `true` | no | 1 | yes | none |

**Criterion 5.** With the cap applied (CI unset, variable unset):
`dotnet test --filter "FullyQualifiedName~BackendTestParallelizationGuardTest|FullyQualifiedName~TestHost_RunsUnderThePinnedInstanceTimeZone"`
-> `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4`. `dotnet build` in `Lighthouse.Backend`: `0 Warning(s)`, `0 Error(s)`.

**Stryker.NET: not affected, by evidence; no real run (would exceed ~15 min).** `VSTestCLIRunSettings` is read only by the
`VSTest` MSBuild target that `dotnet test` invokes. Stryker.NET 4.14.1 (installed global tool) starts tests through
`Stryker.TestRunner.VsTest.dll` on top of `Microsoft.TestPlatform.VsTestConsole.TranslationLayer` (`VsTestConsoleWrapper`) and
builds its own `<RunSettings>` document. A byte search of every `Stryker*.dll` finds no occurrence of `VSTestCLIRunSettings`
or `RunSettingsFilePath`, so neither the new property nor the TZ-pinning runsettings file reaches it. Stryker worker count
stays whatever each config's `concurrency` says (smallest config, `stryker.6094.backend.json`: 4). The smallest config by
mutate scope still builds the whole solution, takes a full dry run of the filtered test set and then mutates, well past 15
minutes on this machine, so it was not run.

**Call-site check** (GitHub Actions sets `CI=true` for every step):

| Call site | What | CI state |
|---|---|---|
| `.github/workflows/ci_backend.yml:151` | `dotnet test ... -- DataCollectionRunSettings... RunConfiguration.MaxCpuCount=0` | `CI=true`; property unset, and the `--` arguments would replace it anyway |
| `.github/workflows/ci_backend.yml:156` | `dotnet test ... -- RunConfiguration.MaxCpuCount=0` | same |
| `.github/actions/build-frontend/action.yml:63` | `pnpm run sonarreport` (`vitest run --coverage`), in `Lighthouse.Frontend` | `CI=true` (frontend resolver leaves CI uncapped) |
| `.github/workflows/ci_frontend.yml:34` | `vitest_to_csv.py` (parses vitest output, does not run it) | `CI=true`; not a runner |
| `.github/workflows/ci_verifysqlite.yml:141`, `ci_verifypostgres.yml:154` | `pnpm run test` in `Lighthouse.EndToEndTests` = Playwright, not vitest/dotnet test | `CI=true`; out of scope |
| `.github/workflows/ci_verifyauth.yml:138,208,279` | `pnpm run test:auth/rbac/proxyauth` = Playwright | `CI=true`; out of scope |
| `.githooks/pre-commit`, `.githooks/pre-push` | no `dotnet test`, no `vitest` | n/a |
| `Lighthouse.Frontend/package.json` `test`, `sonarreport` | `vitest run` (with `--coverage` for sonarreport) | `CI` unset when typed locally: local-only scripts; `sonarreport` runs in CI only through the build-frontend action row above |
| `Lighthouse.EndToEndTests/package.json` `test*` | Playwright | not vitest/dotnet test |
| `.github/instructions/*.md` | `vitest` appears only in code samples | n/a |

No other `package.json` script (outside `node_modules`, worktrees and the runner copy under `tools/codesign`) runs vitest
or `dotnet test`. Every call site that runs with `CI` unset is a local script.
