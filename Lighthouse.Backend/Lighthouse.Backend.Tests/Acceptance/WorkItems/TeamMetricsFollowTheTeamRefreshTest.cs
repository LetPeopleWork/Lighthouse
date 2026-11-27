using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.UpdateQueue;
using System.Globalization;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Modules.WorkItems.Application.Dependencies;
using Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.Architecture;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.Forecasting.Application.WriteBack;

namespace Lighthouse.Backend.Tests.Acceptance.WorkItems
{
    /// <summary>
    /// After a Team refresh, the Team's metrics follow the refreshed Work Items: the metrics answers, the Team's update
    /// time and its Feature WIP are what they were before Flow metrics took the update over from Work Items, and the
    /// update runs before every other reaction to the refresh. When saving the update fails, the refresh still counts
    /// as succeeded, the Team keeps its update time and Feature WIP of before (so it is due again and the next refresh
    /// retries), every other reaction still runs in its order, and nobody is answered from the metrics of before.
    ///
    /// The refresh is asked of the Team updater as the product builds it, over a database of the scenario's own, with
    /// no web host, reading a CSV upload. The reactions to a Team refresh are the ones the start-up code registers, in
    /// its order. The reactions this change leaves alone stand in by name, so the scenario sees that and when they run;
    /// any other reaction is the product's own. Every value is written out: a stale cached answer, an update that never
    /// ran, ran late or failed the refresh turns this red.
    /// </summary>
    [TestFixture]
    public class TeamMetricsFollowTheTeamRefreshTest
    {
        private static readonly DateTimeOffset FirstRefreshAt = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);

        private static readonly DateTimeOffset NextRefreshAt = new(2026, 10, 19, 9, 30, 0, TimeSpan.Zero);

        private static readonly DateTime ThroughputFrom = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime ThroughputUntil = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime FeaturesInProgressOn = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

        private const string MetricsSaved = "the Team's metrics were saved";

        private const string MetricsSaveFailed = "the Team's metrics save failed";

        private const string TheTeamMetricsReaction = "TeamDataRefreshedTeamMetricsUpdateHandler";

        /// <summary>The reactions to a Team refresh this change leaves as they are, in the order they run.</summary>
        private static readonly string[] TheOtherReactions =
        [
            "ReconstructionMemo",
            "TeamDataRefreshedForecastTriggerHandler",
            "BlockedCountSnapshotRecordingHandler",
            "PercentilesOverTimeRecordingHandler",
            "ProcessBehaviorRecordingHandler",
            "DemoBlockedHistoryBackfillHandler",
            "DemoPercentilesBackfillHandler",
        ];

        private const string UploadHeader = "ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id,Tags";

        /// <summary>Two Work Items in progress under FEAT-1 and FEAT-2, one done on 10 October.</summary>
        private static readonly string FirstUpload = string.Join('\n',
            UploadHeader,
            "WI-1,Write the guide,To Do,User Story,,,2026-10-01,FEAT-1,",
            "WI-2,Fix the export,In Progress,Bug,2026-10-05,,2026-10-02,FEAT-1,",
            "WI-3,Wait on legal,In Progress,User Story,2026-10-06,,2026-10-03,FEAT-2,",
            "WI-4,Old cleanup,Done,User Story,2026-10-01,2026-10-10,2026-09-30,,");

        /// <summary>A day later the two in progress left the upload; one in progress under FEAT-3 and one done on 15 October are new.</summary>
        private static readonly string NextUpload = string.Join('\n',
            UploadHeader,
            "WI-1,Write the guide,To Do,User Story,,,2026-10-01,FEAT-1,",
            "WI-4,Old cleanup,Done,User Story,2026-10-01,2026-10-10,2026-09-30,,",
            "WI-5,Add the audit trail,In Progress,User Story,2026-10-12,,2026-10-11,FEAT-3,",
            "WI-6,Tidy the importer,Done,Bug,2026-10-08,2026-10-15,2026-10-07,FEAT-1,");

        private static IReadOnlyList<Type> startUpTeamRefreshReactions = [];

        private ServicePortHarness harness = null!;
        private FakeLighthouseClock clock = null!;
        private FakeTimeProvider timeProvider = null!;
        private RefreshJournal journal = null!;
        private MetricsSaveWatch metricsSave = null!;
        private RecordingLogger<DomainEventDispatcher> reactionFailures = null!;
        private Func<IServiceProvider, Task>? queuedRefresh;
        private int failuresBeforeThisStep;
        private TeamUpdater updater = null!;

