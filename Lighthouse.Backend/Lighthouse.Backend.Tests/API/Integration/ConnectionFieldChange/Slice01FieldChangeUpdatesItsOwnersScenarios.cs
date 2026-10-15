namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// Story #6307, slice 01: a field change on a connection updates its Teams and Portfolios. Priya adds
    /// Fix Versions to "Nordwind Jira DC" and presses Save; the Task Manager lists an update for each of
    /// the 4 Teams and 2 Portfolios that read through it, and each of them downloads everything once, so
    /// the field is filled in on every Work Item and Feature without anybody remembering a step.
    ///
    /// A connection's Additional Fields shape what every Team and Portfolio on it asks the tracker for,
    /// so changing them already made each one download everything at its next scheduled update, up to
    /// hours later. The save now starts those updates itself, through the same queue every update goes
    /// through, Teams first.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6307-refresh-on-additional-fields-change")]
    [Category("slice-01")]
    public partial class Slice01FieldChangeUpdatesItsOwnersTest
    {
        // The tracker holds every answer, so the updates stay in the Task Manager long enough to be read.
        [Test]
        [Category("us-01")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task Adding_a_field_lists_an_update_for_every_Team_and_Portfolio_on_the_connection_in_the_Task_Manager()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            GivenTheTrackerDoesNotAnswerUntilToldTo();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc);

            ThenTheSaveIsAccepted();
            await ThenTheTaskManagerListsOneUpdateForEachOf(EveryTeamAndPortfolioOnNordwindJiraDc);
        }

        // Each of them had been updated before, so without a reason an update would only fetch what
        // changed and the new field would stay empty on everything already stored.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task Every_Team_and_Portfolio_on_the_connection_downloads_everything_once_after_a_field_is_added()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc);

            ThenTheSaveIsAccepted();
            await ThenEachOfThemDownloadsEverythingExactlyOnce(EveryTeamAndPortfolioOnNordwindJiraDc);
            await ThenTheConnectionReads(NordwindJiraDc, StoryPoints, FixVersions);
        }

        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task The_Teams_are_updated_before_any_Portfolio()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc);

            await ThenTheTeamsWereUpdatedBeforeAnyPortfolio();
        }

        // The label is part of what every Team and Portfolio stores against each item, so renaming it
        // costs a full download like any other field edit.
        [TestCase(FieldEdit.RenamesAFieldsLabel)]
        [TestCase(FieldEdit.ChangesAFieldsReference)]
        [TestCase(FieldEdit.RemovesAField)]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        [Ignore(Pending)]
        public async Task Any_other_field_edit_also_makes_every_Team_and_Portfolio_on_the_connection_download_everything(FieldEdit edit)
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenPriyaSaves(NordwindJiraDc, edit);

            ThenTheSaveIsAccepted();
            await ThenEachOfThemDownloadsEverythingExactlyOnce(EveryTeamAndPortfolioOnNordwindJiraDc);
        }

        // Atlas is updating when Priya saves and Cobalt is waiting behind it. Atlas runs once more after
        // its running update, and that run downloads everything. Cobalt's waiting update starts after the
        // save, so it downloads everything, and the one run after it only fetches what changed. Neither
        // shows twice in the Task Manager and neither downloads everything twice.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        [Ignore(Pending)]
        public async Task A_Team_already_updating_or_waiting_is_not_downloaded_in_full_twice()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();
            GivenTheRefreshHistorySoFarIsSetAside();
            GivenTheTrackerDoesNotAnswerUntilToldTo();
            await GivenTheUpdateOfTeamIsRunning(Atlas);
            await GivenTheUpdateOfTeamIsWaiting(Cobalt);

            await WhenPriyaAddsFixVersionsAndSavesWhileOthersAreUpdating(NordwindJiraDc);

            await ThenTheTaskManagerListsOneUpdateForEachOf(EveryTeamAndPortfolioOnNordwindJiraDc);
            WhenTheTrackerAnswersAgain();
            await ThenAtlasAndCobaltRanTwiceAndEveryOneDownloadedEverythingOnce();
        }

        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        [Ignore(Pending)]
        public async Task An_update_that_fails_leaves_the_field_saved_and_the_others_updating()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();
            GivenTheTrackerRefusesTheAccountFor(Borealis);

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc);

            ThenTheSaveIsAccepted();
            await ThenTheUpdateOfFailed(Borealis);
            await ThenEachOfThemFinishesItsUpdate(Atlas, Cobalt, DeltaOps, FleetPlatform, CustomerApps);
            await ThenTheConnectionReads(NordwindJiraDc, StoryPoints, FixVersions);
        }

        // Starts where the scenario above ends: the tracker lets Borealis in again.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        [Ignore(Pending)]
        public async Task The_Team_whose_update_failed_downloads_everything_at_its_next_update()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();
            GivenTheTrackerRefusesTheAccountFor(Borealis);
            await GivenPriyaAddedFixVersionsAndSaved(NordwindJiraDc);
            await GivenTheUpdateOfHasFailed(Borealis);
            GivenTheTrackerAcceptsTheAccountAgain();

            await WhenTheNextScheduledUpdateOfTeamRuns(Borealis);

            await ThenTheUpdateOfDownloadedEverything(Borealis);
        }

        // A connection only Portfolios read from has nobody to update first: its Portfolios are started all the same.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task Adding_a_field_to_a_connection_only_Portfolios_read_from_lists_their_updates_in_the_Task_Manager()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenAConnectionOnlyPortfoliosReadFrom();
            GivenTheTrackerDoesNotAnswerUntilToldTo();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraRoadmaps);

            ThenTheSaveIsAccepted();
            await ThenTheTaskManagerListsOneUpdateForEachOf(RoadmapNorth, RoadmapSouth);
        }

        // The community licence allows one Additional Field, so adding it there takes effect the same way.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        [Ignore(Pending)]
        public async Task Adding_a_field_on_the_community_licence_updates_every_Team_and_Portfolio_on_the_connection()
        {
            await GivenASmallInstanceOnTheCommunityLicence();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc);

            ThenTheSaveIsAccepted();
            await ThenEachOfThemDownloadsEverythingExactlyOnce(Atlas, Borealis, Cobalt, FleetPlatform);
        }

        // The command line and the MCP server never ask, so updating now is what a save does unless it says otherwise.
        [Test]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_save_that_asks_to_update_now_updates_every_Team_and_Portfolio_on_the_connection()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc, UpdateNow);

            ThenTheSaveIsAccepted();
            await ThenEachOfThemDownloadsEverythingExactlyOnce(EveryTeamAndPortfolioOnNordwindJiraDc);
        }

        // Whether to update now is a yes or a no. Anything else is refused before anything is stored.
        [TestCase("maybe")]
        [TestCase("later")]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_save_that_asks_for_neither_now_nor_later_is_refused_and_stores_nothing(string choice)
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc, choice);

            ThenTheSaveIsRefusedBecauseItAsksForNeitherNowNorLater();
            await ThenTheConnectionReads(NordwindJiraDc, StoryPoints);
            await ThenNoTeamOrPortfolioIsUpdated();
        }
    }
}
