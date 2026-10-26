using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
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
    /// What a forecast round gathers on a real relational database: the Features the Teams work in, tracked so the
    /// round can replace their forecasts, read without any histogram; and the Teams a Portfolio's Features name.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-05")]
    public class ForecastGatherReaderRelationalTest
    {
        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;
        private static readonly string[] SearchThenCheckout = ["SEARCH-1", "CHECKOUT-1"];
        private static readonly string[] SearchCheckoutThenBilling = ["SEARCH-1", "CHECKOUT-1", "BILLING-1"];
        private static readonly string[] PlatformAndPayments = ["Platform", "Payments"];
        private static readonly string[] JustAtlas = ["Atlas"];
        private static readonly string[] JustSearch = ["SEARCH-1"];
        private static readonly int[] TwoHundredTrials = [200];

        private int atlasId;
        private int borealisId;
        private int platformId;
        private int paymentsId;
        private int lonelyId;

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

            await Seed();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void Only_the_Features_one_of_the_Teams_works_in_come_back_in_the_order_the_ordering_gives_them()
        {
            var read = TheFeaturesWorkedOnBy([atlasId], out _);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(SearchThenCheckout));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Feature_is_read_once_however_many_of_the_asked_Teams_work_in_it()
        {
            var read = TheFeaturesWorkedOnBy([atlasId, borealisId], out _);

            Assert.That(read.Select(feature => feature.ReferenceId), Is.EqualTo(SearchCheckoutThenBilling));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void No_Team_asked_for_means_no_Feature()
        {
            Assert.That(TheFeaturesWorkedOnBy([], out _), Is.Empty);
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void Each_Feature_brings_what_a_round_reads_and_the_headers_of_the_forecasts_it_replaces()
        {
            var read = TheFeaturesWorkedOnBy([atlasId], out _);
            var checkout = read.Single(feature => feature.ReferenceId == "CHECKOUT-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(checkout.Portfolios.Select(portfolio => portfolio.Name), Is.EquivalentTo(PlatformAndPayments));
                Assert.That(checkout.FeatureWork.Select(work => work.Team.Name), Is.EqualTo(JustAtlas));
                Assert.That(checkout.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(JustSearch));
                Assert.That(checkout.Forecasts.Select(forecast => forecast.TotalTrials), Is.EqualTo(TwoHundredTrials));
                Assert.That(checkout.StartForecasts, Has.Count.EqualTo(1));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void No_histogram_of_any_forecast_is_read()
        {
            var read = TheFeaturesWorkedOnBy([atlasId], out _);

            var forecasts = read.SelectMany(feature => feature.Forecasts).Cast<ForecastBase>()
                .Concat(read.SelectMany(feature => feature.StartForecasts)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecasts, Is.Not.Empty);
                Assert.That(forecasts.SelectMany(forecast => forecast.SimulationResults), Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Replacing_the_forecasts_that_were_read_and_saving_leaves_no_old_forecast_or_histogram_behind()
        {
            var read = TheFeaturesWorkedOnBy([atlasId], out var context);
            var checkout = read.Single(feature => feature.ReferenceId == "CHECKOUT-1");
            var replacement = new WhenForecast { Team = checkout.FeatureWork[0].Team, TotalTrials = 50, CreationTime = DateTime.UtcNow };

            checkout.SetFeatureForecasts([replacement]);
            checkout.SetStartForecasts([]);
            await context.SaveChangesAsync();

            using var afterwards = AContext();
            var stored = afterwards.Features.Include(feature => feature.Forecasts).Include(feature => feature.StartForecasts)
                .Single(feature => feature.ReferenceId == "CHECKOUT-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Forecasts.Select(forecast => forecast.TotalTrials), Is.EqualTo(new List<int> { 50 }));
                Assert.That(stored.StartForecasts, Is.Empty);
                Assert.That(afterwards.Set<IndividualSimulationResult>().Count(row => !afterwards.Set<ForecastBase>().Any(forecast => forecast.Id == row.ForecastId)), Is.Zero,
                    "A histogram row outlived the forecast it belonged to.");
                Assert.That(afterwards.Set<IndividualSimulationResult>().Count(row =>
                    afterwards.Set<WhenForecast>().Any(forecast => forecast.Id == row.ForecastId && forecast.FeatureId == stored.Id)
                    || afterwards.Set<StartForecast>().Any(forecast => forecast.Id == row.ForecastId && forecast.FeatureId == stored.Id)), Is.Zero,
                    "The replaced Feature still has histogram rows.");
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void What_was_read_is_tracked_so_a_round_can_save_it()
        {
            var read = TheFeaturesWorkedOnBy([atlasId], out var context);

            Assert.That(context.ChangeTracker.Entries<Feature>().Select(entry => entry.Entity), Is.EquivalentTo(read));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void The_Teams_of_a_Portfolio_are_the_Teams_working_on_its_Features_each_once()
        {
            using var context = AContext();

            var teams = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance).TeamIdsOf(platformId);

            Assert.That(teams, Is.EquivalentTo(new List<int> { atlasId, borealisId }));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Portfolio_is_not_given_the_Teams_of_another_Portfolio()
        {
            using var context = AContext();

            var teams = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance).TeamIdsOf(paymentsId);

            Assert.That(teams, Is.EquivalentTo(new List<int> { atlasId }));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void A_Portfolio_no_Team_works_on_and_a_Portfolio_that_is_not_there_have_no_Teams()
        {
            using var context = AContext();
            var subject = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.TeamIdsOf(lonelyId), Is.Empty);
                Assert.That(subject.TeamIdsOf(-1), Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void Every_Portfolio_is_listed_by_id_without_reading_any_Feature_or_forecast()
        {
            using var context = AContext();

            var ids = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance).AllIds();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ids, Is.EquivalentTo(new List<int> { platformId, paymentsId, lonelyId }));
                Assert.That(context.ChangeTracker.Entries(), Is.Empty);
            }
        }

        private IReadOnlyList<Feature> TheFeaturesWorkedOnBy(int[] teamIds, out LighthouseAppContext context)
        {
            context = AContext();
            var ordering = new Mock<IFeatureOrdering>();
            ordering.Setup(o => o.Order(It.IsAny<IEnumerable<Feature>>()))
                .Returns((IEnumerable<Feature> features) => features.OrderByDescending(feature => feature.ReferenceId, StringComparer.Ordinal));

            return new FeatureRepository(context, ordering.Object, NullLogger<FeatureRepository>.Instance).FeaturesWorkedOnBy(teamIds);
        }

        private LighthouseAppContext AContext()
            => new(options, new Mock<ICryptoService>().Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task Seed()
        {
            using var context = AContext();
            var atlas = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var borealis = new Team { Name = "Borealis", WorkTrackingSystemConnection = AConnection("Borealis") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };
            var payments = new Portfolio { Name = "Payments", WorkTrackingSystemConnection = AConnection("Payments") };
            var lonely = new Portfolio { Name = "Lonely", WorkTrackingSystemConnection = AConnection("Lonely") };

            var checkout = new Feature([new FeatureDependencyReference(0, "SEARCH-1", DependencySource.TrackerLink)])
            {
                Name = "Checkout",
                ReferenceId = "CHECKOUT-1",
                Order = "1",
            };
            checkout.FeatureWork.Add(new FeatureWork(atlas, 3, 3, checkout));
            checkout.Portfolios.AddRange([platform, payments]);
            AddForecasts(checkout, atlas);

            var search = new Feature([(atlas, 2, 2), (borealis, 1, 1)]) { Name = "Search", ReferenceId = "SEARCH-1", Order = "2" };
            search.Portfolios.Add(platform);
            AddForecasts(search, atlas);

            var billing = new Feature(borealis, 4) { Name = "Billing", ReferenceId = "BILLING-1", Order = "3" };
            billing.Portfolios.Add(platform);

            var untouched = new Feature { Name = "Untouched", ReferenceId = "UNTOUCHED-1", Order = "4" };
            untouched.Portfolios.Add(lonely);

            context.Features.AddRange(checkout, search, billing, untouched);
            await context.SaveChangesAsync();

            atlasId = atlas.Id;
            borealisId = borealis.Id;
            platformId = platform.Id;
            paymentsId = payments.Id;
            lonelyId = lonely.Id;
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
