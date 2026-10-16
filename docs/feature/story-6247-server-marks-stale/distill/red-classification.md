# RED classification — story-6247-server-marks-stale (DISTILL, 2026-10-16)

Every pending test was un-skipped locally, run once against the scaffolds and today's code, and the
skip markers were then put back. Nothing un-skipped was committed.

Classes: `MISSING_FUNCTIONALITY` (correct RED: the assertion fires because the behaviour is not built),
`GREEN_ON_TODAY` (passes already; kept as a guard, not a RED step), `BROKEN` (would block hand-off — none).

## Backend (`dotnet test … --filter "TestCategory=story-6247"`, all `[Ignore]` removed)

The scaffold's `StalenessPolicy.Evaluate` throws `InvalidOperationException("Not yet implemented -- RED
scaffold (thresholds a/b)")`. C# has no `AssertionError`; NUnit reports the exception as a test failure
reached inside the act step, after every precondition was seeded, which is the RED the gate wants. Nothing
calls `Evaluate` from production yet, so the HTTP and refresh scenarios fail on their assertions instead.

| Test | Result | Class | Evidence |
|---|---|---|---|
| `StalenessPolicyTest` — all 22 cases | Failed | MISSING_FUNCTIONALITY | `InvalidOperationException: Not yet implemented -- RED scaffold` from `Evaluate` |
| WS `A_flow_coach_reads_which_Work_Item_has_sat_too_long_in_Review_and_why` | Failed | MISSING_FUNCTIONALITY | `isStale` expected `True`, was `null`; reasons expected 1, got 0 — the item is on the read, the fields are not |
| `A_Work_Item_goes_stale_once_…_longer_than_the_threshold(5,False)` / `(6,True)` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null` |
| `A_Blocked_Work_Item_goes_stale_on_the_day_it_reaches_the_blocked_threshold(2,False)` / `(3,True)` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null` |
| `A_Blocked_Work_Item_under_the_blocked_threshold_is_not_stale_however_long…` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null`; context reason missing |
| `A_Blocked_Work_Item_is_stale_for_its_blocked_time_and_still_shows…` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null`; 2 reasons expected, 0 |
| `A_Work_Item_still_Blocked_in_its_state_is_not_stale_before_the_blocked_threshold…` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null` |
| `With_both_thresholds_off_…`, `Turning_the_time_in_state_trigger_off_…`, `Turning_the_blocked_trigger_off_…` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null` |
| `A_Work_Item_is_judged_as_of_the_day_the_read_asks_about_not_today` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null` on both reads |
| `A_Work_Item_whose_state_entry_Lighthouse_never_saw_is_not_stale` | Failed | MISSING_FUNCTIONALITY | `isStale` expected `False`, was `null` |
| `A_Feature_goes_stale_once_…_Portfolios_threshold(5,False)` / `(6,True)` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null` on the Feature |
| `A_Blocked_Feature_is_stale_for_its_blocked_time_against_the_Portfolios_blocked_threshold` | Failed | MISSING_FUNCTIONALITY | `isStale` was `null`; 2 reasons expected, 0 |
| `A_refresh_does_not_signal_a_Blocked_Work_Item_still_under_the_blocked_threshold` | Failed | MISSING_FUNCTIONALITY | one `WorkItemBecameStale(1, 5)` raised; expected none — today's rule has no Blocked precedence |
| `A_refresh_signals_a_Blocked_Work_Item_with_the_blocked_threshold_it_crossed` | Failed | MISSING_FUNCTIONALITY | raised with `ThresholdDays = 0`, expected 3 — today's rule has no blocked trigger |
| `A_refresh_never_signals_staleness_while_both_thresholds_are_off` | Failed | MISSING_FUNCTIONALITY | two signals with `ThresholdDays = 0` — today's rule does not treat 0 as off |
| `A_refresh_signals_a_Work_Item_going_stale_once_with_the_threshold_it_crossed` | Passed | GREEN_ON_TODAY (time-dependent) | Today's rule measures fractional days from `UtcNow`: the seeded entry is noon five calendar days back, so it passes after 10:00 UTC and fails before. Kept pending; the new rule makes it deterministic |
| `A_refresh_does_not_signal_a_Work_Item_that_has_sat_exactly_as_long_as_the_threshold` | Passed | GREEN_ON_TODAY | under 5 fractional days today too; kept pending as the boundary partner of the one above |

Active at hand-off (not pending, green): `The_Blocked_items_read_says_nothing_about_staleness`,
`A_WIP_read_without_a_day_answers_with_no_Work_Items`, and the two `StalenessPolicyPurityArchUnitTest`
rules. They hold today and must keep holding, so they guard from the first DELIVER step.

## Frontend (`vitest run` on the four files, every `.skip` removed)

| Test | Result | Class | Evidence |
|---|---|---|---|
| Badge: `is red when Lighthouse marks the Work Item stale, even with no threshold given` | Failed | MISSING_FUNCTIONALITY | no `time-in-state-stale` element: the badge still works staleness out from thresholds it was not given |
| Badge: `its tooltip gives Lighthouse's reason: …` (3 cases) | Failed | MISSING_FUNCTIONALITY | same: no stale badge, so no tooltip |
| Badge: not red when Lighthouse says not stale / does not say / Blocked not yet stale | Passed | GREEN_ON_TODAY | no threshold given, so today's badge is not red either; guards for after the switch |
| Aging chart: `reds the dot of a Work Item Lighthouse marks stale, however recently it moved` | Failed | MISSING_FUNCTIONALITY | dot colour is not `errorColor` |
| Aging chart: `does not red the dot of a Work Item Lighthouse says is not stale…` | Passed | GREEN_ON_TODAY | guard |
| Aging chart: `does not red the dot of a Work Item an older Lighthouse said nothing about` | not run un-skipped (added after the run) | GREEN_ON_TODAY by construction | no threshold given and no answer, so neither today's chart nor the switched one reds it |
| Flow Overview: `the Team's Stale count is the number of Work Items Lighthouse marks stale` | Failed | MISSING_FUNCTIONALITY | count `2` (the browser's rule: the two month-old items), expected `3` (Lighthouse's) |
| Flow Overview: `the Portfolio's Stale count …` | Failed | MISSING_FUNCTIONALITY | count `2`, expected `3` |
| Aging health: `counts a Work Item Lighthouse marks stale as stale` | Failed | MISSING_FUNCTIONALITY | RAG `amber`, expected `red` |
| Aging health: `does not call stale a Work Item Lighthouse says is not stale…` | Failed | MISSING_FUNCTIONALITY | RAG `red`, expected `amber` — the past-range defect: the browser counts the month to today |
| Guard: `the browser's copy of the rule is gone` | Failed | MISSING_FUNCTIONALITY | 4 shipped files still name `deriveStaleness` |
| Guard: `nothing in the app compares days with a staleness threshold` | Failed | MISSING_FUNCTIONALITY | 1 file: `utils/staleness/deriveStaleness.ts` |

The guard's nine pattern cases (what it must catch, what it must let through) are active and green.

## Setup defects found and fixed during classification

- The two aging-health tests first failed with "Unable to find `widget-rag-aging`": the view tells a
  Team's metrics service from a Portfolio's by whether it has `getFeaturesInProgress`, and the helper gave
  the Portfolio one too. Fixed in the helper; both now fail on the RAG value.
- The Stale-count mix first passed on today's code by coincidence (both rules counted 2). A third
  Lighthouse-stale item makes the two rules disagree, 3 against 2.

Verdict: 0 BROKEN. DELIVER may start.
