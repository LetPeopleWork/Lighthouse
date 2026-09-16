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

### 5843b — the rows say whose refresh broke (follow-up to slice 06)

Run 2026-09-15 against `efabdb80e` plus the follow-up's own uncommitted changes. Same configs, with
`RecentProblemsReport.cs` added to the backend `mutate` list.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **95.00 %** | 40 | 38 | 2 | 0 | 6 m 31 s |
| Frontend | not re-run — no production file changed (the section renders `message` verbatim; only a fixture sentence was corrected) | | | | | |

**First run scored 80.00 %**, 7 survivors, all in `RecentProblemsSink.cs` and all in code this change
introduced. Two were assertion-strength gaps that let a *wrong message* pass, because the new promises
asserted the name was present and nothing about what else survived:

| Survivor | The row it produced, which the tests accepted |
| --- | --- |
| `:133` pattern negated — the replaced span widens to the whole sentence | `Lagunitas` — "Error processing update task for" gone entirely. A bare name reads as a heading. |
| `:143` `index < numberAt` — the id token falls outside the span | `Error processing update task for Lagunitas1` — a stray digit where the id was. |

Closed by sharpening one `Then` rather than adding scenarios: the row must still say **what went wrong**,
and must **no longer say the id**. All three naming scenarios inherit both. The second half also pins the
change's actual intent, which nothing previously did — every earlier assertion would have passed against
an implementation that appended the name and left `ID 7` in place.

**`:133` has two mutant shapes and only one dies at the acceptance layer.** Negating the whole pattern
gives the bare-name row above. Negating only the property name makes the search skip to `{Id}`, so the
span becomes just the number and the row reads `…for Team with ID Pliny the Elder` — which still says what
broke, still names the team, and no longer contains the id. **All three sharpened acceptance assertions
pass it.** It dies only to the unit test asserting the captured span. Found because a falsifier run
reported one failure where four were expected; the discrepancy was the finding.

#### Accepted survivors

- **`RecentProblemsSink.cs:136`** — `numberAt < kindAt` → `<=`. **Equivalent**: two distinct property
  names can never resolve to the same token index, and when both are absent both are `-1`, where the
  short-circuiting first clause has already returned. Verified empirically under the mutation (29/29
  green), not only argued, and independently re-derived in review. The reason is recorded beside the guard
  tests so the next reader gets an answer rather than filling the gap with an assertion about internals.
- **`LogsController.cs:69`** — unchanged from the slice-06 run: a pre-existing log message from an earlier
  slice, no caller branching on it.

#### A load-bearing fixture assumption, recorded because it is invisible

The assertion that the row no longer says the id works because the seeded names in those three scenarios
carry no digits. A team called "Brewery 3" added to them would fail the assertion for a reason that has
nothing to do with the behaviour. The eviction scenario's `Brewery 1…6` names are safe — that scenario
never asserts on the id.

### 5843c — the Update All button stops duplicating the activity icon

Run 2026-09-15. Maintainer's review decision: the button's pending-count badge and its spinner are both
duplicated by the activity icon beside it, so both go; `disabled={hasActiveUpdates}` stays, so the button
still cannot be fired while an update runs. Two tests pinning the removed behaviour were deleted with it.

Frontend config gained `UpdateAllButton.tsx` as a mutate target and `UpdateAllButton.test.tsx` in the
runner's include list. Backend not re-run — no backend file changed.

| file | score | tested | killed | survived |
| --- | --- | --- | --- | --- |
| `UpdateAllButton.tsx` | **76.47 %** | 17 | 13 | 4 |
| `RecentProblemsSection.tsx` | 66.67 % | 18 | 12 | 6 (unchanged, see above) |

**The question this run existed to answer was not the percentage.** Removing the badge and the spinner
could plausibly have orphaned the coverage of the guard that was deliberately kept. It did not: `||` → `&&`
on `isDisabled`, both conditional replacements, and dropping the `!` all die.

#### What it found instead — a pre-existing crash path with no coverage

