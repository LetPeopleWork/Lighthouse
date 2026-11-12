# Feature: story-6602-refinement-and-report-window-at-the-port

Story #6602 · Move the remaining Refinement and report-window acceptance classes to the service port · tagged
`Continuous Improvement`, **no parent Epic** (a follow-up of T5 #6469, whose Epic #6506 is Closed). Catalogue:
[Epic 6264 workspace](../epic-6264-modular-monolith/feature-delta.md). Plan and measurements:
[T5 workspace](../story-6469-t5-acceptance-at-the-service-port/feature-delta.md), the "Slice E" sections.

DISCUSS: covered by T5. The scope comes from the ADO description and from the maintainer's decision at T5's slice E
checkpoint (2026-11-08). Waves start at DESIGN. Size M.

Order (maintainer, 2026-11-12): **#6602 runs before T10 #6537.** T10's "before" set is taken after this Story's last move
push, so no class moves twice and T10's time limits are measured on the final suite.

## Wave: DISCUSS / [REF] Inherited

Link, do not copy. From the T5 workspace:
- "Slice E note: Refinement (the addendum DESIGN owes)": the design this Story starts from.
- "Slice E checkpoint: measured, Stage 3 re-run, and the question": `c`, `h`, Stage 3 for E and F, the 14.1 s line, `P2`.
- "Maintainer decision at the slice E checkpoint": settings come from the `IAppSettingService` stand-in, not the real
  seeders; E13 checks the stand-in's answer against a seeded database.
- "Slice E review": the guard gaps this Story closes as its classes move (E10, E13, `PortGivensGuardTest`, `ScriptedForecasts`).
From the catalogue: [REF] How to pick up a Story, Outcome KPIs + guardrails, Risks.
Also: `.claude/skills/test-writing/SKILL.md` and `references/backend.md`; ADR-237 (per-tier limits).

## Wave: DISCUSS / [REF] DoR re-run

| Item | Answer |
|---|---|
| Problem stated | Host-based acceptance classes cost about 17 s summed in a full run, where a port test proves the same rules |
| Persona / job | infrastructure-only (D-6264-2) |
| Scope bounded | 3 Refinement classes and `ReportWindowBoundaryScenarios`, plus the wire rows E1, E3–E7 and E11 as their classes move |
| Acceptance criteria | Rows equal before and after (id list); `HostUsersStillToMove` loses the 4 entries; summed time falls by the line |
| Dependencies | T5's harness (`RefinementAtThePortTest`, `RefinementPortGivens`, `TestHostUseGuardTest`) is on `main`. None open |
| Sized | M; carpaccio at DESIGN, at most 5 slices |
| Behaviour-free | Yes, test structure only |
| Cross-cutting | RBAC, Clients, website, usage data, UI sketch, Terminology, context map: N/A, because no production code or user-visible behaviour changes |
| Open for DESIGN | Slice order (E before F); F's `c` measured on its own set-up, not projected from E |

## Wave: DISCUSS / [REF] Baseline re-count (2026-11-12, `e8273cf51`)

| Number | Brief / T5 (2026-11-08) | Today |
|---|---|---|
| Refinement rows to move (`Slice05`, `Slice10`, `Slice12`) | 49 | 41 test methods (11 / 10 / 20) and 15 `[TestCase]` lines; the row count is re-read from a TRX at DESIGN |
| `ReportWindowBoundaryScenarios` rows | 36 (from `TheRowsOfTheBoundaryTable`) | one `TestCaseSource` method; re-read from a TRX at DESIGN |
| `HostUsersStillToMove` entries | not stated | 329 lines; the 4 classes are on it (lines 165, 169, 171, 186) |
| Estimated saving (E + F, no seeders) | about 17.4 s against a 14.1 s line | **DESIGN, 2026-11-12:** F is 15 rows, not 36; projection about 13.6 s (E alone about 10.6 s), see "Measurement plan" |
| Measured at DESIGN (TRX) | 49 Refinement + 36 report-window | **Refinement 56 rows (18 / 12 / 26), 49 move and 7 stay at a host; report-window 15** |
| Changes to these files since 2026-11-08 | — | one: `a28c90685` (Flow metrics contract move), no change to the classes' rows |

Class names in the guard list: `Slice05NeedAndVerdictTest`, `Slice10SleYardstickTest`, `Slice12CommentsTest`
(`Refinement/`), `ReportWindowBoundaryScenarios` (`Reports/`).

## Wave: DISCUSS / [REF] Checklist

- [ ] Behaviour-free: no production code changes; existing API, E2E and response tests green unmodified, apart from the moved tests
- [ ] Every removed host assertion has a row at the port or in a wire test; the id list before and after is compared
- [ ] Ratchet in the same change: each moved class leaves `HostUsersStillToMove`; the list is never widened
- [ ] The guard gaps from T5's slice E review are closed for the classes that move
- [ ] Moves separate from edits (`refactor(test-infra): move …` and a separate edit commit)
- [ ] Re-count before designing (done above; DESIGN re-reads rows from a TRX)
- [ ] Saving measured, as T5 measured it: each class alone and in a full run, before and after
- [ ] Mutation: N/A for production code, because none changes; stated at close-out
- [ ] Architecture docs: ADR-237 or `ARCHITECTURE.md` only where a decision changes
- [ ] One oracle file per nwdes slice (lesson from S02 and S05f)
- [ ] No release notes (behaviour-free)
- [ ] T10 #6537 told when the last move push lands, so its before set starts after it

## Wave: DESIGN / [REF] Scope, route and measured rows (2026-11-12)

Route: spine wave DESIGN, then host-led `nwdes` DELIVER (`/nw-deliver`), no lane. Size M. Paradigm: object-oriented (project rule); the change is test structure only, so no domain class is added. Read first, as asked: `test-writing/SKILL.md` ends "End of SKILL.md"; `references/backend.md` ends "End of backend.md".

**Rows, read from a TRX** (`evidence/trx/*.trx`, `evidence/ids-*.txt`, one round per class alone, Release, timer on, `7` tracker variables removed with `env -u` on every run, `dotnet build-server shutdown` first; all rows passed). Timer CSVs are in `evidence/timer-*.csv`.

| Class | Rows (TRX) | TRX s | One-time set-up s | Host build s | Set-up mean ms | Port | Stays at a host |
|---|---|---|---|---|---|---|---|
| `Slice05NeedAndVerdictTest` | 18 | 5.364 | 1.782 | 1.753 | 156.6 | 17 | 1 (`The_range_uses_the_same_horizon_as_the_manual_forecast`) |
| `Slice10SleYardstickTest` | 12 | 3.186 | 1.587 | 1.562 | 160.3 | 10 | 2 (`Setting_an_SLE…`, `Clearing_the_SLE…`: the settings save is the action) |
| `Slice12CommentsTest` | 26 | 5.501 | 1.641 | 1.616 | 134.2 | 22 | 4 (log 404, 2 slash references, question without a name) |
| `ReportWindowBoundaryScenarios` | **15** | 3.247 | 1.755 | 1.700 | 148.1 | 15 | 0 |
| **Total** | **71** | 17.298 | 6.765 | | | **64** | **7** |

- **The brief's "49 + 36" is wrong on the second term.** Refinement: 56 rows, of which 49 move and 7 stay at a host. The report-window class is **15** rows (the table `reportWindowBoundaries.json` has 15 cases; T5's Slice F table also says 15). T5's checkpoint used "F, 36 rows" in Stage 3; that 36 is not this class. Corrected below.
- Alone, E's three classes cost 19.06 s (TRX plus one-time set-up) and F 5.00 s. Not measured: three rounds, the full-run share. The machine load climbed to about 5 after the build; these are one-round figures and carry that caveat.
- Ids: `evidence/ids-*.txt` holds the 71 ids with outcomes. A moved class keeps its namespace, class name and method names (D-6469-8), so the ids of the 64 port rows stay; the 7 host rows change class and are mapped one by one below.

