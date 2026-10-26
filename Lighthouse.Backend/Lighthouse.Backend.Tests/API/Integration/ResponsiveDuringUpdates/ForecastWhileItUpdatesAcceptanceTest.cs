using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.API.Integration.TaskManager;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates
{
    /// <summary>
    /// The Task Manager's production host with the production forecast in it: the update queue, the
    /// forecast updater, the simulation, the refresh log and the write-back round are all real. The
    /// draws come from a source that counts the runs and can hold the forecast open part-way, which is
    /// how a forecast is made slow enough to read past or to stop, without timing anything.
    ///
    /// Faked, as the infrastructure policy says: the tracker, the licence, the measured throughput a
    /// Team is forecast from (an input to this feature rather than part of it) and the instance clock.
    /// </summary>
    public abstract class ForecastWhileItUpdatesAcceptanceTest : TaskManagerAcceptanceTest
    {
        protected const int TheRunsAForecastMakes = 400;

        protected const int TheRunsBeforeTheForecastIsHeld = 20;

        protected const int TheCoresTheInstanceIsGiven = 4;

        protected const int HalfTheCores = TheCoresTheInstanceIsGiven / 2;

        protected const int TheDaysARunMayCover = 60;

        protected const int FiveItems = 5;

        protected static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

        protected static readonly int[] OneItemThenNothing = [1, 0];

        private static readonly TimeSpan TheLongestAScenarioWaits = TimeSpan.FromSeconds(30);

        protected const string TheForecastFieldInTheTracker = "Custom.Forecast85";

        protected CountedDrawStream Draws { get; private set; } = null!;

        protected FakeLighthouseClock Clock { get; private set; } = null!;

        private Mock<ITeamMetricsService> teamMetrics = null!;

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            Draws = new CountedDrawStream();
            teamMetrics = new Mock<ITeamMetricsService> { DefaultValue = DefaultValue.Mock };

            services.RemoveAll<IForecastService>();
            services.AddScoped<IForecastService, ForecastService>();

            services.RemoveAll<IDrawStreamFactory>();
            services.AddSingleton<IDrawStreamFactory>(Draws);

            services.RemoveAll<ForecastSimulationLimits>();
            services.AddSingleton(new ForecastSimulationLimits(TheRunsAForecastMakes, TheDaysARunMayCover) { CoresSeen = TheCoresTheInstanceIsGiven });

            services.RemoveAll<ITeamMetricsService>();
            services.AddScoped(_ => teamMetrics.Object);

            services.RemoveAll<ILighthouseClock>();
            Clock = new FakeLighthouseClock(Now);
            services.AddSingleton<ILighthouseClock>(Clock);
        }

        /// <summary>
        /// Runs before the harness tears the host down. A forecast left parked would otherwise still hold its
        /// workers while the database underneath it is deleted.
        /// </summary>
        [TearDown]
        public async Task LetAnyHeldForecastFinish()
        {
            Draws.LetItCarryOn();
            await TheQueueGoesIdle();
            Draws.Dispose();
        }

        // --- Given ---

        protected readonly record struct SeededPortfolio(int Id, int TeamId, string Name, int ConnectionId);

        /// <summary>
        /// "Platform Modernisation", one Team and one Feature of five items. A Team delivering one item and
        /// then nothing finishes it on day five in the runs that draw the first day and never in the others;
        /// how many of those there are is the draw source's to say.
        /// </summary>
        protected SeededPortfolio GivenAPortfolioWithOneFeatureOfFiveItems(string featureName = "Billing Migration")
        {
            var connection = SeedConnection();
            var teamName = $"Payments {Guid.NewGuid():N}";
            var teamId = SeedTeam(connection, teamName);
            var portfolioName = $"Platform Modernisation {Guid.NewGuid():N}";
            var portfolioId = SeedPortfolio(connection, portfolioName);

            using (var scope = Factory.Services.CreateScope())
            {
                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
                var portfolio = portfolios.GetById(portfolioId)!;

                portfolio.UpdateFeatures([
                    new Feature(team, FiveItems) { Name = featureName, ReferenceId = "PAY-1", Order = "10" },
                ]);

                portfolios.Update(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
            }

            GivenTheTeamDelivers(teamId, OneItemThenNothing);

            return new SeededPortfolio(portfolioId, teamId, portfolioName, connection);
        }

        /// <summary>The Portfolio's connection writes each Feature's 85% date back to a field in the tracker.</summary>
        protected void GivenTheForecastIsWrittenBackToTheTracker(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            var connection = connections.GetById(portfolio.ConnectionId)!;

            var field = new AdditionalFieldDefinition { Reference = TheForecastFieldInTheTracker, DisplayName = "Forecast 85" };
            connection.AdditionalFieldDefinitions.Add(field);
            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinition = field,
                TargetValueType = WriteBackTargetValueType.Date,
            });

            connections.Update(connection);
            connections.Save().GetAwaiter().GetResult();

            // The tracker is only sent a value that differs from the one Lighthouse last read off the item,
            // so the Feature starts out having read the field, empty.
            var features = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var feature = features.GetAll().Single(candidate => candidate.Portfolios.Any(owner => owner.Id == portfolio.Id));
            feature.AdditionalFieldValues[field.Id] = string.Empty;
            features.Update(feature);
            features.Save().GetAwaiter().GetResult();
        }

        protected void GivenTheTeamDelivers(int teamId, int[] eachDay)
        {
            var history = new RunChartData(RunChartDataGenerator.GenerateRunChartData(eachDay));

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(It.Is<Team>(team => team.Id == teamId), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(history, false, null));
        }

        protected void GivenRunsThatFinish(int inEveryTen) => Draws.RunsInTenThatDrawTheFirstDay = inEveryTen;

        /// <summary>The forecast as it ran yesterday: through the queue, to the end, as a scheduled one would.</summary>
        /// <remarks>
        /// Yesterday's forecast was computed for another day, so what it was computed from no longer holds
        /// today; without forgetting it, the next background forecast would find nothing changed and skip.
        /// </remarks>
        protected async Task GivenThePortfolioWasForecastBefore(SeededPortfolio portfolio)
        {
            await RunUpdate(sp => sp.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolio.Id));

            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IPortfolioRepository>()
                .ForgetForecastInputs([portfolio.Id], CancellationToken.None);
        }

        // --- When ---

        /// <summary>
        /// Starts the forecast through the updater the scheduler and the Portfolio refresh use, and returns
        /// once it is held part-way through its runs.
        /// </summary>
        protected void WhenThePortfolioForecastIsUnderWay(SeededPortfolio portfolio)
        {
            Draws.HoldOnceRunsHaveBegun(TheRunsBeforeTheForecastIsHeld);

            Factory.Services.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolio.Id);

            Assert.That(Draws.IsHeldWithin(TheLongestAScenarioWaits), Is.True,
                "The forecast never reached the run it was to be held at, so nothing here happened during it.");
        }

        protected async Task WhenTheOperatorStopsTheForecastOf(SeededPortfolio portfolio)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(new Uri($"{TaskListRoute}/{UpdateType.Forecasts}/{portfolio.Id}/cancel", UriKind.Relative), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                $"Stop is the driving port here; it answered {(int)response.StatusCode}.");
        }

        /// <summary>
        /// Settled means the browser has been told how the run ended, not only that the key has left the
        /// status store: the queue drops the key before it pushes the terminal status. Endings told before
        /// the forecast is let go belong to earlier runs, so only a new one counts.
        /// </summary>
        protected async Task TheForecastIsLetGoAndSettles(SeededPortfolio portfolio)
        {
            var endingsToldBefore = TheEndingsToldFor(portfolio);

            Draws.LetItCarryOn();

            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var key = new UpdateKey(UpdateType.Forecasts, portfolio.Id);
            var deadline = DateTime.UtcNow + TheLongestAScenarioWaits;

            while (store.TryGet(key, out _) || TheEndingsToldFor(portfolio) == endingsToldBefore)
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail($"The forecast let go after the stop never settled within {TheLongestAScenarioWaits.TotalSeconds}s: "
                        + $"still in the status store: {store.TryGet(key, out _)}. The browser was told: {TheBrowserWasTold.Describe()}");
                }

                await Task.Delay(20);
            }
        }

        private int TheEndingsToldFor(SeededPortfolio portfolio)
            => TheBrowserWasTold.For(UpdateType.Forecasts, portfolio.Id).Count(progress => progress
                is UpdateProgress.Completed or UpdateProgress.Failed or UpdateProgress.Cancelled);

        // --- Then (what the instance says afterwards) ---

        protected (DateTime CreatedAt, string Histogram) TheStoredForecastOf(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            var feature = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>().GetAll()
                .Single(candidate => candidate.Portfolios.Any(owner => owner.Id == portfolio.Id));
            var forecast = feature.Forecasts.Single();

            var histogram = string.Join(", ", forecast.SimulationResults
                .OrderBy(day => day.Key)
                .Select(day => $"{day.Key}:{day.Value}"));

            return (forecast.CreationTime, $"{forecast.TotalTrials} runs [{histogram}]");
        }

        protected async Task<JsonElement> TheFeatureAsTheClientSeesIt(string name)
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri("/api/latest/features", UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            foreach (var feature in document.RootElement.EnumerateArray())
            {
                if (feature.GetProperty("name").GetString() == name)
                {
                    return feature.Clone();
                }
            }

            Assert.Fail($"No Feature named '{name}' came back from the read.");

            return default;
        }

        protected static JsonElement ThePercentile(JsonElement feature, int probability)
        {
            foreach (var percentile in feature.GetProperty("forecasts").EnumerateArray())
            {
                if (percentile.GetProperty("probability").GetInt32() == probability)
                {
                    return percentile;
                }
            }

            Assert.Fail($"The Feature carried no {probability}% percentile: {feature}");

            return default;
        }

        protected static JsonElement TheStartPercentile(JsonElement feature, int probability)
        {
            foreach (var percentile in feature.GetProperty("startForecast").GetProperty("percentiles").EnumerateArray())
            {
                if (percentile.GetProperty("probability").GetInt32() == probability)
                {
                    return percentile;
                }
            }

            Assert.Fail($"The Feature carried no {probability}% start percentile: {feature}");

            return default;
        }

        /// <summary>Every forecast date the tracker was sent for the mapped field, as the tracker received them.</summary>
        protected IReadOnlyList<WriteBackFieldUpdate> TheForecastDatesTheTrackerReceived()
            => ConnectorMock.Invocations
                .Where(call => call.Method.Name == nameof(IWorkTrackingConnector.WriteFieldsToWorkItems))
                .SelectMany(call => call.Arguments[1] as IReadOnlyList<WriteBackFieldUpdate> ?? [])
                .Where(update => update.TargetFieldReference == TheForecastFieldInTheTracker)
                .ToList();
    }
}
