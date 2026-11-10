using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
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
    /// The Portfolio metrics read a Portfolio's Features and their state transitions through named reads. Each named
    /// read must give exactly what the predicate it stands for gives through the same repository: the same Features
    /// or transitions in the same sequence, with the same related data loaded, still tracked, on SQLite and on
    /// Postgres alike. Every read runs in a context of its own, so a navigation loaded by an earlier read cannot fill
    /// in one the read under test failed to load.
    /// </summary>
    [TestFixture]
    public class PortfolioMetricsReadsParityOnBothProvidersTests : IntegrationFixture
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
        public async Task Every_named_Portfolio_read_gives_what_its_predicate_gives_on_Sqlite()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-portfolio-reads-parity-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildProvider(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", DatabaseConfigurator.ConfigureSqlite));

                var instance = await SeedTwoPortfoliosWithFeaturesInEveryCategoryAndTransitions(provider);

                ThenEveryNamedReadGivesWhatItsPredicateGives(provider, instance, "SQLite");
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
        public async Task Every_named_Portfolio_read_gives_what_its_predicate_gives_on_Postgres()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            await using var provider = BuildProvider(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery).MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            var instance = await SeedTwoPortfoliosWithFeaturesInEveryCategoryAndTransitions(provider);

            ThenEveryNamedReadGivesWhatItsPredicateGives(provider, instance, "Postgres");
        }

        private sealed record SeededInstance(IReadOnlyList<int> PortfolioIds, HashSet<int> FeatureIdsWithTransitions);

        private sealed record ReadComparison(string Read, string ByName, string ByPredicate);

        private static void ThenEveryNamedReadGivesWhatItsPredicateGives(ServiceProvider provider, SeededInstance instance, string database)
        {
            var comparisons = instance.PortfolioIds.SelectMany(portfolioId => ComparisonsForPortfolio(provider, portfolioId))
                .Append(TransitionsComparison(provider, instance.FeatureIdsWithTransitions))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                foreach (var comparison in comparisons)
                {
                    Assert.That(comparison.ByPredicate, Is.Not.Empty,
                        $"On {database}, the predicate behind '{comparison.Read}' found nothing, so the comparison would pass without looking at anything. Seed rows that the predicate selects.");
                    Assert.That(comparison.ByName, Is.EqualTo(comparison.ByPredicate),
                        $"On {database}, the named read '{comparison.Read}' gave other Features or transitions, another order, other loaded related data or other tracking than the predicate it replaces. " +
                        "The Portfolio metrics would then answer differently once they switch to it. Make the named read keep the predicate, the includes and the tracking of today's read.");
                }
            }
        }

        private static IEnumerable<ReadComparison> ComparisonsForPortfolio(ServiceProvider provider, int portfolioId)
        {
            yield return new ReadComparison($"done Features of Portfolio {portfolioId}",
                DescribeFeatures(provider, repository => repository.DoneFeaturesOfPortfolio(portfolioId)),
                DescribeFeatures(provider, repository => repository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolioId) &&
                    f.StateCategory == StateCategories.Done).ToList()));

            yield return new ReadComparison($"doing or done Features of Portfolio {portfolioId}, written doing first",
                DescribeFeatures(provider, repository => repository.DoingOrDoneFeaturesOfPortfolio(portfolioId)),
                DescribeFeatures(provider, repository => repository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolioId) &&
                    (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done)).ToList()));

            yield return new ReadComparison($"doing or done Features of Portfolio {portfolioId}, written done first",
                DescribeFeatures(provider, repository => repository.DoingOrDoneFeaturesOfPortfolio(portfolioId)),
                DescribeFeatures(provider, repository => repository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolioId) &&
                    (f.StateCategory == StateCategories.Done || f.StateCategory == StateCategories.Doing)).ToList()));

            yield return new ReadComparison($"to do or doing Features of Portfolio {portfolioId}",
                DescribeFeatures(provider, repository => repository.ToDoOrDoingFeaturesOfPortfolio(portfolioId)),
                DescribeFeatures(provider, repository => repository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolioId) &&
                    (f.StateCategory == StateCategories.ToDo || f.StateCategory == StateCategories.Doing)).ToList()));

            yield return new ReadComparison($"to do, doing or done Features of Portfolio {portfolioId}",
                DescribeFeatures(provider, repository => repository.ToDoDoingOrDoneFeaturesOfPortfolio(portfolioId)),
                DescribeFeatures(provider, repository => repository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolioId) &&
                    (f.StateCategory == StateCategories.Done ||
                     f.StateCategory == StateCategories.ToDo ||
                     f.StateCategory == StateCategories.Doing)).ToList()));

            yield return new ReadComparison($"every Feature of Portfolio {portfolioId}",
                DescribeFeatures(provider, repository => repository.EveryFeatureOfPortfolio(portfolioId)),
                DescribeFeatures(provider, repository => repository.GetAllByPredicate(f => f.Portfolios.Any(p => p.Id == portfolioId)).ToList()));
        }

        private static ReadComparison TransitionsComparison(ServiceProvider provider, HashSet<int> featureIds)
            => new("transitions of the chosen Features",
                DescribeTransitions(provider, repository => repository.TransitionsOfFeatures(featureIds)),
                DescribeTransitions(provider, repository => repository.GetAllByPredicate(transition => featureIds.Contains(transition.FeatureId)).ToList()));

        private static string DescribeTransitions(ServiceProvider provider, Func<FeatureStateTransitionRepository, IReadOnlyList<FeatureStateTransition>> read)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var transitions = read(new FeatureStateTransitionRepository(context, NullLogger<FeatureStateTransitionRepository>.Instance));

            return string.Join("\n", transitions.Select(transition =>
                $"{transition.Id} Feature {transition.FeatureId} {transition.FromState}->{transition.ToState} {context.Entry(transition).State}"));
        }

        private static string DescribeFeatures(ServiceProvider provider, Func<FeatureRepository, IReadOnlyList<Feature>> read)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var features = read(new FeatureRepository(context, FeatureOrderingTestHelper.FollowingTheTracker(), NullLogger<FeatureRepository>.Instance));

            return string.Join("\n", features.Select(feature =>
            {
                var entry = context.Entry(feature);
                return string.Join(" | ",
                    $"{feature.Id} {feature.ReferenceId} {entry.State}",
                    $"portfolios {Loaded(entry.Collection(f => f.Portfolios).IsLoaded)} {string.Join(",", feature.Portfolios.Select(portfolio => portfolio.Name).Order(StringComparer.Ordinal))}",
                    $"work {Loaded(entry.Collection(f => f.FeatureWork).IsLoaded)} {string.Join(",", feature.FeatureWork.Select(work => $"{(context.Entry(work).Reference(w => w.Team).IsLoaded ? work.Team.Name : "Team not loaded")}:{work.TotalWorkItems}").Order(StringComparer.Ordinal))}",
                    $"forecasts {Loaded(entry.Collection(f => f.Forecasts).IsLoaded)} {string.Join(",", feature.Forecasts.Select(forecast => SimulationOf(context, forecast)).Order(StringComparer.Ordinal))}",
                    $"start forecasts {Loaded(entry.Collection(f => f.StartForecasts).IsLoaded)} {string.Join(",", feature.StartForecasts.Select(forecast => SimulationOf(context, forecast)).Order(StringComparer.Ordinal))}",
                    $"depends on {Loaded(entry.Collection(f => f.DependsOnReferences).IsLoaded)} {string.Join(",", feature.DependsOnReferences.Select(reference => reference.ReferenceId).Order(StringComparer.Ordinal))}");
            }));
        }

        private static string SimulationOf(LighthouseAppContext context, ForecastBase forecast)
            => $"{forecast.TotalTrials}[{Loaded(context.Entry(forecast).Collection(f => f.SimulationResults).IsLoaded)} {string.Join(";", forecast.SimulationResults.OrderBy(day => day.Key).Select(day => $"{day.Key}:{day.Value}"))}]";

        private static string Loaded(bool isLoaded) => isLoaded ? "loaded" : "not loaded";

        private static async Task<SeededInstance> SeedTwoPortfoliosWithFeaturesInEveryCategoryAndTransitions(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var atlas = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var borealis = new Team { Name = "Borealis", WorkTrackingSystemConnection = AConnection("Borealis") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };
            var payments = new Portfolio { Name = "Payments", WorkTrackingSystemConnection = AConnection("Payments") };

            var checkout = new Feature([(atlas, 3, 3), (borealis, 2, 2)]) { Name = "Checkout", ReferenceId = "CHECKOUT-1", Order = "1", StateCategory = StateCategories.Doing };
            checkout.Portfolios.AddRange([platform, payments]);
            AddForecasts(checkout, atlas, borealis);

            var search = new Feature(atlas, 2) { Name = "Search", ReferenceId = "SEARCH-1", Order = "2", StateCategory = StateCategories.Done };
            search.Portfolios.Add(platform);
            AddForecasts(search, atlas);

            var reporting = new Feature([(atlas, 0, 0), (borealis, 4, 4)]) { Name = "Reporting", ReferenceId = "REPORTING-1", Order = "3", StateCategory = StateCategories.ToDo };
            reporting.Portfolios.Add(platform);
            AddForecasts(reporting, borealis);

            var inbox = new Feature([(atlas, 2, 2), (borealis, 1, 1)]) { Name = "Inbox", ReferenceId = "INBOX-1", Order = "4", StateCategory = StateCategories.Unknown };
            inbox.Portfolios.Add(platform);

            var billing = new Feature([new FeatureDependencyReference(0, "CHECKOUT-1", DependencySource.TrackerLink)])
            {
                Name = "Billing", ReferenceId = "BILLING-1", Order = "5", StateCategory = StateCategories.ToDo,
            };
            billing.FeatureWork.Add(new FeatureWork(atlas, 5, 5, billing));
            billing.Portfolios.Add(payments);
            AddForecasts(billing, atlas);

            var archive = new Feature([(atlas, 1, 1), (borealis, 1, 1)]) { Name = "Archive", ReferenceId = "ARCHIVE-1", Order = "6", StateCategory = StateCategories.Done };
            archive.Portfolios.Add(payments);
            AddForecasts(archive, borealis);

            var refunds = new Feature(borealis, 3) { Name = "Refunds", ReferenceId = "REFUNDS-1", Order = "7", StateCategory = StateCategories.Doing };
            refunds.Portfolios.Add(payments);

            var triage = new Feature(borealis, 1) { Name = "Triage", ReferenceId = "TRIAGE-1", Order = "8", StateCategory = StateCategories.Unknown };
            triage.Portfolios.Add(payments);

            var orphan = new Feature(atlas, 4) { Name = "Orphan", ReferenceId = "ORPHAN-1", Order = "9", StateCategory = StateCategories.Doing };
            AddForecasts(orphan, atlas);

            context.Features.AddRange(checkout, search, reporting, inbox, billing, archive, refunds, triage, orphan);
            await context.SaveChangesAsync();

            var featuresWithTransitions = new[] { checkout, search, archive, refunds };
            foreach (var feature in featuresWithTransitions.Append(orphan))
            {
                context.FeatureStateTransitions.AddRange(
                    new FeatureStateTransition { FeatureId = feature.Id, FromState = "New", ToState = "Active", TransitionedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc) },
                    new FeatureStateTransition { FeatureId = feature.Id, FromState = "Active", ToState = "Closed", TransitionedAt = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc) });
            }

            await context.SaveChangesAsync();

            return new SeededInstance([platform.Id, payments.Id], featuresWithTransitions.Select(feature => feature.Id).ToHashSet());
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