## Wave: DESIGN / [REF] Decisions

| Id | Decision | One-line why |
|---|---|---|
| D-6602-1 | Order: `Slice10`, then `Slice05`, then `Slice12`, then a stop-and-ask checkpoint, then F | E before F per T5; smallest harness change first (`Slice10` casts no vote); `Slice12` needs the most new wire rows, so it comes last and the checkpoint reads a full E |
| D-6602-2 | F needs a **new** Reports port base, `ReportsAtThePortTest` (CREATE_NEW) | `ServicePortHarness` is used by the Reality Check, Refinement and System-wide-access ports; nothing owns `IReportQueries` / `IReportCommands`; `Program.RegisterReports` lists the 17 registrations to copy |
| D-6602-3 | F's checkpoint decides whether it is built: its `c` is measured on its own set-up, not projected from E | Only 15 rows, about 2.9 s gross; the base is the cost |
| D-6602-4 | Seeders: keep the `IAppSettingService` stand-in, no real seeders (inherited, not reopened) | Maintainer, 2026-11-08; E13 holds the stand-in to a seeded database |
| D-6602-5 | The 7 rows that must stay at a host become wire-contract rows in the **existing** `RefinementWithoutSignInWireContractTest`, one `partial` file per slice | A wire contract is a named reason on the guard; no new host class, no new host build; one oracle file per slice avoids stale approvals |
| D-6602-6 | The port base gets `ISizingLogCommands` (registered as on the host, scoped) and the voter and tab/log readers; Work Item and finished-work Givens move **into** `RefinementPortGivens` | Slice05 and Slice12 cast votes; a wire row needs the same Givens on a host; one builder stays the only way to set up a Team |
| D-6602-7 | A moved class keeps its data: Slice10 and Slice12 use a new port Given for the 5-Work-Item Gravity of the vote host (GR-040, 058, 051, 054, 073, 074), not the 7-item one | A move is not an edit; the 7-item base Given adds GR-059 and changes what "the same Work Items" means |
| D-6602-8 | Port steps get a voter by `VoterIdentityResolver.ForWriteAsync(name, key, none)` and then `ISizingLogCommands`, as `RefinementVotesController` does | Writing the raw key would let `ForTeam` miss the voter's own vote (T5 trap 1) |
| D-6602-9 | Rows are read from `RefinementViewDto` / the log built as the controller builds them, as `NeedOverOneCycleTest` does | The wire text of enums and dates is the DTO's, so port rows keep their string assertions |
| D-6602-10 | Guard gaps close in the slice whose class needs them (table below) | A guard closed with no class that needs it cannot be seen to fail |
| D-6602-11 | No dedup of rows that overlap `NeedOverOneCycleTest` | Ids must survive; deleting is an edit, out of scope for a behaviour-free move |
| D-6602-12 | Today is explicit in every moved Given: `TodayIs(2026, 10, 7)` unless the row sets another day | The host clock starts on 7 October 2026 09:00 UTC; the port base starts on `TestToday.FrozenNow` (23:30 UTC on the process start day). Slice10's window edges and Slice12's `recordedAt` check read the clock |

