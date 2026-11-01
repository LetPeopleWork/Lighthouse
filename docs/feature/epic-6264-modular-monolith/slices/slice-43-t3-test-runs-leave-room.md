# Slice 43 — T3 Test runs leave room for parallel sessions: local worker caps

Epic #6264 · ADO #6461 · `@infrastructure` · job_id: infrastructure-only · **Size S**

## Goal

A local test run leaves room for the parallel sessions on the same machine. Worker counts have a sensible local
default and can be overridden, and CI keeps using the whole runner.

## Why

- Nothing caps either stack. `vitest.config.ts` runs `pool: "threads"` with `maxWorkers: undefined`, which is
  11 workers on the 12-core development machine. NUnit runs `[assembly: Parallelizable(ParallelScope.Fixtures)]`
  with the default `LevelOfParallelism`, one per core.
- Each parallel session adds a full set again. The Roslyn compiler server (`VBCSCompiler`) stays resident at
  about 5.3 GB after builds (observed 2026-10-31).
- Together with T1's leak, this is why test runs and whole sessions get killed when sessions run side by side.

## IN

- One environment variable read by both stacks, for example `LIGHTHOUSE_TEST_WORKERS`, with a local default of
  about half the cores. When `CI` is set, the full count is used.
  - Vitest: `maxWorkers` in `vitest.config.ts`.
  - NUnit: runsettings `NumberOfTestWorkers`, or an assembly-level `LevelOfParallelism` read from the variable.
- Measure wall time and peak memory per stack at the full count and at the default, on the same machine, and pick
  the default from the data.
- Document the variable and `dotnet build-server shutdown` (which releases the compiler server) in `/run-dev` or
  CLAUDE.md's quality-gates section.
- Remove the misleading "Reduce memory usage on CI" comment beside `bail: 1`.

## OUT

- Fixing the leak (T1) or test design (T2, FE7).

## Acceptance criteria

- [ ] A local run uses the default and CI uses the full count, shown by the worker count in the logs.
- [ ] Wall time and peak memory recorded for both settings on both stacks.
- [ ] The override is documented where a contributor looks first.
