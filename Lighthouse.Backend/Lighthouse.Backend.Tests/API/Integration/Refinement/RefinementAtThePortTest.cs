using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Opens a Team's Refinement tab by calling its query, with no web host: each scenario gets a database of its
    /// own, the real repositories, calendar and need calculator the tab reads through, and the shipped forecast
    /// engine under a script a scenario may fill in per number of working days. Sign-in is off and no request is
    /// served, as on an instance without accounts. A Team is set up only through <see cref="RefinementPortGivens"/>.
    /// </summary>
    public abstract class RefinementAtThePortTest
    {
        private ServicePortHarness harness = null!;

        private FakeLighthouseClock clock = null!;

        private ScriptedForecasts forecasts = null!;

        protected RefinementPortGivens Givens { get; private set; } = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            clock = new FakeLighthouseClock(TestToday.FrozenNow);
            forecasts = new ScriptedForecasts();
            harness = new ServicePortHarness(services => Register(services, clock, forecasts));
            Givens = new RefinementPortGivens(harness.NewScope().ServiceProvider);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            harness.Dispose();
        }

        /// <summary>The instance believes it is this day, nine in the morning, in a time zone that is UTC.</summary>
        protected void TodayIs(int year, int month, int day)
            => clock.SetInstant(new DateTimeOffset(year, month, day, 9, 0, 0, TimeSpan.Zero));

        /// <summary>
        /// Over this many working days the Team is likely to pull the given number of Work Items at each given
        /// likelihood, read the way the band reads them. Between two given likelihoods the higher one's count holds.
        /// </summary>
        protected void TheTeamIsLikelyToPull(int overWorkingDays, params (int Percentile, int Count)[] likelihoods)
            => forecasts.Script(overWorkingDays, likelihoods);

        /// <summary>The tab as an anonymous reader opens it, or null when no such Team is stored.</summary>
        protected RefinementView? TheRefinementTabOf(int teamId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IRefinementViewQuery>().ForTeam(teamId, null);
        }

        /// <summary>One step of a scenario, for a Given that goes through a real service.</summary>
        protected IServiceScope NewScope() => harness.NewScope();

        /// <summary>What every scenario of this base adds to the harness, read without building or resolving any of it.</summary>
        internal static IServiceCollection WhatItRegisters
        {
            get
            {
                var services = new ServiceCollection();
                Register(services, new FakeLighthouseClock(TestToday.FrozenNow), new ScriptedForecasts());
                return services;
            }
        }

        private static void Register(IServiceCollection services, FakeLighthouseClock clock, ScriptedForecasts forecasts)
        {
            services.AddSingleton<ILighthouseClock>(clock);
            services.AddScoped(_ => PortDoubles.PremiumLicence());
            services.AddScoped(_ => PortDoubles.SettingsAsSeeded());
            services.AddScoped(_ => PortDoubles.SignInOff());
            services.AddSingleton(PortDoubles.NoRequest());
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<ISizingLogRepository, SizingLogRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<IRecurringBlackoutRuleService, RecurringBlackoutRuleService>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<IForecastFilterRuleService, ForecastFilterRuleService>();
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();

            services.AddScoped<RefinementList>();
            services.AddScoped<SleYardstickResolver>();
            services.AddScoped<VoterIdentityResolver>();
            services.AddScoped<StageRuleMatcher>();
            services.AddScoped<IRefinementCalendar, RefinementCalendar>();
            services.AddScoped<RefinementNeedCalculator>();
            services.AddScoped<IRefinementViewQuery, RefinementViewQuery>();

            RegisterTheShippedEngine(services);
            services.AddScoped<IForecastService>(provider => new ForecastWithScriptedHorizons(ActivatorUtilities.CreateInstance<ForecastService>(provider), forecasts));
        }

        /// <summary>The engine as the product builds it. A horizon no scenario scripted runs it over the Team's finished Work Items.</summary>
        private static void RegisterTheShippedEngine(IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IRandomNumberService, RandomNumberService>();
            services.AddSingleton<IDrawStreamFactory, DrawStreamFactory>();
            services.AddSingleton(ForecastSimulationLimits.Default);
            services.AddSingleton<ForecastSimulation>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            services.AddScoped<IDependencyDecision, DependencyDecision>();
            services.AddScoped<IWhatTheForecastWaitsFor, WhatTheForecastWaitsFor>();
        }

        /// <summary>
        /// The forecasts a scenario chose, by the number of working days they cover. A simulation of one hundred runs
        /// is built so that reading it at (100 - p) gives exactly the count chosen for likelihood p.
        /// </summary>
        private sealed class ScriptedForecasts
        {
            private const int Runs = 100;

            private readonly Dictionary<int, List<(int Percentile, int Count)>> byHorizon = [];

            public void Script(int workingDays, (int Percentile, int Count)[] likelihoods)
                => byHorizon[workingDays] = [.. likelihoods.OrderBy(likelihood => likelihood.Percentile)];

            public HowManyForecast? ForecastFor(int days)
            {
                if (!byHorizon.TryGetValue(days, out var likelihoods))
                {
                    return null;
                }

                var runs = new Dictionary<int, int>();
                for (var position = 1; position <= Runs; position++)
                {
                    var count = CountAt(likelihoods, Runs - position);
                    runs[count] = runs.GetValueOrDefault(count) + 1;
                }

                return new HowManyForecast(runs, days, DateTime.UtcNow);
            }

            private static int CountAt(List<(int Percentile, int Count)> likelihoods, int percentile)
                => likelihoods.Where(likelihood => likelihood.Percentile >= percentile).Select(likelihood => likelihood.Count).DefaultIfEmpty(likelihoods[^1].Count).First();
        }

        /// <summary>The shipped forecast engine, except that a horizon the scenario scripted gets the scripted forecast.</summary>
        private sealed class ForecastWithScriptedHorizons(ForecastService shipped, ScriptedForecasts script) : IForecastService
        {
            public Task<ForecastRunOutcome> UpdateForecastsForPortfolio(Portfolio portfolio, DateOnly instanceDay, CancellationToken cancellationToken = default)
                => shipped.UpdateForecastsForPortfolio(portfolio, instanceDay, cancellationToken);

            public HowManyForecast HowMany(RunChartData throughput, int days)
                => script.ForecastFor(days) ?? shipped.HowMany(throughput, days);

            public HowManyForecast PredictWorkItemCreation(Team team, string[] workItemTypes, DateTime startDate, DateTime endDate, int daysToForecast)
                => shipped.PredictWorkItemCreation(team, workItemTypes, startDate, endDate, daysToForecast);

            public Task<WhenForecast> When(Team team, int remainingItems, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
                => shipped.When(team, remainingItems, mode);
        }
    }
}
