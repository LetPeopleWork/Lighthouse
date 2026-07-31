using System.Globalization;
using System.Linq.Expressions;
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
    // Story #5575, US-02 AC1 / AC5 / AC6 / AC7. The connector asking a ServiceNow instance for one
    // team's work, exercised against a stubbed transport that behaves the way the measured instance
    // behaves — offset paging with X-Total-Count, short pages, and sysparm_display_value=all.
    //
    // Layer 3 (real adapter, stubbed transport): sad paths are enumerated one example each, never
    // generated. The field-by-field mapping rules live in ServiceNowWorkItemMapperTest and the
    // query verdict's rungs in ServiceNowTeamQueryVerdictTest; this file is about what the connector
    // asks for and what it does with the answer.
    [TestFixture]
    public class ServiceNowTeamSyncTest
    {
        private const string InstanceUrl = "https://dev12345.service-now.com/";
        private const string TeamsOwnQuery = "assignment_group.name=Service Desk^active=true";

        private static readonly string[] EveryRecordInTheFixture =
            ["INC0000001", "INC0000002", "INC0000003", "INC0000004", "INC0000005"];

        // AC1. The query the flow coach wrote is the query that gets asked, against the table the
        // connection was configured for. Anything else and the team is looking at somebody else's work.
        [Test]
        public async Task SyncingATeam_AsksTheConfiguredTableForTheWorkTheFlowCoachDescribed()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam(query: TeamsOwnQuery, table: "change_request"));

            var asked = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.AbsoluteUri)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asked, Has.Some.Contains("/api/now/table/change_request"));
                Assert.That(asked, Has.Some.Contains(TeamsOwnQuery),
                    "The flow coach's own query has to reach the instance verbatim.");
            }
        }

        // The replacement for the sys_choice lookup DESIGN named: display_value=all needs no extra
        // table access, works on a read-only account, and returns both forms of every field — the
        // label the flow coach maps and the universal time Throughput buckets by.
        [Test]
        public async Task SyncingATeam_AsksForBothTheLabelAndTheUnderlyingValueOfEveryField()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var asked = instance.Requests.Select(uri => uri.AbsoluteUri).ToList();

            Assert.That(asked, Has.Some.Contains("sysparm_display_value=all"),
                "Without this, state comes back as a bare integer and there is no label to map.");
        }

        // AC7. The instance returns short pages regardless of what was asked for, and says how many
        // rows exist in X-Total-Count. A pager that trusts its own limit stops early and the team's
        // Throughput silently reads low.
        [Test]
        public async Task WorkSpreadAcrossMorePagesThanOne_IsAllBroughtBack()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 2);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState());

            Assert.That(workItems.ToList(), Has.Count.EqualTo(5));
        }

        [Test]
        public async Task PagesOfWork_NeitherOverlapNorSkip()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 2);
            var subject = CreateSubject(instance);

            var referenceIds = (await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState())).Select(item => item.ReferenceId).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(referenceIds, Is.Unique);
                Assert.That(referenceIds, Is.EquivalentTo(EveryRecordInTheFixture));
            }
        }

        // SPIKE Q7 measured ~600ms per Table API call and no rate limiting, so the constraint is
        // wall-clock, not throttling. Five records must cost pages, not five round trips.
        [Test]
        public async Task SyncingATeam_ReadsInBatchesRatherThanOneRecordAtATime()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 2);
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(instance.Requests, Has.Count.LessThanOrEqualTo(3),
                "Five records over pages of two is three reads. Anything approaching one call per record is a five-minute sync on a real instance.");
        }

        // H5. Offset paging is only safe over a stable order, and an incident table on a live
        // instance is neither ordered nor still. This stub gains a record between two pages and
        // places it the way the instance would: at the end when an order was asked for, and at the
        // front when it was not — which is what pushes an unread row past the offset for good.
        [Test]
        public async Task WorkThatArrivesWhileTheTeamIsBeingRead_IsNotSkippedOver()
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 2, GainsARecordAfterTheFirstPage = true });
            var subject = CreateSubject(instance);

            var referenceIds = (await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState())).Select(item => item.ReferenceId).ToList();

            Assert.That(referenceIds, Is.SupersetOf(EveryRecordInTheFixture),
                "Without an explicit order the row created between the pages lands ahead of the rows already read, and the ones it displaced past the offset are never read at all.");
        }

        [Test]
        public async Task SyncingATeam_AsksForTheRecordsInAStableOrder()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeam());

            var asked = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.Query)).ToList();

            Assert.That(asked, Has.All.Contains("^ORDERBYsys_created_on"),
                "Offset paging over an unordered result set skips rows the moment the table changes between pages.");
        }

        // Linear's precedent: a team only sees work in the states it has mapped. An unmapped label
        // is work the flow coach never told Lighthouse how to interpret.
        [Test]
        public async Task WorkInAStateTheTeamNeverMapped_IsLeftOut()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            var workItems = (await subject.GetWorkItemsForTeam(ATeam())).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Has.Count.EqualTo(4),
                    "The other four records sit in states this team did map, and have to survive.");
                Assert.That(workItems.Select(item => item.ReferenceId), Has.No.Member("INC0000005"),
                    "INC0000005 sits in 'Awaiting Vendor', which this team has not mapped to any of its own states.");
            }
        }

        // DoD 5. Dropping records without a word reads as low Throughput with the settings page
        // still saying the team is valid. The flow coach types these labels by hand against a
        // choice list a read-only account cannot query, so the label has to be in the log to be
        // correctable.
        [Test]
        public async Task WorkInAStateTheTeamNeverMapped_IsNamedInTheLogRatherThanDroppedInSilence()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var subject = CreateSubject(AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10), logger.Object);

            await subject.GetWorkItemsForTeam(ATeam());

            logger.Verify(AWarningContaining("Awaiting Vendor"), Times.Once,
                "The label that was left out has to be named, or there is nothing for the flow coach to correct.");
        }

        // The silent-filter trap's sibling. An unconfigured team must not degrade into an
        // unfiltered read, which is precisely how a team ends up reporting the whole instance.
        [Test]
        public async Task ATeamThatHasNotSaidWhichWorkIsTheirs_ReadsNothingRatherThanEverything()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance, logger.Object);

            var workItems = await subject.GetWorkItemsForTeam(ATeam(query: string.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Empty);
                Assert.That(instance.Requests, Is.Empty,
                    "A team with no query must not ask the instance for anything, because asking with no query returns the whole table.");
            }

            logger.Verify(AWarning(), Times.Once,
                "DoD 5 forbids the silent no-op: reading nothing has to say why, or it reads as a team with no work.");
        }

        // AC5. ServiceNow cannot supply transition history on a read-only account, so the connector
        // says so rather than guessing: WorkItemService's sync-delta fallback is what fills the gap,
        // and it only runs when the connector declares the history unsupported. That the mapper
        // leaves SyncedTransitions empty is a field initializer rather than behaviour — the
        // assertion carrying AC5 end to end is CurrentStateEnteredAt in the acceptance test.
        [Test]
        public void ServiceNowWork_DeclaresThatNoTransitionHistoryIsAvailable()
        {
            var subject = CreateSubject(AnInstanceHolding(FiveRecordsOfMixedState()));

            Assert.That(subject.SupportsTransitionHistory(AConnection()), Is.False);
        }

        // AC2 end to end through the connector, because the mapper being right is worth nothing if
        // the connector reads the wrong form of the response.
        [Test]
        public async Task WorkThatWasResolvedButNeverClosed_ArrivesWithTheDayItFinished()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeam());
            var resolvedItem = workItems.SingleOrDefault(item => item.ReferenceId == "INC0000001");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolvedItem, Is.Not.Null);
                Assert.That(resolvedItem?.ClosedDate, Is.EqualTo(new DateTime(2026, 7, 30, 0, 25, 29, DateTimeKind.Utc)),
                    "resolved_at is set and closed_at is empty, and the universal form of resolved_at falls on the 30th.");
                Assert.That(resolvedItem?.State, Is.EqualTo("Resolved"),
                    "The label the service desk uses, not the choice value 6.");
            }
        }

        // The failure the review of this slice stopped. A read that answers a denial with an empty
        // list is not a failed sync — it is a successful sync of nothing, and RefreshWorkItems
        // deletes every stored item the sync did not return. The team's SyncedTransitions and
        // CurrentStateEnteredAt go with them, and restoring the credential does not bring them back.
        [TestCase(nameof(ARefusedRead), "insufficient_permissions", TestName = "ACredentialThatLosesItsRightsPartWayThrough_FailsTheSyncRatherThanEmptyingTheTeam")]
        [TestCase(nameof(ASignInPage), "unexpected_response", TestName = "ASignInPageServedPartWayThrough_FailsTheSyncRatherThanEmptyingTheTeam")]
        [TestCase(nameof(AnErrorEnvelope), "unexpected_response", TestName = "AnErrorEnvelopeServedPartWayThrough_FailsTheSyncRatherThanEmptyingTheTeam")]
        public void AReadThatFailsPartWayThrough_ThrowsRatherThanReportingAnEmptyTeam(string breakage, string expectedCode)
        {
            var subject = CreateSubject(AnInstanceThatBreaksAfterTheFirstPage(breakage));

            var failure = Assert.ThrowsAsync<ServiceNowReadException>(
                async () => await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState()));

            Assert.That(failure?.Code, Is.EqualTo(expectedCode),
                "The read path routes through slice 01's ladder, so a denial keeps the name the settings page would have given it.");
        }

        // An instance that ignores sysparm_offset answers every page with the first page. With
        // X-Total-Count present that reports the same work several times over as the team's;
        // without it the loop never ends. Both fixtures used to compute a perfect
        // Skip(offset).Take(pageSize), so neither failure was reachable.
        [Test]
        public void AnInstanceThatIgnoresTheOffsetItWasGiven_IsCaughtRatherThanCountedTwice()
        {
            var subject = CreateSubject(AnInstanceHolding(
                FiveRecordsCarryingTheirIdentity(),
                new InstanceBehaviour { PageSize = 2, IgnoresTheOffset = true }));

            var failure = Assert.ThrowsAsync<ServiceNowReadException>(
                async () => await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure?.Code, Is.EqualTo("paging_repeated_records"));
                Assert.That(failure?.Message, Does.Contain(ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable),
                    "sysparm_offset is the setting at fault and the table is where to look, so both belong in what the administrator reads.");
            }
        }

        // The cap is the result-set size the instance itself reported, plus two pages of slack for a
        // table that grew while it was being read — so it stops long before memory does, and the two
        // page sizes below prove the cap is derived rather than a constant.
        [TestCase(2, 4, TestName = "AnInstanceThatKeepsOfferingPagesOfTwo_IsStoppedAfterFourReads")]
        [TestCase(1, 7, TestName = "AnInstanceThatKeepsOfferingPagesOfOne_IsStoppedAfterSevenReads")]
        public void AnInstanceThatKeepsOfferingAnotherPage_IsStoppedRatherThanReadWithoutEnd(int pageSize, int expectedReads)
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = pageSize, NeverRunsOutOfPages = true });
            var subject = CreateSubject(instance);

            var failure = Assert.ThrowsAsync<ServiceNowReadException>(
                async () => await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure?.Code, Is.EqualTo("paging_did_not_terminate"));
                Assert.That(failure?.Message, Does.Contain(ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable),
                    "The table has to be named, or an administrator cannot tell which read stopped.");
                Assert.That(instance.Requests, Has.Count.EqualTo(expectedReads));
            }
        }

        // The Link header the SPIKE measured is the paging signal that survives a stripped
        // X-Total-Count, which is exactly what a proxy in front of the instance takes away.
        [Test]
        public async Task AnInstanceThatDoesNotSayHowManyRowsExist_IsStillPagedToTheEnd()
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 2, OmitsTheResultSetSize = true });
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.ToList(), Has.Count.EqualTo(5));
                Assert.That(instance.Requests, Has.Count.EqualTo(3),
                    "Three pages of two hold five records, and the link saying which page is last means the read stops there instead of probing for an empty one.");
            }
        }

        [Test]
        public async Task AnInstanceThatSaysNeitherHowManyRowsExistNorWhereTheNextPageIs_IsReadUntilItRunsOut()
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 2, OmitsTheResultSetSize = true, OmitsThePagingLinks = true });
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState());

            Assert.That(workItems.ToList(), Has.Count.EqualTo(5));
        }

        // AC6. The comparison IS the detection — one probe cannot tell a silently-widened query from
        // a correct one, because both answer 200 with rows.
        [Test]
        public async Task ValidatingATeamsSettings_ComparesWhatTheQuerySelectsAgainstWhatTheTableHolds()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10);
            var subject = CreateSubject(instance);

            await subject.ValidateTeamSettings(ATeam());

            var queries = instance.Requests.Select(uri => Uri.UnescapeDataString(uri.Query)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queries, Has.Some.Contains(TeamsOwnQuery),
                    "One probe asks what the flow coach's query selects.");
                Assert.That(queries, Has.Some.Matches<string>(query => !query.Contains(TeamsOwnQuery)),
                    "The other asks what the table holds without it. Without both counts there is nothing to compare.");
            }
        }

        [Test]
        public async Task ValidatingATeamThatHasNotSaidWhichWorkIsTheirs_AsksForAQueryWithoutContactingTheInstance()
        {
            var instance = AnInstanceHolding(FiveRecordsOfMixedState());
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeam(query: string.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.EqualTo("missing_query"));
                Assert.That(instance.Requests, Is.Empty);
            }
        }

        // The count probe asks for one row, so the body can only ever say 0 or 1 and the header is
        // the only source of the result-set size. Guessing when it is missing makes matched and
        // total both 1 for every team on the instance, which reads as a query selecting the whole
        // table — the wrong cause, named confidently, on every save.
        [TestCase(null, TestName = "AnInstanceThatSendsNoResultSetSize_IsSaidToBeUncountableRatherThanGuessedAt")]
        [TestCase("", TestName = "AnInstanceThatSendsAnEmptyResultSetSize_IsSaidToBeUncountableRatherThanGuessedAt")]
        [TestCase("many", TestName = "AnInstanceThatSendsAResultSetSizeThatIsNotANumber_IsSaidToBeUncountableRatherThanGuessedAt")]
        [TestCase("-1", TestName = "AnInstanceThatSendsANegativeResultSetSize_IsSaidToBeUncountableRatherThanGuessedAt")]
        public async Task AResultSetSizeLighthouseCannotRead_IsReportedRatherThanSubstitutedFor(string? headerValue)
        {
            var subject = CreateSubject(AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour
                {
                    PageSize = 10,
                    OmitsTheResultSetSize = headerValue is null,
                    ResultSetSize = headerValue,
                }));

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("result_size_unknown"),
                    "Guessing the size turns every team on the instance into a widened query, which is a diagnosis rather than an observation.");
            }
        }

        // AC6's other half — an unresolvable table. Slice 01 already built this ladder; team
        // validation routes through it rather than inventing a second vocabulary for the same
        // failures.
        [Test]
        public async Task ValidatingATeamAgainstATableTheInstanceDoesNotHave_IsToldTheTableIsUnknown()
        {
            var subject = CreateSubject(AnInstanceThatAnswers(HttpStatusCode.BadRequest));

            var result = await subject.ValidateTeamSettings(ATeam(table: "no_such_table"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unknown_table"));
            }
        }

        [Test]
        public async Task ValidatingATeamWithACredentialThatCannotReadTheTable_IsToldItIsAPermissionsProblem()
        {
            var subject = CreateSubject(AnInstanceThatAnswers(HttpStatusCode.Forbidden));

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
            }
        }

        [Test]
        public async Task ValidatingATeamAgainstAnInstanceThatCannotBeReached_IsToldTheInstanceIsNotThere()
        {
            var subject = CreateSubject(AnInstanceThatFails(new HttpRequestException("No such host is known.")));

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
            }
        }

        // The whole point of AC6, driven through the connector: the flow coach fat-fingers a field
        // name, ServiceNow drops the term and hands back the entire table, and Lighthouse stops
        // rather than rendering the instance's metrics as the team's.
        //
        // The two cases below differ in one flag and nothing else. That is the point: the fixture
        // used to ignore the flag entirely, so this scenario passed because nothing filtered rather
        // than because the connector caught anything.
        [TestCase(true, "query_matches_whole_table", TestName = "ValidatingAQueryThatTheInstanceSilentlyIgnored_StopsRatherThanAcceptingWholeInstanceMetrics")]
        [TestCase(false, "valid", TestName = "ValidatingTheSameQueryOnAnInstanceThatHonoursIt_Passes")]
        public async Task AQueryTheInstanceMayOrMayNotHonour_IsJudgedByWhatCameBack(bool ignoresTheQuery, string expectedCode)
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 10, RowsTheQuerySelects = 2, IgnoresTheQuery = ignoresTheQuery });
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeam());

            Assert.That(result.Code, Is.EqualTo(expectedCode));
        }

        [Test]
        public async Task ValidatingAQueryThatSelectsOneTeamsWork_Passes()
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 10, RowsTheQuerySelects = 2 });
            var subject = CreateSubject(instance);

            var result = await subject.ValidateTeamSettings(ATeam());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        // The boundary of the no-work rung. One record is work; zero is not, and the difference
        // decides whether a small service desk can save its settings at all.
        [Test]
        public async Task AQueryThatSelectsASingleRecord_IsAccepted()
        {
            var subject = CreateSubject(AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 10, RowsTheQuerySelects = 1 }));

            var result = await subject.ValidateTeamSettings(ATeam());

            Assert.That(result.Code, Is.EqualTo("valid"));
        }

        // Zero rows is a countable answer. Reading an explicit 0 the same way as a missing header
        // would tell an empty service desk its instance is unreadable, which is a different problem
        // with a different fix.
        [Test]
        public async Task ValidatingATeamAgainstATableWithNothingInIt_IsToldTheTableIsEmpty()
        {
            var subject = CreateSubject(AnInstanceHolding([], pageSize: 10));

            var result = await subject.ValidateTeamSettings(ATeam());

            Assert.That(result.Code, Is.EqualTo("no_work_items_found"));
        }

        // The unfiltered probe can fail on its own — a credential may be allowed to read a table
        // through a filter and refused without one. Reporting that as a verdict on the query would
        // send the flow coach to edit a query that is not the problem.
        //
        // Request 4 is that probe: the two class probes (#5611 — the class's own table, then that
        // class under the team's table) come first, then the matched count, then this one.
        [Test]
        public async Task ValidatingATeam_WhenTheProbeForTheWholeTableIsRefused_ReportsTheRefusal()
        {
            var subject = CreateSubject(AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour
                {
                    PageSize = 10,
                    RowsTheQuerySelects = 2,
                    BreaksFromRequest = 4,
                    Breakage = ARefusedRead,
                }));

            var result = await subject.ValidateTeamSettings(ATeam());

            Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
        }

        [Test]
        public async Task ValidatingATeamAgainstAnInstanceThatNeverAnswers_IsToldTheInstanceIsNotThere()
        {
            var subject = CreateSubject(AnInstanceThatFails(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.")));

            var result = await subject.ValidateTeamSettings(ATeam());

            Assert.That(result.Code, Is.EqualTo("connection_failed"));
        }

        // A stored address that is not an address fails both paths. On the read path it must throw
        // rather than return nothing, for the same reason every other read failure does.
        [Test]
        public void AReadAgainstAnAddressThatIsNotAnInstance_FailsRatherThanReturningNothing()
        {
            var subject = CreateSubject(AnInstanceHolding(FiveRecordsOfMixedState()));

            var failure = Assert.ThrowsAsync<ServiceNowReadException>(
                async () => await subject.GetWorkItemsForTeam(ATeam(instanceUrl: "not-an-instance")));

            Assert.That(failure?.Code, Is.EqualTo("invalid_url"));
        }

        [Test]
        public async Task ValidatingATeamAgainstAnAddressThatIsNotAnInstance_IsToldTheAddressIsWrong()
        {
            var subject = CreateSubject(AnInstanceHolding(FiveRecordsOfMixedState()));

            var result = await subject.ValidateTeamSettings(ATeam(instanceUrl: "not-an-instance"));

            Assert.That(result.Code, Is.EqualTo("invalid_url"));
        }

        // The next page is followed blind, so it may only ever point back at the instance that was
        // asked — a rewriting proxy naming another host would otherwise be handed the credential.
        [Test]
        public async Task ANextPageOnAnotherHost_IsNotFollowedAndTheReadCarriesOnByOffset()
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 2, PagingLinksPointElsewhere = true });
            var subject = CreateSubject(instance);

            var workItems = await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(instance.Requests.Select(uri => uri.Authority), Has.All.EqualTo(new Uri(InstanceUrl).Authority),
                    "The credential goes to the configured instance and nowhere the response names.");
                Assert.That(workItems.ToList(), Has.Count.EqualTo(5),
                    "A link Lighthouse will not follow is not evidence that this was the last page, so the read continues by offset rather than stopping short.");
            }
        }

        // AC7. The instance already said how big the result set is, so asking for one more page is a
        // wasted round trip on every sync of every team.
        [Test]
        public async Task AnInstanceThatSaidHowManyRowsExist_IsNotAskedForAPagePastTheEnd()
        {
            var instance = AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour { PageSize = 10, OmitsThePagingLinks = true });
            var subject = CreateSubject(instance);

            await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState());

            Assert.That(instance.Requests, Has.Count.EqualTo(1));
        }

        // A record that carries neither identity is still a distinct record. Treating the two as
        // repeats of one another would fail the sync of a table that answers with neither field.
        [Test]
        public async Task RecordsThatCarryNoIdentity_AreNotMistakenForRepeatsOfOneAnother()
        {
            var subject = CreateSubject(AnInstanceHolding(
                [ARecordWithoutANumber("first"), ARecordWithoutANumber("second")],
                pageSize: 10));

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(workItems.ToList(), Has.Count.EqualTo(2));
        }

        // `number` is not unique on a real instance. Measured on the PDI, 2026-07-31: the demo seeder
        // minted CHG0030004-CHG0030008 over stock sample changes shipped in 2025-11, and
        // change_request held 118 rows with 113 distinct numbers. Identifying records by it made one
        // collision anywhere in the result set cost the customer every work item on that team.
        [Test]
        public async Task RecordsThatShareANumber_AreBothReadRatherThanFailingTheWholeTeam()
        {
            var subject = CreateSubject(AnInstanceHolding(
                [
                    ARecord("CHG0030004", "In Progress", "2", sysId: "1d5b3e79c0a801670060f9d8b1c1a2f1"),
                    ARecord("CHG0030004", "In Progress", "2", sysId: "46e88ff0a9fe19810012d100cca80666"),
                ],
                pageSize: 10));

            var workItems = await subject.GetWorkItemsForTeam(ATeam());

            Assert.That(workItems.ToList(), Has.Count.EqualTo(2),
                "Two records that happen to share a number are two records, and the guard exists to catch a repeated page rather than a repeated label.");
        }

        // The other half: a record genuinely served twice across pages still stops the read, which is
        // the protection the guard exists for. The instance re-sends it with its text changed, so
        // only sys_id can recognise it.
        [Test]
        public void ARecordSentAgainAfterItWasEdited_IsStillRecognisedAsOneAlreadyRead()
        {
            var subject = CreateSubject(AnInstanceHolding(
                FiveRecordsCarryingTheirIdentity(),
                new InstanceBehaviour { PageSize = 2, ResendsTheFirstRecordAmended = true }));

            var failure = Assert.ThrowsAsync<ServiceNowReadException>(
                async () => await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState()));

            Assert.That(failure?.Code, Is.EqualTo("paging_repeated_records"),
                "Comparing the bytes rather than the record would let an edited row through and count the same work twice.");
        }

        // A warning about nothing trains its reader to ignore the ones that matter.
        [Test]
        public async Task ATeamThatMappedEveryStateItsWorkIsIn_IsNotWarnedAbout()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var subject = CreateSubject(AnInstanceHolding(FiveRecordsOfMixedState(), pageSize: 10), logger.Object);

            await subject.GetWorkItemsForTeam(ATeamThatMapsEveryState());

            logger.Verify(AWarning(), Times.Never);
        }

        [Test]
        public async Task WorkCarryingNoStateAtAll_IsNamedInTheLogAsHavingNone()
        {
            var logger = new Mock<ILogger<ServiceNowWorkTrackingConnector>>();
            var subject = CreateSubject(
                AnInstanceHolding([ARecord("INC0000009", string.Empty, string.Empty)], pageSize: 10),
                logger.Object);

            await subject.GetWorkItemsForTeam(ATeam());

            logger.Verify(AWarningContaining("(no state)"), Times.Once,
                "A record with no state at all still has to be countable in the log, or the number left out cannot be reconciled.");
        }

        private static Expression<Action<ILogger<ServiceNowWorkTrackingConnector>>> AWarning()
        {
            return log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>());
        }

        private static Expression<Action<ILogger<ServiceNowWorkTrackingConnector>>> AWarningContaining(string text)
        {
            return log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => $"{state}".Contains(text, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>());
        }

        private static ServiceNowWorkTrackingConnector CreateSubject(
            StubbedInstance instance, ILogger<ServiceNowWorkTrackingConnector>? logger = null)
        {
            return new ServiceNowWorkTrackingConnector(
                logger ?? Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                NoOpAuthStrategyFactory(),
                instance.Handler);
        }

        private static IWorkTrackingAuthStrategyFactory NoOpAuthStrategyFactory()
        {
            var strategy = new Mock<IWorkTrackingAuthStrategy>();
            strategy
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factory = new Mock<IWorkTrackingAuthStrategyFactory>();
            factory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategy.Object);

            return factory.Object;
        }

        private static Team ATeam(
            string query = TeamsOwnQuery,
            string table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable,
            string instanceUrl = InstanceUrl)
        {
            return new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = query,
                // Every ServiceNow team names the kinds of work it handles (#5611, ADR-123 decision 6
                // as amended). A team rooted at one table names that one kind; Team's own Jira-shaped
                // default would model a team that cannot exist.
                WorkItemTypes = [table],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AConnection(table, instanceUrl),
            };
        }

        // The paging tests need every record in the fixture to survive state filtering. Otherwise
        // "all five came back" cannot tell a working pager from one that stopped early and happened
        // to lose the record the team never mapped. This team maps Awaiting Vendor too, so five
        // means five, and a single-page reader still fails at two.
        private static Team ATeamThatMapsEveryState()
        {
            var team = ATeam();
            team.DoingStates = ["In Progress", "Awaiting Vendor"];

            return team;
        }

        private static WorkTrackingSystemConnection AConnection(
            string table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable, string instanceUrl = InstanceUrl)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = instanceUrl },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = "lighthouse.integration" },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = "encrypted-secret", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = table, IsOptional = true },
            ]);

            return connection;
        }

        // INC0000001 is the record ADR-117 is about: resolved, never closed, and its resolution
        // instant falls on a different day in the instance's own timezone than in universal time.
        // INC0000005 sits in a label the team has not mapped.
        private static List<string> FiveRecordsOfMixedState()
        {
            return FiveRecords(withIdentities: false);
        }

        // The same five as a real instance sends them, every record carrying its sys_id. Kept apart
        // from the fixture above because a record with an identity also triggers the history read,
        // and the paging tests count requests.
        private static List<string> FiveRecordsCarryingTheirIdentity()
        {
            return FiveRecords(withIdentities: true);
        }

        private static List<string> FiveRecords(bool withIdentities)
        {
            return
            [
                ARecord("INC0000001", "Resolved", "6", "2026-07-29 17:25:29", "2026-07-30 00:25:29", IdentityOf("INC0000001", withIdentities)),
                ARecord("INC0000002", "Resolved", "6", "2026-07-28 09:00:00", "2026-07-28 16:00:00", IdentityOf("INC0000002", withIdentities)),
                ARecord("INC0000003", "In Progress", "2", sysId: IdentityOf("INC0000003", withIdentities)),
                ARecord("INC0000004", "New", "1", sysId: IdentityOf("INC0000004", withIdentities)),
                ARecord("INC0000005", "Awaiting Vendor", "18", sysId: IdentityOf("INC0000005", withIdentities)),
            ];
        }

        private static string IdentityOf(string number, bool withIdentities)
        {
            return withIdentities ? $"sys{number.ToLowerInvariant()}" : string.Empty;
        }

        // An empty sys_id is how a table that does not answer with one reads to the mapper, so the
        // default keeps the fixture on the identity-less path the paging tests were written against.
        private static string ARecord(
            string number,
            string stateLabel,
            string stateValue,
            string resolvedDisplay = "",
            string resolvedValue = "",
            string sysId = "")
        {
            return $$"""
                {
                  "sys_id": { "display_value": "{{sysId}}", "value": "{{sysId}}" },
                  "number": { "display_value": "{{number}}", "value": "{{number}}" },
                  "short_description": { "display_value": "Request {{number}}", "value": "Request {{number}}" },
                  "state": { "display_value": "{{stateLabel}}", "value": "{{stateValue}}" },
                  "sys_created_on": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                  "opened_at": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                  "resolved_at": { "display_value": "{{resolvedDisplay}}", "value": "{{resolvedValue}}" },
                  "closed_at": { "display_value": "", "value": "" }
                }
                """;
        }

        // Not every ServiceNow table carries `number` — it is an ITSM task field, and a custom table
        // need not have one. Records without it are still distinct records.
        private static string ARecordWithoutANumber(string description)
        {
            return $$"""
                {
                  "short_description": { "display_value": "{{description}}", "value": "{{description}}" },
                  "state": { "display_value": "New", "value": "1" },
                  "sys_created_on": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                  "opened_at": { "display_value": "2026-07-01 00:00:00", "value": "2026-07-01 07:00:00" },
                  "resolved_at": { "display_value": "", "value": "" },
                  "closed_at": { "display_value": "", "value": "" }
                }
                """;
        }

        private static StubbedInstance AnInstanceHolding(List<string> records, int pageSize = 100)
        {
            return AnInstanceHolding(records, new InstanceBehaviour { PageSize = pageSize });
        }

        private static StubbedInstance AnInstanceHolding(List<string> records, InstanceBehaviour behaviour)
        {
            return StubbedInstance.Holding(records, behaviour);
        }

        private static StubbedInstance AnInstanceThatBreaksAfterTheFirstPage(string breakage)
        {
            return AnInstanceHolding(
                FiveRecordsOfMixedState(),
                new InstanceBehaviour
                {
                    PageSize = 2,
                    BreaksFromRequest = 2,
                    Breakage = BreakageNamed(breakage),
                });
        }

        private static Func<HttpResponseMessage> BreakageNamed(string breakage)
        {
            return breakage switch
            {
                nameof(ARefusedRead) => ARefusedRead,
                nameof(ASignInPage) => ASignInPage,
                _ => AnErrorEnvelope,
            };
        }

        // The credential's rights were revoked, or a row-level ACL changed, between two pages.
        private static HttpResponseMessage ARefusedRead()
        {
            return new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
            };
        }

        // Single sign-on kicked in mid-read and the gateway answered 200 with its login form.
        private static HttpResponseMessage ASignInPage()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body><form id=\"sso-login\"></form></body></html>", Encoding.UTF8, "text/html"),
            };
        }

        // A 200 carrying ServiceNow's own error envelope, which has no result array at all.
        private static HttpResponseMessage AnErrorEnvelope()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"error\":{\"message\":\"Operation Failed\",\"detail\":\"Maximum execution time exceeded\"},\"status\":\"failure\"}",
                    Encoding.UTF8,
                    "application/json"),
            };
        }

        private static StubbedInstance AnInstanceThatAnswers(HttpStatusCode statusCode)
        {
            return StubbedInstance.Answering(statusCode);
        }

        private static StubbedInstance AnInstanceThatFails(Exception exception)
        {
            return StubbedInstance.Failing(exception);
        }

        // What a stubbed instance does differently from the well-behaved one. Every flag here is a
        // behaviour the measured API can actually produce, and each one changes the outcome of at
        // least one test — a flag no test can fail over is a flag that proves nothing.
        private sealed record InstanceBehaviour
        {
            /// <summary>Rows per page, whatever sysparm_limit asked for.</summary>
            public int PageSize { get; init; } = 100;

            /// <summary>How many of the held records the flow coach's query legitimately selects.</summary>
            public int? RowsTheQuerySelects { get; init; }

            /// <summary>The instance drops a query term it does not recognise and answers with the whole table.</summary>
            public bool IgnoresTheQuery { get; init; }

            /// <summary>The instance answers every page from the top, whatever sysparm_offset said.</summary>
            public bool IgnoresTheOffset { get; init; }

            /// <summary>A proxy stripped X-Total-Count, or the instance never sent one.</summary>
            public bool OmitsTheResultSetSize { get; init; }

            /// <summary>An X-Total-Count Lighthouse cannot read as a count.</summary>
            public string? ResultSetSize { get; init; }

            public bool OmitsThePagingLinks { get; init; }

            /// <summary>The Link header names a next page on a host other than the one asked.</summary>
            public bool PagingLinksPointElsewhere { get; init; }

            /// <summary>A record already sent comes back on a later page with its text changed.</summary>
            public bool ResendsTheFirstRecordAmended { get; init; }

            /// <summary>The instance keeps naming a next page holding records nobody has seen.</summary>
            public bool NeverRunsOutOfPages { get; init; }

            /// <summary>A record is created on the instance while the team is being read.</summary>
            public bool GainsARecordAfterTheFirstPage { get; init; }

            public int? BreaksFromRequest { get; init; }

            public Func<HttpResponseMessage>? Breakage { get; init; }
        }

        // A ServiceNow instance that behaves the way the measured one does: it honours
        // sysparm_offset, caps its own page size regardless of the requested sysparm_limit, and
        // reports the true total in X-Total-Count with a Link header carrying the paging relations —
        // unless the behaviour it was given says otherwise.
        private sealed class StubbedInstance
        {
            private const string TotalCountHeader = "X-Total-Count";

            private readonly List<string> records;
            private readonly InstanceBehaviour behaviour;

            private int requestsServed;
            private bool hasGained;

            private StubbedInstance(List<string> records, InstanceBehaviour behaviour)
            {
                this.records = records;
                this.behaviour = behaviour;

                Requests = [];
                Handler = HandlerRespondingWith(Answer);
            }

            private StubbedInstance(HttpMessageHandler handler, List<Uri> requests)
            {
                records = [];
                behaviour = new InstanceBehaviour();

                Handler = handler;
                Requests = requests;
            }

            public HttpMessageHandler Handler { get; }

            public List<Uri> Requests { get; }

            public static StubbedInstance Holding(List<string> records, InstanceBehaviour behaviour)
            {
                return new StubbedInstance(records, behaviour);
            }

            public static StubbedInstance Answering(HttpStatusCode statusCode)
            {
                var requests = new List<Uri>();

                var handler = HandlerRespondingWith(request =>
                {
                    requests.Add(request.RequestUri ?? new Uri(InstanceUrl));
                    return new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent("{\"error\":{\"message\":\"denied\"}}", Encoding.UTF8, "application/json"),
                    };
                });

                return new StubbedInstance(handler, requests);
            }

            public static StubbedInstance Failing(Exception exception)
            {
                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                handler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ThrowsAsync(exception);

                return new StubbedInstance(handler.Object, []);
            }

            private HttpResponseMessage Answer(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri(InstanceUrl);
                Requests.Add(uri);
                requestsServed++;

                if (behaviour.Breakage is not null && requestsServed >= behaviour.BreaksFromRequest)
                {
                    return behaviour.Breakage();
                }

                var visible = VisibleTo(uri);
                var offset = behaviour.IgnoresTheOffset ? 0 : NumberFromQuery(uri.Query, "sysparm_offset");

                var page = behaviour.NeverRunsOutOfPages
                    ? AlwaysAnotherPage(offset)
                    : visible.Skip(offset).Take(behaviour.PageSize).ToList();

                // A record edited between two pages comes back a second time with different text.
                // Offset paging cannot tell that from a new row, so the guard has to recognise the
                // record rather than the bytes.
                if (behaviour.ResendsTheFirstRecordAmended && offset > 0 && visible.Count > 0)
                {
                    page.Insert(0, visible[0].Replace("Request INC", "Amended request INC", StringComparison.Ordinal));
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"result\":[{string.Join(",", page)}]}}", Encoding.UTF8, "application/json"),
                };

                ReportTheResultSetSize(response, visible.Count);
                ReportThePagingLinks(response, uri, offset, behaviour.NeverRunsOutOfPages ? int.MaxValue : visible.Count);

                GainARecordIfAsked(uri);

                return response;
            }

            // The flag and the count are independent on purpose. An instance that honours the query
            // answers with the rows it selects; one that drops the term answers with the whole
            // table, whatever the query would have selected.
            //
            // Narrowing is recognised by the team's own query rather than by "a query is present":
            // every ServiceNow read now also carries the class clause (#5611), and the widening
            // detector's baseline probe carries that clause alone. Keying on presence would make the
            // baseline count itself as narrowed, and every team read as query_matches_whole_table.
            private List<string> VisibleTo(Uri uri)
            {
                var isFiltered = Uri.UnescapeDataString(uri.Query)
                    .Contains(TeamsOwnQuery, StringComparison.Ordinal);

                if (!isFiltered || behaviour.IgnoresTheQuery)
                {
                    return [.. records];
                }

                return records.Take(behaviour.RowsTheQuerySelects ?? records.Count).ToList();
            }

            private List<string> AlwaysAnotherPage(int offset)
            {
                return Enumerable.Range(offset, behaviour.PageSize)
                    .Select(index => ARecord($"INC{index.ToString("D7", CultureInfo.InvariantCulture)}", "New", "1"))
                    .ToList();
            }

            // A record created now sorts last under sys_created_on, so everything already read keeps
            // its position. Without that order the API's window is arbitrary, and a new row landing
            // ahead of the rows already read pushes unread ones past the offset for good.
            private void GainARecordIfAsked(Uri uri)
            {
                if (!behaviour.GainsARecordAfterTheFirstPage || hasGained)
                {
                    return;
                }

                hasGained = true;
                var arrival = ARecord("INC0000090", "New", "1");

                if (Uri.UnescapeDataString(uri.Query).Contains("^ORDERBYsys_created_on", StringComparison.Ordinal))
                {
                    records.Add(arrival);
                    return;
                }

                records.Insert(0, arrival);
            }

            private void ReportTheResultSetSize(HttpResponseMessage response, int total)
            {
                if (behaviour.OmitsTheResultSetSize)
                {
                    return;
                }

                response.Headers.TryAddWithoutValidation(
                    TotalCountHeader,
                    behaviour.ResultSetSize ?? total.ToString(CultureInfo.InvariantCulture));
            }

            private void ReportThePagingLinks(HttpResponseMessage response, Uri uri, int offset, int total)
            {
                if (behaviour.OmitsThePagingLinks)
                {
                    return;
                }

                var links = new List<string> { $"<{PageAddress(uri, 0)}>;rel=\"first\"" };
                var next = offset + behaviour.PageSize;

                if (next < total)
                {
                    var address = behaviour.PagingLinksPointElsewhere
                        ? PageAddress(uri, next).Replace(new Uri(InstanceUrl).Authority, "someone-elses-instance.example.com", StringComparison.Ordinal)
                        : PageAddress(uri, next);

                    links.Add($"<{address}>;rel=\"next\"");
                }

                links.Add($"<{PageAddress(uri, Math.Max(0, total - behaviour.PageSize))}>;rel=\"last\"");

                response.Headers.TryAddWithoutValidation("Link", string.Join(",", links));
            }

            // The real header echoes every sysparm_* it was asked with and moves only the offset. A
            // stub that dropped the query would let a connector which follows the link read the
            // whole table with nothing noticing.
            private static string PageAddress(Uri uri, int offset)
            {
                var parameters = uri.Query.TrimStart('?')
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Where(pair => !pair.StartsWith("sysparm_offset=", StringComparison.Ordinal))
                    .Append($"sysparm_offset={offset.ToString(CultureInfo.InvariantCulture)}");

                return $"{uri.GetLeftPart(UriPartial.Path)}?{string.Join("&", parameters)}";
            }

            private static HttpMessageHandler HandlerRespondingWith(Func<HttpRequestMessage, HttpResponseMessage> respond)
            {
                var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
                handler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => respond(request));

                return handler.Object;
            }

            private static int NumberFromQuery(string query, string key)
            {
                foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var separator = pair.IndexOf('=', StringComparison.Ordinal);

                    if (separator > 0
                        && pair[..separator].Equals(key, StringComparison.Ordinal)
                        && int.TryParse(pair[(separator + 1)..], CultureInfo.InvariantCulture, out var value))
                    {
                        return value;
                    }
                }

                return 0;
            }
        }
    }
}
