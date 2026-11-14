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
| 9 | A_field_only_a_default_names_cannot_be_removed | a save that removes the field only the Teams default names is refused, naming that default | ConnectionSaveServiceTest `Commit_RemovingTheFieldOnlyTheTeamsDefaultNames_IsRefusedNamingThatDefault` (save level), `Commit_RemovingAFieldBothDefaultsName_IsRefusedInTheWordsThisInstanceUses`; AdditionalFieldInUseTest `A_field_only_a_default_names_is_refused_naming_...` (the words) | A10 (Teams default not counted), A11, A12, A1 (not refused) | A10: both save rows above; A11: both save rows and the AdditionalFieldInUseTest row; A12: the AdditionalFieldInUseTest row; A1: both save rows |
| 10 | A_field_nobody_uses_is_removed_as_today | a save that removes a field nobody uses is accepted | ConnectionSaveServiceTest `Commit_RemovingAFieldATeamListedBeforeItWentBackToTheDefault_IsSaved`; AdditionalFieldInUseTest `A_field_nobody_uses_is_not_in_use` | A2 (always refused), A11, A12 | A2: the ConnectionSaveServiceTest row (and the field-added rows); A11 and A12: the AdditionalFieldInUseTest row (A12 also the ConnectionSaveServiceTest row) |
| 14 | A_new_Team_cannot_name_a_field_its_connection_does_not_have | a field the connection lacks is refused with its own code | OwnerSettingRefusalsTest `One_field_the_connection_lacks_...` and `A_list_whose_every_field_the_connection_holds_is_not_refused` | A13, A15 | those rows |
| 15 | A_Team_saved_with_a_source_Lighthouse_does_not_know_is_refused | an unknown source is refused | OwnerSettingRefusalsTest source rows | A14 | the unknown-source rows |
| 16-22 | A_Team_saved_by_a_client_that_may_not_send_the_source (7 cases) | what a save with or without a source does to source and value | IncomingSettingTest matrix, TeamExtensionsTest, PortfolioExtensionsTest, ParentSources extension rows | B1 to B6, C3, F4, F6 | the matrix rows |
| 23-29 | A_script_that_saves_back_what_it_read_changes_nothing (7 cases) | a client echoing or dropping properties changes nothing | IncomingSettingTest `An_older_clients_round_trip_keeps_the_...` rows, SettingsOwnerDtoRoundTripTest `EverySettingOfATeamSurvivesBeingSavedAndReadBack` and the Portfolio twin, ConnectionDefaultsDtoTest, ParentSources extension rows `A_changed_old_field_beside_the_list_the_client_read_...` and `An_old_field_beside_a_new_list_has_nothing_left_to_say` | B1, B3, E5, E6, F1, F2, F3, F5 | B1, B3: the older-client rows; F1 to F3: the two EverySetting rows; F5: the two old-field rows; E5, E6: the ConnectionDefaultsDtoTest rows |
| 30 | A_stale_copy_saved_back_without_its_source_makes_the_Team_own_the_old_default | follower sends another field without source: becomes Own | IncomingSettingTest, TeamExtensionsTest | B1, B4, B6 | the matrix rows |
| 31 | A_stale_copy_saved_back_with_its_source_keeps_the_Team_following_the_new_default | follower sends its source: keeps following | IncomingSettingTest | B2, B5 | the matrix rows |
| 32 | A_stale_copy_of_a_follower_still_naming_a_removed_field_keeps_the_Team_following | follower keeps following; removal checked | IncomingSettingTest and ConnectionSaveServiceTest | B2, B3, A2, A12 | B2 and B3: the follower rows of IncomingSettingTest; A2 and A12: `Commit_RemovingAFieldATeamListedBeforeItWentBackToTheDefault_IsSaved` |

Stays: row 1 (409, code and read-back through the controller), row 11 (a save in the gap, real store), row 12 (concurrency token, real store), row 13 (400 and code through the controller).

## Slice05aTeamsParentOverrideDefaultScenarios (13 rows leave, 5 stay)

