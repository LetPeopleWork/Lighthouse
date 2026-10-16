namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// The Portfolios' Feature Owner Field default: a following Portfolio's Features show the owning Team the
    /// default field holds. The Portfolio's own Owning Team setting is a different thing and is never inherited.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    [Category("slice-11")]
    [Category("us-10")]
    public class Slice11PortfoliosFeatureOwnerFieldDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private static readonly string[] CardIssuingDownloadsEverything = [$"{CardIssuing} Full"];

        [Test]
        [Ignore(PendingEpic6303.Slice11)]
        [Category("ac-10.1")]
        [Category("ac-10.2")]
        [Category("ac-10.4")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_following_Portfolio_reads_its_Feature_owners_from_the_default_and_an_owner_is_left_alone()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosFeatureOwnerField, ReleaseTrain, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.EqualTo(CardIssuingDownloadsEverything));
                Assert.That(WhatTheTrackerWasAskedToReadFor(CardIssuing, OwnerSetting.FeatureOwnerField), Is.EqualTo(ReleaseTrain));
                Assert.That(WhatTheTrackerWasAskedToReadFor(MerchantOnboarding, OwnerSetting.FeatureOwnerField), Is.EqualTo(ProductOwner));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("Feature Owner Field default").And.Some.Contains("Card Issuing"));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice11)]
        [Category("ac-10.3")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_following_Feature_with_no_owner_in_the_default_field_is_shared_by_every_Team()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosFeatureOwnerField, ReleaseTrain, startUpdates: true);
            ThenTheSaveIsAccepted();
            await WhatRanSinceTheSave(CardIssuing);
            var shown = await WhatTheFeatureOfShows(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheTrackerWasAskedToReadFor(CardIssuing, OwnerSetting.FeatureOwnerField), Is.EqualTo(ReleaseTrain));
                Assert.That(shown.SplitAcross, Is.EqualTo(EveryTeam()), "An empty value names no owner, so the work is shared as an empty own field shares it.");
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice11)]
        [Category("ac-10.4")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task The_Owning_Team_is_never_taken_from_the_connection()
        {
            await GivenTheDefaultIs(ConnectionDefault.PortfoliosFeatureOwnerField, ReleaseTrain);

            var offered = await WhatTheConnectionOffersAsDefaults(JiraDcPayments);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered, Is.Not.Null.And.Contains("featureOwnerFieldForPortfolios"));
                Assert.That(offered, Is.Not.Null.And.None.Contains("owningTeam"));
            }
        }
    }
}
