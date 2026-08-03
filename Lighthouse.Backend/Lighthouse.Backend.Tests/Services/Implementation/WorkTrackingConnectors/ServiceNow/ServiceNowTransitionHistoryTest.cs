using System.Globalization;
using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5577, US-04 AC1 / AC2 / AC4 + ADR-118 decision 7.
    //
    // Layer 3 (real adapter, stubbed transport). The pure cores are tested next door; this file is
    // about what the connector asks a ServiceNow instance for once it wants history, and what it does
    // with the three answers it can get: spans, a refusal, or nothing that measures state.
    //
    // The stub routes by table, the way the real instance does — incident, metric_definition and
    // metric_instance are three different reads and the connector has to get all three right.
    [TestFixture]
    public class ServiceNowTransitionHistoryTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";
        private const string TeamsOwnQuery = "assignment_group.name=Service Desk^active=true";
        private const string RecordId = "7f10b53a83da4310ad56c670ceaad387";

        // A record the team calls finished, whose closed_at and whose Resolved span name different
        // instants — which is what lets one assertion tell the two sources apart (ADR-117 amended).
        private const string FinishedRecordId = "bbbb222283da4310ad56c670ceaad311";

        private const string StateSpanDefinition = "35f2b283c0a808ae000b7132cd0a4f55";

        // The same table, the same type, and it does not measure state (Bug #5621 F1).
        private const string GroupSpanDefinition = "cccc333383da4310ad56c670ceaad399";

        // A change request, on a stock instance where nothing measures state on its class (Bug #5630).
        private const string ChangeRecordId = "dddd444483da4310ad56c670ceaad422";

        private const string ChangeTypeDefinition = "eeee555583da4310ad56c670ceaad433";

        // AC1. The whole point of the slice: the connector stops answering from a constant and starts
        // answering from what the instance said.
        [Test]
        public async Task AnInstanceThatMeasuresStateSpans_IsDeclaredToSupplyHistory()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.True);
        }

        // AC4, the runtime downgrade. The rights can be revoked after a connection validated
        // perfectly well, and the sync that follows must fall back rather than fail — WorkItemService
        // only runs its sync-delta derivation when the connector declares history unsupported.
        [Test]
        public async Task AnInstanceThatRefusesTheMetricTables_DowngradesRatherThanFailing()
        {
            var instance = AnInstanceRefusing(HttpStatusCode.Forbidden);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty, "The team's work still syncs. Only the history is missing.");
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
            }
        }

        // The second cause, and it must not be reported as the first. An instance where the state
        // metric was disabled answers 200 with nothing matching.
        [Test]
        public async Task AnInstanceMeasuringNoStateSpans_DowngradesRatherThanFailing()
        {
            var instance = AnInstanceThatMeasuresNothing();
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty);
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
            }
        }

        // Bug #5621 F4. ADR-114's sign-in page: the instance answers 200 and hands back a login
        // document rather than a record set. WhenRefused.Downgrade exists so a degraded instance
        // degrades the sync instead of failing it, and it discriminated on the status code alone --
        // so this arrived as a thrown read and took the team's whole sync with it.
        [Test]
        public async Task AnInstanceAnsweringTheMetricTablesWithNoRecordSet_DowngradesRatherThanFailing()
        {
            var instance = AnInstanceAnsweringMetricsWith(ASignInPage);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty, "The team's work still syncs. Only the history is missing.");
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
            }
        }

        // The same answer arriving on the span read rather than the definition read. Reported through
        // the warning rather than absorbed: an empty span set and an unreadable one are the same
        // history to the metrics and different problems to the administrator.
        [Test]
        public async Task AnInstanceAnsweringTheSpanReadWithNoRecordSet_SaysSoRatherThanGoingQuiet()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var instance = AnInstanceAnsweringMetricsWith(ASignInPage, onlyTheSpanRead: true);
            var subject = CreateSubject(instance, logger);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty);
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
            }

            VerifyWarnsThatHistoryIsUnavailable(logger);
        }

        // Bug #5621 F2, the queue-return guard. Work pushed back to New after it started has not
        // started: the start it had was for an attempt that was abandoned, and counting from it would
        // report the days it sat back in the queue as work. Jira and Azure DevOps drop the start date
        // in exactly this shape, and the zero-length-cycle rule then supplies the finish instant.
        [Test]
        public async Task WorkPushedBackToTheQueueAfterStarting_DoesNotKeepTheStartItHad()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true, spansReturnToTheQueue: true);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000003");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc)));
                Assert.That(workItem.StartedDate, Is.EqualTo(workItem.ClosedDate),
                    "The 07-28 arrival in Doing was undone by the return to New on 07-29, so nothing supports a start before the finish.");
            }
        }

        // Bug #5621 F2. A record the team currently calls Doing carries no finish date even though its
        // spans hold a Done arrival -- it was reopened, and the current state is what decides whether
        // the work is over. Without the category gate the spans alone would close it.
        [Test]
        public async Task WorkThatWasReopened_CarriesNoFinishDateWhileItIsBeingWorkedAgain()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true, spansShowAReopen: true);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            Assert.That(workItem.ClosedDate, Is.Null,
                "It is In Progress again. A Done span in its history is where it has been, not where it is.");
        }

        // Bug #5621 F2. A desk that resolves an incident straight out of the queue never puts it in a
        // Doing state, so the spans support no start. Jira and Azure DevOps answer that with a
        // zero-length cycle rather than a null, because a null drops the item out of Cycle Time
        // altogether -- and an item that demonstrably finished belongs in the metric.
        [Test]
        public async Task WorkFinishedWithoutEverBeingObservedInDoing_StartsWhenItFinished()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true, spansSkipDoing: true);
            var subject = CreateSubject(instance);

            var finished = (await subject.GetWorkItemsForTeam(ATeam()))
                .Single(item => item.ReferenceId == "INC0000003");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(finished.ClosedDate, Is.Not.Null);
                Assert.That(finished.StartedDate, Is.EqualTo(finished.ClosedDate),
                    "The spans say it finished and never say it started, so the cycle is zero -- not absent.");
            }
        }

        // DoD 5 forbids the silent no-op. A team quietly losing time-in-state reads as a team whose
        // work never moves, and the administrator has no way to discover why.
        [Test]
        public async Task DowngradingHistory_SaysSoRatherThanGoingQuiet()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var subject = CreateSubject(AnInstanceRefusing(HttpStatusCode.Forbidden), logger);

            await subject.GetWorkItemsForTeam(ATeam());

            VerifyWarnsThatHistoryIsUnavailable(logger);
        }

        // ADR-118 D2. The definitions have to be resolved before the spans are asked for, or the
        // span read cannot be restricted to the ones that measure state.
        [Test]
        public async Task ReadingHistory_ResolvesTheStateMetricBeforeAskingForSpans()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var asked = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri)).ToList();
            var definitionRead = asked.FindIndex(uri => uri.Contains("/api/now/table/metric_definition", StringComparison.Ordinal));
            var spanRead = asked.FindIndex(uri => uri.Contains("/api/now/table/metric_instance", StringComparison.Ordinal));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitionRead, Is.GreaterThanOrEqualTo(0), "Without this read there is nothing to filter the spans by.");
                Assert.That(spanRead, Is.GreaterThan(definitionRead));
            }
        }

        // SPIKE Q7: ~600ms per call and no rate limiting, so the constraint is wall clock. One call
        // per work item would turn a 500-item sync into five minutes.
        [Test]
        public async Task ReadingHistory_AsksForEveryRecordAtOnceRatherThanOneAtATime()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var spanReads = instance.Requests
                .Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri))
                .Where(uri => uri.Contains("/api/now/table/metric_instance", StringComparison.Ordinal))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spanReads, Has.Count.EqualTo(1), "Three records fit in one batch of 200.");
                Assert.That(spanReads[0], Does.Contain("idIN"), "The batch is an IN list of sys_ids, which is what makes one call enough.");
            }
        }

        // AC2 end to end. The pure mapper being right is worth nothing if the connector never hangs
        // the transitions on the work items.
        [Test]
        public async Task WorkSyncedWithHistory_CarriesTheMovesItMade()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.SyncedTransitions, Is.Not.Empty);
                Assert.That(workItem.SyncedTransitions.Select(transition => transition.ToState), Does.Contain("In Progress"));
            }
        }

        // ADR-118 decision 7, the reason the itil escalation is worth paying for. opened_at is nine
        // days before work began in this fixture, and counting that as work is what ADR-117 accepted
        // only until this slice existed.
        [Test]
        public async Task WhenHistoryIsAvailable_WorkStartedWhenItReachedDoing()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)),
                "Not opened_at. The span is when someone actually picked the work up.");
        }

        // The other half of decision 7, and the maintainer's ruling: without rights or without a
        // metric, ADR-117's opened_at is still the honest answer and must not disappear.
        [Test]
        public async Task WhenHistoryIsUnavailable_WorkStartedWhenTheRequestArrived()
        {
            var instance = AnInstanceRefusing(HttpStatusCode.Forbidden);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000001");

            Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 20, 6, 0, 0, DateTimeKind.Utc)),
                "ADR-117's fallback. Inflated by queue time, and the only thing the record itself supports.");
        }

        // ADR-117 decision 1 as amended 2026-07-31, the counterpart of decision 7. Where the spans
        // exist they say when the work reached Done, and they outrank closed_at — which is what makes
        // a shop that never moves a record past Resolved measurable at all.
        [Test]
        public async Task WhenHistoryIsAvailable_WorkFinishedWhenItReachedDone()
        {
            var instance = AnInstanceThatMeasuresStateSpans();
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000003");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc)),
                    "Not closed_at, which this record puts a day later. The span is when the work actually stopped.");
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)),
                    "And its own arrival in Doing, a day earlier -- the two dates are not interchangeable.");
            }
        }

        [Test]
        public async Task WhenHistoryIsUnavailable_WorkFinishedWhenTheRecordSaysItClosed()
        {
            var instance = AnInstanceRefusing(HttpStatusCode.Forbidden);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000003");

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 31, 15, 0, 0, DateTimeKind.Utc)),
                "ADR-117's fallback. closed_at is the only instant on the record that means the work is over.");
        }

        // Bug #5621 F1, the whole-team half. A customer deactivates "Incident State Duration" and
        // leaves the other stock definitions -- assignment_group, assigned_to, active -- active. The
        // definition count stays above zero, so the read reported Available, SupportsTransitionHistory
        // stayed true, WorkItemService skipped its synthetic-transition fallback, and every item on
        // the team got no dates and no transitions with no warning anywhere. Which definition measures
        // state cannot be read off the definition row -- the state field is named differently on every
        // record class -- so it is answered by what came back.
        [Test]
        public async Task AnInstanceWhoseDefinitionsMeasureSomethingOtherThanState_DowngradesRatherThanGoingQuiet()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true, spansMeasureSomethingElse: true);
            var subject = CreateSubject(instance, logger);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty, "The team's work still syncs. Only the history is missing.");
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False,
                    "Nothing the team recognises was measured, so WorkItemService has to derive transitions from its own sync interval instead.");
            }

            VerifyWarnsThatHistoryIsUnavailable(logger);
        }

        // Bug #5621, from the review of the first fix. An instance whose records simply have not
        // moved since the definition was activated answers the span read with an empty result. That
        // is absence of evidence, not evidence that nothing measures state -- and it is exactly the
        // state an administrator is in on the sync right after they act on Lighthouse's own warning,
        // so reporting it would tell them to activate a definition they just activated.
        [Test]
        public async Task AnInstanceThatMeasuresStateButHasRecordedNoSpansYet_KeepsReportingHistoryAsAvailable()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true, spanReadIsEmpty: true);
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.True,
                "The definition is there and readable. Nothing has crossed it yet, which is a quiet team rather than a broken one.");
        }

        // Bug #5621 F6. Definitions attach to concrete record classes and never to a base table
        // (ADR-123 D9), so a team naming two kinds of work needs one on each. The verdict asked only
        // whether the total was above zero, so a team covering incident and change_request where only
        // incident is configured reported Available -- and every change_request silently lost its
        // dates. This needs no misconfiguration at all, only an instance where one class was set up.
        [Test]
        public async Task AnInstanceMeasuringOnlySomeOfTheTeamsRecordClasses_DowngradesRatherThanClaimingHistory()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true);
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamWorkingOnTwoKindsOfRecord());

            Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False,
                "The stub measures incident only. change_request would sync with no dates and no transitions, and saying history is available would hide that.");
        }

        // ...but the class that IS measured keeps its real dates. The verdict decides what the
        // administrator is told, not whether measurements already in hand are thrown away.
        [Test]
        public async Task AnInstanceMeasuringOnlySomeOfTheTeamsRecordClasses_StillDatesTheClassItMeasures()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeamWorkingOnTwoKindsOfRecord()))
                .First(item => item.ReferenceId == "INC0000003");

            Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc)),
                "The span, not closed_at a day later. Suppressing the whole span read over an unmeasured sibling class would have cost this incident its true finish date.");
        }

        // Bug #5630, one level finer than F6. Stock change_request carries `field_value_duration`
        // definitions on `approval` and `type` and none on `state`, so per-class coverage is satisfied
        // by a definition that measures nothing anyone maps: the verdict read Available, every Change
        // Request synced with no time in state, and nothing said so. The definition row cannot declare
        // what it measures -- the state field is named differently on each class -- so the evidence is
        // which classes' spans survived the team's own state mapping, per class rather than per team.
        [Test]
        public async Task AKindOfWorkMeasuredOnlyOnSomethingOtherThanState_IsNamedInAWarning()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var instance = AnInstanceHolding(
                RecordsOfTwoClasses(), measuresStateSpans: true, changeRequestsAreMeasuredOnTypeOnly: true);
            var subject = CreateSubject(instance, logger);

            await subject.GetWorkItemsForTeam(ATeamThatTypedItsKindsOfWorkAsLabels());

            VerifyWarnsThatNothingMeasuresStateOn(logger, "Change Request");
        }

        // What the corrected verdict changes is what the administrator is TOLD. Downgrading the whole
        // team would hand the measured class a synthetic transition dated at sync time, and
        // DeriveCurrentStateEnteredAt takes the latest arrival -- so the class that works today would
        // start reporting its time in state from the sync rather than from the move. Filling the
        // unmeasured class's gap needs per-class capability, which the port does not carry.
        [Test]
        public async Task AKindOfWorkMeasuredOnlyOnSomethingOtherThanState_LeavesTheMeasuredClassAlone()
        {
            var instance = AnInstanceHolding(
                RecordsOfTwoClasses(), measuresStateSpans: true, changeRequestsAreMeasuredOnTypeOnly: true);
            var subject = CreateSubject(instance);

            var workItems = (await subject.GetWorkItemsForTeam(ATeamThatTypedItsKindsOfWorkAsLabels())).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.True,
                    "The incident's spans are real and WorkItemService must not start deriving over them.");
                Assert.That(workItems.First(item => item.ReferenceId == "INC0000001").SyncedTransitions, Is.Not.Empty,
                    "The measured class keeps the moves the instance recorded.");
                Assert.That(workItems.First(item => item.ReferenceId == "CHG0000001").SyncedTransitions, Is.Empty,
                    "A change type is not a state, and pairing those labels would report moves the record never made.");
            }
        }

        // The guard, not the message: with every kind of work measured there is nothing to name, and a
        // warning naming an empty list is the same defect as the unmapped-state warning this feature
        // deleted — noise that teaches the reader to stop reading the channel.
        [Test]
        public async Task AnInstanceMeasuringStateOnEveryKindOfWork_IsNotWarnedAbout()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true);
            var subject = CreateSubject(instance, logger);

            await subject.GetWorkItemsForTeam(ATeam());

            logger.Verify(
                call => call.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((message, _) =>
                        message.ToString()!.Contains(
                            "because the spans it measures on those records are not states the team mapped",
                            StringComparison.Ordinal)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never,
                "No kind of work is unmeasured, so the per-class warning has nothing to name. The team-level history warning is a different message and is not asserted here.");
        }

        // Bug #5621 F1, the per-record half, on a correctly configured instance. metric_definition
        // answers with four field_value_duration definitions for incident on a stock PDI, and only
        // one of them measures state -- the others measure assignment_group, assigned_to and active.
        // A record closed before the state definition was activated, whose group was changed
        // afterwards, therefore HAS spans and has no STATE span. The connector asked "has spans" and
        // the mapper asked "has a state span", so it took the span branch and returned null for both
        // dates on a record whose opened_at and closed_at were sitting in the answer it already had.
        [Test]
        public async Task WorkWhoseOnlySpansMeasureSomethingOtherThanState_KeepsTheRecordsOwnDates()
        {
            var instance = AnInstanceHolding(ThreeRecords(), measuresStateSpans: true, spansMeasureSomethingElse: true);
            var subject = CreateSubject(instance);

            var workItem = (await subject.GetWorkItemsForTeam(ATeam())).First(item => item.ReferenceId == "INC0000003");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 31, 15, 0, 0, DateTimeKind.Utc)),
                    "No state span was measured for this record, so ADR-117's closed_at is the honest answer -- exactly as if no span had come back at all.");
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2026, 7, 20, 6, 0, 0, DateTimeKind.Utc)),
                    "And opened_at for the start, for the same reason.");
                Assert.That(workItem.SyncedTransitions, Is.Empty,
                    "A group change is not a state change, and pairing those labels would report moves the record never made.");
            }
        }

        // The companion to that guard on the other axis: with no definitions to restrict it to, a span
        // read would carry an empty `definitionIN` and match every span in the instance.
        [Test]
        public async Task AnInstanceMeasuringNothing_AsksForNoSpansAtAll()
        {
            var instance = AnInstanceThatMeasuresNothing();
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var spanReads = instance.Requests
                .Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri))
                .Where(uri => uri.Contains("/api/now/table/metric_instance", StringComparison.Ordinal))
                .ToList();

            Assert.That(spanReads, Is.Empty,
                "An empty definitionIN list is an unfiltered read of every span the instance holds.");
        }

        // A team whose query matched nothing must not ask for the history of every record in the
        // instance — an unfiltered idIN is an unfiltered read.
        [Test]
        public async Task ATeamWithNoWork_AsksForNoHistoryAtAll()
        {
            var instance = AnInstanceHolding([], measuresStateSpans: true);
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(
                instance.Requests.Where(uri => uri.AbsoluteUri.Contains("metric_instance", StringComparison.Ordinal)),
                Is.Empty);
        }

        // Definitions attach per class, so a team spanning two of them is the case the aggregate
        // count could not answer (Bug #5621 F6).
        // Any warning would satisfy these otherwise, and the sync emits several -- the
        // unmapped-states one in particular would let them pass while the history warning never fired.
        private static void VerifyWarnsThatHistoryIsUnavailable(Mock<ILogger<ServiceNowWorkTrackingConnector>> logger)
        {
            logger.Verify(
                call => call.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((message, _) =>
                        message.ToString()!.Contains("no transition history", StringComparison.Ordinal)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        // Bug #5630. Distinct from the history warning above, which is per team and says nothing
        // about which class lost its dates -- and which does not fire at all in this case.
        private static void VerifyWarnsThatNothingMeasuresStateOn(
            Mock<ILogger<ServiceNowWorkTrackingConnector>> logger, string kindOfWork)
        {
            logger.Verify(
                call => call.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((message, _) =>
                        message.ToString()!.Contains("no transition history", StringComparison.Ordinal)
                        && message.ToString()!.Contains(kindOfWork, StringComparison.Ordinal)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // ADR-128: a coach types the label they read on their own ServiceNow screen, and the warning
        // has to come back in those words rather than in the class the Table API filters on.
        private static Team ATeamThatTypedItsKindsOfWorkAsLabels()
        {
            var team = ATeam();
            team.WorkItemTypes = ["Incident", "Change Request"];

            return team;
        }

        private static Team ATeamWorkingOnTwoKindsOfRecord()
        {
            var team = ATeam();
            team.WorkItemTypes = ["incident", "change_request"];

            return team;
        }

        private static Team ATeam()
        {
            return new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = TeamsOwnQuery,
                // Every ServiceNow team names the kinds of work it handles (#5611); Team's own
                // Jira-shaped default is one this connector never sees.
                WorkItemTypes = ["incident"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AConnection(),
            };
        }

        private static WorkTrackingSystemConnection AConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = InstanceUrl },
            ]);

            return connection;
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(
            StubbedInstance instance, Mock<ILogger<ServiceNowWorkTrackingConnector>>? logger = null)
        {
            var authStrategy = new Mock<IWorkTrackingAuthStrategy>();
            authStrategy
                .Setup(strategy => strategy.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(authStrategy.Object);

            return new ServiceNowWorkTrackingConnector(
                (logger ?? new Mock<ILogger<ServiceNowWorkTrackingConnector>>()).Object,
                factory.Object,
                instance.Handler);
        }

        private static StubbedInstance AnInstanceThatMeasuresStateSpans()
        {
            return AnInstanceHolding(ThreeRecords(), measuresStateSpans: true);
        }

        private static StubbedInstance AnInstanceThatMeasuresNothing()
        {
            return AnInstanceHolding(ThreeRecords(), measuresStateSpans: false);
        }

        private static StubbedInstance AnInstanceRefusing(HttpStatusCode statusCode)
        {
            return AnInstanceHolding(ThreeRecords(), measuresStateSpans: false, metricStatusCode: statusCode);
        }

        // A 200 whose body is not a record set at all. The team's own table still answers normally,
        // so the sync itself is healthy and only the history read is degraded.
        private static StubbedInstance AnInstanceAnsweringMetricsWith(string body, bool onlyTheSpanRead = false)
        {
            return new StubbedInstance(
                ThreeRecords(),
                measuresStateSpans: true,
                HttpStatusCode.OK,
                metricBody: body,
                metricBodyOnTheSpanReadOnly: onlyTheSpanRead);
        }

        private static StubbedInstance AnInstanceHolding(
            List<string> records,
            bool measuresStateSpans,
            HttpStatusCode metricStatusCode = HttpStatusCode.OK,
            bool spansSkipDoing = false,
            bool spansMeasureSomethingElse = false,
            bool spanReadIsEmpty = false,
            bool spansReturnToTheQueue = false,
            bool spansShowAReopen = false,
            bool changeRequestsAreMeasuredOnTypeOnly = false)
        {
            return new StubbedInstance(
                records,
                measuresStateSpans,
                metricStatusCode,
                spansSkipDoing: spansSkipDoing,
                spansMeasureSomethingElse: spansMeasureSomethingElse,
                spanReadIsEmpty: spanReadIsEmpty,
                spansReturnToTheQueue: spansReturnToTheQueue,
                spansShowAReopen: spansShowAReopen,
                changeRequestsAreMeasuredOnTypeOnly: changeRequestsAreMeasuredOnTypeOnly);
        }

        // What a reverse proxy in front of an SSO-protected instance hands back on a 200: the
        // instance said yes, and returned a page instead of data (ADR-114).
        private const string ASignInPage =
            "<!DOCTYPE html><html><head><title>Sign in</title></head><body><form action=\"/login\"></form></body></html>";

        private static List<string> ThreeRecords()
        {
            return
            [
                ARecord("INC0000001", RecordId, "In Progress"),
                ARecord("INC0000002", "aaaa1111", "New"),
                ARecord("INC0000003", FinishedRecordId, "Resolved", closedAt: "2026-07-31 15:00:00"),
            ];
        }

        // The two classes of Bug #5630: one the instance measures state on, one it does not.
        private static List<string> RecordsOfTwoClasses()
        {
            return
            [
                ARecord("INC0000001", RecordId, "In Progress"),
                ARecord("CHG0000001", ChangeRecordId, "In Progress", recordClass: "change_request"),
            ];
        }

        // opened_at is deliberately nine days before the In Progress span: that gap is the queue time
        // ADR-117 has been counting as work, and the thing this slice removes.
        private static string ARecord(
            string number, string sysId, string state, string closedAt = "", string recordClass = "incident")
        {
            return $$"""
                {
                  "sys_id": { "display_value": "{{sysId}}", "value": "{{sysId}}" },
                  "sys_class_name": { "display_value": "{{recordClass}}", "value": "{{recordClass}}" },
                  "number": { "display_value": "{{number}}", "value": "{{number}}" },
                  "short_description": { "display_value": "Request {{number}}", "value": "Request {{number}}" },
                  "state": { "display_value": "{{state}}", "value": "2" },
                  "sys_created_on": { "display_value": "2026-07-19 23:00:00", "value": "2026-07-20 06:00:00" },
                  "opened_at": { "display_value": "2026-07-19 23:00:00", "value": "2026-07-20 06:00:00" },
                  "closed_at": { "display_value": "{{closedAt}}", "value": "{{closedAt}}" }
                }
                """;
        }

        // Routes by table the way the instance does. The three reads are genuinely different
        // requests, and a connector that conflated them would still look plausible against a stub
        // that answered everything the same way.
        internal sealed class StubbedInstance
        {
            private readonly List<string> records;
            private readonly bool measuresStateSpans;
            private readonly HttpStatusCode metricStatusCode;
            private readonly string? metricBody;
            private readonly bool metricBodyOnTheSpanReadOnly;
            private readonly bool spansSkipDoing;
            private readonly bool spansMeasureSomethingElse;
            private readonly bool spanReadIsEmpty;
            private readonly bool spansReturnToTheQueue;
            private readonly bool spansShowAReopen;
            private readonly bool changeRequestsAreMeasuredOnTypeOnly;

            public StubbedInstance(
                List<string> records,
                bool measuresStateSpans,
                HttpStatusCode metricStatusCode,
                string? metricBody = null,
                bool metricBodyOnTheSpanReadOnly = false,
                bool spansSkipDoing = false,
                bool spansMeasureSomethingElse = false,
                bool spanReadIsEmpty = false,
                bool spansReturnToTheQueue = false,
                bool spansShowAReopen = false,
                bool changeRequestsAreMeasuredOnTypeOnly = false)
            {
                this.changeRequestsAreMeasuredOnTypeOnly = changeRequestsAreMeasuredOnTypeOnly;
                this.spansSkipDoing = spansSkipDoing;
                this.spansMeasureSomethingElse = spansMeasureSomethingElse;
                this.spanReadIsEmpty = spanReadIsEmpty;
                this.spansReturnToTheQueue = spansReturnToTheQueue;
                this.spansShowAReopen = spansShowAReopen;
                this.records = records;
                this.measuresStateSpans = measuresStateSpans;
                this.metricStatusCode = metricStatusCode;
                this.metricBody = metricBody;
                this.metricBodyOnTheSpanReadOnly = metricBodyOnTheSpanReadOnly;

                Requests = [];

                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                handler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => Answer(request));

                Handler = handler.Object;
            }

            public HttpMessageHandler Handler { get; }

            public List<Uri> Requests { get; }

            private HttpResponseMessage Answer(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri(InstanceUrl);
                Requests.Add(uri);

                var path = uri.AbsolutePath;

                if (path.Contains("metric_definition", StringComparison.Ordinal)
                    || path.Contains("metric_instance", StringComparison.Ordinal))
                {
                    return MetricAnswer(path);
                }

                return Rows(records);
            }

            private HttpResponseMessage MetricAnswer(string path)
            {
                var readIsTheOneSubstituted =
                    !metricBodyOnTheSpanReadOnly || path.Contains("metric_instance", StringComparison.Ordinal);

                if (metricBody is not null && readIsTheOneSubstituted)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(metricBody, Encoding.UTF8, "text/html"),
                    };
                }

                if (metricStatusCode != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(metricStatusCode)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                }

                if (!measuresStateSpans)
                {
                    return Rows([]);
                }

                if (path.Contains("metric_definition", StringComparison.Ordinal))
                {
                    // The stock incident table answers with four of these, and DefinitionQueryFor
                    // filters on table and type only -- so the ones measuring something other than
                    // state come back too, and their spans are asked for alongside the real ones.
                    if (changeRequestsAreMeasuredOnTypeOnly)
                    {
                        return Rows([ADefinition(), AChangeTypeDefinition()]);
                    }

                    return spansMeasureSomethingElse
                        ? Rows([ADefinition(), AGroupDurationDefinition()])
                        : Rows([ADefinition()]);
                }

                if (changeRequestsAreMeasuredOnTypeOnly)
                {
                    return Rows(
                    [
                        ASpan(RecordId, "New", "2026-07-20 06:00:00"),
                        ASpan(RecordId, "In Progress", "2026-07-29 09:00:00"),
                        AChangeTypeSpan(ChangeRecordId, "Normal", "2026-07-28 09:00:00"),
                    ]);
                }

                if (spanReadIsEmpty)
                {
                    return Rows([]);
                }

                if (spansReturnToTheQueue)
                {
                    return Rows(
                    [
                        ASpan(FinishedRecordId, "In Progress", "2026-07-28 09:00:00"),
                        ASpan(FinishedRecordId, "New", "2026-07-29 09:00:00"),
                        ASpan(FinishedRecordId, "Resolved", "2026-07-30 10:00:00"),
                    ]);
                }

                if (spansShowAReopen)
                {
                    return Rows(
                    [
                        ASpan(RecordId, "In Progress", "2026-07-20 06:00:00"),
                        ASpan(RecordId, "Resolved", "2026-07-25 10:00:00"),
                        ASpan(RecordId, "In Progress", "2026-07-29 09:00:00"),
                    ]);
                }

                if (spansMeasureSomethingElse)
                {
                    // The record was closed before the state definition existed. All it has is the
                    // group change somebody made during a later queue cleanup.
                    return Rows([AGroupSpan(FinishedRecordId, "Service Desk", "2026-07-31 16:00:00")]);
                }

                if (spansSkipDoing)
                {
                    return Rows(
                    [
                        ASpan(FinishedRecordId, "New", "2026-07-20 06:00:00"),
                        ASpan(FinishedRecordId, "Resolved", "2026-07-30 10:00:00"),
                    ]);
                }

                return Rows(
                    [
                        ASpan(RecordId, "New", "2026-07-20 06:00:00"),
                        ASpan(RecordId, "In Progress", "2026-07-29 09:00:00"),
                        ASpan(FinishedRecordId, "In Progress", "2026-07-29 09:00:00"),
                        ASpan(FinishedRecordId, "Resolved", "2026-07-30 10:00:00"),
                    ]);
            }

            private static string ADefinition()
            {
                return $$"""
                    {
                      "sys_id": { "display_value": "{{StateSpanDefinition}}", "value": "{{StateSpanDefinition}}" },
                      "name": { "display_value": "Incident State Duration", "value": "Incident State Duration" },
                      "type": { "display_value": "Field value duration", "value": "field_value_duration" },
                      "field": { "display_value": "incident_state", "value": "incident_state" },
                      "table": { "display_value": "incident", "value": "incident" }
                    }
                    """;
            }

            // Stock, active, and not a state metric: assignment_group is a reference field, so its
            // spans carry group names where the state definition's carry state labels.
            private static string AGroupDurationDefinition()
            {
                return $$"""
                    {
                      "sys_id": { "display_value": "{{GroupSpanDefinition}}", "value": "{{GroupSpanDefinition}}" },
                      "name": { "display_value": "Assignment Group Duration", "value": "Assignment Group Duration" },
                      "type": { "display_value": "Field value duration", "value": "field_value_duration" },
                      "field": { "display_value": "assignment_group", "value": "assignment_group" },
                      "table": { "display_value": "incident", "value": "incident" }
                    }
                    """;
            }

            // Stock change_request ships `field_value_duration` on `approval` and `type` and none on
            // `state` (dev191338, 2026-08-01). The definition read cannot tell this from a state one.
            private static string AChangeTypeDefinition()
            {
                return $$"""
                    {
                      "sys_id": { "display_value": "{{ChangeTypeDefinition}}", "value": "{{ChangeTypeDefinition}}" },
                      "name": { "display_value": "Change Type Duration", "value": "Change Type Duration" },
                      "type": { "display_value": "Field value duration", "value": "field_value_duration" },
                      "field": { "display_value": "type", "value": "type" },
                      "table": { "display_value": "change_request", "value": "change_request" }
                    }
                    """;
            }

            private static string AChangeTypeSpan(string record, string changeType, string start)
            {
                return $$"""
                    {
                      "id": { "display_value": "Change Request", "value": "{{record}}" },
                      "definition": { "display_value": "Change Type Duration", "value": "{{ChangeTypeDefinition}}" },
                      "field": { "display_value": "type", "value": "type" },
                      "value": { "display_value": "{{changeType}}", "value": "{{changeType}}" },
                      "field_value": { "display_value": "1", "value": "1" },
                      "start": { "display_value": "{{start}}", "value": "{{start}}" },
                      "end": { "display_value": "", "value": "" }
                    }
                    """;
            }

            private static string AGroupSpan(string record, string group, string start)
            {
                return $$"""
                    {
                      "id": { "display_value": "Incident", "value": "{{record}}" },
                      "definition": { "display_value": "Assignment Group Duration", "value": "{{GroupSpanDefinition}}" },
                      "field": { "display_value": "assignment_group", "value": "assignment_group" },
                      "value": { "display_value": "{{group}}", "value": "{{group}}" },
                      "field_value": { "display_value": "1", "value": "1" },
                      "start": { "display_value": "{{start}}", "value": "{{start}}" },
                      "end": { "display_value": "", "value": "" }
                    }
                    """;
            }

            private static string ASpan(string record, string label, string start)
            {
                return $$"""
                    {
                      "id": { "display_value": "Incident", "value": "{{record}}" },
                      "definition": { "display_value": "Incident State Duration", "value": "{{StateSpanDefinition}}" },
                      "field": { "display_value": "incident_state", "value": "incident_state" },
                      "value": { "display_value": "{{label}}", "value": "{{label}}" },
                      "field_value": { "display_value": "1", "value": "1" },
                      "start": { "display_value": "{{start}}", "value": "{{start}}" },
                      "end": { "display_value": "", "value": "" }
                    }
                    """;
            }

            private static HttpResponseMessage Rows(List<string> rows)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"result\":[{string.Join(",", rows)}]}}", Encoding.UTF8, "application/json"),
                };

                response.Headers.TryAddWithoutValidation("X-Total-Count", rows.Count.ToString(CultureInfo.InvariantCulture));

                return response;
            }
        }
    }
}
