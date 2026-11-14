# S1 id table: host rows that leave and the unit rows that prove their rules

Slice base: 3cc76466d. Breaks: `s1_breaks.py` (ids A1 to F6), observed output in `s1-break-evidence.txt`; each break names the production line it changes and the row that turns red is printed under it.
Row numbers are those of section 7.1 and 7.5 of the screen (`docs/feature/story-6537-t10-backend-rules-at-unit-level/evidence/step2/screen.md`).

The oracle (`s1_compare_ids.py`) holds the host rows that stay (Slice05b rows 1, 11, 12, 13; Slice05a rows 1, 5, 6, 12, 17) and every unit row. Before craft it reports the 41 host rows below as EXTRA: the craft removes them.

## Slice05bReferencedFieldsAndOlderPayloadsScenarios (28 rows leave, 4 stay)

| Row | Removed host row | Path it walks | Now proven by (unit class) | Break | Red rows |
|---|---|---|---|---|---|
| 2 | A_field_only_a_Team_uses_as_its_own_cannot_be_removed | save, list the owners of the removed field, refuse | ConnectionSaveServiceTest `Commit_RemovingAFieldTeamsOnThisConnectionChoseThemselves_...` | A3, A1, A2 | that row and the Team list rows |
| 3 | A_field_only_a_Teams_estimation_uses_cannot_be_removed | same, the Team's own Estimation field | same row | A4 | same row |
| 4-8 | A_field_only_a_Portfolio_uses_as_its_own_cannot_be_removed (5 cases) | same, one owned field per setting | ConnectionSaveServiceTest `Commit_RemovingAFieldPortfoliosOnThisConnectionChoseForAnySetting_...` and the Portfolio list rows | A5 Parent, A6 Dependency, A7 Size, A8 Feature Owner, A9 Estimation | the Portfolio rows |
| 9 | A_field_only_a_default_names_cannot_be_removed | the defaults count as users of a field | ConnectionSaveServiceTest `Commit_RemovingAFieldBothDefaultsName_...`, AdditionalFieldInUseTest rows | A10 Teams default, A11, A12 | those rows |
| 10 | A_field_nobody_uses_is_removed_as_today | a field nobody uses passes the check | ConnectionSaveServiceTest field-added and logging rows, AdditionalFieldInUseTest `A_field_nobody_uses_is_not_in_use` | A2, A11, A12 | those rows |
| 14 | A_new_Team_cannot_name_a_field_its_connection_does_not_have | a field the connection lacks is refused with its own code | OwnerSettingRefusalsTest `One_field_the_connection_lacks_...` and `A_list_whose_every_field_the_connection_holds_is_not_refused` | A13, A15 | those rows |
| 15 | A_Team_saved_with_a_source_Lighthouse_does_not_know_is_refused | an unknown source is refused | OwnerSettingRefusalsTest source rows | A14 | the unknown-source rows |
| 16-22 | A_Team_saved_by_a_client_that_may_not_send_the_source (7 cases) | what a save with or without a source does to source and value | IncomingSettingTest matrix, TeamExtensionsTest, PortfolioExtensionsTest, ParentSources extension rows | B1 to B6, C3, F4, F6 | the matrix rows |
| 23-29 | A_script_that_saves_back_what_it_read_changes_nothing (7 cases) | a client echoing or dropping properties changes nothing | IncomingSettingTest, SettingsOwnerDtoRoundTripTest, ConnectionDefaultsDtoTest, ParentSources extension rows | B1, B3, E5, E6, F1, F2, F3, F5 | the round trip rows |
| 30 | A_stale_copy_saved_back_without_its_source_makes_the_Team_own_the_old_default | follower sends another field without source: becomes Own | IncomingSettingTest, TeamExtensionsTest | B1, B4, B6 | the matrix rows |
| 31 | A_stale_copy_saved_back_with_its_source_keeps_the_Team_following_the_new_default | follower sends its source: keeps following | IncomingSettingTest | B2, B5 | the matrix rows |
| 32 | A_stale_copy_of_a_follower_still_naming_a_removed_field_keeps_the_Team_following | follower keeps following; removal checked | IncomingSettingTest and ConnectionSaveServiceTest | B2, B3, A1 | those rows |

Stays: row 1 (409, code and read-back through the controller), row 11 (a save in the gap, real store), row 12 (concurrency token, real store), row 13 (400 and code through the controller).

