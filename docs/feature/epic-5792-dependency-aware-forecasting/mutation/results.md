# Mutation testing — 5826 (Dependencies: one forecast per Portfolio per refresh batch)

Run 2026-08-22 against `main` @ `ada2a86cc`. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **55.23 %** | 288 | 167 | 98 | 56 | 23 | 23 m 56 s |
| Frontend (StrykerJS) | N/A | — | — | — | — | — | — |

Frontend is **N/A, because slice 00 changed no frontend file** — the whole slice is backend scheduling.
Verified with `git diff 8666a1783..HEAD --stat -- 'Lighthouse.Frontend/*'`, which is empty.

Config: `stryker.5826.backend.json`. Score counts `NoCoverage` against the total, which is why it sits
below the raw killed/survived ratio.

**The gate is not met, and this was accepted by the maintainer rather than ground out.** The reasoning is
below, per file, and the two things worth fixing were fixed as code rather than as tests.

## Per file

| file | killed | survived | no coverage | timeout |
| --- | --- | --- | --- | --- |
| UpdateQueueService.cs | 45 | 36 | 7 | 19 |
| UpdateServiceBase.cs | 38 | 22 | 0 | 2 |
| PortfolioUpdater.cs | 19 | 9 | 3 | 0 |
| WriteBackRound.cs | 3 | 10 | 0 | 0 |
| TeamRepository.cs | 6 | 8 | 0 | 0 |
| PortfolioRepository.cs | 4 | 4 | 12 | 0 |
| WriteBackCollector.cs | 6 | 4 | 0 | 0 |
| ForecastUpdater.cs | 29 | 3 | 2 | 1 |
| InProcessUpdateStatusStore.cs | 12 | 1 | 1 | 1 |
| RedisUpdateStatusStore.cs | 0 | 0 | 31 | 0 |
| RefreshRoundSummary.cs | 0 | 1 | 0 | 0 |
| FeatureRankChangedForecastTriggerHandler.cs | 3 | 0 | 0 | 0 |
| TeamDataRefreshedForecastTriggerHandler.cs | 1 | 0 | 0 | 0 |
| FeatureOrderingPolicyChangedForecastTriggerHandler.cs | 1 | 0 | 0 | 0 |

## The 31 `RedisUpdateStatusStore` mutants are a config artefact, not a test gap

`RedisUpdateStatusStore` has real tests — `Integration/Containers/UpdateStatusStoreContainerTests.cs`
drives it against a Redis container and compares every answer against the in-process store. They pass.

They did not run here because this run's `test-case-filter` whitelisted test names by namespace fragment
and never listed `Integration.Containers`, so Stryker saw the adapter as untested code. The filter has
been corrected in `stryker.5826.backend.json`; **the run was not repeated**, because a 24-minute rerun to
move a number nobody disputes was not worth the wall clock. Expect roughly 31 further kills and a score
near 64 % on the next run of this config, before any new test is written.

## Accepted survivors

**Log statements and log wording — about 38 of the 154 unkilled mutants.** Every `String mutation`
survivor in this set rewrites a log message, and ten of `UpdateQueueService`'s statement survivors delete
a `logger.*` call outright (lines 68, 90, 94, 116, 133, 150, 210, 283, 331, 357), with the same shape in
`UpdateServiceBase`. This codebase already treats that class as unkillable on purpose: Stryker's own
ignore-reasons in this run record the convention, that what a test pins is the *level* an operator sees a
thing at, not the sentence. Asserting on message text would pin prose and break on every reword.

**`GC.SuppressFinalize(this)` (UpdateQueueService.cs:411).** Removing it changes finalisation cost, not
observable behaviour. Equivalent mutant.

**`registration.Dispose()` in `RegisterCancellation` (UpdateQueueService.cs:250).** Leaks a cancellation
registration; observable only as memory growth over a long run, which no unit test can see.

**`WriteBackRound` — 10 survivors on 13 mutants.** The round is refcount plumbing exercised indirectly
through the queue rather than directly. Its behaviour *is* covered end to end by
`Slice00OneForecastPerBatchScenarios`, which asserts one conversation with the work tracking system per
refresh round — but that test kills the round's mutants only where they change the flush decision, and
most of these change bookkeeping the scenario cannot observe. Direct unit tests for `WriteBackRound`
would close this; they are the highest-value follow-up here.

## Fixed as code, not as tests

Five defects in `UpdateQueueService` that this run helped surface were repaired with regression tests
rather than left as survivors. Chiefly: a hold's `Join()` on its round leaked whenever the released work
returned early without taking the handover, so that round never flushed and its staged write-back was
lost silently; and `ReleaseClearedHolds()` ran unguarded before the completion publish, so a throw from
caller code stranded cross-pod awaiters. Both were survivors here — `UpdateQueueService.cs:191` and
`:364` — which is mutation testing doing its job.

## Outstanding — genuine gaps, not accepted

These are real and were left for a follow-up rather than fixed tonight:

- `statusStore.Advance(updateKey, UpdateProgress.InProgress)` (UpdateQueueService.cs:272 and :345) —
  nothing asserts an update is observable as in-progress while it runs.
- `statusStore.Requeue(updateKey)` (UpdateQueueService.cs:325) — the coalescing path's requeue is
  unasserted, so nothing catches the idle window it exists to close.
- `PortfolioRepository` — 12 mutants with no coverage at all.
- `TeamRepository` — 8 survivors on 14 mutants.
- `UpdateServiceBase` — 11 non-log statement survivors.

## Not mutated

Excluded from `mutate` because Stryker.NET ignores line ranges and mutates whole files, which would bury
this slice's score under untouched code:

- `WorkItems/WorkItemService.cs` — 13 changed lines in 1173.
- `Program.cs` — 10 changed lines in 1547, all dependency-injection wiring.
- `API/TeamController.cs` — 11 changed lines in 236.
- `Data/LighthouseAppContext.cs`, `Models/Team.cs`, `Models/RefreshLog.cs` — EF model shape and property
  changes, no branching logic.
- The `Services/Interfaces/**` files in the diff — interface declarations carry no mutants.
