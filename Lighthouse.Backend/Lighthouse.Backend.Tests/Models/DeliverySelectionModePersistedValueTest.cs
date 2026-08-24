using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Every saved Delivery records how its Features were chosen as a bare number. Nothing translates
    /// that number back through the member names, so moving one - by inserting a member anywhere but
    /// at the end - re-reads every existing Delivery as a kind it never was, with no error and no log
    /// line. These are the numbers already in the database, and they are what this file pins.
    /// </summary>
    public class DeliverySelectionModePersistedValueTest
    {
        [TestCase(DeliverySelectionMode.Manual, 0)]
        [TestCase(DeliverySelectionMode.RuleBased, 1)]
        [TestCase(DeliverySelectionMode.SourceBound, 2)]
        public void EveryWayOfChoosingFeatures_KeepsTheNumberAlreadyWrittenAgainstIt(DeliverySelectionMode wayOfChoosing, int numberInTheDatabase)
        {
            Assert.That((int)wayOfChoosing, Is.EqualTo(numberInTheDatabase));
        }

        [Test]
        public void HowADeliveryChoosesItsFeatures_IsStoredAsAPlainNumber()
        {
            Assert.That(Enum.GetUnderlyingType(typeof(DeliverySelectionMode)), Is.EqualTo(typeof(int)));
        }
    }
}
