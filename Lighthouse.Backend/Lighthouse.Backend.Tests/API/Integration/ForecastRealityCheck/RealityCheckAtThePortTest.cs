using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
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
    /// same day the scenarios on a web host use, and the licence is premium, as it is there.
    ///
    /// What a caller over HTTP receives (the route, the status, the names and formats of the answer) is pinned by
    /// RealityCheckWireContractTest; scenarios here pin what the check works out.
    /// </summary>
    public abstract class RealityCheckAtThePortTest
    {
        protected static readonly DateTimeOffset Today = RealityCheckForecastScript.Today;

        /// <summary>The sampling windows every Team is checked against, whatever its own setting is.</summary>
        protected static readonly int[] StandardWindowDays = RealityCheckForecastScript.StandardWindowDays;

        /// <summary>One, two, four and eight weeks, each ending today.</summary>
        protected static readonly int[] HorizonDays = RealityCheckForecastScript.HorizonDays;

        protected static readonly int[] ConfidenceLevels = [50, 70, 85, 95];

        private ServicePortHarness harness = null!;

        private RealityCheckSeedSteps seed = null!;

        protected RealityCheckForecastScript Forecasts { get; private set; } = null!;

        protected static DateOnly TodayDay => RealityCheckForecastScript.TodayDay;

        /// <summary>
        /// Whether a scenario of this fixture may ask for the shipped engine. Where none does, the engine under
        /// the script refuses to be reached, so a check the script failed to answer cannot pass by simulating.
        /// </summary>
        protected virtual bool AScenarioMayRunTheShippedEngine => false;

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

        protected int GivenATeamForecastingFromFixedDates(string name, int inertSamplingWindowDays)
            => seed.GivenATeamForecastingFromFixedDates(name, inertSamplingWindowDays);

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

        // --- Reading what is stored ---

        protected RealityCheckSeedSteps.TeamAsStored WhatIsStoredFor(int teamId) => seed.WhatIsStoredFor(teamId);

        protected DatabaseWriteWatch DatabaseWrites => harness.Writes;

        /// <summary>What this fixture adds to the harness, read without building or resolving any of it.</summary>
        internal IServiceCollection WhatItRegisters
        {
            get
            {
                var services = new ServiceCollection();
                RegisterWhatTheCheckReads(services);
                return services;
            }
        }

        private void RegisterWhatTheCheckReads(IServiceCollection services)
        {
            var forecasts = Forecasts;

            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Today));
            services.AddScoped(_ => PortDoubles.PremiumLicence());
            services.AddScoped(_ => PortDoubles.SettingsAsSeeded());
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

            services.AddScoped<IForecastRealityCheckService, ForecastRealityCheckService>();

            if (!AScenarioMayRunTheShippedEngine)
            {
                services.AddScoped(_ => forecasts.Over(Mock.Of<IForecastService>(MockBehavior.Strict)));
                return;
            }

            RegisterTheShippedEngine(services);
            services.AddScoped(provider => forecasts.Over(ActivatorUtilities.CreateInstance<ForecastService>(provider)));
        }

        /// <summary>The engine as the product builds it, with its starting number pinned and fewer runs.</summary>
        private static void RegisterTheShippedEngine(IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IRandomNumberService, RandomNumberService>();
            services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(RealityCheckForecastScript.PinnedStartingNumber));
            services.AddSingleton(new ForecastSimulationLimits(
                RealityCheckForecastScript.SimulatedRunsWhenTheEngineRuns,
                ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));
            services.AddSingleton<ForecastSimulation>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            services.AddScoped<IDependencyDecision, DependencyDecision>();
            services.AddScoped<IWhatTheForecastWaitsFor, WhatTheForecastWaitsFor>();
        }
    }
}