First run scored 58.82 %, with two survivors and one mutant carrying **no coverage at all**, all pointing
at the same hole: nothing rendered the component with `licenseStatus` unset.

That state is not an edge case. `useLicenseRestrictions` initialises `licenseStatus` to `null`, so it is
null on every initial render, and its fetch swallows errors — so a failed licensing call leaves it null
for the life of the page. The optional chaining is load-bearing there: without it,
`licenseStatus.canUsePremiumFeatures` throws during render and takes the **whole header** down, not just
the button.

Two tests closed it — the button renders and refuses when nothing is known, and it says why. 58.82 % →
76.47 %, and the uncovered mutant is gone.

Worth recording: the second test was nearly skipped on the reasoning that telling somebody to buy a
licence when their licence state is unknown is a product guess. What settled it is that the line above
already makes that decision — treating *unknown* as *not permitted* — so the explanation follows the
stance rather than inventing one. Had it been skipped, the `?? false` → `?? true` mutant would have
survived and been written up as equivalent when it is not.

#### Accepted survivors

| Survivor | Why |
| --- | --- |
| `:28` `if (!isDisabled)` → `true` | Equivalent. MUI does not fire `onClick` on a button carrying `disabled`, so the inner guard cannot be reached in the state it guards against. Defence in depth, not a gap. |
| `:36` tooltip string blanked, `:49` `sx` emptied | The cosmetic class. jsdom computes no styles; asserting them means copying the source. |
| `:35` `?? false` → `&& false` | **A real gap, left open deliberately.** Nothing asserts the *plain* tooltip appears when the licence IS valid, so "always show the premium message" would pass — the positive control for the test added above. Pre-existing, produces wrong copy rather than a crash, and closing it was judged disproportionate to a change that removed a badge. Cheap to close if anyone wants it. |

---

## 6011 — The queue reads like a queue (slice 07)

Epic #5511 Task Manager, slice 07. Run 2026-09-16 against `main` @ `c8932fea0` plus the test
strengthening this pass added. Gate is an 80 % kill rate on each stack that has changed files.

| Stack | Score | Tested | Killed | Survived | Timeout | Duration |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **96.30 %** | 27 | 26 | 1 | 0 | 11 m 49 s |
| Frontend (StrykerJS 9.6.1) | **80.95 %** | 252 | 204 | 48 | 0 | 6 m 56 s |

Configs: `stryker.6011.backend.json`, `stryker.6011.frontend.json`, `vitest.stryker.6011.ts`.
ARM64 runner: `run-backend-x64.ps1`, `-TestProject` passed explicitly. Whole files mutated on both
stacks — the range suffix in `mutate` produces zero tested mutants and a vacuous pass, which is in
`docs/ci-learnings.md`.

Both stacks were run twice. The first pass scored 92.59 % backend and 74.21 % frontend; the frontend
was under its gate and the reason was real rather than a scoring artefact.

**`connectionHealthWording.ts` is mutated although this slice did not in the end change it.** Two
commits in the range touch it and the second undoes the first, so `git diff 9a117cb33..HEAD` shows
nothing for that file. Left in the set because the slice's own commits pass through it, and dropping a
file from the set after seeing it score well is how a gate stops meaning anything. It scores 97.78 %,
the same as it did for slice 05.

### Backend

`AdmittedWorkOrdering.cs` — the file this slice introduced — is at **7 of 7**. Both ranks of the sort
key, the fallback for a missing moment and the sort direction itself all die, and they die on the
acceptance scenarios rather than on a unit test written to meet the gate.

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `AdmittedWorkOrdering.cs` (whole file) | 7 | 7 | 0 |
| `UpdateController.cs` (whole file) | 20 | 19 | 1 |

#### Closed by this pass — a promise with no test at all

The first run reported `UpdateController.cs:96` — the sentence inside the delete refusal — as
**NoCoverage**: no test executes that line. The guard *condition* on the line above is covered, because
every ordinary cancel runs through it, which is what made this easy to miss. Nothing had ever asked the
route to stop a removal.

