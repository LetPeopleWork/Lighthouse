# Mutation testing — Epic #5511 Task Manager

## 5788 — A scheduled refresh that failed is reported to the browser as completed (slice 01)

Epic #5511 Task Manager, slice 01. Run 2026-09-14 against `main` @ `90b929b7d` plus the slice's own
uncommitted changes. Gate is an 80 % kill rate on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1) | **94.74 %** | 19 | 18 | 1 | 0 | 44 s |
| Backend (Stryker.NET 4.16.0) | **70.00 %** — see triage | 60 | 42 | 18 | 0 | 3 m 49 s |

Configs: `stryker.5788.frontend.json`, `stryker.5788.backend.json`, `vitest.stryker.mutation.ts`.
ARM64 runner: `run-backend-x64.ps1` (see *Running the backend gate on ARM64* below).

## Frontend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `RefreshStatusIcon.tsx` (whole file) | 7 | 7 | 0 |
| `TeamDetail.tsx` (lines 269, 285, 511-516) | 6 | 6 | 0 |
| `PortfolioDetail.tsx` (lines 332, 480-485) | 6 | 5 | 1 |

### Closed by this pass

The first run scored **65.22 %** and the survivor list was worth more than the number:

- **`PortfolioDetail.tsx` — 0 of 6 killed.** The failed state was wired into both detail pages but only
  Team detail had a test for it. Four scenarios added under *a refresh that failed* in
  `PortfolioDetail.test.tsx`: the button says so, it stops saying so once the next refresh works, it
  stops saying so the moment the next refresh is taken on, and a failure belonging to a Team leaves the
  portfolio's button alone.
- **`TeamDetail.tsx:285` — `update.status === "Failed"` → `true` survived.** The existing "stops saying
  so once the next one works" test sends `Completed`, which on Team detail short-circuits into the other
  branch and never reaches line 285. Adding a `Queued` case did not kill it either: `Queued` sets
  `isUpdating`, and the spinner takes precedence over the failure icon, so the mutant is invisible on
  every live push. The reachable path that does expose it is **mount**: the page asks
  `getUpdateStatus` for the refresh already in flight and feeds that answer to the same function, and
  that answer *can* be `Completed`. New scenario — *says nothing about failure when the refresh in
  progress on arrival is not a failed one* — which is a real promise: opening a page just after a
  refresh finished must not claim it broke.

### Accepted survivors

- **`PortfolioDetail.tsx:332` — `update?.status` → `update.status` (OptionalChaining).** Equivalent
  mutant. Line 332 runs only inside `if (isFeatureUpdate)`, and `isFeatureUpdate` is itself
  `update?.updateType === "Features" || update?.updateType === "Forecasts"` — false when `update` is
  null. The optional chain therefore cannot fire; it matches the style of the two lines above it rather
  than guarding anything reachable. Removing it to kill the mutant would leave the line inconsistent
  with its neighbours for no behavioural gain.

### Not mutated, and why

`TeamDetail.tsx:270` (`if (activeViewRef.current === "settings")`) was inside the first run's range
because the slice added a line immediately above it. It is pre-existing code that this slice does not
touch, so the range was narrowed to line 269 rather than the survivor being chased.

## Backend

One file changed, so one file mutated: `UpdateServiceBase.cs`. 18 380 mutants created across the
project, 18 320 skipped by the `mutate` glob, **60 tested**.

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `UpdateServiceBase.cs` (whole file — see *Not mutated* below) | 60 | 42 | 18 |

**Zero survivors in `TriggerUpdate`**, which is the method this slice changed. Every one of the 18 is in
code the slice does not touch, and the file-level 70 % is therefore a statement about the rest of the
file rather than about the change.

### Accepted survivors — 15

