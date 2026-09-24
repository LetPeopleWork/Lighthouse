# RED classification: optional-feature-toggled-usage-event

Each scenario was un-ignored or un-skipped **by itself** (one build and one run per scenario), run
against unmodified production code, and then ignored or skipped again. Run on 2026-09-24 in the
`eager-moseying-alpaca` worktree. The scenarios reworked after U1 was resolved as (b) were run again
the same way: the backend "setting this list does not have" set, which now includes
`NeverSendUsageData`, and the frontend veto spec, which now expects nothing to be reported.

Classes: `MISSING_FUNCTIONALITY` means the correct RED: the assertion fires because the behaviour does not exist yet.
`BROKEN` means a setup, import or build failure. There are none.

## Backend: `Lighthouse.Backend.Tests/Integration/UsageData/OptionalFeatureToggledEventTests.cs`

All 28 cases reach their assertions. Before DELIVER, a name the list does not have fails to bind, so every
`OptionalFeatureToggled` message gets a 400. That refusal is the missing behaviour, not a fault in the harness.
Messages under the other ten names still get a 204 and still reach the collector.

| Scenario | Cases | Class | Fails on |
|---|---|---|---|
| Switching_the_feature_order_setting_on_arrives_saying_which_setting_and_that_it_is_now_on | 1 | MISSING_FUNCTIONALITY | expected 204, got 400; 0 messages at the collector, expected 1 |
| Switching_it_back_off_arrives_as_one_more_event_saying_it_is_now_off | 1 | MISSING_FUNCTIONALITY | the switch-on control sent 0 messages; the switch-off got 400 and 0 messages |
| Nothing_leaves_a_browser_that_did_not_agree_when_it_switches_a_setting | 3 (refused / never answered / withdrew) | MISSING_FUNCTIONALITY | expected 204, got 400 on all three |
| A_setting_switch_missing_part_of_what_it_says_is_refused | 3 | MISSING_FUNCTIONALITY | the partial message is refused (400) and nothing is sent, but the **complete** control message is also refused (400, expected 204) |
| A_setting_this_list_does_not_have_is_refused | 4 (NeverSendUsageData / DeltaSync / FeatureOrdering / UsageData) | MISSING_FUNCTIONALITY | the refusal and empty-collector legs hold; the complete control message got 400, expected 204. Re-run after the rework. |
| A_setting_switch_that_says_something_other_than_on_or_off_is_refused | 3 (`"true"` / `1` / `null`) | MISSING_FUNCTIONALITY | the complete control message got 400, expected 204 |
| Any_other_event_that_says_a_setting_was_switched_is_refused | 10 (every other event name) | MISSING_FUNCTIONALITY | expected 400, got 204, and the event **reached the collector**: the reader ignores fields it does not know |
| Any_other_event_carrying_either_half_of_a_setting_switch_is_refused | 2 | MISSING_FUNCTIONALITY | expected 400, got 204, and the event reached the collector |
| Nothing_travels_with_a_setting_switch_beyond_which_setting_and_which_way | 1 | MISSING_FUNCTIONALITY | 0 messages at the collector, expected 1 |

Removed after the rework: `Lifting_the_administrators_veto_arrives_saying_so` and
`Switching_the_veto_on_is_never_reported`. The veto is no longer on the event's list, and the refusal
above covers it on the wire.

Each run logged `SQLite Error 1: 'no such table: Features'` at host start-up. That is background start-up work
racing the fixture's `EnsureCreated`. It is also logged by the existing
`Slice04ProductEventsTests.With_nothing_stopping_it_one_of_the_new_events_reaches_the_collector` (checked), and
it is not a failure.

## Frontend

### `src/pages/Settings/System/SystemSettingsTab.usageData.test.tsx` (reporter replaced)

| Spec | Class | Fails on |
|---|---|---|
| reports the ordering setting switched on once the server has accepted it | MISSING_FUNCTIONALITY | the "not reported before the answer" leg passes; after the answer, the reporter was called 0 times, expected 1 |
| reports it switched back off as one more event | MISSING_FUNCTIONALITY | reporter called 0 times after the first switch |
| reports nothing when the veto, stored as false, is switched on | MISSING_FUNCTIONALITY | the veto write went through with nothing reported; the ordering control reported 0 times, expected 1. Re-run after the rework. |
| reports nothing when the veto, stored as true, is switched off | MISSING_FUNCTIONALITY | same as above. Re-run after the rework. |
| reports nothing for a switch the server refused | MISSING_FUNCTIONALITY | the refusal and rollback legs pass; the accepted control switch reported 0 times, expected 1 |
| reports nothing for a setting usage data has no name for | MISSING_FUNCTIONALITY | the unnamed setting's write went through; the ordering control reported 0 times, expected 1 |

### `src/pages/Settings/System/SystemSettingsTab.usageDataHandIn.test.tsx` (real consent answer, reporter and detector)

| Spec | Class | Fails on |
|---|---|---|
| hands in which setting was switched and which way | MISSING_FUNCTIONALITY | nothing was posted; `postEvents` received `[]` |

Removed after the rework: "hands in the veto being lifted".

### Check that the specs can go green: amended DESIGN simulated

`SystemSettingsTab.onToggleOptionalFeature` was temporarily changed as the amended DESIGN describes:
report `{ name, optionalFeature, enabled: !toggledFeature.enabled }` after `updateFeature` resolves,
through a `Record` that maps only `FeatureOrdering` to `FeatureOrder`, and report nothing for any other
key. Both files were then run with every spec un-skipped. **All 7 pass.** The file was restored
afterwards and `git diff` is empty.
