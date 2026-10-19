using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ConnectionDefaultsDtoTest
    {
        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        private static readonly string[] SmallMedium = ["S", "M"];

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

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_saved_Estimation_default_is_sent_as_its_whole_bundle_and_an_explicit_null_clears_the_other()
        {
            var connection = JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(
                """{"id":12,"workTrackingSystem":"Jira","authenticationMethodKey":"jira.cloud","defaults":{"estimationForTeams":{"additionalFieldDefinitionId":5,"unit":"Sizes","useNonNumericEstimation":true,"estimationCategoryValues":["S","M"]},"estimationForPortfolios":null}}""",
                AsTheWireSendsIt);

            var defaults = connection?.Defaults;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults?.Sends(ConnectionDefaultSetting.EstimationForTeams), Is.True);
                Assert.That(defaults?.FieldOf(ConnectionDefaultSetting.EstimationForTeams), Is.EqualTo(5));
                Assert.That(defaults?.BundleOf(ConnectionDefaultSetting.EstimationForTeams), Is.EqualTo(new EstimationBundle(5, "Sizes", true, SmallMedium)));
                Assert.That(defaults?.Sends(ConnectionDefaultSetting.EstimationForPortfolios), Is.True);
                Assert.That(defaults?.FieldOf(ConnectionDefaultSetting.EstimationForPortfolios), Is.Null);
                Assert.That(defaults?.BundleOf(ConnectionDefaultSetting.EstimationForPortfolios), Is.EqualTo(EstimationBundle.None));
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void An_Estimation_default_left_out_of_the_save_is_not_sent()
        {
            var defaults = new ConnectionDefaultsDto { ParentOverrideForTeams = 5 };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults.Sends(ConnectionDefaultSetting.EstimationForTeams), Is.False);
                Assert.That(defaults.BundleOf(ConnectionDefaultSetting.EstimationForTeams), Is.Null);
            }
        }
    }
}
