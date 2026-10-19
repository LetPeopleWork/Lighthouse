namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Estimation is set once per grain on the connection: field, unit, non-numeric flag and categories as one
    /// bundle. It is read when an owner's page loads, so changing it updates nothing; a following owner's page
    /// shows "Connection default (Story Points, Points)". Clearing it leaves the followers without an
    /// estimation.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    [Category("slice-12")]
    [Category("us-11")]
    public class Slice12EstimationDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        [Test]
        [Category("ac-11.1")]
        [Category("ac-11.2")]
        [Category("ac-11.4")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task Every_Team_without_an_estimation_shows_the_default_on_its_next_page_load_and_nothing_updates()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheEstimationDefault(ConnectionDefault.TeamsEstimation, StoryPoints, Points, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave();
            var ledger = await WhatTheEstimationReadsFor(Ledger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ledger, Is.EqualTo(((string?)StoryPoints, (string?)Points, (string?)"ConnectionDefault", (string?)StoryPoints)));
                Assert.That(ran, Is.Empty, "Estimation is read when the page loads, so nothing needs downloading.");
                Assert.That(await HowManyFollow(ConnectionDefault.TeamsEstimation), Is.EqualTo(4), "The dialog says 4 Teams follow it.");
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("Teams' Estimation default").And.Some.Contains("4 Teams follow it"));
            }
        }

        [Test]
        [Category("ac-11.4")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task Changing_only_the_unit_of_the_Estimation_default_is_logged_with_the_whole_default_before_and_after()
        {
            await GivenTheTeamsEstimationDefaultIs(StoryPoints, Points);

            await WhenTheAdminSetsTheEstimationDefault(ConnectionDefault.TeamsEstimation, StoryPoints, "Days");
            ThenTheSaveIsAccepted();

            Assert.That(
                TheLogEntriesAboutChangedDefaults(),
                Has.Some.Contains("Teams' Estimation default from \"Story Points, Points\" to \"Story Points, Days\"; 4 Teams follow it"));
        }

        [Test]
        [Category("ac-11.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Clearing_the_Estimation_default_leaves_the_followers_without_an_estimation()
        {
            await GivenTheTeamsEstimationDefaultIs(StoryPoints, Points);

            await WhenTheAdminClearsTheDefault(ConnectionDefault.TeamsEstimation);
            ThenTheSaveIsAccepted();
            var fraudSignals = await WhatTheEstimationReadsFor(FraudSignals);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fraudSignals.FieldName, Is.Null);
                Assert.That(fraudSignals.Source, Is.EqualTo("ConnectionDefault"));
            }
        }

        [Test]
        [Category("ac-11.1")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_with_its_own_estimation_keeps_it_and_the_Teams_default_never_reaches_a_Portfolio()
        {
            await GivenTheTeamsEstimationDefaultIs(TShirtSize, "Sizes");

            var merchantOnboarding = await WhatTheEstimationReadsFor(MerchantOnboarding);
            var cardIssuing = await WhatTheEstimationReadsFor(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(merchantOnboarding, Is.EqualTo(((string?)StoryPoints, (string?)Points, (string?)"Own", (string?)null)));
                Assert.That(cardIssuing.FieldName, Is.Null, "Card Issuing follows the Portfolios' Estimation default, which is not set.");
            }
        }

        [Test]
        [Category("ac-11.1")]
        [Category("contract-shape:bounded-change")]
        public async Task A_following_Portfolio_shows_the_Portfolios_Estimation_default()
        {
            await WhenTheAdminSetsTheEstimationDefault(ConnectionDefault.PortfoliosEstimation, StoryPoints, Points);
            ThenTheSaveIsAccepted();

            var cardIssuing = await WhatTheEstimationReadsFor(CardIssuing);

            Assert.That(cardIssuing, Is.EqualTo(((string?)StoryPoints, (string?)Points, (string?)"ConnectionDefault", (string?)StoryPoints)));
        }

        [TestCase(FraudSignals)]
        [TestCase(CardIssuing)]
        [Category("ac-11.1")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_script_that_sends_the_estimation_categories_as_null_is_saved_and_the_owner_keeps_following(string owner)
        {
            await GivenTheTeamsEstimationDefaultIs(StoryPoints, Points);

            await WhenAScriptSavesBackTheSettingsWithNullEstimationCategories(owner);
            ThenTheOwnerSaveIsAccepted();

            Assert.That((await WhatTheEstimationReadsFor(owner)).Source, Is.EqualTo("ConnectionDefault"));
        }

        [TestCase(RoundTrip.EchoingEveryProperty)]
        [TestCase(RoundTrip.DroppingWhatItDoesNotKnow)]
        [Category("ac-11.1")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_script_that_saves_back_a_followers_estimation_leaves_it_following(RoundTrip shape)
        {
            await GivenTheTeamsEstimationDefaultIs(StoryPoints, Points);

            await WhenAScriptSavesBackWhatItRead(FraudSignals, shape);
            ThenTheOwnerSaveIsAccepted();

            Assert.That((await WhatTheEstimationReadsFor(FraudSignals)).Source, Is.EqualTo("ConnectionDefault"));
        }
    }
}