That is not a decorative path. The popover draws a Stop control on every row including a removal, and
the frontend's `leaves the row as it was when the instance refuses to stop it` is written about exactly
this refusal — so the browser half of the contract was pinned while the instance half was not. The
guard could have been deleted outright and the suite would have stayed green.

`Asking_to_stop_a_removal_is_refused_and_says_why` now presses it: 400, and the sentence an operator
reads, which is the only thing they get — the row is unchanged afterwards and the popover says nothing
of its own about why. Verified by applying Stryker's own mutant (the message blanked to an empty
string) and rebuilding: the new test reds and nothing else does. Probe reverted, `git diff` on the
production tree empty.

#### Accepted survivor

- **`UpdateController.cs:127`** — `elapsed < TimeSpan.Zero` relaxed to `<=`. **Equivalent**: at exactly
  zero both arms answer 0. Accepted for the same reason in slices 03 and 04, and re-checked rather than
  carried over on trust.

### Frontend

| File | tested | killed | survived |
| --- | --- | --- | --- |
| `connectionHealthWording.ts` | 45 | 44 | 1 |
| `ActivitySection.tsx` | 75 | 63 | 12 |
| `ConnectionsSection.tsx` | 34 | 27 | 7 |
| `useTaskManagerPopover.ts` | 54 | 43 | 11 |
| `TaskManagerIcon.tsx` | 25 | 15 | 10 |
| `RecentProblemsSection.tsx` | 15 | 11 | 4 |
| `SectionHeading.tsx` | 4 | 1 | 3 |

#### Closed by this pass — 17 mutants, seven scenarios

The first run's 74.21 % was not a rounding problem. Six separate promises this slice makes had nothing
holding them.

| What survived | What it meant |
| --- | --- |
| `taskKey` emptied, and reduced to `undefined` | **The browser remembered the stop by number alone.** A team and a portfolio are numbered separately, so one press of Stop could have put every row sharing that number into a state nobody asked for. Nothing had ever rendered two rows carrying the same number. |
| the stopping arm of `RowProgress`, and its accessible name | **Nothing asserted the mark beside the word.** The row read "Stopping…" and the drawing beside it was never looked at, so the two could have disagreed — which is the exact failure the single `RowActivity` decision was extracted to prevent. |
| `tabIndex={-1}` → `+1` | **A positive tabindex, unnoticed.** Focus moving onto the row is covered indirectly — the Escape in `keeps the row until the instance stops reporting the work` only reaches the popover because focus left the disabled button — but nothing said the row must stay out of the tab order, and a row that joined it would sit ahead of every other control on the page. |
| the four ink choices in `HOW_EACH_STATE_IS_DRAWN` | **Only shape was asserted, never colour.** The spec that separates a ring from a tick says in its own name that it does not rely on colour; nothing else did either, so all four states could have been drawn in one ink. |
| `forgetWhatTheInstanceNoLongerReports`'s filter, and both branches of its "did anything change" test — five mutants | **The browser never forgot an ask.** The keys the reader has asked to stop are dropped when the instance stops listing the work, so a key that comes back — because the ask arrived after the refresh had been requeued — reads as running again. Nothing exercised a row leaving and returning, so a stale key could have left a row saying "Stopping…" for good. |
| `candidate.connectionId === answeredFor` → `true` | **A test verdict could have been written across every row.** The existing specs press Test connection on a list of one, or assert only the row they pressed. One press could have re-described every connection as whatever the tested one turned out to be. |
| the badge count, both halves | **The number nobody checked.** It could have been a subtraction, or could have counted every connection rather than the broken ones, and no spec read the badge's text — only its colour. |

All seven scenarios were written against the un-mutated code and pass there; the re-run is what proves
they kill, and it moved exactly the mutants predicted and no others.

#### Accepted survivors — 48, in eight groups

