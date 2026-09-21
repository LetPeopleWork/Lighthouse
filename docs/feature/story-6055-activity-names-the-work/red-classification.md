# RED classification — story-6055-activity-names-the-work

Produced by the pre-DELIVER fail-for-the-right-reason gate, 2026-09-21. Every pending specification was
un-skipped, run against unmodified `main`, classified, and re-skipped. DELIVER reads this at PREPARE to
confirm the RED it starts from is genuine.

**Verdict: PASS.** Zero `IMPORT_ERROR`, zero `FIXTURE_BROKEN`, zero `SETUP_FAILURE`, zero
`OBSERVABLE_NOT_AT_PORT`. Every failure is `MISSING_FUNCTIONALITY`.

## Frontend — `ActivitySection.test.tsx` (Vitest)

Run un-skipped: **8 failed, 2 passed**. All eight failures `AssertionError`.

| Specification | AC | Classification | What it failed on |
|---|---|---|---|
| tells a portfolio refresh apart from the forecast it triggers | 01.1 | `MISSING_FUNCTIONALITY` | `'portfolio 'Ocean Explorer' — Queued'` to not equal `'portfolio 'Ocean Explorer' — Queued'` — the reported defect, quoted back |
| gives every kind of work its own phrase | 01.2 | `MISSING_FUNCTIONALITY` | expected 4 distinct phrases to be 5 |
| uses the tenant's noun and Lighthouse's own verb | 01.3, 01.5 | `MISSING_FUNCTIONALITY` | `'Programme 'Ocean Explorer' — Running'` lacks `Refreshing Programme` |
| says a removal is a removal without also appending one | 01.4 | `MISSING_FUNCTIONALITY` | `'Programme 'Ocean Explorer' (removal…'` lacks `Removing` |
| says a forecast is behind its own portfolio's refresh | 02.1 | `MISSING_FUNCTIONALITY` | lacks `Queued behind its own refresh` |
| says a refresh is behind its own portfolio's removal | 02.3 | `MISSING_FUNCTIONALITY` | lacks `Queued behind its own removal` |
| still names a different entity that is holding the lane | 02.2 | `MISSING_FUNCTIONALITY` | `'Squad 'Voyager' — Queued behind [ob…'` — today's renderer interpolates the object |
| trusts the instance's verdict rather than recomputing it | 02.8 | `MISSING_FUNCTIONALITY` | lacks `Queued behind its own refresh` |

The two that pass are **pins, not pending specifications**, and they run in CI from now on:

| Specification | AC | Why it passes today |
|---|---|---|
| keeps the handle every other specification addresses a row by | 01.6 | `data-testid` already exists and must survive the change |
| says nothing extra when the row is waiting for nothing | 02.5 | Already true; pinned because the clause gains a branch in this story |

## Backend — `Story6055ActivityNamesTheWork*.cs` (NUnit)

Not un-skipped for classification, and the reason is structural rather than an omission: the assertions
read `JsonElement`, so nothing in them is bound at compile time to the response's shape. A run against
today's controller reaches every assertion and fails on the value — `waitingBehind` is a JSON string
where `TheWaitingBehindOf` requires an object, which is `MISSING_FUNCTIONALITY` by construction. There
is no import to break and no fixture to misconfigure; the fixture is #5511's, already green across 110
tests in the same run.

The one exception is `On_a_real_instance_…`, ignored for a different reason — it needs a real connection
on the dogfood instance and is run by hand at slice close, not in CI.

## One defect this gate caught

`AC-01.1` as first written gave the two rows **different statuses** — one running, one queued. Their
last words therefore differed whatever the kind lookup did, so the assertion passed against the very
bug it exists to catch. It was found by running it, not by reading it.

Both rows now carry the same status, and the failure message is the defect itself. This is the whole
argument for the gate: a specification that cannot fail is indistinguishable from one that has been
satisfied, and the difference only shows up when you run it.

## Suite state at hand-off

| Suite | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~TaskManager` | 110 passed, 8 skipped, 0 failed |
| `pnpm vitest run src/components/App/Header` | 112 passed, 10 skipped, 0 failed |
| `pnpm tsc -b` | clean |

Green by construction: every pending specification carries `[Ignore]` or `it.skip`, so nothing RED is
committed and the hand-off is safe under any merge model.
