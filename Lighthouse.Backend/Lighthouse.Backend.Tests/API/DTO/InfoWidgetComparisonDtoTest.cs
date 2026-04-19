using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class InfoWidgetComparisonDtoTest
    {
        [Test]
        public void TrendDirection_Up_RoundTrips()
        {
            var subject = new InfoWidgetComparisonDto("up", "WIP", "2026-04-19", "5", "2026-04-05", "3", "+66.7%", []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Direction, Is.EqualTo("up"));
                Assert.That(subject.MetricLabel, Is.EqualTo("WIP"));
                Assert.That(subject.CurrentLabel, Is.EqualTo("2026-04-19"));
                Assert.That(subject.CurrentValue, Is.EqualTo("5"));
                Assert.That(subject.PreviousLabel, Is.EqualTo("2026-04-05"));
                Assert.That(subject.PreviousValue, Is.EqualTo("3"));
                Assert.That(subject.PercentageDelta, Is.EqualTo("+66.7%"));
            }
        }

        [Test]
        public void TrendDirection_None_HasNullOptionalFields()
        {
            var subject = new InfoWidgetComparisonDto("none", "Blocked Items", null, null, null, null, null, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Direction, Is.EqualTo("none"));
                Assert.That(subject.MetricLabel, Is.EqualTo("Blocked Items"));
                Assert.That(subject.CurrentLabel, Is.Null);
                Assert.That(subject.CurrentValue, Is.Null);
                Assert.That(subject.PreviousLabel, Is.Null);
                Assert.That(subject.PreviousValue, Is.Null);
                Assert.That(subject.PercentageDelta, Is.Null);
                Assert.That(subject.DetailRows, Is.Null);
            }
        }

        [Test]
        public void DetailRows_ForPercentileComparison_ContainsPercentileBreakdown()
        {
            var detailRows = new[]
            {
                new TrendDetailRowDto("50th percentile", "4 days", "6 days"),
                new TrendDetailRowDto("85th percentile", "8 days", "10 days"),
                new TrendDetailRowDto("95th percentile", "15 days", "18 days"),
            };

            var subject = new InfoWidgetComparisonDto(
                "down", "Cycle Time Percentiles",
                "2026-04-05 – 2026-04-19", "4 / 8 / 15",
                "2026-03-22 – 2026-04-04", "6 / 10 / 18",
                null, detailRows);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.DetailRows, Has.Length.EqualTo(3));
                Assert.That(subject.DetailRows![0].Label, Is.EqualTo("50th percentile"));
                Assert.That(subject.DetailRows![1].CurrentValue, Is.EqualTo("8 days"));
                Assert.That(subject.DetailRows![2].PreviousValue, Is.EqualTo("18 days"));
            }
        }
    }

    public class ThroughputInfoDtoTest
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var comparison = new InfoWidgetComparisonDto("up", "Total Throughput", "2026-04-05 – 2026-04-19", "42", "2026-03-22 – 2026-04-04", "35", "+20.0%", null);
            var subject = new ThroughputInfoDto(42, 3.0, comparison);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Total, Is.EqualTo(42));
                Assert.That(subject.DailyAverage, Is.EqualTo(3.0));
                Assert.That(subject.Comparison, Is.Not.Null);
                Assert.That(subject.Comparison.Direction, Is.EqualTo("up"));
            }
        }
    }

    public class ArrivalsInfoDtoTest
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var comparison = new InfoWidgetComparisonDto("down", "Total Arrivals", "2026-04-05 – 2026-04-19", "28", "2026-03-22 – 2026-04-04", "32", "-12.5%", null);
            var subject = new ArrivalsInfoDto(28, 2.0, comparison);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Total, Is.EqualTo(28));
                Assert.That(subject.DailyAverage, Is.EqualTo(2.0));
                Assert.That(subject.Comparison, Is.Not.Null);
                Assert.That(subject.Comparison.Direction, Is.EqualTo("down"));
            }
        }
    }

    public class FeatureSizePercentilesInfoDtoTest
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var detailRows = new[]
            {
                new TrendDetailRowDto("50th percentile", "5 items", "4 items"),
                new TrendDetailRowDto("85th percentile", "10 items", "9 items"),
            };
            var comparison = new InfoWidgetComparisonDto("up", "Feature Size Percentiles", "2026-04-05 – 2026-04-19", "5 / 10", "2026-03-22 – 2026-04-04", "4 / 9", null, detailRows);
            var percentiles = new[]
            {
                new PercentileValueDto(50, 5),
                new PercentileValueDto(85, 10),
            };

            var subject = new FeatureSizePercentilesInfoDto(percentiles, comparison);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Percentiles, Has.Length.EqualTo(2));
                Assert.That(subject.Percentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(subject.Percentiles[0].Value, Is.EqualTo(5));
                Assert.That(subject.Comparison, Is.Not.Null);
                Assert.That(subject.Comparison.DetailRows, Has.Length.EqualTo(2));
            }
        }
    }
}