| Group | Count | Why |
| --- | --- | --- |
| MUI `sx` objects and the strings inside them (`ActivitySection` 153, 160; `ConnectionsSection` 74, 76, 82, 114; `RecentProblemsSection` 35, 54; `SectionHeading` 26; `TaskManagerIcon` 71, 72, 73, 88) | 26 | jsdom computes no emotion styles, so there is nothing to assert. Pinning them means asserting a copy of the source rather than a claim about behaviour. Accepted the same way in slices 02, 05 and 06. |
| Whitespace and empty strings in JSX (`ActivitySection:163` twice, `RecentProblemsSection:56`, and the `", "` separator in `connectionHealthWording:57`) | 4 | `toHaveTextContent` normalises whitespace, and the else-arm of the removal marker is already empty. The words on either side are asserted; the punctuation between them buys brittleness and no defect. |
| The `default:` arms of `activityOf`, `describeState` and `RowProgress`, and the word they answer (`ActivitySection` 68, 69, 83, 110) | 4 | **Unreachable through the port.** `GET /update/tasks` filters to queued and running work before answering, so no status that reaches this list can take these arms. Reported as NoCoverage rather than Survived, which is the honest label for it. |
| `RecentProblemsSection:51`, the React `key` blanked | 1 | Reconciliation identity, not rendered output. Accepted in slice 06 for the same reason. |
| `ActivitySection:167`, the Stop tooltip title blanked | 1 | It is the same sentence as the button's `aria-label`, which every cancel scenario queries by. Killing it means asserting a hover for a string already pinned two lines below. |
| `ActivitySection:180`, the optional chaining on `focus` removed | 1 | Equivalent in the rendered tree: the handler runs on a button inside the row it is looking for, so `closest` cannot answer null. Defence against a future rearrangement, not a gap. |
| `useTaskManagerPopover` 37 (`false`, and the block emptied) and 44 (`false`) | 3 | **Equivalent by construction.** Both are shortcuts: the empty-set early return, and handing back the same set when nothing was dropped. Removing either leaves the general path computing the same membership — only the object identity differs, and identity is a render count rather than a claim. Every mutant that *changed* the membership dies. |
| `useTaskManagerPopover` 76, 77 (the initial `useState` values), 101, 128, 156, 178 (dependency arrays), 166 and 175 (the unmount guard) | 8 | The React plumbing. An initial value is replaced by the first answer before anything renders it, a dependency array is React's memoisation rather than a Lighthouse promise, and the `gone` flag guards a subscription callback firing between unmount and unsubscribe — a window no test can open. Slices 02 and 04 accepted the same set. |

Excluding the 26 `sx` mutants — one accepted class rather than 26 findings — the rest kills 204 of 226,
**90.3 %**.

#### Not mutated, and why

`UpdateAllButton.tsx` was in the slice-06 config and is not in this one: this slice does not touch it.
`Header.test.tsx` is in the vitest include list all the same, because it is what renders the icon for
somebody who is not a System Administrator — and a spec that covers mutated lines but is missing from
that list leaves every mutant in it alive for want of a test *run*, which reads in the report exactly
like a missing test.

### Gates after the test strengthening

| Gate | Result |
| --- | --- |
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6 853 passed, **1 failed**, 10 skipped — see below |
| `pnpm test` | 370 files, 5 204 tests, all green |
| `pnpm build` | clean, Biome included |
| `pnpm biome check ./src` | 816 files checked, no fixes applied |

**The one failure is environmental, and was proved so rather than assumed.**
`ServiceProviderValidationTest.ServiceContainer_BuildsWithoutScopeViolations_WhenValidateScopesIsEnforced`
fails with an `IOException` deleting the throwaway `DiValidation_*.db` it has just created — a SQLite
pool handle still open when `Dispose` runs. Slice 07's write-up attributed this to stale copies
accumulating in `bin/`; that is not the whole story. Deleting all fourteen of them and running the class
alone still fails, and it fails identically with this pass's changes stashed. Not a regression, and
nothing this pass touched can reach it.

## 6018 — Connection health that persists and checks itself (slice 08)

