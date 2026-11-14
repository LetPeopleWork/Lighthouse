using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// The limits route, read through a web host: a team's limits stop on the last day it was watched,
    /// and a second look changes nothing. Which limits are filled, and how, is proven at unit level by
    /// ProcessBehaviorSnapshotWriterTest and RetentionEdgeTest.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public partial class Slice07WhereTheLimitsActuallyMovedTest
    {
        [Test]
        public async Task Limits_stop_where_the_team_stopped_being_watched_and_a_second_look_changes_nothing()
        {
            var lastObservedOn = TodayDay.AddDays(-60);
            var teamId = GivenATeamLastObservedOn(lastObservedOn);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), lastObservedOn);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();
            var afterTheFirstVisit = LimitsReportedFor(teamId, ProcessBehaviorMetricType.Throughput);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheLimitsStopOn(teamId, ProcessBehaviorMetricType.Throughput, lastObservedOn);
                ThenTheLimitsAreUnchangedSince(teamId, ProcessBehaviorMetricType.Throughput, afterTheFirstVisit);
            }
        }
    }
}
