using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
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
using NUnit.Framework;

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
        protected const string Backlog = RefinementPortGivens.Backlog;

        protected const string Implementation = RefinementPortGivens.Implementation;

        protected const string Done = RefinementPortGivens.Done;

        protected const int ThroughputHistoryDays = RefinementPortGivens.ThroughputHistoryDays;

        protected const string UserActivityTracking = RefinementPortGivens.UserActivityTracking;

        protected const string AdvancedSearch = RefinementPortGivens.AdvancedSearch;

        private ServicePortHarness harness = null!;

        private FakeLighthouseClock clock = null!;

        private ScriptedForecasts forecasts = null!;

        protected RefinementPortGivens Givens { get; private set; } = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            clock = new FakeLighthouseClock(TestToday.FrozenNow);
            forecasts = new ScriptedForecasts(clock);
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

        /// <summary>What the votes on this tab are cast against: the source as the browser reads its text, and both numbers.</summary>
        protected static YardstickReading YardstickIn(RefinementViewDto tab)
            => new(tab.Yardstick.Source.ToString(), tab.Yardstick.Days, tab.Yardstick.Probability);

        protected sealed record YardstickReading(string? Source, int? Days, int? Probability);

        /// <summary>The need on this tab as the browser reads its text: the verdict or why there is none, the range, its likelihoods and its working days.</summary>
        protected static NeedFacts TheNeedOn(RefinementViewDto tab)
            => new(
                tab.Need.Verdict?.ToString(),
                tab.Need.UnavailableReason?.ToString(),
                tab.Need.Low,
                tab.Need.High,
                tab.Need.LowPercentile,
                tab.Need.HighPercentile,
                tab.Need.HorizonWorkingDays);

        protected sealed record NeedFacts(
            string? Verdict,
            string? UnavailableReason,
            int? Low,
            int? High,
            int? LowPercentile,
            int? HighPercentile,
            int? HorizonWorkingDays);

        /// <summary>A reader on an instance without sign-in: the name they typed, and the key their browser made up and sends with every vote.</summary>
        protected sealed record PortVoter(string Name, string BrowserKey);

        /// <summary>A browser that has not voted before, so it has a key no other browser has.</summary>
        protected static PortVoter ABrowserOf(string name) => new(name, Guid.NewGuid().ToString("N"));

        /// <summary>
        /// The reader has cast this vote from their browser, with the comment if one is given. The voter is
        /// established as the votes route establishes one, so the tab later recognises the vote as theirs.
        /// </summary>
        protected void HasVoted(PortVoter voter, int teamId, string workItem, SizingAnswer answer, string? comment = null)
            => AssertRecorded(TriesToVote(voter, teamId, workItem, answer, comment), $"{voter.Name}'s {answer} on {workItem}");

        /// <summary>The reader has asked or said this on the Work Item, from their browser, as the comments route records it.</summary>
        protected void HasCommented(PortVoter voter, int teamId, string workItem, string comment)
            => AssertRecorded(TriesToComment(voter, teamId, workItem, comment), $"{voter.Name}'s comment on {workItem}");

        /// <summary>The reader casts this vote, and the outcome is returned as it is, recorded or refused.</summary>
        protected SizingOutcome TriesToVote(PortVoter voter, int teamId, string workItem, SizingAnswer answer, string? comment = null)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<ISizingLogCommands>()
                .Vote(teamId, WorkItemRouteReference.From(workItem), new SizingVote(answer, SizingChannel.Web, comment), EstablishedVoter(scope, voter));
        }

        /// <summary>The reader sends this comment, and the outcome is returned as it is, recorded or refused.</summary>
        protected SizingOutcome TriesToComment(PortVoter voter, int teamId, string workItem, string comment)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<ISizingLogCommands>()
                .Comment(teamId, WorkItemRouteReference.From(workItem), new SizingComment(comment, SizingChannel.Web), EstablishedVoter(scope, voter));
        }

        /// <summary>The tab as the reader's browser reads it: the view the controller wraps for the wire, with the reader's own votes marked.</summary>
        protected RefinementViewDto TheTabAsSeenBy(PortVoter reader, int teamId)
        {
            using var scope = harness.NewScope();
            var view = scope.ServiceProvider.GetRequiredService<IRefinementViewQuery>().ForTeam(teamId, reader.BrowserKey)
                ?? throw new InvalidOperationException($"no Team {teamId} is stored to open the tab of");
            return new RefinementViewDto(view);
        }

        /// <summary>The log of the Work Item as the reader's browser reads it, built as the log route builds it.</summary>
        protected SizingLogDto TheLogAsSeenBy(PortVoter reader, int teamId, string workItem)
        {
            using var scope = harness.NewScope();
            var log = scope.ServiceProvider.GetRequiredService<IRefinementViewQuery>().LogOf(teamId, WorkItemRouteReference.From(workItem), reader.BrowserKey)
                ?? throw new InvalidOperationException($"{workItem} has no log on Team {teamId}: no such Team, or it is not in refinement");
            return new SizingLogDto(log);
        }

        private static Voter EstablishedVoter(IServiceScope scope, PortVoter voter)
        {
            var resolution = scope.ServiceProvider.GetRequiredService<VoterIdentityResolver>()
                .ForWriteAsync(voter.Name, voter.BrowserKey, () => Task.FromResult<UserProfile?>(null))
                .GetAwaiter().GetResult();
            return resolution.Voter
                ?? throw new InvalidOperationException($"{voter.Name} could not vote: {resolution.Refusal}");
        }

        private static void AssertRecorded(SizingOutcome outcome, string what)
            => Assert.That(outcome, Is.EqualTo(SizingOutcome.Recorded).Or.EqualTo(SizingOutcome.RecordedAndMadeReady), $"{what} was not recorded");

        /// <summary>One step of a scenario, for a Given that goes through a real service.</summary>
        protected IServiceScope NewScope() => harness.NewScope();

        /// <inheritdoc cref="RefinementPortGivens.GravityRefinesSixWorkItems"/>
        protected int GravityRefinesSixWorkItems(params (string WorkItem, string Tag)[] tags) => Givens.GravityRefinesSixWorkItems(tags);

        /// <inheritdoc cref="RefinementPortGivens.TheTeamFinishedWorkEveryDay"/>
        protected void TheTeamFinishedWorkEveryDay(int teamId, int perDay, int days = ThroughputHistoryDays)
            => Givens.TheTeamFinishedWorkEveryDay(teamId, perDay, days);

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

        /// <summary>What every scenario of this base adds to the harness, read without building or resolving any of it.</summary>
        internal static IServiceCollection WhatItRegisters
        {
            get
            {
                var services = new ServiceCollection();
                var clock = new FakeLighthouseClock(TestToday.FrozenNow);
                Register(services, clock, new ScriptedForecasts(clock));
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
            services.AddScoped<ISizingLogCommands, SizingLogCommands>();

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
        private sealed class ScriptedForecasts(ILighthouseClock clock)
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

                return new HowManyForecast(runs, days, clock.Now.UtcDateTime);
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
