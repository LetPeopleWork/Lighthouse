# Mutation testing — 5877 (update queue is a single lane)

Run 2026-09-19 against `main` @ `1dcf617da` + slice 01. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **59.40 %** | 215 | 117 | 76 | 22 | 3 m 5 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — |

Config: `stryker.5877.backend.json`.

Frontend is N/A rather than skipped: slice 01's only frontend change is a comment in
`ActivitySection.tsx` whose claim ("the queue runs a single piece of work at a time") stopped being
true, plus the Vitest case that now pins what replaced it. No production logic changed, so there is
nothing there to mutate.

## The runner set excludes the acceptance suite, and that is what the headline number measures

Decided 2026-09-19 by the maintainer, during the run: Stryker may not drive the integration and
acceptance tests. Each of those boots a `WebApplicationFactory`, and with them in the runner set the
baseline plus coverage capture alone took 18 minutes and the whole run passed 80 without finishing.
Without them it is 3 minutes, which is the difference between a gate that gets run and one that does
not.

The cost is that **the headline 59.40 % is not a statement about how well this code is tested.** It is
a statement about how well it is tested *by unit tests alone*, and two of the six mutated files are
covered almost entirely by the acceptance suite that was excluded. Read the per-file table, not the
total.

| file | score | tested | killed | timeout | survived | no coverage |
| --- | --- | --- | --- | --- | --- | --- |
| `UpdateLanes.cs` | **100 %** | 9 | 7 | 2 | 0 | 0 |
| `ForecastUpdater.cs` | **84.6 %** | 39 | 29 | 4 | 4 | 2 |
| `UpdateController.cs` | **81.2 %** | 16 | 11 | 2 | 3 | 0 |
| `UpdateLane.cs` | n/a | 0 | — | — | 0 | 0 |
| `UpdateQueueService.cs` | 52.3 % | 153 | 66 | 14 | 66 | 7 |
| `WriteBackRound.cs` | 23.5 % | 17 | 4 | 0 | 3 | 10 |

`UpdateLane.cs` reports n/a because both its mutants are the switch arms of a total mapping, which
Stryker skips: the switch has no fallback arm on purpose, so a mutated arm does not compile.
`UpdateLaneMappingTest` covers the mapping directly.

## Closed by this pass

Three rounds were run. The first scored 55.13 %, and the survivors it exposed produced three pieces of
test:

- **`UpdateLanesTest.cs`** (new, 7 cases). The lanes had no test of their own at all — everything about
  them was observed through a running host. Now pinned directly, and fast: work of one kind stuck does
  not stop work of another kind; two of the same kind run one after the other; a removal waits for the
  refresh of the same kind of entity; work that throws is named in the log and its lane carries on; a
  drain returns only once work in flight is done; a closed lane refuses work. Took the file from
  66.7 % to **100 %**.
- **`ForecastUpdaterTest`** — the check that stands a second forecast down is `IsHeld(key) ||
  HasQueuedWork(key)`, and the one scenario covering it set *both* halves true, so it could not tell an
  `or` from an `and`. Split into two cases, one per half. 79.5 % → **84.6 %**.
- **`UpdateControllerTest`** — the cancel route and the elapsed-time clamp had no unit coverage at all
  (7 mutants with no test touching them). Four cases added: a removal is refused and nothing is
  stopped, a refresh is passed on whatever state it is in, elapsed time is measured on the instance
  clock, and work that started fractionally in the future reads as just started rather than as a
  negative duration. 50 % → **81.2 %**.

## Accepted survivors

`UpdateQueueService.cs`, 73 unkilled mutants, categorised rather than listed one by one:

- **30 log statements or their wording**, **1 other string literal**, **5 disposal calls**, **1 browser
  push**. The standing position in `docs/ci-learnings.md` is that log-message mutations are acceptable
  survivors; pinning them would be asserting on log wording, which is not behaviour anybody depends on.
- **36 behavioural mutants** in the run/cancel/rerun/hold paths — `statusStore.Advance`, `Requeue`,
  `cancellations.Admit`/`Forget`, the `startedRunning` flag, the rerun enqueue. These are not untested.
  They are the paths the acceptance suite drives end to end, and that suite is the thing this run
  deliberately does not use. The same mutants under a runner set that includes it would need a
  separate, slow run to prove; this file's number should be read as "unit tests alone do not cover the
  queue's execution paths", which is true and is by design — the queue is observed through the port.

`WriteBackRound.cs`, 13 unkilled: 12 behavioural and 1 string. Its behaviour test is
`WriteBackRoundConcurrencyTest`, which lives in the acceptance folder and is excluded by the same
decision. The file was rewritten by step 01-01 precisely so two executions could share a round safely,
and that is asserted — just not by a test this run can see.

Equivalent mutants, kept as survivors because no test can meaningfully kill them:

- `UpdateController.cs:69` — `First()` → `FirstOrDefault()` on a `GroupBy` group. A group produced by
  `GroupBy` is never empty, so the two are the same call.
- `ForecastUpdater.cs:146` — `Append()` → `Prepend()` on the set of refreshes a forecast waits for.
  The set is asked "does it still contain anything", never "what is first".
- `ForecastUpdater.cs:196`/`199` — stopping a stopwatch and reporting a duration to a summary line.

## Not mutated

Nothing was excluded from `mutate`. `UpdateQueueService.cs` (673 lines) and `ForecastUpdater.cs` (242)
are large files this slice changed in part, so their scores describe the whole file rather than the
change; that is recorded here rather than hidden by narrowing the globs.
