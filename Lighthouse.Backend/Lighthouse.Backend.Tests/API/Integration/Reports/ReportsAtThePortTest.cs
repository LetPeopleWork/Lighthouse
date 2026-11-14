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
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
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
            services.AddScoped<IForecastFilterRuleService, ForecastFilterRuleService>();
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
}
