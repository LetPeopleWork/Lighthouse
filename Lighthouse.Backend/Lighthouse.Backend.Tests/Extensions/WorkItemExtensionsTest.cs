using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Extensions
{
    [TestFixture]
    public class WorkItemExtensionsTest
    {
        private const int FieldId = 7;

        [Test]
        public void GetAdditionalFieldValue_NoFieldConfigured_ReturnsNothing()
        {
            var workItem = AWorkItemCarrying(FieldId, "Team Phoenix");

            var value = workItem.GetAdditionalFieldValue(null);

            Assert.That(value, Is.Null);
        }

        [Test]
        public void GetAdditionalFieldValue_FieldConfiguredAndPresent_ReturnsStoredValue()
        {
            var workItem = AWorkItemCarrying(FieldId, "Team Phoenix");

            var value = workItem.GetAdditionalFieldValue(FieldId);

            Assert.That(value, Is.EqualTo("Team Phoenix"));
        }

        // A record stored before the field was configured has no entry for it, and the reader must answer
        // "not set" rather than throw - the callers all treat null as not set, and a throw here fails the
        // whole refresh instead of one field.
        [Test]
        public void GetAdditionalFieldValue_FieldConfiguredButNeverStoredOnTheRecord_ReturnsNothing()
        {
            var workItem = AWorkItemCarrying(FieldId, "Team Phoenix");

            var value = workItem.GetAdditionalFieldValue(FieldId + 1);

            Assert.That(value, Is.Null);
        }

        private static WorkItemBase AWorkItemCarrying(int fieldId, string value)
        {
            return new WorkItemBase
            {
                Name = "Work Item",
                Order = "1",
                AdditionalFieldValues = new Dictionary<int, string?> { [fieldId] = value },
            };
        }
    }
}
