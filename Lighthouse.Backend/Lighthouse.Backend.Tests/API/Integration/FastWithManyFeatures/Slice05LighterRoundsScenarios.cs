using System.Data;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// What a round reads is counted at the store's edge, never timed, so the claim holds however the reads are
    /// rewritten. A round that writes dates back to the tracker has to read the histograms it stages from, which is
    /// why the "reads nothing" claims are made only for a Portfolio with no write-back mapping.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-05")]
    [Category("us-05")]
    [Category("story-6409")]
    public class Slice05LighterRoundsScenarios : ForecastWhileItUpdatesAcceptanceTest
    {
        private const string TheFeature = "Billing Migration";

        private CapturedMaterialisations materialised = null!;

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            base.ConfigureAdditionalServices(services);

            materialised = new CapturedMaterialisations();
            CapturedMaterialisations.CountInto(services, materialised);
        }

        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-shorter-rounds")]
        [Category("contract-shape:pure-function")]
        public async Task A_round_that_changes_nothing_on_a_Portfolio_that_writes_nothing_back_decides_to_skip_without_reading_a_single_forecast_histogram()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            Assert.That(TheWriteBackMappingsOf(portfolio), Is.Zero, "This claim is for a Portfolio that writes nothing back.");
            await WhenTheRoundRunsFor(portfolio);

            materialised.StartListening();
            await WhenTheRoundRunsFor(portfolio);
            materialised.StopListening();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.True,
                    "Nothing changed between the two rounds, so the second one should have been skipped.");
                Assert.That(materialised.Of<IndividualSimulationResult>(), Is.Zero,
                    $"The skipped round read forecast histograms it never uses. Read: {materialised.Describe()}");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("guardrail-G1")]
        [Category("contract-shape:bounded-change")]
        public async Task A_skipped_round_on_a_Portfolio_that_writes_its_dates_back_still_reads_the_histograms_it_stages_from()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            GivenTheForecastIsWrittenBackToTheTracker(portfolio);
            await WhenTheRoundRunsFor(portfolio);

            materialised.StartListening();
            await WhenTheRoundRunsFor(portfolio);
            materialised.StopListening();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.True, "Nothing changed, so the second round should have been skipped.");
                Assert.That(materialised.Of<IndividualSimulationResult>(), Is.GreaterThan(0),
                    $"A skipped round that writes back stages the 85% date, which it reads off the histogram. Read: {materialised.Describe()}");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-shorter-rounds")]
        [Category("contract-shape:bounded-change")]
        public async Task A_computed_round_replaces_yesterdays_forecasts_without_reading_their_histograms()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            await GivenThePortfolioWasForecastBefore(portfolio);
            var yesterdays = TheStoredForecastOf(portfolio);
            GivenRunsThatFinish(inEveryTen: 7);

            materialised.StartListening();
            await WhenTheRoundRunsFor(portfolio);
            materialised.StopListening();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.False, "The inputs were forgotten, so this round computes.");
                Assert.That(TheStoredForecastOf(portfolio).Histogram, Is.Not.EqualTo(yesterdays.Histogram),
                    "The round replaced yesterday's forecast, or it proves nothing about replacing it.");
                Assert.That(materialised.Of<IndividualSimulationResult>(), Is.Zero,
                    $"A computed round read yesterday's histograms only to replace them. Read: {materialised.Describe()}");
            }
        }

        /// <summary>
        /// A store-level integration check, not a port observation: no port exposes a histogram row whose forecast is
        /// gone, yet once deletes rely on the database's cascade instead of loading rows, such a row is exactly what a
        /// connection opened without foreign keys would leave behind.
        /// </summary>
        [Test]
        [Category("store-level")]
        [Category("edge")]
        [Category("guardrail-G1")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task At_the_store_a_computed_round_leaves_no_histogram_row_behind_that_belongs_to_no_forecast()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            await GivenThePortfolioWasForecastBefore(portfolio);

            await WhenTheRoundRunsFor(portfolio);

            Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.False,
                "The round was to replace yesterday's forecasts; a skipped round deletes nothing, so it could leave no row behind.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheDatabaseEnforcesItsForeignKeys(), Is.True,
                    "Rows that belong to a deleted forecast are removed by the database only while it enforces foreign keys.");
                Assert.That(TheHistogramRowsThatBelongToNoForecast(), Is.Zero);
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("guardrail-G1")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:bounded-change")]
        public async Task The_tracker_is_sent_the_same_date_after_a_computed_round_and_after_the_skipped_round_that_follows_it()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            GivenTheForecastIsWrittenBackToTheTracker(portfolio);

            await WhenTheRoundRunsFor(portfolio);
            var afterTheComputedRound = TheForecastDatesTheTrackerReceived().Select(update => update.Value).ToList();
            await WhenTheRoundRunsFor(portfolio);
            var fromTheSkippedRound = TheForecastDatesTheTrackerReceived().Skip(afterTheComputedRound.Count).Select(update => update.Value).ToList();

            Assert.That(afterTheComputedRound, Is.Not.Empty, "The computed round sent the tracker no date.");
            var feature = await TheFeatureAsTheClientSeesIt(TheFeature);
            var the85PercentDay = ThePercentile(feature, 85).GetProperty("expectedDate").GetString()![..10];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.True, "The second round was to be the skipped one.");
                Assert.That(afterTheComputedRound, Has.All.StartsWith(the85PercentDay),
                    "The tracker was sent another date than the Feature's 85% date.");
                Assert.That(fromTheSkippedRound, Is.Not.Empty, "The skipped round sent the tracker nothing, so nothing was compared.");
                Assert.That(fromTheSkippedRound, Has.All.EqualTo(afterTheComputedRound[0]),
                    "The skipped round sent the tracker another date than the computed round before it.");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("guardrail-G1")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:bounded-change")]
        public async Task A_computed_round_and_the_skipped_round_after_it_stage_the_same_values_for_every_Feature_including_one_no_Team_works_on()
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

            Assert.That(fromTheComputedRound, Does.ContainKey(TheStartOf(AuditTrailsReference)),
                "The computed round staged nothing for the Feature no Team works on, so that Feature was never compared. Staged: "
                + string.Join(", ", fromTheComputedRound.Keys));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheLastForecastRoundOf(portfolio)?.Skipped, Is.True, "The second round was to be the skipped one.");
                Assert.That(fromTheSkippedRound, Is.EquivalentTo(fromTheComputedRound),
                    "A skipped round stages what the computed round before it staged, Feature by Feature.");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("guardrail-G1")]
        [Category("kpi-OUT-6400-answers-unchanged")]
        [Category("contract-shape:bounded-change")]
        public async Task A_computed_round_writes_back_the_forecasted_start_of_a_Feature_a_Team_works_on_and_the_day_work_began_on_one_no_Team_works_on()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(TheFeature);
            GivenTheForecastIsWrittenBackToTheTracker(portfolio);
            GivenThePortfolioAlsoHoldsAFeatureNoTeamWorksOn(portfolio);
            GivenEveryFeaturesStartIsWrittenBackToo(portfolio);

            await WhenTheRoundRunsFor(portfolio);
            var staged = TheValuesByWorkItemAndField(TheValuesTheTrackerReceived());

            var feature = await TheFeatureAsTheClientSeesIt(TheFeature);
            var theStart85Day = TheStartPercentile(feature, 85).GetProperty("expectedDate").GetString()![..10];
            var theDayAuditTrailBegan = Now.UtcDateTime.AddDays(-3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(staged, Does.ContainKey(TheStartOf("PAY-1")), "No start was staged for the Feature a Team works on.");
                Assert.That(staged.GetValueOrDefault(TheStartOf("PAY-1")), Does.StartWith(theStart85Day),
                    "The Feature a Team works on was sent another start than its forecasted 85% start.");
                Assert.That(staged.GetValueOrDefault(TheStartOf(AuditTrailsReference)), Does.StartWith(theDayAuditTrailBegan),
                    "The Feature no Team works on was sent another start than the day it began.");
            }
        }

        private const string AuditTrailsReference = "PAY-2";

        private const string TheStartFieldInTheTracker = "Custom.Start85";

        private Task WhenTheRoundRunsFor(SeededPortfolio portfolio)
            => RunUpdate(sp => sp.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolio.Id));

        private int TheWriteBackMappingsOf(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>()
                .GetById(portfolio.ConnectionId)!.WriteBackMappingDefinitions.Count;
        }

        /// <summary>"Audit Trail" is in progress in the Portfolio with no Team's work under it, so no forecast covers it.</summary>
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
                ReferenceId = AuditTrailsReference,
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

        /// <summary>
        /// A Feature no Team works on has no forecast, so no completion date is staged for it. Once it is in progress its
        /// start is the day it began, which a round stages without any forecast, so a start mapping is what shows whether
        /// a round resolves write-back against every Feature of the Portfolio or only the ones a Team works on.
        /// </summary>
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

            // The tracker is only sent a value that differs from the one last read off the item, so each Feature starts
            // out having read the field, empty.
            var features = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            foreach (var feature in features.GetAll().Where(candidate => candidate.Portfolios.Any(owner => owner.Id == portfolio.Id)).ToList())
            {
                feature.AdditionalFieldValues[startField.Id] = string.Empty;
                features.Update(feature);
            }

            features.Save().GetAwaiter().GetResult();
        }

        private List<WriteBackFieldUpdate> TheValuesTheTrackerReceived()
            => ConnectorMock.Invocations
                .Where(call => call.Method.Name == nameof(IWorkTrackingConnector.WriteFieldsToWorkItems))
                .SelectMany(call => call.Arguments[1] as IReadOnlyList<WriteBackFieldUpdate> ?? [])
                .ToList();

        private static string TheStartOf(string referenceId) => $"{referenceId} {TheStartFieldInTheTracker}";

        private static Dictionary<string, string> TheValuesByWorkItemAndField(IEnumerable<WriteBackFieldUpdate> updates)
            => updates.GroupBy(update => $"{update.WorkItemId} {update.TargetFieldReference}")
                .ToDictionary(group => group.Key, group => group.Last().Value);

        /// <summary>Both rounds run at the same pinned instant, so the later one is told apart by when it was written.</summary>
        private RefreshLog? TheLastForecastRoundOf(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRefreshLogService>().GetRefreshLogs()
                .Where(log => log.Type == RefreshType.Forecast && log.EntityId == portfolio.Id)
                .MaxBy(log => log.Id);
        }

        private bool TheDatabaseEnforcesItsForeignKeys()
        {
            using var scope = Factory.Services.CreateScope();
            var connection = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys;";

            return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
        }

        private int TheHistogramRowsThatBelongToNoForecast()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Set<IndividualSimulationResult>().AsNoTracking()
                .Count(row => !context.Set<ForecastBase>().Any(forecast => forecast.Id == row.ForecastId));
        }
    }
}
