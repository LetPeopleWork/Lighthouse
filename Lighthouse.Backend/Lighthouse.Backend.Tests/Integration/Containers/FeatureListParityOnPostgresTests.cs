using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The Features list reads the same Features the plain read does, and reads them the same way on every database
    /// Lighthouse runs on. For one seeded instance, each Feature the list reads is described by what the product
    /// derives from it - its dates, its Teams, its Portfolios, its start histogram - and that description must equal
    /// the one the plain read gives, on SQLite and on Postgres alike.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureListParityOnPostgresTests
    {
        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [Test]
        public async Task The_list_of_a_Portfolio_and_the_list_of_a_Team_read_each_Feature_as_the_plain_read_does_on_Sqlite()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-list-parity-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildProvider(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

                var parity = await TheListsAgainstThePlainRead(provider);

                Assert.That(parity.OfTheList, Is.EqualTo(parity.OfThePlainRead));
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task The_list_of_a_Portfolio_and_the_list_of_a_Team_read_each_Feature_as_the_plain_read_does_on_Postgres()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            await using var provider = BuildProvider(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            var parity = await TheListsAgainstThePlainRead(provider);

            Assert.That(parity.OfTheList, Is.EqualTo(parity.OfThePlainRead));
        }

        [Test]
        public async Task The_two_databases_give_the_same_description_of_the_same_lists()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-list-parity-{Guid.NewGuid():N}.db");
            try
            {
                await using var sqliteProvider = BuildProvider(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));
                var onSqlite = await TheListsAgainstThePlainRead(sqliteProvider);

                postgres ??= await SharedPostgresContainer.StartAsync();
                var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
                await using var postgresProvider = BuildProvider(options =>
                    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));
                var onPostgres = await TheListsAgainstThePlainRead(postgresProvider);

                Assert.That(onPostgres.OfTheList, Is.EqualTo(onSqlite.OfTheList));
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private sealed record Parity(string OfTheList, string OfThePlainRead);

        private static async Task<Parity> TheListsAgainstThePlainRead(ServiceProvider provider)
        {
            var (platformId, atlasId) = await SeedTheInstance(provider);

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var repository = new FeatureRepository(context, FeatureOrderingTestHelper.FollowingTheTracker(), NullLogger<FeatureRepository>.Instance);

            var plain = repository.GetAll().ToList();

            var ofThePortfolio = Describe(repository.FeaturesOfPortfolio(platformId), withoutTeamStartForecasts: true);
            var ofTheTeam = Describe(repository.FeaturesOfTeam(atlasId), withoutTeamStartForecasts: true);

            var plainOfThePortfolio = Describe(plain.Where(feature => feature.Portfolios.Any(portfolio => portfolio.Id == platformId)).ToList(), withoutTeamStartForecasts: true);
            var plainOfTheTeam = Describe(plain.Where(feature => feature.FeatureWork.Any(work => work.TeamId == atlasId)).ToList(), withoutTeamStartForecasts: true);

            return new Parity(
                $"portfolio\n{ofThePortfolio}\nteam\n{ofTheTeam}",
                $"portfolio\n{plainOfThePortfolio}\nteam\n{plainOfTheTeam}");
        }

        private static string Describe(IReadOnlyList<Feature> features, bool withoutTeamStartForecasts)
            => string.Join("\n", features.Select(feature => string.Join(" | ",
                feature.ReferenceId,
                $"portfolios={string.Join(",", feature.Portfolios.Select(portfolio => portfolio.Name).Order(StringComparer.Ordinal))}",
                $"teams={string.Join(",", feature.FeatureWork.Select(work => $"{work.Team.Name}:{work.TotalWorkItems}").Order(StringComparer.Ordinal))}",
                $"p50={feature.Forecast.GetPercentile(50)} p85={feature.Forecast.GetPercentile(85)}",
                $"completions={string.Join(",", feature.Forecasts.Select(forecast => HistogramOf(forecast)).Order(StringComparer.Ordinal))}",
                $"start={string.Join(",", feature.StartForecasts.Where(forecast => !withoutTeamStartForecasts || forecast.TeamId is null).Select(forecast => HistogramOf(forecast)))}")));

        private static string HistogramOf(ForecastBase forecast)
            => $"{forecast.TotalTrials}[{string.Join(";", forecast.SimulationResults.OrderBy(day => day.Key).Select(day => $"{day.Key}:{day.Value}"))}]";

        private static async Task<(int PlatformId, int AtlasId)> SeedTheInstance(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

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

            return (platform.Id, atlas.Id);
        }

        private static void AddForecasts(Feature feature, params Team[] teams)
        {
            var createdAt = new DateTime(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);

            foreach (var team in teams)
            {
                var completion = new WhenForecast { Team = team, TotalTrials = 100, CreationTime = createdAt };
                completion.SimulationResults.Add(new IndividualSimulationResult { Key = 10 + team.Name.Length, Value = 60 });
                completion.SimulationResults.Add(new IndividualSimulationResult { Key = 20 + team.Name.Length, Value = 40 });
                feature.Forecasts.Add(completion);
                feature.StartForecasts.Add(new StartForecast(new Dictionary<int, int> { [3] = 100 }, team, 100, createdAt));
            }

            feature.StartForecasts.Add(new StartForecast(new Dictionary<int, int> { [2] = 60, [6] = 40 }, null, 100, createdAt));
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };

        private static ServiceProvider BuildProvider(Action<DbContextOptionsBuilder> useDatabase)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(useDatabase);

            return services.BuildServiceProvider();
        }
    }
}
