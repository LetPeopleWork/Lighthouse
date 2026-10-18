using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ConnectionDefaultsDtoTest
    {
        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_saved_Dependency_Field_default_binds_to_the_default_this_build_holds()
        {
            var connection = JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(
                """{"id":12,"workTrackingSystem":"Jira","authenticationMethodKey":"jira.cloud","defaults":{"dependencyFieldForPortfolios":5}}""",
                AsTheWireSendsIt);

            var defaults = connection?.Defaults;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults?.FieldOf(ConnectionDefaultSetting.DependencyFieldForPortfolios), Is.EqualTo(5));
                Assert.That(defaults?.NamedButNotHeld(), Is.Empty);
                Assert.That(defaults?.NotHeldByThisBuild, Is.Null.Or.Empty);
            }
        }
    }
}
