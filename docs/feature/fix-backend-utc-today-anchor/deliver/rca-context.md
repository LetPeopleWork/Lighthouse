# Bug #5567 — Backend anchors "today" at UTC

**Feature id:** `fix-backend-utc-today-anchor`
**ADO:** Bug #5567 (Active) — <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5567>
**RCA performed at:** HEAD `efd8d0ff7`, 2026-07-27
**Origin:** surfaced while fixing Bug #5566 (frontend date-range URL round-trip, commit `b956a8857`)

---

## 1. Root cause

Five causal branches, one core defect.

**A — core.** The codebase conflated "store instants in UTC" with "compute calendar days
in UTC". It has a first-class, enforced, tested abstraction for the first — two
`UtcDateTimeConverter` classes (`Data/Converters/UtcDateTimeConverter.cs`, applied as a
global EF convention at `Data/LighthouseAppContext.cs:91-92`, and
`API/JsonConverters/UtcDateTimeConverter.cs`, registered at `Program.cs:273`), plus seven
tests in `Lighthouse.Backend.Tests/LighthouseAppContextUtcTest.cs` that assert nothing but
`Kind == Utc`. It has **no** abstraction for the second: a repo-wide search for
`TimeZoneInfo` across `Lighthouse.Backend` returns zero production hits and `appsettings.json`
has no timezone section. With no named seam, every author reached for `DateTime.UtcNow.Date`.

An instant has no timezone; a calendar day is *defined* by one. Only the first was modelled.

**B — a live bug today, independent of team timezone.** `API/ForecastController.cs:60`
uses `DateTime.Today` (server *local*) while `:85`, `:86`, `:94`, `:133` use
`DateTime.UtcNow.Date` — same file, same request lifetime. The two agree only when the
process runs in UTC. The container does (the `Dockerfile` sets no `TZ`, and
`mcr.microsoft.com/dotnet/aspnet:10.0` defaults to UTC), but the **standalone distribution
inherits the host zone** (`Standalone/StandaloneInitializer.cs`). Of the 49 production
anchor sites, 12 are `DateTime.Today` and 37 are `DateTime.UtcNow.Date`.

**C — "recorded day" was never modelled.** It is re-derived from the ambient clock at eight
snapshot/bucketing sites, giving it eight independent definitions and no single place to
change. Four of these were missing from the ADO report:
`BlockedCountSnapshotRecordingHandler.cs:79`, `ProcessBehaviorRecordingHandler.cs:132,187`,
and `PercentilesOverTimeRecordingHandler.cs:90`.

**D — why this shipped with a green suite.** The tests recompute the production expression.
`Lighthouse.Backend.Tests/API/DTO/FeatureDtoTest.cs:12` is literally
`private static DateTime Today => DateTime.UtcNow.Date;`, and the same line appears in
`BlackoutForecastShiftTestBase.cs:34` and `RecurringBlackoutRulesTestBase.cs:39`. A test that
recomputes the production oracle is a tautology — it passes for every possible value of
"today", including a wrong one. Separately, the existing `TimeProvider` seam
(`Program.cs:990`) reaches only seven OAuth/AppSetting classes, and the four highest-value
anchors live in EF-materialised entities (`Models/Team.cs`, `Models/Feature.cs`,
`Models/Delivery.cs`, `Models/WorkItemBase.cs`) that receive no constructor injection.

**E — the FE/BE contract now disagrees.** `b956a8857` made the client half of the date
contract consistently local (`Lighthouse.Frontend/src/utils/date/localDate.ts`,
`src/services/Api/MetricsService.ts:44`). The backend still anchors UTC. That fix made the
asymmetry sharper, not milder.

## 2. Finding F — item timestamps must move too

**Not in the ADO report, not in the original RCA. Discovered during review; user has ruled
it in scope.**

Throughput, aging and portfolio metrics bucket work items by the **UTC calendar day of a
stored instant**:

- `Services/Implementation/TeamMetricsService.cs:803` — `i.ClosedDate.Value.Date >= startDate.Date && <= endDate.Date`
- `Services/Implementation/PortfolioMetricsService.cs:589-590`, `:785-786` — same shape
- `Services/Implementation/BaseMetricsService.cs:907` — `item.ClosedDate.Value.Date > day.Date`
- `Models/WorkItemBase.cs:120` — `GetDateDifference` reduces *both* ends with `.Date`

