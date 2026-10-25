using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    /// <summary>
    /// What the Features list of a Team reads on a real relational database: the Features the Team has work in,
    /// whichever Portfolio holds them, with every completion forecast and its histogram, and of the start forecasts
    /// only the Feature's own.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureListReaderRelationalTest
    {
        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;

        private static readonly string[] CheckoutThenSearch = ["CHECKOUT-1", "SEARCH-1"];
        private static readonly string[] CheckoutThenPayments = ["CHECKOUT-1", "PAYMENTS-1"];
        private static readonly string[] PlatformAndPayments = ["Platform", "Payments"];
        private static readonly string[] AtlasThenBorealis = ["Atlas", "Borealis"];

        private int atlasId;
        private int borealisId;

        [SetUp]
        public async Task SetUp()
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            options = new DbContextOptionsBuilder<LighthouseAppContext>().UseSqlite(connection).Options;

            using (var context = AContext())
            {
                context.Database.EnsureCreated();
            }

            await SeedTwoTeamsAcrossTwoPortfolios();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Team_gets_the_Features_it_has_work_in_across_Portfolios_in_the_order_the_ordering_gives_them()
        {
            var read = TheFeaturesOf(atlasId, out _);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(CheckoutThenSearch));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Team_sharing_a_Feature_with_another_Team_gets_it_without_the_Features_it_has_no_work_in()
        {
            var read = TheFeaturesOf(borealisId, out _);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(CheckoutThenPayments));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Team_nothing_works_in_gets_an_empty_list()
        {
            var read = TheFeaturesOf(int.MaxValue, out _);

            Assert.That(read, Is.Empty);
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void Each_Feature_brings_all_its_Portfolios_and_all_its_Teams_not_only_the_one_asked_for()
        {
            var checkout = TheFeaturesOf(atlasId, out _).Single(feature => feature.ReferenceId == "CHECKOUT-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(checkout.Portfolios.Select(portfolio => portfolio.Name), Is.EquivalentTo(PlatformAndPayments));
                Assert.That(checkout.FeatureWork.Select(work => work.Team.Name), Is.EquivalentTo(AtlasThenBorealis));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void Every_completion_forecast_is_read_with_its_histogram_and_only_the_Features_own_start_forecast_is()
        {
            var checkout = TheFeaturesOf(atlasId, out _).Single(feature => feature.ReferenceId == "CHECKOUT-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(checkout.Forecasts, Has.Count.EqualTo(2));
                Assert.That(checkout.Forecasts.SelectMany(forecast => forecast.SimulationResults).ToList(), Has.Count.EqualTo(2));
                Assert.That(checkout.StartForecasts, Has.Count.EqualTo(1));
                Assert.That(checkout.StartForecasts.Single().TeamId, Is.Null);
                Assert.That(checkout.StartForecasts.Single().SimulationResults, Has.Count.EqualTo(1));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void Nothing_read_is_tracked_so_nothing_can_reach_a_save()
        {
            TheFeaturesOf(atlasId, out var context);

            Assert.That(context.ChangeTracker.Entries(), Is.Empty);
        }

        private IReadOnlyList<Feature> TheFeaturesOf(int teamId, out LighthouseAppContext context)
        {
            context = AContext();
            var ordering = new Mock<IFeatureOrdering>();
            ordering.Setup(o => o.Order(It.IsAny<IEnumerable<Feature>>()))
                .Returns((IEnumerable<Feature> features) => features.OrderBy(feature => feature.ReferenceId, StringComparer.Ordinal));

            return new FeatureRepository(context, ordering.Object, NullLogger<FeatureRepository>.Instance).FeaturesOfTeam(teamId);
        }

        private LighthouseAppContext AContext()
            => new(options, new Mock<ICryptoService>().Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task SeedTwoTeamsAcrossTwoPortfolios()
        {
            using var context = AContext();
            var atlas = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var borealis = new Team { Name = "Borealis", WorkTrackingSystemConnection = AConnection("Borealis") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };
            var payments = new Portfolio { Name = "Payments", WorkTrackingSystemConnection = AConnection("Payments") };

            var checkout = new Feature { Name = "Checkout", ReferenceId = "CHECKOUT-1", Order = "1" };
            checkout.FeatureWork.Add(new FeatureWork(atlas, 3, 3, checkout));
            checkout.FeatureWork.Add(new FeatureWork(borealis, 2, 2, checkout));
            checkout.Portfolios.AddRange([platform, payments]);
            AddForecasts(checkout, atlas, borealis);

            var search = new Feature(atlas, 2) { Name = "Search", ReferenceId = "SEARCH-1", Order = "2" };
            search.Portfolios.Add(platform);
            AddForecasts(search, atlas);

            var paymentProvider = new Feature(borealis, 2) { Name = "Payment provider", ReferenceId = "PAYMENTS-1", Order = "3" };
            paymentProvider.Portfolios.Add(payments);
            AddForecasts(paymentProvider, borealis);

            context.Features.AddRange(checkout, search, paymentProvider);
            await context.SaveChangesAsync();

            atlasId = atlas.Id;
            borealisId = borealis.Id;
        }

        private static void AddForecasts(Feature feature, params Team[] teams)
        {
            foreach (var team in teams)
            {
                var completion = new WhenForecast { Team = team, TotalTrials = 200, CreationTime = DateTime.UtcNow };
                completion.SimulationResults.Add(new IndividualSimulationResult { Key = 9, Value = 200 });
                feature.Forecasts.Add(completion);
                feature.StartForecasts.Add(new StartForecast(new Dictionary<int, int> { [3] = 200 }, team, 200, DateTime.UtcNow));
            }

            feature.StartForecasts.Add(new StartForecast(new Dictionary<int, int> { [2] = 200 }, null, 200, DateTime.UtcNow));
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };
    }
}
