# RED classification — story-6084-over-time-history-fill-always-on

Run 2026-10-15 against unmodified production code (no scaffold, no production line added), with every new
`[Ignore]` stripped, `dotnet build Lighthouse.sln` (0 errors; the only 2 warnings are the pre-existing NuGet
audit `NU1903` on `Microsoft.AspNetCore.SignalR.Common`), then
`dotnet test Lighthouse.Backend.Tests --no-build --filter "TestCategory=story-6084-over-time-history-fill-always-on|FullyQualifiedName~SeedAsync_RemovesTheRecordThatTheHistoryFillWasSwitchedOn_AndKeepsEverythingTheClearOutByNumberKeeps|FullyQualifiedName~SeedAsync_OnAnEmptyDatabase_NeverAddsTheHistoryFillNorARecordAboutIt"`.
Re-ignored afterwards. 15/15 failed, all on an assertion.

**Review revision, same day.** The two acceptance pairs were un-ignored again after the review changes (one restore
case added, #3 and #9 renamed, #4 now also compares every stored value, #8 checks PBC Over Time on the Portfolio's
own limit days) and run with `--filter "TestCategory=story-6084-over-time-history-fill-always-on"`: 14/14 failed, all
on an assertion, then re-ignored. The two unit tests are unchanged and were not re-run un-ignored. 16/16 in total.

Acceptance (behaviour settings): `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/Story6084HistoryFillAlwaysOnScenarios.cs`.

| Scenario | Classification | Failure observed |
|----------|----------------|------------------|
| `A_new_instance_offers_no_history_fill_setting` | MISSING_FUNCTIONALITY | the list read over HTTP holds `FeatureOrdering`, `OverTimeHistoryFill`, `UsageData`; the guard (`FeatureOrdering` offered) passed |
| `An_upgraded_instance_…(SwitchedOnByDefault)` | MISSING_FUNCTIONALITY | after the upgrade the list still holds the fill (`Enabled = True`), the row and the record are still stored, and the stored values differ from "before minus the record" by the record alone |
| `An_upgraded_instance_…(SwitchedOffByTheAdmin)` | MISSING_FUNCTIONALITY | same four failures; the row reads `Enabled = False` (today's record keeps an admin's off) |
| `An_upgraded_instance_…(SwitchedOffBeforeItWasSwitchedOnByDefault)` | MISSING_FUNCTIONALITY | same four failures; today's seeder flips the row on and writes the record |
| `An_instance_whose_fill_setting_was_already_removed_keeps_nothing_of_it_after_the_upgrade` (renamed at review) | MISSING_FUNCTIONALITY | re-run: the seeder re-adds the row (list holds `OverTimeHistoryFill`, row stored) and the record stays (`"true"`) |
| `Restarting_again_and_again_never_brings_the_history_fill_setting_back` (changed at review: also compares every stored value) | MISSING_FUNCTIONALITY | re-run: checked Given: "the upgrade should already have taken the history fill out of Behaviour Settings" (list `FeatureOrdering`, `OverTimeHistoryFill`, `UsageData`) |
| `A_script_that_still_switches_the_history_fill_is_told_there_is_no_such_setting` | MISSING_FUNCTIONALITY | read by name answers `200`, the switch-off write answers `200`, row and record stored |
| `Restoring_a_backup_…(SwitchedOnByDefault)` (added at review) | MISSING_FUNCTIONALITY | four failures: after the restore and after the restart the list holds the fill, the row is stored, the record reads `"true"`. The restore-and-seed check in the `When` passed |
| `Restoring_a_backup_…(SwitchedOffByTheAdmin)` | MISSING_FUNCTIONALITY | after the restore and after the restart the list holds the fill; row and record stored. The `When` first checked that the restore completed **and** the seeders ran afterwards; both passed |
| `Restoring_a_backup_…(SwitchedOffBeforeItWasSwitchedOnByDefault)` | MISSING_FUNCTIONALITY | same; today's restore hook writes the record so the row stays as the backup held it |
| `Restoring_a_backup_…(NotYetOffered)` | MISSING_FUNCTIONALITY | same; today's restore hook adds the row switched off |
| `Clearing_the_database_leaves_no_history_fill_setting_like_a_new_instance` | MISSING_FUNCTIONALITY | the cleared instance is seeded with the row on and the record; the clear-and-seed check passed |