Epic #5511 Task Manager, slice 08. Run 2026-09-16 against `main` @ `d27609385`. Gate is an 80 % kill
rate on each stack that has changed files. **Frontend: not run — the slice changed no frontend file.**

| stack | score | killed | survived | no coverage | timeout |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **82.8 %** | 47 | 10 | 13 | 1 |

Config: `stryker.6018.backend.json` (concurrency 1 — see *Two ways this run wasted an hour* below).

| File | score | survivors |
| --- | --- | --- |
| `ConnectionHealthCadence.cs` | **100 %** | — |
| `ConnectionHealthProber.cs` | **100 %** | — (8 uncovered, all in `ExecuteAsync`) |
| `ConnectionHealthService.cs` | **88.9 %** | 4, all guard clauses and log text |
| `ConnectionHealthVerdictRepository.cs` | **68.4 %** | 6, five of them one defensive call |

### What the first run found, which was worth more than the number

The first pass scored **80.4 %** — over the gate, and hiding two defects in the tests.

**The threshold formula was not tested at all.** Both mutations to
`ConnectionHealthCadence.HowFreshAnAnswerMustBe` survived: `Math.Max` → `Math.Min`, and the `2 *`
arithmetic. Every scenario had deliberately set verdict ages far from any boundary so that none of them
would pass or fail on arithmetic — and the price of that was that the arithmetic itself was unpinned.
With both seeded refresh intervals at 60 minutes, `min` and `max` cannot be told apart; at 60 and 360
they can, and the difference is the whole of the claim that a connection something refreshes never goes
stale. `An_answer_is_judged_against_the_longest_refresh_interval_not_the_shortest` sets the two intervals
an hour and six hours apart and asks about an answer three hours old — fresh against the longest, stale
against the shortest. That file is now 100 %.

**A test written to kill the claim's connection match did not kill it, and neither did the first fix.**
Mutating `WorkTrackingSystemConnectionId == connectionId` to `!=` makes the claim match the *other*
connection: with one stale row and one fresh row, the stale connection's claim finds the fresh row and
does not match, the fresh connection's claim finds the stale row and does, so the tracker is still asked
**exactly once** and both connections still read **Healthy**. A count assertion and a state assertion are
both satisfied by the mirror image of the correct behaviour.

The first attempt to fix that seeded the fresh connection at `TimeSpan.Zero` and asserted both ended up
observed "just now" — which also holds either way. Seeding it an hour old is what makes the moment the
discriminator: the correct code leaves it an hour old, the mutant advances it to now. Mutant killed;
82.8 %.

Two tests in one slice that looked right, passed, and proved nothing. The other was found at DISTILL —
a slice 05 cancel scenario that had been green for six slices while unable to distinguish a cancelled
refresh from one that simply finished.

### Survivors left deliberately

- **`ForgetAnyTrackedCopyOf`, five mutants (L34, L68, L76, L80).** Removing the call changes no test
  outcome, which is consistent with how it was accepted in review: it guards a stale tracked copy in a
  scope where, on today's paths, none is ever read back. It is defensive rather than covered, and a test
  that killed these would have to construct a flow the product does not have. Recorded as a known gap
  rather than papered over.
- **`ConnectionHealthProber.ExecuteAsync`, 8 uncovered.** The integration harness removes every
  `IHostedService`, so the loop cannot run under test; the single-pass method it calls is covered.
- **Guard clauses and log message text** — `ArgumentNullException.ThrowIfNull`, `LogWarning` strings, and
  the two placeholder `string.Empty` values a claim inserts and the recording immediately overwrites.

### Two ways this run wasted an hour

- **Stryker holds the test assembly.** A `dotnet build` of the test project during a mutation run fails
  to copy `Lighthouse.Backend.Tests.dll` (`locked by .NET Host`), and `dotnet test --no-build` then runs
  the *previous* binaries and reports a pass that means nothing. This nearly shipped an unverified
  rebase. Do not run mutation concurrently with anything that rebuilds the test project.
- **Concurrency 4 gets the run killed** on this machine (OS low-memory). Concurrency 1 completes in
  about 25 minutes.
