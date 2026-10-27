using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    /// <summary>
    /// What the histogram-free read of every Feature hands back on a real relational database: the graph the
    /// dependency decision reads, shared Teams and Portfolios as single objects, and no forecast results.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-03")]
    public class FeatureGraphReaderRelationalTest
    {
        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;
        private static readonly string[] SearchThenCheckout = ["SEARCH-1", "CHECKOUT-1"];
        private static readonly string[] PlatformAndPayments = ["Platform", "Payments"];
        private static readonly string[] JustAtlas = ["Atlas"];
        private static readonly string[] JustSearch = ["SEARCH-1"];
        private static readonly int[] TwoHundredTrials = [200];

        private int checkoutId;

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

            await SeedTwoPortfoliosSharingATeam();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void Every_Feature_comes_back_in_the_order_the_ordering_gives_it()
        {
            var read = TheGraph(out _);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(SearchThenCheckout));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void Each_Feature_brings_what_the_dependency_decision_reads_and_the_forecast_headers()
        {
            var read = TheGraph(out _);
            var checkout = read.Single(feature => feature.Id == checkoutId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(checkout.Portfolios.Select(portfolio => portfolio.Name), Is.EquivalentTo(PlatformAndPayments));
                Assert.That(checkout.FeatureWork.Select(work => work.Team.Name), Is.EqualTo(JustAtlas));
                Assert.That(checkout.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(JustSearch));
                Assert.That(checkout.Forecasts.Select(forecast => forecast.TotalTrials), Is.EqualTo(TwoHundredTrials));
                Assert.That(checkout.CanBeForecast, Is.True);
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void No_forecast_result_and_no_start_forecast_is_read()
        {
            var read = TheGraph(out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.SelectMany(feature => feature.Forecasts).SelectMany(forecast => forecast.SimulationResults), Is.Empty);
                Assert.That(read.SelectMany(feature => feature.StartForecasts), Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Team_and_a_Portfolio_shared_by_several_Features_are_one_object()
        {
            var read = TheGraph(out _);

            var teams = read.SelectMany(feature => feature.FeatureWork).Select(work => work.Team).Distinct().ToList();
            var portfolios = read.SelectMany(feature => feature.Portfolios).Distinct().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teams, Has.Count.EqualTo(1), "Each row got its own copy of the shared Team.");
                Assert.That(portfolios, Has.Count.EqualTo(2), "Each row got its own copy of a shared Portfolio.");
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void Nothing_read_is_tracked_so_nothing_can_reach_a_save()
        {
            TheGraph(out var context);

            Assert.That(context.ChangeTracker.Entries(), Is.Empty);
        }

        private IReadOnlyList<Feature> TheGraph(out LighthouseAppContext context)
        {
            context = AContext();
            var ordering = new Mock<IFeatureOrdering>();
            ordering.Setup(o => o.Order(It.IsAny<IEnumerable<Feature>>()))
                .Returns((IEnumerable<Feature> features) => features.OrderByDescending(feature => feature.ReferenceId, StringComparer.Ordinal));

            return new FeatureRepository(context, ordering.Object, NullLogger<FeatureRepository>.Instance).WholeGraphWithoutHistograms();
        }

        private LighthouseAppContext AContext()
            => new(options, new Mock<ICryptoService>().Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task SeedTwoPortfoliosSharingATeam()
        {
            using var context = AContext();
            var team = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };
            var payments = new Portfolio { Name = "Payments", WorkTrackingSystemConnection = AConnection("Payments") };

            var checkout = new Feature([new FeatureDependencyReference(0, "SEARCH-1", DependencySource.TrackerLink)])
            {
                Name = "Checkout",
                ReferenceId = "CHECKOUT-1",
                Order = "1",
            };
            checkout.FeatureWork.Add(new FeatureWork(team, 3, 3, checkout));
            checkout.Portfolios.AddRange([platform, payments]);
            AddForecasts(checkout, team);

            var search = new Feature(team, 2) { Name = "Search", ReferenceId = "SEARCH-1", Order = "2" };
            search.Portfolios.Add(platform);
            AddForecasts(search, team);

            context.Features.AddRange(checkout, search);
            await context.SaveChangesAsync();

            checkoutId = checkout.Id;
        }

        private static void AddForecasts(Feature feature, Team team)
        {
            var completion = new WhenForecast { Team = team, TotalTrials = 200, CreationTime = DateTime.UtcNow };
            completion.SimulationResults.Add(new IndividualSimulationResult { Key = 9, Value = 200 });
            feature.Forecasts.Add(completion);
            feature.StartForecasts.Add(new StartForecast(new Dictionary<int, int> { [3] = 200 }, team, 200, DateTime.UtcNow));
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };
    }
}
