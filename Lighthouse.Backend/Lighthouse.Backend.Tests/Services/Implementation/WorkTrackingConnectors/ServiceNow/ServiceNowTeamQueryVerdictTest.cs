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

        // A gateway that rewrites ServiceNow's own 400 error envelope into a 200 hands the ladder a
        // body that parses and carries no record set. Read as "JSON, zero rows" that is a misspelt
        // class passing validation, after which the team syncs a subset with nothing logged. The
        // sync's own RecordsFrom has always refused this shape; the class probe now agrees.
        [Test]
        public void AKindOfWorkWhoseAnswerCarriesNoRecordSetAtAll_IsNeverReportedAsReadable()
        {
            var result = ServiceNowTeamQueryVerdict.FromClassProbe(
                "change_request", HttpStatusCode.OK, carriesRecords: false, recordsTheInstanceHolds: 0, visibleRowCount: 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.FieldName, Is.EqualTo("WorkItemTypes"),
                    "Whatever the rung, a kind of work the coach typed sends them back to the field they typed it in.");
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
