using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;

namespace Lighthouse.Backend.Tests.API.DTO
{
    /// <summary>
    /// A Delivery's daily snapshot keeps the percentile dates it had that day as stored JSON, and the
    /// predictability chart reads them back. A percentile that was beyond the horizon that day has to come
    /// back saying so, or the chart would draw the stand-in date as a forecast. Snapshots written before
    /// the flag existed carry no such property and read back as not beyond, which is what they were.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-03")]
    [Category("ac-03.5")]
    [Category("us-03")]
    public class WhenDistributionBeyondTheHorizonDtoTest
    {
        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web);

        private static readonly DateTime TheDelivery = new(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_snapshot_percentile_that_was_beyond_the_horizon_reads_back_as_beyond()
        {
            var read = TheFirstPercentileReadBackFrom(
                """[{"Probability":85,"ExpectedDate":"2031-10-18T00:00:00Z","BeyondHorizon":true}]""");

            Assert.That(read?["beyondHorizon"]?.GetValue<bool>(), Is.True);
        }

        /// <summary>
        /// The years travel with the point, so the chart words "More than 5 years" from the data, and a point
        /// written under another horizon keeps the one it was forecast with rather than today's.
        /// </summary>
        [Test]
        [Category("ac-03.4")]
        [Category("ac-04.2")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public void A_snapshot_percentile_keeps_the_years_of_the_horizon_it_was_forecast_with()
        {
            var underTheProductHorizon = TheFirstPercentileReadBackFrom(
                """[{"Probability":85,"ExpectedDate":"2031-10-18T00:00:00Z","BeyondHorizon":true,"HorizonYears":5}]""");
            var underAnotherHorizon = TheFirstPercentileReadBackFrom(
                """[{"Probability":85,"ExpectedDate":"2033-10-18T00:00:00Z","BeyondHorizon":true,"HorizonYears":7}]""");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(underTheProductHorizon?["horizonYears"]?.GetValue<int>(), Is.EqualTo(5));
                Assert.That(underAnotherHorizon?["horizonYears"]?.GetValue<int>(), Is.EqualTo(7));
            }
        }

        [Test]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public void A_snapshot_written_before_the_flag_existed_reads_back_as_not_beyond()
        {
            var read = TheFirstPercentileReadBackFrom(
                """[{"Probability":85,"ExpectedDate":"2026-12-01T00:00:00Z"}]""");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read?["beyondHorizon"]?.GetValue<bool>(), Is.False);
                Assert.That(read?["expectedDate"]?.GetValue<DateTime>(), Is.EqualTo(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        private static JsonNode? TheFirstPercentileReadBackFrom(string storedJson)
        {
            var snapshot = new DeliveryMetricSnapshot
            {
                RecordedDay = new DateOnly(2026, 10, 18),
                TotalWork = 10,
                RemainingWork = 6,
                WhenDistributionJson = storedJson,
            };

            var history = DeliveryMetricsHistoryDto.From(TheDelivery, [snapshot]);
            var onTheWire = JsonSerializer.SerializeToNode(history, AsTheWireSendsIt);

            return onTheWire?["points"]?[0]?["whenDistribution"]?[0];
        }
    }
}
