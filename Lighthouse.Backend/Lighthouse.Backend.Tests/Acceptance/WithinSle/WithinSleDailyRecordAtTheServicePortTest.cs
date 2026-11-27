using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Application.Policies;
using Lighthouse.Backend.Modules.FlowMetrics.Domain;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.WithinSle
{
    /// <summary>
    /// The Within SLE daily record keeps, for each day and each trailing window of 30, 60 or 90 days, how many Work
    /// Items closed in that window, how many of them finished within the SLE, and the SLE that was in force that day.
    /// A day is recorded when the Team's or Portfolio's data is refreshed, and a past day nobody recorded can be filled
    /// in later against the SLE set at that time. The chart reads the record back day by day. Each scenario has a
    /// database of its own and no web host.
    /// </summary>
    [TestFixture]
    public class WithinSleDailyRecordAtTheServicePortTest
    {
        private static readonly DateOnly TwentiethOfNovember = new(2026, 11, 20);
        private static readonly DateOnly TwentyFirstOfNovember = new(2026, 11, 21);
        private static readonly DateOnly TwentySecondOfNovember = new(2026, 11, 22);
        private static readonly DateOnly TenthOfNovember = new(2026, 11, 10);

        private DailyRecord given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new DailyRecord();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void A_recorded_day_keeps_its_share_and_target_after_the_SLE_is_changed_and_only_later_days_use_the_new_SLE(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TheDataIsRefreshedOn(TwentiethOfNovember);
            given.TheSleIsChangedTo(probability: 85, days: 14);
            given.TheDataIsRefreshedOn(TwentyFirstOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentySecondOfNovember);

            DailyRecord.ThenTheDayWasRecorded(chart, TwentiethOfNovember, closed: 11, within: 8, sharePercent: 72.7, slePercentile: 70, sleDays: 10);
            DailyRecord.ThenTheDayWasRecorded(chart, TwentyFirstOfNovember, closed: 11, within: 10, sharePercent: 90.9, slePercentile: 85, sleDays: 14);
            DailyRecord.ThenNothingIsAnsweredFor(chart, TwentySecondOfNovember);
            DailyRecord.ThenTheCurrentSleIs(chart, slePercentile: 85, sleDays: 14);
        }

        [Test]
        public void Refreshing_again_on_the_same_day_replaces_that_day_with_the_SLE_in_force_at_the_last_refresh()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TheDataIsRefreshedOn(TwentiethOfNovember);
            given.TheSleIsChangedTo(probability: 85, days: 14);
            given.TheDataIsRefreshedOn(TwentiethOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayWasRecorded(chart, TwentiethOfNovember, closed: 11, within: 10, sharePercent: 90.9, slePercentile: 85, sleDays: 14);
        }

        [TestCase(30, 11)]
        [TestCase(60, 12)]
        [TestCase(90, 13)]
        public void Each_day_counts_the_Work_Items_closed_in_its_trailing_window_of_30_60_or_90_days(int horizon, int closedInTheWindow)
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.OneClosedFortyDaysAndOneClosedEightyDaysBefore(TwentiethOfNovember);
            given.TheDataIsRefreshedOn(TwentiethOfNovember);

            var chart = given.TheChartFor(horizon, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayCounts(chart, TwentiethOfNovember, horizon, closedInTheWindow);
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void A_day_with_fewer_than_five_closed_Work_Items_is_kept_with_its_count_and_draws_no_point(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.ThreeClosedInEarlyNovember();
            given.TheDataIsRefreshedOn(TwentiethOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayIsTooFewToDraw(chart, TwentiethOfNovember, closed: 3, slePercentile: 70, sleDays: 10);
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void Without_an_SLE_no_day_is_recorded_and_the_chart_is_told_no_SLE_is_set(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 0, days: 0);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TheDataIsRefreshedOn(TwentiethOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenNoSleIsSetAndNoDayIsAnswered(chart);
        }

        [Test]
        public void Only_the_days_inside_the_selected_range_are_answered()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TheDataIsRefreshedOn(TwentiethOfNovember);
            given.TheDataIsRefreshedOn(TwentyFirstOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TwentyFirstOfNovember, to: TwentyFirstOfNovember);

            DailyRecord.ThenOnlyTheseDaysAreAnswered(chart, TwentyFirstOfNovember);
        }

        [Test]
        public void A_range_before_anything_was_recorded_answers_no_day()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TheDataIsRefreshedOn(TwentyFirstOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TenthOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenOnlyTheseDaysAreAnswered(chart);
        }

        [Test]
        public void Filling_in_a_day_that_was_recorded_leaves_it_as_it_was_recorded()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TheDataIsRefreshedOn(TwentiethOfNovember);
            given.TheSleIsChangedTo(probability: 85, days: 14);
            given.TodayIs(TwentyFirstOfNovember);
            given.TheDayIsFilledInFromHistory(TwentiethOfNovember, cycleTimesInDays: [20, 20, 20, 20, 20, 20]);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayWasRecorded(chart, TwentiethOfNovember, closed: 11, within: 8, sharePercent: 72.7, slePercentile: 70, sleDays: 10);
        }

        [Test]
        public void A_past_day_nobody_recorded_is_filled_in_against_the_SLE_set_now_and_marked_as_filled_in()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 85, days: 14);
            given.TodayIs(TwentyFirstOfNovember);
            given.TheDayIsFilledInFromHistory(TwentiethOfNovember, cycleTimesInDays: [3, 9, 14, 15, 30]);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayWasFilledIn(chart, TwentiethOfNovember, closed: 5, within: 3, slePercentile: 85, sleDays: 14);
        }

        [Test]
        public void A_filled_in_day_with_nothing_closed_is_kept_with_a_count_of_zero_on_every_horizon()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 85, days: 14);
            given.TodayIs(TwentyFirstOfNovember);
            given.TheDayIsFilledInFromHistory(TwentiethOfNovember, cycleTimesInDays: []);

            var chart = given.TheChartFor(horizon: 90, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayWasFilledIn(chart, TwentiethOfNovember, closed: 0, within: 0, slePercentile: 85, sleDays: 14);
        }

        [Test]
        public void Today_filled_in_and_then_refreshed_is_a_recorded_day()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInEarlyNovemberEightOfThemWithinTenDays();
            given.TodayIs(TwentiethOfNovember);
            given.TheDayIsFilledInFromHistory(TwentiethOfNovember, cycleTimesInDays: [20, 20, 20, 20, 20, 20]);
            given.TheDataIsRefreshedOn(TwentiethOfNovember);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenTheDayWasRecorded(chart, TwentiethOfNovember, closed: 11, within: 8, sharePercent: 72.7, slePercentile: 70, sleDays: 10);
        }

        [Test]
        public void Without_an_SLE_filling_in_a_past_day_writes_nothing()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 0, days: 0);
            given.TodayIs(TwentyFirstOfNovember);
            given.TheDayIsFilledInFromHistory(TwentiethOfNovember, cycleTimesInDays: [3, 9, 14, 15, 30]);

            var chart = given.TheChartFor(horizon: 30, from: TwentiethOfNovember, to: TwentiethOfNovember);

            DailyRecord.ThenNoSleIsSetAndNoDayIsAnswered(chart);
        }

        [Test]
        public async Task Two_copies_of_the_application_filling_in_the_same_day_leave_one_day_with_the_first_copys_counts()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 85, days: 14);
            given.TodayIs(TwentyFirstOfNovember);

            var chart = await given.TwoCopiesFillInTheSameDay(TwentiethOfNovember, firstCopysCycleTimes: [3, 9, 14, 15, 30], secondCopysCycleTimes: [1, 1, 1, 1, 1, 1, 1]);

            DailyRecord.ThenTheDayWasFilledIn(chart, TwentiethOfNovember, closed: 5, within: 3, slePercentile: 85, sleDays: 14);
            given.ThenTheDatabaseHoldsOneRowPerHorizonFor(TwentiethOfNovember);
        }

        public enum OwnerKind
        {
            Team,
            Portfolio,
        }

        /// <summary>
        /// A Team or a Portfolio, the Work Items or Features it closed, an instance clock the scenario moves from day
        /// to day, and the daily record as the refresh, the fill-in and the chart reach it.
        /// </summary>
        private sealed class DailyRecord : IDisposable
        {
            private static readonly int[] EveryHorizon = [30, 60, 90];

            private readonly FakeLighthouseClock clock = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
            private readonly ServicePortHarness harness;
            private OwnerKind kind;
            private int ownerId;
            private int itemsAdded;
            private int currentSleProbability;
            private int currentSleDays;

            public DailyRecord() => harness = new ServicePortHarness(Register);

            public void Dispose() => harness.Dispose();

            private OwnerType OwnerType => kind == OwnerKind.Team ? OwnerType.Team : OwnerType.Portfolio;

            public void AnOwnerWithTheSle(OwnerKind ownerKind, int probability, int days)
            {
                kind = ownerKind;
                (currentSleProbability, currentSleDays) = (probability, days);
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

            /// <summary>
            /// Closed from the 2nd to the 12th of November with Cycle Times of 1, 2, 3, 4, 5, 6, 10, 10, 11, 14 and 30
            /// days: eight are within a 10-day SLE and ten within a 14-day SLE.
            /// </summary>
            public void ElevenClosedInEarlyNovemberEightOfThemWithinTenDays()
                => ClosedInEarlyNovemberWithCycleTimes([1, 2, 3, 4, 5, 6, 10, 10, 11, 14, 30]);

            public void ThreeClosedInEarlyNovember() => ClosedInEarlyNovemberWithCycleTimes([2, 3, 4]);

            public void OneClosedFortyDaysAndOneClosedEightyDaysBefore(DateOnly day)
            {
                Add(Closed(day.AddDays(-40).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc), cycleTimeInDays: 2));
                Add(Closed(day.AddDays(-80).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc), cycleTimeInDays: 2));
            }

            public void TodayIs(DateOnly day) => clock.SetInstant(new DateTimeOffset(day.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));

            public void TheDataIsRefreshedOn(DateOnly day)
            {
                TodayIs(day);
                using var scope = harness.NewScope();

                if (kind == OwnerKind.Team)
                {
                    foreach (var handler in scope.ServiceProvider.GetServices<IDomainEventHandler<TeamDataRefreshed>>())
                    {
                        handler.HandleAsync(new TeamDataRefreshed(ownerId), CancellationToken.None).GetAwaiter().GetResult();
                    }

                    return;
                }

                foreach (var handler in scope.ServiceProvider.GetServices<IDomainEventHandler<PortfolioFeaturesRefreshed>>())
                {
                    handler.HandleAsync(new PortfolioFeaturesRefreshed(ownerId), CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            public void TheSleIsChangedTo(int probability, int days)
            {
                (currentSleProbability, currentSleDays) = (probability, days);
                using var scope = harness.NewScope();

                if (kind == OwnerKind.Team)
                {
                    var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                    var team = teams.GetById(ownerId)!;
                    team.ServiceLevelExpectationProbability = probability;
                    team.ServiceLevelExpectationRange = days;
                    teams.Update(team);
                    teams.Save().GetAwaiter().GetResult();
                    return;
                }

                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                var portfolio = portfolios.GetById(ownerId)!;
                portfolio.ServiceLevelExpectationProbability = probability;
                portfolio.ServiceLevelExpectationRange = days;
                portfolios.Update(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
            }

            public void TheDayIsFilledInFromHistory(DateOnly day, IReadOnlyList<int> cycleTimesInDays)
            {
                using var scope = harness.NewScope();
                var writer = scope.ServiceProvider.GetRequiredService<IWithinSleSnapshotWriter>();
                writer.FillDayIfAbsent(ownerId, OwnerType, day, FamilyWith(cycleTimesInDays));
                writer.SaveFilledDay().GetAwaiter().GetResult();
            }

            public async Task<WithinSleOverTimeDto> TwoCopiesFillInTheSameDay(DateOnly day, IReadOnlyList<int> firstCopysCycleTimes, IReadOnlyList<int> secondCopysCycleTimes)
            {
                using (var firstCopy = harness.NewScope())
                using (var secondCopy = harness.NewScope())
                {
                    var firstWriter = firstCopy.ServiceProvider.GetRequiredService<IWithinSleSnapshotWriter>();
                    var secondWriter = secondCopy.ServiceProvider.GetRequiredService<IWithinSleSnapshotWriter>();
                    firstWriter.FillDayIfAbsent(ownerId, OwnerType, day, FamilyWith(firstCopysCycleTimes));
                    secondWriter.FillDayIfAbsent(ownerId, OwnerType, day, FamilyWith(secondCopysCycleTimes));

                    await firstWriter.SaveFilledDay();
                    await Assert.ThatAsync(secondWriter.SaveFilledDay, Throws.Nothing,
                        "The copy that lost the day was refused with an error. A day another copy has already stored is not a failure " +
                        "of the pass that lost it: drop the refused day and carry on.");
                }

                return TheChartFor(horizon: 30, from: day, to: day);
            }

            public WithinSleOverTimeDto TheChartFor(int horizon, DateOnly from, DateOnly to)
            {
                using var scope = harness.NewScope();

                return scope.ServiceProvider.GetRequiredService<IWithinSleOverTimeSeriesQuery>().GetSeries(ownerId, OwnerType, horizon, from, to);
            }

            public void ThenTheDatabaseHoldsOneRowPerHorizonFor(DateOnly day)
            {
                using var scope = harness.NewScope();
                var horizons = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().WithinSleSnapshots
                    .AsNoTracking()
                    .Where(row => row.OwnerId == ownerId && row.RecordedAt == day)
                    .Select(row => row.Horizon)
                    .OrderBy(horizon => horizon)
                    .ToList();

                Assert.That(horizons, Is.EqualTo(EveryHorizon),
                    $"After two copies filled in {day}, the record holds the horizons [{string.Join(", ", horizons)}] for that day. " +
                    "Two copies filling one day must leave exactly one day per horizon, not two stacked on the same date.");
            }

            public static void ThenTheDayWasRecorded(WithinSleOverTimeDto chart, DateOnly day, int closed, int within, double sharePercent, int slePercentile, int sleDays)
            {
                var answered = TheDay(chart, day);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answered.Origin.ToString(), Is.EqualTo("Recorded"),
                        $"{day} reads as '{answered.Origin}'. A day stored by a refresh on that day was recorded as it happened; " +
                        "a later fill-in must never replace it, and a refresh of today always marks it Recorded.");
                    Assert.That(answered.Verdict.ToString(), Is.EqualTo("Measured"),
                        $"{day} with {closed} closed Work Items and an SLE reads '{answered.Verdict}'. Five or more closed are judged: answer Measured so the chart draws a point.");
                    Assert.That((answered.ClosedCount, answered.WithinCount), Is.EqualTo((closed, within)),
                        $"{day} counts {answered.WithinCount} of {answered.ClosedCount} within the SLE, expected {within} of {closed}. " +
                        "A day keeps the counts it was measured with against the SLE in force that day.");
                    Assert.That(Convert.ToDouble(answered.SharePercent), Is.EqualTo(sharePercent).Within(0.001),
                        $"{day} shows a share of {answered.SharePercent}, expected {sharePercent}: {within} of {closed}, to one decimal.");
                    Assert.That((answered.SlePercentile, answered.SleDays), Is.EqualTo((slePercentile, sleDays)),
                        $"{day} is held to {answered.SlePercentile} % within {answered.SleDays} days, expected {slePercentile} % within {sleDays} days. " +
                        "Each day keeps the SLE that was in force when it was last recorded on that day; changing the SLE does not rewrite history.");
                }
            }

            public static void ThenTheDayWasFilledIn(WithinSleOverTimeDto chart, DateOnly day, int closed, int within, int slePercentile, int sleDays)
            {
                var answered = TheDay(chart, day);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answered.Origin.ToString(), Is.EqualTo("FilledIn"),
                        $"{day} reads as '{answered.Origin}'. A past day nobody recorded was worked out afterwards from the stored history; " +
                        "mark it filled in, so the chart can say it is measured against today's SLE.");
                    Assert.That((answered.ClosedCount, answered.WithinCount), Is.EqualTo((closed, within)),
                        $"{day} counts {answered.WithinCount} of {answered.ClosedCount} within the SLE, expected {within} of {closed}, " +
                        "counted against the SLE set when the day was filled in.");
                    Assert.That((answered.SlePercentile, answered.SleDays), Is.EqualTo((slePercentile, sleDays)),
                        $"{day} is held to {answered.SlePercentile} % within {answered.SleDays} days, expected the SLE set at fill time, " +
                        $"{slePercentile} % within {sleDays} days.");
                }
            }

            public static void ThenTheDayIsTooFewToDraw(WithinSleOverTimeDto chart, DateOnly day, int closed, int slePercentile, int sleDays)
            {
                var answered = TheDay(chart, day);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answered.Verdict.ToString(), Is.EqualTo("TooFew"),
                        $"{day} with {closed} closed Work Items reads '{answered.Verdict}'. Fewer than five are too few to judge: answer TooFew so the chart draws no point.");
                    Assert.That(answered.ClosedCount, Is.EqualTo(closed),
                        $"{day} counts {answered.ClosedCount} closed, expected {closed}. A day too few to judge is still kept with its count, so the chart can say why it drew no point.");
                    Assert.That(answered.SharePercent, Is.Null,
                        $"{day} shows a share of {answered.SharePercent} with {closed} closed Work Items. Answer no share below five closed, so no point is drawn.");
                    Assert.That((answered.SlePercentile, answered.SleDays), Is.EqualTo((slePercentile, sleDays)),
                        $"{day} is held to {answered.SlePercentile} % within {answered.SleDays} days, expected {slePercentile} % within {sleDays} days: " +
                        "the SLE in force that day is stored even when too few closed.");
                }
            }

            public static void ThenTheDayCounts(WithinSleOverTimeDto chart, DateOnly day, int horizon, int closed)
            {
                var answered = TheDay(chart, day);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answered.Horizon, Is.EqualTo(horizon),
                        $"The chart asked for the trailing {horizon} days answered a day of the trailing {answered.Horizon} days.");
                    Assert.That(answered.ClosedCount, Is.EqualTo(closed),
                        $"{day} on the trailing {horizon} days counts {answered.ClosedCount} closed Work Items, expected {closed}: " +
                        "count the Work Items closed in the window that ends on the day and reaches back that many days.");
                }
            }

            public static void ThenNothingIsAnsweredFor(WithinSleOverTimeDto chart, DateOnly day)
                => Assert.That(chart.Days.Select(answered => answered.RecordedAt), Does.Not.Contain(day),
                    $"The chart answered a day for {day}, which was never recorded or filled in. A day the stored history does not reach " +
                    "is left out, so the chart draws no point there, not a share of 0.");

            public static void ThenOnlyTheseDaysAreAnswered(WithinSleOverTimeDto chart, params DateOnly[] days)
                => Assert.That(chart.Days.Select(answered => answered.RecordedAt), Is.EquivalentTo(days),
                    $"The chart answered the days [{string.Join(", ", chart.Days.Select(answered => answered.RecordedAt))}], expected " +
                    $"[{string.Join(", ", days)}]. Answer only the stored days inside the selected range, both ends included.");

            public static void ThenTheCurrentSleIs(WithinSleOverTimeDto chart, int slePercentile, int sleDays)
                => Assert.That((chart.CurrentSlePercentile, chart.CurrentSleDays), Is.EqualTo(((int?)slePercentile, (int?)sleDays)),
                    $"The chart names the current SLE as {chart.CurrentSlePercentile} % within {chart.CurrentSleDays} days, expected " +
                    $"{slePercentile} % within {sleDays} days, the SLE set now.");

            public static void ThenNoSleIsSetAndNoDayIsAnswered(WithinSleOverTimeDto chart)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That((chart.CurrentSlePercentile, chart.CurrentSleDays), Is.EqualTo(((int?)null, (int?)null)),
                        $"With no SLE set the chart was told the SLE is {chart.CurrentSlePercentile} % within {chart.CurrentSleDays} days. " +
                        "Name no current SLE, so the chart says that none is set instead of drawing a line.");
                    Assert.That(chart.Days, Is.Empty,
                        $"With no SLE set the record holds {chart.Days.Count()} days. A day has nothing to be measured against without an SLE: write no row.");
                }
            }

            private static WithinSleDayDto TheDay(WithinSleOverTimeDto chart, DateOnly day)
            {
                var matching = chart.Days.Where(answered => answered.RecordedAt == day).ToList();
                Assert.That(matching, Has.Count.EqualTo(1),
                    $"The chart answered {matching.Count} entries for {day}, expected exactly one: the day as it is stored for the horizon asked for. " +
                    $"Days answered: [{string.Join(", ", chart.Days.Select(answered => answered.RecordedAt))}].");
                return matching[0];
            }

            /// <summary>The history of the day as a fill-in reads it, measured against the SLE set on the owner now.</summary>
            private WithinSleFamilyReader FamilyWith(IReadOnlyList<int> cycleTimesInDays)
                => new((_, _) => cycleTimesInDays, () => ServiceLevelExpectation.Of(currentSleProbability, currentSleDays));

            private void ClosedInEarlyNovemberWithCycleTimes(IReadOnlyList<int> cycleTimesInDays)
            {
                for (var index = 0; index < cycleTimesInDays.Count; index++)
                {
                    var closedOn = new DateTime(2026, 11, 2 + index, 9, 0, 0, DateTimeKind.Utc);
                    Add(Closed(closedOn, cycleTimesInDays[index]));
                }
            }

            private void Add(Func<IServiceScope, Action> stage)
            {
                using var scope = harness.NewScope();
                stage(scope)();
            }

            private Func<IServiceScope, Action> Closed(DateTime closedOn, int cycleTimeInDays)
            {
                var reference = $"ORCA-{++itemsAdded}";
                var startedOn = closedOn.AddDays(-(cycleTimeInDays - 1));

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
                        }, startedOn, closedOn));
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
                    features.Add(Dated(feature, startedOn, closedOn));
                    features.Save().GetAwaiter().GetResult();
                };
            }

            private static TItem Dated<TItem>(TItem item, DateTime startedOn, DateTime closedOn)
                where TItem : WorkItemBase
            {
                item.State = "Done";
                item.StateCategory = StateCategories.Done;
                item.CreatedDate = startedOn.AddDays(-3);
                item.StartedDate = startedOn;
                item.ClosedDate = closedOn;
                return item;
            }

            private void Register(IServiceCollection services)
            {
                services.AddSingleton<ILighthouseClock>(clock);
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

                services.AddScoped<IWithinSleSnapshotRepository, WithinSleSnapshotRepository>();
                services.AddScoped<IWithinSleSnapshotWriter, WithinSleSnapshotWriter>();
                services.AddScoped<IWithinSleOverTimeSeriesQuery, WithinSleOverTimeSeriesQuery>();
                services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, WithinSleRecordingHandler>();
                services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, WithinSleRecordingHandler>();
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