Moving only the "today" anchor makes the window bounds instance-zone days while item
bucketing stays UTC days. An item closed at 00:30 Zurich (= 22:30Z the previous day) would
still count on the previous day. The off-by-one survives the fix — it just relocates.

**Mitigating fact:** all three service-layer filters run **client-side**. Each materialises
with `.ToList()` first (`TeamMetricsService.cs:801`, `PortfolioMetricsService.cs:583`) and
filters in memory. There is therefore **no EF translation risk** for this conversion.

## 3. Decisions (user-confirmed 2026-07-27)

| # | Question | Decision |
|---|---|---|
| F | Convert stored instants to instance zone before `.Date` bucketing? | **Yes — convert both ends.** Add `ToInstanceDay(DateTime utcInstant)` to the clock and use it at every site that reduces a stored instant to a calendar day. |
| 1 | License expiry (`Licensing/LicenseService.cs:55`) | **Instance zone.** A licensee keeps premium through their own last day. No grace day. |
| 2 | Delivery date-in-future validation (`API/DeliveriesController.cs:81,139`, `Models/Delivery.cs:16`) | **Instance-zone day comparison** — `DateOnly.FromDateTime(request.Date) <= clock.Today` → reject. Also fixes the pre-existing instant-vs-day bug where today's date was rejected all day. Behaviour changes for UTC users too → release-note line. |
| 3 | `WorkItemAge` (`Models/WorkItemBase.cs:78,80`) | **Move both ends, keep the `+1` inclusive.** Age is already calendar-day based (`GetDateDifference` at `:120` applies `.Date` to both ends), so this is a zone shift only, no arithmetic change. Same treatment for the CycleTime path at `:60` and `GetAgeOnDay` at `:115`. |
| 4 | Tracker history cutoffs (`WorkItems/WorkItemService.cs:442,445`; ADO connector `:993`; Jira connector `:1251`) | **Leave UTC**, add a comment at each site stating why, so the source guard's allowlist has a stated reason rather than looking like a missed site. |
| 5 | `ItemCreationPredictionInputDto` defaults (`API/ForecastController.cs:225,228,231`) | **Delete the initialisers.** All three properties are `[JsonRequired]`. Verified `ForecastControllerTest.cs:387` constructs the DTO bare but returns on the `NotFound` path before reading any date, so it stays green. |
| Scope | One bugfix or split P0/P1? | **One bugfix.** |
| 6 | What ships as the default timezone? | **Ship the key absent.** `appsettings.json` gains no `Lighthouse` section (it has none today — `BaseUrl` and `OAuth:StateSecret` are read from config but never shipped as defaults). The resolution order does the work: containers keep UTC and see no behaviour change on upgrade; standalone picks up the host zone, which is the branch-B fix. **Consequence:** containerised non-UTC teams must opt in via `Lighthouse__TimeZone`, so that opt-in is the user-facing headline of this bugfix, not a footnote. |
| 7 | How is the backend test TZ pinned? | **`.runsettings`** with `RunConfiguration/EnvironmentVariables/TZ = Europe/Zurich`, referenced from the test `.csproj` via `<RunSettingsFilePath>` so a bare local `dotnet test` picks it up, not only CI. Plus an assertion on `TimeZoneInfo.Local.Id` — .NET caches the local zone the way Node does, and `docs/ci-learnings.md:85` records that `TZ` in the vitest `env` block did **not** take effect for exactly that reason. A silently-inert pin must fail loudly. |
| 8 | `DeliveryMetricSnapshot` day key | **Converge on `DateOnly` bucketing** — but NOT for the reason R2 originally gave. **The duplicate-row hazard does not exist:** `DeliveryMetricSnapshotRepository.cs:12` computes `var day = recordedAt.Date` and `:25` writes `RecordedAt = day`, so every persisted value is already midnight; it is the only production writer; and `Data/LighthouseAppContext.cs:400-401` already declares `HasIndex(s => new { s.DeliveryId, s.RecordedAt }).IsUnique()`. A range scan over midnight-valued data under a unique index *is* equality on a day key. What a zone shift actually causes is the same one-day skip/shift accepted for the three `DateOnly` families. **The change is kept on a corrected rationale:** a `DateOnly` key is structurally out of reach of the global `Properties<DateTime>()` converter (genuine R1 hardening, and the only *testable* improvement); the last instant→day reduction inside a repository disappears; all four snapshot tables converge on one shape; and `03-01` gains a **type-level** guard — no persisted snapshot day key may be typed `DateTime` — which catches a fifth snapshot table the day it is added rather than the day someone writes `UtcNow.Date` next to it. Fix's only schema change: expand-only EF migration via `Lighthouse.Backend/Create-Migration.ps1` (hyphenated — a `CreateMigration*` glob misses it; never `dotnet ef migrations add`, never Recreate) — add the column alongside `RecordedAt`, backfill, move reads and writes, leave the old column for a later release to drop. **Do NOT write a "no duplicate row is created" test** — it passes on unmodified HEAD and proves nothing. |
| 9 | Unique-index collision on backfill | **Fail fast with a diagnostic.** The migration must add a unique index over the backfilled `DateOnly` column to preserve today's database-level guarantee. A row with a non-midnight `RecordedAt` — unreachable via the current writer, but reachable via a restored backup or an older version — collides, index creation throws, and the application fails to start. Pre-check during migration and emit a message naming the colliding delivery ids and dates. Silently auto-fixing an operator's historical metrics is worse than a clear stop. This is an acceptance criterion in `02-02`, not an edge note. |