## Wave: DESIGN / [REF] Which wire rows each class needs

Checked against the code that stays on the host (reads of each member in `Slice02`, `03`, `07`, `09`, `11`, `13`, `15`, `16`, `17`). Re-read when the class moves.

| Row | Needed when | Class | Reason |
|---|---|---|---|
| E1 (narrowed) | `Slice10` | `yardstick` member with `source`, `days`, `probability` | Only `Slice10` calls `YardstickIn` |
| E4 (narrowed) | `Slice10`, `Slice05` | yardstick source text `Sle`, `CycleTimeFallback`, `Unavailable`; need reasons `InsufficientData`, `NoRefinementStates` as text | `NoCadence` stays read by `Slice09`; the other two are read only by `Slice05` once `NeedOverOneCycleTest` is gone |
| E3 (narrowed) | `Slice12` | `recordedAt` as `yyyy-MM-ddTHH:mm:ss.fffZ` | Only `Slice12` compares it (`Slice17` nulls it) |
| E5 (narrowed) | `Slice12` | `isOpenQuestion`, and the log's two members `entries`, `voters`, the three answer keys | `isOpenQuestion` is read only in `Slice12`; `Slice17` keeps the rest of the entry names |
| E6 | `Slice12` | 409 and `work-item-not-in-refinement` for a question | The vote's 409 stays pinned by `Slice11` |
| E7 | `Slice12` | 400 and `comment-required` (empty), 400 and `comment-too-long` (a question and a vote comment, at 2001 characters), and 2000 characters accepted at the limit | Only `Slice12` pins them |
| E11 | `Slice12` | the row an answer to a question or comment carries, including its open-question flag | `Slice17` keeps status and row; flag read: re-check |
| E2, E8, E9, E12 | | already landed or not needed | |
| E10, E13 | each slice | tightened, see below | |

New wire rows: **up to 9** (E1 1, E4 1 or 2, E3 1, E5 1, E6 1, E7 3 or 4, E11 1; the feature-A total stays within 9 plus the boundary row). Moved host rows: 7. The wire class goes from 7 rows to at most 23. Window class (F): no new wire row. `today`, `earliestStart`, `minimumLengthDays` are pinned by `Slice01TeamReportScenarios` and `CreateForSeveralTeamsScenarios`; the 400 and the refusal code of each window refusal by `ThenItIsRefusedFor` in `Slice01`, `Slice02`, `Slice07` and `Slice10`, all of which stay. Re-verify at the F slice.

## Wave: DESIGN / [REF] Reuse Analysis

