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
                .Returns(new RunChartData(RunChartDataGenerator.GenerateRunChartData(SixDaysWithWorkFinishedOnFour)));
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
                    || cell.ScoredPeriodStart != Today.AddDays(-cell.HorizonDays)
                    || cell.HistoryWindowEnd != cell.ScoredPeriodStart
                    || cell.HistoryWindowStart != cell.ScoredPeriodStart.AddDays(-cell.SamplingWindowDays))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Cells.Select(cell => (cell.SamplingWindowDays, cell.HorizonDays)),
                    Is.EquivalentTo(StandardWindowDays.SelectMany(window => HorizonDays.Select(horizon => (window, horizon)))));
                Assert.That(misplaced, Is.Empty);
            }
        }

        [Test]
        public void Each_cell_reads_the_history_of_its_own_window()
        {
            Run();

            foreach (var (window, horizon) in StandardWindowDays.SelectMany(window => HorizonDays.Select(horizon => (window, horizon))))
            {
                var historyEnd = AsDateTime(Today.AddDays(-horizon));
                teamMetricsService.Verify(
                    service => service.GetBlackoutAwareThroughputForTeam(team, historyEnd.AddDays(-window), historyEnd, Mode),
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
                    service => service.GetThroughputForTeam(team, AsDateTime(Today.AddDays(-horizon)), AsDateTime(Today), Mode),
                    Times.Once);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamMetricsService.Invocations.Count(invocation => invocation.Method.Name == nameof(ITeamMetricsService.GetThroughputForTeam)),
                    Is.EqualTo(HorizonDays.Length));
                Assert.That(result.Cells.Select(cell => cell.ActualCompleted), Is.All.EqualTo(ActualCompletedInEveryPeriod));
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
                Assert.That(cell.Sufficiency.DaysWithCompletedWork, Is.EqualTo(4));
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

        private RealityCheckResultDto Run()
        {
            var subject = new ForecastRealityCheckService(
                forecastService.Object,
                teamMetricsService.Object,
                blackoutPeriodService.Object,
                new FakeLighthouseClock(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));

            return subject.Run(team, Mode);
        }

        private static DateTime AsDateTime(DateOnly day) => day.ToDateTime(TimeOnly.MinValue);

        /// <summary>Ten trials each at 16, 14, 12 and 10 items, so the four levels read 16, 14, 12 and 10.</summary>
        private static HowManyForecast AForecastOfTenTwelveFourteenSixteen(int days)
            => new(new Dictionary<int, int> { [16] = 50, [14] = 20, [12] = 15, [10] = 15 }, days);
    }
}
