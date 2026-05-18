# Slice 03A — CS-G: Cache concurrency test downscale

**Goal (one sentence)**: Reduce the thread counts and time budgets in the three `CacheTest.Concurrent*` tests so they prove the concurrency invariant without spawning 64 tasks that serialise on 2-vCPU CI runners — cutting BE wall-clock by ~22 % with zero coverage regression and benefiting local dev too.

**Owner story**: US-02 (catalog candidate CS-G, ranked #1 in the resumed alternatives memo).

**Estimated effort**: ≤ ½ day. Touch one test file (`CacheTest.cs`); reduce thread counts and loop durations; verify all assertions still hold.

**Learning hypothesis**:
- Confirms: The next 2-3 CI runs show Cache.Concurrent cluster dropping from ~142 s (n=4 main-green mean) to ~15–25 s, no mutation kill-rate regression on `Cache<,>`, no new flakes across 3 back-to-back green runs.
- Disproves: If the cluster doesn't shrink as expected, thread-count was not the dominant cost (e.g. allocation or GC pressure dominates) — back-prop to feature-delta.md and reopen with a different mechanism.

## IN scope

- `Lighthouse.Backend/Lighthouse.Backend.Tests/Cache/CacheTest.cs` — three methods:
  - `ConcurrentReadersAndWriters_DoNotObserveCorruptedEntries` (median 115.8 s, range 65.2–128.6 s across 11 runs — accounts for ~85 % of the cluster on its own).
  - `ConcurrentInsertsOnUniqueKeys_DoNotThrow_AndAllValuesAreRetrievable` (median 18.5 s).
  - `ConcurrentGetOnExpiredKey_SerialisesLazyRemove_WithoutThrowing` (median 11.7 s).
- Reduce thread counts from 32+32 readers/writers (or 64 inserters) to **8+8** (or **16**). Reduce time-bounded loops from 1000 ms to **200 ms**.
- Keep the same correctness assertions: no torn reads, no exceptions, all inserted values retrievable, no observable cache corruption.
- Re-run Stryker.NET against `Cache<,>` with `stryker-config.bug-5016-cache-thread-safety.json` (existing per-feature config) — kill rate must remain ≥ 80 %.
- Run the full BE suite 3 consecutive times locally — zero new flakes.

## OUT scope

- Replacing the concurrency tests with property-based equivalents (defer — that's a future spike if interesting).
- Removing any of the three tests entirely.
- Touching production `Cache<,>` code — that's covered by [[feedback-finalize-workspace-commit]] elsewhere and is not the bottleneck here.
- Other concurrency tests outside `CacheTest.cs`.
- Doc reflows or test-name rewrites beyond what the smaller numbers naturally demand.

## Acceptance criteria

- AC-03A.1: The three `CacheTest.Concurrent*` methods use ≤ 16 concurrent threads/tasks and ≤ 200 ms time budgets.
- AC-03A.2: Stryker.NET kill rate on the `Cache<,>` mutation surface is ≥ 80 % (matches CLAUDE.md gate).
- AC-03A.3: The same assertions are present (line-by-line review against `git diff` confirms no `Assert.That` was deleted or weakened).
- AC-03A.4: The next 3 consecutive `Build And Deploy Lighthouse` runs on `main` show Cache.Concurrent cluster ≤ 30 s in `test-timings-backend.csv` summary (target ~20 s; ceiling 30 s allows for runner noise).
- AC-03A.5: No new `bug-5016`-class flake reports in the week following merge.

## Dependencies

- slice-pre-integration-labelling can run before, after, or in parallel — no overlap.

## Reference class

Tightly scoped refactor of a single test file. Comparable to the kind of work `/nw-bugfix` ships when it adds a regression test, except in reverse (we're reducing the test's stress dimension while keeping its correctness assertions).

## Pre-slice SPIKE

Not required. The concurrency-invariant rationale was established in `bug-5016-cache-thread-safety` and the alternatives memo. No new hypothesis to probe.

## Taste tests

- Ship 4+ new components? **No** — three method-body edits in one file.
- Depends on a new abstraction? **No**.
- Disproves something? **Yes** — that 64-thread stress is required to catch the invariant.
- Synthetic data only? **No** — verified against real CI timing artifacts post-merge.
- Identical-except-for-scale duplicate of another slice? **No**.

All taste tests pass.

## Mutation testing note

Mutation kill rate is the load-bearing assurance for this slice: if reducing stress weakens the test's ability to catch real `Cache<,>` mutations, the kill rate drops. Hold merge until Stryker re-runs.