| Candidate | Where | Decision | Why |
|---|---|---|---|
| `RefinementAtThePortTest` | `API/Integration/Refinement` | **EXTEND** | Register `ISizingLogCommands`; add voter, tab and log steps. It already has the database, clock, forecast script, blackout doors |
| `RefinementPortGivens` | same | **EXTEND** | Takes `TheTrackerHolds`, `TheTeamFinishedWorkEveryDay`, the 5-item Gravity and `GravitysStates` from the base and the wire class (two copies of `GravitysStates` today) |
| `NeedOverOneCycleTest` | same | **REUSE, unchanged** | Must stay green through every prefactor commit |
| `PortGivensGuardTest` | `Architecture` | **EXTEND** | Gaps listed in the Slice E review |
| `RefinementWithoutSignInWireContractTest` (E2, E10, E13) | `Contracts/Refinement` | **EXTEND**, by `partial` files | One host, seeders per scenario already |
| `WireContractTest` | `Contracts` | **REUSE** | |
| `ServicePortHarness`, `FakeLighthouseClock`, `PortDoubles`, `FixtureSetupTimer` | `TestHelpers`, `TestDoubles` | **REUSE** | |
| `RealityCheckWireContractTest.PortFixtures` (B13) | `Contracts/Forecasting` | **EXTEND** | Add `ReportsAtThePortTest` in F; `ISizingLogCommands` lifetime is compared for Refinement from slice 2 |
| `ScriptedForecasts` (port) and `NeedForecastScript` (host) | | **KEEP both** | The host script's users (`Slice03`, `04`, `07`, `09`, `13`) stay hosted; fold when the last moves (not this Story) |
| `RefinementAcceptanceTest`, `RefinementNeedAcceptanceTest`, `SizingVotesAcceptanceTest` | host bases | **REUSE, unchanged** | Other classes still derive from them |
| `ReportsAcceptanceTest` (host) | `Reports` | **REUSE, unchanged** | 17 other classes |
| Reports port base | none exists | **CREATE_NEW** `ReportsAtThePortTest` (F only) | No candidate owns `IReportQueries`/`IReportCommands`; checked `ServicePortHarness` users |
| A Reports wire class | none | **not created** | F adds no wire row |

Semantic duplicates checked: the DTO readers (`NeedIn`, `CycleIn`) are private to `NeedOverOneCycleSpecifications` and again in the host base. Slice05 would add a third. **Decision:** the new readers go on the port base as protected members; `NeedOverOneCycleSpecifications` keeps its private ones until it is touched (no edit of a moved class here).

## Wave: DESIGN / [REF] Ratchet design

| Slice | Leaves `HostUsersStillToMove` | In which commit |
|---|---|---|
| 1 | line 169 `Slice10SleYardstickTest` | the move commit |
| 2 | line 165 `Slice05NeedAndVerdictTest` | the move commit |
| 3 | line 171 `Slice12CommentsTest` | the move commit |
| 4 (if approved) | line 186 `ReportWindowBoundaryScenarios` | the move commit |

The list never gains a line; the 7 host rows join an existing wire class that is not on it. Guard tightening, each with a planted case so it can fail:

| Guard | Gap (Slice E review) | Slice | Closing |
|---|---|---|---|
| `PortGivensGuardTest` | nested setters outside `Models.Refinement` (`StageRules.Ready.Mode`); `team.ToDoStates.Add`; `context.Set<Team>()`, `context.Add(team)`; no plant for `Update` or a nested set | 1 | Match any `set_` on a type reachable from `Team`; any `Add`/`Insert`/`Remove`/`Clear` on a `Team` collection getter; `LighthouseAppContext.Set<T>` for the guarded tables and `Add`/`Update` with a guarded argument; five more plants |
| E10 | one input at a time; only `RefinementSettings` compared; "No states" stores `[]` while Zenith stores none; `JsonSerializer` hides `[JsonIgnore]` members | 1 (SLE, Team fields, JSON), 3 (a combination case: every input together) | Also compare `ServiceLevelExpectationProbability/Range`, states and `ThroughputHistory` between `ATeam` and a save of the same form; compare by public-property dump; add the all-inputs case |
| E13 | walks only stubbed set-ups; `PremiumLicence`, `SignInOff` not compared | 2 | `MockBehavior.Strict` for the settings stand-in, so an unstubbed read throws at the port; add `SignInOff` against the host's `AuthModeResolver` answer on an instance without sign-in |
| `ScriptedForecasts` stamps `DateTime.UtcNow` | | 2 | Stamp from the port's clock |
| `GravitysStates` duplicated; `NewScope()` public | | 1; stays public for the planted classes (open question 3) | |

## Wave: DESIGN / [REF] Slices

At most 5; each is one nwdes slice with its own oracle file. Oracle = the moved class, running at the port with unchanged ids, plus the id comparison. Every slice: Debug fast loop `Lighthouse.Backend/Test-Fast.ps1` and the named classes; gates (`dotnet build`, the CLAUDE.md filter) once before each push.

