using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Runs the Forecast Reality Check by calling its service, with no web host: each scenario gets a database of
    /// its own, the real services the check reads through, and the scripted forecast. The clock is pinned to the
    /// same day the host's scenarios use, and the licence is premium, as it is there.
    ///
    /// What a caller over HTTP receives (the route, the status, the names and formats of the answer) is pinned at
    /// the host; scenarios here pin what the check works out.
    /// </summary>
    public abstract class RealityCheckAtThePortTest
    {
        protected static readonly DateTimeOffset Today = RealityCheckForecastScript.Today;

        private ServicePortHarness harness = null!;

        private RealityCheckSeedSteps seed = null!;

        protected RealityCheckForecastScript Forecasts { get; private set; } = null!;

        protected static DateOnly TodayDay => RealityCheckForecastScript.TodayDay;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            Forecasts = new RealityCheckForecastScript();
            harness = new ServicePortHarness(RegisterWhatTheCheckReads);
            seed = new RealityCheckSeedSteps(harness.NewScope().ServiceProvider, Forecasts, TodayDay);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            harness.Dispose();
        }

        // --- Teams and their finished work ---

        protected int GivenATeam(string name, int samplingWindowDays) => seed.GivenATeam(name, samplingWindowDays);

        protected void GivenTheTeamFinishedAWorkItemEveryDay(int teamId) => seed.GivenTheTeamFinishedAWorkItemEveryDay(teamId);

        protected void GivenTheTeamFinishedAWorkItemEvery(int teamId, int days) => seed.GivenTheTeamFinishedAWorkItemEvery(teamId, days);

        protected void GivenTheTeamFinishedWorkItemsOn(int teamId, List<DateOnly> days) => seed.GivenTheTeamFinishedWorkItemsOn(teamId, days);

        // --- Asking ---

        /// <summary>The Team's own filter setting decides, as it does when a caller sends no options.</summary>
        protected RealityCheckAnswer TheAnswerTo(int teamId)
        {
            using var scope = harness.NewScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId);
            Assert.That(team, Is.Not.Null, $"no Team {teamId} is stored to run the check for");

            var result = scope.ServiceProvider.GetRequiredService<IForecastRealityCheckService>()
                .Run(team!, ThroughputFilterMode.RespectTeamSetting);

            return RealityCheckPortAnswer.ReadFrom(result, Forecasts);
        }

        private void RegisterWhatTheCheckReads(IServiceCollection services)
        {
            var forecasts = Forecasts;

            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Today));
            services.AddSingleton(PortDoubles.PremiumLicence());
            services.AddSingleton(PortDoubles.SettingsAsSeeded());
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<IForecastFilterRuleService, ForecastFilterRuleService>();
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();

            // No scenario here asks for the simulation, so the engine under the script refuses to be reached.
            services.AddScoped(_ => forecasts.Over(Mock.Of<IForecastService>(MockBehavior.Strict)));
            services.AddScoped<IForecastRealityCheckService, ForecastRealityCheckService>();
        }
    }
}
