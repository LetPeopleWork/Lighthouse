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

---

## 5841 — See how long an update has been running (slice 03)

Epic #5511 Task Manager, slice 03. Run 2026-09-14 against `main` @ `f3f1ff87c`. Gate is an 80 % kill
rate on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1) | **100.00 %** | 37 | 37 | 0 | 0 | 29 s |
| Backend (Stryker.NET 4.16.0) | **95.45 %** | 65 | 62 | 2 | 1 | 4 m 09 s |

Configs: `stryker.5841.frontend.json`, `stryker.5841.backend.json`, `vitest.stryker.5841.ts`.
ARM64 runner: `run-backend-x64.ps1`.

Both stacks were run twice. The first pass scored 91.89 % frontend and 90.91 % backend; everything
below under *Closed by this pass* is the difference, and all of it was worth having.

### Frontend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `formatElapsed.ts` (whole file) | 32 | 32 | 0 |
| `TaskManagerIcon.tsx:51-64` | 5 | 5 | 0 |

#### Closed by this pass

Three survivors, all the same shape: `<` relaxed to `<=` at each unit switchover in `formatElapsed`.
They survived because the specs asserted 12 s, 3 m, 64 m and 51 h — values comfortably inside a band,
never on its edge. The boundaries are pinned exactly now, at one minute, one hour and one day.

Worth more than the score suggests. These are the points a reader watches a row cross while the
popover is open, and getting one wrong by a millisecond renders "60s" or "60m" — a unit that has run
out, which reads as a broken page rather than as elapsed time.

### Backend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `UpdateMoments.cs` (whole file) | 31 | 31 | 0 |
| `UpdateController.cs` (whole file) | 19 | 18 | 1 |
| `InProcessUpdateStatusStore.cs` (whole file) | 15 | 13 | 1 + 1 timeout |

#### Closed by this pass

- **`InProcessUpdateStatusStore`, `&&` relaxed to `||`** in the guard that decides when a start moment
  is recorded. Under the mutant, work advancing straight from waiting to finished gets a start moment
  it never earned — and the task list then reports a refresh that never ran as having been running all
  along. Closed by `Advance_StraightFromWaitingToFinished_RecordsNoStartMoment`, with its positive
  control beside it.
- **`UpdateMoments`, the written form replaced wholesale.** Nothing asserted what the stored value
  actually looks like, only that it round-tripped — which a mutant that corrupts both directions
  equally survives. The form is now pinned as unix milliseconds either side of a bar. It is a contract
  between Lighthouse versions rather than an internal detail: the replica that writes it is often not
  the one that reads it.
- **`UpdateMoments`, the lower end of the range guard.** The guard added after the adversarial review
  had a test on its upper bound and none on its lower, so `>=` relaxed to `>` went unnoticed.

#### Accepted survivors

- **`UpdateController.cs:102`** — `elapsed < TimeSpan.Zero` relaxed to `<=`. Equivalent: at exactly
  zero the true branch returns `0` and the false branch returns `(long)0.0`. There is no input that
  tells them apart.
- **`InProcessUpdateStatusStore.cs:41`** — `(int)to >= (int)status.Status` relaxed to `>`. Advancing a
  key to the status it already holds is a no-op under either operator. The one state that would
  distinguish them — running with no start moment recorded — is not reachable through the port; it can
  only be written into the dictionary by hand.
- **`InProcessUpdateStatusStore.cs:74`, block removal, reported as a timeout.** Emptying
  `GetAdmittedWork` hangs the acceptance harness, which polls that method waiting for the queue to go
  idle. Stryker counts a timeout as detected, and it is: the suite does notice.

#### Not mutated

`RedisUpdateStatusStore.cs` is excluded, as it was for slice 02, and the reason is unchanged: most of
it is only reachable against a real Redis, and the tests that reach it start a container per test.
Under `perTestInIsolation` that is several container starts per mutant across roughly 150 mutants.