## 4. Design

### 4.1 Abstraction

```csharp
public interface ILighthouseClock
{
    DateOnly Today { get; }                          // the instance's calendar day
    DateTime TodayAsUtcMidnight { get; }             // Kind=Utc, EF-converter-safe
    DateTimeOffset Now { get; }                      // instant, delegates to TimeProvider
    TimeZoneInfo Zone { get; }
    DateOnly ToInstanceDay(DateTime utcInstant);     // Finding F
}
```

Two non-negotiable constraints, both evidence-driven:

1. **`Today` is `DateOnly`-first, and any `DateTime` it hands out carries `Kind = Utc`.**
   The global EF converter applies `ToUniversalTime()` to every non-`Unspecified` `DateTime`
   property *and to query parameters*. A `Kind = Local` local-midnight written to
   `DeliveryMetricSnapshot.RecordedAt` would be silently shifted back by the offset on write,
   landing on the previous UTC day — **re-introducing this exact bug through the persistence
   layer**. `Models/Team.cs:44` already applies `DateTime.SpecifyKind(..., DateTimeKind.Utc)`
   as a per-site ritual; the clock makes it the default.
2. **Entities take the day as a parameter, not a field.** `Team.GetThroughputSettings()`,
   `Feature.GetLikelhoodForDate()`, `Delivery.CalculateMetrics()` and
   `WorkItemBase.WorkItemAge` become parameterised on `DateOnly today` (preferred over
   passing `ILighthouseClock` — keeps the entities pure and matches the existing
   `blackoutPeriods` parameter-passing style at `Models/Delivery.cs:99`).

### 4.2 Configuration

Reuse the established instance-config surface, do not invent a new one:

- A new `Lighthouse:TimeZone` key is read in `Services/Implementation/ServiceConfig.cs` next
  to the existing `Lighthouse:BaseUrl` (`:9`) and `Lighthouse:OAuth:StateSecret` (`:10`).
  **Per decision 6, the key is NOT shipped in `appsettings.json`** — that file has no
  `Lighthouse` section today, and writing a concrete default into it would move every
  containerised instance off UTC on upgrade, unannounced. The key is read if present and the
  resolution order below covers its absence.
- The `Lighthouse__TimeZone` env override comes free via the `__` convention already
  documented at `docs/Installation/configuration.md:20`.
- Resolution order: configured IANA/Windows id → `TimeZoneInfo.Local` → `TimeZoneInfo.Utc`.
  Use `TimeZoneInfo.FindSystemTimeZoneById` (.NET 10 accepts IANA ids on Windows too).
  Log the resolved zone at startup. **An unresolvable id fails fast** — a silent fallback
  would reproduce this bug class invisibly.
- **Verified:** `mcr.microsoft.com/dotnet/aspnet:10.0` ships `tzdata`
  (`/usr/share/zoneinfo/Europe/Zurich` is present). No `Dockerfile` change needed. This was
  the RCA's single highest-risk unverified assumption and it is now closed.