| # | Delivers (plain words) | Commits (move separate from edit) | Oracle file | Size |
|---|---|---|---|---|
| 1 | The yardstick class runs without a web host; the builder takes over Work Item set-up; two guards get stricter | (a) `refactor(test-infra): move Work Item and finished-work Givens into RefinementPortGivens` (move; `NeedOverOneCycleTest` unchanged and green). (b) `test(refinement): the yardstick Givens, readers, E1, E4 and E10 on Team fields, PortGivensGuardTest` (edit). (c) `refactor(test-infra): move Slice10SleYardstickTest to the service port` (move: base class and steps, ratchet line, its 2 host rows to a partial of the wire class) | `Slice10SleYardstickScenarios.cs` (the class); wire rows in `RefinementWithoutSignInWireContractTest.Yardstick.cs` | 1 day |
| 2 | The need-and-verdict class runs without a host; voting works at the port | (a) `test(refinement): ISizingLogCommands and voter steps on the port base, B13, E13 strict` (edit). (b) move: `Slice05NeedAndVerdictTest` (ratchet line; the manual-forecast row to the wire class) | `Slice05NeedAndVerdictScenarios.cs`; wire row in `…WireContractTest.NeedParity.cs` | 1 day |
| 3 | The comments class runs without a host; the log and its errors are pinned on the wire | (a) edit: tab and log readers, E3, E5, E6, E7, E11, E10 all-inputs. (b) move: `Slice12CommentsTest` (ratchet line; 4 host rows to the wire class) | `Slice12CommentsScenarios.cs`; wire rows in `…WireContractTest.Log.cs` | 1 day |
| checkpoint | Stop and ask after slice 3 | measurement below | | |
| 4 | The report-window class runs without a host, on a new Reports port base | (a) edit: `ReportsAtThePortTest`, B13 case, a guard like `PortGivensGuardTest` only if the base has a builder rule. (b) move: `ReportWindowBoundaryScenarios` (ratchet line) | `ReportWindowBoundaryScenarios.cs` | 1 day, only if approved |

**Verification (literal argv, from ROOT, tracker variables unset by `env -u` as in the timing run).**

1. After each move: `dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests -c Debug --filter "FullyQualifiedName~<Class>"`. Expected: the row count of the table above, all passed (18/12/26/15 before; 17/10/22/15 at the port plus the host rows, counted in the wire class).
2. Id comparison: sorted ids before (`evidence/ids-*.txt`) against after; the only differences are the 7 mapped host rows and the new wire rows. Any other line fails the slice.
3. Guards: `dotnet test … --filter "FullyQualifiedName~TestHostUseGuardTest|FullyQualifiedName~PortGivensGuardTest|FullyQualifiedName~RefinementWithoutSignInWireContractTest|FullyQualifiedName~RealityCheckWireContractTest"`. Check the count; a filter that matches nothing still passes.
4. `Lighthouse.Backend/Test-Fast.ps1` (builds first).

**Deliberate breaks (each must turn the named row red; revert; `git diff` shows the file as it was).**

| Slice | Break in the working tree | Red row |
|---|---|---|
| 1 | `SleYardstickResolver`: the fallback window one day wider (read the constant first) | `Work_Items_finished_the_day_before_the_Throughput_window_opens_do_not_move_the_fallback` |
| 1 | `PortGivensGuardTest` plant: a port class calls `team.ToDoStates.Add` | the guard's planted row |
| 2 | `RefinementNeedCalculator` verdict: compare `>` for `>=` at the range's ends | `The_verdict_compares_the_ready_count_with_both_ends_of_the_range` |
| 2 | stand-in `GetTeamDataRefreshSettings` stubbed to 61 | E13 (existing) and a strict stand-in throws on an unstubbed read |
| 3 | `SizingLogEntry.LongestComment` 2000 to 1999 | `A_comment_of_two_thousand_characters_is_kept_whole` at the port, and the E7 wire row that sends exactly 2000 characters and expects them kept |
| 3 | `[JsonIgnore]` on `SizingLogLine.IsOpenQuestion` DTO member | E5 |
| 4 | `ReportWindowPolicy.MinimumLengthDays` 14 to 15 | `A_window_at_an_edge_is_answered_as_the_table_says(a Then of 14 days)` |

## Wave: DESIGN / [REF] Measurement plan