        [OneTimeSetUp]
        public void ReadTheTeamRefreshReactionsTheStartUpCodeRegisters()
        {
            startUpTeamRefreshReactions = StartUpDomainEventHandlers.Read(typeof(TeamDataRefreshed)).Of<TeamDataRefreshed>();
        }

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            clock = new FakeLighthouseClock(FirstRefreshAt);
            timeProvider = new FakeTimeProvider(FirstRefreshAt);
            journal = new RefreshJournal();
            metricsSave = new MetricsSaveWatch(journal);
            reactionFailures = new RecordingLogger<DomainEventDispatcher>();
            harness = new ServicePortHarness(RegisterTheRefreshAsTheProductBuildsIt);

            var queue = new Mock<IUpdateQueueService>();
            queue
                .Setup(service => service.EnqueueUpdate(It.IsAny<UpdateType>(), It.IsAny<int>(), It.IsAny<Func<IServiceProvider, Task>>()))
                .Callback((UpdateType _, int _, Func<IServiceProvider, Task> work) => queuedRefresh = work);

            using var scope = harness.NewScope();
            updater = new TeamUpdater(
                NullLogger<TeamUpdater>.Instance,
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                queue.Object,
                timeProvider);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            updater.Dispose();
            harness.Dispose();
        }

        [Test]
        public async Task A_Teams_refresh_updates_its_metrics_before_any_other_reaction_and_answers_from_the_refreshed_Work_Items()
        {
            var teamId = GivenATeamReading(FirstUpload);
            await GivenTheTeamWasRefreshed(teamId);
            GivenTheTeamsMetricsWereRead(teamId);
            GivenADayLaterTheTeamReads(teamId, NextUpload);

            var refresh = await WhenTheTeamIsRefreshed(teamId);

            ThenTheRefreshSucceeded(refresh, teamId);
            ThenTheReactionsRanInThisOrder(MetricsSaved, TheOtherReactions);
            ThenNoReactionFailed();
            ThenTheTeamIsStoredWith(teamId, updatedAt: "2026-10-19 09:30", featureWip: 1);
            ThenTheMetricsAnswer(teamId, throughput: 2, featuresInProgress: ["FEAT-3"]);
        }

        [Test]
        public async Task A_failed_metrics_save_leaves_the_refresh_succeeded_the_Team_due_again_and_every_other_reaction_run()
        {
            var teamId = GivenATeamReading(FirstUpload);
            await GivenTheTeamWasRefreshed(teamId);
            GivenTheTeamsMetricsWereRead(teamId);
            GivenADayLaterTheTeamReads(teamId, NextUpload);
            GivenSavingTheTeamsMetricsFailsOnce();

            var refresh = await WhenTheTeamIsRefreshed(teamId);

            ThenTheRefreshSucceeded(refresh, teamId);
            ThenTheReactionsRanInThisOrder(MetricsSaveFailed, TheOtherReactions);
            ThenOneReactionFailureWasLoggedNaming(TheTeamMetricsReaction);
            ThenTheTeamIsStoredWith(teamId, updatedAt: "2026-10-18 09:30", featureWip: 2);
            ThenTheMetricsAnswer(teamId, throughput: 2, featuresInProgress: ["FEAT-3"]);
        }

        [Test]
        public async Task A_refresh_announced_for_a_Team_that_was_deleted_updates_no_metrics_and_fails_no_reaction()
        {
            var teamId = GivenATeamReading(FirstUpload);
            GivenTheTeamWasDeleted(teamId);

            await WhenTheRefreshOfTheTeamIsAnnounced(teamId);

            ThenTheReactionsRanInThisOrder(null, TheOtherReactions);
            ThenNoReactionFailed();
        }

        // --- Given ---

        private int GivenATeamReading(string upload)
        {
            using var scope = harness.NewScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var team = new Team
            {
                Name = "Metrics Team",
                DataRetrievalValue = upload,
                WorkTrackingSystemConnection = ACsvConnection(),
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                AutomaticallyAdjustFeatureWIP = true,
            };
            dbContext.Teams.Add(team);
            dbContext.SaveChanges();

            return team.Id;
        }

        private async Task GivenTheTeamWasRefreshed(int teamId)
        {
            var refresh = await WhenTheTeamIsRefreshed(teamId);
            Assert.That(refresh.Failure, Is.Null, $"The Team's first refresh, which only sets the scene, failed: {refresh.Failure}");
        }

        /// <summary>Reading the answers keeps them in the metrics cache, so an answer that does not move after the refresh was served from it.</summary>
        private void GivenTheTeamsMetricsWereRead(int teamId)
        {
            var (throughput, featuresInProgress) = TheMetricsAnswersOf(teamId);
            Assert.That((throughput, string.Join(", ", featuresInProgress)), Is.EqualTo((1, "FEAT-1, FEAT-2")),
                "Before the next refresh the Team has one Work Item done in the window and Work Items in progress under FEAT-1 and FEAT-2; the scene is not what the scenario expects.");
        }

        private void GivenADayLaterTheTeamReads(int teamId, string upload)
        {
            using (var scope = harness.NewScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Teams.Single(team => team.Id == teamId).DataRetrievalValue = upload;
                dbContext.SaveChanges();
            }

            clock.SetInstant(NextRefreshAt);
            timeProvider.SetUtcNow(NextRefreshAt);
        }

        private void GivenSavingTheTeamsMetricsFailsOnce() => metricsSave.FailTheNextSave();

        private void GivenTheTeamWasDeleted(int teamId)
        {
            using var scope = harness.NewScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Teams.Remove(dbContext.Teams.Single(team => team.Id == teamId));
            dbContext.SaveChanges();
        }

        private static WorkTrackingSystemConnection ACsvConnection()
        {
            var connection = new WorkTrackingSystemFactory(NullLogger<WorkTrackingSystemFactory>.Instance)
                .CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            connection.Name = "Metrics upload";
            connection.Options.Single(option => option.Key == CsvWorkTrackingOptionNames.DateTimeFormat).Value = "yyyy-MM-dd";

            return connection;
        }

        // --- When ---

        private async Task<RefreshRun> WhenTheTeamIsRefreshed(int teamId)
        {
            journal.StartAfresh();
            failuresBeforeThisStep = reactionFailures.Written(LogLevel.Error).Count;
            queuedRefresh = null;

            updater.TriggerUpdate(teamId);
            Assert.That(queuedRefresh, Is.Not.Null, "The Team updater queued no refresh for the Team, so there is nothing to observe.");

            using var scope = harness.NewScope();
            try
            {
                await queuedRefresh!(scope.ServiceProvider);
                return new RefreshRun(null);
            }
#pragma warning disable CA1031 // the scenario observes how a refresh ends, a failure included
            catch (Exception failure)
#pragma warning restore CA1031
            {
                return new RefreshRun(failure);
            }
        }

        private async Task WhenTheRefreshOfTheTeamIsAnnounced(int teamId)
        {
            journal.StartAfresh();
            failuresBeforeThisStep = reactionFailures.Written(LogLevel.Error).Count;

            using var scope = harness.NewScope();
            await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>().PublishAsync(new TeamDataRefreshed(teamId));
        }

        // --- Then ---

        private void ThenTheRefreshSucceeded(RefreshRun refresh, int teamId)
        {
            var logged = Read(dbContext => dbContext.RefreshLogs
                .Where(log => log.EntityId == teamId && log.Type == RefreshType.Team)
                .OrderByDescending(log => log.Id)
                .Select(log => (bool?)log.Success)
                .FirstOrDefault());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refresh.Failure, Is.Null,
                    $"The Team refresh ended with {refresh.Failure?.GetType().Name}: {refresh.Failure?.Message}. Why it matters: the Work Items were " +
                    "read and saved, so a failure of the metrics update must not fail the refresh. How to fix: update the Team's metrics in " +
                    "the Flow metrics reaction to the refresh announcement, where a failure is logged and the refresh goes on, not inside the refresh.");
                Assert.That(logged, Is.True,
                    $"The refresh history row of the Team says Success {logged?.ToString() ?? "(no row)"}, not True. Why it matters: Refresh History " +
                    "would show a refresh as failed whose Work Items were stored. How to fix: let the refresh return once its Work Items are saved and announced.");
            }
        }

        private void ThenTheReactionsRanInThisOrder(string? metricsUpdate, string[] otherReactions)
        {
            string[] expected = metricsUpdate is null ? otherReactions : [metricsUpdate, .. otherReactions];
            var read = journal.All;

            Assert.That(read, Is.EqualTo(expected),
                $"After the refresh these ran{Environment.NewLine}  {Lines(read)}{Environment.NewLine}not{Environment.NewLine}  {Lines(expected)}{Environment.NewLine}" +
                "Why it matters: the Team's metrics must be updated, or at least their cache dropped, before any other reaction reads them, and " +
                "one reaction failing must not keep the others from running. How to fix: register the Flow metrics reaction that updates " +
                "the Team's metrics first among the Team refresh reactions, keep the others in their order, and let the dispatcher go on after a failure.");
        }

        private IReadOnlyList<string> ReactionFailuresOfThisStep()
            => [.. reactionFailures.Written(LogLevel.Error).Skip(failuresBeforeThisStep)];

        private void ThenNoReactionFailed()
        {
            var failures = ReactionFailuresOfThisStep();

            Assert.That(failures, Is.Empty,
                $"A reaction to the Team refresh failed: {Lines(failures)}. Why it matters: a failing reaction leaves its fact unrecorded. " +
                "How to fix: give the reaction what it needs, or let it return when the Team is gone.");
        }

        private void ThenOneReactionFailureWasLoggedNaming(string reaction)
        {
            var failures = ReactionFailuresOfThisStep();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failures, Has.Count.EqualTo(1),
                    $"The reaction failures logged were {Lines(failures)}, not exactly one for the failed metrics save. Why it matters: an operator " +
                    "learns from this line alone that the Team's metrics were not saved. How to fix: let the metrics update fail inside its own reaction.");
                Assert.That(failures, Has.Some.Contains(reaction),
                    $"No logged reaction failure names {reaction}: {Lines(failures)}. Why it matters: the operator must see which reaction failed. " +
                    $"How to fix: run the Team metrics update in the Flow metrics reaction {reaction}.");
            }
        }

        private void ThenTheTeamIsStoredWith(int teamId, string updatedAt, int featureWip)
        {
            var read = Read(dbContext => dbContext.Teams.Where(team => team.Id == teamId)
                .Select(team => new { team.UpdateTime, team.FeatureWIP }).Single());
            var stored = $"updated {read.UpdateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}, Feature WIP {read.FeatureWIP}";
            var expected = $"updated {updatedAt}, Feature WIP {featureWip}";

            Assert.That(stored, Is.EqualTo(expected),
                $"The Team is stored {stored}, not {expected}. Why it matters: the update time decides when the Team is refreshed next, and the " +
                "Feature WIP feeds its forecasts; a saved update shows the refresh time and the Features in progress now, a failed save keeps " +
                "the values of before so the next refresh retries. How to fix: update and save them in the Flow metrics reaction to the refresh.");
        }

        private void ThenTheMetricsAnswer(int teamId, int throughput, string[] featuresInProgress)
        {
            var (readThroughput, readFeatures) = TheMetricsAnswersOf(teamId);
            var read = $"throughput {readThroughput}, Features in progress {string.Join(", ", readFeatures)}";
            var expected = $"throughput {throughput}, Features in progress {string.Join(", ", featuresInProgress)}";

            Assert.That(read, Is.EqualTo(expected),
                $"The Team's metrics answer {read}, not {expected}. Why it matters: these answers come from the refreshed Work Items; the answers " +
                "of before the refresh mean the metrics cache was not dropped. How to fix: drop the Team's metrics cache in the metrics update, before it saves.");
        }

        private (int Throughput, IReadOnlyList<string> FeaturesInProgress) TheMetricsAnswersOf(int teamId)
        {
            using var scope = harness.NewScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            var metrics = scope.ServiceProvider.GetRequiredService<ITeamMetricsService>();

            return (
                metrics.GetThroughputForTeam(team, ThroughputFrom, ThroughputUntil).Total,
                [.. metrics.GetInProgressFeatureReferences(team, FeaturesInProgressOn).Select(reference => reference ?? "-").Order(StringComparer.Ordinal)]);
        }

        private T Read<T>(Func<LighthouseAppContext, T> query)
        {
            using var scope = harness.NewScope();
            return query(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>());
        }

        private static string Lines(IEnumerable<string> lines)
        {
            var all = lines.ToList();
            return all.Count == 0 ? "(nothing)" : string.Join($"{Environment.NewLine}  ", all);
        }

        // --- The Team updater as the product builds it, reading a CSV upload, with the start-up code's reactions ---

        private void RegisterTheRefreshAsTheProductBuildsIt(IServiceCollection services)
        {
            services.AddSingleton<ILighthouseClock>(clock);
            services.AddSingleton<TimeProvider>(timeProvider);
            services.AddSingleton<ILogger<DomainEventDispatcher>>(reactionFailures);
            services.ConfigureDbContext<LighthouseAppContext>(options => options.AddInterceptors(metricsSave));
            services.AddScoped(_ => PortDoubles.PremiumLicence());
            services.AddScoped(_ => SettingsAsSeeded());
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<UpdateCancellationContext>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
            services.AddScoped<IWorkItemBlockedTransitionRepository, WorkItemBlockedTransitionRepository>();
            services.AddScoped<IFeatureBlockedTransitionRepository, FeatureBlockedTransitionRepository>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IRepository<RefreshLog>, RefreshLogRepository>();

            services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            services.AddScoped<CsvWorkTrackingConnector>();

            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<IBlockedItemService, BlockedItemService>();
            services.AddScoped<IDependencyReconciler, DependencyReconciler>();
            services.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            services.AddScoped<IDependencyDecision, DependencyDecision>();
            services.AddScoped<IDependencyRefreshReporter, DependencyRefreshReporter>();

            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<ForecastFilterRuleService>();
            services.AddScoped<IThroughputFilter>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
            services.AddScoped<ForecastingAnswersToFlowMetrics>();
            services.AddScoped<IForecastDataSufficiency>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
            services.AddScoped<IWithinSleSnapshotRepository, WithinSleSnapshotRepository>();
            services.AddScoped<IWithinSleSnapshotWriter, WithinSleSnapshotWriter>();

            services.AddScoped<IWorkItemService, WorkItemService>();
            services.AddScoped<ITeamDataService, TeamDataService>();
            services.AddScoped<IRefreshLogService, RefreshLogService>();
            services.AddScoped(_ => NothingToWriteBack());
            services.AddScoped(_ => NothingStagedToWriteBack());
            services.AddScoped(_ => Mock.Of<IConnectionHealthService>());

            RegisterTheTeamRefreshReactionsInTheStartUpOrder(services);
        }

        private void RegisterTheTeamRefreshReactionsInTheStartUpOrder(IServiceCollection services)
        {
            foreach (var reaction in startUpTeamRefreshReactions)
            {
                if (TheOtherReactions.Contains(reaction.Name, StringComparer.Ordinal))
                {
                    services.AddScoped<IDomainEventHandler<TeamDataRefreshed>>(_ => new ReactionStandIn(reaction.Name, journal));
                }
                else
                {
                    services.AddScoped(typeof(IDomainEventHandler<TeamDataRefreshed>), reaction);
                }
            }
        }

        /// <summary>The settings a newly seeded database holds for what a Team refresh reads.</summary>
        private static IAppSettingService SettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
            settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 15 });
            settings.Setup(service => service.GetRefreshLogRetentionRuns()).Returns(10);
            return settings.Object;
        }

        private static IWriteBackTriggerService NothingToWriteBack()
        {
            var trigger = new Mock<IWriteBackTriggerService>();
            trigger.Setup(service => service.ResolveWriteBackForTeam(It.IsAny<Team>())).Returns([]);
            return trigger.Object;
        }

        private static IWriteBackCollector NothingStagedToWriteBack()
        {
            var collector = new Mock<IWriteBackCollector>();
            collector.Setup(service => service.FlushAsync()).ReturnsAsync([]);
            return collector.Object;
        }

        private sealed record RefreshRun(Exception? Failure);

        /// <summary>What ran after a refresh, in the order it ran: the reactions standing in by name, and each save of the Team's metrics.</summary>
        private sealed class RefreshJournal
        {
            private readonly List<string> entries = [];
            private readonly Lock gate = new();

            public IReadOnlyList<string> All
            {
                get
                {
                    lock (gate)
                    {
                        return [.. entries];
                    }
                }
            }

            public void StartAfresh()
            {
                lock (gate)
                {
                    entries.Clear();
                }
            }

            public void Add(string entry)
            {
                lock (gate)
                {
                    entries.Add(entry);
                }
            }
        }

        private sealed class ReactionStandIn(string name, RefreshJournal journal) : IDomainEventHandler<TeamDataRefreshed>
        {
            public Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
            {
                journal.Add(name);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Sees each save that writes a Team's update time, which only the metrics update writes, and can make the next
        /// one fail, as a database that refuses the write would.
        /// </summary>
        private sealed class MetricsSaveWatch(RefreshJournal journal) : SaveChangesInterceptor
        {
            private bool failTheNextSave;

            public void FailTheNextSave() => failTheNextSave = true;

            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                Watch(eventData.Context);
                return base.SavingChanges(eventData, result);
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                Watch(eventData.Context);
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            private void Watch(DbContext? context)
            {
                var writesATeamsUpdateTime = context is not null && context.ChangeTracker.Entries<Team>()
                    .Any(entry => entry.State == EntityState.Modified && entry.Property(team => team.UpdateTime).IsModified);

                if (!writesATeamsUpdateTime)
                {
                    return;
                }

                if (failTheNextSave)
                {
                    failTheNextSave = false;
                    journal.Add(MetricsSaveFailed);
                    throw new InvalidOperationException("The database refused to save the Team's metrics.");
                }

                journal.Add(MetricsSaved);
            }
        }
    }
}