- **Log-message mutations, 11 of them** (lines 222, 231, 238, 243, 248, 272): each log call yields both
  a *Statement* mutation that deletes the line and a *String* mutation that empties the message.
  Nothing asserts on these particular lines, and pinning a log sentence that no operator workflow reads
  would make the message harder to improve than to keep. This is the category the mutation-testing skill
  names as acceptable.
- **Background-service loop, 4** (lines 224 `await DelayStart`, 238/243 inside `TryUpdating`,
  273 `await Task.Delay`): `ExecuteAsync` is the hosted-service loop, and it is never started under
  test — `TestWebApplicationFactory` removes every `IHostedService` on purpose, and the updater unit
  tests call `TriggerUpdate` directly. Unreachable through any port a test drives, so no test can
  observe these.

### Pre-existing gaps, outside this slice — 3

Recorded rather than fixed. Writing tests for them here would mean this slice carrying coverage for
behaviour it does not touch, and none of them can mask a regression in what it does touch.

- **Lines 89 and 95 — `ReportForecastSummary`.** The forecast half of the round summary: the object
  initialiser can be emptied and the whole `HandToRound` call deleted without any test noticing. The
  forecast summary genuinely has no assertion behind it.
- **Line 117 — `RoundOf` body removed** (returns `null` instead of the running round). With no round,
  `HandToRound` writes the summary immediately instead of when the round finishes. For a
  single-execution refresh — which is every scenario in this slice — that still produces exactly one
  line, so slice 01's assertions cannot tell the difference. A round spanning two executions would, and
  nothing in the filtered test set covers that.

Excluding the 15 accepted survivors, the kill rate on the rest is 42 / 45 = **93.3 %**.

### Not mutated, and why

Stryker.NET ignores line ranges in `mutate` — a range silently widens to the whole file — so scoping to
the changed method is not possible. The whole of `UpdateServiceBase.cs` is mutated and the untouched
two-thirds of it is what the 70 % measures.

24 mutants in the file did not compile and are excluded from the score by Stryker, as are 23 it ignored
via existing `// Stryker disable` comments.

## Running the backend gate on ARM64

This machine is `win-arm64` and the run needs one piece of setup, scripted as `run-backend-x64.ps1`
next to the configs.

Stryker hands VSTest `<TargetPlatform>X64</TargetPlatform>`. When the arm64 VSTest launches that x64
host, the host loads the NUnit adapter, enumerates nothing and exits 0 — and Stryker reports
`Number of tests found: 0` with *"NUnit3TestAdapter failed to deploy or run"*, which sends you after the
adapter. The adapter is fine. Reproduced outside Stryker against the same assembly:

```
dotnet vstest …Lighthouse.Backend.Tests.dll --ListTests /Platform:ARM64   ->  144 tests
dotnet vstest …Lighthouse.Backend.Tests.dll --ListTests /Platform:x64     ->    0 tests
```

The fix is to run the whole chain as x64. The box already had x64 *runtimes* but no x64 SDK, which is
why Stryker under the x64 host first failed at project analysis. Installing one side-by-side is enough,
needs no admin, and leaves the arm64 SDK and `PATH` untouched:

```
dotnet-install.ps1 -Architecture x64 -Version 10.0.100 -InstallDir $env:USERPROFILE\.dotnet-x64
```