- Deliberately **not** an `AppSetting` DB row: the clock is needed by
  `Standalone/StandaloneInitializer.cs` and by hosted services independently of the
  DB-settings surface, and `AppSettingService` itself takes a `TimeProvider` (circularity).

### 4.3 Rejected alternative

Per-request client timezone. Reads more correct, but the background snapshot jobs have no
request context and would still need a configured zone, and it would fragment the metrics
cache per viewer. **Confirmed by the code:** `TeamMetricsService` caches under
`(team, metric, mode)` keys (`:111`) with no day component; a single instance zone preserves
that, a per-viewer zone destroys it.

## 5. Verified inventory at HEAD

Counts exclude `obj/`, `bin/`, `StrykerOutput*`, `.claude/worktrees/`, and the vendored
`tools/codesign/actions-runner/_work/` runner artifact.

| | occurrences | files |
|---|---|---|
| `UtcNow.Date` / `DateTime.Today` in `Lighthouse.Backend` (prod) | **49** | 24 |
| same in `Lighthouse.Backend.Tests` | **275** | 45 |
| instant-valued `DateTime.UtcNow` / `DateTimeOffset.UtcNow` (prod, excl. `.Date`) | **38** | 24 |

### (a) Move to configured-zone `Today`

**Forecast projection & windows**
`API/DTO/WhenForecastDto.cs:16`; `API/FeatureForecastWindow.cs:20`;
`API/ForecastController.cs:60,85,86,94,133,148`; `API/FeaturesController.cs:93`;
`API/DeliveryRulesController.cs:58`; `API/DeliveriesController.cs:35,327,330`;
`Models/Feature.cs:71`; `Models/Delivery.cs:101`

**Throughput defaults**
`Models/Team.cs:33,34`; `Services/Implementation/TeamMetricsService.cs:75,123,795,809`

**Historic-range detection**
`API/PortfolioMetricsController.cs:108,121,255,272,587`;
`API/TeamMetricsController.cs:116,135,571`

**Snapshot recording / day keys**
`Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs:29,30,83,86`
(note `:30` is a full instant used as a day key — see risk R2);
`.../PercentilesOverTimeRecordingHandler.cs:90,123`;
`.../BlockedCountSnapshotRecordingHandler.cs:79`;
`.../ProcessBehaviorRecordingHandler.cs:132,187`

**Aging** — `Models/WorkItemBase.cs:78` (and `:80`, `:60`, `:115` per decision 3)

**Validation / licensing / write-back**
`Services/Implementation/BaselineValidationService.cs:32,43`;
`Services/Implementation/Licensing/LicenseService.cs:55` (decision 1);
`Services/Implementation/WriteBackTriggerService.cs:227`

**Delivery date validation** (decision 2)
`API/DeliveriesController.cs:81,139`; `Models/Delivery.cs:16`

**Demo data** — must move together with the above or E2E desyncs
`Factories/DemoDataFactory.cs:105`;
`Services/Implementation/DemoDataService.cs:104,142,147`;
`.../DomainEvents/DemoBlockedHistoryBackfillHandler.cs:116,117`;
`.../DomainEvents/DemoPercentilesBackfillHandler.cs:93`

**Finding F — instant→day reduction sites**
`Services/Implementation/TeamMetricsService.cs:803`;
`Services/Implementation/PortfolioMetricsService.cs:589,590,785,786`;
`Services/Implementation/BaseMetricsService.cs:907`;
`Models/WorkItemBase.cs:120`

### (b) Stay UTC — instants, ~30 sites

Audit/lifecycle stamps: `Models/Auth/UserProfile.cs:19,21`; `Models/Auth/ApiKey.cs:25`;
`Models/Authorization/ApiKeyPermission.cs:17`, `UserPermission.cs:19`;
`Services/Implementation/Auth/ApiKeyService.cs:48,66,164`;
`CurrentUserProfileService.cs:35,36,46`;
`Authorization/RbacAdministrationService.cs:482,705,751,1374`.

