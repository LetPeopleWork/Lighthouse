# Instance Calendar Day — Evolution

**Feature:** fix-backend-utc-today-anchor | **ADO:** Bug #5567 | **Shipped:** 2026-07-28 | **Commits:** `3dc237b30..b19f21270` (145 files, +9351/−688)

## What shipped

The backend derived "today" from the machine clock in 49 places, so forecasts, throughput windows, snapshot day keys and work-item ages were computed on the **UTC** calendar day regardless of where the team actually was. At UTC+2 the server day rolled over two hours late; at UTC-7 everything after 17:00 local was already tomorrow.

All 49 anchors now resolve through one configurable instance zone, and a hard-fail source guard makes a 50th impossible to add.

**The setting ships absent.** A container upgrades to byte-identical behaviour; a non-UTC team opts in with `Lighthouse__TimeZone`. That was deliberate — silently re-dating everyone's history on upgrade is worse than an inert release — but it means the release notes, not the code, are the delivery mechanism for the users who reported it.

| Step | Outcome |
|---|---|
| 01-01 | `.runsettings` pins the backend test host to `Europe/Zurich` via `<RunSettingsFilePath>`, proven active by a `TimeZoneInfo.Local.Id` assertion. UTC is the one offset where this bug class cancels out, which is why CI was blind to it. |
| 01-02 | `ILighthouseClock` over the existing `TimeProvider`; `Lighthouse:TimeZone` in `ServiceConfig`; resolution configured → `TimeZoneInfo.Local` → `Utc`; an unresolvable id **fails startup**. |
| 01-03 | `CalendarDayAnchorSeamArchUnitTest` — source scanner with all 49 sites baselined; strips comments and string literals so documenting the bug cannot break the guard. |
| 02-01 | Entities take `DateOnly today` as a **parameter**, never the clock. |
| 02-02 | `DeliveryMetricSnapshot` converges on a `DateOnly` day key; expand-only migration plus a startup collision guard that refuses to boot rather than de-duplicate. |
| 02-03 | The four snapshot recording handlers read the clock; the persisted row is asserted through a **fresh** EF context. |
| 02-04 | 25 metrics/forecast read-path anchors migrated, 3 dead DTO initialisers deleted, T0 un-ignored. |
| 02-05 | Finding F — stored instants reduce to the instance day, so both ends of every day comparison share one definition. |
| 02-06 | Demo data moves with the anchor; the three E2E zones (runner `TZ`, browser `timezoneId`, backend `Lighthouse__TimeZone`) are aligned deliberately. |
| 02-07 | Licensing, baselines, write-back and delivery-date validation; the four tracker cutoffs stay UTC with a stated reason each. |
| 03-01 | Guard flipped to hard-fail, baseline file deleted; `InstantsUnaffectedByZoneTest` pins that instants did **not** move. |
| 03-02 | 17 tautological test occurrences converted; ~100 benign and 49 deliberate ones classified and left. |
| 04-01 | Configuration docs, API date semantics, `brief.md` back-propagation, release notes, ci-learnings preflight entry. |

## Root cause

The codebase conflated **"store instants in UTC"** with **"compute calendar days in UTC"**. It had a first-class, enforced, tested abstraction for the first — two `UtcDateTimeConverter` classes and seven `Kind == Utc` tests — and none at all for the second. An instant has no timezone; a calendar day is *defined* by one. With no named seam, every author reached for `DateTime.UtcNow.Date`.

Two supporting causes mattered as much as the core one:

- **Branch B** — `ForecastController` carried *both* spellings (`DateTime.Today` and `UtcNow.Date`) in one request lifetime. They agree only under UTC, so the containerised build was fine and the **standalone** distribution shipped broken to every non-UTC host.
- **Root cause D** — the suite could not have caught this. `FeatureDtoTest.cs:12` was literally `private static DateTime Today => DateTime.UtcNow.Date;`. A test that recomputes the production oracle passes for every possible value of "today", including a wrong one. 275 such occurrences existed at the start.

## Decisions that shaped it

- **The key ships absent** (decision 6). Safety over reach: no instance changes behaviour on upgrade. Cost: the release is inert for the reporters until they act.
- **Entities take the day as a parameter, never the clock** (decision F/§4.1). Forced independently by two facts — EF materialises them with no constructor injection, and `brief.md:1941`'s shipped ArchUnitNET rule forbids `Models.*` depending on `Services.Interfaces`, where the clock lives.
- **Fail fast on an unresolvable zone id.** Absent means "no opinion"; wrong means "an opinion I cannot honour". Silently downgrading the second to the first is how this bug class hides.
- **The migration refuses to start rather than de-duplicate** (decision 9). An operator whose app won't boot with a message naming the exact rows can fix it; one whose history was quietly rewritten cannot.
- **`CycleTime` required an ADR-061 §3 amendment.** The shape constraint (parameterless get-only property) was relaxed to admit a zone; the model→settings dependency prohibition it actually exists to protect was kept and is still enforced.