What covers it instead: `TaskManagerMomentsMultiReplicaTests` and `UpdateStatusStoreContainerTests`
against a real Redis for the moments, the two Lua guarantees and the no-orphan promise;
`RedisUpdateStatusScriptFreezeTest` for both scripts, character for character, verified by mutating
one of them by hand; and `RedisUpdateStatusStoreBestEffortMomentsTest`, which drives the failure paths
through a mocked `IDatabase` — the paths a working Redis cannot be asked to demonstrate.

---

## 5842 — Cancel a queued or running update (slice 04)

Epic #5511 Task Manager, slice 04. Run 2026-09-14 against `main` @ `99b1249b6`. Gate is an 80 % kill rate
on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **89.19 %** | 35 | 33 | 2 | 0 | 5 m 51 s |
| Frontend (StrykerJS 9.6.1) | **66.67 %** — see triage | 6 | 4 | 2 | 0 | 23 s |

Configs: `stryker.5842.backend.json`, `stryker.5842.frontend.json`, `vitest.stryker.5842.ts`.
ARM64 runner: `run-backend-x64.ps1`.

Both stacks ran twice; everything under *Closed by this pass* is the difference.

### Backend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `AdmittedCancellations.cs` (whole file) | 14 | 13 | 1 |
| `UpdateController.cs` (whole file) | 13 | 12 | 1 |
| `InProcessUpdateCancellationNotifier.cs` (whole file) | 6 | 6 | 0 |
| `UpdateCancellationContext.cs` (whole file) | 2 | 2 | 0 |

#### Closed by this pass

- **`Forget` dropping the source without releasing it.** Nothing noticed, because both the lookup and the
  cancel answer the same way once the key is out of the dictionary. Every refresh this instance runs passes
  through here, so the mutant is a handle leaked per refresh for the life of the process. Killed by holding
  the token across the call and asking for its wait handle — the part that holds an operating-system
  resource, and the only part whose release is observable.
- **A disposed subscription still hearing cancellations.** `InProcessUpdateCancellationNotifier.Dispose`
  could stop removing the handler and no test cared. The handler closes over the whole queue, so the leak
  is not a callback — it is a cancel delivered to a queue that has gone.

#### Accepted survivors

- **`UpdateController.cs`** — `elapsed < TimeSpan.Zero` relaxed to `<=`. Equivalent, and already accepted
  for the same reason in slice 03: at exactly zero both branches produce `0`.
- **`AdmittedCancellations.cs`** — the losing side of a concurrent `Admit` not disposing the source it
  failed to insert. Behaviourally invisible by construction: the loser's source was never handed to
  anybody. It is a resource-only mutant on a path reachable solely by two threads admitting one key in the
  same instant.

### Frontend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `UpdateSubscriptionService.ts:239-248` | 2 | 2 | 0 |
| `TaskManagerIcon.tsx:86-105` | 4 | 2 | 2 |

#### Closed by this pass

The first run scored **33 %** and the reason was a genuine hole rather than a scoring artefact:
`cancelTask` had no service-level test at all. The popover specs drove the button, so the component was
covered while the method it called was not — the whole of `UpdateSubscriptionService.ts` reported *no
coverage*. Three specs now pin which entity is asked about, that the update type is spelled as the instance
spells it, and that a refusal reaches the caller rather than being swallowed into looking like success.
That is the one the popover depends on: it re-reads the list on the strength of this returning.

#### Accepted survivors, and why the stack is under its gate

Both are `ArrayDeclaration` on React `useCallback` dependency arrays — `[updateSubscriptionService]` and
`[refresh, updateSubscriptionService]`. Emptying either leaves the callback closing over a stale value,
which changes nothing a test can see without re-rendering with a different service and asserting on which
instance was called. That is a test of React's memoisation rather than of anything Lighthouse promises,
and the project's doctrine is to test behaviour.

So the denominator is four behavioural mutants and two framework ones, and **66.67 % is four of six with
both survivors equivalent**. On a denominator this small the percentage says less than the list does.

For context, the whole of `TaskManagerIcon.tsx` scores **71.91 %** across 89 mutants, the survivors
dominated by MUI popover geometry and effect bookkeeping — the same shape, and close to the same number,
that slice 02 recorded at 73.33 % and chose to record rather than engineer away. The narrow scope is kept
here because it answers the question the per-feature gate asks: was *this slice's* change tested well.

### Not mutated

