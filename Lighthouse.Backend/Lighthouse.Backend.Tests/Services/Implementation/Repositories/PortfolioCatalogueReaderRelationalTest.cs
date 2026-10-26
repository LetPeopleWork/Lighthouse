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
    /// What naming and counting Portfolios and Teams reads on a real relational database: each Portfolio with its
    /// connection, its Features and the Teams working on them, and no forecast at all.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-02")]
    public class PortfolioCatalogueReaderRelationalTest
    {
        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;

        private static readonly string[] PlatformThenPayments = ["Platform", "Payments"];
        private static readonly string[] CheckoutThenSearch = ["Checkout", "Search"];
        private static readonly string[] AtlasThenBorealis = ["Atlas", "Borealis"];
        private static readonly string[] TheOneOptionKey = ["Url"];
        private static readonly string[] TheOneFieldName = ["Risk"];

        private int platformId;

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

            await SeedTwoPortfoliosWithForecasts();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void Every_Portfolio_comes_back_with_its_Features_and_the_Teams_working_on_them()
        {
            var read = TheRepository(out _).AllWithoutForecasts();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.Select(portfolio => portfolio.Name), Is.EquivalentTo(PlatformThenPayments));

                var platform = read.Single(portfolio => portfolio.Name == "Platform");
                Assert.That(platform.Features.Select(feature => feature.Name), Is.EquivalentTo(CheckoutThenSearch));
                Assert.That(platform.Teams.Select(team => team.Name), Is.EquivalentTo(AtlasThenBorealis));
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void The_connection_and_its_options_come_with_each_Portfolio()
        {
            var platform = TheRepository(out _).AllWithoutForecasts().Single(portfolio => portfolio.Name == "Platform");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(platform.WorkTrackingSystemConnection.Name, Is.EqualTo("Platform Connection"));
                Assert.That(platform.WorkTrackingSystemConnection.Options.Select(option => option.Key), Is.EqualTo(TheOneOptionKey));
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void No_forecast_and_no_histogram_row_is_read_although_the_Features_hold_them()
        {
            var read = TheRepository(out var context).AllWithoutForecasts();
            var features = read.SelectMany(portfolio => portfolio.Features).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Is.Not.Empty);
                Assert.That(features.SelectMany(feature => feature.Forecasts), Is.Empty);
                Assert.That(features.SelectMany(feature => feature.StartForecasts), Is.Empty);
                Assert.That(context.ChangeTracker.Entries<WhenForecast>(), Is.Empty);
                Assert.That(context.ChangeTracker.Entries<IndividualSimulationResult>(), Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_Team_working_in_two_Features_is_one_Team_object()
        {
            var platform = TheRepository(out _).AllWithoutForecasts().Single(portfolio => portfolio.Name == "Platform");

            var atlasInEachFeature = platform.Features
                .SelectMany(feature => feature.FeatureWork)
                .Select(work => work.Team)
                .Where(team => team.Name == "Atlas")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(atlasInEachFeature, Has.Count.EqualTo(2));
                Assert.That(atlasInEachFeature[0], Is.SameAs(atlasInEachFeature[1]));
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void Nothing_read_is_tracked_so_nothing_can_reach_a_save()
        {
            TheRepository(out var context).AllWithoutForecasts();
            TheRepository(out var otherContext).OneWithoutForecasts(platformId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ChangeTracker.Entries(), Is.Empty);
                Assert.That(otherContext.ChangeTracker.Entries(), Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void One_Portfolio_comes_back_alone_read_the_way_the_whole_list_reads_it()
        {
            var one = TheRepository(out _).OneWithoutForecasts(platformId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(one, Is.Not.Null);
                Assert.That(one!.Name, Is.EqualTo("Platform"));
                Assert.That(one.Features.Select(feature => feature.Name), Is.EquivalentTo(CheckoutThenSearch));
                Assert.That(one.Teams.Select(team => team.Name), Is.EquivalentTo(AtlasThenBorealis));
                Assert.That(one.Features.SelectMany(feature => feature.Forecasts), Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_Portfolio_that_does_not_exist_comes_back_as_nothing()
        {
            Assert.That(TheRepository(out _).OneWithoutForecasts(int.MaxValue), Is.Null);
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void The_plain_read_still_brings_the_forecasts()
        {
            var features = TheRepository(out _).GetById(platformId)!.Features;

            Assert.That(features.SelectMany(feature => feature.Forecasts), Is.Not.Empty);
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public async Task The_owning_Team_comes_back_even_when_it_works_none_of_the_Features()
        {
            int portfolioId;
            int ownerId;
            using (var context = AContext())
            {
                var idleOwner = new Team { Name = "Idle owner", WorkTrackingSystemConnection = AConnection("Idle owner") };
                var owned = new Portfolio { Name = "Owned", WorkTrackingSystemConnection = AConnection("Owned"), OwningTeam = idleOwner };
                context.Portfolios.Add(owned);
                await context.SaveChangesAsync();
                portfolioId = owned.Id;
                ownerId = idleOwner.Id;
            }

            var one = TheRepository(out _).OneWithoutForecasts(portfolioId);

            Assert.That(one!.OwningTeam?.Id, Is.EqualTo(ownerId));
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public async Task The_additional_field_definitions_of_the_connection_come_with_the_Portfolio()
        {
            int portfolioId;
            using (var context = AContext())
            {
                var withField = new Portfolio { Name = "With field", WorkTrackingSystemConnection = AConnection("With field") };
                withField.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = "Risk", Reference = "customfield_1" });
                context.Portfolios.Add(withField);
                await context.SaveChangesAsync();
                portfolioId = withField.Id;
            }

            var one = TheRepository(out _).OneWithoutForecasts(portfolioId);

            Assert.That(one!.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Select(field => field.DisplayName), Is.EqualTo(TheOneFieldName));
        }

        private PortfolioRepository TheRepository(out LighthouseAppContext context)
        {
            context = AContext();
            return new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance);
        }

        private LighthouseAppContext AContext()
            => new(options, new Mock<ICryptoService>().Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task SeedTwoPortfoliosWithForecasts()
        {
            using var context = AContext();
            var atlas = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var borealis = new Team { Name = "Borealis", WorkTrackingSystemConnection = AConnection("Borealis") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };
            var payments = new Portfolio { Name = "Payments", WorkTrackingSystemConnection = AConnection("Payments") };

            var checkout = new Feature { Name = "Checkout", ReferenceId = "CHECKOUT-1", Order = "1" };
            checkout.FeatureWork.Add(new FeatureWork(atlas, 3, 3, checkout));
            checkout.FeatureWork.Add(new FeatureWork(borealis, 2, 2, checkout));
            checkout.Portfolios.Add(platform);
            AddForecasts(checkout, atlas, borealis);

            var search = new Feature(atlas, 2) { Name = "Search", ReferenceId = "SEARCH-1", Order = "2" };
            search.Portfolios.Add(platform);
            AddForecasts(search, atlas);

            var paymentProvider = new Feature(borealis, 2) { Name = "Payment provider", ReferenceId = "PAYMENTS-1", Order = "3" };
            paymentProvider.Portfolios.Add(payments);
            AddForecasts(paymentProvider, borealis);

            context.Features.AddRange(checkout, search, paymentProvider);
            await context.SaveChangesAsync();

            platformId = platform.Id;
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
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
        {
            var tracker = new WorkTrackingSystemConnection { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };
            tracker.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Url", Value = "https://tracker.example", IsSecret = false });

            return tracker;
        }
    }
}
