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
        private const string Settlements = "Settlements";

        [Test]
        public async Task A_following_Feature_is_owned_by_the_Team_the_default_field_names()
        {
            GivenEveryFeatureNamesItsOwnerIn(ReleaseTrain, Ledger);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosFeatureOwnerField, ReleaseTrain, startUpdates: true);
            ThenTheSaveIsAccepted();
            await WhatRanSinceTheSave(CardIssuing);
            var shown = await WhatTheFeatureOfShows(CardIssuing);

            Assert.That(shown.SplitAcross, Is.EqualTo(new[] { Ledger }));
        }

        [Test]
        public async Task A_following_Feature_on_a_CSV_connection_is_owned_by_the_Team_the_default_column_names()
        {
            await GivenAConnectionOfEveryOtherTracker();
            await GivenAPortfolioOn(PaymentsCsv, Settlements);
            GivenEveryFeatureNamesItsOwnerIn(ReleaseTrain, Ledger);
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefaultOn(PaymentsCsv, ConnectionDefault.PortfoliosFeatureOwnerField, ReleaseTrain);
            ThenTheSaveIsAccepted();
            await WhenTheNextScheduledUpdateOf(Settlements);
            var shown = await WhatTheFeatureOfShows(Settlements);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheTrackerWasAskedToReadFor(Settlements, OwnerSetting.FeatureOwnerField), Is.EqualTo(ReleaseTrain));
                Assert.That(shown.SplitAcross, Is.EqualTo(new[] { Ledger }));
            }
        }

        [Test]
        public async Task A_following_Feature_with_no_owner_in_the_default_field_is_shared_by_every_Team()
        {
            GivenEveryFeatureNamesItsOwnerIn(ProductOwner, Ledger);
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
    }
}