- **`UpdateQueueService.cs`** — excluded. The cancellation it carries is now in `AdmittedCancellations`,
  which is mutated whole; what remains is the queue mechanics slices 01 and 02 already exercise, and
  mutating a 600-line class to re-measure them would bury this slice's score under untouched code.
- **`RedisUpdateCancellationNotifier.cs`** — excluded for the reason its sibling store is: the tests that
  reach it start a container per test, which under `perTestInIsolation` is several container starts per
  mutant. Covered instead by `TaskManagerCancellationMultiReplicaTests` against a real Redis.
- **The Jira paging walks** — not in this run. They are covered by `JiraCancellationGranularityTest`,
  which counts real HTTP round trips and was itself verified by hand-mutating the two calls to
  `CancellationToken.None`, killing both Data Center tests. That check is recorded in the commit rather
  than here because it was a deliberate one-off, not a Stryker run.

---

## 5019 — I get warned when any connection's authentication breaks, not just OAuth (slice 05)

Epic #5511 Task Manager, slice 05. Run 2026-09-15 against `main` @ `5372e8353` plus the slice's own
uncommitted changes. Gate is an 80 % kill rate on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **91.89 %** | 37 | 34 | 3 | 0 | 2 m 41 s |
| Frontend (StrykerJS 9.6.1) | **86.96 %** | 69 | 60 | 9 | 0 | 1 m 38 s |

Configs: `stryker.5019.backend.json`, `stryker.5019.frontend.json`, `vitest.stryker.5019.ts`.
ARM64 runner: `run-backend-x64.ps1`. Pass `-TestProject` explicitly — `$PSScriptRoot` does not populate
in the param-block default under `powershell -File`, and the script then resolves the test project to
`C:\..\..\..\..` and dies before Stryker starts.

Both stacks started below the gate. The survivors are the reason this pass exists, so they are recorded
in the order they were found.

### Backend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `ConnectionHealthService.cs` | 32 | 29 | 3 |
| `ConnectionHealthController.cs` | 5 | 5 | 0 |

First run: **75.00 %**. Three clusters, all real.

| Probe | What it exposed |
|---|---|
| the unreadable-secret message blanked to `""` | **Nothing asserted the message at all.** The scenario proved the state was `AuthenticationFailed` and that the tracker was never contacted — but the sentence naming *which field to re-enter* was unpinned, and that sentence is the entire reason this case does not read as a rejected token. |
| `verdict ??= new(...)` → `verdict = new(...)`, and the `isNew` branch inverted | **No connection ever failed twice.** Under the mutant the second failure inserts a second row, loses to the unique index, and the resulting `DbUpdateException` is swallowed as non-fatal — so the connection goes on showing the *first* cause forever and an administrator chases the wrong problem. |
| `CredentialFor`'s predicate `==` → `!=` (no coverage) | **Test connection had never been pressed on an OAuth connection.** The button could have answered `Healthy` for a connection whose grant Lighthouse already knew was broken. |

Three scenarios closed them. The second run reached **87.50 %** and left two of the three clusters'
mutants alive, which turned out to be the more interesting finding:

| Probe | What it exposed |
|---|---|
| `verdictRepository.Update(verdict)` removed | **Equivalent — and the call was therefore dead.** An existing verdict is read through the same scoped context, so writing to its properties is enough to have `Save` persist them. `Update` read as though it were doing something. Deleted, along with the `isNew`/`??=` branch it justified. |
| `CredentialFor`'s predicate, still | **The Test connection route's own answer was never asserted.** Every scenario re-read `GET /connectionhealth` afterwards, so the `POST` could have returned anything — and the popover writes that response straight into the row it was pressed from. `ThenTheTestItselfAnswered` now pins it. |

Final: **91.89 %**, 3 survivors, all equivalent:

| Survivor | Why it cannot be killed meaningfully |
| --- | --- |
| `ArgumentNullException.ThrowIfNull(connection)` ×2, removed | Defensive guards on two methods only the updaters call, and they always pass a loaded connection. Nothing reachable through a port can observe the difference. |
| the early `return` in `RecordRefreshSucceededAsync`, removed | `RepositoryBase.Remove(T?)` is null-tolerant, so dropping the guard behaves identically. What it actually costs is a pointless `Save` on **every** successful refresh — a performance difference no assertion at the port can see. |

