# Spike — Backend NUnit fixture parallelization (CS-P)

**Goal (one sentence)**: Probe whether adding `[assembly: Parallelizable(ParallelScope.Fixtures)]` to `Lighthouse.Backend.Tests` cuts wall-clock by 4–8× without breaking test isolation, and enumerate any tests that fail under parallel execution so the follow-up slice can fix them surgically.

**Owner story**: US-02 (catalog candidate CS-P, discovered 2026-05-18 after slice-pre + CS-G shipped).

**Estimated effort**: ½ day for the spike. The follow-up slice (`slice-be-parallel-enable`) costs 0–2 days depending on residual breakage.

**Learning hypothesis**:
- Confirms: With `ParallelScope.Fixtures`, BE test wall-clock drops from ~6 min local / ~11 min CI to ~1–2 min local / ~2–3 min CI. Either zero tests break, or a small named set (<10) that map to known-isolation patterns from `docs/ci-learnings.md` (static caches keyed by URL + Id-0, `Environment` variable mutation, `Directory.SetCurrentDirectory`, in-memory DB sharing).
- Disproves: If a large fraction of tests break under parallel, fixture-scope is too aggressive and the next step is per-fixture opt-in via `[Parallelizable(ParallelScope.Self)]` on safe fixtures only.

## IN scope

- On a throwaway branch (`spike/be-parallelism`):
  - Add `[assembly: Parallelizable(ParallelScope.Fixtures)]` to `Lighthouse.Backend.Tests/GlobalUsings.cs` (next to the `global using NUnit.Framework;` line).
  - Run the full BE test suite three times locally: `dotnet test Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter "Category!=Integration"` (3× to surface flakes that pass on first run).
  - Capture wall-clock per run. Compare against the pre-spike local baseline (~5 min after CS-G).
  - Build a failure inventory: every test that fails on any of the 3 runs, deduplicated, with a one-line classification per CI-learnings 2026-05-17 categories:
    - **Static-state collision** (cache keyed by URL+Id-0, ClientCache, ConnectionCache, etc.)
    - **Environment-variable mutation** (test sets/unsets `Environment.SetEnvironmentVariable` without resetting)
    - **Working directory / file system** (`Directory.SetCurrentDirectory`, temp-file collision)
    - **In-memory DB / EF context sharing** (multiple fixtures share a `DbContextOptions` or singleton)
    - **NUnit fixture state** (`SetUp` mutates shared static)
    - **Other** (named individually)
  - Optionally run with `Category=Integration` filter too if env vars are available locally — same failure inventory.
- Report: `docs/feature/test-speed-improvements/spike-be-parallelism-findings.md` with:
  - Pre-spike vs post-spike wall-clock (3 runs each, mean + range).
  - Failure inventory by category.
  - Per-failure: classification + smallest fix that preserves the test's intent.
  - Verdict: **GO** (open `slice-be-parallel-enable` with the fix list as scope), **PARTIAL** (apply per-fixture opt-in instead), or **NO-GO** (parallel-scope causes more pain than it's worth — fall back to other levers).

## OUT scope

- Applying any fix. Fixes go in the follow-up slice (`slice-be-parallel-enable`), not in the spike.
- Touching production code. The spike is read-only on production; only test-project attributes change, and only on the spike branch.
- Frontend parallelization. Vitest already parallelises by default; FE pipeline is unchanged.
- Per-test parallelization (`ParallelScope.All` / `Children`). Fixture-scope is the safer first cut; if it works, `Children` can be a follow-up experiment.
- Mutation testing under parallel. Out of scope; existing Stryker per-feature configs are unaffected.

## Acceptance criteria

- AC-SPIKE-P.1: `docs/feature/test-speed-improvements/spike-be-parallelism-findings.md` exists.
- AC-SPIKE-P.2: Wall-clock comparison table with 3 pre-spike + 3 post-spike runs (mean ± range each).
- AC-SPIKE-P.3: Failure inventory complete; every failing test classified into one of the categories above (or `Other` with a named explanation).
- AC-SPIKE-P.4: Each failing test has a proposed fix (e.g. "set unique negative `Id` on test fixture's WorkTrackingSystemConnection", "wrap env-var mutation in `try/finally` restoring prior value", etc.) — implementation deferred to the follow-up slice.
- AC-SPIKE-P.5: Verdict line: GO / PARTIAL / NO-GO with one-paragraph rationale.
- AC-SPIKE-P.6: Spike branch deleted after the report is merged into `main`.

## Dependencies

- slice-pre + slice-03A already shipped (commits `eb6fe68d`, `e1cbb4a3`) — gives a cleaner baseline.

## Reference class

NUnit configuration spike. Similar to a `/nw-spike` probe; intentionally short, intentionally throwaway, intentionally answers exactly one question.

## Pre-slice SPIKE

This IS the spike. No nested spike.

## Taste tests

- Ship 4+ new components? **No** — one markdown report.
- Depends on a new abstraction? **No** — NUnit's built-in `Parallelizable` attribute.
- Disproves something? **Yes** — that BE test slowness is per-test cost.
- Synthetic data only? **No** — real local + (optionally) CI measurements.
- Identical-except-for-scale duplicate of another slice? **No**.

All taste tests pass.

## Risk note

The 2026-05-17 CI learning about `VssConnection` static-cache collisions documents the exact class of issue this spike will surface. That bug was diagnosed when only *some* tests were parallel (the integration suite); flipping the global default to `ParallelScope.Fixtures` will surface the same class of issue more broadly. The mitigation patterns are already known and documented. The spike's job is to count how many surfaces there are.

If the spike's count is small (<10) the follow-up slice is well-scoped (~1 day). If the count is large (>20), the alternative is **per-fixture opt-in** (`[Parallelizable(ParallelScope.Self)]` on the safe fixtures, leaving the rest serial), which is a smaller win but ships incrementally.
