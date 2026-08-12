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

---

# Mutation testing — 5726 (Faster updates: a Jira Cloud portfolio refresh fetches only the Features that moved)

Run 2026-08-11 against `main` @ `1c5d1f9c1`. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0), whole files | 54.63 % | 426 | — | — | — | 30 m 26 s |
| **Backend, slice-03 changed lines** | **78.69 %** | **61** | **48** | 13 | 0 | — |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — |

Config: `stryker.5726.backend.json`. **Frontend is N/A, not skipped**: slice 03 changed zero files under
`Lighthouse.Frontend/` — the epic is backend-only by decision D4.

## The gate as written is not met, and this is what stands behind the number

**78.69 % on the changed lines — 1.64 points, one mutant, short of 80 %.** Recorded rather than closed,
because every one of the 13 survivors was verified equivalent by applying the mutation and running the
fixture. On the mutants that are not equivalent the pass is **48 of 48**.

The arithmetic is worth stating, because it is why an honest triage cannot move this number: killing a
survivor raises the numerator, but the six mutants that were `NoCoverage` in the first run moved to
`Survived` in the second — both count as not-killed. Closing that coverage gap, which was worth doing on
its own merits, was worth exactly zero points.

Two runs were made:

- **Run A** — before triage: **53.38 %** whole-file, **70.49 %** on changed lines (61 tested, 43 killed,
  12 survived, 6 no-coverage).
- **Run B** — after the triage commit `1c5d1f9c1` added six scenarios and strengthened one connector
  assertion: **54.63 %** whole-file, **78.69 %** on changed lines.

The whole-file figures are in the table for honesty, not as the verdict. Stryker.NET **ignores line
ranges** in `mutate` (frontend StrykerJS supports them; .NET does not), so mutating `WorkItemService.cs`
whole buries 279 changed lines under 1117. The per-changed-line figure is recovered from
`mutation-report.json` by intersecting each mutant's location with `git diff -U0 0c4e129c6..HEAD`.

## Backend

| file | tested | killed | survived | score |
| --- | --- | --- | --- | --- |
| `JiraWorkTrackingConnector.cs` | 11 | 11 | 0 | **100 %** |
| `WorkItemService.cs` | 50 | 37 | 13 | 74.0 % |

### Closed by this pass

Five survivors produced tests (`1c5d1f9c1`). Each was proven by hand-applying the mutation, watching the
new test go red, reverting, and watching it go green.

- **`:759` — deleting `SweepDepartedFeatureSpells`.** A Feature blocked when it left the query kept
  accruing blocked time forever. This survivor is inherited: slice 01 recorded it as a known gap on the
  portfolio path and slice 02 carried it forward. Now killed by
  `A_feature_that_was_blocked_when_it_left_the_query_stops_accruing_blocked_time`.
- **`:1015` — forcing the parent scan's opt-in branch true.** A cycle nobody volunteered for swept the
  parent keys anyway. The Feature half had this assertion (AC-3.4 / A1); the parent half did not.
- **`:1088` and `:1100` — the parent download rule.** `keysToDownload.Count == 0` forced true, and
  `swept == null || HasMoved(…)` mutated to `&&`. Both mean parents are never downloaded under delta,
  and both are killed by the two scenarios that exercise the inversion: a parent that moved, and a
  parent the sweep did not answer for. This is the parent half's whole reason to differ from the child
  half — that it went untested until mutation asked is the most useful thing this run produced.
- **`JiraWorkTrackingConnector.cs:295` — removing the empty-key-set guard.** The assertion counted
  `SearchRequests`, so it never saw that `CreateFeaturesFromIssues` still GETs `rest/api/latest/field`
  through `GetCustomFieldReferences`. It now counts every request, which is what the test's name already
  claimed.

### Accepted survivors

All 13 verified by applying the mutation and running the fixture green.

- **`:735` — deleting the claim sweep `featuresTheQueryStillReturns.ForEach(f => AddProjectToFeature(f, portfolio))`.**
  Equivalent, and the statement appears to be redundant: `HasMany(f => f.Portfolios).WithMany(p => p.Features)`
  (`LighthouseAppContext.cs:224-225`) is one relationship with both navigations as inverses, and
  `portfolio.UpdateFeatures` is handed the identical list — `FeatureOrdering.Order` sorts, it never
  filters — so both ends write the same join row. Removed by hand: 103 tests green. **Deleting it is a
  production change and was deliberately not made during a mutation pass; it is flagged for a decision.**
  The line also documents intent at the exact place the delete hazard lives, which is an argument for
  keeping it whatever the mutation score says.
- **`:794` ×2, `:1017` ×2 — the opt-in-off `IdentityScan(TrackerCanBeScanned:…, Succeeded:…)`.** The
  resolver's first guard returns `Full` on the opt-in flag before either field is read. Identical to the
  team path's accepted `:115` in the 5725 run.
