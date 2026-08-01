using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5577, US-04 AC2 + ADR-118 decisions 1 and 7. The functional core of slice 04.
    //
    // Layer 1 (pure, no IO). ServiceNow reports history as spans — "this record sat in In Progress
    // from 09:00" — while Lighthouse models transitions — "it went New -> In Progress at 09:00".
    // Converting one to the other is where slice 04 is either right or quietly wrong, and it is
    // testable without an HttpMessageHandler anywhere near it.
    //
    // Every fixture uses the span shape the live instance returns: a label from `value` (never the
    // choice number in `field_value`) and a start in universal time. No `end`, no `duration` — the
    // span type does not carry them, which is ADR-118 decision 6 made structural.
    [TestFixture]
    public class ServiceNowStateSpanMapperTest
    {
        private const string Record = "7f10b53a83da4310ad56c670ceaad387";

        // A service desk that has told Lighthouse what its labels mean. Matches the label set both
        // `state` and `incident_state` were measured to carry on the live instance.
        private static Team AServiceDesk()
        {
            return new Team
            {
                Name = "Service Desk",
                ToDoStates = ["New"],
                DoingStates = ["In Progress", "On Hold"],
                DoneStates = ["Resolved", "Closed"],
            };
        }

        private static ServiceNowStateSpan ASpan(string label, string start)
        {
            return new ServiceNowStateSpan(Record, label, DateTime.Parse(start, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal));
        }

        // AC2, the headline behaviour. Three spans describe two moves, and each move is dated by the
        // arrival — the start of the span being entered, never the end of the one being left. The
        // two differ whenever the metric calculation lags, which it does by ~30s on a live instance.
        [Test]
        public void ConsecutiveSpans_BecomeTheMovesBetweenThem()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-29 06:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
            ];

            var transitions = ServiceNowStateSpanMapper.ToTransitions(spans, AServiceDesk());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(2));
                Assert.That(transitions[0].FromState, Is.EqualTo("New"));
                Assert.That(transitions[0].ToState, Is.EqualTo("In Progress"));
                Assert.That(transitions[0].TransitionedAt, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)),
                    "The move is dated by the arrival, not by the departure — they differ while the metric calculation lags.");
                Assert.That(transitions[1].FromState, Is.EqualTo("In Progress"));
                Assert.That(transitions[1].ToState, Is.EqualTo("Resolved"));
            }
        }

        // The first DISTILL question, answered: the earliest span is an arrival Lighthouse did not
        // witness, and it stays unreported.
        //
        // Spans only start once the metric definition was activated, so a record older than that
        // carries partial history and its first observed label is not necessarily the state it was
        // created in. Manufacturing a "created -> first observed label" transition would assert a
        // state the record may never have held, dated to a moment nothing measured. That is the
        // invented-data failure this epic exists to prevent, so the first span yields nothing.
        [Test]
        public void TheEarliestSpan_IsAnArrivalNobodyWitnessed_AndIsNotReportedAsAMove()
        {
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("In Progress", "2026-07-29 09:00:00")];

            var transitions = ServiceNowStateSpanMapper.ToTransitions(spans, AServiceDesk());

            Assert.That(transitions, Is.Empty,
                "One span is one arrival with nothing before it. Inventing a move into it would date a state change nothing measured.");
        }

        // The Table API's row order is not the chronological order of the spans, and nothing in the
        // response promises it is. Pairing unsorted spans produces moves that never happened.
        [Test]
        public void SpansArrivingOutOfOrder_ArePairedByWhenTheyStarted()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("Resolved", "2026-07-29 17:00:00"),
                ASpan("New", "2026-07-29 06:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
            ];

            var transitions = ServiceNowStateSpanMapper.ToTransitions(spans, AServiceDesk());

            string[] expectedMoves = ["New->In Progress", "In Progress->Resolved"];

            Assert.That(
                transitions.Select(transition => $"{transition.FromState}->{transition.ToState}"),
                Is.EqualTo(expectedMoves));
        }

        // The second DISTILL question, answered. A reopened incident produces a later span carrying
        // a label it already held, and pairing reports the journey back — which is correct, and is
        // exactly what a flow coach investigating rework needs to see.
        [Test]
        public void AReopenedRecord_ReportsTheJourneyBackOutOfDone()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-29 06:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
                ASpan("In Progress", "2026-07-30 08:00:00"),
            ];

            var transitions = ServiceNowStateSpanMapper.ToTransitions(spans, AServiceDesk());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(3));
                Assert.That(transitions[2].FromState, Is.EqualTo("Resolved"));
                Assert.That(transitions[2].ToState, Is.EqualTo("In Progress"));
                Assert.That(transitions[2].TransitionedAt, Is.EqualTo(new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc)));
            }
        }

        // AC2 requires the shared mapper, and this is what the shared mapper buys. A team that has
        // grouped two ServiceNow labels under one name did so because it considers them one state;
        // reporting a move between them would put phantom churn in every state-time chart.
        [Test]
        public void TwoLabelsTheTeamTreatsAsOneState_ProduceNoMoveBetweenThem()
        {
            var team = AServiceDesk();
            team.StateMappings = [new StateMapping { Name = "Working", States = ["In Progress", "On Hold"] }];

            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("On Hold", "2026-07-29 12:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
            ];

            var transitions = ServiceNowStateSpanMapper.ToTransitions(spans, team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(1),
                    "The team calls both labels Working, so moving between them is not a state change it recognises.");
                Assert.That(transitions[0].FromState, Is.EqualTo("Working"));
                Assert.That(transitions[0].ToState, Is.EqualTo("Resolved"));
            }
        }

        // Measured on the live PDI: `field_value_duration` is not unique to the state field. The
        // stock incident table carries four such definitions — Incident State Duration, Open (on
        // `active`), Assignment Group and Assigned to Duration — and one record returns spans from
        // all of them at once. Pairing them all reports `true -> false` and a group name as state
        // changes. A label no team mapped is not a state, and that discriminator works for a
        // customer's own definitions too, which filtering by field name never would.
        [Test]
        public void SpansMeasuringSomethingOtherThanState_ProduceNoMovesBetweenThem()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-29 06:00:00"),
                ASpan("true", "2026-07-29 06:00:00"),
                ASpan("Network Team", "2026-07-29 07:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("false", "2026-07-29 17:00:00"),
            ];

            var transitions = ServiceNowStateSpanMapper.ToTransitions(spans, AServiceDesk());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(1),
                    "Only two of these spans measure state; the others measure `active` and `assignment_group`.");
                Assert.That(transitions[0].FromState, Is.EqualTo("New"));
                Assert.That(transitions[0].ToState, Is.EqualTo("In Progress"));
            }
        }

        [Test]
        public void ARecordWithNoHistory_ReportsNoMoves()
        {
            var transitions = ServiceNowStateSpanMapper.ToTransitions([], AServiceDesk());

            Assert.That(transitions, Is.Empty);
        }

        // ADR-118 decision 7, and the reason the itil escalation is worth paying for. Before this,
        // StartedDate was opened_at — when the request was logged, which on a real service desk can
        // be days before anyone picked it up.
        [Test]
        public void WorkStarted_WhenTheRecordFirstReachedAStateTheTeamCallsDoing()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-20 06:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
            ];

            var startedAt = ServiceNowStateSpanMapper.WhenWorkStarted(spans, AServiceDesk());

            Assert.That(startedAt, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)),
                "Nine days sat in New is queue time, and counting it as work is what ADR-117 accepted only until this slice.");
        }

        // Rework must not restart the clock. Cycle Time spans the whole life of the work, and taking
        // the later visit would report a fraction of it as though the first attempt never happened.
        [Test]
        public void WorkThatReturnedToDoing_StartedTheFirstTimeItGotThere()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
                ASpan("In Progress", "2026-07-30 08:00:00"),
            ];

            var startedAt = ServiceNowStateSpanMapper.WhenWorkStarted(spans, AServiceDesk());

            Assert.That(startedAt, Is.EqualTo(new DateTime(2026, 7, 29, 9, 0, 0, DateTimeKind.Utc)));
        }

        // Bug #5621 F2, and the point where returning to the QUEUE parts company with returning from
        // Done. A reopen leaves work that was genuinely begun, so its clock must not restart -- but
        // work pushed back to New was un-started, and the attempt that counts is the one that stuck.
        // This is what Jira and Azure DevOps have always reported.
        [Test]
        public void WorkThatWentBackToTheQueueAndStartedAgain_StartedTheSecondTime()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-01 06:00:00"),
                ASpan("In Progress", "2026-07-02 09:00:00"),
                ASpan("New", "2026-07-04 11:00:00"),
                ASpan("In Progress", "2026-07-06 08:00:00"),
            ];

            var startedAt = ServiceNowStateSpanMapper.WhenWorkStarted(spans, AServiceDesk());

            Assert.That(startedAt, Is.EqualTo(new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc)),
                "The first attempt was returned to the queue rather than reworked, so counting from it would report two days of queue time as work.");
        }

        // The caller needs this to tell work that was pushed back and left there from work that was
        // pushed back and picked up again.
        [Test]
        public void WorkReturnedToTheQueue_ReportsWhenItGotThere()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-02 09:00:00"),
                ASpan("New", "2026-07-04 11:00:00"),
            ];

            var queuedAt = ServiceNowStateSpanMapper.WhenWorkWasQueued(spans, AServiceDesk());

            Assert.That(queuedAt, Is.EqualTo(new DateTime(2026, 7, 4, 11, 0, 0, DateTimeKind.Utc)));
        }

        // A reopen passes back through the team's Doing states on its way, and that is rework rather
        // than a return to the queue -- so it must not read as one.
        [Test]
        public void WorkComingBackFromDone_IsNotAReturnToTheQueue()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-02 09:00:00"),
                ASpan("Resolved", "2026-07-03 09:00:00"),
                ASpan("New", "2026-07-04 11:00:00"),
            ];

            var queuedAt = ServiceNowStateSpanMapper.WhenWorkWasQueued(spans, AServiceDesk());

            Assert.That(queuedAt, Is.Null);
        }

        // Work sitting in the queue has not started, and saying it has would put every untouched
        // ticket into Cycle Time and Work Item Age with a start date it never earned.
        [Test]
        public void WorkThatNeverLeftTheQueue_HasNotStarted()
        {
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("New", "2026-07-29 06:00:00")];

            var startedAt = ServiceNowStateSpanMapper.WhenWorkStarted(spans, AServiceDesk());

            Assert.That(startedAt, Is.Null);
        }

        // A record whose spans begin after the metric definition was activated has no span for the
        // state it was created in. The earliest span it does have is still an arrival Lighthouse
        // witnessed, even though nothing can say what preceded it -- which is why the dates cannot be
        // read off ToTransitions, whose first span deliberately yields no transition.
        [Test]
        public void WorkWhoseSpansBeginAlreadyInProgress_StartedAtTheEarliestSpan()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-02 09:00:00"),
                ASpan("Resolved", "2026-07-03 09:00:00"),
            ];

            var startedAt = ServiceNowStateSpanMapper.WhenWorkStarted(spans, AServiceDesk());

            Assert.That(startedAt, Is.EqualTo(new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc)));
        }

        // A record whose history is unreadable is a different question from a record that has not
        // started, but both arrive here as no spans. The caller decides which it is — this function
        // only reports what the spans support, and no spans support nothing.
        [Test]
        public void NoSpansAtAll_ReportNoStart()
        {
            var startedAt = ServiceNowStateSpanMapper.WhenWorkStarted([], AServiceDesk());

            Assert.That(startedAt, Is.Null);
        }

        // ADR-117 decision 1 as amended 2026-07-31. The record's own closed_at is empty on Resolved,
        // and a shop that never moves work past Resolved has no instant on the record at all — the
        // spans are the only place the day it finished exists.
        [Test]
        public void WorkFinished_WhenTheRecordReachedAStateTheTeamCallsDone()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-20 06:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
            ];

            var finishedAt = ServiceNowStateSpanMapper.WhenWorkFinished(spans, AServiceDesk());

            Assert.That(finishedAt, Is.EqualTo(new DateTime(2026, 7, 29, 17, 0, 0, DateTimeKind.Utc)));
        }

        // Where this parts company with WhenWorkStarted, and why it is not the same function with a
        // different category. Rework must not restart the clock, so work started the FIRST time it
        // reached Doing — but it finished the LAST time it reached Done, because the earlier arrival
        // was undone and reporting it would end the item's life before its second attempt began.
        [Test]
        public void WorkThatWasReopenedAndFinishedAgain_FinishedTheLastTimeItGotThere()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-29 09:00:00"),
                ASpan("Resolved", "2026-07-29 17:00:00"),
                ASpan("In Progress", "2026-07-30 08:00:00"),
                ASpan("Closed", "2026-07-31 12:00:00"),
            ];

            var finishedAt = ServiceNowStateSpanMapper.WhenWorkFinished(spans, AServiceDesk());

            Assert.That(finishedAt, Is.EqualTo(new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc)),
                "The first resolution was undone, so dating the finish by it would close the item before the second attempt even started.");
        }

        // Bug #5621 F2. A desk that maps BOTH Resolved and Closed to Done finishes the work when
        // somebody resolves it; the instance's own close-resolved job moving it a week later has
        // undone nothing. Dating the finish by the later arrival inflates Cycle Time by the whole
        // close-out window and lands the item in Throughput a week late -- for every incident on the
        // instance, since the job runs on all of them.
        [Test]
        public void WorkThatPassedThroughTwoDoneStatesInARow_FinishedAtTheFirstOfThem()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("In Progress", "2026-07-08 09:00:00"),
                ASpan("Resolved", "2026-07-10 14:00:00"),
                ASpan("Closed", "2026-07-17 03:00:00"),
            ];

            var finishedAt = ServiceNowStateSpanMapper.WhenWorkFinished(spans, AServiceDesk());

            Assert.That(finishedAt, Is.EqualTo(new DateTime(2026, 7, 10, 14, 0, 0, DateTimeKind.Utc)),
                "Resolved and Closed are both Done to this team, so moving between them crossed no boundary and finished nothing that was not already finished.");
        }

        [Test]
        public void WorkThatNeverReachedDone_HasNotFinished()
        {
            IReadOnlyList<ServiceNowStateSpan> spans =
            [
                ASpan("New", "2026-07-29 06:00:00"),
                ASpan("In Progress", "2026-07-29 09:00:00"),
            ];

            var finishedAt = ServiceNowStateSpanMapper.WhenWorkFinished(spans, AServiceDesk());

            Assert.That(finishedAt, Is.Null);
        }

        [Test]
        public void NoSpansAtAll_ReportNoFinish()
        {
            var finishedAt = ServiceNowStateSpanMapper.WhenWorkFinished([], AServiceDesk());

            Assert.That(finishedAt, Is.Null);
        }
    }
}
