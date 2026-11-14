# Story 6537 (T10): step 5, stop and ask

Written 2026-11-14. T10 has moved two suites and now stops. It takes no third suite and makes no further move commit until you reply and the reply is recorded in `feature-delta.md` (scenario 5.1).

**Labels used below.** Every figure is marked **measured** (read from CI CSVs or from a timed local run) or **predicted** (arithmetic on measured class seconds). "CI seconds" are the median over the 10 ranking runs of step 1 (2026-11-12 and 2026-11-13). Nothing here is a measured fall on CI yet: the after set does not exist (see "What step 6 needs").

**Words.** "Class" means a test class under `API/Integration`. The names `Slice05`, `Slice05b` and so on are the test classes' own names, not nWave slices. "Work package" means one nwdes slice of a suite (S1 to S4 in suite 1, S1 to S5 in suite 2). "Moved" means a host row left the web-host test and a unit test (or a service-port scenario) now proves the same rule. "Tier" means the summed `Integration` seconds of one CI run.

## 1. What moved, per suite

### Suite 1: the `PercentilesOverTime` family (2026-11-13 to 2026-11-14)

44 host rows moved, 5 kept. No production file changed. All four work packages are pushed with green CI. Two classes (`Slice08`, `Slice10`) left the host entirely and were deleted.

| Class | Rows | Moved | Kept | CI seconds before (measured) | Predicted CI fall | How it was predicted |
|---|---|---|---|---|---|---|
| `Slice07WhereTheLimitsActuallyMovedTest` | 10 | 9 | 1 (limits route) | 88.3 | about 74 s | Timed locally: 33.2 s to about 5.4 s (measured, minus 27.8 s, 84 %). Scaled by CI's 2.66 times slowdown on this class (predicted) |
| `Slice06EveryPercentileTabSpansTheSamePeriodTest` | 11 | 10 | 1 (tab route) | 48.6 | about 37 s | Not timed. 48.6 x 10/11 x the pilot's 84 % (projection) |
| `Slice05ReconstructCycleTimeHistoryTest` | 19 | 16 | 3 (read route, read-fill-read loop, refresh mid-fill) | 89.7 | about 46 s, up to about 63 s | Timed locally: 24.6 s to 7.2 s (measured, minus 17.4 s, 71 %). 46 s uses the 2.66 factor; 63 s uses this class's own CI-to-local ratio (89.7 / 24.6 = 3.6). My arithmetic on the two ratios |
| `Slice10OnlyWhatTheTeamStillKeepsTest` | 4 | 4 | 0 (class deleted) | not in the sources (at most 14.8 s) | not in the sources | Ranking ends at 14.8 s, so each class is at most that |
| `Slice08NothingToShowTest` | 5 | 5 | 0 (class deleted) | not in the sources (at most 14.8 s) | not in the sources | Same. `Slice08` and `Slice10` together are at most 37.2 s (family total less the other classes) |
| **Suite 1, the three timed or projected classes** | 40 | 35 | 5 | 226.6 | **about 157 s (predicted)**, 7.7 % of the tier | 74 + 46 + 37 = 157. Without the two deleted classes, whose figure is not in the sources |

### Suite 2: the `ConfigureOncePerConnection` family (2026-11-14)

128 host rows moved, 35 kept, of 163. No production file changed. Every CI run was green. One class (`Slice09`) left the host entirely. The other 12 keep at least one host row.

| Work package | Classes | Rows moved |
|---|---|---|
| S1 (`bac2135c1`) | `Slice05b`, `Slice05a` | 41 |
| S2 (`d7326c838`) | `Slice02`, `Slice01` | 16 |
| S3 (`9eccd3123`) | `Slice04`, `Slice03` | 40 |
| S4 (`4b4991176`) | `Slice12`, `Slice09`, `Slice10`, `Slice11` | 25 |
| S5 (`f2a2dd345`) | `Slice06`, `Slice08` | 6 |
| **Total** | | **128** |

By class. Only five classes have a measured CI time. The other eight were not in the ranking's top 30, so all that is known is "at most 14.8 s each, 78.2 s together over 80 rows".

