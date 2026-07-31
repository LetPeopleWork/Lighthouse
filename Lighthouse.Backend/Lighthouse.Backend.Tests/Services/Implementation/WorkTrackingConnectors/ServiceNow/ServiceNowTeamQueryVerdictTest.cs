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

        // Rungs 1 and 2 of ADR-124 decision 2. A class IS a table, so these are the connection
        // ladder's own verdicts with a class name where the table name went -- but pointing at the
        // field the flow coach typed it in, not at the connection's.
        [TestCase(HttpStatusCode.BadRequest, "unknown_table", TestName = "AKindOfWorkTheInstanceDoesNotHave_IsNamedBackAsAnUnknownTable")]
        [TestCase(HttpStatusCode.Forbidden, "insufficient_permissions", TestName = "AKindOfWorkTheInstanceRefuses_IsNamedBackAsAPermissionsProblem")]
        public void AKindOfWorkTheInstanceWillNotAnswerFor_KeepsTheConnectionLaddersName(HttpStatusCode status, string expectedCode)
        {
            var result = ServiceNowTeamQueryVerdict.FromClassProbe(
                Changes, status, carriesRecords: false, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(expectedCode));
                Assert.That(result.Message, Does.Contain(Changes));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // Rung 4, and the mechanism the whole acceptance criterion rests on: X-Total-Count is
        // ACL-blind, so a count above zero with an empty body is the one signal that a kind of work
        // is hidden rather than empty.
        [Test]
        public void AKindOfWorkTheInstanceHoldsAndTheAccountCannotSee_IsReportedAsHidden()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassProbe(
                Problems, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 24, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("class_records_not_visible"));
                Assert.That(result.Message, Does.Contain(Problems).And.Contain("role"),
                    "Both causes and the role to grant, because the platform cannot separate a class-level denial from a row-level one.");
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // Rung 5 and the readable rung. An empty kind of work is a legitimate configuration (OQ-8) --
        // refusing it would block a team on a quiet quarter.
        [TestCase(103, 1, TestName = "AKindOfWorkTheAccountCanRead_IsAccepted")]
        [TestCase(0, 0, TestName = "AKindOfWorkWithNothingInItYet_IsAccepted")]
        public void AKindOfWorkThisAccountHasNoProblemWith_IsAccepted(int holds, int visible)
        {
            var result = ServiceNowTeamQueryVerdict.FromClassProbe(
                Changes, HttpStatusCode.OK, carriesRecords: true, holds, visible);

            Assert.That(result.Code, Is.EqualTo("valid"));
        }

        // A gateway that rewrites ServiceNow's own 400 error envelope into a 200 hands the ladder a
        // body that parses and carries no record set. Read as "JSON, zero rows" that is a misspelt
        // class passing validation, after which the team syncs a subset with nothing logged. The
        // sync's own RecordsFrom has always refused this shape; the class probe now agrees.
        [Test]
        public void AKindOfWorkWhoseAnswerCarriesNoRecordSetAtAll_IsNeverReportedAsReadable()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassProbe(
                Changes, HttpStatusCode.OK, carriesRecords: false, recordsTheInstanceHolds: 0, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField),
                    "Whatever the rung, a kind of work the coach typed sends them back to the field they typed it in.");
            }
        }

        // X-Total-Count against the row count is the ONLY signal that separates a class this account
        // may not read from one that is genuinely empty, so a probe that did not get the header has
        // measured nothing. Collapsing the absent header into 0 disables rung 4 silently and reports
        // a pass -- the same proxy CountRows already refuses over, one rung earlier and about the
        // same instance.
        [Test]
        public void AKindOfWorkTheInstanceWouldNotSizeAtAll_IsRefusedRatherThanPassed()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassProbe(
                Problems, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("result_size_unknown"));
                Assert.That(result.Message, Does.Contain(Problems));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // ---------------------------------------------------------------------------------------
        // The second probe: the same kind of work, asked of the table this connection is rooted at.
        // Readable on its own table is not the fact the read depends on.
        // ---------------------------------------------------------------------------------------

        // The headline of the amendment. Measured: /change_request answers 105 to the account that
        // gets header = 0 from /incident?sys_class_name=change_request. Accepting that team means it
        // syncs no change at all and says nothing about it.
        [Test]
        public void AKindOfWorkThatDoesNotLiveUnderTheConfiguredTable_NamesBothTheKindAndTheTable()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassUnderTableProbe(
                Changes, Table, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 0, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("class_not_under_configured_table"));
                Assert.That(result.Message, Does.Contain(Changes).And.Contain(Table),
                    "Which kind of work, and which table it is not under. Either half alone is unactionable.");
                Assert.That(result.Message, Does.Not.Contain("does not exist"),
                    "The class exists and is readable -- the other probe already covers a name that does not, with a better message.");
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField));
            }
        }

        // The gap between the ACL-blind header and the body survives a class-scoped sysparm_query,
        // measured: /task?sys_class_name=problem reports 24 to an account shown none of them. So the
        // hidden-kind rung is reachable here too, and keeps its own name rather than being reported
        // as a class living somewhere else.
        [Test]
        public void AKindOfWorkUnderTheTableThatTheAccountCannotSee_IsStillReportedAsHidden()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassUnderTableProbe(
                Problems, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 24, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("class_records_not_visible"));
                Assert.That(result.Message, Does.Contain(Problems));
            }
        }

        [Test]
        public void AKindOfWorkThatIsReadableUnderTheConfiguredTable_IsAccepted()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassUnderTableProbe(
                Changes, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: 105, visibleRowCount: 1);

            Assert.That(result.Code, Is.EqualTo("valid"));
        }

        // This probe addressed the connection's table, so a transport failure is about that table.
        // Naming the class instead would send the flow coach to correct a spelling that is right.
        [TestCase(HttpStatusCode.Forbidden, "insufficient_permissions", TestName = "ATableTheAccountMayNotReadThroughAClassFilter_IsReportedAgainstTheTable")]
        [TestCase(HttpStatusCode.BadRequest, "unknown_table", TestName = "ATableTheInstanceDoesNotHave_IsReportedAgainstTheTable")]
        public void ARefusalOfTheConfiguredTable_IsReportedAgainstThatTableRatherThanTheKindOfWork(
            HttpStatusCode status, string expectedCode)
        {
            var result = ServiceNowTeamQueryVerdict.FromClassUnderTableProbe(
                Changes, TheWholeHierarchy, status, carriesRecords: false, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo(expectedCode));
                Assert.That(result.Message, Does.Contain(TheWholeHierarchy));
                Assert.That(result.FieldName, Is.EqualTo(KindsOfWorkField),
                    "The flow coach reached this by typing a kind of work, so that is the field to send them back to.");
            }
        }

        [Test]
        public void AKindOfWorkUnderATableTheInstanceWouldNotSize_IsRefusedRatherThanPassed()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassUnderTableProbe(
                Changes, TheWholeHierarchy, HttpStatusCode.OK, carriesRecords: true, recordsTheInstanceHolds: null, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("result_size_unknown"));
                Assert.That(result.Message, Does.Contain(TheWholeHierarchy));
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
