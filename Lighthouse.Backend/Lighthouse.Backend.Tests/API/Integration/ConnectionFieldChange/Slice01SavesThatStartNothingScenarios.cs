namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// Story #6307, slice 01, the other side of the same promise: a save starts updates only when it
    /// changes what the Teams and Portfolios on the connection ask the tracker for, and only for those.
    /// A new API token, a new name, a refused save, a save the admin chose to keep from starting updates,
    /// or reading the connection, starts nothing; each of those Teams and Portfolios keeps its next
    /// scheduled update.
    ///
    /// These hold today, when no save starts anything, and they have to keep holding once a field change
    /// does. They run now rather than waiting for DELIVER.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6307-refresh-on-additional-fields-change")]
    [Category("slice-01")]
    public partial class Slice01SavesThatStartNothingTest
    {
        [TestCase(EditThatChangesNothingFetched.NewApiToken)]
        [TestCase(EditThatChangesNothingFetched.NewConnectionName)]
        [TestCase(EditThatChangesNothingFetched.NewJiraAddress)]
        [TestCase(EditThatChangesNothingFetched.NewSignInMethod)]
        [TestCase(EditThatChangesNothingFetched.NewWriteBackMapping)]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("kpi")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_save_that_changes_nothing_the_Teams_and_Portfolios_ask_for_starts_no_update(EditThatChangesNothingFetched edit)
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();

            await WhenPriyaSaves(NordwindJiraDc, edit);

            ThenTheSaveIsAccepted();
            await ThenNoTeamOrPortfolioIsUpdated();
        }

        // The Teams and Portfolios ask the tracker for the same fields whatever order they are listed in.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("kpi")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Saving_the_same_fields_in_another_order_starts_no_update()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenTheConnectionAlsoReads(NordwindJiraDc, FixVersions, FixVersionsReference);

            await WhenPriyaSaves(NordwindJiraDc, EditThatChangesNothingFetched.SameFieldsInAnotherOrder);

            ThenTheSaveIsAccepted();
            await ThenNoTeamOrPortfolioIsUpdated();
        }

        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_Team_on_another_connection_is_left_alone()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc);

            ThenTheSaveIsAccepted();
            await ThenTheTeamIsNotUpdated(Harbor);
        }

        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_added_to_a_connection_nobody_uses_is_saved_and_starts_no_update()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraSandbox);

            ThenTheSaveIsAccepted();
            await ThenTheConnectionReads(NordwindJiraSandbox, StoryPoints, FixVersions);
            await ThenNoTeamOrPortfolioIsUpdated();
        }

        // Someone else saved after Priya opened the page; the community licence allows one Additional
        // Field and this would be the second; a write-back mapping is missing its field.
        [TestCase(Refusal.SomeoneElseSavedFirst)]
        [TestCase(Refusal.TooManyFieldsForTheLicence)]
        [TestCase(Refusal.AnUnfinishedWriteBackMapping)]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_save_that_is_refused_stores_nothing_and_starts_no_update(Refusal refusal)
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc, refusal);

            ThenTheSaveIsRefusedBecause(refusal);
            await ThenTheConnectionReads(NordwindJiraDc, StoryPoints);
            await ThenNoTeamOrPortfolioIsUpdated();
        }

        [Test]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Saving_only_stores_the_field_and_starts_no_update()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();

            await WhenPriyaAddsFixVersionsAndSaves(NordwindJiraDc, SaveOnly);

            ThenTheSaveIsAccepted();
            await ThenTheConnectionReads(NordwindJiraDc, StoryPoints, FixVersions);
            await ThenNoTeamOrPortfolioIsUpdated();
        }

        // Starts where the scenario above ends: the field fills in with each Team's next scheduled update.
        [Test]
        [Category("us-03")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task After_saving_only_the_next_scheduled_update_downloads_everything()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();
            await GivenPriyaAddedFixVersionsAndSaved(NordwindJiraDc, SaveOnly);
            await GivenNoTeamOrPortfolioHasBeenUpdatedSince();

            await WhenTheNextScheduledUpdateOfTeamRuns(Atlas);

            await ThenTheUpdateOfDownloadedEverything(Atlas);
        }

        // Reading a connection registers the tracker's own fields on it. Nobody pressed Save, so nobody
        // asked for downloads.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Looking_up_a_connection_that_registers_the_trackers_own_field_starts_no_update()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            GivenTheTrackerOffersAFieldOfItsOwn(Releases, ReleasesReference);

            await WhenSomeoneLooksUpTheConnection(NordwindJiraDc);

            await ThenTheConnectionReads(NordwindJiraDc, StoryPoints, Releases);
            await ThenNoTeamOrPortfolioIsUpdated();
        }

        // Starts where the scenario above ends: a later save that only renames the connection does not count
        // the field registered by the lookup as a change of its own.
        [Test]
        [Category("us-01")]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_field_registered_by_a_lookup_does_not_make_the_next_save_start_updates()
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            GivenTheTrackerOffersAFieldOfItsOwn(Releases, ReleasesReference);
            await GivenSomeoneHasLookedUpTheConnection(NordwindJiraDc);

            await WhenPriyaSaves(NordwindJiraDc, EditThatChangesNothingFetched.NewConnectionName);

            ThenTheSaveIsAccepted();
            await ThenNoTeamOrPortfolioIsUpdated();
        }
    }
}
