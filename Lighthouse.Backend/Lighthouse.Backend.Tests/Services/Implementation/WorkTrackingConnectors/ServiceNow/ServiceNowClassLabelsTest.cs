using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5612, ADR-128. The vocabulary itself: what each kind of work is called on either side
    // of the connector boundary. Layer 1 (pure), so this is the one file that can enumerate the map
    // cheaply — the behaviour that DEPENDS on it lives in ServiceNowRecordClassTest.
    //
    // The resolution and passthrough tests are the load-bearing ones. Everything else in this
    // feature rests on a kind of work's two names agreeing on one class, and on an unknown name
    // surviving unchanged.
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
        public void ClassFor_AKnownLabel_IsTheClassTheTableApiFiltersOn((string RecordClass, string Label) kindOfWork)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(kindOfWork.Label), Is.EqualTo(kindOfWork.RecordClass));
        }

        // The invariant the whole design rests on: a kind of work resolves to the same record class
        // whichever of its two names the coach typed. If the two names disagreed, config and data
        // would drift apart, which is the silent zero AC-D1 exists to catch.
        [TestCaseSource(nameof(EveryKnownKindOfWork))]
        public void EveryKnownKindOfWork_ResolvesToOneClassFromEitherOfItsNames(
            (string RecordClass, string Label) kindOfWork)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ServiceNowClassLabels.ClassFor(kindOfWork.Label), Is.EqualTo(kindOfWork.RecordClass));
                Assert.That(ServiceNowClassLabels.ClassFor(kindOfWork.RecordClass), Is.EqualTo(kindOfWork.RecordClass));
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

        // Two labels collapsing onto one class would silently merge two kinds of work into one
        // query. Its mirror image — two classes sharing a label — is caught structurally instead:
        // the reverse map is built with ToDictionary, which throws on the duplicate key the moment
        // the type initialises, so every test in this file would fail at once.
        [Test]
        public void NoTwoLabels_ResolveToTheSameKindOfWork()
        {
            var everyLabel = new[]
            {
                "Task", "Incident", "Problem", "Change Request", "Change Task", "Incident Task",
                "Problem Task", "Catalog Task", "Requested Item", "Request", "Feature Task", "Ticket",
            };

            var classes = everyLabel.Select(ServiceNowClassLabels.ClassFor).ToList();

            Assert.That(classes.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(everyLabel.Length),
                "Two labels meaning the same class would merge two kinds of work into one query.");
        }

        // Measured on the PDI, 2026-08-01, with the two curls in this story's dogfood: sysparm_query's
        // IN matches a VALUE case-insensitively, so sys_class_nameINChange_Request returns exactly the
        // rows the lowercase form does -- and every one of them says `change_request`. A team left
        // holding the typed casing would sync happily and then disagree with its own work items,
        // because AsTyped compares ordinally. That is the silent zero the epic exists to prevent, so
        // the case is folded away here rather than being allowed to reach the query.
        [TestCase("Change_Request")]
        [TestCase("CHANGE_REQUEST")]
        [TestCase("change_Request")]
        public void ARecordClassInTheWrongCase_IsAnsweredWithTheCaseServiceNowStores(string typed)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(typed), Is.EqualTo("change_request"));
        }

        // Recognising a class name in any case is only safe while no LABEL resolves to a different
        // class than the class-first step would pick. Four labels equal their own class name ignoring
        // case -- Task, Incident, Problem, Ticket -- and on those both paths agree. A future entry
        // whose label collided with some OTHER class's name would silently reroute it, so the
        // invariant is asserted rather than assumed.
        [TestCaseSource(nameof(EveryKnownKindOfWork))]
        public void NoLabel_IsAlsoSomeOtherKindOfWorksClassName((string RecordClass, string Label) kindOfWork)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(kindOfWork.Label), Is.EqualTo(kindOfWork.RecordClass),
                "A label that reads as another class's name would be resolved to that class instead.");
        }

        // The case that nearly shipped broken, kept as a regression guard: LabelByClass is
        // case-insensitive, so asking it whether "Incident" is a record class answers yes -- it matches
        // the key `incident`. Handing back the INPUT there returns the label untranslated; handing back
        // the canonical KEY, which is what ClassFor now does, is the fix.
        [Test]
        public void ALabelWhoseClassDiffersOnlyByCase_IsStillResolvedToTheClass()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ServiceNowClassLabels.ClassFor("Incident"), Is.EqualTo("incident"));
                Assert.That(ServiceNowClassLabels.ClassFor("Problem"), Is.EqualTo("problem"));
                Assert.That(ServiceNowClassLabels.ClassFor("Task"), Is.EqualTo("task"));
                Assert.That(ServiceNowClassLabels.ClassFor("Ticket"), Is.EqualTo("ticket"));
            }
        }

        // sc_task is Catalog Task and release_task is Feature Task. Neither is reachable by rewriting
        // the class name, which is the entire argument for a map over a transform (ADR-128 / D5).
        [TestCase("sc_task", "Catalog Task")]
        [TestCase("release_task", "Feature Task")]
        [TestCase("change_request_imac", "IMAC")]
        [TestCase("sysapproval_group", "Group approval")]
        public void AKindOfWorkWhoseLabelNoTransformProduces_IsStillCorrect(string recordClass, string label)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(label), Is.EqualTo(recordClass));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void AnEmptyName_IsReturnedUnchangedRatherThanMappedToAnything(string empty)
        {
            Assert.That(ServiceNowClassLabels.ClassFor(empty), Is.EqualTo(empty));
        }
    }
}
