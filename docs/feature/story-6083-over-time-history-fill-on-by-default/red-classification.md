# RED classification — story-6083-over-time-history-fill-on-by-default

Run 2026-10-01 against unmodified production code (only the `AppSettingKeys.HistoryFillSwitchedOnByDefault`
constant added), with every new `[Ignore]` stripped, `dotnet build Lighthouse.sln` (0 warnings), then
`dotnet test --no-build --filter "FullyQualifiedName~Story6083HistoryFillOnByDefaultTest|FullyQualifiedName~SeedAsync_AddsTheOverTimeHistoryFill_OnInPreviewAndFree_AndRecordsThatItSwitchedItOn|FullyQualifiedName~SeedAsync_RunAgainInTheSameProcessAfterTheAdministratorSwitchedTheFillOff_LeavesItOff|FullyQualifiedName~SeedAsync_KeepsTheRecordThatTheHistoryFillWasSwitchedOn"`.
Re-ignored afterwards. 11/11 failed, all on an assertion.

Acceptance: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/Story6083HistoryFillOnByDefaultScenarios.cs`.

| Scenario | Classification | Failure observed |
|----------|----------------|------------------|
| `A_new_instance_has_the_history_fill_switched_on` | MISSING_FUNCTIONALITY | `enabled` read `false` over HTTP; expected `true` (seeded off) |
| `An_upgraded_instance_…(SwitchedOff)` | MISSING_FUNCTIONALITY | reads `false`, and no switch-on record stored |
| `An_upgraded_instance_…(SwitchedOn)` | MISSING_FUNCTIONALITY | reads `true` (unchanged), but no switch-on record stored |
| `An_upgraded_instance_…(NotYetOffered)` | MISSING_FUNCTIONALITY | the setting is re-added `false`, and no switch-on record stored |
| `The_upgrade_switches_the_history_fill_on_and_changes_nothing_else_in_behaviour_settings` | MISSING_FUNCTIONALITY | before/after lists identical; the only difference expected (`OverTimeHistoryFill` `Enabled` false → true) is absent. The diff shows every other field and setting already equal |
| `An_admin_who_switches_the_history_fill_off_after_the_upgrade_keeps_it_off_across_every_restart` | MISSING_FUNCTIONALITY | checked Given: "the upgrade should have switched the history fill on" (read `false`) |
| `A_new_instance_whose_admin_switches_the_history_fill_off_keeps_it_off_across_restarts` | MISSING_FUNCTIONALITY | checked Given: "a new instance should start with the history fill on" (read `false`) |
| `A_history_fill_setting_removed_by_hand_comes_back_on_at_the_next_restart` | MISSING_FUNCTIONALITY | checked Given (read `false`). Re-run after D7 made it an ordinary pending scenario: same failure |

Unit (`Services/Implementation/Seeding/`):

| Test | Classification | Failure observed |
|------|----------------|------------------|
| `OptionalFeatureSeederTests.SeedAsync_AddsTheOverTimeHistoryFill_OnInPreviewAndFree_AndRecordsThatItSwitchedItOn` | MISSING_FUNCTIONALITY | 2 of 4: `Enabled` false, no record. `IsPreview`/`IsPremium` already pass |
| `OptionalFeatureSeederTests.SeedAsync_RunAgainInTheSameProcessAfterTheAdministratorSwitchedTheFillOff_LeavesItOff` | MISSING_FUNCTIONALITY | checked arrangement: the first run did not switch the fill on |
| `AppSettingSeederTests.SeedAsync_KeepsTheRecordThatTheHistoryFillWasSwitchedOn` | MISSING_FUNCTIONALITY | checked arrangement: start-up wrote no record for the clean-up to keep |

Control: the `SwitchedOn` case shows the read port, the rewind and the record probe all work (it reads `true` and
fails only on the missing record), and the before/after diff in the "changes nothing else" scenario differs only
in the one field the story changes. Each acceptance test logs `SQLite Error 1: 'no such table: Features'` from
"Startup orphaned-feature cleanup failed (non-fatal)". That is harness noise: the existing
`Slice02OneListOfSwitchesTest` logs it too, and it never decides an outcome.

No IMPORT_ERROR / FIXTURE_BROKEN / SETUP_FAILURE / WRONG_ASSERTION.

## Addendum — restore and clear (D8), run 2026-10-01

Same procedure, filter `FullyQualifiedName~Story6083HistoryFillOnByDefaultTest.Restoring|…Clearing|…A_history_fill_setting_removed`.
Both drive the real `DatabaseManagementService` with only `IDatabaseManagementProvider` replaced. Each `When` first
checks that the operation completed **and** that the seeders ran afterwards, because a seeding failure there is
logged, not reported. Both checks passed, so neither failure below is a harness fault.

| Scenario | Classification | Failure observed |
|----------|----------------|------------------|
| `Restoring_a_backup_from_before_this_release_brings_the_history_fill_back_off_and_a_restart_keeps_it_off` | MISSING_FUNCTIONALITY | the fill reads off after the restore and after the restart (there is no flip yet to suppress), but no switch-on record is written |
| `Clearing_the_database_leaves_the_history_fill_switched_on_like_a_new_instance` | MISSING_FUNCTIONALITY | reads `false` after the clear, and no switch-on record |

The restore scenario's two "stays off" assertions pass on today's code only because nothing flips yet. They are
the guard that goes red if DELIVER's flip also runs on the restore path.
