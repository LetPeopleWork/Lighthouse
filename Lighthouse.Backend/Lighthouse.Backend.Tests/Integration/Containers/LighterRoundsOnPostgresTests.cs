using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// What a forecast round changes, and what it stages for the tracker, must not depend on the provider the instance
    /// lives on. The rounds run through the production queue against the one shared Postgres container, and what they
    /// stored is read back through a fresh context, ordered, because a provider may hand rows back in any order.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-05")]
    public class LighterRoundsOnPostgresTests : ForecastWhileItUpdatesAcceptanceTest
    {
        private const string TheFeature = "Billing Migration";

        private const string TheBystandingFeature = "Ledger Archive";

        private const string TheFeatureATeamWorksOn = "PAY-1";

        private const string TheFeatureNoTeamWorksOn = "PAY-2";

        private const string TheStartFieldInTheTracker = "Custom.Start85";

        private const string EveryTableWithAHashOfItsRows = """
            SELECT table_name || '=' || md5(query_to_xml(format('select * from %I as t order by t::text', table_name), false, false, '')::text) AS "Value"
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            ORDER BY table_name
            """;

        private static readonly string[] TablesARoundMayWrite = ["ForecastBase", "IndividualSimulationResult", "RefreshLogs", "Portfolios"];

        private CapturedMaterialisations materialised = null!;

        private SharedPostgresContainer? postgres;

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            base.ConfigureAdditionalServices(services);

            materialised = new CapturedMaterialisations();
            CapturedMaterialisations.CountInto(services, materialised);
        }

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [SetUp]
        public async Task MoveTheInstanceToPostgres()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

            Factory = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<LighthouseAppContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LighthouseAppContext>>();
                services.AddDbContext<LighthouseAppContext>(options => options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres");
                }));
                CapturedMaterialisations.CountInto(services, materialised);
            }));

            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.MigrateAsync();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }
        }

        [Test]
        public async Task A_computed_round_on_Postgres_changes_only_the_forecasts_of_the_Features_it_gathered()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            var bystander = GivenAPortfolioWithOneFeatureOfFiveItems(TheBystandingFeature);
            await GivenThePortfolioWasForecastBefore(bystander);
            await GivenThePortfolioWasForecastBefore(portfolio);
            GivenRunsThatFinish(inEveryTen: 8);

            var tablesBefore = await TheTablesAsTheyAre();
            var gatheredBefore = await TheForecastsOf(TheFeature);
            var bystanderBefore = await TheForecastsOf(TheBystandingFeature);
            await WhenTheRoundRunsFor(portfolio);
            var tablesAfter = await TheTablesAsTheyAre();

            var changed = tablesAfter.Where(table => tablesBefore[table.Key] != table.Value).Select(table => table.Key).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await TheForecastsOf(TheFeature), Is.Not.EqualTo(gatheredBefore),
                    "The round replaced no forecast of the Feature it gathered, so it proves nothing about what else it left alone.");
                Assert.That(await TheForecastsOf(TheBystandingFeature), Is.EqualTo(bystanderBefore),
                    "The round changed the forecasts of a Feature of another Portfolio.");
                Assert.That(changed, Is.SubsetOf(TablesARoundMayWrite),
                    $"The round changed rows in tables that hold no forecast of its Features. Changed: {string.Join(", ", changed)}");
            }
        }

        [Test]
        public async Task A_computed_round_and_the_skipped_round_after_it_stage_the_same_values_on_Postgres_for_every_Feature_including_one_no_Team_works_on()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            GivenTheForecastIsWrittenBackToTheTracker(portfolio);
            GivenThePortfolioAlsoHoldsAFeatureNoTeamWorksOn(portfolio);
            GivenEveryFeaturesStartIsWrittenBackToo(portfolio);

            await WhenTheRoundRunsFor(portfolio);
            var staged = TheValuesTheTrackerReceived();
            var fromTheComputedRound = TheValuesByWorkItemAndField(staged);
            await WhenTheRoundRunsFor(portfolio);
            var fromTheSkippedRound = TheValuesByWorkItemAndField(TheValuesTheTrackerReceived().Skip(staged.Count));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromTheComputedRound, Does.ContainKey($"{TheFeatureNoTeamWorksOn} {TheStartFieldInTheTracker}"),
                    "The computed round staged nothing for the Feature no Team works on, so that Feature was never compared. Staged: "
                    + string.Join(", ", fromTheComputedRound.Keys));
                Assert.That(fromTheComputedRound, Does.ContainKey($"{TheFeatureATeamWorksOn} {TheForecastFieldInTheTracker}"),
                    "The computed round staged no forecast date for the Feature a Team works on, so there was no forecast to compare. Staged: "
                    + string.Join(", ", fromTheComputedRound.Keys));
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.True, "The second round was to be the skipped one.");
                Assert.That(fromTheSkippedRound, Is.EquivalentTo(fromTheComputedRound),
                    "A skipped round stages what the computed round before it staged, Feature by Feature.");
            }
        }

        [Test]
        public async Task Without_a_write_back_mapping_neither_a_computed_nor_a_skipped_round_reads_a_forecast_histogram_on_Postgres()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            await GivenThePortfolioWasForecastBefore(portfolio);

            materialised.StartListening();
            await WhenTheRoundRunsFor(portfolio);
            materialised.StopListening();
            var readByTheComputedRound = materialised.Of<IndividualSimulationResult>();
            var computedAndRecorded = TheLastForecastRoundOf(portfolio)?.Skipped;

            materialised.StartListening();
            await WhenTheRoundRunsFor(portfolio);
            materialised.StopListening();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(computedAndRecorded, Is.False, "The first round was to compute.");
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.True, "The second round was to be the skipped one.");
                Assert.That(readByTheComputedRound, Is.Zero, "The computed round read histograms it only replaced.");
                Assert.That(materialised.Of<IndividualSimulationResult>(), Is.Zero,
                    $"The skipped round read histograms it never uses. Read: {materialised.Describe()}");
            }
        }

        private Task WhenTheRoundRunsFor(SeededPortfolio portfolio)
            => RunUpdate(sp => sp.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolio.Id));

        private async Task<Dictionary<string, string>> TheTablesAsTheyAre()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var rows = await context.Database.SqlQueryRaw<string>(EveryTableWithAHashOfItsRows).ToListAsync();

            return rows.Select(row => row.Split('=', 2)).ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
        }

        private async Task<string> TheForecastsOf(string featureName)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var feature = await context.Features.AsNoTracking()
                .Include(candidate => candidate.Forecasts).ThenInclude(forecast => forecast.SimulationResults)
                .Include(candidate => candidate.StartForecasts).ThenInclude(forecast => forecast.SimulationResults)
                .SingleAsync(candidate => candidate.Name == featureName);

            return string.Join("; ", feature.Forecasts.Cast<ForecastBase>().Concat(feature.StartForecasts)
                .OrderBy(forecast => forecast.Id)
                .Select(forecast => $"{forecast.Id}@{forecast.CreationTime:O} {forecast.TotalTrials} runs [{string.Join(", ", forecast.SimulationResults.OrderBy(day => day.Key).Select(day => $"{day.Key}:{day.Value}"))}]"));
        }

        private RefreshLog? TheLastForecastRoundOf(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRefreshLogService>().GetRefreshLogs()
                .Where(log => log.Type == RefreshType.Forecast && log.EntityId == portfolio.Id)
                .MaxBy(log => log.Id);
        }

        private List<WriteBackFieldUpdate> TheValuesTheTrackerReceived()
            => ConnectorMock.Invocations
                .Where(call => call.Method.Name == nameof(IWorkTrackingConnector.WriteFieldsToWorkItems))
                .SelectMany(call => call.Arguments[1] as IReadOnlyList<WriteBackFieldUpdate> ?? [])
                .ToList();

        private static Dictionary<string, string> TheValuesByWorkItemAndField(IEnumerable<WriteBackFieldUpdate> updates)
            => updates.GroupBy(update => $"{update.WorkItemId} {update.TargetFieldReference}")
                .ToDictionary(group => group.Key, group => group.Last().Value);

        private void GivenThePortfolioAlsoHoldsAFeatureNoTeamWorksOn(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            var field = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>()
                .GetById(portfolio.ConnectionId)!.AdditionalFieldDefinitions.Single(definition => definition.Reference == TheForecastFieldInTheTracker);
            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var owner = portfolios.GetById(portfolio.Id)!;

            var auditTrail = new Feature
            {
                Name = "Audit Trail",
                ReferenceId = TheFeatureNoTeamWorksOn,
                Order = "20",
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                StartedDate = Now.UtcDateTime.AddDays(-3),
            };
            auditTrail.AdditionalFieldValues[field.Id] = string.Empty;
            owner.UpdateFeatures([.. owner.Features, auditTrail]);

            portfolios.Update(owner);
            portfolios.Save().GetAwaiter().GetResult();
        }

        private void GivenEveryFeaturesStartIsWrittenBackToo(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            var connection = connections.GetById(portfolio.ConnectionId)!;

            var startField = new AdditionalFieldDefinition { Reference = TheStartFieldInTheTracker, DisplayName = "Start 85" };
            connection.AdditionalFieldDefinitions.Add(startField);
            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastedStartPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinition = startField,
                TargetValueType = WriteBackTargetValueType.Date,
            });
            connections.Update(connection);
            connections.Save().GetAwaiter().GetResult();

            var features = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            foreach (var feature in features.GetAll().Where(candidate => candidate.Portfolios.Any(owner => owner.Id == portfolio.Id)).ToList())
            {
                feature.AdditionalFieldValues[startField.Id] = string.Empty;
                features.Update(feature);
            }

            features.Save().GetAwaiter().GetResult();
        }
    }
}
