using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DISTILL acceptance scenarios for story 6053, slice 03 - where the process limits actually moved.
    /// A delivery lead opens Predictability - PBC Over Time and reads the upper limit, the average and
    /// the lower limit as three dated lines across the period under review, for every behaviour the
    /// toggle offers, rather than the three points the recorder happened to catch.
    ///
    /// Driving port: the shipped process-behavior-over-time read endpoint, team and portfolio routes.
    ///
    /// Step definitions live in Slice07WhereTheLimitsActuallyMovedSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-07")]
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
