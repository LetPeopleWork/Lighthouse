using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// The Portfolios' Dependency Field default: a following Portfolio reads its dependencies from what the
    /// default names, field or link, exactly as if it had named it itself. Ignore Dependencies stays each
    /// Portfolio's own choice.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    [Category("slice-09")]
    [Category("us-08")]
    public class Slice09PortfoliosDependencyFieldDefaultScenarios : ConnectionDefaultsAcceptanceTest
    {
        private static readonly string[] CardIssuingDownloadsEverything = [$"{CardIssuing} Full"];

        [Test]
        [Category("ac-8.1")]
        [Category("ac-8.2")]
        [Category("ac-8.4")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_following_Portfolio_reads_its_dependencies_from_the_default_and_an_owner_is_left_alone()
        {
            await GivenEveryTeamAndPortfolioHasBeenUpdatedOnce();

            await WhenTheAdminSetsTheDefault(ConnectionDefault.PortfoliosDependencyField, ReleaseTrain, startUpdates: true);
            ThenTheSaveIsAccepted();
            var ran = await WhatRanSinceTheSave(CardIssuing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ran, Is.EqualTo(CardIssuingDownloadsEverything));
                Assert.That(WhatTheTrackerWasAskedToReadFor(CardIssuing, OwnerSetting.DependencyField), Is.EqualTo(ReleaseTrain));
                Assert.That(WhatTheTrackerWasAskedToReadFor(MerchantOnboarding, OwnerSetting.DependencyField), Is.EqualTo(BlockedByKeys));
                Assert.That(TheLogEntriesAboutChangedDefaults(), Has.Some.Contains("1 Portfolio follows it: Card Issuing"));
            }
        }

        [Test]
        [Category("ac-8.3")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Ignore_Dependencies_is_never_taken_from_the_connection()
        {
            await GivenTheDefaultIs(ConnectionDefault.PortfoliosDependencyField, ReleaseTrain);

            var page = await WhatTheConnectionPageReads(JiraDcPayments);
            var offered = await WhatTheConnectionOffersAsDefaults(JiraDcPayments);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheDefaultNamedOnTheConnection(ConnectionDefault.PortfoliosDependencyField), Is.EqualTo(ReleaseTrain));
                Assert.That(offered, Is.Not.Null.And.Contains("dependencyFieldForPortfolios"));
                Assert.That(page["defaults"], Is.InstanceOf<JsonObject>(), "The connection page reads the connection's defaults.");
                Assert.That(page["defaults"]?["ignoreDependenciesForPortfolios"], Is.Null);
                Assert.That(offered, Is.Not.Null.And.None.Contains("ignoreDependencies"));
            }
        }
    }
}