### Frontend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `connectionHealthWording.ts` | 45 | 44 | 1 |
| `ConnectionsSection.tsx` | 24 | 16 | 8 |

First run: **75.00 %**. What it found, beyond missing tests:

| Probe | What it exposed |
|---|---|
| the `Tooltip` title on each row | **The connector's message — the half that says what to do — was only reachable by hovering a row inside a popover.** Killing the mutant and fixing the design were the same change: the explanation is now rendered under the row for a connection that is broken. |
| `if (connections.length === 0)` → `false` | No test rendered an instance with no connections at all. |
| `Unreachable: "Unreachable"` → `""` | The third rung of the wording table was never read. |
| `"primary"` → `""`, and the `some(Unreachable)` condition | No test asserted the ordinary badge colour, or a list where only *one* of several connections is unreachable. |
| the Edit button's `navigate(...)` arrow → `() => undefined` | Nothing asserted the route goes anywhere. The spec now renders a real `/connections/:id/edit` route and clicks through to it. |

Second run: **84.06 %**, and it caught a test that was passing for the wrong reason. The icon reads
`Activity` *before* the health read returns, so
`leaves the icon its ordinary colour when no connection is in trouble` was asserting against an empty
list rather than against the data. A hand-mutation to an unconditional `true` did **not** reveal this —
only Stryker's narrower mutation of the `some` callback did, because with an empty array both agree.
Both affected specs now wait for a row to render first. Worth generalising: **a component spec that
asserts an absence has to wait for the data whose arrival would change the answer**, and the wait has to
be on something the data itself produces.