Sync/refresh bookkeeping: `Models/WorkTrackingSystemOptionsOwner.cs:138`;
`BackgroundServices/Update/TeamUpdater.cs:69,77`; `PortfolioUpdater.cs:32,101`;
`WorkItems/WorkItemService.cs:64`.

Blocked-transition intervals:
`DomainEvents/WorkItemBlockedTransitionCaptureHandler.cs:29`,
`WorkItemBlockedTransitionCloseHandler.cs:25`,
`FeatureBlockedTransitionCaptureHandler.cs:30`,
`FeatureBlockedTransitionCloseHandler.cs:25`.

Artifact naming / metadata: `API/DatabaseManagementController.cs:47`; `API/LogsController.cs:60`;
`DatabaseManagement/DatabaseManagementService.cs:230`; `Models/Forecast/ForecastBase.cs:19`.

Elapsed-duration measurement: `Cache/CacheItem.cs:13`; `Cache/Cache.cs:29`
(`DateTimeOffset.Now` — the only `.Now` in production; correct, but should move to
`TimeProvider` for testability, not to `Today`).

Tracker history cutoffs (decision 4, allowlisted with a comment):
`WorkItems/WorkItemService.cs:442,445`; ADO connector `:993`; Jira connector `:1251`.

### (c) Deleted

`API/ForecastController.cs:225,228,231` — DTO default initialisers (decision 5).

## 6. Migration sequence

One commit per step, each independently green.

1. **Pin the backend test TZ first** — `TZ=Europe/Zurich` for `dotnet test`, mirroring
   `Lighthouse.Frontend/package.json:12` (`cross-env TZ=Europe/Zurich vitest run`) and
   `Lighthouse.EndToEndTests/playwright.config.ts:90` (`timezoneId: "Europe/Zurich"`), both of
   which `b956a8857` added while leaving the backend un-pinned. This alone turns branch B red
   **before any production change**, giving a real failing test to fix against.
2. **Introduce `ILighthouseClock` + `ServiceConfig.TimeZone` + DI registration**, wired to
   the existing `TimeProvider` singleton. No call-site changes yet. Add `FakeLighthouseClock`
   to `Lighthouse.Backend.Tests/TestDoubles/`.
3. **Add the source-scan guard** (T2 below) in warn-list mode with the 49 known sites
   baselined, so no new site can be added while the migration runs.
4. **Migrate by cluster**, shrinking the baseline each time: entities (4 sites — highest blast
   radius, do while the change set is small) → snapshot handlers (12) → metrics/forecast
   controllers (20) → Finding F instant→day sites (8) → demo data (7) →
   validation/licensing/delivery (6).
5. **Delete the baseline**, flip the guard to hard-fail.
6. **Migrate the 275 test occurrences** to the fake clock in the same commit as their
   production cluster — otherwise they re-tautologise against the new expression.

## 7. Risks

**R1 — EF converter double-shift. Severity: HIGH. The most likely way this ships broken.**
`Data/Converters/UtcDateTimeConverter.cs` applies `ToUniversalTime()` to every non-`Unspecified`
`DateTime`, by convention over all properties (`LighthouseAppContext.cs:91-92`). EF applies
value converters to **query parameters** as well as stored values. So
`Services/Implementation/Repositories/DeliveryMetricSnapshotRepository.cs:12-16`
(`RecordedAt >= day && < nextDay`) would have both its stored values and its comparison bounds
shifted if `day` arrives `Kind = Local`. Reads would stay self-consistent while the stored data
silently became wrong for every other consumer.
*Mitigation:* the clock never returns `Kind = Local`; add a test asserting the **persisted**
`RecordedAt` day equals the configured-zone day, reading the row back through EF rather than
from the change tracker.

**R2 — `DeliveryMetricSnapshot` day-bucketing. Severity: MEDIUM.**
The ADO report's claim that only one row shifts is **verified correct for the `DateOnly`-keyed
snapshots**: `PercentilesOverTimeRecordingHandler.cs:164-169` upserts on exact `RecordedAt`
equality, so a zone change writes to a new-or-same day key and at most one day is
duplicated/skipped.

