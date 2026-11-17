using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Reads and makes a Team's reports by calling the Reports services, with no web host: each scenario gets a
    /// database of its own and the real report services over the real repositories. The instance has no premium
    /// licence and believes it is nine in the morning of Friday 2 Oct 2026 in Europe/Zurich, as the hosted
    /// Reports scenarios do. Who may make a report is the route's rule, not the service's, so no sign-in is involved.
    /// </summary>
    public abstract class ReportsAtThePortTest
    {
        private static readonly TimeZoneInfo InstanceZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private static readonly DateTimeOffset NineInTheMorningOnFriday2Oct2026
            = new(2026, 10, 2, 9, 0, 0, InstanceZone.GetUtcOffset(new DateTime(2026, 10, 2, 9, 0, 0)));

        private ServicePortHarness harness = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            harness = new ServicePortHarness(Register);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            harness.Dispose();
        }

        /// <summary>A Team is stored that keeps its finished Work Items for this many days, or for ever when it is 0.</summary>
        protected ReportOwner ATeam(string name, int keepsFinishedItemsForDays = 365)
        {
            using var scope = harness.NewScope();

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DataRetrievalValue = "project = LIGHTSPEED",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = keepsFinishedItemsForDays,
            };

            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();

            return ReportOwner.Team(team.Id);
        }

        /// <summary>A Portfolio is stored that keeps its finished Features for a year.</summary>
        protected ReportOwner APortfolio(string name)
        {
            using var scope = harness.NewScope();

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DataRetrievalValue = "project = APOLLO",
                DoneItemsCutoffDays = 365,
            };

            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolios.Add(portfolio);
            portfolios.Save().GetAwaiter().GetResult();

            return ReportOwner.Portfolio(portfolio.Id);
        }

        /// <summary>
        /// The owner starts this many Work Items (a Portfolio, Features) on one day; they finish on <paramref name="finishedOn"/>,
        /// or are still in progress when it is not given.
        /// </summary>
        protected void TheOwnerStarted(ReportOwner owner, int count, DateOnly startedOn, DateOnly? finishedOn = null)
        {
            using var scope = harness.NewScope();
            var done = finishedOn is not null;
            var stateCategory = done ? StateCategories.Done : StateCategories.Doing;
            static DateTime During(DateOnly day) => day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);

            if (owner.Kind == ReportOwnerKind.Team)
            {
                var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(owner.Id)!;
                var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
                for (var i = 0; i < count; i++)
                {
                    workItems.Add(new WorkItem
                    {
                        Team = team,
                        TeamId = team.Id,
                        ReferenceId = $"WI-{Guid.NewGuid():N}",
                        Name = "Work Item",
                        Type = "User Story",
                        State = done ? "Done" : "In Progress",
                        StateCategory = stateCategory,
                        CreatedDate = During(startedOn.AddDays(-1)),
                        StartedDate = During(startedOn),
                        ClosedDate = done ? During(finishedOn!.Value) : null,
                        Order = "1",
                    });
                }

                workItems.Save().GetAwaiter().GetResult();
                return;
            }

            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(owner.Id)!;
            var features = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
            for (var i = 0; i < count; i++)
            {
                var feature = new Feature
                {
                    ReferenceId = $"FT-{Guid.NewGuid():N}",
                    Name = "Feature",
                    Type = "Epic",
                    State = done ? "Done" : "In Progress",
                    StateCategory = stateCategory,
                    CreatedDate = During(startedOn.AddDays(-1)),
                    StartedDate = During(startedOn),
                    ClosedDate = done ? During(finishedOn!.Value) : null,
                    Order = "1",
                };
                feature.Portfolios.Add(portfolio);
                features.Add(feature);
            }

            features.Save().GetAwaiter().GetResult();
        }

        /// <summary>A made report is read back as the route that opens one reads it.</summary>
        protected ReportOutcome<ReportRead> ReadAReport(ReportOwner owner, int reportId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().Get(owner, reportId);
        }

        /// <summary>The types of report the owner may make, as the route that lists them reads them.</summary>
        protected ReportOutcome<ReportTemplatesRead> TheTypesOfReportFor(ReportOwner owner)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().Templates(owner);
        }

        /// <summary>The report is asked for as the route that creates one asks, and the outcome is returned as it is, created or refused.</summary>
        protected async Task<ReportOutcome<ReportRead>> CreateAReport(ReportOwner owner, NewReport request)
        {
            using var scope = harness.NewScope();
            return await scope.ServiceProvider.GetRequiredService<IReportCommands>().Create(owner, request);
        }

        /// <summary>What every scenario of this base adds to the harness, read without building or resolving any of it.</summary>
        internal static IServiceCollection WhatItRegisters
        {
            get
            {
                var services = new ServiceCollection();
                Register(services);
                return services;
            }
        }

        private static void Register(IServiceCollection services)
        {
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(NineInTheMorningOnFriday2Oct2026, InstanceZone));
            services.AddSingleton(TimeProvider.System);
            services.AddScoped(_ => NoPremiumLicence());
            services.AddScoped(_ => RefreshSettingsOfTeamsAndPortfolios());
            services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
            services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<IRecurringBlackoutRuleService, RecurringBlackoutRuleService>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<ForecastFilterRuleService>();
            services.AddScoped<IForecastFilterRuleService>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
            services.AddScoped<IThroughputFilter>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
            services.AddScoped<ForecastingAnswersToFlowMetrics>();
            services.AddScoped<IForecastDataSufficiency>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
            services.AddScoped<IHowManyForecaster>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();

            services.AddScoped<ReportRepository>();
            services.AddScoped<IReportReader>(provider => provider.GetRequiredService<ReportRepository>());
            services.AddScoped<IReportWriter>(provider => provider.GetRequiredService<ReportRepository>());
            services.AddScoped<IReportOwnerLookup, ReportOwnerLookup>();
            services.AddScoped<TeamReportMetricSeries>();
            services.AddScoped<PortfolioReportMetricSeries>();
            services.AddScoped<IReportMetricSeries, ReportMetricSeriesByOwner>();
            services.AddScoped<IReportMetric, CycleTimeReportMetric>();
            services.AddScoped<IReportMetric, ThroughputReportMetric>();
            services.AddScoped<IReportMetric, WipReportMetric>();
            services.AddScoped<IReportMetric, WorkItemAgeReportMetric>();
            services.AddScoped<IReportMetric, ArrivalsReportMetric>();
            services.AddScoped<ReportMetricCatalog>();
            services.AddScoped<IReportTemplate, ThenAndNowTemplate>();
            services.AddScoped<IReportQueries, ReportQueries>();
            services.AddScoped<IReportCommands, ReportCommands>();
            services.AddScoped<IReportBulkCommands, ReportBulkCommands>();
            services.AddScoped<IReportBulkRemover, ReportBulkRemover>();
        }

        /// <summary>The metrics of a Team and of a Portfolio both read how often their numbers refresh when they are built.</summary>
        private static IAppSettingService RefreshSettingsOfTeamsAndPortfolios()
        {
            var refresh = new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 };
            var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
            settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(refresh);
            settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(refresh);
            return settings.Object;
        }

        private static ILicenseService NoPremiumLicence()
        {
            var licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(false);
            return licence.Object;
        }
    }
    /// <summary>
    /// A Team and a Portfolio each make a report that shows Arrivals and read it back: the type offers Arrivals last,
    /// the report carries what the owner really started and finished, and an empty window is a real zero, never a dash.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class ArrivalsInReportsScenarios : ReportsAtThePortTest
    {
        private static readonly string[] Members = ["total", "perDay", "throughputPerDay", "gapPerDay"];

        private const double Tolerance = 1e-9;

        private static readonly NewReport ArrivalsOverThirtyDays = new(
            "then-and-now", "Started against finished", ["arrivals"], new NewThenAndNow(new DateOnly(2026, 7, 31), 30, 30));

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_report_shows_how_many_Work_Items_the_owner_started_and_finished_in_Then(string ownerKind)
        {
            var owner = AnOwner(ownerKind);
            TheOwnerStarted(owner, 2, new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12));
            TheOwnerStarted(owner, 1, new DateOnly(2026, 7, 10));

            var created = await CreateAReport(owner, ArrivalsOverThirtyDays);
            Assert.That(created.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the report was not created: {created.Code}");
            var read = ReadAReport(owner, created.Value!.Id);
            Assert.That(read.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the report could not be read: {read.Code}");
            var then = read.Value!.ThenAndNow!.Metrics["arrivals"].AtThen;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(then.Keys, Is.EquivalentTo(Members), "Then must be frozen with all four values");
                Assert.That(then["total"], Is.EqualTo(CapturedValue.Shown(3, 30)), "three started in the window, over its thirty days");
                Assert.That(then["perDay"].Value, Is.EqualTo(3.0 / 30).Within(Tolerance), "started per day must be three over thirty days");
                Assert.That(then["throughputPerDay"].Value, Is.EqualTo(2.0 / 30).Within(Tolerance), "only two finished, so finished per day is two over thirty days");
                Assert.That(then["gapPerDay"].Value, Is.EqualTo(1.0 / 30).Within(Tolerance), "one more started than finished, so the gap is one over thirty days");
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_window_where_nothing_was_started_or_finished_reads_a_real_zero_and_Arrivals_is_offered_last(string ownerKind)
        {
            var owner = AnOwner(ownerKind);

            var offered = TheTypesOfReportFor(owner).Value!.Templates.Single().Metrics.Select(metric => metric.Key).ToList();
            var created = await CreateAReport(owner, ArrivalsOverThirtyDays);
            Assert.That(created.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the report was not created: {created.Code}");
            var read = ReadAReport(owner, created.Value!.Id);
            Assert.That(read.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the report could not be read: {read.Code}");
            var arrivals = read.Value!.ThenAndNow!.Metrics["arrivals"];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered[^1], Is.EqualTo("arrivals"), "Arrivals must be offered, last in the catalog order");
                Assert.That(arrivals.AtThen.Keys, Is.EquivalentTo(Members), "Then must be frozen with all four values");
                Assert.That(arrivals.AtNow.Keys, Is.EquivalentTo(Members), "Now must be computed with all four values");
                Assert.That(arrivals.AtThen["gapPerDay"], Is.EqualTo(CapturedValue.Shown(0, 30)), "nothing started or finished is a real zero gap");
            }
        }

        private ReportOwner AnOwner(string ownerKind)
            => ownerKind == "team" ? ATeam("Team Lightspeed") : APortfolio("Project Apollo");
    }
}
