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

        private static readonly ParentSources AListOfTwoFields = ParentSources.Of(
            [ParentSourceEntry.Field(1), ParentSourceEntry.TrackerParent, ParentSourceEntry.Field(2)]).List!;

        private const ConnectionDefaultSetting ForTeams = ConnectionDefaultSetting.ParentOverrideForTeams;

        private static readonly int[] TheTwoFields = [1, 2];

        [Test]
        public void A_connection_is_read_with_the_list_of_each_grain()
        {
            var connection = new WorkTrackingSystemConnection();
            connection.SetParentSourcesDefaultForTeams(AListOfTwoFields);

            var defaults = new ConnectionDefaultsDto(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ParentSourceEntryDto.TextOf(defaults.ParentSourcesForTeams), Is.EqualTo(ParentSourceEntryDto.TextOf(AListOfTwoFields)));
                Assert.That(defaults.ParentSourcesForPortfolios, Is.Null);
            }
        }

        [Test]
        public void The_fields_of_a_sent_list_are_all_the_fields_it_names()
        {
            var defaults = new ConnectionDefaultsDto { ParentSourcesForTeams = ParentSourceEntryDto.ListOf(AListOfTwoFields) };

            Assert.That(defaults.FieldsOf(ForTeams, held: null), Is.EqualTo(TheTwoFields));
        }

        [Test]
        public void A_sent_list_that_cannot_be_kept_names_no_fields()
        {
            var defaults = new ConnectionDefaultsDto { ParentSourcesForTeams = [] };

            Assert.That(defaults.FieldsOf(ForTeams, held: null), Is.Empty);
        }

        [Test]
        public void An_explicit_null_list_clears_the_default_rather_than_saying_nothing()
        {
            var defaults = new ConnectionDefaultsDto { ParentSourcesForTeams = null };

            var choice = defaults.ParentSourcesChoiceFor(ForTeams, AListOfTwoFields);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(choice, Is.Not.Null);
                Assert.That(choice?.Outcome, Is.Null);
            }
        }

        [Test]
        public void A_changed_old_field_beside_the_list_the_client_read_edits_the_field()
        {
            var defaults = new ConnectionDefaultsDto
            {
                ParentOverrideForTeams = 9,
                ParentSourcesForTeams = ParentSourceEntryDto.ListOf(AListOfTwoFields),
            };

            Assert.That(defaults.ParentSourcesChoiceFor(ForTeams, AListOfTwoFields), Is.Null);
        }

        [Test]
        public void An_old_field_beside_a_new_list_leaves_the_list_as_the_edit()
        {
            var defaults = new ConnectionDefaultsDto
            {
                ParentOverrideForTeams = 9,
                ParentSourcesForTeams = ParentSourceEntryDto.ListOf(ParentSources.OnlyTheTrackersOwnParent),
            };

            var choice = defaults.ParentSourcesChoiceFor(ForTeams, AListOfTwoFields);

            Assert.That(choice?.Outcome?.List?.ToStoredText(), Is.EqualTo(ParentSources.OnlyTheTrackersOwnParent.ToStoredText()));
        }

        [Test]
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
        public void A_Parent_default_left_out_of_the_save_keeps_what_the_connection_holds()
        {
            var connection = new WorkTrackingSystemConnection { ParentOverrideForTeams = 5 };

            var defaults = new ConnectionDefaultsDto(connection);

            Assert.That(defaults.FieldOf(ConnectionDefaultSetting.ParentOverrideForTeams), Is.EqualTo(5), "A save that names no Parent default must not clear the one the connection holds.");
        }

        [TestCase(ConnectionDefaultSetting.ParentOverrideForTeams)]
        [TestCase(ConnectionDefaultSetting.ParentOverrideForPortfolios)]
        public void A_save_naming_neither_the_field_nor_the_list_of_a_Parent_default_does_not_send_it(ConnectionDefaultSetting setting)
        {
            var defaults = JsonSerializer.Deserialize<ConnectionDefaultsDto>("""{"dependencyFieldForPortfolios":5}""", AsTheWireSendsIt)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults.Sends(setting), Is.False, "A save that leaves a default out must not clear it.");
                Assert.That(defaults.FieldOf(setting), Is.Null);
                Assert.That(defaults.ParentSourcesChoiceFor(setting, AListOfTwoFields), Is.Null);
            }
        }

        [TestCase(ConnectionDefaultSetting.ParentOverrideForTeams, "parentSourcesForTeams")]
        [TestCase(ConnectionDefaultSetting.ParentOverrideForPortfolios, "parentSourcesForPortfolios")]
        public void A_save_sending_the_list_the_page_read_without_its_old_field_keeps_that_list(ConnectionDefaultSetting setting, string listMember)
        {
            var defaults = JsonSerializer.Deserialize<ConnectionDefaultsDto>(
                $$"""{"{{listMember}}":[{"kind":"AdditionalField","additionalFieldDefinitionId":1},{"kind":"TrackerParent"},{"kind":"AdditionalField","additionalFieldDefinitionId":2}]}""", AsTheWireSendsIt)!;

            var choice = defaults.ParentSourcesChoiceFor(setting, AListOfTwoFields);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults.Sends(setting), Is.True);
                Assert.That(choice?.Outcome?.List?.ToStoredText(), Is.EqualTo(AListOfTwoFields.ToStoredText()), "The list the page read is the list the connection keeps.");
            }
        }

        [TestCase(ConnectionDefaultSetting.ParentOverrideForTeams, "parentOverrideForTeams", "parentSourcesForTeams")]
        [TestCase(ConnectionDefaultSetting.ParentOverrideForPortfolios, "parentOverrideForPortfolios", "parentSourcesForPortfolios")]
        public void An_old_field_sent_as_empty_beside_the_list_the_page_read_clears_the_default(ConnectionDefaultSetting setting, string fieldMember, string listMember)
        {
            var defaults = JsonSerializer.Deserialize<ConnectionDefaultsDto>(
                $$"""{"{{fieldMember}}":null,"{{listMember}}":[{"kind":"AdditionalField","additionalFieldDefinitionId":1},{"kind":"TrackerParent"},{"kind":"AdditionalField","additionalFieldDefinitionId":2}]}""", AsTheWireSendsIt)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaults.Sends(setting), Is.True, "An explicit null is a choice to clear.");
                Assert.That(defaults.ParentSourcesChoiceFor(setting, AListOfTwoFields), Is.Null, "The emptied old field, not the list the page read, is the edit.");
                Assert.That(defaults.FieldOf(setting), Is.Null);
                Assert.That(defaults.FieldsOf(setting, AListOfTwoFields), Is.Empty);
            }
        }

        [Test]
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
