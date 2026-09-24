# RED classification — story-5913-always-faster-updates

Run 2026-09-24 against unmodified production code, one scenario at a time (`[Ignore]` commented out,
`dotnet test --no-build --filter "FullyQualifiedName~Story5913AlwaysFasterUpdatesTest.<name>"`), then re-ignored.
File: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Story5913AlwaysFasterUpdatesScenarios.cs`.

| Scenario | Classification | Failure observed |
|----------|----------------|------------------|
| `A_fresh_install_offers_no_faster_updates_switch` | MISSING_FUNCTIONALITY | settings list is `DeltaSync, FeatureOrdering, UsageData`; expected no `DeltaSync` (the seeder still writes the row) |
| `An_upgraded_instance_offers_no_faster_updates_switch_whichever_way_it_was_set(On)` | MISSING_FUNCTIONALITY | after the re-seed the list is still `DeltaSync, FeatureOrdering, UsageData`; expected `FeatureOrdering, UsageData` |
| `An_upgraded_instance_offers_no_faster_updates_switch_whichever_way_it_was_set(Off)` | MISSING_FUNCTIONALITY | same as above for an instance that had it off (the seeder keeps the operator's row) |
| `Upgrading_again_never_brings_the_faster_updates_switch_back` | MISSING_FUNCTIONALITY | `DeltaSync` still offered after two further re-seeds (it was never removed) |
| `A_team_on_an_instance_that_had_faster_updates_off_downloads_only_the_issues_that_moved_after_the_upgrade` | MISSING_FUNCTIONALITY | `ScansIssued` 0 (expected 1), `FullDownloadsIssued` 1 (expected 0): the kept "off" still forces a full download |
| `A_portfolio_on_an_instance_that_had_faster_updates_off_downloads_only_the_features_that_moved_after_the_upgrade` | MISSING_FUNCTIONALITY | `FeatureScansIssued` 0 (expected 1), `FullFeatureDownloadsIssued` 1 (expected 0) |
| `The_parent_features_on_an_instance_that_had_faster_updates_off_are_scanned_rather_than_downloaded_after_the_upgrade` | MISSING_FUNCTIONALITY | `ParentFeatureScans` empty (expected one scan of `PARENT-1`): the parent path still reads the switch |

Control run: with the pre-upgrade switch flipped to **on** instead of off, the three refresh scenarios pass
unchanged on today's code (3/3 green, the team one on an unlicensed instance). So the only thing between them
and green is the switch being honoured, and none of them fails on setup, fixtures or the licence.

No IMPORT_ERROR / FIXTURE_BROKEN / SETUP_FAILURE / WRONG_ASSERTION. No production scaffolds were needed.
