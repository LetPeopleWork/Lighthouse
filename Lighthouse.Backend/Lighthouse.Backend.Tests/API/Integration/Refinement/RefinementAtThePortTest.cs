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
        protected const string Backlog = "Backlog";

        protected const string Next = "Next";

        protected const string Analysing = "Analysing";

        protected const string Implementation = "Implementation";

        protected const string Done = "Done";

        protected const int ThroughputHistoryDays = 30;

        protected const string BillingExport = "GR-040";

        protected const string AdvancedReporting = "GR-051";

        protected const string ApiVersioning = "GR-054";

        protected const string UserActivityTracking = "GR-058";

        protected const string AdvancedSearch = "GR-059";

        protected const string ConfigurationManagement = "GR-073";

        protected const string LoadTesting = "GR-074";

        /// <summary>The Gravity demo Team's tracker states: To Do "Backlog"; Doing "Next", "Analysing" and "Implementation"; Done "Done".</summary>
        protected static readonly TeamStates GravitysStates = new([Backlog], [Next, Analysing, Implementation], [Done]);

        /// <summary>Wednesday 7 October 2026, nine in the morning: the moment the tracker's dates are counted back from.</summary>
        private static readonly DateTime TrackerReadOn = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

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

        /// <summary>
        /// Team Gravity refines in Backlog, Analysing and Next, and its tracker holds, in backlog order: GR-040
        /// being implemented (not in refinement), GR-058 and GR-059 in Next, GR-051 and GR-054 in Analysing,
        /// GR-073 and GR-074 in Backlog - six Work Items in refinement. The given Work Items carry the given
        /// tags. No stage, cadence or band has been chosen.
        /// </summary>
        protected int GravityRefinesSixWorkItems(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = Givens.ATeam("Team Gravity", GravitysStates, ThroughputHistoryDays, sleProbability: 85, sleRange: 7);

            TheTrackerHolds(gravity, tags,
            [
                new TrackerWorkItem(BillingExport, "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
                new TrackerWorkItem(UserActivityTracking, "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedSearch, "Advanced search filters", Next, StateCategories.Doing, "3", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedReporting, "Advanced reporting module", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 3),
                new TrackerWorkItem(ApiVersioning, "Public API versioning", Analysing, StateCategories.Doing, "5", StartedDaysAgo: 2),
                new TrackerWorkItem(ConfigurationManagement, "Configuration management", Backlog, StateCategories.ToDo, "6"),
                new TrackerWorkItem(LoadTesting, "Load testing framework", Backlog, StateCategories.ToDo, "7"),
            ]);

            Givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesInBacklogAnalysingAndNext);
            return gravity;
        }

        /// <summary>
        /// The Team finished this many Work Items on each of the last <paramref name="days"/> days before the tracker
        /// was read, that day included, each taking one day. A Team that finishes the same number every day makes the
        /// shipped forecast engine answer one number, whatever it draws.
        /// </summary>
        protected void TheTeamFinishedWorkEveryDay(int teamId, int perDay, int days = ThroughputHistoryDays)
        {
            using var scope = harness.NewScope();
            var owner = TheStoredTeam(scope, teamId);
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var daysAgo in Enumerable.Range(0, days))
            {
                foreach (var index in Enumerable.Range(0, perDay))
                {
                    var referenceId = $"DONE-{daysAgo}-{index}";
                    var closed = TrackerReadOn.Date.AddDays(-daysAgo).AddHours(12);

                    workItemRepository.Add(new WorkItem
                    {
                        Team = owner,
                        TeamId = owner.Id,
                        ReferenceId = referenceId,
                        Name = $"Finished {referenceId}",
                        Type = "User Story",
                        State = Done,
                        StateCategory = StateCategories.Done,
                        CreatedDate = closed.AddDays(-11),
                        StartedDate = closed,
                        ClosedDate = closed,
                        Order = "99",
                        Url = TrackerAddressOf(referenceId),
                    });
                }
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        /// <summary>A system admin has declared every day from the first to the last a blackout day for the whole instance.</summary>
        protected void BlackoutDaysFrom(DateOnly first, DateOnly last)
        {
            using var scope = harness.NewScope();
            scope.ServiceProvider.GetRequiredService<IBlackoutPeriodService>()
                .Create(new BlackoutPeriodDto { Start = first, End = last, Description = "Company offsite" })
                .GetAwaiter().GetResult();
        }

        /// <summary>A system admin has blacked out these weekdays every week from the first day on, up to the last when there is one.</summary>
        protected void AWeeklyBlackout(DayOfWeek[] weekdays, DateOnly first, DateOnly? last, string description)
        {
            using var scope = harness.NewScope();
            scope.ServiceProvider.GetRequiredService<IRecurringBlackoutRuleService>()
                .Create(new RecurringBlackoutRuleDto { Weekdays = [.. weekdays], IntervalWeeks = 1, Start = first, End = last, Description = description })
                .GetAwaiter().GetResult();
        }

        private void TheTrackerHolds(int teamId, (string WorkItem, string Tag)[] tags, List<TrackerWorkItem> workItems)
        {
            using var scope = harness.NewScope();
            var owner = TheStoredTeam(scope, teamId);
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var tracked in workItems)
            {
                workItemRepository.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = tracked.ReferenceId,
                    Name = tracked.Name,
                    Type = "User Story",
                    State = tracked.State,
                    StateCategory = tracked.Category,
                    CreatedDate = TrackerReadOn.AddDays(-60),
                    StartedDate = tracked.Category == StateCategories.Doing ? TrackerReadOn.Date.AddDays(-tracked.StartedDaysAgo).AddHours(12) : null,
                    Order = tracked.Rank,
                    Url = TrackerAddressOf(tracked.ReferenceId),
                    ParentReferenceId = string.Empty,
                    Tags = [.. tags.Where(tag => tag.WorkItem == tracked.ReferenceId).Select(tag => tag.Tag)],
                });
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        private static Team TheStoredTeam(IServiceScope scope, int teamId)
            => scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)
                ?? throw new InvalidOperationException($"no Team {teamId} is stored to give Work Items");

        private static string TrackerAddressOf(string referenceId) => $"https://tracker.example/browse/{referenceId}";

        private sealed record TrackerWorkItem(string ReferenceId, string Name, string State, StateCategories Category, string Rank, int StartedDaysAgo = 0);

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