## Four claims that were wrong

Recorded because they cost real time and would otherwise be re-derived. All are corrected in place in `docs/analysis/ADO-5567-backend-utc-today-anchor.md`.

1. **The `DeliveryMetricSnapshot` duplicate-row hazard does not exist.** `GetOrCreateForDay` always stored midnight under a unique index, so the range scan *was* equality on a day key. The `DateOnly` convergence was kept on a corrected rationale — converter reach and the type-level guard — not the imagined hazard.
2. **T0 does not prove branch B, and nothing at runtime can.** Verified by un-ignoring it: both endpoints agreed and only the expected-day assertions failed, because an injected fake instant never reaches a statically-read clock. The *source guard* is branch B's proof.
3. **`LighthouseAppContextUtcTest` does not guard R1.** Proven by sabotage: with the repository writing `DateTimeKind.Local`, all seven assertions stayed **green** while the persisted value shifted a day back. The converter restores `Kind` on read after shifting the value, so asserting the Kind cannot detect a defect whose signature is *Kind right, value wrong*.
4. **`BaselineValidationService`'s `DateOnly.FromDateTime` is correct.** The Phase-4 review flagged it as a mixed-definition defect; the bounds are user-picked calendar days stored at UTC midnight, and `ToInstanceDay` there would move a date picked west of UTC back a day.

## Lessons

**A half-converted comparison is worse than none.** `02-05` shipped `WorkItemAge` with a UTC start and an instance-day end — over-counting age by one for items started in the offset window, where the old code had been right by accident. Its own regression test could not see it, because that test pinned byte-identity *under a UTC clock*, where the two definitions collapse. Fixed in a follow-up commit; the governing rule is now explicit — **both ends of any day comparison use the same definition of a day.**

**Sabotage every assertion that guards a silent failure.** Three of the most important tests in this feature were only trustworthy because they were deliberately broken first. One of them (R6) turned out to be guarding nothing at all.

**Estimates derived from a document drift from the code.** The RCA's headline cluster counts were consistently off — "12 sites" was 9 anchors across 4 files, "8 sites" was 7 expressions. The per-step enumerations reconciled; the summary numbers never did. Gate on the guard's own count, not on arithmetic.

**A required local gate that depends on a third party is not a gate.** Two of them here: the GitHub release tests (unauthenticated 60/hr, shared IP) and `JiraWriteBackTest` (live Jira, eventually-consistent JQL read-back). Between them they produced a 600-second apparent "hang" and two false regressions during this work.

## Verification

- Backend suite **3855 passed, 0 failed, 0 skipped**; `dotnet build` 0 warnings under `TreatWarningsAsErrors`.
- Mutation **83.86%** (239/285) against the 14 files carrying the arithmetic and persistence decisions — up from 77.19% on the first run; seven new tests closed 19 mutants. Config and survivors in `docs/feature/fix-backend-utc-today-anchor/mutation/`.
- CI green on backend, **E2E**, **sonar-gates**, SQLite, Postgres and auth. The three standalone packaging jobs were queued behind an offline self-hosted signing runner, which equally blocked an unrelated dependabot commit.
- Production calendar-day anchor count **49 → 0**, enforced by a hard-fail guard.

## Links

- RCA, all nine decisions, verified inventory, risk register: `docs/analysis/ADO-5567-backend-utc-today-anchor.md`
- Roadmap: `docs/feature/fix-backend-utc-today-anchor/deliver/roadmap.json`
- ADR amendment: `docs/product/architecture/adr-061-named-cycle-time-ordered-boundary-computation-placement.md` (Amendment — 2026-07-27)
- Architecture: `docs/product/architecture/brief.md` (`ILighthouseClock` seam, snapshot day-key convention)
- Configuration: `docs/Installation/configuration.md`; API date semantics: `docs/concepts/api-versioning.md`

## Open

- The legacy `DeliveryMetricSnapshot.RecordedAt` column survives by design (expand-only). `CalendarDayAnchorSeamArchUnitTest` holds an exemption entry whose stale check forces its deletion in the same commit as the eventual drop.
- The four snapshot tables name their `DateOnly` key inconsistently — three `RecordedAt`, one `RecordedDay` alongside a legacy `DateTime RecordedAt`. Settle at the contract-phase drop; the type-level guard keys off type, not name, so a rename will not break it.
- No `@screenshot` baselines were regenerated: at the hour `02-06` ran, the instance day equalled the UTC day, so no demo date moved. Any doc screenshot showing a forecast or metric date is worth re-checking on a non-UTC instance.
