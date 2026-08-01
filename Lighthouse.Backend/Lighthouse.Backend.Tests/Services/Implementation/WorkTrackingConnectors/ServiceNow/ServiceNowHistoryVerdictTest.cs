using System.Net;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5577, ADR-118 decision 5. Layer 1 (pure, no IO).
    //
    // Two different things stop a team getting true time-in-progress: the account cannot read the
    // metric tables, or it can but the instance measures no state spans. Slice 01's C-1 amendment had
    // to give up on a distinction the platform could not make; this one it can — a 403 and a 200 with
    // nothing matching are different answers — so telling them apart is not optional here. An
    // administrator who is told "grant itil" when the real problem is a disabled metric definition
    // goes and asks their platform team for a role that changes nothing.
    [TestFixture]
    public class ServiceNowHistoryVerdictTest
    {
        private static readonly string[] OneKindOfWork = ["incident"];

        private static readonly string[] TwoKindsOfWork = ["incident", "change_request"];

        private static readonly string[] MeasuredOnIt = ["incident"];

        private static readonly string[] MeasuredOnNothing = [];

        private static readonly string[] OnlyTheChangeRequests = ["change_request"];

        private static readonly string[] MeasuredOnItInUpperCase = ["INCIDENT"];

        private static readonly string[] MeasuredOnItInTitleCase = ["Incident"];

        // A team that named no kinds of work cannot have every one of them measured. Vacuous truth
        // would otherwise report Available off an empty definition read.
        [Test]
        public void ATeamThatNamedNoKindsOfWork_ReportsNoStateMetric()
        {
            var availability = ServiceNowHistoryVerdict.From(
                HttpStatusCode.OK, carriesRecords: true, MeasuredOnNothing, MeasuredOnNothing);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        // The unreadable-answer ladder keeps the two remedies apart: a refusal needs a role, and a
        // body that was not a record set needs the instance looked at. Neither is "activate a metric".
        [Test]
        public void ARefusedAnswer_ReportsNoRightsRatherThanAMissingMetric()
        {
            Assert.That(
                ServiceNowHistoryVerdict.FromAnUnreadableAnswer(HttpStatusCode.Forbidden),
                Is.EqualTo(ServiceNowHistoryAvailability.NoRights));
        }

        [Test]
        public void AnUnreadableButUnrefusedAnswer_ReportsNoStateMetric()
        {
            Assert.That(
                ServiceNowHistoryVerdict.FromAnUnreadableAnswer(HttpStatusCode.OK),
                Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        // Bug #5621 F6. Definitions attach per record class, so measuring one of a team's two kinds
        // of work leaves the other with no dates and no transitions -- which an aggregate count
        // reported as Available.
        [Test]
        public void ADefinitionOnOnlySomeOfTheTeamsKindsOfWork_ReportsNoStateMetric()
        {
            var availability = ServiceNowHistoryVerdict.From(
                HttpStatusCode.OK, carriesRecords: true, TwoKindsOfWork, MeasuredOnIt);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        [Test]
        public void ADefinitionOnEveryKindOfWorkTheTeamNamed_ReportsAvailable()
        {
            var availability = ServiceNowHistoryVerdict.From(
                HttpStatusCode.OK, carriesRecords: true, TwoKindsOfWork, TwoKindsOfWork);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.Available));
        }

        private const string Table = "incident";

        // Bug #5621. A body that is not a record set cannot be counted, so the count it produced is
        // not evidence of a state metric -- the remedy is the one an administrator can act on.
        [Test]
        public void AnAnswerCarryingNoRecordSet_ReportsNoStateMetricRegardlessOfTheCount()
        {
            var availability = ServiceNowHistoryVerdict.From(
                HttpStatusCode.OK, carriesRecords: false, OneKindOfWork, MeasuredOnIt);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        [Test]
        public void AnInstanceMeasuringStateSpans_CanSupplyHistory()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.OK, carriesRecords: true, OneKindOfWork, MeasuredOnIt);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.Available));
        }

        // The measured signal: metric_definition and metric_instance are both 403 for every
        // read-only role, opening only at itil / itil_admin / metric_admin (SPIKE Q8).
        [Test]
        public void AnAccountRefusedTheMetricTables_LacksTheRights()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.Forbidden, carriesRecords: true, OneKindOfWork, MeasuredOnNothing);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoRights));
        }

        // Readable, but nothing measures state spans on this table — the out-of-box "Incident State
        // Duration" definition was disabled, or the customer's table never had one.
        [Test]
        public void AnInstanceMeasuringNothing_HasNoStateMetric()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.OK, carriesRecords: true, OneKindOfWork, MeasuredOnNothing);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        // A 403 outranks the count. Zero definitions came back because the read was refused, not
        // because none exist, and telling the administrator to go configure a metric they cannot
        // see would send them after the wrong thing entirely.
        [Test]
        public void ARefusedReadReturningNothing_IsAboutRightsRatherThanConfiguration()
        {
            var refused = ServiceNowHistoryVerdict.From(HttpStatusCode.Forbidden, carriesRecords: true, OneKindOfWork, MeasuredOnNothing);
            var readable = ServiceNowHistoryVerdict.From(HttpStatusCode.OK, carriesRecords: true, OneKindOfWork, MeasuredOnNothing);

            Assert.That(refused, Is.Not.EqualTo(readable),
                "Both saw zero definitions. Only one of them is a permissions problem, and the remedies are different.");
        }

        // Anything the ladder does not recognise is not evidence that history works. Assuming
        // availability from an unrecognised answer would have the connector ask for spans it cannot
        // get and report an empty history as though it were a record that never moved.
        [Test]
        public void AnAnswerNobodyExpected_IsNotTreatedAsWorking()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.InternalServerError, carriesRecords: true, OneKindOfWork, MeasuredOnNothing);

            Assert.That(availability, Is.Not.EqualTo(ServiceNowHistoryAvailability.Available));
        }

        // The same answer with definitions in it is where it gets decided. A status nobody expected
        // and a count that says history works disagree, and the status has to win: an instance that
        // echoed rows alongside a 500 would otherwise be read as a capability Lighthouse can rely on.
        [Test]
        public void AnAnswerNobodyExpected_IsNotTreatedAsWorkingEvenWhenItCarriesDefinitions()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.InternalServerError, carriesRecords: true, OneKindOfWork, MeasuredOnIt);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        // Bug #5630. `field_value_duration` is not the same thing as a definition on the STATE field,
        // and F6's per-class coverage is satisfied by either. Stock change_request carries those
        // definitions on `approval` and `type` and none on `state`, so a class nothing measures state
        // on still answers the definition read -- and every check above reports it measured. The spans
        // it returns carry labels the team never mapped, which is the only evidence there is.
        [Test]
        public void AKindOfWorkWhoseSpansTheTeamRecognisesNoneOf_IsMeasuredByNothingOnState()
        {
            var unmeasured = ServiceNowHistoryVerdict.KindsOfWorkMeasuredByNothingOnState(
                TwoKindsOfWork, TwoKindsOfWork, MeasuredOnIt);

            Assert.That(unmeasured, Is.EqualTo(OnlyTheChangeRequests));
        }

        // The per-class reading of the rule the whole-team guard already followed: an instance whose
        // records of one class simply have not moved since the definition was activated returns no
        // rows for it, and that is absence of evidence rather than evidence of absence.
        [Test]
        public void AKindOfWorkThatReturnedNoSpansAtAll_IsNotReportedAsUnmeasured()
        {
            var unmeasured = ServiceNowHistoryVerdict.KindsOfWorkMeasuredByNothingOnState(
                TwoKindsOfWork, MeasuredOnIt, MeasuredOnIt);

            Assert.That(unmeasured, Is.Empty);
        }

        [Test]
        public void ATeamWhoseEveryKindOfWorkKeptSpans_HasNothingToReport()
        {
            var unmeasured = ServiceNowHistoryVerdict.KindsOfWorkMeasuredByNothingOnState(
                TwoKindsOfWork, TwoKindsOfWork, TwoKindsOfWork);

            Assert.That(unmeasured, Is.Empty);
        }

        // A class the team never named is not its problem to hear about. Records of it can reach the
        // span read through a class the team DID name -- the read is keyed on record ids, not classes.
        [Test]
        public void AKindOfWorkTheTeamNeverNamed_IsNotReported()
        {
            var unmeasured = ServiceNowHistoryVerdict.KindsOfWorkMeasuredByNothingOnState(
                OneKindOfWork, TwoKindsOfWork, MeasuredOnIt);

            Assert.That(unmeasured, Is.Empty);
        }

        // The class names come off `sys_class_name` on one side and off the team's config on the
        // other. Comparing them case-sensitively would report a measured class as unmeasured.
        [Test]
        public void TheClassNamesAreComparedWithoutRegardToCase()
        {
            var unmeasured = ServiceNowHistoryVerdict.KindsOfWorkMeasuredByNothingOnState(
                OneKindOfWork, MeasuredOnItInUpperCase, MeasuredOnItInTitleCase);

            Assert.That(unmeasured, Is.Empty);
        }
    }
}