Acceptance (the charts): `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/PercentilesOverTime/Story6084TheFillRunsEverywhereScenarios.cs`.

| Scenario | Classification | Failure observed |
|----------|----------------|------------------|
| `After_the_upgrade_an_instance_whose_admin_had_switched_the_fill_off_fills_in_both_over_time_charts` (changed at review: PBC read from its own limit days) | MISSING_FUNCTIONALITY | re-run: two failures, "Percentiles Over Time for owner 1" (cycle-time percentile days) and "PBC Over Time for owner 1" (Portfolio throughput limit days), each `<empty>` for 2026-08-23..2026-09-22. The upgrade keeps the admin's off, so the reconciler declines to ask |
| `A_chart_waiting_its_turn_still_fills_in_whatever_an_old_fill_setting_says` (renamed at review) | MISSING_FUNCTIONALITY | re-run: one failure, "Percentiles Over Time for owner 2" `<empty>`. The first team (held in flight, then released) is filled; the waiting team holds nothing, because the filler read the switched-off row and dropped its pass. The hold's own checks (a pass parked, the filler reporting it in flight) passed, so the drop path really ran |

Unit (`Services/Implementation/Seeding/`, real seeder over real EF/SQLite):

| Test | Classification | Failure observed |
|------|----------------|------------------|
| `AppSettingSeederTests.SeedAsync_RemovesTheRecordThatTheHistoryFillWasSwitchedOn_AndKeepsEverythingTheClearOutByNumberKeeps` | MISSING_FUNCTIONALITY | `Extra (1): "OptionalFeatures:HistoryFillSwitchedOnByDefault"`. Nothing else differs: the row on number 9 is gone, the rows on 18/26/37, `Install:Timestamp` (number 0) and the seven defaults are kept |
| `OptionalFeatureSeederTests.SeedAsync_OnAnEmptyDatabase_NeverAddsTheHistoryFillNorARecordAboutIt` | MISSING_FUNCTIONALITY | both: the row is added, and the record is written |

Control: in the three upgrade cases the stored-values comparison differs by exactly one line,
`OptionalFeatures:HistoryFillSwitchedOnByDefault=true`, so the "nothing else changed" half is not tripping on
anything another seeder rewrites. The behaviour-settings list differs by the fill row alone. The unit test's
key set differs by the record alone, so the clear-out by number already does what the test expects of it.

Review-revision controls, each a temporary edit reverted before re-ignoring:

- **#8 with the fill left on.** The Given stored the row `Enabled = true` instead of off: #8 **passed**, so the
  Portfolio's throughput limit days really cover all 30 days once the fill runs, and the PBC half is not
  unsatisfiable.
- **#4 past its Given.** The checked Given was flipped to expect the row still offered, so the scenario ran its
  three restarts on today's code: only the two "still stored" assertions (row, record) failed. The Behaviour
  Settings comparison and the new every-stored-value comparison both passed, so a restart changes nothing else
  today and the new half is not tripping on another seeder.

No IMPORT_ERROR / FIXTURE_BROKEN / SETUP_FAILURE / WRONG_ASSERTION.

After re-ignoring: `dotnet build Lighthouse.sln` 0 errors, the same 2 `NU1903` warnings;
`dotnet test --filter "TestCategory=story-6084-over-time-history-fill-always-on|FullyQualifiedName~AppSettingSeederTests|FullyQualifiedName~OptionalFeatureSeederTests"`:
38 passed, 15 skipped, 0 failed. `dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes`
reports no finding in any new or touched file.

After the review revision, re-ignored: `dotnet build` of `Lighthouse.Backend` 0 errors, the same 2 `NU1903`
warnings; the same filter: 38 passed, 16 skipped, 0 failed (54 total).
