using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Bug #5567 - T1 at the unit level. The four entity anchors are driven from a
    /// <see cref="FakeLighthouseClock"/> whose instant and zone are set independently, because the
    /// two disagree for roughly two hours every night and that window is where the bug lives.
    ///
    /// Deliberately NOT recomputing the production expression: root cause D of this bug is a test
    /// suite that re-derived <c>DateTime.UtcNow.Date</c> and therefore passed for every possible
    /// value of "today", including a wrong one. Every expectation below is a literal date.
    /// </summary>
    [TestFixture]
    public class InstanceDayAnchorEntityTest
    {
        /// <summary>22:30 in Zurich on 2026-07-27 is already 2026-07-28 there, and still 2026-07-27 in UTC.</summary>
        private static readonly DateTimeOffset LateEveningInZurich = new(2026, 7, 27, 23, 30, 0, TimeSpan.Zero);

        private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        [Test]
        public void GetThroughputSettings_AtAZoneBoundary_EndsOnTheInstanceDay()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var team = new Team { ThroughputHistory = 30 };

            var throughputSettings = team.GetThroughputSettings(clock.Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DateOnly.FromDateTime(throughputSettings.EndDate), Is.EqualTo(new DateOnly(2026, 7, 28)));
                Assert.That(DateOnly.FromDateTime(throughputSettings.StartDate), Is.EqualTo(new DateOnly(2026, 6, 29)));
                Assert.That(throughputSettings.EndDate.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(throughputSettings.StartDate.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
        }

        [Test]
        public void GetLikelhoodForDate_CountsWorkingDaysFromTheInstanceDay()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var team = ContributingTeam();
            var feature = new Feature(team, 10);

            // 40% of the simulations finish in 9 days, the rest need 10.
            feature.Forecasts.Add(ForecastFrom(team, new Dictionary<int, int> { { 9, 40 }, { 10, 60 } }));

            // 2026-08-06 is nine days after the Zurich day (2026-07-28) but ten after the UTC day,
            // so the anchor alone decides which side of the distribution the target lands on.
            var target = new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc);

            var fromInstanceDay = feature.GetLikelhoodForDate(target, clock.Today, []);

            clock.SetZone(TimeZoneInfo.Utc);
            var fromUtcDay = feature.GetLikelhoodForDate(target, clock.Today, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromInstanceDay, Is.EqualTo(40.0));
                Assert.That(fromUtcDay, Is.EqualTo(100.0));
            }
        }

        [Test]
        public void ToWhenPercentile_ProjectsFromTheInstanceDay()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var delivery = new Delivery("Zone boundary", TestToday.AFutureDate, 1, TestToday.Ambient);
            var team = ContributingTeam();
            var feature = new Feature(team, 10);
            feature.Forecasts.Add(ForecastFrom(team, new Dictionary<int, int> { { 10, 100 } }));
            delivery.Features.Add(feature);

            var metrics = delivery.CalculateMetrics(clock.Today, [], 85);

            Assert.That(
                DateOnly.FromDateTime(metrics.WhenDistribution.Single().ExpectedDate),
                Is.EqualTo(new DateOnly(2026, 8, 7)));
        }

        [Test]
        public void WorkItemAge_AtAZoneBoundary_MovesBothEndsAndKeepsTheInclusivePlusOne()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var workItem = new WorkItem
            {
                StateCategory = StateCategories.Doing,
                StartedDate = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc),
            };

            var ageInZurich = workItem.WorkItemAge(clock.Zone, clock.Today);

            clock.SetZone(TimeZoneInfo.Utc);
            var ageInUtc = workItem.WorkItemAge(clock.Zone, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                // 24th..28th inclusive - both ends are calendar days, the +1 is unchanged.
                Assert.That(ageInZurich, Is.EqualTo(5));

                // The same instant read as a UTC instance is still the 27th, so one day younger.
                Assert.That(ageInUtc, Is.EqualTo(4));
            }
        }

        private static Team ContributingTeam()
        {
            return new Team { Id = 1, Name = "Contributing" };
        }

        // The row names its team: since Story #5587 a contributing pair whose row does not name it has
        // no honest distribution, so the feature reports "cannot forecast" rather than a number.
        private static WhenForecast ForecastFrom(Team team, Dictionary<int, int> simulationResult)
        {
            var forecast = new WhenForecast { TeamId = team.Id };
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, [simulationResult]);
            return forecast;
        }
    }
}