| Class | Rows | Moved | Kept | CI seconds before | Predicted CI fall (class seconds x moved share) |
|---|---|---|---|---|---|
| `Slice05bReferencedFieldsAndOlderPayloadsScenarios` | 32 | 28 | 4 | 19.6 (measured) | 17.2 s |
| `Slice02NamedLinkThatMatchedNothingScenarios` | 7 | 6 | 1 | 19.1 (measured) | 16.4 s (the two slowest rows both left, so this may be low) |
| `Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios` | 19 | 17 | 2 | 16.5 (measured) | 14.8 s |
| `Slice12EstimationDefaultScenarios` | 20 | 16 | 4 | 15.8 (measured) | 12.6 s (the screen assumed 18 rows would move; 16 did) |
| `Slice06DefaultChangeUpdatesTheFollowersScenarios` | 5 | 3 | 2 | 15.2 (measured) | 9.1 s (the screen says the share undercounts this one) |
| **Five measured classes** | 83 | 70 | 13 | 86.2 | **70.0 s (predicted)**, 3.4 % of the tier |
| `Slice01JiraDependenciesFromANamedLinkScenarios` | 13 | 10 | 3 | not in the sources (at most 14.8) | at most 11.4 s |
| `Slice03AzureDevOpsRelationTypeAcceptedScenarios` | 24 | 23 | 1 | not in the sources (at most 14.8) | at most 14.2 s |
| `Slice05aTeamsParentOverrideDefaultScenarios` | 18 | 13 | 5 | not in the sources (at most 14.8) | at most 10.7 s |
| `Slice08PortfoliosParentOverrideDefaultScenarios` | 9 | 3 | 6 | not in the sources (at most 14.8) | at most 4.9 s (the screen planned 8 rows; 3 moved) |
| `Slice09PortfoliosDependencyFieldDefaultScenarios` | 3 | 3 | 0 (class gone) | not in the sources (at most 14.8) | at most 14.8 s |
| `Slice10PortfoliosSizeEstimateFieldDefaultScenarios` | 5 | 3 | 2 | not in the sources (at most 14.8) | at most 8.9 s |
| `Slice11PortfoliosFeatureOwnerFieldDefaultScenarios` | 6 | 3 | 3 | not in the sources (at most 14.8) | at most 7.4 s |
| `ClockIsolationScenarios` | 2 | 0 | 2 | not in the sources (at most 14.8) | none (not rule-shaped) |
| **Eight unmeasured classes** | 80 | 58 | 22 | 78.2 in total (upper bound, not a measurement) | at most 56.7 s if each class's seconds are spread evenly over its rows (78.2 x 58/80; an assumption the measured classes show is false, 0.61 to 3.0 s a row). The lower end is not in the sources |
| **Suite 2, all classes** | 163 | 128 | 35 | 164.4 (family total, measured) | **predicted 70.0 s from the measured classes, at most about 127 s with the unmeasured ones** |

Both suite 2 figures are projections. The screen warns they are too high in one way: the host build and set-up of the 12 classes that still keep a row are not subtracted.

## 2. Days against the estimate

DESIGN estimated 0.25 (step 1) + 0.75 (step 2, ranking and pilot) + 1 (step 3, rest of suite 1) + 1 (step 4, suite 2) = 3 days, 3.25 with step 6. Working-day effort was not recorded. The table gives calendar time from the commit log (`git log --grep=6537`), all in AFK mode, so it includes waits for reviews and the overnight hold.

| Step | Estimate | What the log shows |
|---|---|---|
| Suite 1 (steps 1 to 3) | 2.0 days | 2026-11-13 14:19 (decisions) to 2026-11-14 09:54 (suite 1 delivered): about 19.5 h elapsed over two calendar dates. Step 1 and the pilot were done by 2026-11-13 15:20 (pilot measured). It then needed a rejected whole-candidate review, a revision, and a re-approval of four oracles. Suite 1 took three pushes against DESIGN's plan of three pushes in total |
| Suite 2 (step 4) | 1.0 day | 2026-11-14 10:58 (screen) to 19:29 (EXAMINE closed): about 8.5 h elapsed, one calendar date. One rejected whole-candidate review (the scope check compared HEAD with itself after a push), fixed and accepted. The push count for suite 2 is not in the sources |
| Both | 3.0 days | Two calendar dates, 2026-11-13 and 2026-11-14 |

