using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5612, ADR-128. The vocabulary itself: what each kind of work is called on either side
    // of the connector boundary. Layer 1 (pure), so this is the one file that can enumerate the map
    // cheaply — the behaviour that DEPENDS on it lives in ServiceNowRecordClassTest.
    //
    // The round-trip and passthrough tests are the load-bearing ones. Everything else in this
    // feature rests on the two directions agreeing, and on an unknown name surviving both of them
    // unchanged.
    [TestFixture]
    public class ServiceNowClassLabelsTest
    {
        // Confirmed present on the PDI across the epic's SPIKEs. sc_task is the one that proves the
        // map is a map: no case transform produces "Catalog Task" from "sc_task".
        private static readonly (string RecordClass, string Label)[] KnownKindsOfWork =
        [
            ("incident", "Incident"),
            ("problem", "Problem"),
            ("change_request", "Change Request"),
            ("sc_task", "Catalog Task"),
            ("task", "Task"),
        ];

        public static IEnumerable<(string RecordClass, string Label)> EveryKnownKindOfWork() => KnownKindsOfWork;

        [TestCaseSource(nameof(EveryKnownKindOfWork))]
        public void LabelFor_AKnownRecordClass_IsTheLabelTheInstanceShows((string RecordClass, string Label) kindOfWork)
        {
            Assert.That(ServiceNowClassLabels.LabelFor(kindOfWork.RecordClass), Is.EqualTo(kindOfWork.Label));
        }

        [TestCaseSource(nameof(EveryKnownKindOfWork))]
        public void ClassFor_AKnownLabel_IsTheClassTheTableApiFiltersOn((string RecordClass, string Label) kindOfWork)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(kindOfWork.Label), Is.EqualTo(kindOfWork.RecordClass));
        }

        // The invariant the whole design rests on: whichever way a name goes in, it comes back the
        // same. A one-way map would let config and data drift apart, which is the silent zero
        // AC-D1 exists to catch.
        [TestCaseSource(nameof(EveryKnownKindOfWork))]
        public void EveryKnownKindOfWork_RoundTripsInBothDirections((string RecordClass, string Label) kindOfWork)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ServiceNowClassLabels.ClassFor(ServiceNowClassLabels.LabelFor(kindOfWork.RecordClass)),
                    Is.EqualTo(kindOfWork.RecordClass));
                Assert.That(ServiceNowClassLabels.LabelFor(ServiceNowClassLabels.ClassFor(kindOfWork.Label)),
                    Is.EqualTo(kindOfWork.Label));
            }
        }

        // A class name typed directly still has to work — it is what every team configured before
        // this shipped holds, and what #5610's board picker pre-fills.
        [Test]
        public void ClassFor_ARecordClassTypedDirectly_IsThatRecordClass()
        {
            Assert.That(ServiceNowClassLabels.ClassFor("change_request"), Is.EqualTo("change_request"));
        }

        // A coach types what is on their screen, and screens are not consistent about case.
        [TestCase("change request")]
        [TestCase("CHANGE REQUEST")]
        [TestCase("Change request")]
        public void ClassFor_ALabelInAnyCase_IsTheSameRecordClass(string typed)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(typed), Is.EqualTo("change_request"));
        }

        // AC-D2. Passthrough, both directions, for a class Lighthouse has never heard of. This is
        // what keeps a custom class CONSISTENT rather than merely unimproved: the item stores
        // u_maintenance_task and the config entry stays u_maintenance_task, so they still match.
        [Test]
        public void AnUnknownKindOfWork_SurvivesBothDirectionsUnchanged()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ServiceNowClassLabels.LabelFor("u_maintenance_task"), Is.EqualTo("u_maintenance_task"));
                Assert.That(ServiceNowClassLabels.ClassFor("u_maintenance_task"), Is.EqualTo("u_maintenance_task"));
                Assert.That(ServiceNowClassLabels.ClassFor("Maintenance Task"), Is.EqualTo("Maintenance Task"),
                    "An unknown LABEL passes through too — the map never invents a class name.");
            }
        }

        // A misspelling has to survive to the validation probe intact, or ADR-124 rung 1 loses the
        // string it needs to put in the message.
        [Test]
        public void AMisspeltKindOfWork_IsNotSilentlyCorrected()
        {
            Assert.That(ServiceNowClassLabels.ClassFor("not_a_real_class"), Is.EqualTo("not_a_real_class"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void AnEmptyName_IsReturnedUnchangedRatherThanMappedToAnything(string empty)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ServiceNowClassLabels.ClassFor(empty), Is.EqualTo(empty));
                Assert.That(ServiceNowClassLabels.LabelFor(empty), Is.EqualTo(empty));
            }
        }
    }
}