| Row | Removed host row | Path it walks | Now proven by (unit class) | Break | Red rows |
|---|---|---|---|---|---|
| 2 | A_Team_with_its_own_Parent_Override_Field_is_unaffected_by_the_default | what a Team reads by its source, and where its item hangs | EffectiveSettingTest `A_Team_reads_its_own_Parent_Override_Field_or_the_connections_default_by_its_source` (the reading); WhereATeamsWorkItemHangsTest `A_Team_with_its_own_field_hangs_its_item_under_that_field_whatever_the_default_names` (the hang: effective list, then the first place with a parent wins) | C1, C2; G1 (own list ignored), G5 (item hung under nothing) | the EffectiveSettingTest cases; the hang row |
| 3 | A_Team_that_chose_None_keeps_the_trackers_own_parent_whatever_the_default_says | an owner of None reads nothing and hangs under the tracker's own parent; followers counted | EffectiveSettingTest, WorkTrackingSystemConnectionUsageTest; WhereATeamsWorkItemHangsTest `A_Team_that_chose_None_hangs_its_item_under_the_trackers_own_parent_whatever_the_default_names` | C2, C3; G1, G4 (None still reads a field), G5 | the reading rows, the follower-count rows, the None hang row |
| 4 | A_new_Team_follows_the_default_from_the_start | a new Team without a source follows | TeamExtensionsTest, EffectiveSettingTest | B1, B3 (new-Team rows of TeamExtensionsTest), C4, C5 (follower counts) | those rows |
| 7-8 | Jira_and_Azure_DevOps_offer_every_default (2 cases) | what a tracker offers | SupportedConnectionDefaultsTest | D1 | `AzureDevOps x ParentOverrideForTeams`, `Jira x ParentOverrideForTeams` |
| 9 | A_CSV_connection_offers_only_Feature_Owner_and_Estimation | what a CSV file offers | SupportedConnectionDefaultsTest | D2, D4 | `Csv x ParentOverrideForTeams`, `Csv x FeatureOwnerFieldForPortfolios` |
| 10-11 | Linear_and_ServiceNow_offer_no_default (2 cases) | a tracker with no defaults | SupportedConnectionDefaultsTest | D3 | `Linear x EstimationForTeams`, `ServiceNow x EstimationForTeams` |
| 13 | A_save_that_changes_no_default_writes_no_log_entry | an unchanged default is not logged | ConnectionSaveServiceTest `Commit_TheTeamsDefaultUnchanged_LogsNothing` | E1 | that row |
| 14 | A_default_nobody_follows_still_writes_an_entry_saying_none_follow | the entry says no Team follows it | ConnectionSaveServiceTest `Commit_TheTeamsDefaultSetButEveryTeamOwnsItsField_...` | E2 | that row |
| 15 | A_save_without_the_defaults_keeps_every_default | a default left out of a save is kept | ConnectionDefaultsDtoTest `A_Parent_default_left_out_of_the_save_keeps_what_the_connection_holds` and the Estimation variant | E5, E6 | those rows |
| 16 | Clearing_a_default_stops_the_followers_reading_it_and_is_logged | a cleared default names the field it was; its followers then hang under the tracker's own parent | ConnectionSaveServiceTest `Commit_TheTeamsDefaultCleared_LogsOneEntryNamingTheFieldItWas`, EffectiveSettingTest; WhereATeamsWorkItemHangsTest `A_Team_that_followed_the_default_hangs_its_item_under_the_trackers_own_parent_once_the_default_is_cleared` and `A_Team_that_follows_a_default_nobody_set_...` | E3, C1; G2 (follower never reads the default), G3 (wrong default read), G5 | the log row, the reading rows, the hang rows |
| 18 | A_field_added_in_the_same_save_can_be_the_default | the default is pointed at the stored field | ConnectionSaveServiceTest `Commit_TheDefaultNamesAFieldAddedInThisSave_PointsTheDefaultAtTheStoredField` | E4 | that row |

The hang rows (2, 3, 16) are proven by WhereATeamsWorkItemHangsTest, which runs the Team's effective list through the production selector with a fixed stand-in for what each field holds; the connector's own reading of a field is proven by the unchanged, non-live JiraParentReadingsTest (23 rows) and stays outside the S1 break set.

The screen lists 13 moves: rows 2, 3, 4, 7, 8, 9, 10, 11, 13, 14, 15, 16, 18.

Stays: row 1 (skeleton), 5 and 6 (RBAC), 12 (400 and code through the controller), 17 (concurrency token).

## Touched paths

Only test files and evidence. No production file, no budget file, no guard line is added.

| File | Why | Break showing its earlier rows still fail when they should |
|---|---|---|
| Lighthouse.Backend/Lighthouse.Backend.Tests/API/Helpers/OwnerSettingRefusalsTest.cs | one new row, `A_list_whose_every_field_the_connection_holds_is_not_refused` (helper `TheRefusalIfAny`), so a refusal that fires for a clean list turns red | A13 turns the new row red; A14 and A15 turn the earlier rows red |
| Lighthouse.Backend/Lighthouse.Backend.Tests/Models/WhereATeamsWorkItemHangsTest.cs | new class, five rows: where a Team's item hangs for an own field, None, a followed default, an unset default and a cleared default | G1 to G5 turn the hang rows red |
| Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Connections/ConnectionSaveServiceTest.cs | one new row, `Commit_RemovingTheFieldOnlyTheTeamsDefaultNames_IsRefusedNamingThatDefault` | A10, A11 and A1 turn it red |
| Lighthouse.Backend/Lighthouse.Backend.Tests/API/DTO/ConnectionDefaultsDtoTest.cs | one new row, `A_Parent_default_left_out_of_the_save_keeps_what_the_connection_holds` | E5 turns the new row red; E6 turns the earlier list row red |
| Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ConfigureOncePerConnection/Slice05bReferencedFieldsAndOlderPayloadsScenarios.cs and Slice05aTeamsParentOverrideDefaultScenarios.cs | the craft removes the rows listed above; the rows that stay are not edited | n/a, host rows that stay are run by the oracle |
