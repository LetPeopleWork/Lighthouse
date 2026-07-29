using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    // Story #5587 (ADR-113), slice-01. The composing builder: contributing (team, feature) pairs ->
    // bucket by team -> Min within a bucket -> carrier -> AggregatedWhenForecast (product across
    // buckets). Deliberately constructible without a Delivery graph, so the grain traps are pinned on a
    // pure target rather than through an EF entity.
    //
    // The three-way fixture below is the kill shot: it produces THREE distinct values from one input,
    // so it separates the correct row grain (0.720) from multiplying feature CDFs (0.684) and from
    // taking the team term off feature.Forecast (0.518). A fixture that cannot separate all three is
    // not coverage - constant-throughput fixtures in particular are point masses whose product IS the
    // number the wrong implementations return.
    [Ignore("RED until Story #5587 slice-01 implements DeliveryCompletionForecast")]
    public class DeliveryCompletionForecastTest
    {
        private const int TargetDay = 10;
        private const int TailDay = 20;

        // Cumulative probability at TargetDay: Alpha/Checkout .90, Beta/Checkout .80, Beta/Reporting .95.
        private static Dictionary<int, int> AlphaOnCheckout => new() { { TargetDay, 9000 }, { TailDay, 1000 } };

        private static Dictionary<int, int> BetaOnCheckout => new() { { TargetDay, 8000 }, { TailDay, 2000 } };

        private static Dictionary<int, int> BetaOnReporting => new() { { TargetDay, 9500 }, { TailDay, 500 } };

        [Test]
        public void ContributingRows_TeamWorksOnlyOneOfTwoFeatures_ProducesThreeRowsNotFour()
        {
            // AC-01.6 / D10. AddOrUpdateWorkForTeam and RemoveTeamFromFeature make the pair set
            // genuinely sparse; a cartesian product of the delivery's teams x features would inject a
            // degenerate empty CDF for Alpha on Reporting.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 3, AlphaOnCheckout), new Row(beta, 3, BetaOnCheckout)]);
            var reporting = FeatureFor([new Row(beta, 4, BetaOnReporting)]);

            var rows = DeliveryCompletionForecast.ContributingRows([checkout, reporting]);

            Assert.That(rows, Has.Count.EqualTo(3));
        }

        [Test]
        public void ContributingRows_NoFeatures_IsEmpty()
        {
            var rows = DeliveryCompletionForecast.ContributingRows([]);

            Assert.That(rows, Is.Empty);
        }

        [Test]
        public void Build_ThreeWayFixture_IsTheProductOfPerTeamMinimaNotOfFeatureDistributions()
        {
            // AC-01.1 / AC-01.2 / AC-01.3. bucket(Alpha) = min(.90) = .90; bucket(Beta) =
            // min(.80, .95) = .80; delivery = .90 x .80 = .720. Multiplying the two features' own CDFs
            // gives .684 (Beta double-penalised); taking each team's term off feature.Forecast gives
            // .518 (Beta folded into Alpha's term, then multiplied again). Asserting 72 % excludes both.
            var forecast = BuildThreeWayFixture();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetLikelihood(TargetDay), Is.EqualTo(72).Within(0.001));
                Assert.That(HistogramOf(forecast), Is.EqualTo(new Dictionary<int, int> { { TargetDay, 7200 }, { TailDay, 2800 } }));
            }
        }

        [Test]
        public void Build_ThreeWayFixture_IsNeverAboveAnyFeaturesOwnProbabilityAndIsAllowedToEqualOne()
        {
            // AC-01.4. The invariant is delivery <= every breakdown row, with EQUALITY PERMITTED - here
            // the delivery equals Checkout's own row (.720) because Checkout governs entirely and
            // Reporting carries slack. A test that asserts strict inequality is wrong, not strict.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 3, AlphaOnCheckout), new Row(beta, 3, BetaOnCheckout)]);
            var reporting = FeatureFor([new Row(beta, 4, BetaOnReporting)]);

            var forecast = DeliveryCompletionForecast.Build([checkout, reporting])!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetLikelihood(TargetDay), Is.LessThanOrEqualTo(checkout.Forecast.GetLikelihood(TargetDay)));
                Assert.That(forecast.GetLikelihood(TargetDay), Is.LessThanOrEqualTo(reporting.Forecast.GetLikelihood(TargetDay)));
                Assert.That(forecast.GetLikelihood(TargetDay), Is.EqualTo(checkout.Forecast.GetLikelihood(TargetDay)).Within(0.001), "equality is legitimate");
            }
        }

        [Test]
        public void Build_OneFeatureSharedByTwoTeams_IsBitIdenticalToThatFeaturesOwnForecast()
        {
            // AC-01.5 / D11. The SHARED-feature version is the required fixture: the single-team version
            // is trivially true and proves nothing. Both buckets hold one row, each Min short-circuits
            // verbatim, and the aggregate is then literally the call feature.Forecast already makes -
            // which is what forces reuse of JointCompletionDistribution rather than a parallel product.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var shared = new Feature([(alpha, 3, 6), (beta, 4, 8)]);
            shared.SetFeatureForecasts([
                new WhenForecast(new Dictionary<int, int> { { 10, 9000 }, { 20, 1000 } })
                {
                    Team = alpha,
                    TeamId = alpha.Id,
                    NumberOfItems = 3,
                    CreationTime = new DateTime(2026, 7, 20, 6, 0, 0, DateTimeKind.Utc),
                    FilterApplied = false,
                    ExcludedSummary = null,
                    HasSufficientData = true,
                },
                new WhenForecast(new Dictionary<int, int> { { 12, 8000 }, { 25, 2000 } })
                {
                    Team = beta,
                    TeamId = beta.Id,
                    NumberOfItems = 4,
                    CreationTime = new DateTime(2026, 7, 27, 6, 0, 0, DateTimeKind.Utc),
                    FilterApplied = true,
                    ExcludedSummary = "2 outliers excluded",
                    HasSufficientData = false,
                },
            ]);

            var expected = shared.Forecast;

            var forecast = DeliveryCompletionForecast.Build([shared])!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HistogramOf(forecast), Is.EqualTo(HistogramOf(expected)), "histogram");
                Assert.That(forecast.TotalTrials, Is.EqualTo(expected.TotalTrials), "total trials");
                Assert.That(forecast.GetLikelihood(TargetDay), Is.EqualTo(expected.GetLikelihood(TargetDay)), "likelihood");
                Assert.That(forecast.GetProbability(70), Is.EqualTo(expected.GetProbability(70)), "70th percentile day");
                Assert.That(forecast.GetProbability(85), Is.EqualTo(expected.GetProbability(85)), "85th percentile day");
                Assert.That(forecast.GetProbability(95), Is.EqualTo(expected.GetProbability(95)), "95th percentile day");
                Assert.That(forecast.NumberOfItems, Is.EqualTo(expected.NumberOfItems), "number of items");
                Assert.That(forecast.CreationTime, Is.EqualTo(expected.CreationTime), "oldest contributor wins");
                Assert.That(forecast.FilterApplied, Is.EqualTo(expected.FilterApplied), "filter applied");
                Assert.That(forecast.ExcludedSummary, Is.EqualTo(expected.ExcludedSummary), "excluded summary");
                Assert.That(forecast.HasSufficientData, Is.EqualTo(expected.HasSufficientData), "sufficiency");
            }
        }

        [Test]
        public void Build_ContributingPairWithNoForecastRow_ReportsNoForecast()
        {
            // C1 / DDD-7, and the reason the enumeration runs FROM FeatureWork rather than from
            // Forecasts. WorkItemService calls AddOrUpdateWorkForTeam during work-item sync, which is
            // not a forecast run, so a team newly added to an already-forecast feature has exactly this
            // shape. Driving from Forecasts would emit no row for it, land it in no bucket, and let it
            // contribute a silent CDF of 1 - this feature's own defect, one grain lower.
            var alpha = TeamNamed(1, "Alpha");
            var newlySynced = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 3, AlphaOnCheckout), new Row(newlySynced, 3, null)]);

            var forecast = DeliveryCompletionForecast.Build([checkout]);

            Assert.That(forecast, Is.Null);
        }

        [Test]
        public void ContributingRows_PairWhoseWorkFinishedSinceTheLastForecastRun_IsNotEnumerated()
        {
            // AC-01.7, the COMMON stale shape: Forecasts is EF-persisted and lags FeatureWork, so a pair
            // whose work finished after the last run keeps its full 10 000 trials. It is dropped on the
            // remaining-work predicate, not on the emptiness of its forecast.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 0, AlphaOnCheckout), new Row(beta, 3, BetaOnCheckout)]);

            var rows = DeliveryCompletionForecast.ContributingRows([checkout]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows[0].TeamId, Is.EqualTo(beta.Id));
            }
        }

        [Test]
        public void ContributingRows_PairWhoseWorkIsFinishedAndWhoseRowIsAbsent_IsNotEnumeratedAndIsNotACannotForecast()
        {
            // AC-01.7, the normal shape: InitializeSimulationResults filters RemainingWorkItems > 0, so
            // a finished pair has no row at all. That is NOT the C1 shape - the exemption keys off
            // remaining work, so it must resolve to certainty rather than to "cannot forecast".
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 0, null), new Row(beta, 3, BetaOnCheckout)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DeliveryCompletionForecast.ContributingRows([checkout]), Has.Count.EqualTo(1));
                Assert.That(DeliveryCompletionForecast.Build([checkout]), Is.Not.Null);
            }
        }

        [Test]
        public void Build_TeamWhoseOnlyPairIsFinished_ContributesCertaintyRatherThanCannotForecast()
        {
            // AC-01.8. Alpha's only pair is done, so its bucket is absent from the bucket set - which IS
            // bucket(Alpha) = 1, because 1 is the identity of the cross-bucket product. The delivery
            // must therefore read exactly Beta's own distribution: not "cannot forecast", and not a
            // degenerate empty CDF that would drag the product to zero.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 0, AlphaOnCheckout), new Row(beta, 3, BetaOnCheckout)]);

            var forecast = DeliveryCompletionForecast.Build([checkout]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast, Is.Not.Null);
                Assert.That(forecast!.GetLikelihood(TargetDay), Is.EqualTo(80).Within(0.001));
                Assert.That(HistogramOf(forecast), Is.EqualTo(BetaOnCheckout));
            }
        }

        [Test]
        public void ContributingRows_WholeFeatureDayZeroSentinel_IsNeverEnumeratedAsARow()
        {
            // AC-01.8, second half. ForecastService's no-rows sentinel is built with the parameterless
            // SimulationResult ctor, so Team AND TeamId are null. Enumerating FROM FeatureWork makes a
            // null-keyed bucket structurally unrepresentable: the sentinel matches no pair.
            var beta = TeamNamed(2, "Beta");

            var checkout = new Feature([(beta, 3, 6)]);
            checkout.SetFeatureForecasts([
                new WhenForecast(BetaOnCheckout) { Team = beta, TeamId = beta.Id, HasSufficientData = true },
                new WhenForecast(new Dictionary<int, int> { { 0, 0 } }),
            ]);

            var rows = DeliveryCompletionForecast.ContributingRows([checkout]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows[0].TeamId, Is.EqualTo(beta.Id));
            }
        }

        private static AggregatedWhenForecast BuildThreeWayFixture()
        {
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = FeatureFor([new Row(alpha, 3, AlphaOnCheckout), new Row(beta, 3, BetaOnCheckout)]);
            var reporting = FeatureFor([new Row(beta, 4, BetaOnReporting)]);

            return DeliveryCompletionForecast.Build([checkout, reporting])!;
        }

        private static Dictionary<int, int> HistogramOf(WhenForecast forecast)
        {
            return forecast.SimulationResult.ToDictionary(bucket => bucket.Key, bucket => bucket.Value);
        }

        private static Team TeamNamed(int id, string name)
        {
            return new Team { Id = id, Name = name };
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