- **`:813` ×2, `:1053` ×2 — the scan-refused returns.** Guards 2 and 3 both answer `Full`, so flipping
  either flag only changes which guard fires.
- **`:833`, `:1072` ×2 — the scan-failed returns.** Same absorption. `:1072`'s `Succeeded: false → true`
  is the interesting one and is still equivalent: it routes the parent path to `Delta` with an EMPTY
  stamp list, so `TheSweepDidNotVouchForThisParent` is true for every key and `GetParentFeaturesDetails`
  is called with the identical list. Delta-with-no-stamps degenerates exactly onto Full — which is D8
  behaving as designed, and defence in depth showing up as unkillability.
- **`:1026` — forcing the parent `mode == SyncMode.Delta` branch true.** The branches differ only when the
  sweep vouches for a stored parent while the mode is `Full`, which is reachable only through the
  resolver's "nothing stored" or "a stored record without a stamp" guards. A test that kills it would
  assert that the cycle deliberately re-downloads a parent whose stamp says it did not move — pinning
  D8's "ambiguity resolves to a full fetch" as an observable cost rather than as a safety rule. The
  mutant is arguably the better program: same stored state, one fewer round trip. Killing it would fight
  the next person who tries to make this cheaper, so it is accepted rather than tested.

### Not mutated

- **Log message templates and display strings**, via `ignore-mutations: ["string"]` and
  `ignore-methods: ["*Log*", "Console.*"]`. One exception is pinned behaviourally rather than by wording:
  a failed sweep must emit exactly one Warning-or-above line.
- **The four non-Jira connectors** and `IWorkTrackingConnector`. Slice 03 added three members that return
  false or throw on all of them — no behaviour to mutate.
- **`SyncModeResolver.cs`**: the only slice-03 change was widening a parameter type to `WorkItemBase`,
  which produces no mutants on changed lines. Its branches were covered at 100 % in the 5725 run and
  their tests still run.

## Test filter

`TestCategory!=JiraIntegration` is load-bearing: `JiraWorkTrackingConnectorTest` and `JiraWriteBackTest`
hit `letpeoplework.atlassian.net` for real and Stryker reruns the suite hundreds of times.
`JiraIncrementalSyncTest` carries no category — it drives a recording `HttpMessageHandler` — and is
included, which is why the connector scores 100 % here.

# Mutation testing — 5728 (Faster updates: a setting costs a refetch only when it changes what is fetched)

Two runs 2026-08-12. Gate is 80 % kill rate on the lines the slice changed.

| run | whole-file | changed lines | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A — before closing survivors | 69.00 % | 73.91 % (17/23) | 425 | 312 | 109 | 33 | 4 | 36 m 20 s |
| **B — after `6e75e3c49`** | **69.43 %** | **82.61 % (19/23)** | 425 | 318 | 107 | 33 | 0 | 33 m 18 s |

Config: `stryker.5728.backend.json`. Run B is the verdict: **82.61 % — gate met.** Frontend is **N/A, not
skipped**: slice 05 changes zero files under `Lighthouse.Frontend/`, by decision D4 (no UI this epic).

Run B was taken after a rebase onto eight dependabot commits, so it measures the tree that actually
shipped as `5f2ac86c5`, including the `StackExchange.Redis 3.1.3 → 3.1.13` bump.

## Which number is the gate, and why 23 is a thin one

**82.61 %, on the 23 mutants that land on lines slice 05 changed.** Stryker.NET ignores line ranges in
`mutate` — only whole-file globs work — so the whole-file figure buries the slice's own score under
eleven files it barely touched. The per-changed-line figure is recovered from `mutation-report.json` by
intersecting each mutant's location with `git diff -U0 a27b4acf2^..HEAD`.

Worth stating plainly: **23 mutants is a thin basis for a percentage.** Slice 02 had 90, slice 03 had 61.
Two mutants move this number by nine points. The reason is that most of what slice 05 adds is a static
registry of `nameof(...)` entries and string rendering, and `ignore-mutations: ["string"]` removes the
latter wholesale — `FetchFingerprint.cs` is 137 lines and yields six mutants. The score is honest for
what it measures; it is not a dense measurement.

## Backend