Third run: **85.51 %**, one real survivor left — `isBroken(connection) && connection.message` → `||`.
A connection that has just been tested successfully carries a message too ("Connection validated
successfully"), so the `&&` is doing real work: the fixture was strengthened from a connection with no
message to a healthy one that has one.

Final: **86.96 %**, 9 survivors, all cosmetic:

| Survivor | Why it cannot be killed meaningfully |
| --- | --- |
| 8 × MUI `sx` layout props (`{ py: 0.5 }`, `{ display: "flex", … }`, `{ flexGrow: 1 }`, `{ display: "block" }`, `" "`) | jsdom does not compute emotion's styles, so there is nothing to assert. Pinning them would mean asserting the `sx` object itself, which is a copy of the source rather than a claim about behaviour. |
| `.join(", ")` → `""` in the header tooltip | The separator between two named connections. The spec asserts both names are present, which is the claim; asserting the exact punctuation between them buys brittleness and no defect. |

### Not mutated, and why

`TaskManagerIcon.tsx` is excluded. Its slice-05 change is the wiring — one more fetch in `refresh`,
one more callback, and the two sections it now composes — and mutating the whole file would bury this
slice's score under slices 02, 03 and 04's code, which has its own recorded runs. The logic that moved
out of it (`connectionHealthWording.ts`) is mutated at 97.78 %.

`UpdateServiceBase.RecordConnectionHealth` is excluded for the same reason: 20 lines added to a
276-line file the previous four slices already mutated. Its two branches — the cancel that records
nothing and the success/failure split — are pinned by
`A_refresh_an_operator_stopped_says_nothing_about_the_credential` and by the four refresh scenarios
either side of it.

## 5843 — The warnings, without reading the log (slice 06)

Epic #5511 Task Manager, slice 06. Run 2026-09-15 against `main` @ `d4f8532ac` plus the slice's own
uncommitted changes. Gate is an 80 % kill rate on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **95.65 %** | 23 | 22 | 1 | 0 | 4 m 46 s |
| Frontend (StrykerJS 9.6.1) | **66.67 %** — see triage | 18 | 12 | 6 | 0 | 42 s |

Configs: `stryker.5843.backend.json`, `stryker.5843.frontend.json`, `vitest.stryker.5843.ts`.
ARM64 runner: `run-backend-x64.ps1`, `-TestProject` passed explicitly.

### Backend

First run **82.61 %**, 4 survivors. Three were real gaps in new code and were closed:

| Survivor | What it showed |
| --- | --- |
| `RecentProblemsSink.cs:51` — `ArgumentNullException.ThrowIfNull(logEvent)` deleted | Nothing pinned that a null event is refused. |
| `LoggingConfigurator.cs:27` — `ArgumentNullException.ThrowIfNull(recentProblems)` deleted | Same shape, same gap. |
| `RecentProblemsSink.cs:100` — `shortName.Length == 0 ? TheApplicationItself : shortName` forced to `shortName` | The existing test reached the `Lighthouse` fallback by the *other* road — a missing `SourceContext`, which returns early at line 92. A `SourceContext` that ends in a separator (`"Lighthouse.Backend.Services."`) is the only way to reach the trimming and get an empty string back, and nothing exercised it. |

All three confirmed by applying the mutations **simultaneously** and rebuilding: exactly three failures, one per
mutant, no collateral — which is what shows they were genuine gaps rather than incidentally covered. The two
guard tests assert the refusal names its parameter, since a guard that throws about the wrong argument is not
the guard it looks like.

#### Accepted survivor

- **`LogsController.cs:69`** — the message inside `SetLogLevel`'s catch block blanked to `""`. Pre-existing
  code from an earlier slice, swept in because the config mutates whole files rather than byte ranges. No
  caller branches on the sentence; asserting on it would break the next time someone improves the wording.

### Frontend

`RecentProblemsSection.tsx`, whole file. **Every behavioural mutant was killed — 12 of 12:** all four
mutations of the `problems === null` guard, all three of the `length === 0` branch, the row `.map`, the
navigate handler, and both module constants blanked (the copy sentence and the log route).

The six survivors are the whole of the shortfall, and all are cosmetic:

| Survivor | Why it cannot be killed meaningfully |
| --- | --- |
| 4 × MUI `sx` layout props (`{ my: 1.5 }`, `{ py: 0.5 }`, `{ display: "block", mt: 1 }`, and `display` blanked within it) | jsdom does not compute emotion's styles, so there is nothing to assert. Same class accepted in slice 05. |
| The React `key` blanked | Reconciliation identity, not rendered output. Killing it would mean asserting on React internals. |
| `—{" "}` → `—{""}` | The space between the timestamp and the level. `toHaveTextContent` normalises whitespace, so pinning it buys brittleness and no defect. Slice 05 accepted the same mutant. |

On a denominator of 18, for a component that is 80 lines of almost-pure presentation, the percentage says
less than the list does: excluding the six cosmetic survivors the kill rate is **12 / 12**.

#### A gap the gate is silent on, closed by hand

**StrykerJS does not mutate JSX text content.** The section's empty state — "Nothing has gone wrong since
this instance started." — is JSX text, so no mutant was ever generated for it: it appears in neither the
killed nor the survived list. It was asserted by the substring `/nothing has gone wrong/i`, which would hold
even if the half of the sentence saying *how far back* "nothing" reaches were lost — and that half is the
whole of AC-06.6. Tightened to the full literal. Both module-level constants were already pinned against
literals and both mutants were killed, so this was the only copy in the section standing on a loose match.

### Running the frontend gate — the config cannot run from where it is stored

`vitest.stryker.*.ts` has to be **copied to `Lighthouse.Frontend/`** before the run, and the Stryker config
must reference it by bare filename. Two separate resolution rules make this the only arrangement that works:
`vitest.configFile` resolves against the working directory, and Node resolves `vitest/config` upward from the
config file's own directory — so a config left in `docs/` fails to be found from the frontend, and a config
*pointed at* in `docs/` is found but cannot import vitest, because `node_modules` is under
`Lighthouse.Frontend/`. `.gitignore` already encodes the arrangement — line 450 ignores
`**/vitest.stryker*.ts`, line 453 carves out `docs/feature/*/mutation/` — so the working copy cannot be
committed by accident.

Also: an `include:` entry naming a spec file that **does not exist** leaves the runner with nothing to run
and reports every mutant alive, which is indistinguishable in the report from a real test gap. The existing
ledger note warns about a spec *missing* from the list; this is the same failure from the other direction.