On calendar time, both suites came in under the estimate. Two things took more effort than planned: the oracle reviews (each work package was rejected once or twice on a missing break or a missing pin) and the post-push scope check.

## 3. Classes not moved, and why

**Suite 1 family (`PercentilesOverTime`, 12 classes, 111 rows).** Moved: five classes, 44 rows.

| Class | Rows | Why it stays |
|---|---|---|
| `Slice01`, `Slice02`, `Slice03`, `Slice03b`, `Slice04` | 3, 5, 12, 22, 15 (57) | Different base, no history fill, reads of stored snapshots and wire rows. Not screened row by row; the screen expects mostly wire. Their seconds are not in the sources |
| `Slice09TheFillShipsOptInTest` | 3 | No fill in any row. Row 3 must stay (it needs the real host's command interceptor), so only two seed-and-refresh bodies would leave, and two new handler rows would be needed first. Judged not worth it |
| `Story6084TheFillRunsEverywhereTest` | 2 | 18.3 s CI, the heaviest per row. Both rows stay: one is a stored setting flowing through the host, the other a held pass on its own thread. A cheaper Given (61 days of history, not 121) is an edit, not a move, and is left to you |
| Kept rows in moved classes | 5 | `Slice07` limits route, `Slice06` tab route, `Slice05` read route, read-fill-read loop and refresh mid-fill |

**Suite 2 family (`ConfigureOncePerConnection`, 13 classes, 163 rows).** 35 rows stay on the host, in four groups: one end-to-end row per flow, real-store concurrency rows, wire rows (RBAC, status and code of a refusal, the validate route), and rows whose rule owner was not found. The rows kept against the screen's plan, because no unit row proves them:

| Rows | What no unit row proves |
|---|---|
| `Slice12` rows 1 and 16 | Row 1: the claim that an Estimation default queues nothing (no production line decides it, so no break is possible). Row 16: `TeamController` refuses a move before it saves the Work Items; only the host sees that order **Moved the same day by the follow-up** (see the S4 and S5 id tables, "Follow-up") |
| `Slice08` rows 2, 4, 5-6 and 7 | The controller's save gate (`.Where(defaults.Sends)`) and the `newFields` lookup that sets `DefaultsAddedInThisSave` are controller logic with no port. Where a Portfolio's item hangs has no unit row either (the Team side has one) **Moved the same day by the follow-up** (see the S4 and S5 id tables, "Follow-up") |
| `Slice06` row 3, `Slice10` rows 3-4, `Slice11` rows 3-5 | The owner of the rule (Full or Delta by fetch fingerprint; Feature size and owner from the default field) was not located |
| `ClockIsolation` (2 rows) | They guard the base class's clock reset; nothing is rule-shaped |

**Not touched by either suite (left out in step 1, per the ranking).**

| Class or family | Why it was out |
|---|---|
| `Integration.Containers.*` (40 classes, 469.8 s) | They prove a database provider, not a rule |
| `ConnectionFieldChange.Slice01FieldChangeUpdatesItsOwnersTest` (40.8 s, 13 rows) | Screened: update queue and Task Manager, nine stay, four are rewrite candidates; a small saving |
| `FasterUpdates.Slice05FetchFingerprintTest` (39.5 s, 47 rows) | Screened: about 42 rows could move, but two `WorkItemService` unit rows and one `Contracts` row are needed first; short rows, so the saving is only host and queue overhead |

## 4. The 5.2 checks

**The 1 % line.** 1 % of the 10 ranking runs' median tier (2,031.7 s) is **20.3 s**. The rebuilt before set's own median (and so the final line) is not in the sources.

| Suite | Predicted fall | Against the line |
|---|---|---|
| Suite 1 | about 157 s (three classes only) | Above, about 7.7 times |
| Suite 2 | 70.0 s (five measured classes only), at most about 127 s | Above, about 3.4 times, even if the eight unmeasured classes saved nothing |
| The pair | about 227 s | 11.2 % of the tier, above the 5.0 % target `P` (101.6 s) |

**No suite is under the 1 % line**, so the stop rule of 5.2 on that side does not fire. This is a prediction and not a CI measurement. Suite 1's figure leans on a local timing of two classes and a 2.66 times scaling.

**The predicted `Integration` mean against its ceiling.** Current ledger entries in `Scripts/test-timings/test-time-budget.json`: mean ceiling **0.81 s**, total ceiling **2,780 s** (set at 3,353 tests), fast-test limit 20.0 s. Hand check, my arithmetic, not a recorded one:

- Before: 2,031.7 s over 3,322 rows (the newest ranking run's count) = about 0.61 s a row. The largest single-run mean in the ranking set was 0.680 s.
- Removed: 172 rows (44 + 128). Removed seconds, from the projections above without the nine rows of suite 1's two deleted classes: about 269 s (suite 1 by share: 79.5 + 44.2 + 75.5; suite 2: 70.0) up to about 326 s (with the unmeasured suite 2 classes at their upper bound).
- After: (2,031.7 - 269 to 326) / (3,322 - 172) = **about 0.54 to 0.56 s**, against the 0.81 ceiling. It **falls**, because the moved rows were dear (about 1.0 s a row in the measured suite 2 classes, more in suite 1), not cheap as DESIGN assumed. Total falls to about 1,700 to 1,760 s against 2,780 s.
- So no mean breach is predicted. The mean ceiling was not raised and none is asked for.
- **Not in the sources:** a record of the hand check before each of the pushes (DESIGN item 7 asks for it). The pushes were gated by the full backend run and a path classifier. This note gives the check after the fact.
- **Provisional ceilings.** T9 closed with the backend ceilings provisional (commit `10220a0f7`). DESIGN says the Story does not close while a limit it lowered is provisional. Whether the entries are still provisional is not in the sources.

## 5. What step 6 needs

1. **Five eligible plain `main` runs** whose head is after `15d0e986f` (the last move commit, the follow-up) and before the data commit. The push of `15d0e986f` starts the first one, if it is eligible. Eligible means: test step passed, no `Failed` row, every connector answer `false`, not force-full. Up to four of the five depend on other Stories' pushes. T10 makes no synthetic push, and the data commit waits. **No run after `15d0e986f` has been read yet.** The after set is not in the sources.
2. **The before set.** Only 2 clean runs existed at the first move push (`b8c425ce6`, `61cda4e8e`). You decided on 2026-11-13 to rebuild it from those 2 plus older plain runs, with the classes Story 6602 moved removed by id. The rebuilt table is not in the workspace evidence (`evidence/step1/` holds only the 10-run ranking). It must be written before the success reading.
3. **The success reading**, with all three conditions together: after median below before median; by-class fall larger than the range of the moved ids' own seconds across the before runs; fall reaches `P` = 5.0 %. The tier before spread was 1.62 over the 10 ranking runs, far larger than T4's 1.13, so a tier median alone may sit inside noise (scenario 6.2). The by-class fall is the proof that counts. Per-id seconds come from the CSVs and the id tables; the CSVs are not stored in the workspace.
4. **Your ruling on the ceilings** (DESIGN questions 2, 4, 5), **before** the data commit. My recommendation: the formula is likely to lower both mean and total this time (section 4), so write what it gives where it is lower than the entry, and write nothing where it is not. Both are in the rules already; the ruling confirms them.
5. **Budget entries by hand.** For a class that only shrank, its allowlist entry drops when its largest time over 5 plain runs times 2.1 is at most 34 s. Nothing in the build asks for it. Whether this is done for the classes of both suites is not in the sources.
6. **Data commit:** JSON only, with a reason naming the moved suites and the count at derivation, alone in a clean window.

## 6. Follow-up candidates

**Unit rows for the host rows kept in suite 2** (each would let a host row go):
- Controller unit rows for the save gate (`.Where(defaults.Sends)`) and the `newFields` lookup that sets `DefaultsAddedInThisSave` (`Slice08` rows 5-6 and 7). Done in the follow-up of 2026-11-14.
- A unit test for where a Portfolio's item hangs (`Slice08` rows 2 and 4). Done in the follow-up of 2026-11-14.
- The proof that an Estimation default queues nothing (`Slice12` row 1). Done in the follow-up of 2026-11-14.
- The refused-move ordering in `TeamController` (`Slice12` row 16). Done in the follow-up of 2026-11-14.
- Rule owners for the Full-versus-Delta choice (`Slice06` row 3) and the Feature size and owner rules (`Slice10` rows 3-4, `Slice11` rows 3-5).
- The wire and real-store rows could share one `Contracts` class on `WireContractTest` instead of seven host fixtures. The screen warns a merge can be slower and says to measure it.

**Class-level `epic-6303` categories** left on `Slice05a`, `Slice05b`, `Slice10` and `Slice11` of suite 2. The sources do not say which category value.

**Suite 1 left-overs:** `Slice09TheFillShipsOptIn` and `Story6084TheFillRunsEverywhere` (smaller Given only), and the five wire-shaped `PercentilesOverTime` classes (`Slice01` to `Slice04`, `Slice03b`) if you want them screened.

**Third-suite candidates from the ranking** (families, CI seconds over 10 runs, classes, rows). Only the two named classes were screened; all else is a guess from the ranking.

| Family | Seconds | Classes | Rows | Share of tier |
|---|---|---|---|---|
| `FasterUpdates` | 74.0 | 6 | 88 | 3.6 % |
| `Refinement` | 73.7 | 15 | 222 | 3.6 % |
| `Reports` | 69.9 | 16 | 172 | 3.4 % |
| `SkipUnchangedForecasts` | 61.6 | 6 | 37 | 3.0 % |
| `ConnectionFieldChange` | 54.0 | 3 | 32 | 2.7 % |
| `FastWithManyFeatures` | 52.9 | 8 | 60 | 2.6 % |

`Refinement` and `Reports` are already smaller than step 1 shows: Story 6602 moved classes from both before the ranking, and the ranking only counts what is still `Integration`. The delta's own plan is a separate backend Story for `FasterUpdates`, `Refinement`, `Reports` and `FetchFingerprint`, plus a frontend Story to move rule rows from `.test.tsx` down to `.test.ts` (your decision: a separate topic).

## 7. The question

**Context.** Both suites are moved and pushed. Every suite is predicted above the 1 % line. The measured fall waits for five CI runs after `15d0e986f`; by the maintainer's reply it is taken under #6647.

**Recommendation.** Do not start a third suite inside T10. Let the after set build, make the success reading, then rule on the ceilings. Take the third suite as the separate backend Story above.

**Options.**
1. **Wait for the after set, then step 6.** Safe. Delay: up to four other pushes by others.
2. **Start a third suite now** (for example `FasterUpdates`, about 74 s). More saving. It needs a screen first, and it moves test ids under the five after runs, so the success reading is harder to defend.
3. **Close now without the data commit.** The Story would pass on predicted figures only, and scenario 5.3 and 6.1 forbid calling that passed.

**Question.** Do you choose option 1, and do you confirm the ceiling approach in step 5.4?

## 8. After this note was written (2026-11-14)

- The follow-up moved the seven host rows listed in section 3 for `Slice08` and `Slice12` down to unit rows. Each one has a
  deliberate break that turns it red (`story-6537-b-configure-suite/evidence/followup_breaks.py`). Suite 2 now moves 135 host
  rows, and the two suites together move 179. This was done directly, without an nwdes review.
- Two standing Stories hold the next offenders on each stack, tagged Continuous Improvement: #6647 (backend host tests) and
  #6648 (frontend page and dialog tests).
