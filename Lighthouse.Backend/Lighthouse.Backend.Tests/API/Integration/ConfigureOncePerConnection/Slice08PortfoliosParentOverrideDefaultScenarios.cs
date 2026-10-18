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
        [Ignore(PendingEpic6303.Slice08)]
        [Category("ac-7.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task The_Teams_default_never_reaches_a_Portfolio()
        {
            await GivenTheDefaultIs(ConnectionDefault.TeamsParentOverrideField, CausedBy);

            await WhenTheNextScheduledUpdateOf(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhereTheItemOfHangs(CardIssuing), Is.EqualTo(TheTrackersOwnParent));
                Assert.That(await HowManyFollow(ConnectionDefault.PortfoliosParentOverrideField), Is.Zero);
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
    }
}
