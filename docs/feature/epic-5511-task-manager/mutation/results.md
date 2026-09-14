# Mutation testing — 5788 (A scheduled refresh that failed is reported to the browser as completed)

Epic #5511 Task Manager, slice 01. Run 2026-09-14 against `main` @ `90b929b7d` plus the slice's own
uncommitted changes. Gate is an 80 % kill rate on each stack that has changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1) | **94.74 %** | 19 | 18 | 1 | 0 | 44 s |
| Backend (Stryker.NET) | **not run** — see below | — | — | — | — | — |

Configs: `stryker.5788.frontend.json`, `stryker.5788.backend.json`, `vitest.stryker.mutation.ts`.

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

## Backend — not run, and why

`UpdateServiceBase.cs` is the one changed backend file and it has a config ready
(`stryker.5788.backend.json`, scoped to that file, filtered to
`BackgroundServices.Update` + `TaskManager` tests). **It could not be executed on this machine.**

Stryker.NET's initial test discovery reports `Number of tests found: 0` and aborts with *"did not report
any test. This may be because the test adapter package, NUnit3TestAdapter, failed to deploy or run."*
Confirmed to be environmental rather than a config error:

- `dotnet test` discovers and runs all 6693 tests in the same assembly without complaint.
- `NUnit3.TestAdapter.dll` is present in `bin/Debug/net10.0/` alongside the test assembly.
- Both **5.0.0** and **4.16.0** (the version the ServiceNow evidence was produced with) fail identically.
- The host is `win-arm64`, which is the same footing as the other ARM64 tooling gaps in this checkout.

The change itself is one removed `catch`, and what it does is pinned by eight acceptance scenarios plus
`UpdateQueueServiceTests.EnqueueUpdate_TheUpdateFails_StillLetsGoOfTheWorkHeldBehindIt`. That is not a
substitute for the gate. **The backend gate is outstanding** — run the committed config on an x64
machine or in CI before this slice is called finished.
