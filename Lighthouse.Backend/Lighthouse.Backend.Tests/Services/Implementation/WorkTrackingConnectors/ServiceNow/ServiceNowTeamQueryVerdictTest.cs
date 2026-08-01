using System.Net;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5575, US-02 AC6. What a flow coach is told when the query they pasted is wrong.
    //
    // Layer 1 (pure, no IO). The rung that matters is the silently-widened query: ServiceNow drops
    // a term naming a field the table does not have and answers with the entire table — measured,
    // 96 rows for `not_a_real_field=whatever`, byte-identical to asking with no query at all. The
    // team's metrics are then computed over every record in the instance, and nothing anywhere
    // reports a failure.
    [TestFixture]
    public class ServiceNowTeamQueryVerdictTest
    {
        private const string Table = "incident";

        private const string TheWholeHierarchy = "task";

        private const string Changes = "change_request";

        private const string Problems = "problem";

        /// <summary>A real, readable, populated table that is not a kind of work (641 rows, none under task).</summary>
        private const string NotWork = "sys_user";

        /// <summary>The settings field every kind-of-work verdict has to send the flow coach to.</summary>
        private const string KindsOfWorkField = "WorkItemTypes";

        [Test]
        public void ATeamThatHasNotSaidWhichWorkIsTheirs_IsAskedForAQuery()
        {
            var result = ServiceNowTeamQueryVerdict.FromMissingQuery();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("missing_query"));
                Assert.That(result.Message, Is.Not.Empty);
                Assert.That(result.FieldName, Is.EqualTo("DataRetrievalValue"),
                    "The message has to point at the field on the settings page the flow coach must fix.");
            }
        }

        [Test]
        public void AQueryThatSelectsNoWork_IsToldItSelectedNoWork()
        {
            var result = ServiceNowTeamQueryVerdict.FromTeamProbe(Table, matchedCount: 0, tableTotalCount: 96);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("no_work_items_found"));
            }
        }

        // The headline. Equal counts is the only signal a read-only account can obtain, and it is
        // suspicion rather than proof — so the verdict stops the flow coach and explains, instead of
        // letting a whole-instance metric render as though it were their team's.
        [Test]
        public void AQueryThatSelectsEveryRecordInTheTable_StopsTheFlowCoachRatherThanShowingWholeInstanceMetrics()
        {
            var result = ServiceNowTeamQueryVerdict.FromTeamProbe(Table, matchedCount: 96, tableTotalCount: 96);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("query_matches_whole_table"));
            }
        }

        // Same obligation as slice 01's no_records_visible: when the platform cannot tell two causes
        // apart, Lighthouse names both rather than asserting a certainty it does not have.
        [Test]
        public void AQueryThatSelectsEverything_NamesBothPossibleCausesRatherThanGuessing()
        {
            var result = ServiceNowTeamQueryVerdict.FromTeamProbe(Table, matchedCount: 96, tableTotalCount: 96);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Message, Does.Contain(Table),
                    "The flow coach has to know which table this is about.");
                Assert.That(result.Message, Does.Contain("field"),
                    "One cause is a query naming a field the table does not have, which ServiceNow drops in silence.");
                Assert.That(result.TechnicalDetails, Is.Not.Null.And.Contains("96"),
                    "The counts that produced the suspicion belong in the support detail.");
            }
        }

        // The converse, and the reason this is a comparison rather than a blanket warning: a query
        // that genuinely narrows must go through without a false alarm, or the check trains people
        // to ignore it.
        [Test]
        public void AQueryThatSelectsOneTeamsWork_IsAccepted()
        {
            var result = ServiceNowTeamQueryVerdict.FromTeamProbe(Table, matchedCount: 24, tableTotalCount: 96);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        // Zero equals zero, and a table with nothing in it is not a widened query. Getting the rung
        // order wrong here hands an empty service desk a confusing accusation.
        [Test]
        public void AQueryAgainstATableWithNothingInIt_IsToldTheTableIsEmptyRatherThanAccused()
        {
            var result = ServiceNowTeamQueryVerdict.FromTeamProbe(Table, matchedCount: 0, tableTotalCount: 0);

            Assert.That(result.Code, Is.EqualTo("no_work_items_found"));
        }

        // ---------------------------------------------------------------------------------------
        // Story #5611, AC-B6. The kind-of-work rungs, as the pure functions they are. Until now they
        // were reached only through the connector fixture, one example each -- which is where the
        // two review findings below lived unseen.
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ATeamThatHasNotSaidWhichKindsOfWorkAreItsOwn_IsAskedForThem()
        {
            var result = ServiceNowTeamQueryVerdict.FromMissingWorkItemTypes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("missing_work_item_types"));
                Assert.That(result.Message, Does.Contain("change_request").And.Contain("Change Request"),
                    "The name-versus-label correction is the single most likely mistake, so it belongs in the message rather than in the docs.");
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // ---------------------------------------------------------------------------------------
        // The primary probe: does this kind of work contribute rows to the read Lighthouse makes?
        // One request per class when the configuration is right (ADR-124 decision 2, re-ordered).
        // ---------------------------------------------------------------------------------------

        // This probe addressed the work hierarchy, so a transport failure is about the hierarchy.
        // Naming the class instead would send the flow coach to correct a spelling that is right.
        [TestCase(HttpStatusCode.Forbidden, "insufficient_permissions", TestName = "AHierarchyTheAccountMayNotReadThroughAClassFilter_IsReportedAgainstTheHierarchy")]
        [TestCase(HttpStatusCode.BadRequest, "unknown_table", TestName = "AHierarchyTheInstanceDoesNotHave_IsReportedAgainstTheHierarchy")]
        public void ARefusalOfTheWorkHierarchy_IsReportedAgainstItRatherThanAgainstTheKindOfWork(
            HttpStatusCode status, string expectedCode)
        {
            var result = ServiceNowTeamQueryVerdict.FromWorkHierarchyProbe(
                Changes, TheWholeHierarchy, status, carriesRecords: false, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(expectedCode));
                Assert.That(result.Message, Does.Contain(TheWholeHierarchy));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField),
                    "The flow coach reached this by typing a kind of work, so that is the field to send them back to.");
            }
        }

        // A gateway that rewrites ServiceNow's own error envelope into a 200 hands the ladder a body
        // that parses and carries no record set. Read as "JSON, zero rows" that is a broken read
        // passing validation. The sync's own RecordsFrom has always refused this shape.
        [Test]
        public void AnAnswerCarryingNoRecordSetAtAll_IsNeverReportedAsReadable()
        {
            var result = ServiceNowTeamQueryVerdict.FromWorkHierarchyProbe(
                Changes, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: false, recordsTheInstanceHolds: 0, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField),
                    "Whatever the rung, a kind of work the coach typed sends them back to the field they typed it in.");
            }
        }

        // X-Total-Count against the row count is the ONLY signal that separates a kind of work this
        // account may not read from one the hierarchy holds none of, so a probe that did not get the
        // header has measured nothing. Collapsing the absent header into 0 disables the hidden rung
        // silently and reports a pass.
        [Test]
        public void AHierarchyTheInstanceWouldNotSizeAtAll_IsRefusedRatherThanPassed()
        {
            var result = ServiceNowTeamQueryVerdict.FromWorkHierarchyProbe(
                Problems, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("result_size_unknown"));
                Assert.That(result.Message, Does.Contain(TheWholeHierarchy));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // The mechanism the whole acceptance criterion rests on: X-Total-Count is ACL-blind, and the
        // blindness survives a class-scoped sysparm_query — measured, /task?sys_class_name=problem
        // reports 32 to an account shown none of them. A count above zero with an empty body is the
        // one signal that a kind of work is hidden rather than absent.
        [Test]
        public void AKindOfWorkTheHierarchyHoldsAndTheAccountCannotSee_IsReportedAsHidden()
        {
            var result = ServiceNowTeamQueryVerdict.FromWorkHierarchyProbe(
                Problems, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 24, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("class_records_not_visible"));
                Assert.That(result.Message, Does.Contain(Problems).And.Contain("role"),
                    "Both causes and the role to grant, because the platform cannot separate a class-level denial from a row-level one.");
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // The two valid answers, and they are NOT the same answer: rows means done, none means the
        // caller has to ask the class's own table why. Both come back valid because neither is yet a
        // failure — which is what makes the caller's header check load-bearing rather than defensive.
        [TestCase(105, 1, TestName = "AKindOfWorkThatContributesRows_IsAccepted")]
        [TestCase(0, 0, TestName = "AKindOfWorkTheHierarchyHoldsNoneOf_IsNotYetAFailure")]
        public void AKindOfWorkTheHierarchyHasNoObjectionTo_IsAccepted(int holds, int visible)
        {
            var result = ServiceNowTeamQueryVerdict.FromWorkHierarchyProbe(
                Changes, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, holds, visible);

            Assert.That(result.Code, Is.EqualTo("valid"));
        }

        // ---------------------------------------------------------------------------------------
        // The secondary probe: why the hierarchy holds none of it. Paid for only by the class that
        // is actually wrong.
        // ---------------------------------------------------------------------------------------

        // Rungs 1 and 2 of ADR-124 decision 2. A class IS a table, so these are the connection
        // ladder's own verdicts with a class name where the table name went -- but pointing at the
        // field the flow coach typed it in, not at the connection's.
        [TestCase(HttpStatusCode.BadRequest, false, "unknown_table", TestName = "AKindOfWorkTheInstanceDoesNotHave_IsNamedBackAsAnUnknownTable")]
        [TestCase(HttpStatusCode.Forbidden, false, "insufficient_permissions", TestName = "AKindOfWorkTheInstanceRefuses_IsNamedBackAsAPermissionsProblem")]
        [TestCase(HttpStatusCode.BadRequest, true, "unknown_table", TestName = "AKindOfWorkRefusedWithAnErrorBodyThatParses_IsStillNamedBackAsAnUnknownTable")]
        public void AKindOfWorkTheInstanceWillNotAnswerFor_KeepsTheConnectionLaddersName(HttpStatusCode status, bool carriesRecords, string expectedCode)
        {
            var result = ServiceNowTeamQueryVerdict.FromClassTableProbe(
                Changes, TheWholeHierarchy, status, carriesRecords, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(expectedCode));
                Assert.That(result.Message, Does.Contain(Changes));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // The rung the second probe exists for. Measured: /sys_user answers 641 to the same account
        // that gets header = 0 from /task?sys_class_name=sys_user. A ladder that only asked whether
        // the name resolves accepts that team, which then syncs nothing of that kind in silence.
        [Test]
        public void AKindOfWorkThatIsNotWorkAtAll_NamesBothTheKindAndTheHierarchy()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassTableProbe(
                NotWork, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 641, visibleRowCount: 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("class_is_not_a_kind_of_work"));
                Assert.That(result.Message, Does.Contain(NotWork).And.Contain(TheWholeHierarchy),
                    "Which name, and what it is not under. Either half alone is unactionable.");
                Assert.That(result.Message, Does.Not.Contain("does not exist"),
                    "The name resolves and is readable -- a name that does not gets a 400 and a better message.");
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // OQ-8's charitable reading, and the case that keeps a team on a quiet quarter saveable: both
        // probes said zero, so the class exists and the instance holds none of it anywhere.
        [Test]
        public void AKindOfWorkTheInstanceHoldsNothingOfAnywhere_IsAccepted()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassTableProbe(
                Changes, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 0, visibleRowCount: 0);

            Assert.That(result.Code, Is.EqualTo("valid"));
        }

        // The two halves of the refusal are independent. A 200 whose body is a sign-in page carries
        // no result set to count, so it has to reach the connection ladder rather than fall through
        // to a count that would then be read off a body that was never data.
        [Test]
        public void AKindOfWorkWhoseOwnTableAnsweredWithSomethingOtherThanData_IsRefusedBeforeTheCountIsRead()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassTableProbe(
                Changes, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: false, recordsTheInstanceHolds: 641, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("unexpected_response"));
                Assert.That(result.Code, Is.Not.EqualTo("class_is_not_a_kind_of_work"),
                    "X-Total-Count arrived beside a body that is not data, and a header alone cannot say the class is not work.");
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        [Test]
        public void AKindOfWorkWhoseOwnTableTheInstanceWouldNotSize_IsRefusedRatherThanPassed()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassTableProbe(
                Problems, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("result_size_unknown"));
                Assert.That(result.Message, Does.Contain(Problems));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // Slice 01 established this shape: a settings problem is never dressed up as a transport
        // problem, because the two send an administrator to entirely different people.
        [TestCase(0, 96)]
        [TestCase(96, 96)]
        [TestCase(0, 0)]
        public void AQueryProblem_IsNeverReportedAsAReachabilityOrCredentialProblem(int matched, int total)
        {
            var result = ServiceNowTeamQueryVerdict.FromTeamProbe(Table, matched, total);

            Assert.That(result.Code, Is.Not.AnyOf("connection_failed", "invalid_url", "authentication_failed", "insufficient_permissions"));
        }
    }
}
