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
    public class DeliveryCompletionForecastTest
    {
        private const int TargetDay = 10;
        private const int TailDay = 20;

        // Cumulative probability at TargetDay: Alpha/Checkout .90, Beta/Checkout .80, Beta/Reporting .95.
        private static Dictionary<int, int> AlphaOnCheckout => new() { { TargetDay, 9000 }, { TailDay, 1000 } };

        private static Dictionary<int, int> BetaOnCheckout => new() { { TargetDay, 8000 }, { TailDay, 2000 } };

        private static Dictionary<int, int> BetaOnReporting => new() { { TargetDay, 9500 }, { TailDay, 500 } };

        // A fixed instant, not a clock reading: the assertion is "the oldest contributor wins", which
        // does not depend on today (Bug #5567 - a calendar day is defined by a zone, an instant is not).
        private static readonly DateTime OldestCreationTime = new(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc);

        // Per TEAM, not per row: ForecastService reads the summary off the team's chip status, so both
        // of a team's rows carry the identical string.
        private const string AlphaExcludedSummary = "3 items excluded for Alpha";
        private const string BetaExcludedSummary = "2 items excluded for Beta";

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

                // Sufficiency is NOT part of the bit-identity claim. The carriers do not carry it, so the
                // delivery aggregate's flag is false by default rather than by computation, and nothing
                // reads it - Delivery.HasSufficientDataAcrossContributingFeatures owns the rule at the
                // grain AC-02.1 words it (slice-02). Asserting equality here would pin an accident.
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

        [Test]
        public void Build_TeamWorkingTwoFeatures_ComposesTheCarrierMetadataAcrossBothOfItsRows()
        {
            // The bit-identity fixture has single-row buckets only, so every carrier there is a straight
            // copy and this composition never runs. Delivery.CalculateMetrics reads FilterApplied and
            // ExcludedSummary off the joint forecast and puts both on WhenForecastDto, so an
            // implementation that composes the histogram correctly and drops the metadata regresses the
            // "filter applied" indicator on every delivery whose teams work more than one feature.
            // Within a bucket the rule is the one AggregatedWhenForecast already uses across carriers:
            // FilterApplied is an OR, ExcludedSummary a distinct join, CreationTime the oldest.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = new Feature([(alpha, 3, 6), (beta, 3, 6)]);
            checkout.SetFeatureForecasts([
                // Alpha's row must carry a CreationTime too. Left unset it defaults to DateTime.MinValue,
                // which wins the minimum ACROSS carriers and hides whatever the bucket composed - the
                // assertion would then hold for every implementation, including one that composes nothing.
                new WhenForecast(AlphaOnCheckout)
                {
                    Team = alpha,
                    TeamId = alpha.Id,
                    HasSufficientData = true,
                    ExcludedSummary = AlphaExcludedSummary,
                    CreationTime = OldestCreationTime.AddDays(1),
                    NumberOfItems = 3,
                },
                new WhenForecast(BetaOnCheckout)
                {
                    Team = beta,
                    TeamId = beta.Id,
                    HasSufficientData = true,
                    FilterApplied = true,
                    ExcludedSummary = BetaExcludedSummary,
                    CreationTime = OldestCreationTime,
                    NumberOfItems = 4,
                },
            ]);

            var reporting = new Feature([(beta, 4, 7)]);
            reporting.SetFeatureForecasts([
                new WhenForecast(BetaOnReporting)
                {
                    Team = beta,
                    TeamId = beta.Id,
                    // One thin row in the bucket. The carrier ANDs, so the delivery inherits it - an
                    // implementation that ORed would report sufficient off this row's sibling.
                    HasSufficientData = false,
                    // The SAME string as Beta's other row, because ForecastService derives the summary
                    // from the TEAM's chip status - so within a bucket it is always identical, and
                    // Distinct() is the only thing standing between the delivery and "X; X".
                    ExcludedSummary = BetaExcludedSummary,
                    CreationTime = OldestCreationTime.AddDays(2),
                    NumberOfItems = 6,
                },
            ]);

            var forecast = DeliveryCompletionForecast.Build([checkout, reporting])!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.FilterApplied, Is.True);

                // Exactly once for Beta, even though both of its rows carry it - and Alpha's joined
                // after. Without Distinct() this reads "beta; beta; alpha".
                Assert.That(forecast.ExcludedSummary, Is.EqualTo($"{AlphaExcludedSummary}; {BetaExcludedSummary}"));
                Assert.That(forecast.CreationTime, Is.EqualTo(OldestCreationTime));

                // Summed within the bucket, not maxed: Beta's two rows carry 4 and 6, so its carrier is
                // 10 and the delivery is 13. Taking the larger row instead would read 9.
                Assert.That(forecast.NumberOfItems, Is.EqualTo(13));
                // No sufficiency assertion: the carrier does not carry it. Slice-02 established that
                // sufficiency is a FEATURE-grain rule (Delivery.HasSufficientDataAcrossContributingFeatures),
                // and a row-grain answer here would diverge on a stale done row inside a live feature.
            }
        }

        [Test]
        public void Build_ContributingPairWhoseRowRanNoTrials_IsUnknownRatherThanACertainty()
        {
            // Min drops a zero-trial contributor, so without this guard the team becomes CDF = 1 - the
            // same silent certainty as a missing row, one shape lower. Feature.TeamsWithoutForecast
            // normally catches it, but only when it can NAME the team: here the row has no Team
            // navigation and the pair's Team is not loaded either, which is what a read path without
            // FeatureWork.ThenInclude(Team) produces.
            var alpha = TeamNamed(1, "Alpha");

            var checkout = new Feature([(alpha, 3, 6)]);
            checkout.FeatureWork[0].Team = null!;
            checkout.SetFeatureForecasts([
                new WhenForecast(new Dictionary<int, int>()) { TeamId = alpha.Id },
            ]);

            Assert.That(DeliveryCompletionForecast.Build([checkout]), Is.Null);
        }

        [Test]
        public void ContributingRows_ForecastNamesADifferentTeamThanItsTeamId_BelongsToTheTeamItRanFor()
        {
            // The join takes Team?.Id before TeamId, matching Feature.TeamFor: the forecast knows which
            // team it was run for, the id is the fallback. Swapping that precedence would hand Beta's
            // row to Alpha's pair - a shape production never writes (ForecastService sets both from one
            // SimulationResult) but which decides which pair a row is matched against.
            var alpha = TeamNamed(1, "Alpha");
            var beta = TeamNamed(2, "Beta");

            var checkout = new Feature([(alpha, 3, 6)]);
            checkout.SetFeatureForecasts([
                new WhenForecast(BetaOnCheckout) { Team = beta, TeamId = alpha.Id, HasSufficientData = true },
            ]);

            var rows = DeliveryCompletionForecast.ContributingRows([checkout]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows[0].TeamId, Is.EqualTo(alpha.Id));

                // Alpha's pair owns no row of its own, because the only row belongs to Beta.
                Assert.That(rows[0].Forecast, Is.Null);
            }
        }

        [Test]
        public void Build_BucketWhoseRowsExcludeNothing_LeavesTheExcludedSummaryUnset()
        {
            // null, not "". The DTO passes ExcludedSummary straight to WhenForecastDto, where an empty
            // string renders as a filter note about nothing.
            var alpha = TeamNamed(1, "Alpha");

            var checkout = FeatureFor([new Row(alpha, 3, AlphaOnCheckout)]);

            var forecast = DeliveryCompletionForecast.Build([checkout])!;

            Assert.That(forecast.ExcludedSummary, Is.Null);
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
