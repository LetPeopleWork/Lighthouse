using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    [TestFixture]
    public class RealityCheckPortAnswerTest
    {
        private const int OneDayMoreThanTheThirtyDayWindow = 31;

        [Test]
        public void An_answer_whose_check_learned_from_more_than_its_own_window_is_not_read()
        {
            var forecasts = new RealityCheckForecastScript();
            GivenACheckThatLearnedFrom(forecasts, OneDayMoreThanTheThirtyDayWindow);

            var failure = Assert.Throws<AssertionException>(() => RealityCheckPortAnswer.ReadFrom(AnAnswer(), forecasts));

            Assert.That(failure!.Message, Does.Contain($"{OneDayMoreThanTheThirtyDayWindow} days"));
        }

        private static void GivenACheckThatLearnedFrom(RealityCheckForecastScript forecasts, int days)
        {
            var history = new RunChartData(Enumerable.Range(0, days).ToDictionary(day => day, _ => new List<WorkItemBase>()));

            forecasts.Over(Mock.Of<IForecastService>(MockBehavior.Strict)).HowMany(history, 7);
        }

        private static RealityCheckResultDto AnAnswer() => new(
            1,
            "Ocean Explorer",
            new DateOnly(2026, 9, 22),
            [],
            [],
            [],
            [],
            5,
            new RealityCheckDenominatorDto(0, 0, 0, 0),
            new RealityCheckSoundWindowDto([], [], Determination.AllWindowsAlike, 30, true, CurrentSettingStanding.Inside, null),
            [],
            [],
            []);
    }
}