- **Invocation**: the shell allowlist refuses `run-backend-x64.ps1` by path and mis-splits a multi-line
  PowerShell equivalent. `pwsh -NoProfile -File <script> -Config <json>` is the form that works.

## 6010 — Test connection on Azure DevOps, and one connection ending a whole health pass (reopened)

Bug #6010, reopened after the slice-08 build was run against the maintainer's own instance. Run
2026-09-16 against `main` @ `6a49e1972` plus the fix's uncommitted changes.

| target | score | tested | killed | survived | wall clock |
| --- | --- | --- | --- | --- | --- |
| `ConnectionHealthService.cs` (whole file) | **90.48 %** | 41 | 38 | 3 | 18 m |
| `AzureDevOpsWorkTrackingConnector.cs` — **the changed methods** | **83.33 %** | 12 | 10 | 2 | 6 m |
| `AzureDevOpsWorkTrackingConnector.cs` — whole file | 34.16 % | 363 | 121 | 101 | 6 m |

Configs: `stryker.6010.health.backend.json`, `stryker.6010.ado.backend.json`.

**Read the whole-file Azure DevOps number as a baseline, not as a verdict on this change.** 138 of its
mutants have no coverage at all: the connector is ~1000 lines and most of it is reachable only through
the `AdoIntegration` tests, which the filter excludes on purpose because they call a real organisation
over the network. The row that answers "is this change tested" is the changed-methods row, counted over
lines 686-740 (`GetMissingAdditionalFields`, `TheFieldsTheOrganisationDefines`,
`GetCustomFieldReferences`). Whole-file moved 33.33 % → 34.16 % across this work, which is the same
three kills seen from the other end.

### Closed by this pass

Two of the three survivors that mattered were in code written the same day, which is the argument for
running the gate before the push rather than after.

- **`string.IsNullOrEmpty(reference)` → `reference != null` survived** on the new validate path.
  `TheReferenceOfTheFieldNamed` answers `string.Empty` and never null, so the mutant reports *every*
  configured field as missing — and the only test used a field that genuinely was missing, which passes
  either way. A filter that answers "everything" is only visible against a field that is really there:
  `ValidateConnection_ReportsNothingMissingWhenTheOrganisationHasTheFieldTheConfigurationAsksFor`.
- **The same shape one layer down, on the refresh path.** `GetWorkItemsForTeam_...DoesNotHave` asserted
  the warning is present for a missing field; nothing asserted it is *absent* for a field that resolved.
  Mutated, every field on every cycle gets "nothing will be read for it", which is how an operator learns
  to skim the line that was telling them something. Added `...SaysNothingAboutAnAdditionalFieldTheOrganisationDoesHave`.
- **The new cancellation guard had no test at all** — the negate survived and the `return` behind it was
  NoCoverage. `A_pass_entered_while_the_host_is_stopping_says_nothing_about_any_connection` pins both:
  a pass entered during shutdown asks nothing and reports nothing.
- **The failed-check warning was unasserted.** A connection that could not be checked reads `Unknown`,
  which is also what a connection nobody asked reads — so that log line is the only place the difference
  exists. The failure scenario now asserts the operator is told which connection.

### Survivors judged, not killed

- **`LogWarning` at the field-list refusal** (statement removal, and message blanking) — 2 of the 2
  survivors in the changed region. Unlike its sibling above, this one is *not* the only trace: the
  exception thrown on the next line carries `WhatAdoSaid(refusal)` into the verdict, and two existing
  tests assert that verdict's code, message and field name. Asserting the log line as well would be a
  test written for the score.
- **`ArgumentNullException.ThrowIfNull(connection)` ×2** — pre-existing guards on the two record methods.
  A test for these is a language-guarantee test.
- **The `return` in the loop's own cancellation check.** Equivalent as the code now stands: the catch
  below it handles a cancelled claim identically, so removing the guard changes nothing observable. It
  stays because it is the safety net for a provider that does not honour the token, which is a property
  no test here can exhibit.
