using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models
{
    // Story #5587 (ADR-113) DDD-8. Feature.TeamsWithoutForecast gains a SECOND clause: a contributing
    // pair (FeatureWork with remaining work) that has no Forecasts row at all. Reachable because
    // WorkItemService calls Feature.AddOrUpdateWorkForTeam during work-item sync, which is not a
    // forecast run - so a team newly added to an already-forecast feature has exactly this shape and
    // today's Forecasts-only iteration cannot see it.
    //
    // Ratified 2026-07-29: fix it at SOURCE. That deliberately MOVES THE FEATURE SURFACE too - Team and
    // Portfolio feature grids read "Cannot forecast" for such a feature until the next forecast run.
    // Transient, self-healing, and a latent ADR-112 fix: ADR-112's premise is "a team that must still
    // finish and has no honest distribution makes the feature un-forecastable", and no row at all is
    // strictly worse than zero trials. The delivery-local containment alternative was declined.
    //
    // The existing zero-trial clause and the completed-feature exemption are covered by
    // FeatureUnknownForecastTest and are not duplicated here.
    public class FeatureMissingForecastRowTest
    {
        private static readonly string[] TheNewlySyncedTeam = ["Beta"];

        private static readonly DateOnly Today = new(2026, 7, 29);
        private static readonly DateTime TargetDate = new(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void TeamsWithoutForecast_ContributingPairHasNoForecastRow_NamesThatTeam()
        {
            var subject = FeatureWithNewlySyncedTeam();

            Assert.That(subject.TeamsWithoutForecast.Select(team => team.Name), Is.EqualTo(TheNewlySyncedTeam));
        }

        [Test]
        public void CanBeForecast_ContributingPairHasNoForecastRow_IsFalse()
        {
            var subject = FeatureWithNewlySyncedTeam();

            Assert.That(subject.CanBeForecast, Is.False);
        }

        [Test]
        public void GetLikelhoodForDate_ContributingPairHasNoForecastRow_IsUnknownRatherThanAlphasNumberAlone()
        {
            // The sharp edge at feature grain: today the feature answers with Alpha's distribution as
            // though Beta's work were already done. That silent certainty is the exact defect this
            // story removes one grain up.
            var subject = FeatureWithNewlySyncedTeam();

            Assert.That(subject.GetLikelhoodForDate(TargetDate, Today, []), Is.Null);
        }

        [Test]
        public void TeamsWithoutForecast_PairWithNoRemainingWorkAndNoForecastRow_IsNotNamed()
        {
            // The exemption keys off REMAINING WORK, not off the emptiness or absence of a forecast.
            // Alpha finished and was dropped from the simulation; Beta is still working and has a row.
            // Green today and after - it pins the predicate the new clause must use.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var subject = new Feature([(alpha, 0, 5), (beta, 3, 5)]);
            subject.SetFeatureForecasts([ForecastFor(beta, new Dictionary<int, int> { { 10, 10000 } })]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.TeamsWithoutForecast, Is.Empty);
                Assert.That(subject.CanBeForecast, Is.True);
            }
        }

        [Test]
        public void TeamsWithoutForecast_ContributingPairWithNoForecastRowAndNoTeamNavigation_IsNotNamed()
        {
            // A pair loaded without its Team navigation cannot be named, so the new clause must drop it
            // exactly as the existing zero-trial clause drops unnameable teams - otherwise the
            // cannot-forecast tooltip renders a dangling "no throughput history for ". The delivery
            // still refuses to guess: Delivery.CalculateMetrics guard 4 catches this shape at pair grain
            // (see DeliveryJointForecastTest), which is precisely why that backstop is retained.
            var alpha = TeamNamed(1, "Alpha");

            var subject = new Feature([(alpha, 3, 5)]);
            subject.FeatureWork.Add(new FeatureWork { TeamId = 99, RemainingWorkItems = 3, TotalWorkItems = 3 });
            subject.SetFeatureForecasts([ForecastFor(alpha, new Dictionary<int, int> { { 10, 10000 } })]);

            Assert.That(subject.TeamsWithoutForecast, Is.Empty);
        }

        [Test]
        public void TeamsWithoutForecast_EveryContributingPairHasARow_StaysEmpty()
        {
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var subject = new Feature([(alpha, 3, 5), (beta, 4, 6)]);
            subject.SetFeatureForecasts([
                ForecastFor(alpha, new Dictionary<int, int> { { 10, 10000 } }),
                ForecastFor(beta, new Dictionary<int, int> { { 12, 10000 } }),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.TeamsWithoutForecast, Is.Empty);
                Assert.That(subject.CanBeForecast, Is.True);
            }
        }

        private static Feature FeatureWithNewlySyncedTeam()
        {
            var alpha = TeamNamed(1, "Alpha");
            var newlySynced = TeamNamed(2, "Beta");

            var feature = new Feature([(alpha, 3, 5), (newlySynced, 3, 3)]);
            feature.SetFeatureForecasts([ForecastFor(alpha, new Dictionary<int, int> { { 10, 10000 } })]);

            return feature;
        }

        private static Team TeamNamed(int id, string name)
        {
            return new Team { Id = id, Name = name };
        }

        private static WhenForecast ForecastFor(Team team, Dictionary<int, int> histogram)
        {
            return new WhenForecast(histogram)
            {
                Team = team,
                TeamId = team.Id,
                HasSufficientData = true,
            };
        }
    }
}
