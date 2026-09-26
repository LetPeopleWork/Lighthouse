using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    [TestFixture]
    public class ForecastRealityCheckServiceTest
    {
        private const ThroughputFilterMode Mode = ThroughputFilterMode.SkipFilter;

        private const int ActualCompletedInEveryPeriod = 12;

        private const int TeamSamplingWindowDays = 30;

        private static readonly DateOnly Today = new(2026, 9, 22);

        private static readonly int[] StandardWindowDays = [14, 30, 60, 90];

        private static readonly int[] HorizonDays = [7, 14, 28, 56];

        private static readonly int[] ConfidenceLevels = [50, 70, 85, 95];

        private static readonly int[] SevenDaysWithWorkFinishedOnFive = [1, 0, 2, 1, 0, 1, 3];

        private static readonly int[] SixDaysWithWorkFinishedOnFour = [1, 0, 2, 1, 0, 1];

        private static readonly int[] AllOfTheActualOnOneDay = [ActualCompletedInEveryPeriod];

        private static readonly BlackoutPeriod[] OneBlackoutDayInsideEveryHorizon =
        [
            new BlackoutPeriod { Start = Today.AddDays(-3), End = Today.AddDays(-3) },
        ];

        private static readonly (int, int)[] TheFourLevelsTheForecastReads = [(50, 16), (70, 14), (85, 12), (95, 10)];

        private static readonly (int, int, bool)[] OnlyTheTwoLevelsAtOrBelowTwelveHeld = [(50, 16, false), (70, 14, false), (85, 12, true), (95, 10, true)];

        private Mock<IForecastService> forecastService = null!;

        private Mock<ITeamMetricsService> teamMetricsService = null!;

        private Mock<IBlackoutPeriodService> blackoutPeriodService = null!;

        private Team team = null!;

        [SetUp]
        public void SetUp()
        {
            forecastService = new Mock<IForecastService>();
            forecastService
                .Setup(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns((RunChartData _, int days) => AForecastOfTenTwelveFourteenSixteen(days));

            teamMetricsService = new Mock<ITeamMetricsService>();
            teamMetricsService
                .Setup(service => service.GetBlackoutAwareThroughputForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new RunChartData(RunChartDataGenerator.GenerateRunChartData(SevenDaysWithWorkFinishedOnFive)));
            teamMetricsService
                .Setup(service => service.GetThroughputForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new RunChartData(RunChartDataGenerator.GenerateRunChartData(AllOfTheActualOnOneDay)));

            blackoutPeriodService = new Mock<IBlackoutPeriodService>();
            blackoutPeriodService
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(OneBlackoutDayInsideEveryHorizon);

            team = new Team { Id = 7, Name = "Ocean Explorer", ThroughputHistory = TeamSamplingWindowDays };
        }

        [Test]
        public void Every_cell_scores_the_period_ending_today_and_learns_from_the_window_right_before_it()
        {
            var result = Run();

            var misplaced = result.Cells
                .Where(cell => cell.ScoredPeriodEnd != Today
                    || cell.ScoredPeriodStart != Today.AddDays(-(cell.HorizonDays - 1))
                    || cell.HistoryWindowEnd != cell.ScoredPeriodStart.AddDays(-1)
                    || cell.HistoryWindowStart != cell.HistoryWindowEnd.AddDays(-(cell.SamplingWindowDays - 1)))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Cells.Select(cell => (cell.SamplingWindowDays, cell.HorizonDays)),
                    Is.EquivalentTo(EveryCellOf(StandardWindowDays)));
                Assert.That(misplaced, Is.Empty);
            }
        }

        [Test]
        public void Each_cell_reads_the_history_of_its_own_window()
        {
            Run();

            foreach (var (window, horizon) in EveryCellOf(StandardWindowDays))
            {
                var historyEnd = AsDateTime(Today.AddDays(-horizon));
                teamMetricsService.Verify(
                    service => service.GetBlackoutAwareThroughputForTeam(team, historyEnd.AddDays(-(window - 1)), historyEnd, Mode),
                    Times.Once);
            }
        }

        [Test]
        public void What_the_team_delivered_is_read_once_per_horizon_and_shared_by_every_window()
        {
            var result = Run();

            foreach (var horizon in HorizonDays)
            {
                teamMetricsService.Verify(
                    service => service.GetThroughputForTeam(team, AsDateTime(Today.AddDays(-(horizon - 1))), AsDateTime(Today), Mode),
                    Times.Once);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamMetricsService.Invocations.Count(invocation => invocation.Method.Name == nameof(ITeamMetricsService.GetThroughputForTeam)),
                    Is.EqualTo(HorizonDays.Length));
                Assert.That(result.Cells.Select(cell => cell.ActualCompleted), Is.All.EqualTo(ActualCompletedInEveryPeriod));
            }
        }

        [TestCase(14, new[] { 14, 30, 60, 90 })]
        [TestCase(30, new[] { 14, 30, 60, 90 })]
        [TestCase(45, new[] { 14, 30, 45, 60, 90 })]
        [TestCase(7, new[] { 7, 14, 30, 60, 90 })]
        [TestCase(120, new[] { 14, 30, 60, 90, 120 })]
        public void The_teams_own_window_is_always_one_of_those_checked_and_the_ladder_stays_in_order(int teamWindowDays, int[] expectedSampledWindowDays)
        {
            team.ThroughputHistory = teamWindowDays;

            var result = Run();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StandardWindowDays, Is.EqualTo(StandardWindowDays));
                Assert.That(result.SampledWindowDays, Is.EqualTo(expectedSampledWindowDays));
                Assert.That(result.Cells.Select(cell => (cell.SamplingWindowDays, cell.HorizonDays)),
                    Is.EquivalentTo(EveryCellOf(expectedSampledWindowDays)));
                Assert.That(result.Denominator.RunsAttempted, Is.EqualTo(expectedSampledWindowDays.Length * HorizonDays.Length));
                Assert.That(teamMetricsService.Invocations.Count(invocation => invocation.Method.Name == nameof(ITeamMetricsService.GetBlackoutAwareThroughputForTeam)),
                    Is.EqualTo(expectedSampledWindowDays.Length * HorizonDays.Length));
                Assert.That(teamMetricsService.Invocations.Count(invocation => invocation.Method.Name == nameof(ITeamMetricsService.GetThroughputForTeam)),
                    Is.EqualTo(HorizonDays.Length), "the actual depends only on the horizon");
                Assert.That(result.SoundWindow.SoundWindowDays, Is.EqualTo(expectedSampledWindowDays));
                Assert.That(result.SoundWindow.CurrentSettingDays, Is.EqualTo(teamWindowDays));
            }
        }

        [TestCase(true, 45)]
        [TestCase(true, 7)]
        [TestCase(true, 0)]
        [TestCase(false, 0)]
        [TestCase(false, -7)]
        public void A_team_without_a_rolling_window_of_its_own_adds_nothing_to_the_ladder(bool usesFixedDates, int storedWindowDays)
        {
            team.UseFixedDatesForThroughput = usesFixedDates;
            team.ThroughputHistory = storedWindowDays;

            var result = Run();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.SampledWindowDays, Is.EqualTo(StandardWindowDays));
                Assert.That(result.Denominator.RunsAttempted, Is.EqualTo(StandardWindowDays.Length * HorizonDays.Length));
                Assert.That(result.SoundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.NotTested));
                Assert.That(result.SoundWindow.CurrentSettingDays, Is.EqualTo(storedWindowDays));
            }
        }

        [Test]
        public void The_filter_status_is_never_read_by_the_sweep()
        {
            Run();

            teamMetricsService.Verify(
                service => service.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()),
                Times.Never);
        }

        [Test]
        public void Each_horizon_is_forecast_over_its_working_days()
        {
            Run();

            foreach (var horizon in HorizonDays)
            {
                forecastService.Verify(
                    service => service.HowMany(It.IsAny<RunChartData>(), horizon - OneBlackoutDayInsideEveryHorizon.Length),
                    Times.Exactly(StandardWindowDays.Length));
            }
        }

        /// <summary>
        /// The one-week horizon runs from six days ago to today. Its first day counts like its last, so a
        /// blackout six days ago takes a day away, and a blackout seven days ago - the day before it starts -
        /// takes nothing.
        /// </summary>
        [TestCase(null, 7)]
        [TestCase(-6, 6)]
        [TestCase(-7, 7)]
        public void The_one_week_horizon_is_forecast_over_its_seven_days_less_the_blackout_days_among_them(int? blackoutDaysFromToday, int forecastDays)
        {
            BlackoutPeriod[] blackouts = blackoutDaysFromToday is { } offset
                ? [new BlackoutPeriod { Start = Today.AddDays(offset), End = Today.AddDays(offset) }]
                : [];
            blackoutPeriodService
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(blackouts);

            Run();

            forecastService.Verify(
                service => service.HowMany(It.IsAny<RunChartData>(), forecastDays),
                Times.Exactly(StandardWindowDays.Length));
        }

        [Test]
        public void The_day_every_check_ends_on_is_the_instances_own_day_not_the_utc_one()
        {
            var lateEveningUtcThatIsAlreadyTomorrowInZurich = new DateTimeOffset(Today.ToDateTime(new TimeOnly(23, 30)), TimeSpan.Zero);
            var instanceDay = Today.AddDays(1);

            var result = Run(new FakeLighthouseClock(lateEveningUtcThatIsAlreadyTomorrowInZurich, TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich")));

            var misplaced = result.Cells
                .Where(cell => cell.ScoredPeriodEnd != instanceDay
                    || cell.ScoredPeriodStart != instanceDay.AddDays(-(cell.HorizonDays - 1))
                    || cell.HistoryWindowEnd != instanceDay.AddDays(-cell.HorizonDays))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.AnchorDate, Is.EqualTo(instanceDay));
                Assert.That(misplaced, Is.Empty);
                Assert.That(result.Cells, Is.Not.Empty);
            }

            teamMetricsService.Verify(
                service => service.GetThroughputForTeam(team, AsDateTime(instanceDay.AddDays(-6)), AsDateTime(instanceDay), Mode),
                Times.Once);
        }

        [Test]
        public void A_level_holds_exactly_when_the_team_delivered_at_least_its_forecast()
        {
            var result = Run();

            var cell = result.Cells[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cell.Forecast!.Select(level => (level.Probability, level.Value)),
                    Is.EqualTo(TheFourLevelsTheForecastReads));
                Assert.That(cell.LevelOutcomes!.Select(level => (level.ConfidenceLevel, level.ForecastValue, level.Held)),
                    Is.EqualTo(OnlyTheTwoLevelsAtOrBelowTwelveHeld));
                Assert.That(cell.Sufficiency, Is.EqualTo(new RealityCheckSufficiencyDto(true, SufficiencyReason.Sufficient, 5)));
            }
        }

        [Test]
        public void The_answer_echoes_what_it_checked_and_counts_what_it_evaluated()
        {
            var result = Run();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TeamId, Is.EqualTo(team.Id));
                Assert.That(result.TeamName, Is.EqualTo(team.Name));
                Assert.That(result.AnchorDate, Is.EqualTo(Today));
                Assert.That(result.StandardWindowDays, Is.EqualTo(StandardWindowDays));
                Assert.That(result.SampledWindowDays, Is.EqualTo(StandardWindowDays));
                Assert.That(result.SampledHorizonDays, Is.EqualTo(HorizonDays));
                Assert.That(result.ConfidenceLevels, Is.EqualTo(ConfidenceLevels));
                Assert.That(result.MinimumActiveDays, Is.EqualTo(ForecastDataSufficiencyPolicy.MinimumActiveDays));
                Assert.That(result.Denominator, Is.EqualTo(new RealityCheckDenominatorDto(16, 16, 4, 64)));
                Assert.That(result.SoundWindow.CurrentSettingDays, Is.EqualTo(TeamSamplingWindowDays));
                Assert.That(result.SoundWindow.CurrentSettingWasTested, Is.True);
                Assert.That(result.LevelCoverage.Select(line => line.ConfidenceLevel), Is.EqualTo(ConfidenceLevels));
            }
        }

        [Test]
        public void A_cell_whose_history_the_shipped_bar_refuses_is_never_forecast_and_counts_for_nothing()
        {
            teamMetricsService
                .Setup(service => service.GetBlackoutAwareThroughputForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new RunChartData(RunChartDataGenerator.GenerateRunChartData(SixDaysWithWorkFinishedOnFour)));

            var result = Run();

            using (Assert.EnterMultipleScope())
            {
                forecastService.Verify(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()), Times.Never);
                Assert.That(result.Cells, Has.Count.EqualTo(16), "a check that cannot run is still reported");
                Assert.That(result.Cells.Select(cell => cell.Sufficiency),
                    Is.All.EqualTo(new RealityCheckSufficiencyDto(false, SufficiencyReason.TooFewActiveDays, 4)));
                AssertNothingWasReadOffAnyCell(result);
            }
        }

        [Test]
        public void A_cell_whose_forecast_has_no_reading_is_named_degenerate_and_counts_for_nothing()
        {
            forecastService
                .Setup(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns((RunChartData _, int days) => new HowManyForecast([], days));

            var result = Run();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Cells, Has.Count.EqualTo(16), "a check that cannot run is still reported");
                Assert.That(result.Cells.Select(cell => cell.Sufficiency),
                    Is.All.EqualTo(new RealityCheckSufficiencyDto(false, SufficiencyReason.DegenerateForecast, 5)));
                AssertNothingWasReadOffAnyCell(result);
            }
        }

        private static void AssertNothingWasReadOffAnyCell(RealityCheckResultDto result)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Cells.Where(cell => cell.Forecast is not null || cell.ActualCompleted is not null
                    || cell.Outcome is not null || cell.LevelOutcomes is not null), Is.Empty);
                Assert.That(result.Denominator, Is.EqualTo(new RealityCheckDenominatorDto(16, 0, 4, 0)));
                Assert.That(result.LevelCoverage.Select(line => (line.HeldCount, line.ExpectedHeldCount, line.Reading)),
                    Is.All.EqualTo((0, 0.0, LevelReading.NotEvaluated)));
                Assert.That(result.SoundWindow.Determination, Is.EqualTo(Determination.NotEnoughEvidence));
            }
        }

        private RealityCheckResultDto Run()
            => Run(new FakeLighthouseClock(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));

        private RealityCheckResultDto Run(ILighthouseClock clock)
        {
            var subject = new ForecastRealityCheckService(
                forecastService.Object,
                teamMetricsService.Object,
                blackoutPeriodService.Object,
                clock);

            return subject.Run(team, Mode);
        }

        private static DateTime AsDateTime(DateOnly day) => day.ToDateTime(TimeOnly.MinValue);

        private static List<(int Window, int Horizon)> EveryCellOf(int[] windowDays)
            => [.. windowDays.SelectMany(window => HorizonDays.Select(horizon => (window, horizon)))];

        /// <summary>Ten trials each at 16, 14, 12 and 10 items, so the four levels read 16, 14, 12 and 10.</summary>
        private static HowManyForecast AForecastOfTenTwelveFourteenSixteen(int days)
            => new(new Dictionary<int, int> { [16] = 50, [14] = 20, [12] = 15, [10] = 15 }, days);
    }
}
