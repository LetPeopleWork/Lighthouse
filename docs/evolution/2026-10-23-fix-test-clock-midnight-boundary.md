# Fix — backend tests failed every night between 22:00 and 24:00 UTC

**Shipped** 2026-10-23 · 19 commits `7d98c679e` … `44e131d70` on `main` · no ADO item exists for this
fix (it was raised from a red CI run, not from the board) · design: the approved root-cause analysis
in `docs/feature/fix-test-clock-midnight-boundary/rca.md`.

## Summary

The backend suite failed whenever CI happened to run in the hour or two before UTC midnight. All 23
failures were the same mistake: two different notions of "today" being compared. 22 of them were test
bugs and one was a small production bug (the log download's file name). The fix makes the mistake
fail on **every** run instead of only at night: the test host's clock is frozen at 23:30 UTC, the
moment where the UTC day and the Zurich day disagree, and the ambient clock is banned by the compiler
in both the tests and production.

## The problem

CI run 39873168742 ran at about 2026-10-22T23:43Z — already 2026-10-23 in Zurich. Result: 23 failed,
11,234 passed, every failure an off-by-one calendar day.

The backend tests run with `TZ=Europe/Zurich` (pinned on purpose, so that day bugs cannot hide behind
a UTC runner). The application takes its "today" from the instance's time zone through
`ILighthouseClock`. Many tests worked out "today" themselves from the real clock — `DateTime.UtcNow.Date`
in the integration tests, `DateTime.Now` / `DateTime.Today` in unit tests — and between 22:00Z and 24:00Z
(23:00Z in winter) those answers are a day apart from the application's.

## Root cause chain

1. **Integration tests (16 failures):** private `Today => DateTime.UtcNow.Date` helpers, written before
   the instance-day clock existed, were never migrated. The integration host ran on the real clock, so
   test and host agreed for 22 hours a day and disagreed for the rest.
2. **Unit tests (6 failures):** `TestToday.Clock` is a fake clock in UTC, while fixtures fed the subject
   `DateTime.Now` / `DateTime.Today`, which are Zurich-local. Nothing stopped a test from reading the
   local clock.
3. **Production (1 failure):** `LogsController` labelled the log download with
   `{DateTime.UtcNow:yyyy.MM.dd}` — the UTC day, not the instance day. The existing calendar-day guard
   scans for known spellings and did not know the format-string one.
4. **Underneath all three:** tests had no frozen, boundary-placed time, and 39 production sites still
   read the ambient clock directly. Whether the defect class showed up depended on the hour the build
   ran.

## What changed

- **A frozen boundary instant, with a guard.** `TestToday.FrozenNow` is 23:30 UTC on the day the test
  process started. `TestClockBoundaryTest` fails, naming the zone, if the Zurich pin is removed or the
  instant drifts off the boundary, and checks the frozen day against the real clock (today or
  yesterday) rather than against the value it was built from.
- **The host clock is frozen.** Every host built on `TestWebApplicationFactory` reads a
  `FakeTimeProvider` stopped at `FrozenNow`, and `TestToday.Clock` is built from the same instant. A
  test that mixes the UTC day with the instance day now fails at any hour.
- **Tests read the host or `TestToday` clock.** Integration tests take "today" from the host's
  `ILighthouseClock` (`IntegrationTestBase.InstanceClock` / `InstanceToday`) and send as-of dates as
  `yyyy-MM-dd` days, as the frontend does. Unit tests take days and instants from `TestToday`. The
  embed test host forges session cookies from the host's clock, and integration fixtures seed
  host-compared instants (OAuth expiries, last-update times) from it too.
- **The ambient clock is banned by the compiler.** `Microsoft.CodeAnalysis.BannedApiAnalyzers` runs on
  the test project (`BannedSymbols.Tests.txt`: `DateTime.Now`, `DateTime.Today`, `DateTimeOffset.Now`)
  and on the production project (`BannedSymbols.txt`: those plus `DateTime.UtcNow` and
  `DateTimeOffset.UtcNow`). A new read fails the build with RS0030. Migration projects are not covered.
- **Production instants come from the injected clock.** Blocked-transition handlers, the
  Team/Portfolio/Forecast updaters, work-item sync, the cache, API keys, user profiles, permission
  grants, delivery notes, forecast creation time, tracker history cutoffs and the backup file name and
  manifest all read `TimeProvider` (instants) or `ILighthouseClock` (days). Production behaviour is
  unchanged: the registered `TimeProvider` is `TimeProvider.System`.
- **The log download is named for the instance day** (`clock.Today`), the one user-visible fix.
- **The calendar-day guard is wider.** `CalendarDayAnchorSeamArchUnitTest` now also reports
  `timeProvider.GetUtcNow().Date`, `.UtcDateTime.Date`, a UTC instant handed to `DateOnly.FromDateTime`,
  and a UTC instant formatted as a date inside an interpolation hole. The backup file name, which
  formats the UTC instant to the second on purpose, is excused by name.

## Key decisions

Taken with the maintainer: the RCA and an 11-step roadmap, delivered as one slice; the frozen instant is
"23:30Z on today's UTC date" rather than a fixed literal date, because the ~760 fixtures that still
read real `UtcNow` stay within a day of it, while a literal would put them months away; the regression
guard landed first and the freeze only after the tests it would turn red were fixed, so every commit was
green.

Taken autonomously during the AFK run, and recorded for review:

- **`ForecastSimulation` parameter object.** `ForecastService` was at Sonar's seven-constructor-parameter
  limit (S107), so the draw-stream factory and the simulation limits now travel as one object, freeing a
  slot for `TimeProvider`. Rejected: stamping the creation time in `ForecastUpdater`, which would have
  left never-persisted manual forecasts with `DateTime.MinValue`.
- **Backups via an injected `TimeProvider`, not a suppression.** The backup file name and manifest keep
  real UTC labels; reading them from an injected provider avoided any `#pragma warning disable RS0030`.
- **Two deleted tests replaced.** `CreateApiKey_CreatedAtIsApproximatelyNow` and
  `AuditStamps_UnderANonUtcInstanceZone_StayAtTheUtcInstant` pinned the ambient clock. The adversarial
  review found their replacements compared values only (`DateTime` equality ignores `Kind`), so they
  were rewritten to drive the real callers under a clock fourteen hours ahead of UTC and assert both the
  value and `DateTimeKind.Utc`.
- **Precautionary host-clock fixture seeds.** OAuth expiry and last-update seeds were moved to the host
  clock even though a probe showed no current scenario reaches that comparison: the next one that does
  would otherwise depend on the time of day.

## Gates

- **DES:** 11/11 steps, each RED → GREEN → COMMIT, integrity verified.
- **Refactor pass:** comments that cited internal ids rewritten in plain language; duplicated clock
  lookups removed; the arch-guard extension above.
- **Adversarial review: REJECTED** (one blocker — a stamp test that could not fail and Kind-blind stamp
  tests; two medium — real-clock OAuth fixtures that flip around 22:35Z, guard gaps; lows). No
  production defect found. One revision pass of four test-only commits, each proven red → green.
- **Mutation (Stryker.NET, 28 files):** first run 64.22 %; one kill round of tests; combined
  **83.67 %** (gate 80 %), every mutant on a changed line killed. Six large files were not mutated,
  each changed by 1–8 lines in a 1,000–3,600-line file (whole-file mutation would bury the change);
  their changed lines are pinned by clock-driven tests instead. Frontend N/A — no frontend file changed.
  Detail: `docs/feature/fix-test-clock-midnight-boundary/mutation/results.md`.
- **Final suite on the rebased tree:** 11,102 passed, 0 failed; `dotnet build` zero warnings.

## Residual risks and follow-ups

- **A run that crosses 00:00Z still masks the class for that run.** The frozen day is the start day,
  and real `UtcNow.Date` then catches up with the instance day. Accepted: one run in many.
- **Live Jira and ADO connector tests** compute their expected cutoff from `DateTime.UtcNow` while the
  connector uses `TimeProvider.System`. Both are real time, so only a midnight race; live-only.
- **Four classes sit at the S107 limit of seven constructor parameters:** `PortfolioUpdater`,
  `ForecastUpdater`, `ForecastService` and `JiraWorkTrackingConnector`. The next dependency needs a
  parameter object or a split.
- **Flaky under full-suite load:** `TeamDeleteSerialisationTests.DeleteTeam_NoOtherActivity_*` ("Timed
  out waiting for active updates to drain"). Passes alone; unrelated to this change.
- **About 140 `UtcNow.Date` uses remain in unit tests.** The analyzer cannot ban a property chain;
  integration tests are covered by the freeze, and unit tests compare against a UTC-zoned `TestToday`,
  where the UTC day is the right answer. Zoning `TestToday` to the instance zone (about 30 unit tests to
  migrate) would close this and is a separate story if wanted.

## Lessons learned

- **A zone pin makes a day bug possible to detect, not certain.** Detection still depended on the hour
  of the build. Freezing time at the boundary is what turns it into a deterministic failure.
- **Ban the ambient API rather than scanning for spellings.** The text guard missed a format string;
  the analyzer cannot be spelled around. The guard stays for the day-from-instant cases the ban cannot
  see.
- **Freezing one clock exposes every helper that reads another.** The embed cookie forger hid 38 tests
  the moment the host clock moved; time-bound fixtures must read the host's clock.
- **A stamp test that hands in the value it asserts cannot fail**, and `DateTime` equality ignores
  `Kind`. Drive the real caller under a zone far from UTC and assert the kind as well as the value.
