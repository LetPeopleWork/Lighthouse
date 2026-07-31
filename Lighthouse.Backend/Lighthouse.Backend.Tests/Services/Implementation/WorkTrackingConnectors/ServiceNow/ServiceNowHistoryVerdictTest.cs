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
        private const string Table = "incident";

        [Test]
        public void AnInstanceMeasuringStateSpans_CanSupplyHistory()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.OK, stateSpanDefinitions: 1);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.Available));
        }

        // The measured signal: metric_definition and metric_instance are both 403 for every
        // read-only role, opening only at itil / itil_admin / metric_admin (SPIKE Q8).
        [Test]
        public void AnAccountRefusedTheMetricTables_LacksTheRights()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.Forbidden, stateSpanDefinitions: 0);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoRights));
        }

        // Readable, but nothing measures state spans on this table — the out-of-box "Incident State
        // Duration" definition was disabled, or the customer's table never had one.
        [Test]
        public void AnInstanceMeasuringNothing_HasNoStateMetric()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.OK, stateSpanDefinitions: 0);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        // A 403 outranks the count. Zero definitions came back because the read was refused, not
        // because none exist, and telling the administrator to go configure a metric they cannot
        // see would send them after the wrong thing entirely.
        [Test]
        public void ARefusedReadReturningNothing_IsAboutRightsRatherThanConfiguration()
        {
            var refused = ServiceNowHistoryVerdict.From(HttpStatusCode.Forbidden, stateSpanDefinitions: 0);
            var readable = ServiceNowHistoryVerdict.From(HttpStatusCode.OK, stateSpanDefinitions: 0);

            Assert.That(refused, Is.Not.EqualTo(readable),
                "Both saw zero definitions. Only one of them is a permissions problem, and the remedies are different.");
        }

        // Anything the ladder does not recognise is not evidence that history works. Assuming
        // availability from an unrecognised answer would have the connector ask for spans it cannot
        // get and report an empty history as though it were a record that never moved.
        [Test]
        public void AnAnswerNobodyExpected_IsNotTreatedAsWorking()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.InternalServerError, stateSpanDefinitions: 0);

            Assert.That(availability, Is.Not.EqualTo(ServiceNowHistoryAvailability.Available));
        }

        // The same answer with definitions in it is where it gets decided. A status nobody expected
        // and a count that says history works disagree, and the status has to win: an instance that
        // echoed rows alongside a 500 would otherwise be read as a capability Lighthouse can rely on.
        [Test]
        public void AnAnswerNobodyExpected_IsNotTreatedAsWorkingEvenWhenItCarriesDefinitions()
        {
            var availability = ServiceNowHistoryVerdict.From(HttpStatusCode.InternalServerError, stateSpanDefinitions: 1);

            Assert.That(availability, Is.EqualTo(ServiceNowHistoryAvailability.NoStateMetric));
        }

        // A missing capability is not a broken connection. Failing validation over it would stop an
        // administrator finishing a setup that works perfectly well for throughput and forecasting.
        [Test]
        public void AMissingCapability_DoesNotFailTheConnection()
        {
            var verdict = ServiceNowHistoryVerdict.ToValidationResult(ServiceNowHistoryAvailability.NoRights, Table);

            Assert.That(verdict.IsValid, Is.True,
                "ADR-118 D5: the advisory rides a success. ServiceNow without itil still gives throughput and a forecast.");
        }

        [Test]
        public void AnInstanceThatCanSupplyHistory_CarriesNoAdvisory()
        {
            var verdict = ServiceNowHistoryVerdict.ToValidationResult(ServiceNowHistoryAvailability.Available, Table);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True);
                Assert.That(verdict.Advisory, Is.Null, "Nothing to warn about, so nothing is said.");
                Assert.That(verdict.AdvisoryCode, Is.Null);
            }
        }

        // The advisory has to name the remedy, and the two remedies are different. This is the whole
        // reason the availability is three-valued rather than a boolean.
        [Test]
        public void TheAdvisoryForMissingRights_NamesTheRoleToGrant()
        {
            var verdict = ServiceNowHistoryVerdict.ToValidationResult(ServiceNowHistoryAvailability.NoRights, Table);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.AdvisoryCode, Is.EqualTo(ServiceNowHistoryVerdict.NoRightsCode));
                Assert.That(verdict.Advisory, Does.Contain("grant the integration account the itil role"),
                    "An administrator cannot act on 'history unavailable'. They can act on the name of a role.");
                Assert.That(verdict.Advisory, Does.Contain("ServiceNow refuses this account the metric tables"),
                    "And they can only judge whether that role is the right remedy if they are told what was refused.");
            }
        }

        [Test]
        public void TheAdvisoryForAMissingMetric_NamesTheTableAndTheMetricKind()
        {
            var verdict = ServiceNowHistoryVerdict.ToValidationResult(ServiceNowHistoryAvailability.NoStateMetric, Table);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.AdvisoryCode, Is.EqualTo(ServiceNowHistoryVerdict.NoStateMetricCode));
                Assert.That(verdict.Advisory, Does.Contain("nothing on the incident table measures how long a record spends in each state"));
                Assert.That(verdict.Advisory, Does.Contain("Activate a Field value duration metric definition on the state field of incident"),
                    "Naming the metric type, and the field of the table it has to go on, is what turns this from a complaint into an instruction.");
            }
        }

        // The honesty obligation ADR-117 made load-bearing and deferred to this slice. Whichever
        // cause fired, the administrator has to learn that the number they are about to read is
        // request-to-resolution and not time-in-progress.
        [TestCase(ServiceNowHistoryAvailability.NoRights)]
        [TestCase(ServiceNowHistoryAvailability.NoStateMetric)]
        public void WhateverTheCause_TheAdvisorySaysWhichNumberTheTeamWillGet(ServiceNowHistoryAvailability availability)
        {
            var verdict = ServiceNowHistoryVerdict.ToValidationResult(availability, Table);

            Assert.That(verdict.Advisory, Does.Contain("resolution").IgnoreCase,
                "ADR-117's honesty obligation: shipping the inflated number unqualified is what this slice exists to stop.");
        }

        // Each advisory is one sentence concatenated out of several fragments, so losing any one of
        // them still leaves a message that reads like a message. What the administrator has to be
        // told does not vary with the cause: that the connection itself is fine, what Lighthouse
        // cannot see, what to do about it, and what to do afterwards.
        [TestCase(ServiceNowHistoryAvailability.NoRights)]
        [TestCase(ServiceNowHistoryAvailability.NoStateMetric)]
        public void WhateverTheCause_TheAdvisorySaysWhatIsWrongAndWhatToDoAboutIt(ServiceNowHistoryAvailability availability)
        {
            var verdict = ServiceNowHistoryVerdict.ToValidationResult(availability, Table);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Advisory, Is.Not.Null.And.Not.Empty, "An advisory nobody can read is the silent no-op DoD 5 forbids.");
                Assert.That(verdict.Advisory, Does.Contain("The connection works, but"),
                    "Leading with the reassurance is what stops an administrator undoing a connection that is fine.");
                Assert.That(verdict.Advisory, Does.Contain("Lighthouse cannot see when work started or stopped"),
                    "The consequence, in the flow coach's words rather than ServiceNow's.");
                Assert.That(verdict.Advisory, Does.Contain("then validate the connection again to pick up true time in progress"),
                    "A remedy with no way to check it worked leaves the administrator guessing.");
            }
        }
    }
}
