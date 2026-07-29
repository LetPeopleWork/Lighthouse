using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.DTO
{
    // Story #5587 (ADR-113 point 7), slice-02. GetLeastLikelyFeature is deleted and
    // DeliveryWithLikelihoodDto.HasSufficientData becomes the AND across the delivery's features that
    // have REMAINING WORK; the empty set yields true (D6 / AC-02.1, AC-02.2).
    //
    // Why this file asserts at the DTO grain and not at DeliveryMetricsProjection: AC-02.1 words its
    // subject as "DeliveryWithLikelihoodDto.HasSufficientData", and the DTO is where the value is
    // genuinely computed TODAY - so every fixture below discriminates the old rule from the new one.
    // DDD-2 routes the value through DeliveryMetricsProjection, which is the internal carrier DESIGN
    // chose to reach delivery.Features from FromDelivery; a field with no behaviour and no wire
    // surface is not a separate observable, and pinning the route rather than the answer would be an
    // AST-shape test (same reasoning as slice-01's DT-10 on the deleted selectors).
    //
    // The landmine this slice exists to avoid: a feature with no remaining work carries the
    // whole-feature {0: 0} sentinel whose Team is null, so CreateWhenForecastForSimulationResult
    // (ForecastService.cs:156) never assigns HasSufficientData and the bool stays at its false default
    // (WhenForecast.cs:36, no initializer). A plain All(...) therefore reports "not enough data" on
    // EVERY delivery containing a completed feature. Today's least-likely path masks that by accident,
    // because a finished feature sorts to likelihood 100 and is never selected unless it is alone -
    // which is exactly why FromDelivery_EveryFeatureIsFinished_... below is RED and
    // FromDelivery_FinishedFeatureAlongsideAWellSupportedOne_... is green.
    public class DeliverySufficiencyDtoTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 29, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

        // The delivery date lands ON a histogram key. ForecastBase.GetLikelihood adds a day's mass
        // BEFORE testing key >= daysToTargetDate, so a target BETWEEN two keys reads the later day's
        // mass too (slice-01 upstream note 4). Keeping the target on a key keeps this suite
        // independent of that quirk.
        private const int TargetDay = 10;
        private const int TailDay = 20;

        private static Dictionary<int, int> SixtyPercentByTargetDay => new() { { TargetDay, 6000 }, { TailDay, 4000 } };

        private static Dictionary<int, int> EightyPercentByTargetDay => new() { { TargetDay, 8000 }, { TailDay, 2000 } };

        private static Dictionary<int, int> NinetyFivePercentByTargetDay => new() { { TargetDay, 9500 }, { TailDay, 500 } };

        [Test]
        public void FromDelivery_ThinHistoryOnAFeatureThatIsNotTheLeastLikely_ReportsInsufficientData()
        {
            // AC-02.1 and AC-02.4, the visible delta. Checkout is the LEAST LIKELY feature and rests on
            // ample history; Reporting is the most likely and rests on three days of it. Today the
            // delivery reads the flag off Checkout alone and shows no warning at all, even though the
            // joint number it publishes now rests on Reporting's thin history too.
            var delivery = DeliveryWith(
                FeatureFor("Alpha", 1, remaining: 5, SixtyPercentByTargetDay, hasSufficientData: true),
                FeatureFor("Beta", 2, remaining: 5, NinetyFivePercentByTargetDay, hasSufficientData: false));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.HasSufficientData, Is.False);

                // The per-feature rows are marginals and keep saying exactly what they said before -
                // the change is a delivery-grain rollup, not a per-feature one (AC-02.1 OUT of scope).
                Assert.That(dto.FeatureLikelihoods[0].HasSufficientData, Is.True);
                Assert.That(dto.FeatureLikelihoods[1].HasSufficientData, Is.False);
            }
        }

        [Test]
        public void FromDelivery_EveryFeatureIsFinished_ReportsSufficientDataRatherThanTheSentinelDefault()
        {
            // AC-02.1's "empty set yields true", and the sharpest statement of the landmine. Every
            // feature is exempt, so the AND is over nothing. Today the delivery instead reads the flag
            // off the finished feature's {0: 0} sentinel, whose bool was never assigned, and reports
            // "not enough data" on a delivery that is DONE.
            var delivery = DeliveryWith(FinishedFeature("Alpha", 1), FinishedFeature("Beta", 2));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.HasSufficientData, Is.True);
                Assert.That(dto.LikelihoodPercentage, Is.EqualTo(100));
            }
        }

        [Test]
        public void FromDelivery_UnforecastableDeliveryWithThinHistoryElsewhere_ReportsBothSignals()
        {
            // AC-02.5: the ADR-112 unknown state and the sufficiency signal COMPOSE. A delivery can be
            // both un-forecastable and insufficient; "cannot forecast" merely wins on screen. Today the
            // un-forecastable feature drops out of the ranking (null >= 0 is false in C#) and the
            // second-least-likely feature answers for sufficiency, so the thin history stays hidden
            // behind the unknown state instead of being reported alongside it.
            var delivery = DeliveryWith(
                UnforecastableFeature("Meridian", 1),
                FeatureFor("Alpha", 2, remaining: 5, SixtyPercentByTargetDay, hasSufficientData: true),
                FeatureFor("Beta", 3, remaining: 5, NinetyFivePercentByTargetDay, hasSufficientData: false));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.LikelihoodPercentage, Is.Null);
                Assert.That(dto.HasSufficientData, Is.False);
            }
        }

        [Test]
        public void FromDelivery_FinishedFeatureAlongsideAWellSupportedOne_StillReportsSufficientData()
        {
            // AC-02.2, the regression fixture the exemption exists for. Green today only because a
            // finished feature sorts to likelihood 100 and is therefore never the least likely one -
            // an accident, not a rule. Without the remaining-work exemption the new AND reads the
            // finished feature's sentinel default and this goes false, which would put a "not enough
            // data" indicator on every delivery that contains a completed feature.
            var delivery = DeliveryWith(
                FinishedFeature("Legacy", 1),
                FeatureFor("Alpha", 2, remaining: 5, EightyPercentByTargetDay, hasSufficientData: true));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.True);
        }

        [Test]
        public void FromDelivery_FinishedFeatureAlongsideAThinOne_StillReportsInsufficientData()
        {
            // The exemption must not over-exempt. An implementation that drops every feature from the
            // AND - or that returns true whenever any feature is finished - passes the fixture above
            // and fails here. Green today and after.
            var delivery = DeliveryWith(
                FinishedFeature("Legacy", 1),
                FeatureFor("Alpha", 2, remaining: 5, SixtyPercentByTargetDay, hasSufficientData: false));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.False);
        }

        [Test]
        public void FromDelivery_EveryContributingFeatureHasAmpleHistory_KeepsReportingSufficientData()
        {
            // The negative control. AND can only flip true -> false, never the reverse (D6 point 4), so
            // a delivery whose every contributing feature is well supported must keep reading true.
            var delivery = DeliveryWith(
                FeatureFor("Alpha", 1, remaining: 5, SixtyPercentByTargetDay, hasSufficientData: true),
                FeatureFor("Beta", 2, remaining: 5, NinetyFivePercentByTargetDay, hasSufficientData: true));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.True);
        }

        [Test]
        public void FromDelivery_ThinHistoryOnTheLeastLikelyFeature_KeepsReportingInsufficientData()
        {
            // The direction-of-change guard. The one case today's rule already gets right must not
            // regress: AND never newly HIDES a warning, it only surfaces one that was masked.
            var delivery = DeliveryWith(
                FeatureFor("Alpha", 1, remaining: 5, SixtyPercentByTargetDay, hasSufficientData: false),
                FeatureFor("Beta", 2, remaining: 5, NinetyFivePercentByTargetDay, hasSufficientData: true));

            var dto = DeliveryWithLikelihoodDto.FromDelivery(delivery, Clock.Today, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.False);
        }

        [Test]
        public void FromDelivery_DeliveryWithoutFeatures_ReportsSufficientData()
        {
            // The other empty-AND case, and the only one today's `?? featureLikelihoods.All(...)`
            // fallback ever reaches. AC-02.1 keeps the answer; deleting GetLeastLikelyFeature deletes
            // the fallback with it, so this pins the value rather than the expression.
            var dto = DeliveryWithLikelihoodDto.FromDelivery(DeliveryWith(), Clock.Today, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.True);
        }

        [Test]
        public void FromDelivery_StaleDoneRowInsideALiveFeature_IsStillCountedByTheFeatureGrainAnd()
        {
            // DDD-2's named nuance, pinned so nobody "unifies" the two grains without noticing they are
            // two different sets. Beta has finished its share of a feature that is still live overall
            // and its row carries the insufficiency. FEATURE grain - which is how AC-02.1 words the
            // rule, and what f.Forecasts.All(...) computes - includes that row. ROW grain (only rows
            // with remaining work) would exclude it and report true. Feature grain wins; the delivery
            // says "not enough data".
            var alpha = new Team { Id = 1, Name = "Alpha" };
            var beta = new Team { Id = 2, Name = "Beta" };

            var feature = new Feature([(alpha, 5, 8), (beta, 0, 3)]);
            feature.SetFeatureForecasts([
                new WhenForecast(EightyPercentByTargetDay) { Team = alpha, TeamId = alpha.Id, NumberOfItems = 5, HasSufficientData = true },
                new WhenForecast(NinetyFivePercentByTargetDay) { Team = beta, TeamId = beta.Id, NumberOfItems = 0, HasSufficientData = false },
            ]);

            var dto = DeliveryWithLikelihoodDto.FromDelivery(DeliveryWith(feature), Clock.Today, NoBlackoutPeriods);

            Assert.That(dto.HasSufficientData, Is.False);
        }

        private static Delivery DeliveryWith(params Feature[] features)
        {
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q3 Launch",
                Date = Clock.TodayAsUtcMidnight.AddDays(TargetDay),
            };

            foreach (var feature in features)
            {
                delivery.Features.Add(feature);
            }

            return delivery;
        }

        private static Feature FeatureFor(string teamName, int teamId, int remaining, Dictionary<int, int> histogram, bool hasSufficientData)
        {
            var team = new Team { Id = teamId, Name = teamName };
            var feature = new Feature([(team, remaining, remaining + 3)]) { Id = teamId };
            feature.SetFeatureForecasts([
                new WhenForecast(histogram) { Team = team, TeamId = team.Id, NumberOfItems = remaining, HasSufficientData = hasSufficientData },
            ]);

            return feature;
        }

        // ForecastService.cs:141-146 - the whole-feature day-0 sentinel, built with the parameterless
        // SimulationResult ctor, so Team is null and HasSufficientData is never assigned. Reproduced
        // exactly: a fixture that sets the flag here would hide the very defect this slice closes.
        private static Feature FinishedFeature(string teamName, int teamId)
        {
            var team = new Team { Id = teamId, Name = teamName };
            var feature = new Feature([(team, 0, 5)]) { Id = teamId };
            feature.SetFeatureForecasts([new WhenForecast(new Dictionary<int, int> { { 0, 0 } })]);

            return feature;
        }

        private static Feature UnforecastableFeature(string teamName, int teamId)
        {
            var team = new Team { Id = teamId, Name = teamName };
            var feature = new Feature([(team, 3, 3)]) { Id = teamId };
            feature.SetFeatureForecasts([new WhenForecast([]) { Team = team, TeamId = team.Id, HasSufficientData = true }]);

            return feature;
        }
    }
}
