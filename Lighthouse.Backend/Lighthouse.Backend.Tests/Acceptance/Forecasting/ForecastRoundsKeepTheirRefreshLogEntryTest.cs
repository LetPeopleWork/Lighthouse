using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.Forecasting.Adapters.Background;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Application.WriteBack;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.UpdateQueue;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.Architecture;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Mono.Cecil;
using Moq;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.Acceptance.Forecasting
{
    /// <summary>
    /// The System info page lists one refresh-log entry for every forecast round, and an operator reads from it
    /// whether the round worked, was stopped or found nothing new. That entry must stay exactly what it is, while
    /// the forecasting code stops writing it itself and Work Items writes it on hearing that the round finished.
    /// Every field of the entry is written out here by hand for each of the four ways a round ends, and a log
    /// write that throws must never fail the round it reports on.
    ///
    /// The round is asked for as a person asks for one, through the forecast updater as the product composes it,
    /// over a database of the scenario's own and with no web host. Only the forecast's answer and the operator's
    /// stop are doubles; the reaction to the finished round is registered exactly as the start-up code registers it.
    /// </summary>
    [TestFixture]
    public class ForecastRoundsKeepTheirRefreshLogEntryTest
    {
        private const string FinishedRoundEvent = "Lighthouse.Backend.Modules.Forecasting.Contracts.ForecastRoundFinished";

        private const string ThePortfolioName = "Pinned forecast Portfolio";

        private static readonly DateTimeOffset RoundFinishesAt = new(2026, 11, 20, 14, 45, 0, TimeSpan.Zero);

        private static IReadOnlyList<ServiceDescriptor> reactionsToAFinishedRound = [];

        private ServicePortHarness harness = null!;

        private Mock<IForecastService> forecast = null!;

        private UpdateCancellationContext operatorStop = null!;

        private CapturedLogs logs = null!;

        private bool theRefreshLogWriteThrows;

        private RoundRunner rounds = null!;

        [OneTimeSetUp]
        public static void ReadTheReactionsTheStartUpCodeRegisters()
        {
            var finishedRound = StartUpDomainEventHandlers.EventNamed(FinishedRoundEvent);
            reactionsToAFinishedRound = finishedRound is null ? [] : StartUpDomainEventHandlers.RegistrationsOf(finishedRound);
        }

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            forecast = new Mock<IForecastService>();
            operatorStop = new UpdateCancellationContext();
            logs = new CapturedLogs();
            theRefreshLogWriteThrows = false;
            rounds = new RoundRunner();
            harness = new ServicePortHarness(RegisterTheRoundAsTheProductBuildsIt);
            rounds.RunIn(harness);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            harness.Dispose();
            logs.Dispose();
        }

        [Test]
        public void A_round_that_forecasts_its_Features_logs_a_successful_entry_counting_them()
        {
            var portfolioId = GivenAPortfolioWithFeatures("Self-service export", "Legal sign-off", "Archive");
            GivenTheForecastComputesNewForecasts();

            WhenAForecastIsAskedFor(portfolioId);

            ThenTheRefreshLogHoldsOneForecastEntry(portfolioId, itemCount: 3, success: true, cancelled: false, skipped: false);
        }

        [Test]
        public void A_round_whose_forecast_fails_logs_a_failed_entry_counting_nothing()
        {
            var portfolioId = GivenAPortfolioWithFeatures("Self-service export", "Legal sign-off");
            GivenTheForecastFails();

            WhenAForecastIsAskedFor(portfolioId);

            ThenTheRefreshLogHoldsOneForecastEntry(portfolioId, itemCount: 0, success: false, cancelled: false, skipped: false);
        }

        [Test]
        public void A_round_the_operator_stops_logs_a_cancelled_entry()
        {
            var portfolioId = GivenAPortfolioWithFeatures("Self-service export", "Legal sign-off");
            GivenTheOperatorStopsTheForecast();

            WhenAForecastIsAskedFor(portfolioId);

            ThenTheRefreshLogHoldsOneForecastEntry(portfolioId, itemCount: 0, success: false, cancelled: true, skipped: false);
        }

        [Test]
        public void A_round_that_finds_nothing_new_logs_a_successful_skipped_entry_counting_the_Features()
        {
            var portfolioId = GivenAPortfolioWithFeatures("Self-service export", "Legal sign-off");
            GivenTheForecastFindsTheForecastsUnchanged();

            WhenAForecastIsAskedFor(portfolioId);

            ThenTheRefreshLogHoldsOneForecastEntry(portfolioId, itemCount: 2, success: true, cancelled: false, skipped: true);
        }

        [Test]
        public void A_round_whose_refresh_log_write_fails_still_completes_and_the_failure_is_logged()
        {
            var portfolioId = GivenAPortfolioWithFeatures("Self-service export");
            GivenTheForecastComputesNewForecasts();
            GivenWritingTheRefreshLogFails();

            WhenAForecastIsAskedFor(portfolioId);

            ThenTheRoundCompleted();
            ThenTheFailedLogWriteIsReportedAsAFailedReactionToTheFinishedRound();
            ThenTheRefreshLogHoldsNoForecastEntry();
        }

        [Test]
        public void The_forecast_round_no_longer_writes_the_refresh_log_itself()
        {
            var writers = WhenTheForecastUpdatersCompiledCodeIsRead();

            ThenNothingInTheForecastUpdaterNamesTheRefreshLogService(writers);
        }

        // --- Given ---

        private int GivenAPortfolioWithFeatures(params string[] featureNames)
        {
            using var scope = harness.NewScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Pinned forecast connection",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            var portfolio = new Portfolio { Name = ThePortfolioName, WorkTrackingSystemConnectionId = connection.Id };
            portfolio.UpdateFeatures(featureNames.Select((name, index) => new Feature
            {
                ReferenceId = $"FEAT-{index + 1}",
                Name = name,
                Type = "Feature",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Url = string.Empty,
                ParentReferenceId = string.Empty,
                Order = $"{index + 1}",
            }));
            dbContext.Portfolios.Add(portfolio);
            dbContext.SaveChanges();

            return portfolio.Id;
        }

        private void GivenTheForecastComputesNewForecasts()
            => forecast
                .Setup(service => service.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ForecastRunOutcome.Computed(RunMark: null, FreshFingerprint: null));

        private void GivenTheForecastFindsTheForecastsUnchanged()
            => forecast
                .Setup(service => service.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Portfolio portfolio, DateOnly _, CancellationToken _) =>
                    new ForecastRunOutcome.ConfirmedUnchanged([.. portfolio.Features.Select(feature => feature.Id)]));

        private void GivenTheForecastFails()
            => forecast
                .Setup(service => service.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The forecast could not read its history."));

        private void GivenTheOperatorStopsTheForecast()
        {
            var stopped = new CancellationToken(canceled: true);
            operatorStop.Current = stopped;
            forecast
                .Setup(service => service.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(stopped));
        }

        private void GivenWritingTheRefreshLogFails() => theRefreshLogWriteThrows = true;

        // --- When ---

        private void WhenAForecastIsAskedFor(int portfolioId)
            => rounds.Run(() => harness.NewScope().ServiceProvider.GetRequiredService<IForecastUpdater>().TriggerImmediateUpdate(portfolioId));

        private static List<IlDependent> WhenTheForecastUpdatersCompiledCodeIsRead()
        {
            using var production = IlDependencies.ReadModule(typeof(IForecastUpdater).Assembly);

            return IlDependencies.DependentsIn(production, @"^Lighthouse\.Backend\.Modules\.Forecasting\.Adapters\.Background$", NamesTheRefreshLogService)
                .Where(dependent => dependent.TypeName == "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater")
                .ToList();
        }

        private static bool NamesTheRefreshLogService(TypeReference type)
            => type.FullName == "Lighthouse.Backend.Modules.WorkItems.Contracts.IRefreshLogService";

        // --- Then ---

        private void ThenTheRefreshLogHoldsOneForecastEntry(int portfolioId, int itemCount, bool success, bool cancelled, bool skipped)
        {
            var entries = ForecastEntriesInTheRefreshLog();

            Assert.That(entries, Has.Count.EqualTo(1),
                $"After one forecast round the refresh log holds {entries.Count} forecast entries, not one. The System info page shows one entry per round; " +
                "a missing entry means nothing reacts to the finished round (check the reaction is registered at start-up and is the only writer), two mean the round still writes its own entry as well.");

            var entry = entries[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.EntityId, Is.EqualTo(portfolioId), "The forecast entry names another Portfolio than the one forecast; the entry must carry the round's Portfolio id.");
                Assert.That(entry.EntityName, Is.EqualTo(ThePortfolioName), "The forecast entry names the Portfolio differently; it must carry the Portfolio's name as the round read it.");
                Assert.That(entry.ItemCount, Is.EqualTo(itemCount), $"The forecast entry counts {entry.ItemCount} Features, not {itemCount}; a round that finished counts the Portfolio's Features, a round that failed or was stopped counts none.");
                Assert.That(entry.Mode, Is.EqualTo(SyncMode.Full), "A forecast fetches nothing from the tracker, so its entry reads as a full sync of nothing, as it always has.");
                Assert.That(entry.RecordsScanned, Is.Zero, "A forecast scans no tracker records, so its entry must say none were scanned.");
                Assert.That(entry.RecordsFetched, Is.Zero, "A forecast fetches no tracker records, so its entry must say none were fetched.");
                Assert.That(entry.ExecutedAt, Is.EqualTo(RoundFinishesAt.UtcDateTime), "The forecast entry carries another time than the instant the round finished on Lighthouse's clock; it must carry that instant.");
                Assert.That(entry.DurationMs, Is.GreaterThanOrEqualTo(0), "The forecast entry carries a negative duration; it must carry how long the round took.");
                Assert.That(entry.Success, Is.EqualTo(success), $"The forecast entry says success {entry.Success}, not {success}: a round that forecast or found nothing new succeeded, a failed or stopped round did not. The round ended with: {rounds.WhatTheLastRoundThrew?.ToString() ?? "nothing thrown"}.");
                Assert.That(entry.Cancelled, Is.EqualTo(cancelled), $"The forecast entry says cancelled {entry.Cancelled}, not {cancelled}: only a round the operator stopped is cancelled, and its entry must still be written although the round was stopped.");
                Assert.That(entry.Skipped, Is.EqualTo(skipped), $"The forecast entry says skipped {entry.Skipped}, not {skipped}: only a round that found the forecasts unchanged is skipped.");
            }
        }

        private void ThenTheRefreshLogHoldsNoForecastEntry()
        {
            var entries = ForecastEntriesInTheRefreshLog();

            Assert.That(entries, Is.Empty,
                $"The refresh-log write was made to fail, yet the refresh log holds {entries.Count} forecast entries. The failing write must leave no entry behind.");
        }

        private void ThenTheRoundCompleted()
            => Assert.That(rounds.WhatTheLastRoundThrew, Is.Null,
                $"The forecast round failed with '{rounds.WhatTheLastRoundThrew?.Message}' because writing its refresh-log entry failed. " +
                "The entry is a report on the round, not part of it: a failing write must be logged by the reaction's dispatcher and the round must complete.");

        private void ThenTheFailedLogWriteIsReportedAsAFailedReactionToTheFinishedRound()
        {
            var reported = logs.Errors.Where(message =>
                message.Contains("ForecastRoundFinishedRefreshLogHandler", StringComparison.Ordinal)
                && message.Contains("ForecastRoundFinished", StringComparison.Ordinal)).ToList();

            Assert.That(reported, Has.Count.EqualTo(1),
                $"The errors logged were [{string.Join(" | ", logs.Errors)}], and not exactly one names the Work Items reaction to the finished forecast round and that event. " +
                "An operator must find in the log that writing the round's refresh-log entry failed; the dispatcher logs a failing reaction with both names.");
        }

        private static void ThenNothingInTheForecastUpdaterNamesTheRefreshLogService(List<IlDependent> writers)
            => Assert.That(writers, Is.Empty,
                $"The forecast updater still names the refresh-log service ({string.Join(", ", writers.SelectMany(writer => writer.Targets))}). " +
                "Forecasting must not command Work Items to write its entry: it announces that the round finished, and Work Items writes the entry itself. Remove the direct write.");

        private List<RefreshLog> ForecastEntriesInTheRefreshLog()
        {
            using var scope = harness.NewScope();

            return [.. scope.ServiceProvider.GetRequiredService<IRepository<RefreshLog>>().GetAll().Where(entry => entry.Type == RefreshType.Forecast)];
        }

        // --- The round as the product composes it ---

        private void RegisterTheRoundAsTheProductBuildsIt(IServiceCollection services)
        {
            services.AddSingleton<ILoggerProvider>(logs);
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(RoundFinishesAt));
            services.AddSingleton(RefreshLogKeepsItsSeededRuns());
            services.AddSingleton(PortDoubles.PremiumLicence());
            services.AddSingleton(operatorStop);
            services.AddSingleton<IUpdateStatusStore>(provider => new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), provider.GetRequiredService<ILighthouseClock>()));
            services.AddSingleton<IUpdateQueueService>(rounds);
            services.AddSingleton(new ForecastSimulationLimits(1, 1) { CoresSeen = 8 });
            services.AddSingleton(provider => new ForecastLane(provider.GetRequiredService<ForecastSimulationLimits>()));
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddSingleton<IForecastUpdater, ForecastUpdater>();
            services.AddScoped(_ => forecast.Object);
            services.AddScoped<IWriteBackCollector, NothingToWriteBack>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
            services.AddScoped<IRepository<RefreshLog>, RefreshLogRepository>();

            services.AddScoped<RefreshLogService>();
            services.AddScoped<IRefreshLogService>(provider => theRefreshLogWriteThrows
                ? new RefreshLogWriteThatFails()
                : provider.GetRequiredService<RefreshLogService>());

            foreach (var reaction in reactionsToAFinishedRound)
            {
                services.Add(reaction);
            }
        }

        /// <summary>The refresh log keeps a number of runs per entity; any number above one keeps this round's entry.</summary>
        private static IAppSettingService RefreshLogKeepsItsSeededRuns()
        {
            var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
            settings.Setup(service => service.GetRefreshLogRetentionRuns()).Returns(50);
            return settings.Object;
        }

        /// <summary>
        /// The update queue runs each round on a scope of its own and keeps what the round threw, so a round that
        /// fails is recorded rather than surfacing at the caller. This one does the same, at once and on the
        /// caller's thread, so a scenario sees the round's outcome as soon as it asked for it. The scenario's
        /// container is only built after the queue is registered, so it is handed over once it exists.
        /// </summary>
        private sealed class RoundRunner : IUpdateQueueService
        {
            private ServicePortHarness? harness;

            public Exception? WhatTheLastRoundThrew { get; private set; }

            public void RunIn(ServicePortHarness scenarioHarness) => harness = scenarioHarness;

            public void Run(Action askForTheRound) => askForTheRound();

            public void EnqueueUpdate(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask)
            {
                ArgumentNullException.ThrowIfNull(updateTask);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
                var scenarioHarness = harness ?? throw new InvalidOperationException("The round was asked for before the scenario's container existed.");

                WhatTheLastRoundThrew = null;
                using var scope = scenarioHarness.NewScope();
                try
                {
                    updateTask(scope.ServiceProvider).GetAwaiter().GetResult();
                }
#pragma warning disable CA1031 // the queue records whatever a round throws, which is what this scenario observes
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    WhatTheLastRoundThrew = exception;
                }
            }

            public Task EnqueueAndAwaitAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, CancellationToken cancellationToken = default)
            {
                EnqueueUpdate(updateType, id, updateTask);
                return Task.CompletedTask;
            }

            public void HoldUntilQueuedWorkClears(UpdateKey heldFor, IReadOnlyCollection<UpdateKey> waitingOn, Action onQueuedWorkCleared)
                => throw new InvalidOperationException("A forecast asked for by a person never waits for queued work.");

            public bool IsHeld(UpdateKey heldFor) => false;

            public Task CancelAsync(UpdateKey key) => Task.CompletedTask;

            public Task CancelAllAsync() => Task.CompletedTask;

            public Task DrainAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        /// <summary>The Portfolio maps no forecast to a tracker field, so the round stages nothing to write back.</summary>
        private sealed class NothingToWriteBack : IWriteBackCollector
        {
            public void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
            {
                ArgumentNullException.ThrowIfNull(connection);
                ArgumentNullException.ThrowIfNull(updates);
            }

            public Task<IReadOnlyList<WriteBackResult>> FlushAsync() => Task.FromResult<IReadOnlyList<WriteBackResult>>([]);
        }

        private sealed class RefreshLogWriteThatFails : IRefreshLogService
        {
            public Task LogRefreshAsync(RefreshLog entry) => throw new IOException("The refresh log could not be written: the disk is full.");

            public IEnumerable<RefreshLog> GetRefreshLogs() => [];

            public Task RemoveRefreshLogsForEntity(RefreshType type, int entityId) => Task.CompletedTask;

            public Task RemoveOrphanedRefreshLogs() => Task.CompletedTask;
        }

        private sealed class CapturedLogs : ILoggerProvider
        {
            private readonly ConcurrentQueue<string> errors = new();

            public IReadOnlyList<string> Errors => [.. errors];

            public ILogger CreateLogger(string categoryName) => new Capturing(this);

            public void Dispose()
            {
                // Nothing to release: the messages stay readable for the scenario's Then steps.
            }

            private sealed class Capturing(CapturedLogs owner) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    if (logLevel >= LogLevel.Error)
                    {
                        owner.errors.Enqueue(formatter(state, exception));
                    }
                }
            }
        }
    }
}