| file | killed | survived | no coverage | score | note |
| --- | --- | --- | --- | --- | --- |
| `SyncModeResolver.cs` | 13 | 0 | 0 | 100 % | every branch of the mode decision has a named unit test |
| `WorkItemExtensions.cs` | 2 | 0 | 0 | 100 % | the `GetValueOrDefault` fix |
| `UpdateServiceBase.cs` | 15 | 2 | 0 | 88.2 % | was 82.4 % in run A |
| `FetchFingerprint.cs` | 5 | 1 | 0 | 83.3 % | was 66.7 % in run A |
| `WorkItemService.cs` | 203 | 60 | 0 | 77.2 % | 263 mutants over a 1144-line file; the slice touched ~140 lines of it |
| `PortfoliosController.cs` | 16 | 6 | 0 | 72.7 % | |
| `TeamExtensions.cs` | 20 | 18 | 0 | 52.6 % | mostly pre-existing settings-sync code |
| `PortfolioController.cs` | 18 | 18 | 0 | 50.0 % | |
| `PortfolioExtensions.cs` | 14 | 16 | 0 | 46.7 % | |
| `WorkTrackingSystemOptionsOwner.cs` | 11 | 17 | 0 | 39.3 % | |
| `WorkTrackingSystemOptionsOwnerExtensions.cs` | 1 | 1 | 1 | 33.3 % | 3 mutants total; see the triage below |

The low whole-file scores on the last five are pre-existing surface the slice passed through, not code it
wrote. None of their survivors sit on a changed line.

### Closed by this pass

Two survivors produced tests (`6e75e3c49`). Both were real gaps, and both were verified by **applying the
mutant to production code by hand**, running the suite, watching exactly the intended test go red, and
reverting — not by trusting the report.

- **`UpdateServiceBase.cs:65` — the reason ternary forced false.** Every summary line would carry a bare
  ` | reason=`. The upgrade scenario's `ThenTheOperatorIsNotToldConfigurationIsWhy` only forbade the word
  `configuration-changed`, so a blank field passed it — and an empty `reason=` on every line is precisely
  the log noise slice 01 existed to remove. The assertion now forbids the **field**, not the word.

- **`FetchFingerprint.cs:120` — the null guard on the connection's field definitions.** Every owner in the
  fixture had a loaded connection, so the `Absent` branch was never taken. A query that does not include
  the navigation property hands back null, and throwing there takes the whole update cycle down instead of
  costing one full fetch. `For_AnOwnerWhoseConnectionWasNotLoaded_StillProducesAFingerprintAndADifferentOne`
  covers it.

### Survivors on changed lines, all four equivalent

Recorded rather than chased. None is a missing test.

- **`WorkTrackingSystemOptionsOwnerExtensions.cs:16` — `Any()` → `All()`.**
  `PropertiesThatAlsoCostAFreshStart` holds exactly one element, and `Any` and `All` are the same function
  on a one-element sequence. It stops being equivalent the day a second property is registered — which is
  the drift the guard test exists to catch, so the guard is the right instrument here, not a unit test
  written against a list of one.

- **`WorkTrackingSystemOptionsOwnerExtensions.cs:23` — the `_ => true` fallback arm, no coverage.**
  Unreachable by construction: the switch is driven by `PropertiesThatAlsoCostAFreshStart`, every member of
  which has an explicit arm. Reaching the default requires registering a property with no arm, which
  `FetchShapingPropertyGuardTest` forbids. The arm is the safe default for exactly that mistake and stays.

- **`FetchFingerprint.cs:132` — `OrderBy()` → `OrderByDescending()`.** The digest needs *a* total order, not
  a particular one. Descending is as stable across restarts as ascending, so no input distinguishes them.

- **`WorkItemService.cs:72` — `await teamRepository.Save()` removed.** The team's fingerprint is still
  persisted, because a sibling repository's `SaveChanges` on the same tracked `DbContext` flushes it. The
  call is kept deliberately: without it, persistence of a sync-owned field would depend on an unrelated
  repository happening to save in the same scope. Equivalent today, load-bearing as a guarantee.

## Test filter

Fifteen `FullyQualifiedName` fragments covering the eleven mutated files, minus every live-IO category —
`JiraIntegration`, `AdoIntegration`, `LinearIntegration`, `ServiceNowIntegration`, `GithubIntegration`,
`requires-docker`, `real-io`. Without those exclusions Stryker would make a network round trip per mutant.

## Two false-green generators found during this run

Both are now in `docs/ci-learnings.md` under `## Tests`, because both cost a cycle here:

1. **`dotnet test --no-build` after a failed build runs the previous binary and prints `Passed!`.** It
   means "do not build", not "require a build". Chaining with `&&` does not save you when the build's exit
   code is masked by a pipe (`dotnet build | tail` exits with `tail`'s status). Assert `0 Error(s)` and
   `0 Warning(s)` from captured output before testing.

2. **The mutation text Stryker reports does not compile if you apply it literally.** Stryker emits
   `MutantControl.IsActive(n)` switches that keep the original expression in the sibling branch, so
   nullable-flow analysis stays satisfied; a human deleting that branch trips CS8604 — and the other
   obvious probe shapes trip S1172, S1125, S3981, CS0162 or S1144 under `TreatWarningsAsErrors`.
   **Inversions and value swaps are the only safe hand-probe shape here.**
