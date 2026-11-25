using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class FeatureHasNoChildWorkItemsYetTest
    {
        private static readonly JsonSerializerOptions AsTheWireWritesIt = new(JsonSerializerDefaults.Web);

        private static readonly Team Atlas = new() { Id = 1, Name = "Atlas" };

        [Test]
        public void FeatureWithoutChildWorkItemsAndNoDefaultSize_SaysItHasNoChildWorkItemsYet_OnBothPayloads()
        {
            var feature = new Feature { StateCategory = StateCategories.ToDo };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.True, "The Feature payload must carry hasNoChildWorkItemsYet = true for a childless Feature that is not sized by default, so the detail views can say why it shows 0.");
                Assert.That(OnTheListRow(feature), Is.True, "The Feature list row must carry hasNoChildWorkItemsYet = true for the same Feature, so the Portfolio list can say why it shows 0.");
            }
        }

        [Test]
        public void FeatureSizedByTheDefault_DoesNotClaimToHaveNoChildWorkItemsYet()
        {
            var feature = new Feature { StateCategory = StateCategories.Doing, IsUsingDefaultFeatureSize = true };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.False, "A Feature counted by the default size has its own warning; the note would claim a cause that is false. The payload must carry hasNoChildWorkItemsYet = false.");
                Assert.That(OnTheListRow(feature), Is.False, "A Feature counted by the default size has its own warning on the list row too. The row must carry hasNoChildWorkItemsYet = false.");
            }
        }

        [Test]
        public void FeatureWithChildWorkItems_DoesNotClaimToHaveNoChildWorkItemsYet()
        {
            var feature = new Feature([(Atlas, 3, 5)]) { StateCategory = StateCategories.Doing };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.False, "A Feature with real child Work Items must not say it has none.");
                Assert.That(OnTheListRow(feature), Is.False, "A list row with real child Work Items must not say it has none.");
            }
        }

        [Test]
        public void DoneFeatureWithoutChildWorkItems_DoesNotClaimToHaveNoChildWorkItemsYet()
        {
            var feature = new Feature { StateCategory = StateCategories.Done };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.False, "A finished Feature is not waiting for child Work Items.");
                Assert.That(OnTheListRow(feature), Is.False, "A finished Feature's list row is not waiting for child Work Items.");
            }
        }

        private static bool? OnTheFeaturePayload(Feature feature)
        {
            var dto = new FeatureDto(feature, TestToday.Clock, [], false, null);
            return ReadTheFlag(dto);
        }

        private static bool? OnTheListRow(Feature feature)
        {
            var dto = new FeatureListRowDto(feature, TestToday.Clock, [], false, new HashSet<int>());
            return ReadTheFlag(dto);
        }

        private static bool? ReadTheFlag(object dto)
        {
            var wire = JsonNode.Parse(JsonSerializer.Serialize(dto, dto.GetType(), AsTheWireWritesIt))!.AsObject();
            return wire["hasNoChildWorkItemsYet"]?.GetValue<bool>();
        }
    }
}
