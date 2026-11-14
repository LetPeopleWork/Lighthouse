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

        // A Portfolio's default size, as a new Portfolio is created with it.
        private const int ThePortfoliosDefaultSize = 25;

        private const int TheSizeAFieldHolds = 8;

        [Test]
        public async Task A_following_Feature_is_sized_at_what_the_default_field_holds()
        {
            GivenEveryFeatureHoldsTheSizeIn(StoryPoints, TheSizeAFieldHolds);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosSizeEstimateField, StoryPoints, startUpdates: true);
            ThenTheSaveIsAccepted();
            await WhatRanSinceTheSave(CardIssuing);
            var shown = await WhatTheFeatureOfShows(CardIssuing);

            Assert.That(shown.SizedAt, Is.EqualTo(TheSizeAFieldHolds));
        }

        [Test]
        public async Task A_following_Feature_with_no_size_in_the_default_field_takes_the_Portfolios_default_size()
        {
            GivenEveryFeatureHoldsTheSizeIn(TShirtSize, TheSizeAFieldHolds);
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
    }
}
