namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// The Portfolios' Size Estimate Field default: a following Portfolio's Features carry the size the
    /// default field holds; a Portfolio with its own field keeps reading it.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    [Category("slice-10")]
    [Category("us-09")]
    public class Slice10PortfoliosSizeEstimateFieldDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private static readonly string[] CardIssuingDownloadsEverything = [$"{CardIssuing} Full"];

        [Test]
        [Category("ac-9.1")]
        [Category("ac-9.2")]
        [Category("ac-9.4")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_following_Portfolio_reads_its_sizes_from_the_default_and_an_owner_is_left_alone()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosSizeEstimateField, StoryPoints, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave(CardIssuing);
            var page = await WhatTheSettingsPageShows(CardIssuing, OwnerSetting.SizeEstimateField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.EqualTo(CardIssuingDownloadsEverything));
                Assert.That(WhatTheTrackerWasAskedToReadFor(CardIssuing, OwnerSetting.SizeEstimateField), Is.EqualTo(StoryPoints));
                Assert.That(WhatTheTrackerWasAskedToReadFor(MerchantOnboarding, OwnerSetting.SizeEstimateField), Is.EqualTo(TShirtSize));
                Assert.That(page, Is.EqualTo(((string?)"ConnectionDefault", (string?)StoryPoints, (string?)StoryPoints)));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("Size Estimate Field default").And.Some.Contains("Card Issuing"));
            }
        }

        [Test]
        [Category("ac-9.1")]
        [Category("contract-shape:pure-function")]
        public async Task The_settings_page_names_the_Size_Estimate_Field_default_not_the_Dependency_one()
        {
            await GivenTheDefaultIs(ConnectionDefault.PortfoliosDependencyField, ReleaseTrain);
            await GivenTheDefaultIs(ConnectionDefault.PortfoliosSizeEstimateField, StoryPoints);

            var cardIssuingsPage = await WhatTheSettingsPageShows(CardIssuing, OwnerSetting.SizeEstimateField);
            var merchantOnboardingsPage = await WhatTheSettingsPageShows(MerchantOnboarding, OwnerSetting.SizeEstimateField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cardIssuingsPage, Is.EqualTo(((string?)"ConnectionDefault", (string?)StoryPoints, (string?)StoryPoints)));
                Assert.That(merchantOnboardingsPage, Is.EqualTo(((string?)"Own", (string?)StoryPoints, (string?)TShirtSize)));
            }
        }

        // A Portfolio's default size, as a new Portfolio is created with it.
        private const int ThePortfoliosDefaultSize = 25;

        [Test]
        [Category("ac-9.3")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_following_Feature_with_no_size_in_the_default_field_takes_the_Portfolios_default_size()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosSizeEstimateField, StoryPoints, startUpdates: true);
            ThenTheSaveIsAccepted();
            await WhatRanSinceTheSave(CardIssuing);
            var shown = await WhatTheFeatureOfShows(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheTrackerWasAskedToReadFor(CardIssuing, OwnerSetting.SizeEstimateField), Is.EqualTo(StoryPoints));
                Assert.That(shown.SizedAt, Is.EqualTo(ThePortfoliosDefaultSize), "An empty value sizes the Feature as an empty own field does.");
            }
        }

        [Test]
        [Category("ac-9.2")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_that_owns_its_size_field_starts_no_update_when_the_default_changes()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosSizeEstimateField, StoryPoints, startUpdates: true);
            var ran = await WhatRanSinceTheSave(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Has.Some.StartsWith(CardIssuing), "The follower did update.");
                Assert.That(ran, Has.None.StartsWith(MerchantOnboarding));
            }
        }
    }
}
