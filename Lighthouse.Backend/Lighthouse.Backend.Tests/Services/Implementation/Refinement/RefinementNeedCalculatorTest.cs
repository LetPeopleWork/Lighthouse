using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class RefinementNeedCalculatorTest
    {
        private static readonly DateOnly Today = new(2026, 10, 2);

        private static readonly DateOnly NextThursday = new(2026, 10, 8);

        private static readonly DateTime TodayAtMidnight = new(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime NextThursdayAtMidnight = new(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);

        private static readonly RefinementCalendarFacts RefiningOnThursday = new(NextThursday, false, 6);

        // Five of a hundred runs pull 10, ten pull 8, twenty pull 6, fifteen pull 5 and the rest 3.
        private static readonly Dictionary<int, int> FiveToEight = new() { [10] = 5, [8] = 10, [6] = 20, [5] = 15, [3] = 50 };

        private Mock<IRefinementCalendar> calendarMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IForecastService> forecastServiceMock;
        private RunChartData throughput;
        private List<BlackoutPeriod> blackoutDays;
        private RefinementNeedCalculator subject;

        [SetUp]
        public void SetUp()
        {
            calendarMock = new Mock<IRefinementCalendar>();
            calendarMock.Setup(calendar => calendar.FactsFor(It.IsAny<RefinementCadence?>())).Returns(RefiningOnThursday);

            var clockMock = new Mock<ILighthouseClock>();
            clockMock.Setup(clock => clock.Today).Returns(Today);
            clockMock.Setup(clock => clock.TodayAsUtcMidnight).Returns(TodayAtMidnight);

            blackoutDays = [];
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(() => blackoutDays);

            throughput = new RunChartData();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            TheThroughputHistoryIsSufficient(true);

            forecastServiceMock = new Mock<IForecastService>();
            forecastServiceMock
                .Setup(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns((RunChartData _, int days) => new HowManyForecast(FiveToEight, days));

            subject = new RefinementNeedCalculator(
                calendarMock.Object,
                clockMock.Object,
                blackoutPeriodServiceMock.Object,
                teamMetricsServiceMock.Object,
                forecastServiceMock.Object);
        }

        [Test]
        public void The_range_is_the_How_Many_for_the_working_days_to_the_next_Refinement_read_at_the_default_band()
        {
            var outlook = subject.For(ATeam(), 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outlook.Need, Is.EqualTo(new RefinementNeed(RefinementVerdict.Below, null, new NeedRange(5, 8, 50, 85, 6))));
                Assert.That(outlook.Calendar, Is.EqualTo(RefiningOnThursday));
            }

            forecastServiceMock.Verify(service => service.HowMany(throughput, 6), Times.Once);
            blackoutPeriodServiceMock.Verify(service => service.GetEffectiveBlackoutDays(TodayAtMidnight, NextThursdayAtMidnight));
            teamMetricsServiceMock.Verify(service => service.GetForecastThroughputStatus(It.IsAny<Team>(), ThroughputFilterMode.RespectTeamSetting));
        }

        [Test]
        public void A_blackout_day_before_the_next_Refinement_is_not_a_working_day()
        {
            blackoutDays = [new BlackoutPeriod { Start = new DateOnly(2026, 10, 5), End = new DateOnly(2026, 10, 5) }];

            var outlook = subject.For(ATeam(), 2);

            Assert.That(outlook.Need.Range?.HorizonWorkingDays, Is.EqualTo(5));
            forecastServiceMock.Verify(service => service.HowMany(throughput, 5), Times.Once);
        }

        [Test]
        public void The_Teams_band_chooses_the_likelihoods_the_range_is_read_at()
        {
            var team = ATeam();
            team.RefinementSettings!.Band = new RefinementBand { LowPercentile = 30, HighPercentile = 95 };

            var outlook = subject.For(team, 11);

            Assert.That(outlook.Need, Is.EqualTo(new RefinementNeed(RefinementVerdict.Above, null, new NeedRange(3, 10, 30, 95, 6))));
        }

        [TestCase(4, RefinementVerdict.Below)]
        [TestCase(5, RefinementVerdict.In)]
        [TestCase(8, RefinementVerdict.In)]
        [TestCase(9, RefinementVerdict.Above)]
        public void The_ready_count_given_is_judged_against_the_range(int readyCount, RefinementVerdict expected)
        {
            Assert.That(subject.For(ATeam(), readyCount).Need.Verdict, Is.EqualTo(expected));
        }

        [Test]
        public void Without_a_next_Refinement_there_is_no_range_and_no_forecast_is_run()
        {
            calendarMock.Setup(calendar => calendar.FactsFor(It.IsAny<RefinementCadence?>())).Returns(RefinementCalendarFacts.None);

            var outlook = subject.For(ATeam(), 2);

            Assert.That(outlook.Need, Is.EqualTo(RefinementNeed.Unavailable(NeedUnavailableReason.NoCadence)));
            forecastServiceMock.Verify(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Too_little_Throughput_history_gives_no_range_and_no_forecast_is_run()
        {
            TheThroughputHistoryIsSufficient(false);

            var outlook = subject.For(ATeam(), 2);

            Assert.That(outlook.Need, Is.EqualTo(RefinementNeed.Unavailable(NeedUnavailableReason.InsufficientData)));
            forecastServiceMock.Verify(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()), Times.Never);
        }

        [TestCase(false, false, false, NeedUnavailableReason.NoRefinementStates)]
        [TestCase(false, false, true, NeedUnavailableReason.NoRefinementStates)]
        [TestCase(false, true, false, NeedUnavailableReason.NoRefinementStates)]
        [TestCase(false, true, true, NeedUnavailableReason.NoRefinementStates)]
        [TestCase(true, false, false, NeedUnavailableReason.NoCadence)]
        [TestCase(true, false, true, NeedUnavailableReason.NoCadence)]
        [TestCase(true, true, false, NeedUnavailableReason.InsufficientData)]
        [TestCase(true, true, true, null)]
        public void The_most_basic_missing_piece_names_why_there_is_no_range(
            bool hasRefinementStates, bool hasCadence, bool hasSufficientData, NeedUnavailableReason? expected)
        {
            var reason = RefinementNeedCalculator.UnavailableReasonFor(hasRefinementStates, hasCadence, () => hasSufficientData);

            Assert.That(reason, Is.EqualTo(expected));
        }

        [Test]
        public void The_Throughput_history_is_not_read_while_set_up_is_missing()
        {
            var asked = false;

            RefinementNeedCalculator.UnavailableReasonFor(true, false, () => asked = true);

            Assert.That(asked, Is.False);
        }

        [Test]
        public void A_Team_without_refinement_states_gives_no_range_and_its_history_is_never_read()
        {
            var team = ATeam();
            team.RefinementSettings!.States = [];

            var outlook = subject.For(team, 0);

            Assert.That(outlook.Need, Is.EqualTo(RefinementNeed.Unavailable(NeedUnavailableReason.NoRefinementStates)));
            teamMetricsServiceMock.Verify(service => service.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()), Times.Never);
            forecastServiceMock.Verify(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void The_calendar_is_asked_about_the_Teams_own_cadence()
        {
            var team = ATeam();
            var cadence = RefinementCadence.Of([DayOfWeek.Thursday], 1, null);
            team.RefinementSettings!.Cadence = cadence;

            subject.For(team, 2);

            calendarMock.Verify(calendar => calendar.FactsFor(cadence), Times.Once);
        }

        private void TheThroughputHistoryIsSufficient(bool sufficient)
        {
            teamMetricsServiceMock
                .Setup(service => service.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(throughput, false, null, sufficient));
        }

        private static Team ATeam() => new()
        {
            Id = 7,
            RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
        };
    }
}
