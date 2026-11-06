using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OrderedParentSources
{
    /// <summary>
    /// What a refresh of a Jira Team does with its ordered parent sources, one scenario per behaviour a customer
    /// can see. They run through the running application on the harness of the placement scenarios, which
    /// describes a Jira instance, refreshes a Team or a Portfolio and reads back what was stored and what
    /// Refresh History said. The harness has gained the vocabulary a list needs: entries for the Caused-by link,
    /// a custom field or something the instance does not have, a list the Team owns, and a Data Center switch.
    ///
    /// The scenarios are written against the planned shape of the owner and the record. What is missing until
    /// the slice builds it: <c>OwnParentSources</c> on the owner (a stand-in that throws is there), the record's
    /// readings, the connector's reading of every position, and the placement step that reads the list in order.
    /// The harness answers only the Cloud search endpoint, so the Data Center scenario may also need the
    /// harness to serve that deployment's search path.
    /// </summary>
    [TestFixture]
    public class ParentSourcesRefreshScenarios : ParentPlacementAcceptanceTest
    {
        private const string TheHousekeepingEpic = "PAY-1";

        private const string TheFeatureTheLinkNames = "PAY-7";

        private const string TheOtherFeatureTheLinkNames = "PAY-8";

        private const string TheFeatureProgramRefNames = "PAY-12";

        private const string AnIncident = "INC-88";

        private const string ProgramRef = "Program Ref";

        private const int TheIdOfAFieldNoLongerDefined = 9901;

        private const string TheNextSourceSentence = "The next source in the list was read instead.";

        private static ShownFinding AFindingNaming(string entry) => new("ParentSourceMatchedNothing", "ParentOverrideField", entry, HadANextSource: true);

        private static ShownFinding AFindingNamingTheLastEntry(string entry) => new("ParentSourceMatchedNothing", "ParentOverrideField", entry, HadANextSource: false);

        /// <summary>
        /// Given Payments lists "Is caused by", "Program Ref" and the tracker's own parent in that order, and the
        /// instance holds: PAY-142 linked to Feature PAY-7; PAY-143 with only Program Ref PAY-12; PAY-171 linked
        /// only to the incident INC-88 and with Program Ref PAY-12; PAY-160 with only Jira's own parent PAY-1;
        /// PAY-150 linked to the Features PAY-7 and PAY-8, the second link made first.
        /// When Payments refreshes, each Work Item hangs under the parent its own readings give: PAY-7, PAY-12,
        /// PAY-12 (the incident is passed over and the link entry shows nothing), PAY-1 (listed last, so read
        /// last) and PAY-8 (the oldest link). Only the one record whose links named several Features is counted
        /// and named in the placed line, so single links add neither a count nor a warning line.
        /// </summary>
        [Test]
        public async Task A_refresh_places_every_Work_Item_by_its_own_readings_and_counts_only_the_ambiguous_one()
        {
            var link = TheTeamsCausedByLink();
            var programRef = TheTeamsCustomField(ProgramRef);
            TheTeamListsTheParentSources(link, programRef, ParentSourceEntry.TrackerParent);
            ThePortfolioHoldsTheFeature(TheFeatureTheLinkNames);
            ThePortfolioHoldsTheFeature(TheOtherFeatureTheLinkNames);
            await ThePortfolioRefreshes();

            TheTeamHoldsTheWorkItem("PAY-142");
            TheIssueIsCausedBy("PAY-142", TheFeatureTheLinkNames);
            TheTeamHoldsTheWorkItem("PAY-143");
            TheIssueHoldsInTheField("PAY-143", ProgramRef, TheFeatureProgramRefNames);
            TheTeamHoldsTheWorkItem("PAY-171");
            TheIssueIsCausedBy("PAY-171", AnIncident);
            TheIssueHoldsInTheField("PAY-171", ProgramRef, TheFeatureProgramRefNames);
            TheTeamHoldsTheWorkItem("PAY-160");
            TheTrackersOwnParentOf("PAY-160", TheHousekeepingEpic);
            TheTeamHoldsTheWorkItem("PAY-150");
            TheIssueHasTheLinkHistory(
                "PAY-150",
                LinkEvent.Linked(TheOtherFeatureTheLinkNames, On(2026, 4, 1)),
                LinkEvent.Linked(TheFeatureTheLinkNames, On(2026, 5, 1)));

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem("PAY-142"), Is.EqualTo(TheFeatureTheLinkNames));
                Assert.That(TheStoredParentOfTheWorkItem("PAY-143"), Is.EqualTo(TheFeatureProgramRefNames));
                Assert.That(TheStoredParentOfTheWorkItem("PAY-171"), Is.EqualTo(TheFeatureProgramRefNames),
                    "An incident is no Feature, so the link entry yields nothing and the next source is read.");
                Assert.That(WhatTheLinkEntryShowsOnTheWorkItem("PAY-171", link), Is.EqualTo(NoValueShown),
                    "A link that lost shows nothing, or somebody goes looking for a parent that was never stored.");
                Assert.That(TheStoredParentOfTheWorkItem("PAY-160"), Is.EqualTo(TheHousekeepingEpic));
                Assert.That(TheStoredParentOfTheWorkItem("PAY-150"), Is.EqualTo(TheOtherFeatureTheLinkNames));
                Assert.That(figures.Placed, Is.EqualTo(1), "Only PAY-150 had links that named more than one parent.");
                Assert.That(WhatTheLastRefreshWarned().Where(line => line.Contains("PAY-142", StringComparison.Ordinal)), Is.Empty);
            }
        }

        /// <summary>
        /// Given Payments lists "Is caused by" then "Program Ref", and PAY-150 has Program Ref PAY-12 and a link to
        /// PAY-7, when Payments refreshes, then PAY-150 shows under PAY-7; and after the order is swapped and
        /// Payments refreshes again, it shows under PAY-12.
        /// </summary>
        [Test]
        public async Task Moving_a_source_up_and_refreshing_moves_the_Work_Item_to_the_Feature_that_source_names()
        {
            var link = TheTeamsCausedByLink();
            var programRef = TheTeamsCustomField(ProgramRef);
            TheTeamListsTheParentSources(link, programRef);
            ThePortfolioHoldsTheFeature(TheFeatureTheLinkNames);
            ThePortfolioHoldsTheFeature(TheFeatureProgramRefNames);
            TheTeamHoldsTheWorkItem("PAY-150");
            TheIssueIsCausedBy("PAY-150", TheFeatureTheLinkNames);
            TheIssueHoldsInTheField("PAY-150", ProgramRef, TheFeatureProgramRefNames);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();
            var withTheLinkFirst = TheStoredParentOfTheWorkItem("PAY-150");

            TheTeamListsTheParentSources(programRef, link);
            await TheTeamRefreshes();
            var withTheFieldFirst = TheStoredParentOfTheWorkItem("PAY-150");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withTheLinkFirst, Is.EqualTo(TheFeatureTheLinkNames));
                Assert.That(withTheFieldFirst, Is.EqualTo(TheFeatureProgramRefNames),
                    "Saving a different order has to re-read the Team's items, or they stay under the parent the old order chose.");
            }
        }

        /// <summary>
        /// Given the instance is Jira Data Center and the Epic Link field holds PAY-7 on a Work Item with no Jira
        /// parent, when the Team lists the tracker's own parent and refreshes, then the Work Item hangs under PAY-7;
        /// and when the Team lists only the Caused-by link, the same Work Item hangs under nothing, because the
        /// instance's parent field is read only while the own parent is listed.
        /// </summary>
        [Test]
        public async Task Data_Center_reads_the_instances_parent_field_only_when_the_own_parent_is_listed()
        {
            TheInstanceIsJiraDataCenter();
            var link = TheTeamsCausedByLink();
            TheTeamListsTheParentSources(ParentSourceEntry.TrackerParent);
            ThePortfolioHoldsTheFeature(TheFeatureTheLinkNames);
            TheTeamHoldsTheWorkItem("PAY-142");
            TheIssueHoldsInTheField("PAY-142", "Epic Link", TheFeatureTheLinkNames);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();
            var whileTheOwnParentIsListed = TheStoredParentOfTheWorkItem("PAY-142");

            TheTeamListsTheParentSources(link);
            await TheTeamRefreshes();
            var afterItIsLeftOut = TheStoredParentOfTheWorkItem("PAY-142");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileTheOwnParentIsListed, Is.EqualTo(TheFeatureTheLinkNames));
                Assert.That(afterItIsLeftOut, Is.Empty, "Nothing is guessed: the field the instance hangs parents on is not read unless the own parent is listed.");
            }
        }

        /// <summary>
        /// Given Payments lists an entry whose reference matches no field and no link type on the instance, ahead
        /// of "Program Ref", when Payments refreshes, then the refresh names that entry in what it warned and the
        /// next entry is still read, so PAY-143 shows under the parent Program Ref names.
        /// </summary>
        [Test]
        public async Task An_entry_that_matches_nothing_is_named_in_what_the_refresh_reports_and_the_next_is_still_read()
        {
            var nothing = TheTeamsEntryThatMatchesNothing("Nothing Here");
            var programRef = TheTeamsCustomField(ProgramRef);
            TheTeamListsTheParentSources(nothing, programRef);
            TheTeamHoldsTheWorkItem("PAY-143");
            TheIssueHoldsInTheField("PAY-143", ProgramRef, TheFeatureProgramRefNames);

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheLastRefreshWarned(), Has.Some.Contain("Nothing Here"),
                    "The administrator has to be told which entry reads nothing, or they cannot tell a typo from a quiet instance.");
                Assert.That(TheStoredParentOfTheWorkItem("PAY-143"), Is.EqualTo(TheFeatureProgramRefNames));
            }
        }

        /// <summary>
        /// Given Payments lists two entries that match nothing, the second shown under a name of its own, and holds
        /// two Work Items, when Payments refreshes, then Refresh History carries one finding per entry, in the
        /// list's order and in the name the settings show, and the log says each once: the finding is about the
        /// list, so how many records were read does not multiply it.
        /// </summary>
        [Test]
        public async Task Two_failing_entries_of_one_list_are_both_reported_once_however_many_records_were_read()
        {
            var nothing = TheTeamsEntryThatMatchesNothing("Nothing Here");
            var epicName = TheTeamsEntryThatMatchesNothing("customfield_404", shownAs: "Epic Name");
            TheTeamListsTheParentSources(nothing, epicName, ParentSourceEntry.TrackerParent);
            TheTeamHoldsTheWorkItem("PAY-143");
            TheTeamHoldsTheWorkItem("PAY-144");

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(figures.Findings, Is.EqualTo(new[] { AFindingNaming("Nothing Here"), AFindingNaming("Epic Name") }));
                Assert.That(WhatTheLastRefreshWarned(), Has.Exactly(1).Contain("Nothing Here"));
                Assert.That(WhatTheLastRefreshWarned(), Has.Exactly(1).Contain("Epic Name"));
            }
        }

        /// <summary>
        /// Given Payments lists an entry that matches nothing, then "Program Ref", then another entry that matches
        /// nothing, when Payments refreshes, then each finding says whether a source came after it, and the log
        /// says the next source was read only for the entry that had one: after the last there is nothing to read.
        /// </summary>
        [Test]
        public async Task Only_an_entry_with_a_source_after_it_claims_the_next_source_was_read()
        {
            var nothing = TheTeamsEntryThatMatchesNothing("Nothing Here");
            var programRef = TheTeamsCustomField(ProgramRef);
            var epicName = TheTeamsEntryThatMatchesNothing("customfield_404", shownAs: "Epic Name");
            TheTeamListsTheParentSources(nothing, programRef, epicName);
            TheTeamHoldsTheWorkItem("PAY-143");

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(figures.Findings, Is.EqualTo(new[] { AFindingNaming("Nothing Here"), AFindingNamingTheLastEntry("Epic Name") }));
                Assert.That(WhatTheLastRefreshWarned().Where(line => line.Contains("Nothing Here", StringComparison.Ordinal)), Has.Exactly(1).Contain(TheNextSourceSentence));
                Assert.That(WhatTheLastRefreshWarned(), Has.Exactly(1).Contain("Epic Name"));
                Assert.That(WhatTheLastRefreshWarned().Where(line => line.Contains("Epic Name", StringComparison.Ordinal)), Has.None.Contain(TheNextSourceSentence));
            }
        }

        /// <summary>
        /// Given Payments lists "Program Ref", which the instance defines, and PAY-143 holds nothing in it, when
        /// Payments refreshes, then nothing is reported: an empty field on one record is the ordinary case.
        /// </summary>
        [Test]
        public async Task An_entry_that_is_merely_empty_on_a_record_raises_no_finding()
        {
            TheTeamListsTheParentSources(TheTeamsCustomField(ProgramRef), ParentSourceEntry.TrackerParent);
            TheTeamHoldsTheWorkItem("PAY-143");

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            Assert.That(figures.Findings, Is.Empty);
        }

        /// <summary>
        /// Given Payments lists a field whose definition has gone from the connection, ahead of "Program Ref", when
        /// Payments refreshes, then the entry is reported in the words the settings show for it and the next entry
        /// is still read.
        /// </summary>
        [Test]
        public async Task An_entry_whose_field_definition_is_gone_is_reported_in_the_words_the_settings_use()
        {
            TheTeamListsTheParentSources(ParentSourceEntry.Field(TheIdOfAFieldNoLongerDefined), TheTeamsCustomField(ProgramRef));
            TheTeamHoldsTheWorkItem("PAY-143");
            TheIssueHoldsInTheField("PAY-143", ProgramRef, TheFeatureProgramRefNames);

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(figures.Findings, Is.EqualTo(new[] { AFindingNaming($"Unknown field (id {TheIdOfAFieldNoLongerDefined})") }));
                Assert.That(TheStoredParentOfTheWorkItem("PAY-143"), Is.EqualTo(TheFeatureProgramRefNames));
            }
        }

        /// <summary>
        /// Given a Portfolio lists an entry that matches nothing, when the Portfolio refreshes, then Refresh History
        /// names that entry as it does for a Team.
        /// </summary>
        [Test]
        public async Task A_Portfolio_reports_an_entry_that_matches_nothing()
        {
            ThePortfolioListsTheParentSources(ThePortfoliosEntryThatMatchesNothing("Nothing Here"), ParentSourceEntry.TrackerParent);
            ThePortfolioHoldsTheFeature(TheFeatureTheLinkNames);

            await ThePortfolioRefreshes();

            var figures = await WhatRefreshHistoryShowsForThePortfolio();
            Assert.That(figures.Findings, Is.EqualTo(new[] { AFindingNaming("Nothing Here") }));
        }

        /// <summary>
        /// Given Payments lists the Caused-by link and Jira refuses its link type list, when Payments refreshes,
        /// then no finding says the entry matched no link type: nobody could tell, and the administrator would go
        /// looking for a mistake in a list that is right.
        /// </summary>
        [Test]
        public async Task A_refused_link_type_list_does_not_claim_that_an_entry_matched_nothing()
        {
            TheTeamListsTheParentSources(TheTeamsCausedByLink(), ParentSourceEntry.TrackerParent);
            TheInstanceRefusesItsLinkTypeList();
            TheTeamHoldsTheWorkItem("PAY-143");

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            Assert.That(figures.Findings, Is.Empty);
        }

        /// <summary>
        /// Given Hans Weber's Team had "Is caused by" as its only Parent Override Field and another Team had none,
        /// so after the upgrade they list "Is caused by" then the own parent, and the own parent alone; and one item
        /// of Hans Weber's Team has no link and a Jira parent, and another has a link only to an incident and a Jira
        /// parent, when both Teams refresh, then every Work Item whose link gave a parent, the item with no link (Jira
        /// already fell back to its own parent for an empty override) and every Work Item of the other Team keep the
        /// parent they had, and only the item whose single link is no Feature, which hung under the incident, moves
        /// to its Jira parent.
        /// </summary>
        [Test]
        public async Task After_the_upgrade_only_the_items_the_release_note_names_change_parent()
        {
            var link = TheTeamsCausedByLink();
            TheTeamListsTheParentSources(link, ParentSourceEntry.TrackerParent);
            TheOtherTeamFollowsTheConnectionsList();
            ThePortfolioHoldsTheFeature(TheFeatureTheLinkNames);
            await ThePortfolioRefreshes();
            TheTeamHoldsTheWorkItem("PAY-310");
            TheIssueIsCausedBy("PAY-310", TheFeatureTheLinkNames);
            TheTeamHoldsTheWorkItem("PAY-311");
            TheTrackersOwnParentOf("PAY-311", TheHousekeepingEpic);
            TheTeamHoldsTheWorkItem("PAY-312");
            TheIssueIsCausedBy("PAY-312", AnIncident);
            TheTrackersOwnParentOf("PAY-312", TheHousekeepingEpic);
            TheOtherTeamHoldsTheWorkItem("OPS-1");
            TheTrackersOwnParentOf("OPS-1", TheHousekeepingEpic);

            await TheTeamRefreshes();
            await TheOtherTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem("PAY-310"), Is.EqualTo(TheFeatureTheLinkNames), "The link gave a parent before, and still does.");
                Assert.That(TheStoredParentOfTheWorkItem("PAY-311"), Is.EqualTo(TheHousekeepingEpic), "Jira already fell back to its own parent for an empty override, so this item does not move.");
                Assert.That(TheStoredParentOfTheWorkItem("PAY-312"), Is.EqualTo(TheHousekeepingEpic), "The one item the release note names: its only link is no Feature, so the next entry is read instead of keeping the incident.");
                Assert.That(TheStoredParentOfTheWorkItem("OPS-1"), Is.EqualTo(TheHousekeepingEpic), "A Team with no override read Jira's own parent before and still does.");
            }
        }

        /// <summary>
        /// Given the instance is Jira Data Center and a Team lists "Is caused by" then the own parent, as the upgrade
        /// leaves an override, and PAY-313 has no link, no Jira parent and PAY-7 in the Epic Link field, when the Team
        /// refreshes, then PAY-313 hangs under PAY-7: the empty entry falls through to the own parent, and on Data
        /// Center that is the field the instance keeps its parents in.
        /// </summary>
        [Test]
        public async Task After_the_upgrade_an_empty_override_on_Data_Center_falls_through_to_the_instances_parent_field()
        {
            TheInstanceIsJiraDataCenter();
            TheTeamListsTheParentSources(TheTeamsCausedByLink(), ParentSourceEntry.TrackerParent);
            ThePortfolioHoldsTheFeature(TheFeatureTheLinkNames);
            await ThePortfolioRefreshes();
            TheTeamHoldsTheWorkItem("PAY-313");
            TheIssueHoldsInTheField("PAY-313", "Epic Link", TheFeatureTheLinkNames);

            await TheTeamRefreshes();

            Assert.That(TheStoredParentOfTheWorkItem("PAY-313"), Is.EqualTo(TheFeatureTheLinkNames),
                "Data Center names no own parent on the issue, so reading only that would leave this item with none.");
        }
    }
}