As T5 measured: Release, timer on (`LIGHTHOUSE_FIXTURE_TIMING=1`, `LIGHTHOUSE_FIXTURE_TIMING_OUT` absolute), the seven variables removed with `env -u` (`AzureDevOpsLighthouseIntegrationTestToken`, `JiraLighthouseIntegrationTestToken`, `JiraLighthouseRestrictedIntegrationTestToken`, `JiraScopedTokenIntegrationTestToken`, `ServiceNowLighthouseIntegrationTestToken`, `LinearAPIKey`, `AzureDevOpsLighthouseE2ETestToken`), `dotnet build-server shutdown` first, TRX each time, `trxsum.py`-style sum (TRX plus `one_time_setup_ms`), load noted.

- **Done at DESIGN (before):** each class alone, one round (table above).
- **Per slice:** one round of the moved class alone after the move, plus the wire class alone (the host rows' cost). Three rounds only at the checkpoint.
- **Checkpoint after slice 3:** 3 rounds for each of the 3 classes at the port and for the wire class; **one** full run with clause A (T5's `P2` clause) for the id list, host-build count and the full-run seconds. The unfiltered suite is not run otherwise. Compare with `P2`'s 624 host builds: expected 624 minus 3 (the three classes leave) and no new host build.
- **Payoff against the 14.1 s line.** Stage 3 with the corrected F:

| Term | Seconds |
|---|---|
| Already realised, `NeedOverOneCycleTest`, net of its 7 host rows (T5, measured) | 3.74 |
| E rest: 49 rows at T5's 0.19 s a row, `c` 0.7 ms, `h` 0 | 9.3 |
| 7 moved host rows and up to 9 new wire rows, 16 at 0.18 s | −2.9 |
| Host builds saved: 3 classes at 0.17 s | +0.5 |
| **E total, projected** | **about 10.6** |
| F: 15 rows at 0.19 s, no new wire row, one build saved | +3.0 |
| **E and F, projected** | **about 13.6** |

That is about 0.5 s **under** the 14.1 s line, not over it, on a projection; the earlier 17.4 s used 36 rows for F and is wrong by about 4.4 s. Treat the line as the stop heuristic it is. Not measured: `c` on these classes (expected 0.3 to 0.7 ms from `Slice04` and `NeedOverOneCycleTest`; `Slice05`'s 300-Work-Item row seeds more), and the cost of the new wire rows. The checkpoint fills both.

## Wave: DESIGN / [REF] Formal verification and algebra

Not applicable, with reasons. No production code, state, transition, protocol or concurrency changes. The only laws are the already-unit-tested rules the rows pin, and the id-preservation rule, which is a set equality checked by the id lists. Quint, TLA+ and Agda would model nothing new, so no model is written and no checker was run; none is claimed. The invariants that matter here are guarantee-style and are checked by running code: ids equal before and after; every removed assertion has a row; every guard has a plant that makes it red.

## Wave: DESIGN / [REF] Self-falsification

| Counterexample | What would happen | Where it is caught |
|---|---|---|
| A host assertion is lost: `ThenTheRangeIsTheManualForecast…` compares with the manual-forecast route. Moved to the port it would compare the engine with itself | The row cannot fail | It stays a wire row (D-6602-5), comparing the tab with `GET` of the forecast route; its break: use horizon 6 in the tab |
| `ThenTheCommentIsRefusedAndTheRowIsUntouched` asserts a status and a code | Both vanish at the port, where only `SizingOutcome` exists | E6 and E7 carry status and code; the port row reads the outcome and the untouched row |
| `The_log_of_a_Work_Item_that_is_not_in_refinement_is_not_found` (404) | `LogOf` returns null; no status at the port | Stays as a wire row |
| A seeded setting reads empty at the port | `TeamMetricsService` reads a default | E13, strict stand-in |
| The port clock starts on `FrozenNow`, not 7 October | Slice10's edge rows and Slice12's `recordedAt == 2026-10-07` fail or, worse, pass on a shifted window | D-6602-12; the window-edge rows are the proof; a break that shifts the clock by one day |
| Slice12 log order with a frozen clock | Entries share one instant; order could depend on the store | The host ran the same frozen clock and passed; the "oldest first" rows stay the check |
| A moved row keeps a stale host-only Given (a settings save) | A Given stores what no save produces | E10 plus the tightened `PortGivensGuardTest` |
| A class counted twice with T10 #6537 | T10 measures a class that moved | Order decided: T10's before-set is taken after slice 4's push (or after the checkpoint if F is dropped) |
| A guard that cannot fail | | Every tightened guard ships a plant; the deliberate-break table |
| 5-item Gravity swapped for the 7-item one | Slice10 `ThenTheSameWorkItemsAreListed` still green | D-6602-7; a row count check on the Given |
| The wire class grows past its budget | Per-class budget (about 34 s on CI, T5) | At most 23 rows, about 4 s plus the build; re-read at the checkpoint |

## Wave: DESIGN / [REF] Cross-cutting, answered

| Item | Answer |
|---|---|
| RBAC | N/A: no production or route change; the role-check rows stay in the classes that stay at the host. `IRbacAdministrationService` untouched |
| Lighthouse-Clients (CLI/MCP) | N/A: no API change; the client-reading rows (`Slice09`, `Slice17`) do not move |
| Website | N/A: no user-visible change |
| Usage data | N/A, because no feature becomes usable; no event |
| UI sketch | N/A: nothing a user sees |
| Terminology | N/A in code; this document uses the seeded defaults (Work Item, Feature, Team) |
| Context map (ADR-270) | The Refinement and Reports contexts only; no new concept, edge or event; no ADR-270 or `ARCHITECTURE.md` change. Slice 4 adds a second port base beside `ServicePortHarness`; nothing to record unless the maintainer wants it in ADR-237 |
| Loading / failure looks | N/A: no view |
| Mutation | N/A: no production file changes; stated at close-out. Backend rule 6: a moved class keeps `API.Integration`, so no Stryker config change |
| ADR-237 per-tier limits | The 5 s class limit read by the wire class is re-read at the checkpoint (not measured here) |
| Release notes | None (behaviour-free) |

## Wave: DESIGN / [REF] Open questions for the maintainer

| # | Question | Recommendation |
|---|---|---|
| 1 | Build the Reports port base for 15 rows (about 3 s gross, about 1 day)? | Decide at the checkpoint, on measured `c`. Lean yes: the base is the first of any later Reports move, and F adds no wire row, so it nets about +3 s |
| 2 | Put the 7 stay-at-host rows in the existing wire class (partial files), not a new host class? | Yes (D-6602-5): no new host build, a named guard reason |
| 3 | `NewScope()` stays public for the guard's plants? | Yes; the alternative is an `internal` seam and `InternalsVisibleTo` noise for one guard |
| 4 | Drop the rows of `Slice05` that duplicate `NeedOverOneCycleTest`? | No, not in a behaviour-free move; a later edit item |
| 5 | The 14.1 s line, now about 13.6 s projected with F and 10.6 s without | Treat as a heuristic; go on to slice 3, then ask |

## Wave: DESIGN / [REF] Architecture summary for the maintainer

**1. Shape, in hexagonal terms.**

Before: four test classes drive the application through the web host: HTTP in (`TeamController`, `RefinementVotesController`, `ReportsController`), then services, then a database per scenario. Each class builds its own host (1.6 to 1.8 s) and its Givens call the settings and blackout routes.

After (E): they call the driving ports directly, `IRefinementViewQuery` and `ISizingLogCommands`, over the real repositories and a database per scenario, and only the 7 rows that are about a route stay behind HTTP, as wire-contract rows in a class that already has a host.

```
 now:  test --HTTP--> controller --> service --> repository --> db     (host per class)
 after: test --------------------> service port --> repository --> db  (no host)
        wire rows --HTTP--> controller (one shared host, 7 + 9 rows)
```

| Piece | Status |
|---|---|
| Driving ports `IRefinementViewQuery`, `ISizingLogCommands` | unchanged (production); now used by tests |
| `RefinementAtThePortTest` | modified: votes, readers |
| `RefinementPortGivens` | modified: owns Work Item set-up |
| `PortGivensGuardTest`, E10, E13 | tightened |
| `RefinementWithoutSignInWireContractTest` | extended by 7 moved and up to 9 new rows (partial files) |
| `ReportsAtThePortTest` | new, only if F is approved |
| Driven adapters, trackers, production code | unchanged |

**2. Patterns weighed.**

| Pattern | Chosen / rejected | Why |
|---|---|---|
| Ports and adapters (drive the port, not the route) | Chosen | The rule is proven where it lives; set-up fell from about 115 ms to under 1 ms a row at the port |
| One builder for Team set-up (Test Data Builder) | Chosen | The single writer that E10 checks against the save |
| Wire contract on one shared host | Chosen for the 7 rows | They are about routes, headers or the settings save |
| Strict mock for the settings stand-in | Chosen | An unstubbed read fails loud instead of reading a default |
| Real seeders in every port scenario | Rejected (D-6602-4) | About 36 ms a row |
| A new host class for the stay rows | Rejected | One more host build for 7 rows |
| One shared Reports builder now | Rejected | One class uses it; build it when a second does |
| Deleting overlapping rows | Rejected | Ids must survive a move |

## Wave: DESIGN / [REF] Maintainer review (2026-11-12)

The slices are approved as designed.

| Open question | Answer |
|---|---|
| 1 Reports port base for slice 4 | Decided at the checkpoint after slice 3, on the measured saving (unchanged) |
| 2 The 7 stay-rows go into `RefinementWithoutSignInWireContractTest` | **Yes** |
| 3 `NewScope()` stays public | Explained, answer pending. Correction to the design's reason: `ServicePortHarness` and the planted classes are in the same test assembly, so `internal` would need no `InternalsVisibleTo` and would protect nothing. What stops a port class from writing a Team itself is `PortGivensGuardTest`, not the method's visibility |
| 4 Drop the `Slice05` rows that repeat `NeedOverOneCycleTest` | **Yes, after the move**, as its own edit: a fifth slice after slice 4 (or after the checkpoint, if slice 4 is not approved). Each dropped row is mapped to the `NeedOverOneCycleTest` row that proves the same rule with the same inputs, and the id comparison shows exactly the dropped ids. It is never folded into a move commit |
| 5 The 14.1 s line is a stop heuristic; continue to slice 3, then ask | **Yes** |

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-12)

- **Platform risk:** none. Test code only; no build, CI workflow, deployment or configuration change. The moved classes run in the same `dotnet test` step under the same filter, and lose their web host.
- **CI time:** the backend Integration tier should fall by about 10.6 s summed for slices 1 to 3 (projected). ADR-237's per-class budget is read for the grown `RefinementWithoutSignInWireContractTest` at the checkpoint.
- **Usage-data event:** N/A, because the Story is behaviour-free: no user sees or does anything new.
- **Rollback:** revert the slice's commits; each class still has its host version in history, and `HostUsersStillToMove` gets its line back only through that revert.
- **T10 #6537:** its before set starts after this Story's last move push. Tell T10 when that push lands.

## Wave: DELIVER / [REF] Maintainer decisions at DELIVER start (2026-11-12)

- **Open question 3:** `NewScope()` stays public. `PortGivensGuardTest` is what stops a port class from writing a Team itself; visibility inside one test assembly protects nothing.
- **UI sketches:** N/A, because nothing a user sees changes.
- **Rigor profile per slice** (model passed on every dispatch; EXAMINE uses the reviewer model):

| Slice | Profile | Crafter, acceptance designer | Reviewers |
|---|---|---|---|
| 1 yardstick, builder, guards | `thorough` | `opus` | `sonnet` |
| 2 need-and-verdict, strict stand-in | `thorough` | `opus` | `sonnet` |
| 3 comments, wire rows | `standard` | `sonnet` | `haiku` |
| 4 report window (if approved at the checkpoint) | `standard` | `sonnet` | `haiku` |
| 5 drop the rows that repeat `NeedOverOneCycleTest` | `thorough` | `opus` | `sonnet` |

- **nwdes features:** two, split at the checkpoint, because slices 4 and 5 depend on its answer. **Feature A** `story-6602-a-refinement-at-the-port`: slices 1 to 3, then its whole-candidate review and EXAMINE, then the checkpoint measurement and the question. **Feature B** `story-6602-b-report-window-and-overlap`: slices 4 and 5, opened only after the checkpoint answer. The whole-candidate review of A runs at `sonnet` (the strictest profile in A).

## Wave: DELIVER / [REF] Findings carried to feature B (2026-11-12)

- **From the S2 slice review (medium, duplication):** `Slice05NeedAndVerdictSpecifications` and `NeedOverOneCycleSpecifications` both derive from `RefinementAtThePortTest` and carry the same Gravity ready-rule builder, the tab reader, three constants and the five-to-eight script; `NeedOverOneCycleSpecifications` keeps its own `NeedIn` / `NeedReading` beside the base's `TheNeedOn` / `NeedFacts`. Fixing it in feature A edits the approved S2 oracle and a class outside feature A. It goes to **slice 5** (feature B), which already reworks `Slice05` against `NeedOverOneCycleTest`: hoist the shared members into the port base or the builder, and make `NeedOverOneCycleSpecifications` use `NeedFacts` and `TheNeedOn`.
- **From the S1 slice review (low, stale comment):** `Slice10SleYardstickSpecifications.cs` line 84 narrates the old host design. It is in the approved S1 oracle; reword it in feature B's first slice that touches the file, or at the finalize step.
- **The settings-save case with every input together (slice 3), root decision in AFK mode, 2026-11-12:** `EveryInput` holds inputs that set the same field (`RefinesInBacklog` and `RefinesInNoState` both set the states), so "every input together" cannot be built as written. The case combines one input per field it changes: the states, the cadence, the stage rules, the SLE and the Throughput history each taken once, so a save-side tidy that depends on a combination of fields is seen. Which input stands for a field is the crafter's choice and is named in the test. Revisit if the maintainer wants every pair instead.
