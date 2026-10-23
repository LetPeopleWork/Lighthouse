using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.API.Integration.TaskManager;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// The production host with the production forecast in it: the Team, Portfolio and forecast updaters,
    /// the update queue, the work item refresh, the simulation, the refresh log, the domain events and the
    /// controllers are all real. "Payments Platform" is built the way an instance builds it, from what the
    /// work tracking system holds, by letting its Teams and the Portfolio refresh.
    ///
    /// Faked, as the infrastructure policy says: the work tracking system, the licence, the instance clock,
    /// the source of the simulation's random numbers (which also counts every number drawn, so "nothing was
    /// simulated" is a count of zero), and the measured throughput a Team is forecast from. The throughput is
    /// faked because what a blackout, the throughput window or the forecast filter do to it is the metrics
    /// service's own tested behaviour, unchanged here; what this feature promises is that a change in the
    /// throughput as the forecast is handed it is never confirmed as unchanged.
    /// </summary>
    public abstract class SkipUnchangedForecastsAcceptanceTest : TaskManagerAcceptanceTest
    {
        protected const string PaymentsPlatform = "Payments Platform";

        protected const string Onboarding = "Onboarding";

        protected const string Fss = "FSS";

        protected const string Atlas = "Atlas";

        protected const string Orion = "Orion";

        protected const string Loyalty = "Loyalty";

        protected static readonly DateTimeOffset TenOClock = new(2026, 10, 23, 10, 0, 0, TimeSpan.Zero);

        protected static readonly TimeSpan AQuarterOfAnHour = TimeSpan.FromMinutes(15);

        private const int TheRunsAForecastMakes = 200;

        private const int TheDaysARunMayCover = 60;

        private const int TheRunsBeforeAForecastIsHeld = 20;

        private static readonly TimeSpan TheLongestAScenarioWaits = TimeSpan.FromSeconds(30);

        // Each Team's history starts with a delivering day, which is the day every run draws.
        protected static readonly int[] FssDelivers = [1, 0, 1, 1, 1, 0, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1];

        private static readonly int[] AtlasDelivers = [1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 1];

        private static readonly int[] OrionDelivers = [1, 0, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 1, 1];

        protected FakeLighthouseClock Clock { get; private set; } = null!;

        protected DrawsCountedAndHeld Draws { get; private set; } = null!;

        protected CapturedDomainEvents RaisedSignals { get; private set; } = null!;

        protected WhatTheTrackerHolds Tracker { get; private set; } = null!;

        protected int PaymentsPlatformId { get; private set; }

        protected int OnboardingId { get; private set; }

        protected int LoyaltyId { get; private set; }

        protected int FssId { get; private set; }

        protected int AtlasId { get; private set; }

        protected int OrionId { get; private set; }

        private Mock<ITeamMetricsService> teamMetrics = null!;

        private int connectionId;

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            var draws = new DrawsCountedAndHeld();
            var signals = new CapturedDomainEvents();
            var tracker = new WhatTheTrackerHolds();
            var metrics = new Mock<ITeamMetricsService> { DefaultValue = DefaultValue.Mock };
            var clock = new FakeLighthouseClock(TenOClock);

            Draws = draws;
            RaisedSignals = signals;
            Tracker = tracker;
            teamMetrics = metrics;
            Clock = clock;

            services.RemoveAll<IForecastService>();
            services.AddScoped<IForecastService, ForecastService>();

            services.RemoveAll<IDrawStreamFactory>();
            services.AddSingleton<IDrawStreamFactory>(draws);

            services.RemoveAll<ForecastSimulationLimits>();
            services.AddSingleton(new ForecastSimulationLimits(TheRunsAForecastMakes, TheDaysARunMayCover) { CoresSeen = 4 });

            services.RemoveAll<ITeamMetricsService>();
            services.AddScoped(_ => metrics.Object);

            services.RemoveAll<ILighthouseClock>();
            services.AddSingleton<ILighthouseClock>(clock);

            services.AddScoped<IDomainEventHandler<PortfolioForecastsUpdated>>(_ => new CapturingDomainEventHandler<PortfolioForecastsUpdated>(signals));

            ConnectorMock
                .Setup(connector => connector.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Team team, CancellationToken _) => tracker.WorkItemsOf(team));
            ConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Portfolio portfolio, CancellationToken _) => tracker.FeaturesOf(portfolio));
        }

        [TearDown]
        public async Task LetAnyHeldForecastFinish()
        {
            Draws.LetItCarryOn();
            await TheQueueGoesIdle();
            Draws.Dispose();
        }

        // --- Given: Payments Platform as the work tracking system holds it ---

        /// <summary>
        /// Three Features in order: Card Vault (three items of FSS), Refunds (two of FSS, two of Atlas) and
        /// Fraud Rules, not broken down yet and so sized by the Portfolio's default. Orion is a Team of the
        /// instance that does nothing for this Portfolio until a scenario says so.
        /// </summary>
        protected void GivenPaymentsPlatformAsTheTrackerHoldsIt()
        {
            connectionId = SeedConnection();
            FssId = SeedTeam(connectionId, Fss);
            AtlasId = SeedTeam(connectionId, Atlas);
            OrionId = SeedTeam(connectionId, Orion);
            PaymentsPlatformId = SeedPortfolio(connectionId, PaymentsPlatform);

            ChangeTheTeam(FssId, team => team.FeatureWIP = 2);

            Tracker.HoldsFeature(PaymentsPlatform, "PAY-88", "Card Vault", order: "1");
            Tracker.HoldsFeature(PaymentsPlatform, "PAY-90", "Refunds", order: "2");
            Tracker.HoldsFeature(PaymentsPlatform, "PAY-97", "Fraud Rules", order: "3");

            Tracker.HoldsItems(Fss, "PAY-88", "FSS-1", "FSS-2", "FSS-3");
            Tracker.HoldsItems(Fss, "PAY-90", "FSS-4", "FSS-5");
            Tracker.HoldsItems(Atlas, "PAY-90", "ATL-1", "ATL-2");

            TheTeamDelivers(FssId, FssDelivers);
            TheTeamDelivers(AtlasId, AtlasDelivers);
            TheTeamDelivers(OrionId, OrionDelivers);
        }

        /// <summary>The first background cycle of the day: both Teams refresh, then the Portfolio, which forecasts.</summary>
        protected async Task GivenPaymentsPlatformWasForecastAt(DateTimeOffset at)
        {
            var cycle = await TheBackgroundCycleAt(at, FssId, AtlasId);

            Assert.That(cycle.NumbersDrawn, Is.GreaterThan(0),
                "The forecast this scenario starts from never ran, so nothing after it can be told apart.");
        }

        /// <summary>
        /// The skip has to be seen before a change can be seen to undo it: a scenario whose quiet round still
        /// computed cannot tell a change from no change at all.
        /// </summary>
        protected async Task GivenAQuietRoundSkippedTheForecastAt(DateTimeOffset at)
        {
            var quiet = await TheTeamRoundAt(at, FssId);

            Assert.That(quiet.NumbersDrawn, Is.Zero,
                $"A refresh of {Fss} that changed nothing the forecast reads still simulated {quiet.NumbersDrawn} numbers.");
        }

        /// <summary>Onboarding: one Feature, delivered by Orion alone, so no forecast of Payments Platform reads it.</summary>
        protected async Task GivenOnboardingWasForecastAt(DateTimeOffset at)
        {
            OnboardingId = SeedPortfolio(connectionId, Onboarding);
            Tracker.HoldsFeature(Onboarding, "ONB-1", "Self Sign-up", order: "1");
            Tracker.HoldsItems(Orion, "ONB-1", "ORN-7", "ORN-8");

            Clock.SetInstant(at);
            await TheTeamRefreshRuns(OrionId);
            await ThePortfolioRefreshRuns(OnboardingId);
        }

        /// <summary>
        /// Loyalty: one Feature, worked on by FSS. Because a forecast of Payments Platform forecasts every
        /// Feature its Teams work on, Loyalty's Feature is one of the inputs of Payments Platform's forecast
        /// although it belongs to another Portfolio. Comes before the cycle that forecasts Payments Platform,
        /// so FSS's items find their Feature.
        /// </summary>
        protected async Task GivenLoyaltyWhichFssAlsoWorksOn()
        {
            LoyaltyId = SeedPortfolio(connectionId, Loyalty);
            Tracker.HoldsFeature(Loyalty, "LOY-1", "Points Wallet", order: "1");
            Tracker.HoldsItems(Fss, "LOY-1", "FSS-20", "FSS-21");

            Clock.SetInstant(TenOClock);
            await ThePortfolioRefreshRuns(LoyaltyId);
        }

        /// <summary>
        /// Loyalty is first forecast before Payments Platform's Features have loaded, so its forecast covers fewer
        /// Features than FSS now works on. Another refresh of FSS forecasts it over the Features as they now stand,
        /// which a quiet round needs before it can draw nothing anywhere.
        /// </summary>
        protected Task GivenLoyaltyWasForecastOverTheFeaturesFssNowWorksOnAt(DateTimeOffset at) => TheTeamRoundAt(at, FssId);

        /// <summary>
        /// The work tracking system answers the cheap identity scan, as Jira Cloud and Azure DevOps do, so a
        /// refresh after the first downloads only what moved: the reporter's "mode=Delta | fetched=0" rounds.
        /// </summary>
        protected void GivenTheTrackerCanBeScanned()
        {
            ConnectorMock.Setup(connector => connector.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(true);
            ConnectorMock
                .Setup(connector => connector.SweepWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Team team, CancellationToken _) => Tracker.StampsOfTheItemsOf(team));
            ConnectorMock
                .Setup(connector => connector.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Team team, IReadOnlyCollection<string> referenceIds, CancellationToken _) => Tracker.WorkItemsOf(team).Where(item => referenceIds.Contains(item.ReferenceId)));
            ConnectorMock
                .Setup(connector => connector.SweepFeaturesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Portfolio portfolio, CancellationToken _) => Tracker.StampsOfTheFeaturesOf(portfolio));
            ConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Portfolio portfolio, IReadOnlyCollection<string> referenceIds, CancellationToken _) => [.. Tracker.FeaturesOf(portfolio).Where(feature => referenceIds.Contains(feature.ReferenceId))]);
        }

        protected int GivenTheQ4ReleaseIsTrackedOnPaymentsPlatform()
        {
            using var scope = Factory.Services.CreateScope();
            var deliveries = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            var delivery = new Delivery("Q4 Release", new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc), PaymentsPlatformId);

            deliveries.Add(delivery);
            deliveries.Save().GetAwaiter().GetResult();

            return delivery.Id;
        }

        protected void TheTeamDelivers(int teamId, int[] eachDay, bool filterApplied = false, string? excludedSummary = null, bool hasSufficientData = true)
        {
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData(eachDay));

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(It.Is<Team>(team => team.Id == teamId), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(throughput, filterApplied, excludedSummary, hasSufficientData));
        }

        protected void ReadingTheThroughputOfTheTeamFails(int teamId)
        {
            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(It.Is<Team>(team => team.Id == teamId), It.IsAny<ThroughputFilterMode>()))
                .Throws(new InvalidOperationException("The throughput of the Team could not be read."));
        }

        protected void ChangeTheTeam(int teamId, Action<Team> change)
        {
            using var scope = Factory.Services.CreateScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var team = teams.GetById(teamId)!;

            change(team);

            teams.Update(team);
            teams.Save().GetAwaiter().GetResult();
        }

        protected void ChangeThePortfolio(int portfolioId, Action<Portfolio> change)
        {
            using var scope = Factory.Services.CreateScope();
            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = portfolios.GetById(portfolioId)!;

            change(portfolio);

            portfolios.Update(portfolio);
            portfolios.Save().GetAwaiter().GetResult();
        }

        protected void TheFeatureNowWaitsFor(string dependentReferenceId, string blockerReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var features = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var dependent = features.GetAll().Single(feature => feature.ReferenceId == dependentReferenceId);

            dependent.ReplaceDependsOnReferences([new FeatureDependencyReference(dependent.Id, blockerReferenceId, DependencySource.TrackerLink)]);

            features.Update(dependent);
            features.Save().GetAwaiter().GetResult();
        }

        // --- When: the background rounds the scheduler runs ---

        /// <summary>What one or more background rounds did to the forecast of Payments Platform.</summary>
        protected sealed record ObservedRounds(long NumbersDrawn, IReadOnlyList<JsonElement> NewForecastRows, IReadOnlyList<string> OperatorLines);

        protected Task<ObservedRounds> TheTeamRoundAt(DateTimeOffset at, int teamId)
            => Observe(async () =>
            {
                Clock.SetInstant(at);
                await TheTeamRefreshRuns(teamId);
            });

        protected Task<ObservedRounds> ThePortfolioRoundAt(DateTimeOffset at)
            => Observe(async () =>
            {
                Clock.SetInstant(at);
                await ThePortfolioRefreshRuns(PaymentsPlatformId);
            });

        /// <summary>Some Teams refresh one after the other, then Payments Platform does.</summary>
        protected Task<ObservedRounds> TheBackgroundCycleAt(DateTimeOffset at, params int[] teamIds)
            => Observe(async () =>
            {
                Clock.SetInstant(at);

                foreach (var teamId in teamIds)
                {
                    await TheTeamRefreshRuns(teamId);
                }

                await ThePortfolioRefreshRuns(PaymentsPlatformId);
            });

        /// <summary>
        /// Starts a periodic refresh of FSS and returns once the forecast it causes is part-way through its
        /// runs and held there. Only a forecast that computes can be held: a skipped one draws nothing.
        /// </summary>
        protected void AForecastIsUnderWayAfterTheRoundAt(DateTimeOffset at)
        {
            Clock.SetInstant(at);
            Draws.HoldOnceRunsHaveBegun(TheRunsBeforeAForecastIsHeld);

            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(FssId);

            Assert.That(Draws.IsHeldWithin(TheLongestAScenarioWaits), Is.True,
                "The forecast never reached the run it was to be held at, so nothing in this scenario happened during it.");
        }

        /// <summary>
        /// While the forecast is held, something nobody pressed asks for another forecast of the Portfolio, the
        /// way a changed Feature rank does. The queue keeps it as the one forecast to run after the one in flight.
        /// </summary>
        protected void AnotherForecastIsAlreadyWaitingBehindIt()
        {
            Factory.Services.GetRequiredService<IForecastUpdater>().TriggerUpdate(PaymentsPlatformId);
        }

        /// <summary>
        /// The operator presses the button and the forecast it causes is part-way through its runs and held
        /// there. The press forgot what was confirmed, so that forecast computes, and only a computing
        /// forecast can be held.
        /// </summary>
        protected async Task AForecastIsUnderWayAfterThePressAt(DateTimeOffset at, UserPress press)
        {
            Clock.SetInstant(at);
            Draws.HoldOnceRunsHaveBegun(TheRunsBeforeAForecastIsHeld);

            await Press(press);

            Assert.That(Draws.IsHeldWithin(TheLongestAScenarioWaits), Is.True,
                "The forecast the press caused never reached the run it was to be held at, so nothing in this scenario happened during it.");
        }

        /// <summary>
        /// The instance is started again, by an upgrade, a rollback or a restart, and does at start what it
        /// always does then: the start-up seeding, which is also what runs after a database is restored.
        /// </summary>
        protected void WhenTheInstanceStartsAgain()
        {
            using var scope = Factory.Services.CreateScope();

            DatabaseConfigurator.SeedDatabase(scope.ServiceProvider);
        }

        protected async Task TheRunningForecastIsLetGo()
        {
            Draws.LetItCarryOn();
            await TheQueueGoesIdle();
        }

        protected async Task WhenTheOperatorStopsTheRunningForecast()
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(new Uri($"{TaskListRoute}/{UpdateType.Forecasts}/{PaymentsPlatformId}/cancel", UriKind.Relative), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), $"Stop answered {(int)response.StatusCode}.");
        }

        // --- When: a person presses a button ---

        public enum UserPress
        {
            UpdateForecastOfThePortfolio,
            UpdateForecastsOfATeamsPortfolios,
            RefreshOfATeam,
            RefreshAllTeams,
            RefreshOfThePortfolio,
            RefreshAllPortfolios,
        }

        /// <summary>Presses the button and waits for the queue to settle and a forecast of Payments Platform to be recorded.</summary>
        protected Task<ObservedRounds> WhenTheOperatorPresses(UserPress press)
            => Observe(async () =>
            {
                var rowsBefore = (await TheForecastRowsOfPaymentsPlatform()).Count;

                await Press(press);

                await Until(
                    async () => !TheQueueIsBusy() && (await TheForecastRowsOfPaymentsPlatform()).Count > rowsBefore,
                    $"Pressing {press} never led to a forecast of {PaymentsPlatform}.");
            });

        /// <summary>The press only, for a scenario in which a forecast is held and the press has to land during it.</summary>
        protected async Task Press(UserPress press)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(new Uri(RouteOf(press), UriKind.Relative), null);

            Assert.That((int)response.StatusCode, Is.InRange(200, 299), $"Pressing {press} answered {(int)response.StatusCode}.");
        }

        private string RouteOf(UserPress press) => press switch
        {
            UserPress.UpdateForecastOfThePortfolio => $"/api/latest/forecast/update/{PaymentsPlatformId}",
            UserPress.UpdateForecastsOfATeamsPortfolios => $"/api/latest/forecast/update-portfolios-for-team/{FssId}",
            UserPress.RefreshOfATeam => $"/api/latest/teams/{FssId}",
            UserPress.RefreshAllTeams => "/api/latest/teams/update-all",
            UserPress.RefreshOfThePortfolio => $"/api/latest/portfolios/{PaymentsPlatformId}/refresh",
            UserPress.RefreshAllPortfolios => "/api/latest/portfolios/refresh-all",
            _ => throw new ArgumentOutOfRangeException(nameof(press), press, null),
        };

        // --- Then: what the operator and the reader can see ---

        protected static void ThenNothingWasSimulated(ObservedRounds rounds, string because)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rounds.NumbersDrawn, Is.Zero, $"{because}: the forecast simulated {rounds.NumbersDrawn} numbers.");
                Assert.That(rounds.NewForecastRows, Is.Not.Empty, $"{because}: no forecast of {PaymentsPlatform} was recorded at all.");
                Assert.That(rounds.NewForecastRows.Select(row => Bool(row, "skipped")), Is.All.True,
                    $"{because}: the refresh history does not mark the forecast as skipped. Rows: {Describe(rounds.NewForecastRows)}");
            }
        }

        protected static void ThenPaymentsPlatformWasForecastInFull(ObservedRounds rounds, string because)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rounds.NumbersDrawn, Is.GreaterThan(0), $"{because}, yet nothing was simulated.");
                Assert.That(rounds.NewForecastRows.Any(row => Bool(row, "skipped") == false && Bool(row, "success") == true), Is.True,
                    $"{because}, yet the refresh history records no computed forecast. Rows: {Describe(rounds.NewForecastRows)}");
            }
        }

        protected static void ThenTheForecastOfTheRoundFailed(ObservedRounds rounds)
            => Assert.That(rounds.NewForecastRows.Select(row => Bool(row, "success")), Has.Some.False,
                "The forecast this scenario breaks did not fail, so it says nothing about a failed one.");

        /// <summary>The Task Manager row of the forecast ends the way any finished forecast ends, within the time a scenario waits.</summary>
        protected Task ThenTheTaskManagerShowsTheForecastFinished()
            => Until(
                () => TheBrowserWasTold.LastStatusOf(UpdateType.Forecasts, PaymentsPlatformId) == UpdateProgress.Completed,
                $"The Task Manager row of the forecast of {PaymentsPlatform} never read as finished. The browser was told: {TheBrowserWasTold.Describe()}");

        protected static string? TheLineOfTheTeamRound(ObservedRounds rounds, string teamName)
            => rounds.OperatorLines.LastOrDefault(line => line.Contains($"Update completed | Team '{teamName}'", StringComparison.Ordinal));

        protected static readonly Regex ASkippedForecastOnTheLine = new(@" \| forecast=skipped after \d+ms \(inputs unchanged\)( \||$)", RegexOptions.None, TimeSpan.FromSeconds(1));

        /// <summary>Every Feature of Payments Platform with what its completion forecast says, read as the stored rows.</summary>
        protected IReadOnlyList<string> TheForecastValuesOfPaymentsPlatform()
            => [.. TheStoredForecastsOf(PaymentsPlatformId).Select(row => $"{row.ReferenceId} {row.Kind}: {row.Values}")];

        protected sealed record StoredForecast(string ReferenceId, string Kind, DateTime CreationTime, string Values);

        protected IReadOnlyList<StoredForecast> TheStoredForecastsOf(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var features = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>().GetAll()
                .Where(feature => feature.Portfolios.Any(owner => owner.Id == portfolioId))
                .OrderBy(feature => feature.ReferenceId, StringComparer.Ordinal);

            return [.. features.SelectMany(feature => feature.Forecasts.Select(forecast => AsStored(feature.ReferenceId, $"completion {forecast.TeamId}", forecast))
                .Concat(feature.StartForecasts.Select(forecast => AsStored(feature.ReferenceId, $"start {forecast.TeamId}", forecast))))];
        }

        private static StoredForecast AsStored(string referenceId, string kind, ForecastBase forecast)
            => new(referenceId, kind, DateTime.SpecifyKind(forecast.CreationTime, DateTimeKind.Utc),
                $"{forecast.TotalTrials} runs [{string.Join(", ", forecast.SimulationResults.OrderBy(day => day.Key).Select(day => $"{day.Key}:{day.Value}"))}]");

        /// <summary>Each Feature as the web app and the clients read it: when its forecast was last updated, and its dates.</summary>
        protected async Task<IReadOnlyDictionary<string, (DateTime LastUpdated, string Forecasts)>> TheFeaturesAsTheClientsReadThem(params string[] referenceIds)
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri("/api/latest/features", UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return document.RootElement.EnumerateArray()
                .Where(feature => Text(feature, "referenceId") is { } referenceId && referenceIds.Contains(referenceId))
                .ToDictionary(
                    feature => Text(feature, "referenceId")!,
                    feature => (feature.GetProperty("lastUpdated").GetDateTime().ToUniversalTime(), feature.GetProperty("forecasts").ToString()));
        }

        protected async Task<IReadOnlyList<JsonElement>> TheForecastRowsOfPaymentsPlatform()
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri("/api/latest/systeminfo/refreshlog", UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Refresh History reads the refresh log; it could not be read.");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return [.. document.RootElement.EnumerateArray()
                .Where(row => IsAForecast(row) && Number(row, "entityId") == PaymentsPlatformId)
                .OrderBy(row => Number(row, "id"))
                .Select(row => row.Clone())];
        }

        protected int TheSnapshotsOfTheDay(int deliveryId, DateOnly day)
        {
            using var scope = Factory.Services.CreateScope();

            return scope.ServiceProvider.GetRequiredService<IDeliveryMetricSnapshotRepository>()
                .GetByDelivery(deliveryId)
                .Count(snapshot => snapshot.RecordedDay == day);
        }

        // --- Plumbing ---

        private async Task<ObservedRounds> Observe(Func<Task> rounds)
        {
            var drawnBefore = Draws.NumbersDrawn;
            var rowsBefore = (await TheForecastRowsOfPaymentsPlatform()).Count;

            await rounds();

            var rowsAfter = await TheForecastRowsOfPaymentsPlatform();

            return new ObservedRounds(Draws.NumbersDrawn - drawnBefore, [.. rowsAfter.Skip(rowsBefore)], [.. TheOperatorVisibleLines]);
        }

        private static bool IsAForecast(JsonElement row)
            => row.TryGetProperty("type", out var type)
               && (type.ValueKind == JsonValueKind.String ? type.GetString() == nameof(RefreshType.Forecast) : type.GetInt32() == (int)RefreshType.Forecast);

        private bool TheQueueIsBusy() => Factory.Services.GetRequiredService<IUpdateStatusStore>().HasActiveWork();

        protected static Task Until(Func<bool> condition, string failure) => Until(() => Task.FromResult(condition()), failure);

        protected static async Task Until(Func<Task<bool>> condition, string failure)
        {
            var deadline = DateTime.UtcNow + TheLongestAScenarioWaits;

            while (!await condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail(failure);
                }

                await Task.Delay(20);
            }
        }

        /// <summary>
        /// The work tracking system as one test sees it: the Features of each Portfolio and the items of each
        /// Team, which a scenario changes the way somebody working in the tracker would. Every change moves the
        /// record's change stamp, so a refresh that scans for what moved sees it, as it would in the tracker.
        /// </summary>
        protected sealed class WhatTheTrackerHolds
        {
            private static readonly DateTime TheFirstChange = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            private readonly Lock gate = new();
            private readonly Dictionary<string, List<(string ReferenceId, string Name, string Order, DateTime ChangedAt)>> featuresByPortfolio = [];
            private readonly Dictionary<string, List<(string ReferenceId, string Parent, DateTime ChangedAt)>> itemsByTeam = [];
            private int changes;

            public void HoldsFeature(string portfolio, string referenceId, string name, string order)
            {
                lock (gate)
                {
                    FeaturesIn(portfolio).Add((referenceId, name, order, AChange()));
                }
            }

            public void NoLongerHoldsFeature(string portfolio, string referenceId)
            {
                lock (gate)
                {
                    FeaturesIn(portfolio).RemoveAll(feature => feature.ReferenceId == referenceId);
                }
            }

            public void Reorders(string portfolio, string referenceId, string order)
            {
                lock (gate)
                {
                    var features = FeaturesIn(portfolio);
                    var index = features.FindIndex(feature => feature.ReferenceId == referenceId);
                    features[index] = features[index] with { Order = order, ChangedAt = AChange() };
                }
            }

            public void HoldsItems(string team, string parent, params string[] referenceIds)
            {
                lock (gate)
                {
                    ItemsOf(team).AddRange(referenceIds.Select(referenceId => (referenceId, parent, AChange())));
                }
            }

            public void NoLongerShowsTheTeam(string team, params string[] referenceIds)
            {
                lock (gate)
                {
                    ItemsOf(team).RemoveAll(item => referenceIds.Length == 0 || referenceIds.Contains(item.ReferenceId));
                }
            }

            public IEnumerable<WorkItem> WorkItemsOf(Team team)
            {
                lock (gate)
                {
                    return [.. ItemsOf(team.Name).Select(item => new WorkItem(new WorkItemBase
                    {
                        ReferenceId = item.ReferenceId,
                        Name = item.ReferenceId,
                        Type = "Story",
                        State = "New",
                        StateCategory = StateCategories.ToDo,
                        Order = "1",
                        ParentReferenceId = item.Parent,
                        CreatedDate = TheFirstChange,
                        LastChangedRemote = item.ChangedAt,
                    }, team))];
                }
            }

            public List<RemoteRecordStamp> StampsOfTheItemsOf(Team team)
            {
                lock (gate)
                {
                    return ItemsOf(team.Name).ConvertAll(item => new RemoteRecordStamp(item.ReferenceId, item.ChangedAt));
                }
            }

            public List<RemoteRecordStamp> StampsOfTheFeaturesOf(Portfolio portfolio)
            {
                lock (gate)
                {
                    return FeaturesIn(portfolio.Name).ConvertAll(feature => new RemoteRecordStamp(feature.ReferenceId, feature.ChangedAt));
                }
            }

            public List<Feature> FeaturesOf(Portfolio portfolio)
            {
                lock (gate)
                {
                    return [.. FeaturesIn(portfolio.Name).Select(feature => new Feature(new WorkItemBase
                    {
                        ReferenceId = feature.ReferenceId,
                        Name = feature.Name,
                        Type = "Epic",
                        State = "New",
                        StateCategory = StateCategories.ToDo,
                        Order = feature.Order,
                        ParentReferenceId = string.Empty,
                        CreatedDate = TheFirstChange,
                        LastChangedRemote = feature.ChangedAt,
                    }))];
                }
            }

            private DateTime AChange() => TheFirstChange.AddMinutes(++changes);

            private List<(string ReferenceId, string Name, string Order, DateTime ChangedAt)> FeaturesIn(string portfolio)
            {
                if (!featuresByPortfolio.TryGetValue(portfolio, out var features))
                {
                    features = [];
                    featuresByPortfolio[portfolio] = features;
                }

                return features;
            }

            private List<(string ReferenceId, string Parent, DateTime ChangedAt)> ItemsOf(string team)
            {
                if (!itemsByTeam.TryGetValue(team, out var items))
                {
                    items = [];
                    itemsByTeam[team] = items;
                }

                return items;
            }
        }

        /// <summary>
        /// The simulation's random numbers, counted. A forecast that was skipped draws none, so "nothing was
        /// simulated" is a count that stayed where it was, wherever in the forecast the skip is decided. It can
        /// also hold a computing forecast part-way, so a press can land while it runs.
        /// </summary>
        protected sealed class DrawsCountedAndHeld : IDrawStreamFactory, IDrawStream, IDisposable
        {
            private readonly CountedDrawStream draws = new();
            private long numbersDrawn;

            public long NumbersDrawn => Interlocked.Read(ref numbersDrawn);

            public long StartingNumber => draws.StartingNumber;

            public IDrawStream ForOneRun()
            {
                draws.ForOneRun();
                return this;
            }

            public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
            {
                Interlocked.Increment(ref numbersDrawn);
                return draws.Draw(trial, team, day, ordinal, maxExclusive);
            }

            public void HoldOnceRunsHaveBegun(int runs) => draws.HoldOnceRunsHaveBegun(runs);

            public bool IsHeldWithin(TimeSpan wait) => draws.IsHeldWithin(wait);

            public void LetItCarryOn() => draws.LetItCarryOn();

            public void Dispose() => draws.Dispose();
        }
    }
}