**The claim that `DeliveryMetricSnapshot` is different was WRONG — disproven at HEAD.** The
original reading was that `DeliveryMetricSnapshotRepository.cs:12-18` buckets a full *instant*
by range scan, so a zone shift could push an existing row outside `[day, nextDay)` and produce a
second row for the same visual day. It cannot: `:12` computes `var day = recordedAt.Date` and
`:25` writes `RecordedAt = day`, so **every persisted value is already midnight** even though
`DeliveryMetricSnapshotRecordingHandler.cs:30` passes a full `DateTime.UtcNow`; that method is
the only production writer; and `Data/LighthouseAppContext.cs:400-401` already declares
`HasIndex(s => new { s.DeliveryId, s.RecordedAt }).IsUnique()`. A range scan over
midnight-valued data under a unique index *is* equality on a day key. **R2's real severity is
LOW**, and its real artifact is the same one-day skip/shift in the 22:00–24:00 UTC window that
the three `DateOnly` families already accept.

Decision 8 keeps the `DateOnly` convergence anyway, on the corrected rationale recorded in §3.
**Consequence for test design:** do NOT write a "no duplicate row is created" test — it passes
on unmodified HEAD and proves nothing, the same self-satisfying failure mode `ci-learnings`
records for constants. Test the R1 hardening (the `DateOnly` key is out of reach of the global
`Properties<DateTime>()` converter) and the decision-9 fail-fast diagnostic instead.

**R3 — EF query translation. Severity: LOW.**
`DateOnly` is untouched by the global convention (`Properties<DateTime>()` only) and already
round-trips through both SQLite and Postgres (`PercentilesOverTimeRecordingHandler.cs:169`,
`TeamMetricsController.cs:491-492` are shipped and green). Finding F's sites are all
**client-side** post-`.ToList()`, so they carry no translation risk at all. Prefer `DateOnly`
for every new comparison introduced by the fix.

**R4 — Demo data + Playwright E2E. Severity: MEDIUM.**
`Factories/DemoDataFactory.cs:105` resolves the `{n}` / `{wn}` CSV placeholders against the
anchor, so demo dates move in lockstep. The exposure is on the **E2E side**, which does not
move with it: `Lighthouse.EndToEndTests/tests/helpers/csv/csvTestData.ts:26,70`,
`fixutres/LighthouseFixture.ts:90` and `specs/screenshots/Screenshots.spec.ts:143,145,266` — **six
`toISOString()` sites across three files, re-verified at `10c772076`** (the RCA originally listed
four; `Screenshots.spec.ts` has three, not one) — build dates
with `toISOString()` — UTC days computed in the Node test process, whose zone is the CI runner's
and is **not** affected by `playwright.config.ts:90`'s `timezoneId` (that sets only the browser
context). Once the backend day is Europe/Zurich, the Node-side UTC day and the backend day
differ for ~2 hours each night and any date-boundary assertion becomes flaky.
*Mitigation:* move the E2E helpers to a `formatLocalDate` equivalent and pin the E2E runner's
`TZ` to match `timezoneId`. Regenerate `@screenshot` baselines — and `rm` the PNGs first, since
`@screenshot` keeps the old image when the diff is under 0.5%.

**R5 — Frontend contract. Severity: LOW — the frontend is already correct.**
`localDate.ts` + `MetricsService.ts:44` send local calendar days post-`b956a8857`. This fix
closes the asymmetry rather than creating one. No frontend change required. One thing to watch:
`Lighthouse.Frontend/src/models/Delivery.ts:83` renders with `timeZone: "UTC"` — the correct fix
for Bug #4975 given a UTC-stored date, and it stays correct as long as the backend keeps
stamping `Kind = Utc` per §4.1. It becomes wrong the moment R1 is mishandled.

**R6 — `LighthouseAppContextUtcTest.cs` is NOT a tripwire for R1. Claim DISPROVEN 2026-07-27.**
The original claim here was that its seven `Kind == Utc` assertions are "the cheapest existing
guard against R1" and that any implementation leaking a `Local` kind into an entity trips them.
**That is false, and it was proven false empirically during step 02-03.** With
`DeliveryMetricSnapshotRepository.GetOrCreateForDay` deliberately sabotaged to write
`day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local)` under `TZ=Europe/Zurich`, all seven of
those assertions stayed **GREEN** while the persisted value was silently shifted a day backwards
(`Expected: 3/18/2026 / But was: 3/17/2026`, `Expected: 00:00:00 / But was: 23:00:00`).

