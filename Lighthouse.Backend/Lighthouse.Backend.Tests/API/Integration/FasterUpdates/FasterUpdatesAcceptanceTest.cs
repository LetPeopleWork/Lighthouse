using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System.Net.Http.Json;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance harness (Epic 5687 — Faster Updates, slice 01 / Story #5724). The driving port
    /// is the one the epic is observed through: the scheduled refresh, triggered on an updater and run by
    /// the production update queue in its own DI scope. The observable surface is the log stream — so the
    /// harness replaces the logger factory rather than the services that write to it.
    ///
    /// Real: EF over SQLite, the update queue, <c>TeamDataService</c>, <c>WorkItemService</c>, the
    /// metrics services, the domain-event dispatcher and <c>RefreshLogService</c>. Faked: the
    /// work-tracking connector and <see cref="IForecastService"/> — the two external / non-deterministic
    /// driven ports per docs/architecture/atdd-infrastructure-policy.md — and the licence service.
    ///
    /// Faking the data services (as the Quiet-write-back harness does) would make the noise assertions
    /// vacuous: <c>WorkItemService</c> and <c>TeamDataService</c> are the loudest voices on the update
    /// path and AC-1.7 is a promise about exactly them.
    /// </summary>
    public abstract class FasterUpdatesAcceptanceTest
    {
        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;

        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected Mock<IForecastService> ForecastServiceMock = null!;

        /// <summary>
        /// Every log line the refresh produced, with its level. The level is half the promise: a line an
        /// operator is meant to read has to arrive at Information, and a line that was demoted has to
        /// still exist at Debug.
        /// </summary>
        protected CapturedLogMessages CapturedLogs = null!;

        /// <summary>
        /// Every domain event the refresh raised. Staleness (AC-2.5) has no handler that persists
        /// anything, so the bus is the only place the promise is observable.
        /// </summary>
        protected CapturedDomainEvents CapturedEvents = null!;

        /// <summary>
        /// Which work items the refresh handed to storage to be written. An issue that did not move is
        /// written with exactly what it already said, so comparing its stored values cannot tell a write
        /// from no write at all (AC-2.4) - the write path itself is where the two differ.
        /// </summary>
        protected CapturedWorkItemWrites CapturedWorkItemWrites = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();
            CapturedLogs = new CapturedLogMessages();
            CapturedEvents = new CapturedDomainEvents();
            CapturedWorkItemWrites = new CapturedWorkItemWrites();
            ResetTrackerObservations();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock.Setup(c => c.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            ConnectorMock.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>())).Returns([]);
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync([]);

            // Epic #5687 slice 02: a connector that cannot scan is the default, so nothing in slice 01
            // changes shape. A scenario that wants the two-phase path says so explicitly.
            ConnectorMock.Setup(c => c.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            ConnectorMock.Setup(c => c.SweepWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);

            // Epic #5687 slice 03: the portfolio half of the same default. A portfolio whose scenario says
            // nothing about the two-phase path behaves exactly as it did in slices 01 and 02.
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.SweepFeaturesForPortfolio(It.IsAny<Portfolio>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
            ConnectorMock
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult());

            // Monte Carlo is non-deterministic and this slice asserts nothing about a forecast; faking it
            // keeps the portfolio refresh's duration and log shape reproducible.
            ForecastServiceMock = new Mock<IForecastService>();

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(ConnectorMock.Object);

            Factory = RootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => LicenseServiceMock.Object);

                    services.RemoveAll<IWorkTrackingConnectorFactory>();
                    services.AddScoped(_ => connectorFactoryMock.Object);

                    services.RemoveAll<IForecastService>();
                    services.AddScoped(_ => ForecastServiceMock.Object);

                    // Added alongside the production handlers, never in place of one.
                    services.AddScoped<IDomainEventHandler<WorkItemBecameStale>>(_ => new CapturingDomainEventHandler<WorkItemBecameStale>(CapturedEvents));
                    services.AddScoped<IDomainEventHandler<TeamDataRefreshed>>(_ => new CapturingDomainEventHandler<TeamDataRefreshed>(CapturedEvents));

                    // Epic #5687 slice 03. FeatureUnblocked is the one that matters: the departed-spell
                    // sweep closes a spell for every Feature missing from the refreshed list, so a delta
                    // cycle that hands it only the Features that moved closes the rest silently.
                    services.AddScoped<IDomainEventHandler<FeatureUnblocked>>(_ => new CapturingDomainEventHandler<FeatureUnblocked>(CapturedEvents));
                    services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>>(_ => new CapturingDomainEventHandler<PortfolioFeaturesRefreshed>(CapturedEvents));
                    services.AddScoped<IDomainEventHandler<PortfolioForecastsUpdated>>(_ => new CapturingDomainEventHandler<PortfolioForecastsUpdated>(CapturedEvents));

                    // Wraps the real repository rather than replacing it: the adapter under test is still
                    // EF over SQLite, and only the calls are recorded on the way through.
                    services.RemoveAll<IWorkItemRepository>();
                    services.AddScoped<WorkItemRepository>();
                    services.AddScoped<IWorkItemRepository>(sp =>
                        new WriteRecordingWorkItemRepository(sp.GetRequiredService<WorkItemRepository>(), CapturedWorkItemWrites));

                    // ADR-137 D72: Serilog is the pipeline, so an ILoggerProvider added here would be
                    // inert. Replacing the factory is what makes the refresh's own log readable.
                    services.RemoveAll<ILoggerFactory>();
                    // The framework overrides mirror appsettings.json: "operator-visible" has to mean the
                    // stream the operator actually reads, and EF's SQL never reaches it.
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                            .WriteTo.Sink(CapturedLogs)
                            .CreateLogger(),
                        dispose: true));
                });
            });

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only — never the expected output) ---

        protected int SeedConnection(WorkTrackingSystems system = WorkTrackingSystems.Jira)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = system };
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return connection.Id;
        }

        protected int SeedPortfolio(int connectionId, string name)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                UpdateTime = DateTime.UtcNow,
            };

            var repository = sp.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        protected int SeedTeam(int connectionId, string name, int stalenessThresholdDays = 0)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                StalenessThresholdDays = stalenessThresholdDays,
                UpdateTime = DateTime.UtcNow,
            };

            var repository = sp.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        // --- The tracker ---

        protected void TheTrackerReturnsWorkItems(int count)
        {
            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>()))
                .ReturnsAsync((Team team) => [.. Enumerable.Range(1, count).Select(index => new WorkItem(new WorkItemBase
                {
                    ReferenceId = $"ITEM-{index}",
                    Name = $"Work Item {index}",
                    Type = "Story",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = index.ToString(),
                    ParentReferenceId = string.Empty,
                    StartedDate = DateTime.UtcNow.AddDays(-index),
                }, team))]);
        }

        protected void TheTrackerReturnsFeatures(int count)
        {
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync([.. Enumerable.Range(1, count).Select(index => new Feature(new WorkItemBase
                {
                    ReferenceId = $"FEAT-{index}",
                    Name = $"Feature {index}",
                    Type = "Epic",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = index.ToString(),
                    ParentReferenceId = string.Empty,
                }))]);
        }

        protected void TheTrackerIsUnreachable(Exception failure)
        {
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>())).ThrowsAsync(failure);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>())).ThrowsAsync(failure);
        }

        // --- Driving port: the scheduled refresh ---

        protected Task TheTeamRefreshRuns(int teamId)
            => RunUpdate(sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamId));

        protected Task ThePortfolioRefreshRuns(int portfolioId)
            => RunUpdate(sp => sp.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolioId));

        /// <summary>
        /// Triggers one update and waits for the queue to go idle. Admission is synchronous inside
        /// <c>EnqueueUpdate</c>, so the key is already active when this starts polling.
        /// </summary>
        private async Task RunUpdate(Action<IServiceProvider> trigger)
        {
            var statusStore = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            // Host startup and fixture seeding log through the same sink; the line budget is a promise
            // about one update, so counting starts here. The same applies to what the tracker was asked
            // for and to the signals raised (Epic #5687 slice 02, where scenarios chain two refreshes).
            CapturedLogs.Clear();
            CapturedEvents.Clear();
            CapturedWorkItemWrites.Clear();
            ForgetWhatTheTrackerWasAsked();

            trigger(Factory.Services);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue did not go idle within 30s — the refresh never completed.");
                }

                await Task.Delay(20);
            }
        }

        // --- Observation ---

        protected RefreshLog? TheRefreshLogFor(RefreshType type, int entityId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .SingleOrDefault(log => log.Type == type && log.EntityId == entityId);
        }

        /// <summary>
        /// What the MOST RECENT refresh of this entity recorded. A chained scenario runs more than one
        /// cycle, and each cycle writes its own row.
        /// </summary>
        protected RefreshLog? TheLastRefreshLogFor(RefreshType type, int entityId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .Where(log => log.Type == type && log.EntityId == entityId)
                .OrderByDescending(log => log.Id)
                .FirstOrDefault();
        }

        protected IReadOnlyList<string> TheOperatorVisibleLines => CapturedLogs.AtOrAbove(LogEventLevel.Information);

        /// <summary>
        /// The reference ids this refresh handed to storage to be written, in order. Seeding writes
        /// through the same port, so the recording is reset at the start of every cycle.
        /// </summary>
        protected List<string> TheIssuesWrittenToStorage => CapturedWorkItemWrites.ReferenceIds;

        // --- The tracker as the two-phase fetch sees it (Epic #5687 slice 02) ---

        /// <summary>
        /// One record as the tracker holds it right now. <see cref="ChangedAt"/> is what the identity
        /// sweep reports; <see cref="StoredStamp"/> and <see cref="StateEnteredAt"/> only apply when the
        /// record is seeded straight into storage to stand for an instance that upgraded into this
        /// release.
        /// </summary>
        protected sealed record RemoteRecord(string ReferenceId, DateTime ChangedAt)
        {
            public string Name { get; init; } = string.Empty;

            public string State { get; init; } = "In Progress";

            public StateCategories StateCategory { get; init; } = StateCategories.Doing;

            public string ParentReferenceId { get; init; } = string.Empty;

            public DateTime? StartedDate { get; init; }

            public DateTime? StateEnteredAt { get; init; }

            public DateTime? StoredStamp { get; init; }
        }

        private readonly List<RemoteRecord> remoteRecords = [];

        /// <summary>How many identity sweeps the refresh issued.</summary>
        protected int ScansIssued { get; private set; }

        /// <summary>How many whole-query payload downloads the refresh issued.</summary>
        protected int FullDownloadsIssued { get; private set; }

        /// <summary>The reference ids of each by-reference-id payload download, in order.</summary>
        protected List<List<string>> PayloadDownloads { get; } = [];

        /// <summary>How many identity sweeps over the portfolio's Feature query the refresh issued.</summary>
        protected int FeatureScansIssued { get; private set; }

        /// <summary>How many whole-query Feature payload downloads the refresh issued.</summary>
        protected int FullFeatureDownloadsIssued { get; private set; }

        /// <summary>The reference ids of each by-reference-id Feature download, in order.</summary>
        protected List<List<string>> FeaturePayloadDownloads { get; } = [];

        /// <summary>The keys of each parent-Feature identity sweep, in order.</summary>
        protected List<List<string>> ParentFeatureScans { get; } = [];

        /// <summary>The keys of each parent-Feature payload download, in order.</summary>
        protected List<List<string>> ParentFeatureDownloads { get; } = [];

        private void ResetTrackerObservations()
        {
            ForgetWhatTheTrackerWasAsked();
            remoteRecords.Clear();
            remoteFeatures.Clear();
            remoteParentFeatures.Clear();
        }

        /// <summary>
        /// Forgets the calls, not the records. Every assertion in this epic is about what ONE refresh
        /// asked for, and a chained scenario runs more than one.
        /// </summary>
        private void ForgetWhatTheTrackerWasAsked()
        {
            ScansIssued = 0;
            FullDownloadsIssued = 0;
            PayloadDownloads.Clear();

            FeatureScansIssued = 0;
            FullFeatureDownloadsIssued = 0;
            FeaturePayloadDownloads.Clear();
            ParentFeatureScans.Clear();
            ParentFeatureDownloads.Clear();
        }

        /// <summary>When the tracker says the named record last changed.</summary>
        protected DateTime TheTrackersChangeStampFor(string referenceId)
        {
            var record = remoteRecords.Find(candidate => candidate.ReferenceId == referenceId);
            Assert.That(record, Is.Not.Null, $"The tracker does not hold '{referenceId}'.");

            return record!.ChangedAt;
        }

        /// <summary>
        /// Programs the connector from one coherent picture of the tracker: the whole-query download, the
        /// identity sweep and the by-reference-id download all read the same records. Every setup reads
        /// the list lazily, so a scenario can move an issue between two refreshes.
        /// </summary>
        protected void TheTrackerHolds(params RemoteRecord[] records)
        {
            remoteRecords.Clear();
            remoteRecords.AddRange(records);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>()))
                .ReturnsAsync((Team team) =>
                {
                    FullDownloadsIssued++;
                    return AsWorkItems(remoteRecords, team);
                });

            ConnectorMock
                .Setup(c => c.SweepWorkItemsForTeam(It.IsAny<Team>()))
                .ReturnsAsync(() =>
                {
                    ScansIssued++;
                    return remoteRecords.ConvertAll(record => new RemoteRecordStamp(record.ReferenceId, record.ChangedAt));
                });

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync((Team team, IReadOnlyCollection<string> referenceIds) =>
                {
                    PayloadDownloads.Add([.. referenceIds]);
                    return AsWorkItems(remoteRecords.Where(record => referenceIds.Contains(record.ReferenceId)), team);
                });
        }

        protected void TheTrackerCanBeScanned()
            => ConnectorMock.Setup(c => c.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(true);

        protected void TheScanFails(Exception failure)
            => ConnectorMock.Setup(c => c.SweepWorkItemsForTeam(It.IsAny<Team>())).ThrowsAsync(failure);

        protected void OnTheTrackerTheIssueChanges(string referenceId, DateTime changedAt, string? state = null)
        {
            var index = remoteRecords.FindIndex(record => record.ReferenceId == referenceId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"The tracker does not hold '{referenceId}'.");

            var record = remoteRecords[index];
            remoteRecords[index] = record with { ChangedAt = changedAt, State = state ?? record.State };
        }

        protected void OnTheTrackerTheIssueIsGone(string referenceId)
            => remoteRecords.RemoveAll(record => record.ReferenceId == referenceId);

        // --- The tracker's Features, as the two-phase portfolio fetch sees them (Epic #5687 slice 03) ---

        private readonly List<RemoteRecord> remoteFeatures = [];

        private readonly List<RemoteRecord> remoteParentFeatures = [];

        /// <summary>When the tracker says the named Feature last changed.</summary>
        protected DateTime TheTrackersChangeStampForFeature(string referenceId)
        {
            var record = remoteFeatures.Find(candidate => candidate.ReferenceId == referenceId)
                ?? remoteParentFeatures.Find(candidate => candidate.ReferenceId == referenceId);
            Assert.That(record, Is.Not.Null, $"The tracker does not hold Feature '{referenceId}'.");

            return record!.ChangedAt;
        }

        /// <summary>
        /// Programs the portfolio's Feature query from one coherent picture of the tracker: the whole-query
        /// download, the identity sweep and the by-reference-id download all read the same records. Every
        /// setup reads the list lazily, so a scenario can move a Feature between two refreshes.
        /// </summary>
        protected void TheTrackerHoldsFeatures(params RemoteRecord[] records)
        {
            remoteFeatures.Clear();
            remoteFeatures.AddRange(records);

            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() =>
                {
                    FullFeatureDownloadsIssued++;
                    return AsFeatures(remoteFeatures);
                });

            ConnectorMock
                .Setup(c => c.SweepFeaturesForPortfolio(It.IsAny<Portfolio>()))
                .ReturnsAsync(() =>
                {
                    FeatureScansIssued++;
                    return remoteFeatures.ConvertAll(record => new RemoteRecordStamp(record.ReferenceId, record.ChangedAt));
                });

            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync((Portfolio _, IReadOnlyCollection<string> referenceIds) =>
                {
                    FeaturePayloadDownloads.Add([.. referenceIds]);
                    return AsFeatures(remoteFeatures.Where(record => referenceIds.Contains(record.ReferenceId)));
                });
        }

        /// <summary>
        /// The parent-Feature half. Both phases are keyed queries, so both record which keys they were
        /// asked for - which is the observation that catches a parent key list derived from what this
        /// cycle fetched rather than from what is stored.
        /// </summary>
        protected void TheTrackerHoldsParentFeatures(params RemoteRecord[] records)
        {
            remoteParentFeatures.Clear();
            remoteParentFeatures.AddRange(records);

            ConnectorMock
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync((Portfolio _, IEnumerable<string> parentFeatureIds) =>
                {
                    var requested = parentFeatureIds.ToList();
                    ParentFeatureDownloads.Add(requested);
                    return AsFeatures(remoteParentFeatures.Where(record => requested.Contains(record.ReferenceId)));
                });

            ConnectorMock
                .Setup(c => c.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync((Portfolio _, IEnumerable<string> parentFeatureIds) =>
                {
                    var requested = parentFeatureIds.ToList();
                    ParentFeatureScans.Add(requested);
                    return remoteParentFeatures
                        .FindAll(record => requested.Contains(record.ReferenceId))
                        .ConvertAll(record => new RemoteRecordStamp(record.ReferenceId, record.ChangedAt));
                });
        }

        protected void TheFeatureScanFails(Exception failure)
            => ConnectorMock.Setup(c => c.SweepFeaturesForPortfolio(It.IsAny<Portfolio>())).ThrowsAsync(failure);

        protected void OnTheTrackerTheFeatureChanges(string referenceId, DateTime changedAt, string? state = null)
        {
            var index = remoteFeatures.FindIndex(record => record.ReferenceId == referenceId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"The tracker does not hold Feature '{referenceId}'.");

            var record = remoteFeatures[index];
            remoteFeatures[index] = record with { ChangedAt = changedAt, State = state ?? record.State };
        }

        protected void OnTheTrackerTheFeatureIsGone(string referenceId)
            => remoteFeatures.RemoveAll(record => record.ReferenceId == referenceId);

        protected void TheParentFeatureScanFails(Exception failure)
            => ConnectorMock
                .Setup(c => c.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ThrowsAsync(failure);

        /// <summary>
        /// A parent Feature's own record moves while its children stay put - a rename, a state change, an
        /// owner swapped. The parent half has to notice on its own, because nothing about the children
        /// says so.
        /// </summary>
        protected void OnTheTrackerTheParentFeatureChanges(string referenceId, DateTime changedAt, string? name = null)
        {
            var index = remoteParentFeatures.FindIndex(record => record.ReferenceId == referenceId);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"The tracker does not hold Parent Feature '{referenceId}'.");

            var record = remoteParentFeatures[index];
            remoteParentFeatures[index] = record with { ChangedAt = changedAt, Name = name ?? record.Name };
        }

        /// <summary>
        /// The keyed parent query stops answering for a parent the portfolio's children still name -
        /// deleted, moved out of scope, or hidden by a permission change. Both phases go quiet about it.
        /// </summary>
        protected void OnTheTrackerTheParentFeatureIsGone(string referenceId)
            => remoteParentFeatures.RemoveAll(record => record.ReferenceId == referenceId);

        /// <summary>
        /// The connector hands back a Feature that already carries its remote change stamp - the same
        /// port contract the work-item side holds, and set after construction for the same reason: the
        /// copy path is what the stamp-survives specification measures, so a double that inherits its
        /// defect could not measure it.
        /// </summary>
        private static List<Feature> AsFeatures(IEnumerable<RemoteRecord> records)
            => [.. records.Select(record => new Feature(new WorkItemBase
            {
                ReferenceId = record.ReferenceId,
                Name = string.IsNullOrEmpty(record.Name) ? record.ReferenceId : record.Name,
                Type = "Epic",
                State = record.State,
                StateCategory = record.StateCategory,
                Order = "1",
                ParentReferenceId = record.ParentReferenceId,
                StartedDate = record.StartedDate,
            })
            {
                LastChangedRemote = record.ChangedAt,
            })];

        /// <summary>
        /// The connector hands back an item that already carries its remote change stamp - that is the
        /// port's contract, and mapping it out of a Jira payload is the connector's own business. Setting
        /// it after construction on purpose: the copy constructor is exactly what AC-2.7 is about, and a
        /// double that inherits the defect under test cannot measure it.
        /// </summary>
        private static List<WorkItem> AsWorkItems(IEnumerable<RemoteRecord> records, Team team)
            => [.. records.Select(record => new WorkItem(new WorkItemBase
            {
                ReferenceId = record.ReferenceId,
                Name = string.IsNullOrEmpty(record.Name) ? record.ReferenceId : record.Name,
                Type = "Story",
                State = record.State,
                StateCategory = record.StateCategory,
                Order = "1",
                ParentReferenceId = record.ParentReferenceId,
                StartedDate = record.StartedDate,
            }, team)
            {
                LastChangedRemote = record.ChangedAt,
            })];

        // --- Storage as it was before this refresh ---

        /// <summary>
        /// Puts work items straight into storage. Stands for an instance that upgraded into this release:
        /// its items exist, and none of them carries a remote change stamp yet (D8).
        /// </summary>
        protected void SeedStoredWorkItems(int teamId, params RemoteRecord[] records)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            var repository = sp.GetRequiredService<IWorkItemRepository>();

            foreach (var record in records)
            {
                repository.Add(new WorkItem(new WorkItemBase
                {
                    ReferenceId = record.ReferenceId,
                    Name = string.IsNullOrEmpty(record.Name) ? record.ReferenceId : record.Name,
                    Type = "Story",
                    State = record.State,
                    StateCategory = record.StateCategory,
                    Order = "1",
                    ParentReferenceId = record.ParentReferenceId,
                    StartedDate = record.StartedDate,
                }, team)
                {
                    CurrentStateEnteredAt = record.StateEnteredAt,
                    LastChangedRemote = record.StoredStamp,
                });
            }

            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// A feature a team is already delivering. <paramref name="teamId"/> and
        /// <paramref name="workAlreadyCounted"/> stand for what the previous cycle rolled up - and they
        /// are also what makes the team belong to the portfolio at all, because <c>Portfolio.Teams</c> is
        /// derived from feature work rather than stored.
        /// </summary>
        protected int SeedFeature(int portfolioId, string referenceId, string name, int? teamId = null, int workAlreadyCounted = 0)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;
            var feature = new Feature(new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = name,
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
                ParentReferenceId = string.Empty,
            });

            portfolio.Features.Add(feature);
            feature.Portfolios.Add(portfolio);

            if (teamId.HasValue)
            {
                var team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId.Value)!;
                feature.AddOrUpdateWorkForTeam(team, workAlreadyCounted, workAlreadyCounted);
            }

            var repository = sp.GetRequiredService<IRepository<Feature>>();
            repository.Add(feature);
            repository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        // --- Storage as it is now (always through a fresh context) ---

        protected List<WorkItem> TheStoredWorkItemsFor(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IWorkItemRepository>()
                .GetAllByPredicate(workItem => workItem.TeamId == teamId)
                .OrderBy(workItem => workItem.ReferenceId)];
        }

        protected List<WorkItemStateTransition> TheStoredTransitionsFor(int workItemId)
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IWorkItemStateTransitionRepository>()
                .GetAllByPredicate(transition => transition.WorkItemId == workItemId)
                .OrderBy(transition => transition.TransitionedAt)
                .ThenBy(transition => transition.ToState)];
        }

        protected Feature? TheStoredFeature(string referenceId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<Feature>>()
                .GetByPredicate(feature => feature.ReferenceId == referenceId);
        }

        // --- The portfolio as storage holds it (Epic #5687 slice 03) ---

        /// <summary>
        /// Puts Features straight into storage and claims them for the portfolio. Stands for an instance
        /// that upgraded into this release: its Features exist, and unless the record says otherwise none
        /// of them carries a remote change stamp yet (D8).
        /// </summary>
        protected void SeedStoredFeatures(int portfolioId, params RemoteRecord[] records)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;
            var repository = sp.GetRequiredService<IRepository<Feature>>();

            foreach (var record in records)
            {
                var feature = new Feature(new WorkItemBase
                {
                    ReferenceId = record.ReferenceId,
                    Name = string.IsNullOrEmpty(record.Name) ? record.ReferenceId : record.Name,
                    Type = "Epic",
                    State = record.State,
                    StateCategory = record.StateCategory,
                    Order = "1",
                    ParentReferenceId = record.ParentReferenceId,
                    StartedDate = record.StartedDate,
                })
                {
                    CurrentStateEnteredAt = record.StateEnteredAt,
                    LastChangedRemote = record.StoredStamp,
                };

                portfolio.Features.Add(feature);
                feature.Portfolios.Add(portfolio);
                repository.Add(feature);
            }

            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Which Features the portfolio still claims. This is the observable the portfolio half of the
        /// delta contract turns on: a Feature that drops out of this collection loses its last portfolio
        /// claim and is then deleted outright by the orphaned-Feature cleanup the updater runs.
        /// </summary>
        protected List<Feature> TheFeaturesInThePortfolio(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId);

            return portfolio == null ? [] : [.. portfolio.Features.OrderBy(feature => feature.ReferenceId, StringComparer.Ordinal)];
        }

        protected List<FeatureStateTransition> TheStoredTransitionsForFeature(int featureId)
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IFeatureStateTransitionRepository>()
                .GetAllByPredicate(transition => transition.FeatureId == featureId)
                .OrderBy(transition => transition.TransitionedAt)
                .ThenBy(transition => transition.ToState)];
        }

        /// <summary>
        /// Opens a blocked spell for a Feature in a portfolio, the way the capture handler does. A spell
        /// is what the departed-spell sweep closes, so a Feature with one open is the only way to observe
        /// a cycle closing spells for Features it simply did not refetch.
        /// </summary>
        protected void AFeatureIsAlreadyBlockedInThePortfolio(int portfolioId, int featureId, DateTime since)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFeatureBlockedTransitionRepository>();

            repository.Add(new FeatureBlockedTransition
            {
                FeatureId = featureId,
                PortfolioId = portfolioId,
                EnteredAt = since,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        protected Dictionary<int, FeatureBlockedTransition> TheOpenBlockedSpellsInThePortfolio(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IFeatureBlockedTransitionRepository>()
                .GetOpenSpellsForPortfolio(portfolioId);
        }

        // --- The opt-in gate (Epic #5687 A1) ---

        protected OptionalFeature? TheCheaperRefreshOption()
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>()
                .GetByPredicate(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey);
        }

        /// <summary>
        /// Turns the option on the way the Settings screen does - through the repository, against a
        /// running host. Nothing is restarted, which is the whole point of AC-2.11.
        /// </summary>
        protected void TheOperatorAsksForTheCheaperRefresh()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var option = repository.GetByPredicate(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey);

            if (option == null)
            {
                repository.Add(new OptionalFeature
                {
                    Id = 0,
                    Key = OptionalFeatureKeys.DeltaSyncKey,
                    Name = "Faster Updates",
                    Description = "Download only the records that changed.",
                    Enabled = true,
                    IsPreview = false,
                });
            }
            else
            {
                option.Enabled = true;
                repository.Update(option);
            }

            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Turns the option off the way the Settings screen does. The seeded default is on, so a scenario
        /// about the whole query still being fetched has to say so out loud rather than lean on the default.
        /// </summary>
        protected void TheOperatorTurnsOffTheCheaperRefresh()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var option = repository.GetByPredicate(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey);

            Assert.That(option, Is.Not.Null,
                "The cheaper refresh is not offered at all, so it cannot be switched off - it is an absent option.");

            option!.Enabled = false;
            repository.Update(option);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>Re-runs the seeders, the way starting a newer build against an existing database does.</summary>
        protected void TheInstanceIsUpgradedAgain()
        {
            using var scope = Factory.Services.CreateScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        // --- The settings screen (Epic #5687 slice 05) ---

        /// <summary>
        /// The admin's browser, against the running host. Settings saves go through the controller because
        /// half of what slice 05 decides - whether the edit costs a purge - is decided there, in
        /// <c>WorkItemRelatedSettingsChanged</c>. An edit applied straight through the repository would
        /// bypass that decision entirely and leave the scenario measuring the other half only.
        /// </summary>
        private HttpClient TheSettingsScreen() => Factory.CreateClient().AsSystemAdmin();

        protected TeamSettingDto TheTeamsCurrentSettings(int teamId)
        {
            using var client = TheSettingsScreen();
            var settings = client.GetFromJsonAsync<TeamSettingDto>($"/api/latest/teams/{teamId}/settings").GetAwaiter().GetResult();

            Assert.That(settings, Is.Not.Null, $"Team {teamId} has no settings to read.");
            return settings!;
        }

        protected PortfolioSettingDto ThePortfoliosCurrentSettings(int portfolioId)
        {
            using var client = TheSettingsScreen();
            var settings = client.GetFromJsonAsync<PortfolioSettingDto>($"/api/latest/portfolios/{portfolioId}/settings").GetAwaiter().GetResult();

            Assert.That(settings, Is.Not.Null, $"Portfolio {portfolioId} has no settings to read.");
            return settings!;
        }

        /// <summary>
        /// Saves the team's settings the way the Settings screen does. Round-tripping the DTO the GET
        /// returned rather than hand-building one is deliberate: a hand-built payload that misses a
        /// [JsonRequired] field is a deterministic 400 that reads like a scenario failure
        /// (docs/ci-learnings.md 2026-07-07).
        /// </summary>
        protected void TheOperatorSavesTheTeamsSettings(int teamId, TeamSettingDto settings)
        {
            using var client = TheSettingsScreen();
            var response = client.PutAsJsonAsync($"/api/latest/teams/{teamId}", settings).GetAwaiter().GetResult();

            Assert.That(response.IsSuccessStatusCode, Is.True,
                $"The settings save was refused with {(int)response.StatusCode}: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
        }

        protected void TheOperatorSavesThePortfoliosSettings(int portfolioId, PortfolioSettingDto settings)
        {
            using var client = TheSettingsScreen();
            var response = client.PutAsJsonAsync($"/api/latest/portfolios/{portfolioId}", settings).GetAwaiter().GetResult();

            Assert.That(response.IsSuccessStatusCode, Is.True,
                $"The settings save was refused with {(int)response.StatusCode}: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
        }

        // --- What the last cycle asked for, as storage holds it (Epic #5687 slice 05) ---

        protected string? TheStoredFetchFingerprintForTeam(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)?.FetchFingerprint;
        }

        protected string? TheStoredFetchFingerprintForPortfolio(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)?.FetchFingerprint;
        }

        // --- The connection, which the query owner only points at (Epic #5687 slice 05) ---

        /// <summary>
        /// Adds a field definition to the CONNECTION, not to the team. Everything the connector reads out
        /// of a payload is defined here, so this is an edit that changes what is stored without touching a
        /// single property of the team - and it is invisible to any save-time comparison against a team
        /// settings DTO, because the DTO carries no field definitions.
        /// </summary>
        protected int SeedAdditionalFieldDefinition(int connectionId, string reference)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = repository.GetById(connectionId)!;
            var definition = new AdditionalFieldDefinition { DisplayName = reference, Reference = reference };
            connection.AdditionalFieldDefinitions.Add(definition);

            repository.Update(connection);
            repository.Save().GetAwaiter().GetResult();

            return definition.Id;
        }

        // --- History as it was before this refresh (Epic #5687 slice 05) ---

        /// <summary>
        /// One recorded state change for a stored issue. Transition rows are the part of a purge nobody
        /// gets back: <c>RemoveWorkItemsForTeam</c> deletes the work item, and the cascade on
        /// <c>WorkItemStateTransition.WorkItemId</c> takes its whole history with it.
        /// </summary>
        protected void SeedStoredTransition(int workItemId, string fromState, string toState, DateTime transitionedAt)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkItemStateTransitionRepository>();

            repository.Add(new WorkItemStateTransition
            {
                WorkItemId = workItemId,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = transitionedAt,
            });

            repository.Save().GetAwaiter().GetResult();
        }
    }
}
