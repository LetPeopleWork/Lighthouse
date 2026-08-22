using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    // Story #5587 (ADR-113), slice-01. Delivery.CalculateMetrics keeps the guards - delivery policy -
    // and delegates the combination to DeliveryCompletionForecast. GetGoverningFeature is deleted, so
    // both the headline likelihood and the 70/85/95 chips come off the joint histogram (D7).
    //
    // Guard order, per DESIGN: (1) no features -> 0 %; (2) any feature that cannot be forecast ->
    // unknown, teams named, BEFORE the maths (ADR-112 D8 / D2); (3) no remaining work -> 100 % with the
    // day-0 marker; (4) a contributing pair with no forecast row -> unknown (backstop); (5) the joint.
    // Guards 1 and 2 are DISJOINT predicates, so their relative order is unobservable - there is
    // deliberately no order-sensitivity test here, because it could never fail (DDD-6).
    public class DeliveryJointForecastTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 29, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

        private const int TargetDay = 10;
        private const int TailDay = 20;

        // Hoisted rather than inline: CA1861 fires on constant arrays inside assertions, and the Sonar
        // gate is zero new issues of any severity (docs/ci-learnings.md).
        private static readonly double?[] ExpectedMarginalRowLikelihoods = [72, 95];

        // Cumulative probability at TargetDay: .90 / .80 / .95 - the three-way fixture from DISCUSS.
        private static Dictionary<int, int> NinetyPercentByTargetDay => new() { { TargetDay, 9000 }, { TailDay, 1000 } };

        private static Dictionary<int, int> EightyPercentByTargetDay => new() { { TargetDay, 8000 }, { TailDay, 2000 } };

        private static Dictionary<int, int> NinetyFivePercentByTargetDay => new() { { TargetDay, 9500 }, { TailDay, 500 } };

        [Test]
        public void CalculateMetrics_DeliveryWithoutFeatures_ReportsZeroPercentAndNoDates()
        {
            // AC-01.11 / guard 1. Unchanged behaviour - but for a NARROWER reason: today the same
            // return is keyed on "no governing feature", which also swallows the all-un-forecastable
            // case. Splitting that condition is what DDD-6 fixes.
            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay));

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.Zero);
                Assert.That(metrics.WhenDistribution, Is.Empty);
                Assert.That(metrics.FeatureBreakdown, Is.Empty);
            }
        }

        [Test]
        public void CalculateMetrics_EveryFeatureCannotBeForecast_ReportsUnknownRatherThanZeroPercent()
        {
            // DDD-6, a visible delta. GetGoverningFeature filters likelihood >= 0 and `null >= 0` is
            // false in C#, so today a delivery in which EVERY feature is un-forecastable falls into the
            // "no governing feature" branch and reports 0 % - a direct contradiction of ADR-112 D8.
            // Deleting the selector splits that one condition in two and the case falls through to
            // guard 2, where it belongs.
            var delivery = DeliveryOn(
                Clock.TodayAsUtcMidnight.AddDays(TargetDay),
                UnforecastableFeature(1, "Meridian"),
                UnforecastableFeature(2, "Pulsar"));

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.Null);
                Assert.That(metrics.WhenDistribution, Is.Empty);
            }
        }

        [Test]
        public void CalculateMetrics_OneFeatureCannotBeForecast_ReportsUnknownAndNoDates()
        {
            // AC-01.10 / D2 / D8. The ADR-112 short-circuit runs before the joint computation and stays
            // exactly as it is. No forecastable-subset number is produced.
            var delivery = DeliveryOn(
                Clock.TodayAsUtcMidnight.AddDays(TargetDay),
                FeatureFor([new Row(TeamNamed(1, "Alpha"), 3, NinetyPercentByTargetDay)]),
                UnforecastableFeature(2, "Meridian"));

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.Null);
                Assert.That(metrics.WhenDistribution, Is.Empty);
            }
        }

        [Test]
        public void CalculateMetrics_EveryFeatureWasAlreadyFinishedAtTheLastForecastRun_ReportsHundredPercentForToday()
        {
            // Guard 3. A finished feature carries ForecastService's whole-feature {0: 0} day-0 marker,
            // so today's path and the joint path already agree here. The guard exists so the rollup
            // never reaches ForecastBase.GetLikelihood's trialCounter == 0 branch (ADO Bug #5586): a
            // 100 % that is MEANT is returned by an explicit rule, never fallen into.
            var team = TeamNamed(1, "Alpha");
            var finished = new Feature([(team, 0, 5)]);
            finished.SetFeatureForecasts([new WhenForecast(new Dictionary<int, int> { { 0, 0 } })]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay), finished);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.EqualTo(100));
                Assert.That(
                    metrics.WhenDistribution.Select(percentile => percentile.ExpectedDate),
                    Has.All.EqualTo(Clock.TodayAsUtcMidnight));
            }
        }

        [Test]
        public void CalculateMetrics_DeliveryFinishedBetweenForecastRuns_MovesEveryPercentileDateToToday()
        {
            // DDD-9, a visible delta the earlier design draft denied. When the work finished BETWEEN
            // forecast runs the persisted rows still carry their full trials, so today the delivery
            // reports 100 % against FUTURE percentile dates - the Feature.GetLikelhoodForDate
            // short-circuit fires but nothing touches the histogram. Guard 3 moves them to today.
            var team = TeamNamed(1, "Alpha");
            var finished = new Feature([(team, 0, 5)]);
            finished.SetFeatureForecasts([
                new WhenForecast(new Dictionary<int, int> { { 50, 10000 } }) { Team = team, TeamId = team.Id, HasSufficientData = true },
            ]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay), finished);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.EqualTo(100));
                Assert.That(
                    metrics.WhenDistribution.Select(percentile => percentile.ExpectedDate),
                    Has.All.EqualTo(Clock.TodayAsUtcMidnight));
            }
        }

        [Test]
        public void CalculateMetrics_TwoFeaturesOnSeparateTeams_HeadlineAndPercentileDatesComeFromTheJointHistogram()
        {
            // AC-01.1 and AC-01.9 in one fixture: two independent teams each 90 % likely by the delivery
            // date. Today the governing feature answers 90 % with an 85th percentile on the target day.
            // The joint is .90 x .90 = .81, whose 85th percentile falls past the target day onto the
            // tail - so the badge drops AND the chips move outward, which is the single most
            // under-communicated consequence of the change.
            var checkout = FeatureFor([new Row(TeamNamed(1, "Alpha"), 3, NinetyPercentByTargetDay)]);
            var reporting = FeatureFor([new Row(TeamNamed(2, "Beta"), 3, NinetyPercentByTargetDay)]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay), checkout, reporting);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.EqualTo(81).Within(0.001));
                Assert.That(DateAt(metrics, 70), Is.EqualTo(Clock.TodayAsUtcMidnight.AddDays(TargetDay)));
                Assert.That(DateAt(metrics, 85), Is.EqualTo(Clock.TodayAsUtcMidnight.AddDays(TailDay)));
                Assert.That(DateAt(metrics, 95), Is.EqualTo(Clock.TodayAsUtcMidnight.AddDays(TailDay)));
            }
        }

        [Test]
        public void CalculateMetrics_ThreeWayFixture_HeadlineIsSeventyTwoAndEqualsTheGoverningBreakdownRow()
        {
            // GRAIN ANCHOR, not a discriminator of old vs new - it passes today and must pass after.
            // On this fixture the correct joint (.720) COINCIDES with today's governing-feature answer,
            // because Checkout governs entirely and Reporting carries slack; that coincidence is exactly
            // the AC-01.4 equality corner, which is why the copy in slice 03 must not promise the header
            // is always lower than every row. What the test still buys: it fails loudly if DELIVER lands
            // a wrong GRAIN, since multiplying feature CDFs gives 68.4 and taking each team's term off
            // feature.Forecast gives 51.84. The old-vs-new discrimination lives in the test above.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 3, NinetyPercentByTargetDay), new Row(beta, 3, EightyPercentByTargetDay)]);
            var reporting = FeatureFor([new Row(beta, 4, NinetyFivePercentByTargetDay)]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay), checkout, reporting);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.EqualTo(72).Within(0.001));
                // The exact pair, not just ">= 72": rows that had gone joint too would all read 72 and
                // an at-least assertion would still pass. The rows are marginals and stay marginals.
                Assert.That(metrics.FeatureBreakdown.Select(row => row.Likelihood), Is.EqualTo(ExpectedMarginalRowLikelihoods).Within(0.001));
            }
        }

        [Test]
        public void CalculateMetrics_ContributingPairHasNoForecastRow_ReportsUnknownRatherThanASilentCertainty()
        {
            // C1 / DDD-7 through guard 2. A team added to an already-forecast feature by work-item sync
            // has remaining work and no Forecasts row. Today TeamsWithoutForecast only iterates
            // Forecasts, so it cannot see the pair, CanBeForecast stays true, and the delivery reports a
            // number that quietly assumes Beta's work is already done.
            var alpha = TeamNamed(1, "Alpha");
            var newlySynced = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 3, NinetyPercentByTargetDay), new Row(newlySynced, 3, null)]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay), checkout);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.Null);
                Assert.That(metrics.WhenDistribution, Is.Empty);
            }
        }

        [Test]
        public void CalculateMetrics_ContributingPairHasNoForecastRowAndNoTeamNavigation_StillReportsUnknown()
        {
            // Guard 4, and the reason it is retained even though DDD-8 makes guard 2 cover C1. A pair
            // loaded without its Team navigation cannot be NAMED - DDD-8's new clause has to drop it,
            // exactly as the existing zero-trial clause drops unnameable teams, or the tooltip renders a
            // dangling "no throughput history for ". Guard 4 is the one place that re-derives the
            // predicate from the row set the maths actually consumes, so the two cannot drift apart.
            // (Whether the FRONTEND degrades gracefully on an empty team list is slice 03's AC.)
            var alpha = TeamNamed(1, "Alpha");

            var checkout = FeatureFor([new Row(alpha, 3, NinetyPercentByTargetDay)]);
            checkout.FeatureWork.Add(new FeatureWork { TeamId = 99, RemainingWorkItems = 3, TotalWorkItems = 3 });

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(TargetDay), checkout);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.Null);
                Assert.That(metrics.WhenDistribution, Is.Empty);
            }
        }

        [Test]
        public void CalculateMetrics_DeliveryWithoutADate_KeepsReportingHundredPercentAndPublishesTheJointDates()
        {
            // DESIGN deferred question 1, decided in DISTILL: MIRROR the guard, do not invent a third
            // behaviour. Today the delivery inherits Feature.GetLikelhoodForDate's `date != default`
            // short-circuit through the governing-feature call and reports 100 %; after the change it
            // would silently flip to 0 %, because CountWorkingDays against DateTime.MinValue is
            // negative. Reachable only through the EF parameterless constructor, and preserving it costs
            // one condition. The DATES still come off the joint histogram, exactly as on every other
            // path - which is the half of this test that is red today.
            var checkout = FeatureFor([new Row(TeamNamed(1, "Alpha"), 3, NinetyPercentByTargetDay)]);
            var reporting = FeatureFor([new Row(TeamNamed(2, "Beta"), 3, NinetyPercentByTargetDay)]);

            var delivery = DeliveryOn(default, checkout, reporting);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Date, Is.Default);
                Assert.That(metrics.LikelihoodPercentage, Is.EqualTo(100));
                Assert.That(DateAt(metrics, 85), Is.EqualTo(Clock.TodayAsUtcMidnight.AddDays(TailDay)));
            }
        }

        [Test]
        public void CalculateMetrics_LateAndEarlyFeatureOnSeparateTeams_PercentileDatesAreNeverEarlierThanTheLatestFeature()
        {
            // AC-01.9 - the ADO #5435 regression, re-asserted DIRECTLY rather than through the deleted
            // tie-break. The delivery CDF is pointwise <= every feature's CDF, so a delivery date
            // earlier than an individual feature's becomes structurally impossible; the selection step
            // that used to need a tie-break is gone. Green today, and it must stay green.
            var late = FeatureFor([new Row(TeamNamed(1, "Alpha"), 5, new Dictionary<int, int> { { 50, 100 } })]);
            var early = FeatureFor([new Row(TeamNamed(2, "Beta"), 5, new Dictionary<int, int> { { 5, 100 } })]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(200), late, early);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 70, 85, 95);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    metrics.WhenDistribution.Select(percentile => percentile.ExpectedDate),
                    Has.All.EqualTo(Clock.TodayAsUtcMidnight.AddDays(50)));
            }
        }

        [Test]
        public void CalculateMetrics_OverdueDelivery_ReportsZeroPercentAndStillSaysWhenItWillLand()
        {
            // A likelihood of exactly 0 % is the single most important thing Lighthouse has to say, and
            // an empty when-distribution is what DeliveryMetricSnapshotRecordingHandler reads as "no
            // forecast" - so the deliveries most in trouble would be the ones that silently stopped
            // reporting. Green today; the joint rollup must not regress it.
            var checkout = FeatureFor([new Row(TeamNamed(1, "Alpha"), 5, new Dictionary<int, int> { { 50, 100 } })]);

            var delivery = DeliveryOn(Clock.TodayAsUtcMidnight.AddDays(-5), checkout);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 85);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(metrics.LikelihoodPercentage, Is.Zero);
                Assert.That(metrics.WhenDistribution, Is.Not.Empty);
                Assert.That(DateAt(metrics, 85), Is.EqualTo(Clock.TodayAsUtcMidnight.AddDays(50)));
            }
        }

        private static DateTime DateAt(DeliveryMetricsProjection metrics, int percentile)
        {
            return metrics.WhenDistribution.Single(entry => entry.Percentile == percentile).ExpectedDate;
        }

        private static Delivery DeliveryOn(DateTime date, params Feature[] features)
        {
            var delivery = new Delivery { Id = 1, Name = "Q3 Launch", Date = date };

            delivery.ReplaceFeatures(features);

            return delivery;
        }

        private static Team TeamNamed(int id, string name)
        {
            return new Team { Id = id, Name = name };
        }

        private static Feature UnforecastableFeature(int teamId, string teamName)
        {
            var team = TeamNamed(teamId, teamName);
            var feature = new Feature([(team, 3, 3)]);
            feature.SetFeatureForecasts([new WhenForecast([]) { Team = team, TeamId = team.Id, HasSufficientData = true }]);

            return feature;
        }

        private static Feature FeatureFor(List<Row> rows)
        {
            var feature = new Feature(rows.Select(row => (row.Team, row.Remaining, row.Remaining + 3)));

            feature.SetFeatureForecasts(rows
                .Where(row => row.Histogram is not null)
                .Select(row => new WhenForecast(row.Histogram!)
                {
                    Team = row.Team,
                    TeamId = row.Team.Id,
                    NumberOfItems = row.Remaining,
                    HasSufficientData = true,
                }));

            return feature;
        }

        private sealed record Row(Team Team, int Remaining, Dictionary<int, int>? Histogram);
    }
}
