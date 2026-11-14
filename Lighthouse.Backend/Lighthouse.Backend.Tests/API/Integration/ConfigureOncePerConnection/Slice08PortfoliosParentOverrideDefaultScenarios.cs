namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// The Portfolios get a Parent sources default of their own, separate from the Teams' one. Card
    /// Issuing names nothing and follows it; Merchant Onboarding names "Parent Link" itself and is left alone.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class Slice08PortfoliosParentOverrideDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private static readonly string[] CardIssuingDownloadsEverything = [$"{CardIssuing} Full"];

        [Test]
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
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("Portfolios' Parent sources default").And.Some.Contains("1 Portfolio follows it: Card Issuing"));
            }
        }
    }
}