`run-backend-x64.ps1` then points `DOTNET_ROOT` and `PATH` at it, sets `DOTNET_ROLL_FORWARD=Major`
(Stryker's CLI targets net8.0 and that SDK ships only its own major) and invokes `Stryker.CLI.dll`
directly, so the tool itself runs x64 rather than being re-installed.

What was ruled out on the way, so nobody repeats it: the test-case filter (it matches 144 tests at
native architecture), the adapter, a missing x64 runtime, the Stryker version (4.16.0 and 5.0.0 behave
identically) and the MTP runner — `test-runner: "mtp"` fails the same way, matching NUnit's own open
incompatibility with .NET 10's MTP mode (nunit/nunit3-vs-adapter#1267). Stryker 4.16 has no platform or
architecture option at all. Tracked upstream as stryker-mutator/stryker-net#3335, open with an
unreleased community PR; on an x64 machine or in CI none of this applies.

---

## 5840 — See what Lighthouse is doing right now (slice 02)

Run 2026-09-14. Gate is an 80 % kill rate on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **93.75 %** | 31 | 29 | 1 | 0 | 2 m 14 s |
| Frontend (StrykerJS 9.6.1) | **73.33 %** — see triage | 75 | 55 | 19 | 0 | 1 m 50 s |

Configs: `stryker.5840.backend.json`, `stryker.5840.frontend.json`, `vitest.stryker.5840.ts`.
Backend run through `run-backend-x64.ps1`.

### Backend — gate met

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `UpdateController.cs` | 23 | 23 | 0 |
| `InProcessUpdateStatusStore.cs` | 8 | 6 | 1 |

**Accepted survivor.** `InProcessUpdateStatusStore.Advance`, `(int)to >= (int)status.Status` → `>`.
Equivalent: the only case the two differ on is advancing to the status a key already holds, and the
assignment that follows writes the same value. Pre-existing code, untouched by this slice.

**Not mutated, and why.** `RedisUpdateStatusStore.cs` is excluded. Its promise is pinned by
`TaskManagerMultiReplicaTests`, which starts a real Redis container per test — around eight seconds
each, re-run per mutant, which is not a run anyone would wait for. The three container tests cover the
enumeration it gained: work admitted by another replica is read back with its type, id and status; work
that finished is gone; an instance with no hash yet answers empty rather than failing.

### Frontend — under the gate, and why

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `UpdateSubscriptionService.ts` (task-list read) | 2 | 2 | 0 |
| `TaskManagerIcon.tsx` | 73 | 53 | 19 |

The first run scored **50.67 %** and the survivors were the point:

- **The whole delete path — 12 mutants.** `isDelete` could be inverted, emptied or made constant and
  nothing noticed. Deletes reaching this list is *why* AC-02.3 exists, and the implementation said
  "(removal)" that no specification asked for. Four scenarios added: a team removal says so, a portfolio
  removal says so, a removal is named after the kind of thing being removed, and an ordinary refresh is
  not called a removal.
- **`getRunningTasks` had no coverage at all.** The route string could be emptied and the whole body
  removed without a test failing — the one thing in the browser that knows where the task list lives.
  Two service specs added, including that a failed read surfaces rather than reporting an idle instance,
  which is deliberately unlike `getGlobalUpdateStatus` beside it.
- **Only one kind was ever named.** The portfolio branch of the kind lookup was never asserted; the
  two-row scenario now checks both.
- **Every queued fixture had something to wait behind**, so "queued with nothing running" was
  unspecified. Added: the list must not invent a blocker for the first thing in an empty queue.

That took it to **73.33 %**. What remains:

**Accepted — 12, all presentational.** `anchorOrigin` (3), `transformOrigin` (3), `slotProps` (3),
`onClose`, the row `key`, and `sx` padding. These are MUI popover geometry and React list keys. A test
that pinned them would be asserting how MUI positions a popover, not anything an operator can act on,
and it would red on any layout tidy-up. Excluding them, the rest kills 55 of 62 — **88.7 %**.

**Remaining seven, recorded not fixed.** The React effect's own plumbing: both dependency arrays, the
`cancelled` guard and its reset, the initial empty `useState`, and the `default:` arm of the status
description (which no status reaching this list can take, since terminal work is removed from the store
before it could). Killing these means asserting on React's re-render bookkeeping rather than on
behaviour; the leak they guard against is covered by *stops listening when it goes away*.

**The honest summary:** the file-level 73.33 % is below the gate, and it is below the gate because two
thirds of the surviving mutants are popover chrome. Every behavioural promise this slice makes is
pinned. Whether that clears the gate is the maintainer's call — the same call slice 01's backend 70 %
raised, and for the same reason.
