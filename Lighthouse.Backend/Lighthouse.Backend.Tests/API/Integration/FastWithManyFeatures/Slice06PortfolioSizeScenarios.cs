using System.Net;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// The server answers whether a Portfolio is over the threshold, so the threshold lives in one place and the
    /// edit page's note never decides it on its own.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-06")]
    [Category("us-06")]
    [Category("story-6410")]
    public class Slice06PortfolioSizeScenarios : FastWithManyFeaturesAcceptanceTest
    {
        private const int TheThreshold = 100;

        private const string NorthwindPlatform = "Northwind Platform";
        private const string FeatureCount = "featureCount";
        private const string IsOverThreshold = "isOverThreshold";

        private const int AnOwnerThatDoesNotExist = 9999;

        private static string TheSizeOf(int portfolioId) => $"/api/latest/portfolios/{portfolioId}/size";

        [Test]
        [Category("driving_port")]
        [Category("contract-shape:pure-function")]
        public async Task A_Portfolio_whose_last_update_stored_101_Features_is_over_the_threshold_of_100()
        {
            var portfolio = GivenAPortfolioHoldingFeatures(NorthwindPlatform, 101);

            var size = await TheAnswerTo(TheSizeOf(portfolio));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(size.GetProperty(FeatureCount).GetInt32(), Is.EqualTo(101));
                Assert.That(size.GetProperty("threshold").GetInt32(), Is.EqualTo(TheThreshold));
                Assert.That(size.GetProperty(IsOverThreshold).GetBoolean(), Is.True);
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public async Task A_Portfolio_holding_exactly_100_Features_is_not_over_the_threshold()
        {
            var portfolio = GivenAPortfolioHoldingFeatures("Northwind Mobile", TheThreshold);

            var size = await TheAnswerTo(TheSizeOf(portfolio));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(size.GetProperty(FeatureCount).GetInt32(), Is.EqualTo(TheThreshold));
                Assert.That(size.GetProperty(IsOverThreshold).GetBoolean(), Is.False);
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public async Task A_Portfolio_that_was_never_updated_holds_no_Features_and_is_not_over_the_threshold()
        {
            var portfolio = GivenAPortfolio("Northwind Mobile");

            var size = await TheAnswerTo(TheSizeOf(portfolio));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(size.GetProperty(FeatureCount).GetInt32(), Is.Zero);
                Assert.That(size.GetProperty(IsOverThreshold).GetBoolean(), Is.False);
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-first-feature-row")]
        [Category("contract-shape:pure-function")]
        public async Task Counting_a_Portfolios_Features_reads_none_of_them()
        {
            var portfolio = GivenAPortfolioHoldingFeatures(NorthwindPlatform, 101);

            await TheAnswerCountingWhatTheDatabaseHandsBack(TheSizeOf(portfolio));

            Assert.That(Materialised.Of<Feature>(), Is.Zero,
                $"Counting the Features read them all. Read: {Materialised.Describe()}");
        }

        [Test]
        [Ignore(PendingEpic6400.Slice06)]
        [Category("driving_port")]
        [Category("error")]
        [Category("rbac")]
        [Category("contract-shape:pure-function")]
        public async Task A_reader_who_may_not_open_the_Portfolio_is_refused_its_size()
        {
            var platform = GivenAPortfolioHoldingFeatures(NorthwindPlatform, 101);
            var payments = GivenAPortfolio("Northwind Payments");

            var status = await TheStatusOf(TheSizeOf(platform), AViewerOfThePortfolio(payments));

            Assert.That(status, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        [Ignore(PendingEpic6400.Slice06)]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task The_size_of_a_Portfolio_that_does_not_exist_is_not_found()
        {
            var aPortfolioThatExists = await TheStatusOf(TheSizeOf(GivenAPortfolio(NorthwindPlatform)), Client);
            var aPortfolioThatDoesNot = await TheStatusOf(TheSizeOf(AnOwnerThatDoesNotExist), Client);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aPortfolioThatExists, Is.EqualTo(HttpStatusCode.OK), "Without a size for a Portfolio that exists, a missing one proves nothing.");
                Assert.That(aPortfolioThatDoesNot, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        private int GivenAPortfolioHoldingFeatures(string name, int count)
        {
            var portfolio = GivenAPortfolio(name);
            GivenThePortfolioHolds(portfolio, [.. Enumerable.Range(1, count).Select(index => new HeldFeature($"SZ-{index}", $"Feature {index}", []))]);

            return portfolio;
        }
    }
}
