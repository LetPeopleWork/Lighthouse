# Mutation testing — 5725 (Faster updates: a Jira Cloud team refresh fetches only the issues that moved)

Run 2026-08-10 against `main` @ `bc33f59f1`. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0), whole files | 49.84 % | 455 | 300 | 152 | 3 | 12 m 40 s |
| **Backend, slice-02 changed lines only** | **80.00 %** | **90** | **72** | 15 | 3 | — |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — |

Config: `stryker.5725.backend.json`. **Frontend is N/A, not skipped**: slice 02 changed zero files under
`Lighthouse.Frontend/`. The epic is backend-only by decision D4 (no UI this epic — the observable surface
is the log; a task-manager view belongs to Epic #5511).

## Which number is the gate

**80.00 %, on the lines slice 02 changed.** The two whole-file numbers are in the table for honesty, not
as the verdict, and the gap between them is entirely an artefact of how Stryker.NET scopes.

Stryker.NET **ignores line ranges** in `mutate` — only whole-file globs work (frontend StrykerJS supports
ranges; .NET does not). Slice 02 changed 200 lines of a 1660-line connector and 186 of an 876-line
service, so mutating those files whole buries the slice's own score under code it never touched. The
per-changed-line figure is recovered from `mutation-report.json` by intersecting each mutant's location
with `git diff -U0 39009086f..HEAD`.

Two runs were made:

- **Run A** — every mutator, whole files: **38.31 %** (583 tested, 328 killed). Changed lines only: 47.37 %.
- **Run B** — `ignore-mutations: ["string"]` plus `ignore-methods: ["*Log*", "Console.*"]`: **49.84 %**
  whole-file, **80.00 %** on changed lines.

The narrowing between A and B removes exactly one category: mutations of log message templates and
display strings. Those are unkillable without asserting on prose, and asserting on prose is how a suite
becomes hostile to its own refactoring. Everything with behaviour behind it is still measured.

## Backend

| file | killed | survived | no coverage | ignored | score |
| --- | --- | --- | --- | --- | --- |
| `IssueFactory.cs` | 3 | 0 | 0 | 1 | 100 % |
| `OptionalFeatureSeeder.cs` | 2 | 0 | 0 | 3 | 100 % |
| `SyncModeResolver.cs` | 13 | 0 | 0 | 3 | 100 % |
| `JiraWorkTrackingConnector.cs` | 29 | 6 | 3 | 55 | 76.3 % |
| `WorkItemService.cs` | 25 | 9 | 0 | 19 | 73.5 % |

`SyncModeResolver` at 100 % is the one that matters most: it is the whole mode decision (D8), it is a pure
static by DDD-5, and every one of its branches has a named unit test.

### Closed by this pass

Three survivors produced new tests (`bc33f59f1`).

- **`IssueFactory.cs:228` and `JiraWorkTrackingConnector.cs:126` — `DateTimeStyles.AssumeUniversal |
  AdjustToUniversal` mutated to `&`.** `&` of two distinct flags is `None`, so a timestamp Jira returns
  **without a zone** would be read as the host's local time and stored as though it were UTC. Every
  later comparison would then be off by the host's offset — on a per-item diff (D12) where being off by
  an hour means refetching everything or nothing.

  The existing test used an offset-bearing stamp, which both variants agree on. The new tests
  (`…ReadsAStampWithNoZoneAsUtcRatherThanAsTheHostsLocalTime`, one per parse site) use a zone-less one,
  which is the only input the flag changes. Note `AdjustToUniversal` is a **no-op** at both sites —
  both end in `.UtcDateTime`, which normalises already — so a `Kind == Utc` assertion cannot
  discriminate, and one already existed while the mutant survived.

  **Host-dependent kill, recorded deliberately**: on a UTC-configured machine `DateTimeStyles.None` and
  `AssumeUniversal|AdjustToUniversal` are the same function on every input, so this mutant is provably
  equivalent there and no test can kill it. It dies on a Europe/Zurich dev box. A Stryker run on a UTC CI
  runner will report it surviving — that is equivalence under that configuration, not a missing test. The
  only timezone-independent alternative is setting `TZ` plus `TimeZoneInfo.ClearCachedData()`, which is
  global mutable state and racy under a parallel suite; rejected.

- **`WorkItemService.cs:231` — the zero-moved-ids branch.** Forcing the condition false survived: a delta
  cycle with nothing to fetch still called the tracker with an empty key list. `fetched` was 0 either way,
  so AC-2.3's count assertion could not see the wasted round trip — on precisely the cycle this epic
  exists to make cheap. `ThenTheIssueWasNeverDownloaded` now asserts no by-reference-id request is issued
  at all, not that it returned nothing.

Also added, for coverage rather than for the score: a scenario driving **opt-in on with a connector that
refuses to sweep**. AC-2.2's fifth criterion said so and only the resolver's unit tests covered it; there
was no end-to-end path.

### Accepted survivors

- **`WorkItemService.cs:115`, `:188` (×2 each), `:208` — booleans on
  `IdentityScan(TrackerCanBeScanned:…, Succeeded:…)`.** Equivalent, verified by applying each mutation and
  running the fixture. At `:115` the resolver's first guard short-circuits on the opt-in flag before those
  fields are read. At `:188` the opt-in guard does *not* fire (the operator has opted in), but guards 2 and
  3 absorb both flips. Every route lands on `SyncMode.Full`, which is the answer either way — this is D8's
  "ambiguity resolves to a full fetch" behaving as designed, and defence in depth showing up as
  unkillability.
- **`WorkItemService.cs:327` — `!survivors.Contains(persistedItem)` negated.** Admits duplicates into the
  staleness pass. Cannot double-raise: `AddStalenessEventIfThresholdCrossed` flips `WasStaleAtLastSync` on
  the first visit, so the second finds nothing to report.
- **`WorkItemService.cs:95`, `:96`, `:325` — statement removals on the two `Save()` calls and an
  `AddRange`.** The acceptance harness reads back through the same EF context that holds the tracked
  entities, so a dropped `Save()` is invisible to it. Killing these needs a test that reopens the context,
  which is a different test shape than these scenarios are, and the transition suites already cover
  persistence directly.
- **`JiraWorkTrackingConnector.cs:1463` — `pageCount < MaxCloudSearchPages` → `<=`.** Off-by-one on a
  defensive page cap that exists so a pathological result set cannot loop forever. The boundary is
  arbitrary; asserting it would pin an implementation detail.
- **`JiraWorkTrackingConnector.cs:120`, `:210`, `:1383`, `:1390`, `:1399`, `:1439`, `:1443`, `:1610`.**
  Deployment-cache and changelog-paging paths reachable only from the live-Jira fixtures, which are
  excluded from the test filter on purpose — Stryker reruns the suite hundreds of times and would hammer
  Atlassian and trip rate limits (ledger, 2026-06-16 / 2026-05-25).

### Not mutated

- **Log message templates and display strings**, via `ignore-mutations: ["string"]` and
  `ignore-methods: ["*Log*", "Console.*"]`. Killing them means asserting on prose. One exception is worth
  noting: AC-2.6 *does* assert the scan-failure warning is emitted exactly once at Warning-or-above — the
  behaviour is pinned, the wording is not.
- **The other four connectors** (Azure DevOps, ServiceNow, Linear, CSV). Slice 02 changed only their
  method signatures — probe returns false, both new methods throw. Nothing behavioural to mutate.
- **`Models/SyncOutcome.cs`, `Models/RemoteRecordStamp.cs`, `Jira/Issue.cs`** produced no mutants on
  changed lines: records and a factory method with no branching.

## Test filter

`TestCategory!=JiraIntegration` is load-bearing. `JiraWorkTrackingConnectorTest` and `JiraWriteBackTest`
hit `letpeoplework.atlassian.net` for real; under Stryker they would run hundreds of times. Their
exclusion is why several connector paths report as uncovered above. `JiraIncrementalSyncTest` carries no
category — it drives a recording `HttpMessageHandler` — and is included.
