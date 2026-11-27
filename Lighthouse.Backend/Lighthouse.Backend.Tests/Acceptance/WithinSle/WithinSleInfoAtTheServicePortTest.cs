using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Licensing.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.WithinSle
{
    /// <summary>
    /// The Within SLE widget answers, for the Work Items a Team or a Portfolio closed in the chosen dates, how many of
    /// them finished within the Service Level Expectation, how many were closed, and the target. A Work Item is within
    /// when its Cycle Time is at or below the SLE days. Fewer than five closed Work Items are too few to judge. Each
    /// scenario asks the read port of the widget, with a database of its own, no web host and no Premium licence.
    /// </summary>
    [TestFixture]
    public class WithinSleInfoAtTheServicePortTest
    {
        private ClosedWork given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new ClosedWork();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_widget_shows_the_share_within_the_SLE_with_its_counts_and_target_without_a_Premium_licence(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.ElevenClosedInNovemberEightOfThemWithinTenDays();
            given.TwoWithinTenDaysClosedInOctoberOneClosedInDecemberAndOneStillOpen();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheWidgetMeasures(widget, closed: 11, within: 8, sharePercent: 72.7, slePercentile: 70, sleDays: 10);
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void A_Work_Item_finished_in_exactly_the_SLE_days_is_within(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.FiveClosedInNovemberOfExactlyTenDaysEach();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheWidgetMeasures(widget, closed: 5, within: 5, sharePercent: 100, slePercentile: 70, sleDays: 10);
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void Fewer_than_five_closed_Work_Items_are_too_few_to_judge(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.FourClosedInNovemberOfThreeDaysEach();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheWidgetIsTooFewToJudge(widget, closed: 4);
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void Without_an_SLE_the_widget_says_none_is_set_and_judges_nothing(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 0, days: 0);
            given.ElevenClosedInNovemberEightOfThemWithinTenDays();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheWidgetHasNoSle(widget);
        }

        [TestCaseSource(nameof(ListsOfClosedWork))]
        public void The_share_within_the_SLE_follows_a_direct_count_of_the_Cycle_Times_whatever_their_order(ClosedMix mix)
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 85, days: mix.SleDays);
            given.ClosedInNovemberWithCycleTimes(mix.CycleTimesInDays);

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheWidgetCountsLikeALoop(widget, mix);
        }

        public enum OwnerKind
        {
            Team,
            Portfolio,
        }

        public sealed record ClosedMix(int Seed, int SleDays, IReadOnlyList<int> CycleTimesInDays)
        {
            public override string ToString() => $"seed {Seed}, SLE {SleDays} days, {CycleTimesInDays.Count} closed";
        }

        private static IEnumerable<ClosedMix> ListsOfClosedWork()
        {
            yield return new ClosedMix(0, 10, []);
            yield return new ClosedMix(4, 7, [3, 7, 8, 12]);
            yield return new ClosedMix(5, 7, [3, 7, 8, 12, 7]);
            yield return new ClosedMix(40, 30, [.. Enumerable.Range(1, 40).Select(index => (index * 7 % 45) + 1)]);

            foreach (var seed in new[] { 11, 23, 37, 41, 59, 73 })
            {
                var random = new Random(seed);
                var count = random.Next(5, 41);
                var sleDays = random.Next(1, 61);
                var cycleTimes = Enumerable.Range(0, count).Select(_ => random.Next(1, 61)).ToList();
                cycleTimes[random.Next(count)] = sleDays;
                yield return new ClosedMix(seed, sleDays, cycleTimes);
            }
        }

        /// <summary>
        /// A Team or a Portfolio on an instance where it is 5 December 2026, with no Premium licence, and the Work Items
        /// or Features it closed on given days, each measured from the day it started to the day it closed.
        /// </summary>
        private sealed class ClosedWork : IDisposable
        {
            private static readonly DateTime FirstOfNovember = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            private static readonly DateTime LastOfNovember = new(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);

            private readonly ServicePortHarness harness = new(Register);
            private OwnerKind kind;
            private int ownerId;
            private int itemsAdded;

            public void Dispose() => harness.Dispose();

            public void AnOwnerWithTheSle(OwnerKind ownerKind, int probability, int days)
            {
                kind = ownerKind;
                using var scope = harness.NewScope();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                };

                if (kind == OwnerKind.Team)
                {
                    var team = new Team
                    {
                        Name = "Team Orca",
                        WorkTrackingSystemConnection = connection,
                        DataRetrievalValue = "project = ORCA",
                        WorkItemTypes = ["User Story"],
                        ToDoStates = ["New"],
                        DoingStates = ["In Progress"],
                        DoneStates = ["Done"],
                        DoneItemsCutoffDays = 0,
                        ServiceLevelExpectationProbability = probability,
                        ServiceLevelExpectationRange = days,
                    };
                    var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                    teams.Add(team);
                    teams.Save().GetAwaiter().GetResult();
                    ownerId = team.Id;
                    return;
                }

                var portfolio = new Portfolio
                {
                    Name = "Portfolio Orca",
                    WorkTrackingSystemConnection = connection,
                    DataRetrievalValue = "project = ORCA",
                    ToDoStates = ["New"],
                    DoingStates = ["In Progress"],
                    DoneStates = ["Done"],
                    DoneItemsCutoffDays = 0,
                    ServiceLevelExpectationProbability = probability,
                    ServiceLevelExpectationRange = days,
                };
                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                portfolios.Add(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
                ownerId = portfolio.Id;
            }

            /// <summary>Cycle Times of 1, 2, 3, 4, 5, 6, 10 and 10 days are within a 10-day SLE; 11, 14 and 30 days are late.</summary>
            public void ElevenClosedInNovemberEightOfThemWithinTenDays()
                => ClosedInNovemberWithCycleTimes([1, 2, 3, 4, 5, 6, 10, 10, 11, 14, 30]);

            public void TwoWithinTenDaysClosedInOctoberOneClosedInDecemberAndOneStillOpen()
            {
                Add(Closed(new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc), cycleTimeInDays: 2));
                Add(Closed(new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc), cycleTimeInDays: 4));
                Add(Closed(new DateTime(2026, 12, 1, 9, 0, 0, DateTimeKind.Utc), cycleTimeInDays: 3));
                Add(StillOpen(new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc)));
            }

            public void FiveClosedInNovemberOfExactlyTenDaysEach() => ClosedInNovemberWithCycleTimes([10, 10, 10, 10, 10]);

            public void FourClosedInNovemberOfThreeDaysEach() => ClosedInNovemberWithCycleTimes([3, 3, 3, 3]);

            public void ClosedInNovemberWithCycleTimes(IReadOnlyList<int> cycleTimesInDays)
            {
                for (var index = 0; index < cycleTimesInDays.Count; index++)
                {
                    var closedOn = new DateTime(2026, 11, 2 + (index % 27), 9, 0, 0, DateTimeKind.Utc);
                    Add(Closed(closedOn, cycleTimesInDays[index]));
                }
            }

            public WithinSleInfoDto TheWidgetForNovember()
            {
                using var scope = harness.NewScope();
                var widget = scope.ServiceProvider.GetRequiredService<IWithinSleInfoQuery>();

                return kind == OwnerKind.Team
                    ? widget.GetForTeam(scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(ownerId)!, FirstOfNovember, LastOfNovember)
                    : widget.GetForPortfolio(scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(ownerId)!, FirstOfNovember, LastOfNovember);
            }

            public static void ThenTheWidgetMeasures(WithinSleInfoDto widget, int closed, int within, double sharePercent, int slePercentile, int sleDays)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(widget.Verdict.ToString(), Is.EqualTo("Measured"),
                        $"With {closed} Work Items closed and an SLE set, the widget judged '{widget.Verdict}'. Five or more closed Work Items are judged: " +
                        "answer the share within the SLE, not a refusal.");
                    Assert.That(widget.ClosedCount, Is.EqualTo(closed),
                        $"The widget counted {widget.ClosedCount} closed Work Items, expected {closed}. Count the Work Items closed inside the chosen dates, " +
                        "the same population Cycle Time Percentiles uses: not those closed before or after, not those still open.");
                    Assert.That(widget.WithinCount, Is.EqualTo(within),
                        $"The widget counted {widget.WithinCount} Work Items within the SLE, expected {within}. A Work Item is within when its Cycle Time " +
                        "is at or below the SLE days, so one that took exactly the SLE days is within.");
                    Assert.That(widget.SharePercent, Is.Not.Null,
                        "A judged widget carries the share, so the page shows it as the server counted it. Answer the share with the counts.");
                    Assert.That(Convert.ToDouble(widget.SharePercent), Is.EqualTo(sharePercent).Within(0.001),
                        $"The widget showed a share of {widget.SharePercent}, expected {sharePercent}: {within} of {closed}, to one decimal. " +
                        "The server owns the share and rounds it to one decimal.");
                    Assert.That(widget.SlePercentile, Is.EqualTo(slePercentile),
                        $"The widget named the target {widget.SlePercentile} %, expected {slePercentile} %. Answer the SLE's percentile as the target.");
                    Assert.That(widget.SleDays, Is.EqualTo(sleDays),
                        $"The widget named the SLE as {widget.SleDays} days, expected {sleDays}. Answer the SLE days the share is measured against.");
                }
            }

            public static void ThenTheWidgetIsTooFewToJudge(WithinSleInfoDto widget, int closed)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(widget.Verdict.ToString(), Is.EqualTo("TooFew"),
                        $"With {closed} closed Work Items the widget judged '{widget.Verdict}'. Fewer than five are too few to judge: answer TooFew and no share.");
                    Assert.That(widget.ClosedCount, Is.EqualTo(closed),
                        $"The widget counted {widget.ClosedCount} closed Work Items, expected {closed}. The reader is told how many closed, even when too few to judge.");
                    Assert.That(widget.SharePercent, Is.Null,
                        $"A share of {widget.SharePercent} was shown for {closed} closed Work Items. Show no share below five closed Work Items.");
                }
            }

            public static void ThenTheWidgetHasNoSle(WithinSleInfoDto widget)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(widget.Verdict.ToString(), Is.EqualTo("NoSle"),
                        $"With no SLE set the widget judged '{widget.Verdict}'. Without a target there is nothing to measure against: answer NoSle.");
                    Assert.That(widget.SharePercent, Is.Null,
                        $"A share of {widget.SharePercent} was shown with no SLE set. Show no share until a target exists.");
                }
            }

            public static void ThenTheWidgetCountsLikeALoop(WithinSleInfoDto widget, ClosedMix mix)
            {
                var closed = mix.CycleTimesInDays.Count;
                var within = mix.CycleTimesInDays.Count(days => days <= mix.SleDays);
                var population = $"({mix}; Cycle Times [{string.Join(", ", mix.CycleTimesInDays)}])";

                if (closed < 5)
                {
                    Assert.That(widget.Verdict.ToString(), Is.EqualTo("TooFew"),
                        $"With {closed} closed Work Items the widget judged '{widget.Verdict}' {population}. Fewer than five are too few to judge: answer TooFew.");
                    return;
                }

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(widget.Verdict.ToString(), Is.EqualTo("Measured"),
                        $"With {closed} closed Work Items the widget judged '{widget.Verdict}' {population}. Five or more are judged: answer Measured.");
                    Assert.That(widget.ClosedCount, Is.EqualTo(closed), $"The widget counted {widget.ClosedCount} closed, expected {closed} {population}.");
                    Assert.That(widget.WithinCount, Is.EqualTo(within),
                        $"The widget counted {widget.WithinCount} within the SLE, a direct count gives {within} {population}. Count Cycle Times at or below the SLE days.");
                    Assert.That(Convert.ToDouble(widget.SharePercent), Is.EqualTo(within * 100.0 / closed).Within(0.05),
                        $"The widget showed a share of {widget.SharePercent}, expected {within} of {closed} within one decimal {population}.");
                }
            }

            private void Add(Func<IServiceScope, Action> stage)
            {
                using var scope = harness.NewScope();
                stage(scope)();
            }

            private Func<IServiceScope, Action> Closed(DateTime closedOn, int cycleTimeInDays)
                => Entered(closedOn.AddDays(-(cycleTimeInDays - 1)), closedOn);

            private Func<IServiceScope, Action> StillOpen(DateTime startedOn) => Entered(startedOn, closedOn: null);

            private Func<IServiceScope, Action> Entered(DateTime startedOn, DateTime? closedOn)
            {
                var reference = $"ORCA-{++itemsAdded}";
                var isClosed = closedOn.HasValue;

                if (kind == OwnerKind.Team)
                {
                    return scope => () =>
                    {
                        var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(ownerId)!;
                        var items = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
                        items.Add(Dated(new WorkItem
                        {
                            Team = team,
                            TeamId = team.Id,
                            ReferenceId = reference,
                            Name = $"Story {reference}",
                            Type = "User Story",
                            Url = $"https://example.test/items/{reference}",
                            Order = reference,
                        }, startedOn, closedOn, isClosed));
                        items.Save().GetAwaiter().GetResult();
                    };
                }

                return scope => () =>
                {
                    var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(ownerId)!;
                    var feature = new Feature
                    {
                        ReferenceId = reference,
                        Name = $"Feature {reference}",
                        Type = "Epic",
                        Url = $"https://example.test/features/{reference}",
                        Order = reference,
                    };
                    feature.Portfolios.Add(portfolio);
                    var features = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
                    features.Add(Dated(feature, startedOn, closedOn, isClosed));
                    features.Save().GetAwaiter().GetResult();
                };
            }

            private static TItem Dated<TItem>(TItem item, DateTime startedOn, DateTime? closedOn, bool isClosed)
                where TItem : WorkItemBase
            {
                item.State = isClosed ? "Done" : "In Progress";
                item.StateCategory = isClosed ? StateCategories.Done : StateCategories.Doing;
                item.CreatedDate = startedOn.AddDays(-3);
                item.StartedDate = startedOn;
                item.ClosedDate = closedOn;
                return item;
            }

            private static void Register(IServiceCollection services)
            {
                services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero)));
                services.AddSingleton(TimeProvider.System);
                services.AddScoped(_ => NoPremiumLicence());
                services.AddScoped(_ => RefreshSettingsOfTeamsAndPortfolios());
                services.AddSingleton<Cache<string, object>>();
                services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

                services.AddScoped<IRepository<Team>, TeamRepository>();
                services.AddScoped<IPortfolioRepository, PortfolioRepository>();
                services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
                services.AddScoped<IFeatureRepository, FeatureRepository>();
                services.AddScoped<IFeatureOrdering, FeatureOrdering>();
                services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
                services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
                services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
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
                services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
                services.AddScoped<IWithinSleInfoQuery, WithinSleInfoQuery>();
            }

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
}
