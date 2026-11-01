# Slice 43 — T3 Test runs leave room for parallel sessions: local worker caps

Epic #6264 · ADO #6461 · `@infrastructure` · job_id: infrastructure-only · **Size S**

- infrastructure_rationale: Vitest (11 thread workers) and NUnit (one fixture worker per core) are uncapped, and every parallel session adds a full set. The compiler server stays resident at about 5.3 GB.

> Revised 2026-11-01 at DESIGN of #6461: AC1 re-scoped. CI keeps each tool's own default (nothing is configured)
> instead of an explicit "full count", and the evidence is the resolver's unit tests plus `dotnet msbuild -getProperty`
> output, not a worker count in the logs (Vitest prints none).

## Goal

A local test run leaves room for the parallel sessions on the same machine. Worker counts have a sensible local
default and can be overridden, and CI keeps each tool's own default.

## Why

- Nothing caps either stack. `vitest.config.ts` runs `pool: "threads"` with `maxWorkers: undefined`, which is
  11 workers on the 12-core development machine. NUnit runs `[assembly: Parallelizable(ParallelScope.Fixtures)]`
  with the default `LevelOfParallelism`, one per core.
- Each parallel session adds a full set again. The Roslyn compiler server (`VBCSCompiler`) stays resident at
  about 5.3 GB after builds (observed 2026-10-31).
- Together with T1's leak, this is why test runs and whole sessions get killed when sessions run side by side.

## IN

- One environment variable read by both stacks, for example `LIGHTHOUSE_TEST_WORKERS`, with a local default of
  about half the cores. When `CI` is set and no explicit value is given, nothing is configured, so CI keeps each tool's default.
  - Vitest: `maxWorkers` in `vitest.config.ts`.
  - NUnit: the `VSTestCLIRunSettings` MSBuild property in the test project, built from the variable and `CI`
    (a runsettings file cannot read an environment variable and an assembly attribute cannot either; both were
    rejected at DESIGN).
- Measure wall time and peak memory per stack with each tool's own default (nothing set) and at the candidate
  default, on the same machine, and pick the default from the data.
- Document the variable and `dotnet build-server shutdown` (which releases the compiler server) in CLAUDE.md's
  quality-gates section.
- Remove the misleading "Reduce memory usage on CI" comment beside `bail: 1`.

## OUT

- Fixing the leak (T1) or test design (T2, FE7).

## Acceptance criteria

- [ ] A local run uses the local default and CI is unchanged (each tool's own default), shown by the resolver's unit tests and `dotnet msbuild -getProperty:VSTestCLIRunSettings` output; IDE NUnit runners are an accepted exception.
- [ ] Wall time and peak memory recorded for both settings on both stacks.
- [ ] The override is documented where a contributor looks first.
