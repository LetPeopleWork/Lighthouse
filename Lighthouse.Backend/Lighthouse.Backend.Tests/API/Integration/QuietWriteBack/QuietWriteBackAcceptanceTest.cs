using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.QuietWriteBack
{
    /// <summary>
    /// DISTILL acceptance harness (Epic 5500 - Quiet write-back, slice 01 / Story #5502). Single source
    /// of truth for HOW slice-01 scenarios reach the system: through the real driving port these
    /// scenarios are about - the scheduled refresh - by triggering an updater and letting the production
    /// <see cref="UpdateQueueService"/> run it in its own DI scope. That scope is the collector's
    /// lifetime (ADR-144), so a scenario that drove the updaters' <c>Update</c> method directly would
    /// assert nothing about the seam it is testing.
    ///
    /// Real: EF over SQLite, the update queue, <see cref="IWriteBackTriggerService"/>,
    /// <see cref="IWriteBackService"/> and the collector. Faked: the work-tracking connector (the one
    /// external/non-deterministic driven port, per docs/architecture/atdd-infrastructure-policy.md),
    /// the license service, and the three data-refresh services whose job is to talk to the tracker -
    /// so every recorded connector call is a write-back and nothing else.
    /// </summary>
    public abstract class QuietWriteBackAcceptanceTest
    {
        protected const string FieldReference = "customfield_10042";
        protected const string ForecastFieldReference = "customfield_10099";

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;

        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected Mock<IWorkItemService> WorkItemServiceMock = null!;
        protected Mock<IForecastService> ForecastServiceMock = null!;
        protected Mock<ITeamDataService> TeamDataServiceMock = null!;

        /// <summary>
        /// Every <see cref="IWorkTrackingConnector.WriteFieldsToWorkItems"/> call the refresh made, in
        /// order, each captured as an immutable snapshot. The count IS the promise slice 01 makes.
        /// </summary>
        protected List<ConnectorWrite> ConnectorWrites = null!;

        private Func<WriteBackFieldUpdate, (bool Success, string? Error)> writeOutcome = null!;
        private Exception? writeFailure;

        protected sealed record ConnectorWrite(int ConnectionId, IReadOnlyList<WriteBackFieldUpdate> Updates);

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();

            ConnectorWrites = [];
            writeOutcome = _ => (true, null);
            writeFailure = null;

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .Returns((WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates) =>
                {
                    ConnectorWrites.Add(new ConnectorWrite(connection.Id, [.. updates]));

                    if (writeFailure != null)
                    {
                        throw writeFailure;
                    }

                    return Task.FromResult(new WriteBackResult
                    {
                        ItemResults = [.. updates.Select(update =>
                        {
                            var (success, error) = writeOutcome(update);
                            return new WriteBackItemResult
                            {
                                WorkItemId = update.WorkItemId,
                                TargetFieldReference = update.TargetFieldReference,
                                Success = success,
                                ErrorMessage = error,
                            };
                        })],
                    });
                });

            // The three services whose whole job is to fetch from the tracker. Faking them keeps the
            // recorded connector calls unambiguous - and keeps the refresh deterministic, which a real
            // Monte Carlo run would not be.
            WorkItemServiceMock = new Mock<IWorkItemService>();
            ForecastServiceMock = new Mock<IForecastService>();
            TeamDataServiceMock = new Mock<ITeamDataService>();

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

                    services.RemoveAll<IWorkItemService>();
                    services.AddScoped(_ => WorkItemServiceMock.Object);

                    services.RemoveAll<IForecastService>();
                    services.AddScoped(_ => ForecastServiceMock.Object);

                    services.RemoveAll<ITeamDataService>();
                    services.AddScoped(_ => TeamDataServiceMock.Object);
                });
            });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // The refresh reads app settings on its way to write-back, and an unseeded settings table
            // makes it throw before it ever gets there - which looks exactly like "write-back did not
            // fire" and would have made half of these scenarios pass for the wrong reason.
            foreach (var seeder in setupScope.ServiceProvider.GetServices<Lighthouse.Backend.Services.Interfaces.Seeding.ISeeder>())
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

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only - never the expected output) ---

        /// <summary>
        /// A connection carrying the two mappings a Portfolio refresh resolves in its two separate
        /// passes: one non-forecast (the Features pass) and one forecast (the forecast pass). They target
        /// different fields because <see cref="Lighthouse.Backend.Services.Implementation.WriteBackTriggerService"/>
        /// filters the two passes on disjoint value sources - one mapping cannot be resolved by both.
        /// </summary>
        protected (int ConnectionId, int SizeFieldId, int ForecastFieldId) SeedPortfolioConnection(WorkTrackingSystems system = WorkTrackingSystems.Jira)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = system };

            var sizeField = new AdditionalFieldDefinition { Reference = FieldReference, DisplayName = "Size" };
            var forecastField = new AdditionalFieldDefinition { Reference = ForecastFieldReference, DisplayName = "Forecast" };
            connection.AdditionalFieldDefinitions.Add(sizeField);
            connection.AdditionalFieldDefinitions.Add(forecastField);

            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinition = sizeField,
                TargetValueType = WriteBackTargetValueType.FormattedText,
            });
            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinition = forecastField,
                TargetValueType = WriteBackTargetValueType.Date,
            });

            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return (connection.Id, sizeField.Id, forecastField.Id);
        }

        protected (int ConnectionId, int FieldId) SeedTeamConnection(WorkTrackingSystems system = WorkTrackingSystems.Jira)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = system };
            var field = new AdditionalFieldDefinition { Reference = FieldReference, DisplayName = "Age" };
            connection.AdditionalFieldDefinitions.Add(field);
            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinition = field,
                TargetValueType = WriteBackTargetValueType.FormattedText,
            });

            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return (connection.Id, field.Id);
        }

        protected int SeedPortfolio(int connectionId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                // The refresh is driven explicitly by every scenario; a stale update time would only
                // matter to the background loop, which the test host does not run.
                UpdateTime = DateTime.UtcNow,
            };

            var repository = sp.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        protected int SeedTeam(int connectionId, int? portfolioId = null)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                UpdateTime = DateTime.UtcNow,
            };

            if (portfolioId.HasValue)
            {
                team.Portfolios.Add(sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId.Value)!);
            }

            var repository = sp.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        /// <summary>
        /// A Feature whose Size is <paramref name="size"/> (Size is the sum of its work, so the Team edge
        /// is what makes it non-zero), carrying whatever the last inbound sync stored for the mapped
        /// fields. <c>WriteBackService.GetChangedFields</c> skips a field the item has no stored value
        /// for at all, so a scenario that wants a write must seed a stale value, not an absent one.
        /// </summary>
        protected int SeedFeature(int portfolioId, int teamId, string referenceId, int size, params (int FieldId, string? StoredValue)[] storedFields)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!;

            var feature = new Feature(team, size)
            {
                Name = $"Feature {referenceId}",
                ReferenceId = referenceId,
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
            };
            feature.Portfolios.Add(sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!);

            foreach (var (fieldId, storedValue) in storedFields)
            {
                feature.AdditionalFieldValues[fieldId] = storedValue;
            }

            var repository = sp.GetRequiredService<IRepository<Feature>>();
            repository.Add(feature);
            repository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        protected void SeedWorkItem(int teamId, string referenceId, int ageInDays, params (int FieldId, string? StoredValue)[] storedFields)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var today = sp.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;

            var workItem = new WorkItem
            {
                Name = $"Work item {referenceId}",
                ReferenceId = referenceId,
                Type = "Story",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                TeamId = teamId,
                ParentReferenceId = string.Empty,
                Order = string.Empty,
                StartedDate = today.AddDays(-ageInDays),
            };

            foreach (var (fieldId, storedValue) in storedFields)
            {
                workItem.AdditionalFieldValues[fieldId] = storedValue;
            }

            var repository = sp.GetRequiredService<IWorkItemRepository>();
            repository.Add(workItem);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Makes the forecast pass resolvable. Production populates a Feature's forecasts inside
        /// <see cref="IForecastService.UpdateForecastsForPortfolio"/>; the fake does the same thing with
        /// a fixed number of working days, so the resolved date is deterministic without pinning a
        /// Monte Carlo run.
        /// </summary>
        protected void TheForecastRunProduces(int workingDaysToCompletion)
        {
            ForecastServiceMock
                .Setup(s => s.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()))
                .Callback((Portfolio portfolio) =>
                {
                    foreach (var feature in portfolio.Features)
                    {
                        var simulation = new SimulationResult();
                        simulation.SimulationResults[workingDaysToCompletion] = 100;
                        feature.SetFeatureForecasts([new WhenForecast(simulation) { HasSufficientData = true }]);
                    }
                })
                .Returns(Task.CompletedTask);
        }

        protected void TheTrackerRejects(Func<WriteBackFieldUpdate, bool> predicate, string errorMessage)
        {
            writeOutcome = update => predicate(update) ? (false, errorMessage) : (true, null);
        }

        protected void TheTrackerThrows(Exception exception)
        {
            writeFailure = exception;
        }

        protected void TheInstanceIsNotLicensedForPremium()
        {
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        }

        // --- Driving port: the scheduled refresh ---

        protected Task ThePortfolioRefreshRuns(int portfolioId)
            => RunUpdate(u => u.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolioId));

        protected Task TheForecastRefreshRuns(int portfolioId)
            => RunUpdate(u => u.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolioId));

        protected Task TheTeamRefreshRuns(int teamId)
            => RunUpdate(u => u.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamId));

        /// <summary>
        /// Triggers one update and waits for the queue to go idle. Admission is synchronous inside
        /// <c>EnqueueUpdate</c> (<c>TryAdmit</c> runs before the trigger returns), so the key is already
        /// active when this starts polling - the "not enqueued yet looks exactly like done" race that
        /// bites callers polling over HTTP cannot happen here.
        /// </summary>
        private async Task RunUpdate(Action<IServiceProvider> trigger)
        {
            var statusStore = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            trigger(Factory.Services);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue did not go idle within 30s - the refresh never completed.");
                }

                await Task.Delay(20);
            }
        }

        // --- Observation ---

        protected string? TheStoredValueOf(string referenceId, int fieldId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var feature = sp.GetRequiredService<IRepository<Feature>>().GetAll().FirstOrDefault(f => f.ReferenceId == referenceId);
            if (feature != null)
            {
                return feature.AdditionalFieldValues.TryGetValue(fieldId, out var featureValue) ? featureValue : null;
            }

            var workItem = sp.GetRequiredService<IWorkItemRepository>().GetAll().FirstOrDefault(w => w.ReferenceId == referenceId);
            return workItem != null && workItem.AdditionalFieldValues.TryGetValue(fieldId, out var value) ? value : null;
        }

        protected IReadOnlyList<RefreshLog> TheRefreshLog()
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IRefreshLogService>().GetRefreshLogs()];
        }

        /// <summary>
        /// Overwrites the stored copy the way an inbound sync does, so a scenario can show that the
        /// tracker still has the last word over a value write-back persisted locally (ADR-144's third
        /// bound on the D11 exception).
        /// </summary>
        protected void TheInboundSyncReports(string referenceId, int fieldId, string value)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // Straight through the context, not the repository: the repository's GetAll pulls the whole
            // Feature graph, and saving it back re-inserts the PortfolioTeam join row it already has.
            var context = sp.GetRequiredService<LighthouseAppContext>();
            var feature = context.Features.First(f => f.ReferenceId == referenceId);
            feature.AdditionalFieldValues[fieldId] = value;
            context.SaveChanges();
        }
    }
}
