using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates
{
    /// <summary>
    /// A real application host whose forecast draws from a source the test chose and runs however many
    /// simulated runs the test asks for.
    ///
    /// Both are necessary rather than convenient. The product draws a fresh starting number every run, so
    /// two runs over identical data return dates a day or so apart - a test asserting which day a Feature
    /// starts would be asserting sampling noise. And the ten thousand runs the product ships with buy
    /// precision nothing here needs: every scenario below reads one day off a distribution whose shape it
    /// has already pinned.
    ///
    /// Delivery history is handed in rather than seeded as closed work items. The throughput a Team is
    /// forecast from is an input to this feature, not part of it: building it out of dated work items would
    /// put the metrics service's own windowing, filtering and blackout arithmetic between the number the
    /// test chose and the number the simulation drew from, and a scenario that says "three a day" would be
    /// asserting all of that too.
    /// </summary>
    internal sealed class ForecastedStartDateHost(IDrawStreamFactory draws, int trials) : TestWebApplicationFactory<Program>
    {
        public Mock<ITeamMetricsService> TeamMetrics { get; } = new();

        public Mock<ILicenseService> License { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            License.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDrawStreamFactory>();
                services.AddSingleton(draws);

                services.RemoveAll<ForecastSimulationLimits>();
                services.AddSingleton(new ForecastSimulationLimits(
                    trials,
                    ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));

                services.RemoveAll<ITeamMetricsService>();
                services.AddScoped(_ => TeamMetrics.Object);

                services.RemoveAll<ILicenseService>();
                services.AddScoped(_ => License.Object);
            });
        }
    }

    /// <summary>
    /// What every forecasted-start-date scenario needs: a Portfolio whose Teams deliver at a rate the test
    /// chose, a way to run the real forecast over it, and the read every screen makes afterwards.
    ///
    /// The read is the driving port and it is deliberately the wire rather than the domain. Nothing in this
    /// fixture names a type this feature is about to add, so the scenarios compile against the application
    /// as it stands and fail on the answer rather than on the build - which is the difference between a
    /// test that is RED and one that is broken.
    /// </summary>
    public abstract class ForecastedStartDateAcceptanceTest : IntegrationTestBase
    {
        protected const int DeterministicTrials = 50;

        protected const int SampledTrials = 1_000;

        protected static readonly int[] TheFourPercentiles = [50, 70, 85, 95];

        /// <summary>
        /// A date the read has to have carried. Asserted separately, and outside any multiple-assert
        /// scope, so that a Feature carrying no start date at all fails saying exactly that instead of
        /// throwing on the way into a comparison - the difference between a test that is RED and one that
        /// is broken.
        /// </summary>
        protected static DateTime ADateThatMustBeThere(DateTime? date, string what)
        {
            Assert.That(date, Is.Not.Null, what);

            return date!.Value;
        }

        private readonly ForecastedStartDateHost host;

        private protected ForecastedStartDateAcceptanceTest(ForecastedStartDateHost host) : base(host)
        {
            this.host = host;
        }

        private protected ForecastedStartDateHost Host => host;

        // --- Given ---

        protected WorkTrackingSystemConnection GivenAConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var connections = ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            connections.Add(connection);

            return connection;
        }

        /// <param name="deliversEachDay">
        /// The whole measured history. A draw stream that always draws zero takes the first entry every
        /// day, which is what makes a run arithmetic; a stream that samples takes any of them.
        /// </param>
        protected async Task<Team> GivenATeamThatDelivers(
            WorkTrackingSystemConnection connection,
            string name,
            int[] deliversEachDay,
            int featureWip)
        {
            var team = new Team
            {
                Name = name,
                FeatureWIP = featureWip,
                WorkTrackingSystemConnection = connection,
            };

            var teams = ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Add(team);
            await teams.Save();

            GivenTheTeamDelivers(team, deliversEachDay);

            return team;
        }

        protected void GivenTheTeamDelivers(Team team, int[] deliversEachDay)
        {
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData(deliversEachDay));

            host.TeamMetrics
                .Setup(service => service.GetForecastThroughputStatus(
                    It.Is<Team>(candidate => candidate.Id == team.Id),
                    It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(throughput, false, null));
        }

        /// <summary>
        /// A Team with nothing measured. The simulation admits no row for it, which is how a Feature it
        /// contributes to has been un-forecastable since long before start dates existed.
        /// </summary>
        protected void GivenTheTeamHasNeverDeliveredAnything(Team team)
        {
            host.TeamMetrics
                .Setup(service => service.GetForecastThroughputStatus(
                    It.Is<Team>(candidate => candidate.Id == team.Id),
                    It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(new RunChartData(), false, null, HasSufficientData: false));
        }

        protected static Feature AFeature(string name, string order, params (Team team, int remaining)[] work)
        {
            return new Feature(work.Select(entry => (entry.team, entry.remaining, entry.remaining)))
            {
                Name = name,
                ReferenceId = name.ToUpperInvariant(),
                Order = order,
            };
        }

        protected async Task<Portfolio> GivenAPortfolioOf(WorkTrackingSystemConnection connection, params Feature[] features)
        {
            var portfolio = new Portfolio
            {
                Name = "Portfolio",
                WorkTrackingSystemConnection = connection,
            };

            portfolio.UpdateFeatures(features);

            var portfolios = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolios.Add(portfolio);
            await portfolios.Save();

            return portfolios.GetAll().Single();
        }

        /// <summary>
        /// One Feature waits on another. Seeded as the reference the connectors write, because that is what
        /// the forecast reads - a wait invented straight onto the run plan would prove the simulation can
        /// carry one without proving a stored dependency ever reaches it.
        /// </summary>
        protected async Task GivenTheFeatureWaitsOn(Feature waiting, Feature blocker)
        {
            waiting.ReplaceDependsOnReferences(
                [new FeatureDependencyReference(waiting.Id, blocker.ReferenceId, DependencySource.TrackerLink)]);

            var features = ServiceProvider.GetRequiredService<IRepository<Feature>>();
            features.Update(waiting);
            await features.Save();
        }

        // --- When ---

        protected async Task WhenTheForecastRuns(Portfolio portfolio)
        {
            var forecast = ServiceProvider.GetRequiredService<IForecastService>();
            await forecast.UpdateForecastsForPortfolio(portfolio);
        }

        // --- Then (the read every screen makes) ---

        protected async Task<JsonElement> TheFeaturesAsTheClientSeesThem(int portfolioId)
        {
            Client.AsPortfolioViewer(portfolioId);

            var response = await Client.GetAsync("/api/latest/features");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(body).RootElement.Clone();
        }

        protected static JsonElement TheFeatureNamed(JsonElement features, string name)
        {
            foreach (var feature in features.EnumerateArray())
            {
                if (feature.GetProperty("name").GetString() == name)
                {
                    return feature;
                }
            }

            Assert.Fail($"No Feature named '{name}' came back from the read.");

            return default;
        }

        /// <summary>
        /// Where a Feature's start date came from, said out loud rather than worked out from what is
        /// missing. "No percentiles" and "observed" are different answers and a reader that guessed one
        /// from the other would be re-deciding, in every client, the rule the domain already decided.
        /// </summary>
        protected static string? TheStartSource(JsonElement feature)
        {
            if (!feature.TryGetProperty("startForecast", out var start) || start.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return start.TryGetProperty("source", out var source) ? source.GetString() : null;
        }

        protected static DateTime? TheObservedStart(JsonElement feature)
        {
            if (!feature.TryGetProperty("startForecast", out var start) || start.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!start.TryGetProperty("observedDate", out var observed) || observed.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return observed.GetDateTime();
        }

        /// <summary>
        /// The forecasted start at one percentile, or nothing at all when the Feature carries no start
        /// forecast. Absence is a real answer here - an un-forecastable Feature reports no dates rather
        /// than dates it cannot stand behind - so it is reported as null rather than asserted away.
        /// </summary>
        protected static DateTime? TheForecastedStart(JsonElement feature, int percentile)
        {
            if (!feature.TryGetProperty("startForecast", out var start) || start.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return TheDateAt(start, "percentiles", percentile);
        }

        protected static DateTime? TheForecastedCompletion(JsonElement feature, int percentile)
            => TheDateAt(feature, "forecasts", percentile);

        protected static DateTime? TheForecastedStartFor(JsonElement feature, int teamId, int percentile)
            => TheDateAt(TheTeamForecastFor(feature, teamId), "startPercentiles", percentile);

        protected static DateTime? TheForecastedCompletionFor(JsonElement feature, int teamId, int percentile)
            => TheDateAt(TheTeamForecastFor(feature, teamId), "completionPercentiles", percentile);

        private static JsonElement TheTeamForecastFor(JsonElement feature, int teamId)
        {
            if (!feature.TryGetProperty("teamForecasts", out var perTeam) || perTeam.ValueKind != JsonValueKind.Array)
            {
                return default;
            }

            foreach (var forecast in perTeam.EnumerateArray())
            {
                if (forecast.GetProperty("teamId").GetInt32() == teamId)
                {
                    return forecast;
                }
            }

            return default;
        }

        private static DateTime? TheDateAt(JsonElement owner, string property, int percentile)
        {
            if (owner.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!owner.TryGetProperty(property, out var forecasts) || forecasts.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var forecast in forecasts.EnumerateArray())
            {
                if (forecast.GetProperty("probability").GetInt32() == percentile)
                {
                    return forecast.GetProperty("expectedDate").GetDateTime();
                }
            }

            return null;
        }

        protected static IReadOnlyList<int> TheCompletionPercentilesOf(JsonElement feature)
        {
            if (!feature.TryGetProperty("forecasts", out var forecasts) || forecasts.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return forecasts.EnumerateArray()
                .Select(forecast => forecast.GetProperty("probability").GetInt32())
                .ToList();
        }

        protected static IReadOnlyList<string> TheTeamsWithoutForecastOf(JsonElement feature)
        {
            if (!feature.TryGetProperty("teamsWithoutForecast", out var teams) || teams.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return teams.EnumerateArray().Select(team => team.GetString() ?? string.Empty).ToList();
        }
    }
}
