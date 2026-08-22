using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// A bulk refresh touches every Team and every Portfolio. The simulation behind a delivery date is
    /// not seeded, so running it twice for the same Portfolio returns two slightly different dates and
    /// whoever is watching sees the date settle and then move. The promise here is that one round of
    /// "refresh everything" produces one forecast per Portfolio, and that splitting the forecast out of
    /// the Portfolio refresh costs the work tracking system nothing: it is still reached once for the
    /// round, carrying exactly what both passes resolved.
    ///
    /// The driving port is the scheduled refresh: an updater is triggered and the production update
    /// queue runs it in its own scope. Faked are the work-tracking connector and the licence service -
    /// the two ports that would otherwise reach outside the process - the forecast service, which is
    /// both non-deterministic and, being the thing counted here, needed as a countable seam, and the
    /// item-fetching service, whose job is to pull items from the tracker and which would otherwise
    /// delete the seeded Features as items the (empty) fake tracker no longer returns. Everything
    /// between the trigger and those seams stays production: the queue, the status store, the domain-event
    /// bus, the write-back resolver and collector, and EF over SQLite.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-00")]
    public class Slice00OneForecastPerBatchScenarios
    {
        private const string SizeFieldReference = "customfield_10042";
        private const string ForecastFieldReference = "customfield_10099";
        private const string StaleSize = "1";
        private const string StaleForecast = "1999-01-01";
        private const string FeatureReference = "PROJ-1";
        private const int FeatureSize = 5;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private Mock<IForecastService> forecastServiceMock = null!;

        /// <summary>
        /// Every call the refresh made to the work tracking system, in order, each an immutable snapshot
        /// of what that call carried. This list is the guarantee: not how many times Lighthouse flushed,
        /// but how many times and with what it reached the tracker.
        /// </summary>
        private List<IReadOnlyList<WriteBackFieldUpdate>> connectorWrites = null!;

        private Exception? trackerFailure;

        /// <summary>
        /// The signals the refresh raised. Nothing persists the fact that a delivery date was announced,
        /// so how often it was announced is only observable on the bus.
        /// </summary>
        private CapturedDomainEvents raisedSignals = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            connectorWrites = [];
            trackerFailure = null;
            raisedSignals = new CapturedDomainEvents();

            var licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            var connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock.Setup(c => c.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            connectorMock.Setup(c => c.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            connectorMock.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>())).Returns([]);
            connectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync([]);
            connectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>())).ReturnsAsync([]);
            connectorMock.Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .Returns((WorkTrackingSystemConnection _, IReadOnlyList<WriteBackFieldUpdate> updates) => RecordWrite(updates));

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);

            forecastServiceMock = new Mock<IForecastService>();

            var workItemServiceMock = new Mock<IWorkItemService>();
            workItemServiceMock.Setup(s => s.UpdateFeaturesForPortfolio(It.IsAny<Portfolio>())).ReturnsAsync(SyncOutcome.None);
            workItemServiceMock.Setup(s => s.UpdateWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync(SyncOutcome.None);

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);

                    services.RemoveAll<IWorkTrackingConnectorFactory>();
                    services.AddScoped(_ => connectorFactoryMock.Object);

                    services.RemoveAll<IForecastService>();
                    services.AddScoped(_ => forecastServiceMock.Object);

                    services.RemoveAll<IWorkItemService>();
                    services.AddScoped(_ => workItemServiceMock.Object);

                    services.AddScoped<IDomainEventHandler<PortfolioForecastsUpdated>>(_ => new CapturingDomainEventHandler<PortfolioForecastsUpdated>(raisedSignals));
                    services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>>(_ => new CapturingDomainEventHandler<PortfolioFeaturesRefreshed>(raisedSignals));
                });
            });

            using var setupScope = factory.Services.CreateScope();
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
            using (var teardownScope = factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            factory.Dispose();
            rootFactory.Dispose();
        }

        // @driving_port @us-10 - one round of "refresh everything", one delivery date.
        [Test]
        public async Task Refreshing_everything_produces_one_forecast_for_the_portfolio_not_one_per_team()
        {
            var portfolioId = GivenAPortfolioDeliveredByTeams(teamCount: 2);

            await WhenEverythingIsRefreshed();

            ThenThePortfolioWasForecastExactlyOnce(portfolioId);
        }

        // @driving_port @us-10 - moving the forecast out of the Portfolio refresh costs the work tracking
        // system nothing: one round, one conversation, carrying what both passes resolved.
        [Test]
        public async Task Moving_the_forecast_into_its_own_execution_still_reaches_the_tracker_once()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenTheForecastRunCompletesIn(10);

            await WhenThePortfolioIsRefreshed(portfolio);

            ThenTheTrackerWasReached(times: 1);
            ThenTheWriteCarriedExactly(0,
                (FeatureReference, SizeFieldReference, FeatureSize.ToString()),
                (FeatureReference, ForecastFieldReference, TheForecastDateAfter(10)));
        }

        // @driving_port @us-10 - the baseline shape captured on the dogfood instance: one call per
        // refresh, the second carrying only what actually moved since the first.
        [Test]
        public async Task Two_consecutive_refreshes_reach_the_tracker_once_each_and_the_second_carries_only_what_moved()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();

            GivenTheForecastRunCompletesIn(10);
            await WhenThePortfolioIsRefreshed(portfolio);

            GivenTheForecastRunCompletesIn(11);
            await WhenThePortfolioIsRefreshed(portfolio);

            ThenTheTrackerWasReached(times: 2);
            ThenTheWriteCarriedExactly(0,
                (FeatureReference, SizeFieldReference, FeatureSize.ToString()),
                (FeatureReference, ForecastFieldReference, TheForecastDateAfter(10)));
            ThenTheWriteCarriedExactly(1,
                (FeatureReference, ForecastFieldReference, TheForecastDateAfter(11)));
        }

        // @error @driving_port @us-10 - the round drains its staging area before the first write, so a
        // write that failed leaves neither a half-updated local copy nor a residue to be sent twice.
        [Test]
        public async Task A_forecast_write_back_that_failed_leaves_nothing_half_written()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenTheForecastRunCompletesIn(10);
            GivenTheTrackerIsUnreachable();

            await WhenThePortfolioIsRefreshed(portfolio);

            ThenTheTrackerWasReached(times: 1);
            ThenNothingWasWrittenIntoTheLocalCopy(portfolio);

            GivenTheTrackerIsReachableAgain();
            await WhenThePortfolioIsRefreshed(portfolio);

            ThenTheTrackerWasReached(times: 2);
            ThenTheWriteCarriedExactly(1,
                (FeatureReference, SizeFieldReference, FeatureSize.ToString()),
                (FeatureReference, ForecastFieldReference, TheForecastDateAfter(10)));
        }


        // @driving_port @us-10 - the two background loops keep their own schedules, so a team refresh is
        // regularly still in flight when the portfolio refresh arrives and asks for a delivery date.
        [Test]
        public async Task A_portfolio_refresh_overlapping_a_team_refresh_settles_on_one_delivery_date()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenTheForecastRunCompletesIn(10);

            await WhenThePortfolioAndItsTeamAreRefreshedTogether(portfolio);

            ThenThePortfolioWasForecastExactlyOnce(portfolio.Id);
            ThenANewDeliveryDateWasAnnouncedExactlyOnceFor(portfolio.Id);
            ThenTheDeliveryDateTheTrackerReceivedWas(TheForecastDateAfter(10));
        }

        // @driving_port @us-10 - one announcement per portfolio, not one for the batch and not one per
        // portfolio pass: everything listening records a round once per announcement.
        [Test]
        public async Task Refreshing_everything_announces_a_new_delivery_date_once_for_each_portfolio()
        {
            var firstPortfolioId = GivenAPortfolioDeliveredByTeams(teamCount: 2);
            var secondPortfolioId = GivenAPortfolioDeliveredByTeams(teamCount: 2);

            await WhenEverythingIsRefreshed();

            ThenANewDeliveryDateWasAnnouncedExactlyOnceFor(firstPortfolioId);
            ThenANewDeliveryDateWasAnnouncedExactlyOnceFor(secondPortfolioId);
        }

        // @driving_port @us-10 - the forecast left the portfolio refresh; nothing else did.
        [Test]
        public async Task The_portfolio_refresh_still_does_everything_that_was_never_about_forecasting()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            var abandonedFeature = GivenAFeatureNoPortfolioClaimsAnyMore();
            GivenTheForecastRunCompletesIn(10);

            await WhenThePortfolioIsRefreshed(portfolio);

            ThenTheRefreshedFeaturesWereAnnouncedFor(portfolio.Id);
            ThenTheFeatureSizeTheTrackerReceivedWas(FeatureSize.ToString());
            ThenTheAbandonedFeatureIsGone(abandonedFeature);
        }

        // --- Given ---

        private int GivenAPortfolioDeliveredByTeams(int teamCount)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            var portfolio = new Portfolio
            {
                Name = "Payments Modernisation",
                WorkTrackingSystemConnection = NewConnection(),
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            var feature = new Feature
            {
                Name = "Instant settlement",
                ReferenceId = "FTR-1",
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
            };
            feature.Portfolios.Add(portfolio);

            for (var i = 1; i <= teamCount; i++)
            {
                var team = new Team
                {
                    Name = $"Team {i}",
                    WorkTrackingSystemConnection = NewConnection(),
                    DoneItemsCutoffDays = 365,
                    DataRetrievalValue = "project = TEST",
                    WorkItemTypes = ["Story"],
                    ToDoStates = ["New"],
                    DoingStates = ["In Progress"],
                    DoneStates = ["Done"],
                };

                teamRepository.Add(team);
                teamRepository.Save().GetAwaiter().GetResult();

                feature.FeatureWork.Add(new FeatureWork(team, 3, 3, feature));
            }

            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        /// <summary>
        /// A Portfolio carrying one mapping of each kind, so both passes of the refresh resolve something:
        /// the Features pass writes the size, the forecast pass writes the delivery date. They target
        /// different fields because the resolver splits its mappings on whether the value comes from a
        /// forecast, so one mapping can never be resolved by both passes.
        ///
        /// The stored copies are seeded stale on purpose: a field the item has no stored value for at all
        /// is skipped, and a field already holding the resolved value is not a change, so neither would
        /// reach the tracker.
        /// </summary>
        private WrittenBackPortfolio GivenAPortfolioWhoseSizeAndForecastAreWrittenBack()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = NewConnection();

            var sizeField = new AdditionalFieldDefinition { Reference = SizeFieldReference, DisplayName = "Size" };
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

            var connectionRepository = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            connectionRepository.Add(connection);
            connectionRepository.Save().GetAwaiter().GetResult();

            var portfolio = new Portfolio
            {
                Name = "Payments Modernisation",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            var team = new Team
            {
                Name = "Payments Team",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            var feature = new Feature(team, FeatureSize)
            {
                Name = "Instant settlement",
                ReferenceId = FeatureReference,
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
            };
            feature.Portfolios.Add(portfolio);
            feature.AdditionalFieldValues[sizeField.Id] = StaleSize;
            feature.AdditionalFieldValues[forecastField.Id] = StaleForecast;

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return new WrittenBackPortfolio(portfolio.Id, team.Id, sizeField.Id, forecastField.Id);
        }

        /// <summary>
        /// Production fills a Feature's forecasts inside the forecast service; the fake does the same with
        /// a fixed number of working days, so the resolved date is deterministic without pinning a Monte
        /// Carlo run.
        /// </summary>
        private void GivenTheForecastRunCompletesIn(int workingDays)
        {
            forecastServiceMock
                .Setup(s => s.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()))
                .Callback((Portfolio portfolio) =>
                {
                    foreach (var feature in portfolio.Features)
                    {
                        var simulation = new SimulationResult();
                        simulation.SimulationResults[workingDays] = 100;
                        feature.SetFeatureForecasts([new WhenForecast(simulation) { HasSufficientData = true }]);
                    }
                })
                .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// A Feature no Portfolio claims any more - what a portfolio's query leaves behind when it stops
        /// matching an item. Removing it is part of the refresh, not part of forecasting.
        /// </summary>
        private string GivenAFeatureNoPortfolioClaimsAnyMore()
        {
            using var scope = factory.Services.CreateScope();
            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            var abandoned = new Feature
            {
                Name = "Dropped out of the query",
                ReferenceId = "FTR-ABANDONED",
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
            };

            featureRepository.Add(abandoned);
            featureRepository.Save().GetAwaiter().GetResult();

            return abandoned.ReferenceId;
        }

        private void GivenTheTrackerIsUnreachable()
            => trackerFailure = new HttpRequestException("The tracker is unreachable");

        private void GivenTheTrackerIsReachableAgain() => trackerFailure = null;

        // --- When ---

        /// <summary>
        /// What the two background loops do on their own schedule: every Team is triggered, every
        /// Portfolio is triggered, and the queue is left to work through all of it.
        /// </summary>
        private async Task WhenEverythingIsRefreshed()
        {
            var sp = factory.Services;
            var teamUpdater = sp.GetRequiredService<ITeamUpdater>();
            var portfolioUpdater = sp.GetRequiredService<IPortfolioUpdater>();

            using (var scope = sp.CreateScope())
            {
                foreach (var team in scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetAll().ToList())
                {
                    teamUpdater.TriggerUpdate(team.Id);
                }

                foreach (var portfolio in scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetAll().ToList())
                {
                    portfolioUpdater.TriggerUpdate(portfolio.Id);
                }
            }

            await WaitUntilTheQueueStaysIdle();
        }

        private async Task WhenThePortfolioIsRefreshed(WrittenBackPortfolio portfolio)
        {
            factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id);

            await WaitUntilTheQueueStaysIdle();
        }

        /// <summary>
        /// The portfolio refresh asks for a delivery date while its team is still being refreshed - the
        /// two loops do not coordinate, so whether the team is admitted before or after that request is
        /// a race. The promise holds either way round.
        /// </summary>
        private async Task WhenThePortfolioAndItsTeamAreRefreshedTogether(WrittenBackPortfolio portfolio)
        {
            var sp = factory.Services;

            sp.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id);
            sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(portfolio.TeamId);

            await WaitUntilTheQueueStaysIdle();
        }

        /// <summary>
        /// A forecast that is waiting for the last Team to finish is not in the queue yet, so a single
        /// idle reading cannot tell "everything is done" from "the next thing has not been handed over
        /// yet". Idle has to hold still for a while before it means anything.
        /// </summary>
        private async Task WaitUntilTheQueueStaysIdle()
        {
            var statusStore = factory.Services.GetRequiredService<IUpdateStatusStore>();

            var deadline = DateTime.UtcNow.AddSeconds(60);
            var consecutiveIdleReadings = 0;

            while (consecutiveIdleReadings < 25)
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue never settled within 60s, so nothing can be concluded about how often the portfolio was forecast.");
                }

                await Task.Delay(20);
                consecutiveIdleReadings = statusStore.HasActiveWork() ? 0 : consecutiveIdleReadings + 1;
            }
        }

        // --- Then ---

        private void ThenThePortfolioWasForecastExactlyOnce(int portfolioId)
            => forecastServiceMock.Verify(
                s => s.UpdateForecastsForPortfolio(It.Is<Portfolio>(p => p.Id == portfolioId)),
                Times.Once,
                "The simulation is not seeded, so a second run for the same portfolio moves the delivery date the first one just showed.");

        private void ThenTheTrackerWasReached(int times)
            => Assert.That(connectorWrites, Has.Count.EqualTo(times),
                $"The refresh reached the work tracking system {connectorWrites.Count} time(s): {DescribeWrites()}");

        /// <summary>
        /// The set of values one call carried, compared pair for pair. Comparing how many values arrived
        /// would let a split write that carried the right number of wrong ones pass, which is the failure
        /// this scenario exists to exclude.
        /// </summary>
        private void ThenTheWriteCarriedExactly(int callIndex, params (string WorkItem, string Field, string Value)[] expected)
        {
            var carried = callIndex < connectorWrites.Count
                ? ValuesOf(connectorWrites[callIndex])
                : [];

            Assert.That(carried, Is.EquivalentTo(expected),
                $"One round resolves in two passes and reaches the tracker once, carrying every value both passes resolved and no other: {DescribeWrites()}");
        }

        private void ThenNothingWasWrittenIntoTheLocalCopy(WrittenBackPortfolio portfolio)
        {
            using var scope = factory.Services.CreateScope();
            var feature = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>()
                .GetAll().First(f => f.ReferenceId == FeatureReference);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.AdditionalFieldValues[portfolio.SizeFieldId], Is.EqualTo(StaleSize),
                    "A write the tracker never accepted must leave the local copy exactly as it was.");
                Assert.That(feature.AdditionalFieldValues[portfolio.ForecastFieldId], Is.EqualTo(StaleForecast),
                    "A write the tracker never accepted must leave the local copy exactly as it was.");
            }
        }

        private void ThenANewDeliveryDateWasAnnouncedExactlyOnceFor(int portfolioId)
            => Assert.That(
                raisedSignals.Of<PortfolioForecastsUpdated>().Count(raised => raised.PortfolioId == portfolioId),
                Is.EqualTo(1),
                "Everything downstream of this announcement runs once per announcement - the delivery snapshot above all - so a second one for the same round records that round twice.");

        private void ThenTheRefreshedFeaturesWereAnnouncedFor(int portfolioId)
            => Assert.That(
                raisedSignals.Of<PortfolioFeaturesRefreshed>().ConvertAll(raised => raised.PortfolioId),
                Has.Member(portfolioId),
                "Moving the forecast out of the portfolio refresh must not have taken this announcement with it.");

        /// <summary>
        /// Every value the delivery-date field received across the whole round, so a second write moving a
        /// date the first one just showed is a failure rather than an unobserved extra.
        /// </summary>
        private void ThenTheDeliveryDateTheTrackerReceivedWas(string expected)
            => Assert.That(ValuesWrittenTo(ForecastFieldReference), Is.EquivalentTo(new[] { expected }),
                $"A delivery date that reaches the tracker twice in one round is one an operator watches settle and then move: {DescribeWrites()}");

        private void ThenTheFeatureSizeTheTrackerReceivedWas(string expected)
            => Assert.That(ValuesWrittenTo(SizeFieldReference), Is.EquivalentTo(new[] { expected }),
                $"The size is resolved by the features pass, which the forecast moving out was not supposed to touch: {DescribeWrites()}");

        private void ThenTheAbandonedFeatureIsGone(string referenceId)
        {
            using var scope = factory.Services.CreateScope();
            var stored = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>()
                .GetAll().Select(feature => feature.ReferenceId).ToList();

            Assert.That(stored, Has.No.Member(referenceId),
                "Clearing out features no portfolio claims any more is part of the refresh, and it still is.");
        }

        // --- Helpers ---

        private Task<WriteBackResult> RecordWrite(IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            connectorWrites.Add([.. updates]);

            if (trackerFailure != null)
            {
                throw trackerFailure;
            }

            return Task.FromResult(new WriteBackResult
            {
                ItemResults = [.. updates.Select(update => new WriteBackItemResult
                {
                    WorkItemId = update.WorkItemId,
                    TargetFieldReference = update.TargetFieldReference,
                    Success = true,
                    NotificationSuppression = NotificationSuppression.Suppressed,
                })],
            });
        }

        private List<string> ValuesWrittenTo(string fieldReference)
            => [.. connectorWrites
                .SelectMany(write => write)
                .Where(update => update.TargetFieldReference == fieldReference)
                .Select(update => update.Value)];

        private static IEnumerable<(string WorkItem, string Field, string Value)> ValuesOf(IReadOnlyList<WriteBackFieldUpdate> updates)
            => updates.Select(update => (update.WorkItemId, update.TargetFieldReference, update.Value));

        private string DescribeWrites()
            => string.Join(" | ", connectorWrites.Select(write =>
                string.Join(", ", write.Select(update => $"{update.WorkItemId}/{update.TargetFieldReference}={update.Value}"))));

        private string TheForecastDateAfter(int workingDays)
        {
            using var scope = factory.Services.CreateScope();
            var clock = scope.ServiceProvider.GetRequiredService<ILighthouseClock>();

            return clock.TodayAsUtcMidnight.AddDays(workingDays).ToString("yyyy-MM-dd");
        }

        private static WorkTrackingSystemConnection NewConnection() => new()
        {
            Name = $"Connection {Guid.NewGuid():N}",
            WorkTrackingSystem = WorkTrackingSystems.Jira,
        };

        private readonly record struct WrittenBackPortfolio(int Id, int TeamId, int SizeFieldId, int ForecastFieldId);
    }
}
