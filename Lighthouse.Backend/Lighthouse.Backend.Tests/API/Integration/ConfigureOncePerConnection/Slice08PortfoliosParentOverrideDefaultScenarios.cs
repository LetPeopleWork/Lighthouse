namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// The Portfolios get a Parent Override Field default of their own, separate from the Teams' one. Card
    /// Issuing names nothing and follows it; Merchant Onboarding names "Parent Link" itself and is left alone.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    [Category("slice-08")]
    [Category("us-07")]
    public class Slice08PortfoliosParentOverrideDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private static readonly string[] CardIssuingDownloadsEverything = [$"{CardIssuing} Full"];

        [Test]
        [Category("ac-7.1")]
        [Category("ac-7.2")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_that_names_nothing_hangs_its_Features_under_what_the_Portfolios_default_names()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosParentOverrideField, CausedBy, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.EqualTo(CardIssuingDownloadsEverything), "Merchant Onboarding owns its value and no Team reads this default.");
                Assert.That(await WhereTheItemOfHangs(CardIssuing), Is.EqualTo(WhatCausedByHolds));
                Assert.That(await HowManyFollow(ConnectionDefault.PortfoliosParentOverrideField), Is.EqualTo(1));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("Portfolios' Parent Override Field default").And.Some.Contains("1 Portfolio follows it: Card Issuing"));
            }
        }

        [Test]
        [Category("ac-7.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task The_Teams_default_never_reaches_a_Portfolio()
        {
            var portfoliosFollowingBefore = await HowManyFollow(ConnectionDefault.PortfoliosParentOverrideField);

            await WhenTheAdminSetsTheDefault(ConnectionDefault.TeamsParentOverrideField, CausedBy);
            ThenTheSaveIsAccepted();
            var logged = TheLogEntriesAboutChangedDefaults();
            await WhenTheNextScheduledUpdateOf(CardIssuing);
            var page = await WhatTheSettingsPageShows(CardIssuing, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page, Is.EqualTo(((string?)"ConnectionDefault", (string?)null, (string?)null)), "Card Issuing follows the Portfolios' default, which names nothing.");
                Assert.That(await WhereTheItemOfHangs(CardIssuing), Is.EqualTo(TheTrackersOwnParent));
                Assert.That(await HowManyFollow(ConnectionDefault.PortfoliosParentOverrideField), Is.EqualTo(portfoliosFollowingBefore));
                Assert.That(logged, Has.Count.EqualTo(1));
                Assert.That(logged, Has.None.Contains("Portfolio").And.None.Contains(CardIssuing).And.None.Contains(MerchantOnboarding));
            }
        }

        [Test]
        [Category("ac-7.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task The_Portfolios_default_never_reaches_a_Team()
        {
            await GivenTheDefaultIs(ConnectionDefault.PortfoliosParentOverrideField, CausedBy);

            await WhenTheNextScheduledUpdateOf(FraudSignals);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.PortfoliosParentOverrideField), Is.EqualTo(CausedBy));
                Assert.That(await WhereTheItemOfHangs(FraudSignals), Is.EqualTo(TheTrackersOwnParent));
            }
        }

        [Test]
        [Category("ac-7.4")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_with_its_own_field_is_unaffected()
        {
            await GivenTheDefaultIs(ConnectionDefault.PortfoliosParentOverrideField, CausedBy);

            await WhenTheNextScheduledUpdateOf(MerchantOnboarding);
            var page = await WhatTheSettingsPageShows(MerchantOnboarding, OwnerSetting.ParentOverrideField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhereTheItemOfHangs(MerchantOnboarding), Is.EqualTo(WhatParentLinkHolds));
                Assert.That((page.Source, page.Reads), Is.EqualTo(((string?)"Own", (string?)ParentLink)));
            }
        }

        // A tab opened before a default existed, or an older client, sends the defaults it knows. Leaving one
        // out must not read as clearing it.
        [TestCase(ConnectionDefault.TeamsParentOverrideField)]
        [TestCase(ConnectionDefault.PortfoliosParentOverrideField)]
        [Category("ac-4.1")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_save_that_leaves_one_default_out_keeps_it(ConnectionDefault theDefault)
        {
            await GivenTheDefaultIs(theDefault, CausedBy);

            await WhenAnOlderPageSavesTheConnectionWithout(theDefault);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(await TheDefaultNamedOnTheConnection(theDefault), Is.EqualTo(CausedBy));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Is.Empty);
            }
        }

        [TestCase(ConnectionDefault.TeamsParentOverrideField)]
        [TestCase(ConnectionDefault.PortfoliosParentOverrideField)]
        [Category("ac-4.1")]
        [Category("contract-shape:bounded-change")]
        public async Task A_default_sent_as_empty_is_cleared(ConnectionDefault theDefault)
        {
            await GivenTheDefaultIs(theDefault, CausedBy);

            await WhenTheAdminClearsTheDefault(theDefault);

            using (Assert.EnterMultipleScope())
            {
                ThenTheSaveIsAccepted();
                Assert.That(await TheDefaultNamedOnTheConnection(theDefault), Is.Null);
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Count.EqualTo(1));
            }
        }
    }
}