The reason is the converter's own symmetry: `UtcDateTimeConverter` shifts the value on write and
then restores `Kind = Utc` on read, so a test that only inspects the Kind sees a correct-looking
`Utc` value that is on the wrong day. **Asserting the Kind cannot detect a defect whose whole
signature is that the Kind ends up right and the value ends up wrong.**

Consequence: the read-back-through-a-fresh-EF-context day assertion is not a belt-and-braces
duplicate of R6 — it is the ONLY thing covering this defect. Do not let a future reader drop it
on the grounds that `LighthouseAppContextUtcTest` "already covers Kind". Keep that file for what
it does cover, but it earns no credit against R1.

**R7 — Metrics cache. Severity: LOW.**
`TeamMetricsService` caches under `(team, metric, mode)` keys (`:111`) with no day component,
and the handlers already invalidate after recording
(`PercentilesOverTimeRecordingHandler.cs:110`). A single instance zone does not fragment the
cache.

## 8. Regression tests

**T0 — fails today, needs no new abstraction.** Pin `TZ`, drive both `ForecastController`
paths, and assert they anchor on the same calendar day. This is branch B and it is a live
standalone-distribution bug. Prefer driving the two endpoints over asserting clock identity, so
it is a behaviour test rather than a tautology.

**T1 — the timezone-boundary test (the core one).** Requires `ILighthouseClock` +
`FakeLighthouseClock`. Table-driven:

| fake instant (UTC) | configured zone | expected `Today` |
|---|---|---|
| `2026-07-27T23:30Z` | `Europe/Zurich` (UTC+2) | `2026-07-28` |
| `2026-07-28T00:30Z` | `America/Los_Angeles` (UTC-7) | `2026-07-27` |
| `2026-07-28T00:30Z` | `UTC` | `2026-07-28` |

Assert at three levels against the same fixture:
- **Unit** — `Team.GetThroughputSettings()` end date equals the expected day
  (`Models/Team.cs:33-34`). Returns the UTC day today → red.
- **Handler/persistence** — run `PercentilesOverTimeRecordingHandler.HandleAsync(TeamDataRefreshed)`
  and assert the persisted `PercentilesOverTimeSnapshot.RecordedAt` equals the expected day
  (`:90`, `:123`). Read the row back through EF, not from the change tracker — this also guards R1.
- **API** — `POST /api/forecast/manual/{id}` with a target date; assert the returned
  `ExpectedDate` is projected from the expected day, not the UTC day (`WhenForecastDto.cs:16`).

**T1b — Finding F boundary.** An item closed at `2026-07-27T22:30Z` under `Europe/Zurich`
belongs to throughput day `2026-07-28`, not `2026-07-27`. Assert through
`TeamMetricsService.GetThroughputForTeam` (`:803` is the site under test).

**T2 — anchor-seam source guard** (prevention, addresses root causes B and D directly).
Model it on `Lighthouse.Backend.Tests/Architecture/ExpandOnlyMigrationGuard.cs` — a plain
source-file scanner, **not** ArchUnitNET, because `DateTime.UtcNow` is a property access on a
type every class already depends on and ArchUnitNET's dependency rules cannot express it. Scan
`Lighthouse.Backend/**/*.cs` for `UtcNow.Date`, `DateTime.Today` and
`DateOnly.FromDateTime(DateTime.` and fail on any occurrence outside the clock adapter and the
decision-4 allowlist. Name it `CalendarDayAnchorSeamArchUnitTest.cs` to sit with the twelve
existing `*SeamArchUnitTest.cs` files.

**T3 — instants must not move.** Assert that `TimeProvider`-backed instants (token expiry,
`GrantedAt`, blocked-transition `EnteredAt`) are unchanged by a zone configuration, so the
migration cannot over-reach into category (b).

## 9. Follow-through outside the code

- **Docs** — `docs/Installation/configuration.md` gains the `Lighthouse:TimeZone` /
  `Lighthouse__TimeZone` setting. Document on the metrics and forecast endpoints that a bare
  `YYYY-MM-DD` names a calendar day **in the instance timezone** (root cause E: the semantics
  were never written down, which is why the two stacks converged on different readings).
- **Release notes** — the R2 one-day kink, and the decision-2 delivery-date behaviour change
  which affects UTC users too.