## Slice05aTeamsParentOverrideDefaultScenarios (13 rows leave, 5 stay)

| Row | Removed host row | Path it walks | Now proven by (unit class) | Break | Red rows |
|---|---|---|---|---|---|
| 2 | A_Team_with_its_own_Parent_Override_Field_is_unaffected_by_the_default | what a Team reads by its source | EffectiveSettingTest `A_Team_reads_its_own_Parent_Override_Field_or_the_connections_default_by_its_source` | C1, C2 | that test's cases |
| 3 | A_Team_that_chose_None_keeps_the_trackers_own_parent_whatever_the_default_says | an owner of None reads nothing; followers counted | EffectiveSettingTest, WorkTrackingSystemConnectionUsageTest | C2, C3 | those rows |
| 4 | A_new_Team_follows_the_default_from_the_start | a new Team without a source follows | TeamExtensionsTest, EffectiveSettingTest | B1, B3 (new-Team rows of TeamExtensionsTest), C4, C5 (follower counts) | those rows |
| 7-8 | Jira_and_Azure_DevOps_offer_every_default (2 cases) | what a tracker offers | SupportedConnectionDefaultsTest | D1 | `AzureDevOps x ParentOverrideForTeams`, `Jira x ParentOverrideForTeams` |
| 9 | A_CSV_connection_offers_only_Feature_Owner_and_Estimation | what a CSV file offers | SupportedConnectionDefaultsTest | D2, D4 | `Csv x ParentOverrideForTeams`, `Csv x FeatureOwnerFieldForPortfolios` |
| 10-11 | Linear_and_ServiceNow_offer_no_default (2 cases) | a tracker with no defaults | SupportedConnectionDefaultsTest | D3 | `Linear x EstimationForTeams`, `ServiceNow x EstimationForTeams` |
| 13 | A_save_that_changes_no_default_writes_no_log_entry | an unchanged default is not logged | ConnectionSaveServiceTest `Commit_TheTeamsDefaultUnchanged_LogsNothing` | E1 | that row |
| 14 | A_default_nobody_follows_still_writes_an_entry_saying_none_follow | the entry says no Team follows it | ConnectionSaveServiceTest `Commit_TheTeamsDefaultSetButEveryTeamOwnsItsField_...` | E2 | that row |
| 15 | A_save_without_the_defaults_keeps_every_default | a default left out of a save is kept | ConnectionDefaultsDtoTest `A_Parent_default_left_out_of_the_save_keeps_what_the_connection_holds` and the Estimation variant | E5, E6 | those rows |
| 16 | Clearing_a_default_stops_the_followers_reading_it_and_is_logged | a cleared default names the field it was | ConnectionSaveServiceTest `Commit_TheTeamsDefaultCleared_...`, EffectiveSettingTest | E3, C1 | those rows |
| 18 | A_field_added_in_the_same_save_can_be_the_default | the default is pointed at the stored field | ConnectionSaveServiceTest `Commit_TheDefaultNamesAFieldAddedInThisSave_PointsTheDefaultAtTheStoredField` | E4 | that row |

Rows 2, 3 and 4 add no new row; the screen lists 13 moves: rows 2, 3, 4, 7, 8, 9, 10, 11, 13, 14, 15, 16, 18.

Stays: row 1 (skeleton), 5 and 6 (RBAC), 12 (400 and code through the controller), 17 (concurrency token).

## Touched paths

Only test files and evidence. No production file, no budget file, no guard line is added.

| File | Why | Break showing its earlier rows still fail when they should |
|---|---|---|
| Lighthouse.Backend/Lighthouse.Backend.Tests/API/Helpers/OwnerSettingRefusalsTest.cs | one new row, `A_list_whose_every_field_the_connection_holds_is_not_refused` (helper `TheRefusalIfAny`), so a refusal that fires for a clean list turns red | A13 turns the new row red; A14 and A15 turn the earlier rows red |
| Lighthouse.Backend/Lighthouse.Backend.Tests/API/DTO/ConnectionDefaultsDtoTest.cs | one new row, `A_Parent_default_left_out_of_the_save_keeps_what_the_connection_holds` | E5 turns the new row red; E6 turns the earlier list row red |
| Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ConfigureOncePerConnection/Slice05bReferencedFieldsAndOlderPayloadsScenarios.cs and Slice05aTeamsParentOverrideDefaultScenarios.cs | the craft removes the rows listed above; the rows that stay are not edited | n/a, host rows that stay are run by the oracle |
