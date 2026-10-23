using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    public class RefreshLogDtoTest
    {
        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void A_row_carries_whether_its_forecast_was_skipped(bool skipped)
        {
            var row = SerialisedRowFor(new RefreshLog { Id = 1, Type = RefreshType.Forecast, EntityId = 7, Skipped = skipped });

            Assert.That(row["skipped"]?.GetValue<bool>(), Is.EqualTo(skipped));
        }

        [Test]
        public void A_computed_row_says_so_explicitly_rather_than_leaving_the_member_out()
        {
            var row = SerialisedRowFor(new RefreshLog { Id = 1, Type = RefreshType.Team, EntityId = 3 });

            Assert.That(row.ContainsKey("skipped"), Is.True);
        }

        private static JsonObject SerialisedRowFor(RefreshLog log)
        {
            var dto = RefreshLogDto.ListFor([log]).Single();

            return JsonNode.Parse(JsonSerializer.Serialize(dto, AsTheWireSendsIt))!.AsObject();
        }
    }
}
