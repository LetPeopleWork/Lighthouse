using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    // Epic #5585 slice 02 (US-02). Delivery.ToFeatureMetric already computes a feature's total child
    // items to derive completion and then throws the number away; these pin it onto the breakdown so a
    // backlog jump can be attributed to a named epic. The estimate flag rides along here (recorded in
    // this slice, rendered in slice 03).
    public class DeliveryFeatureSizeTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 29, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

        [Test]
        public void CalculateMetrics_ReportsEachFeaturesTotalChildItems()
        {
            var delivery = DeliveryWith(
                FeatureNamed("EPIC-1", "Checkout", remaining: 5, total: 8),
                FeatureNamed("EPIC-2", "Search", remaining: 1, total: 3));

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 85);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SizeOf(metrics, "EPIC-1"), Is.EqualTo(8));
                Assert.That(SizeOf(metrics, "EPIC-2"), Is.EqualTo(3));
            }
        }

        [Test]
        public void CalculateMetrics_CountsEveryChildItem_EvenWhenTheFeatureIsFinished()
        {
            var delivery = DeliveryWith(FeatureNamed("EPIC-1", "Checkout", remaining: 0, total: 8));

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 85);

            Assert.That(SizeOf(metrics, "EPIC-1"), Is.EqualTo(8));
        }

        [Test]
        public void CalculateMetrics_MarksAFeatureWhoseSizeIsThePortfolioDefault()
        {
            var guessed = FeatureNamed("EPIC-1", "Checkout", remaining: 5, total: 8);
            guessed.IsUsingDefaultFeatureSize = true;
            var brokenDown = FeatureNamed("EPIC-2", "Search", remaining: 1, total: 3);

            var delivery = DeliveryWith(guessed, brokenDown);

            var metrics = delivery.CalculateMetrics(Clock.Today, NoBlackoutPeriods, 85);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EntryFor(metrics, "EPIC-1").IsUsingDefaultSize, Is.True);
                Assert.That(EntryFor(metrics, "EPIC-2").IsUsingDefaultSize, Is.False);
            }
        }

        private static int? SizeOf(DeliveryMetricsProjection metrics, string referenceId)
        {
            return EntryFor(metrics, referenceId).TotalItems;
        }

        private static DeliveryFeatureMetric EntryFor(DeliveryMetricsProjection metrics, string referenceId)
        {
            return metrics.FeatureBreakdown.Single(entry => entry.ReferenceId == referenceId);
        }

        private static Delivery DeliveryWith(params Feature[] features)
        {
            var delivery = new Delivery { Id = 1, Name = "Q3 Launch", Date = Clock.TodayAsUtcMidnight.AddDays(30) };

            delivery.ReplaceFeatures(features);

            return delivery;
        }

        private static Feature FeatureNamed(string referenceId, string name, int remaining, int total)
        {
            var team = new Team { Id = 1, Name = "Alpha" };
            var feature = new Feature([(team, remaining, total)]);
            feature.ReferenceId = referenceId;
            feature.Name = name;

            return feature;
        }
    }
}
