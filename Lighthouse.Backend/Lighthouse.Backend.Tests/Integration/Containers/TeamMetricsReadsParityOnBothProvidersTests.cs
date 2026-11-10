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
    /// The Team metrics read a Team's work items, their state transitions and the Features the Team really works
    /// on through named reads. Each named read must give exactly what the predicate it stands for gives through the
    /// same repository: the same items in the same sequence, with the same related data loaded, still tracked, on
    /// SQLite and on Postgres alike. Every read runs in a context of its own, so a navigation loaded by an earlier
    /// read cannot fill in one the read under test failed to load.
    /// </summary>
    [TestFixture]
    public class TeamMetricsReadsParityOnBothProvidersTests : IntegrationFixture
    {
        private static readonly StateCategories?[] EveryFeatureFilter =
            [null, StateCategories.Unknown, StateCategories.ToDo, StateCategories.Doing, StateCategories.Done];

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
        public async Task Every_named_Team_read_gives_what_its_predicate_gives_on_Sqlite()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-team-reads-parity-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildProvider(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", DatabaseConfigurator.ConfigureSqlite));

                var instance = await SeedTwoTeamsWithItemsTransitionsAndFeatures(provider);

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
        public async Task Every_named_Team_read_gives_what_its_predicate_gives_on_Postgres()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            await using var provider = BuildProvider(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery).MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            var instance = await SeedTwoTeamsWithItemsTransitionsAndFeatures(provider);

            ThenEveryNamedReadGivesWhatItsPredicateGives(provider, instance, "Postgres");
        }

        private sealed record SeededInstance(IReadOnlyList<int> TeamIds, HashSet<int> ItemIdsWithTransitions);

        private sealed record ReadComparison(string Read, string ByName, string ByPredicate);

        private static void ThenEveryNamedReadGivesWhatItsPredicateGives(ServiceProvider provider, SeededInstance instance, string database)
        {
            var comparisons = instance.TeamIds.SelectMany(teamId => ComparisonsForTeam(provider, teamId))
                .Append(TransitionsComparison(provider, instance.ItemIdsWithTransitions))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                foreach (var comparison in comparisons)
                {
                    Assert.That(comparison.ByPredicate, Is.Not.Empty,
                        $"On {database}, the predicate behind '{comparison.Read}' found nothing, so the comparison would pass without looking at anything. Seed rows that the predicate selects.");
                    Assert.That(comparison.ByName, Is.EqualTo(comparison.ByPredicate),
                        $"On {database}, the named read '{comparison.Read}' gave other items, another order, other loaded related data or other tracking than the predicate it replaces. " +
                        "The Team metrics would then answer differently once they switch to it. Make the named read keep the predicate, the include and the tracking of today's read.");
                }
            }
        }

        private static IEnumerable<ReadComparison> ComparisonsForTeam(ServiceProvider provider, int teamId)
        {
            yield return new ReadComparison($"done items of Team {teamId}",
                DescribeItems(provider, repository => repository.DoneItemsOfTeam(teamId)),
                DescribeItems(provider, repository => repository.GetAllByPredicate(i => i.TeamId == teamId && i.StateCategory == StateCategories.Done).ToList()));

            yield return new ReadComparison($"doing or done items of Team {teamId}, written doing first",
                DescribeItems(provider, repository => repository.DoingOrDoneItemsOfTeam(teamId)),
                DescribeItems(provider, repository => repository.GetAllByPredicate(i => i.TeamId == teamId && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done)).ToList()));

            yield return new ReadComparison($"doing or done items of Team {teamId}, written done first",
                DescribeItems(provider, repository => repository.DoingOrDoneItemsOfTeam(teamId)),
                DescribeItems(provider, repository => repository.GetAllByPredicate(i => i.TeamId == teamId && (i.StateCategory == StateCategories.Done || i.StateCategory == StateCategories.Doing)).ToList()));

            yield return new ReadComparison($"to do or doing items of Team {teamId}",
                DescribeItems(provider, repository => repository.ToDoOrDoingItemsOfTeam(teamId)),
                DescribeItems(provider, repository => repository.GetAllByPredicate(i => i.TeamId == teamId && (i.StateCategory == StateCategories.ToDo || i.StateCategory == StateCategories.Doing)).ToList()));

            yield return new ReadComparison($"count of to do or doing items of Team {teamId}",
                CountItems(provider, repository => repository.CountToDoOrDoingItemsOfTeam(teamId)),
                CountItems(provider, repository => repository.GetAllByPredicate(i => i.TeamId == teamId && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.ToDo)).Count()));

            yield return new ReadComparison($"count of to do or doing items of Team {teamId} against the length of the to do or doing read",
                CountItems(provider, repository => repository.CountToDoOrDoingItemsOfTeam(teamId)),
                CountItems(provider, repository => repository.ToDoOrDoingItemsOfTeam(teamId).Count));

            yield return new ReadComparison($"all items of Team {teamId}",
                DescribeItems(provider, repository => repository.ItemsOfTeam(teamId)),
                DescribeItems(provider, repository => repository.GetAllByPredicate(item => item.TeamId == teamId).ToList()));

            foreach (var onlyIn in EveryFeatureFilter)
            {
                yield return new ReadComparison($"Features with real work for Team {teamId}, only in {onlyIn?.ToString() ?? "any state"}",
                    DescribeFeatures(provider, repository => repository.FeaturesWithRealWorkForTeam(teamId, onlyIn)),
                    DescribeFeatures(provider, repository => repository.GetAllByPredicate(f =>
                        (onlyIn == null || f.StateCategory == onlyIn) &&
                        !f.IsUsingDefaultFeatureSize &&
                        f.FeatureWork.Any(fw => fw.TeamId == teamId && fw.TotalWorkItems > 0)).ToList()));
            }
        }

        private static ReadComparison TransitionsComparison(ServiceProvider provider, HashSet<int> itemIds)
            => new("transitions of the chosen items",
                DescribeTransitions(provider, repository => repository.TransitionsOfWorkItems(itemIds)),
                DescribeTransitions(provider, repository => repository.GetAllByPredicate(transition => itemIds.Contains(transition.WorkItemId)).ToList()));

        private static string DescribeItems(ServiceProvider provider, Func<WorkItemRepository, IReadOnlyList<WorkItem>> read)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var items = read(new WorkItemRepository(context, NullLogger<WorkItemRepository>.Instance));

            return string.Join("\n", items.Select(item =>
            {
                var entry = context.Entry(item);
                var team = entry.Reference(i => i.Team).IsLoaded ? item.Team.Name : "Team not loaded";
                return $"{item.Id} {item.ReferenceId} {team} {entry.State}";
            }));
        }

        private static string CountItems(ServiceProvider provider, Func<WorkItemRepository, int> count)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return count(new WorkItemRepository(context, NullLogger<WorkItemRepository>.Instance)).ToString();
        }

        private static string DescribeTransitions(ServiceProvider provider, Func<WorkItemStateTransitionRepository, IReadOnlyList<WorkItemStateTransition>> read)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var transitions = read(new WorkItemStateTransitionRepository(context, NullLogger<WorkItemStateTransitionRepository>.Instance));

            return string.Join("\n", transitions.Select(transition =>
                $"{transition.Id} item {transition.WorkItemId} {transition.FromState}->{transition.ToState} {context.Entry(transition).State}"));
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

        private static async Task<SeededInstance> SeedTwoTeamsWithItemsTransitionsAndFeatures(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var atlas = new Team { Name = "Atlas", WorkTrackingSystemConnection = AConnection("Atlas") };
            var borealis = new Team { Name = "Borealis", WorkTrackingSystemConnection = AConnection("Borealis") };
            var platform = new Portfolio { Name = "Platform", WorkTrackingSystemConnection = AConnection("Platform") };
            var payments = new Portfolio { Name = "Payments", WorkTrackingSystemConnection = AConnection("Payments") };

            var items = new List<WorkItem>();
            foreach (var round in new[] { 1, 2 })
            {
                foreach (var category in Enum.GetValues<StateCategories>())
                {
                    items.Add(AnItem(atlas, category, round));
                    items.Add(AnItem(borealis, category, round));
                }
            }

            context.WorkItems.AddRange(items);

            var checkout = new Feature([(atlas, 3, 3), (borealis, 2, 2)]) { Name = "Checkout", ReferenceId = "CHECKOUT-1", Order = "1", StateCategory = StateCategories.Doing };
            checkout.Portfolios.AddRange([platform, payments]);
            AddForecasts(checkout, atlas, borealis);

            var search = new Feature(atlas, 2) { Name = "Search", ReferenceId = "SEARCH-1", Order = "2", StateCategory = StateCategories.Done, IsUsingDefaultFeatureSize = true };
            search.Portfolios.Add(platform);

            var reporting = new Feature([(atlas, 0, 0), (borealis, 4, 4)]) { Name = "Reporting", ReferenceId = "REPORTING-1", Order = "3", StateCategory = StateCategories.ToDo };
            reporting.Portfolios.Add(payments);
            AddForecasts(reporting, borealis);

            var billing = new Feature([new FeatureDependencyReference(0, "CHECKOUT-1", DependencySource.TrackerLink)])
            {
                Name = "Billing", ReferenceId = "BILLING-1", Order = "4", StateCategory = StateCategories.ToDo,
            };
            billing.FeatureWork.Add(new FeatureWork(atlas, 5, 5, billing));
            billing.Portfolios.Add(payments);
            AddForecasts(billing, atlas);

            var archive = new Feature([(atlas, 1, 1), (borealis, 1, 1)]) { Name = "Archive", ReferenceId = "ARCHIVE-1", Order = "5", StateCategory = StateCategories.Done };
            archive.Portfolios.Add(platform);

            var inbox = new Feature([(atlas, 2, 2), (borealis, 1, 1)]) { Name = "Inbox", ReferenceId = "INBOX-1", Order = "6", StateCategory = StateCategories.Unknown };
            inbox.Portfolios.Add(platform);

            context.Features.AddRange(checkout, search, reporting, billing, archive, inbox);
            await context.SaveChangesAsync();

            var itemsWithTransitions = items.Where(item => item.Team == atlas && item.StateCategory is StateCategories.Doing or StateCategories.Done).ToList();
            var borealisItemWithTransitions = items.First(item => item.Team == borealis && item.StateCategory == StateCategories.Done);
            foreach (var item in itemsWithTransitions.Append(borealisItemWithTransitions))
            {
                context.WorkItemStateTransitions.AddRange(
                    new WorkItemStateTransition { WorkItemId = item.Id, FromState = "New", ToState = "Active", TransitionedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc) },
                    new WorkItemStateTransition { WorkItemId = item.Id, FromState = "Active", ToState = "Closed", TransitionedAt = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc) });
            }

            await context.SaveChangesAsync();

            return new SeededInstance([atlas.Id, borealis.Id], itemsWithTransitions.Select(item => item.Id).ToHashSet());
        }

        private static WorkItem AnItem(Team team, StateCategories category, int round)
            => new()
            {
                Team = team,
                Name = $"{team.Name} {category} {round}",
                ReferenceId = $"{team.Name.ToUpperInvariant()}-{category}-{round}",
                Order = $"{round}",
                Type = "User Story",
                State = category.ToString(),
                StateCategory = category,
            };

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
