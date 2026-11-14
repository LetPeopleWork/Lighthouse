using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Once the chart has filled in, every tab of the Percentiles Over Time widget covers the same period,
    /// read through the shipped team percentiles-over-time endpoint.
    ///
    /// Step definitions live in Slice06EveryPercentileTabSpansTheSamePeriodSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public partial class Slice06EveryPercentileTabSpansTheSamePeriodTest
    {
        /// <summary>
        /// The comparison the combined widget exists for - is age climbing while finished cycle time
        /// looks steady - only works when both tabs cover the same period.
        /// </summary>
        [Test]
        public async Task Every_tab_covers_the_same_period_once_the_chart_has_filled_in()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            GivenAnItemStillInFlightSince(teamId, "in-flight-throughout", TodayDay.AddDays(-150));

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 30, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 60, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 90, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenEveryTabCoversTheSamePeriod(teamId, TodayDay.AddDays(-30), TodayDay);
        }
    }
}
