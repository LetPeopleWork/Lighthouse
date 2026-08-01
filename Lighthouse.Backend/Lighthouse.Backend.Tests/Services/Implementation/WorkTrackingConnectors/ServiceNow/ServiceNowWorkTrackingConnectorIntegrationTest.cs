using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Live tests against a real ServiceNow instance. Turns the SPIKE's one-off hand measurements
    /// (Q8 role matrix, spike/findings.md) into a standing guard: the unit tests pin the verdict
    /// ladder against a stubbed transport, and these pin that the instance still behaves the way
    /// the ladder assumes.
    ///
    /// Path-scoped via Category("ServiceNowIntegration") — see Scripts/test-selection/path-classifier.sh.
    /// Slice 02 extends this fixture with work-item reads rather than adding a second one.
    /// </summary>
    [Category("Integration")]
    [Category("ServiceNowIntegration")]
    public class ServiceNowWorkTrackingConnectorIntegrationTest
    {
        // PDIs are reclaimed after ~10 days idle, so the instance moves. Override without a code
        // change when it does.
        private const string DefaultInstanceUrl = "https://dev191338.service-now.com";

        private const string AdminUser = "admin";

        // Created during the SPIKE with no roles at all. The account that proves the headline bug:
        // it authenticates, and every ITSM read comes back 200 with zero rows.
        private const string NoRolesUser = "lh_probe_none";

        // Created during the SPIKE with sn_incident_read but no sn_problem_read. The asymmetry is what
        // makes AC-B6 provable: incident and problem are the same response shape to this account, and
        // only the ACL-blind X-Total-Count tells them apart (ADR-124).
        private const string RestrictedUser = "lh_probe_snc_read";

        // ServiceNow's work hierarchy. Everything the ITSM applications file lives under it, which is
        // why a team rooted here has to name the kinds of work that are its own (ADR-123 decision 5).
        private const string HierarchyRootTable = "task";

        private const string MetricsTable = "metric_definition";

        // Visual Task Boards. Read-guarded by a script rather than a role: a board is shared through
        // vtb_board_member, so roles predict nothing about who can see one (SPIKE, 2026-08-01).
        private const string BoardTable = "vtb_board";

        // The only boards Lighthouse can turn into a team: a freeform board stores neither a table
        // nor a filter, and a board with a table but no filter pre-fills an empty query.
        private const string UsableBoards = "active=true^tableISNOTEMPTY^filterISNOTEMPTY";

        // Held 105 records when slice 02 was written, which is what makes it the table that can
        // prove paging on an instance whose incident table fits in a single page.
        private const string ChangeTable = "change_request";

        // A real, readable, populated table that is not a task descendant — 641 rows, and zero of
        // them under `task`. The case the second probe exists for.
        private const string NotWorkTable = "sys_user";

        // A genuine task descendant this instance holds no records of. The case the second probe has
        // to ACCEPT rather than refuse (OQ-8).
        private const string EmptyKindOfWorkTable = "incident_task";

        // Mirrors the connector's own sysparm_limit. A pager that reads one page and stops brings
        // back exactly this many.
        private const int SinglePageSize = 100;

        [Test]
        public async Task ACredentialThatCanSeeIncidents_ValidatesSuccessfully()
        {
            var connection = CreateConnection(AdminUser);

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        /// <summary>
        /// The headline bug, against a real instance. SPIKE Q8 measured that a permitted-but-
        /// unauthorised read returns 200 with zero rows — indistinguishable from an empty table —
        /// so a naive connector reports "connected, 0 work items found" and sends the customer
        /// hunting for a query bug that is actually a permissions problem.
        /// </summary>
        [Test]
        public async Task ACredentialWithNoRoles_IsNeverReportedAsValid()
        {
            var connection = CreateConnection(NoRolesUser);

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("no_records_visible"));
                Assert.That(result.Message, Is.Not.Empty);
            }
        }

        [Test]
        public async Task ACredentialTheInstanceRejects_IsReportedAsAnAuthenticationFailure()
        {
            var connection = CreateConnection(AdminUser, password: "not-the-password");

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("authentication_failed"));
            }
        }

        /// <summary>
        /// SPIKE Q8: <c>metric_definition</c> returns 403 for every read-only role and opens only at
        /// itil-grade. This is the rung that proves ServiceNow does sometimes deny honestly — without
        /// it, "everything is a silent 200" would be indistinguishable from a bug in the ladder. It
        /// moved from connection scope to the kind-of-work ladder when the connection stopped having
        /// a table to point anywhere: a name the hierarchy holds none of gets a second probe, and a
        /// refusal there keeps its own name rather than being reported as an absence.
        /// </summary>
        [Test]
        public async Task AKindOfWorkTheCredentialMayNotTouch_IsReportedAsInsufficientPermissions()
        {
            var team = ATeamCovering([MetricsTable], "active=true", NoRolesUser);

            var result = await CreateSubject().ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
            }
        }

        [Test]
        public async Task AnInstanceThatIsNotThere_IsReportedAsAConnectionFailure()
        {
            var connection = CreateConnection(AdminUser, instanceUrl: "https://127.0.0.1:1");

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("connection_failed"));
            }
        }

        /// <summary>
        /// US-02 AC1/AC2/AC3 against real records. <c>sysparm_display_value=all</c> is the mechanism
        /// the whole slice rests on (the Q10 correction replaced a <c>sys_choice</c> lookup only
        /// <c>admin</c> can perform), and its two halves are exactly where a mapping bug hides: the
        /// label a flow coach maps arrives in <c>display_value</c>, and the instant Throughput
        /// buckets by arrives in <c>value</c>.
        /// </summary>
        [Test]
        public async Task ATeamsOwnQuery_BringsBackRealRecordsCarryingLabelsAndUniversalTimes()
        {
            var team = ATeamReadingIncidents("active=true");

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty);
                Assert.That(workItems.Select(item => item.ReferenceId), Is.All.StartsWith("INC"));
                Assert.That(
                    workItems.Select(item => item.State),
                    Is.All.Matches<string>(state => !int.TryParse(state, CultureInfo.InvariantCulture, out _)),
                    "The state has to be the label the service desk uses. The raw choice value is an integer nobody outside the platform team recognises — on change_request it is even negative.");
                Assert.That(workItems.Select(item => item.CreatedDate?.Kind), Is.All.EqualTo(DateTimeKind.Utc));

                // Slice 04 turned this assertion around: an itil-grade credential DOES get history,
                // so the guard is no longer "none arrives" but "none of it was invented". The stock
                // incident table measures `active`, `assigned_to` and `assignment_group` with the
                // same `field_value_duration` definition type as the state field, so before the
                // discriminator moved to the label this reported `true -> false` and group names as
                // state changes.
                var transitions = workItems.SelectMany(item => item.SyncedTransitions).ToList();
                Assert.That(transitions, Is.Not.Empty,
                    "An itil-grade credential can read metric_instance, so the incidents carry history. Empty here means the definition query matched nothing again.");
                Assert.That(
                    transitions.SelectMany(transition => new List<string> { transition.FromState, transition.ToState }),
                    Is.All.Matches<string>(team.AllStates.Contains),
                    "A move between labels the team never mapped is not a state change — it is a span from a definition measuring some other field.");
            }
        }

        /// <summary>
        /// AC7. The instance honours the requested <c>sysparm_limit</c> here rather than capping it,
        /// so proving the pager needs a table holding more rows than one page — <c>change_request</c>
        /// held 105 when this was written. A pager that reads one page and stops brings back exactly
        /// <see cref="SinglePageSize"/> and the team's Throughput reads low with nothing anywhere
        /// reporting a failure.
        /// </summary>
        [Test]
        public async Task WorkSpreadAcrossMorePagesThanOne_ComesBackWhole()
        {
            var team = ATeamReadingEveryChange("numberSTARTSWITHCHG");

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Has.Count.GreaterThan(SinglePageSize),
                    "If change_request now holds less than a page, this instance can no longer prove paging and the fixture needs a bigger table rather than a smaller assertion.");
                Assert.That(workItems.Select(item => item.ReferenceId), Is.Unique,
                    "Offset paging returns disjoint pages. A repeated reference id means the offset did not advance by the rows that actually came back.");
            }
        }

        /// <summary>
        /// The tie-breaker, live, and the only place it can be proved. <c>sys_created_on</c> has
        /// one-second resolution and the seeder writes in bulk — measured, 159 rows over 98 distinct
        /// values with up to 10 sharing one second — so ordering by it alone leaves ties in an
        /// arbitrary order and offset paging silently drops whatever the second page shuffled past
        /// the boundary. Measured before the fix: pages 1 and 2 overlapped by one <c>sys_id</c> and
        /// their union was 158 of 159.
        ///
        /// The invariant asserted is the one that breaks: each class on its own fits inside a single
        /// page, so those two reads cannot lose anything; the merged read has to page, and has to
        /// come back with exactly their sum.
        /// </summary>
        [Test]
        public async Task WorkOfSeveralKindsSpreadAcrossPages_ComesBackWholeAndWithoutRepeats()
        {
            var subject = CreateSubject();

            var incidents = await subject.GetWorkItemsForTeam(ATeamCovering(["incident"], "active=true", AdminUser));
            var changes = await subject.GetWorkItemsForTeam(ATeamCovering([ChangeTable], "active=true", AdminUser));
            var both = (await subject.GetWorkItemsForTeam(ATeamCovering(["incident", ChangeTable], "active=true", AdminUser))).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(both, Has.Count.GreaterThan(SinglePageSize),
                    "If the two kinds together now fit in one page, this instance can no longer prove the tie-breaker and the fixture needs more data rather than a smaller assertion.");
                Assert.That(both, Has.Count.EqualTo(incidents.Count() + changes.Count()),
                    "A row lost at the page boundary shows up here and nowhere else: no error, no gap, just a total one short.");
            }
        }

        /// <summary>
        /// ADR-117 decision 1 as amended 2026-07-31, live. State 6 (Resolved) leaves <c>closed_at</c>
        /// empty — measured, and re-measured here on every run — and <c>resolved_at</c> is no longer
        /// read at all, so the ONLY thing that can date this work is the instance's own transition
        /// history. The assertion is therefore not "a date arrived" but "the date is the arrival in
        /// Done": a connector that fell back to a record field would satisfy the first and fail this.
        /// </summary>
        [Test]
        public async Task WorkThatWasResolvedButNeverClosed_ArrivesWithTheDayItsHistorySaysItFinished()
        {
            var team = ATeamReadingIncidents("state=6");

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty,
                    "This instance holds no resolved-but-not-closed incident any more, so it can no longer prove the rule ADR-117 exists for.");
                Assert.That(workItems.Select(item => item.ClosedDate), Is.All.Not.Null,
                    "closed_at is empty on every one of these, so a null here means the state spans were not consulted.");
                Assert.That(workItems.Select(item => item.ClosedDate?.Kind), Is.All.EqualTo(DateTimeKind.Utc));
                Assert.That(
                    workItems.Select(item => item.ClosedDate),
                    Is.EqualTo(workItems.Select(ArrivalInDone)),
                    "The finish date has to be the moment the record entered the state this team calls Done.");
            }
        }

        // The last transition into a state the team maps to Done, read back off the work item the
        // connector produced rather than off a second request.
        private static DateTime? ArrivalInDone(WorkItem workItem)
        {
            return workItem.SyncedTransitions
                .LastOrDefault(transition => transition.ToState == "Resolved")?.TransitionedAt;
        }

        /// <summary>
        /// AC6, and the guard on the assumption the whole detector rests on: the count comparison is
        /// read from <c>X-Total-Count</c>, so a narrowing query has to pass without a false alarm.
        /// </summary>
        [Test]
        public async Task AQueryThatSelectsOneTeamsWork_ValidatesSuccessfully()
        {
            var result = await CreateSubject().ValidateTeamSettings(ATeamReadingIncidents("active=true"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True, result.Message);
                Assert.That(result.Code, Is.EqualTo("valid"));
            }
        }

        /// <summary>
        /// The silent-filter trap, reproduced against the instance rather than carried on trust:
        /// ServiceNow drops a query term naming a field the table does not have and answers with the
        /// entire table. A flow coach who fat-fingers a field name otherwise gets metrics computed
        /// over every incident in the instance, looking plausible and being wrong.
        /// </summary>
        [Test]
        public async Task AQueryNamingAFieldTheTableDoesNotHave_IsCaughtRatherThanSilentlyWidened()
        {
            var result = await CreateSubject().ValidateTeamSettings(ATeamReadingIncidents("not_a_real_field=whatever"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("query_matches_whole_table"));
            }
        }

        /// <summary>
        /// Story #5611 slice 01, AC-B6 / ADR-124 decision 2 rung 1. The one link in the ladder that was
        /// inferred before it was measured: a class is a table, so a name that is not a table answers
        /// 400 rather than narrowing to nothing in silence. Measured credential-independent across all
        /// four probe accounts; this assertion exists so a future ServiceNow release cannot quietly
        /// turn it into a 200.
        /// </summary>
        [Test]
        public async Task AKindOfWorkTheInstanceDoesNotHave_IsRefusedBySaveAndNamed()
        {
            var team = ATeamCovering(["not_a_real_class"], "active=true", AdminUser);

            var result = await CreateSubject().ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unknown_table"));
                Assert.That(result.Message, Does.Contain("not_a_real_class"));
            }
        }

        /// <summary>
        /// AC-B6 / ADR-124 decision 2 rungs 3 and 4, and the single mechanism the whole acceptance
        /// criterion rests on: X-Total-Count reports what the instance holds while the body reports
        /// what the account may read. <c>lh_probe_snc_read</c> can read incidents but not problems, and
        /// the two answers are otherwise the same HTTP response with fewer rows in it. If this ever
        /// passes for both classes, ServiceNow has started applying ACLs to the header and the ladder
        /// has lost its only signal.
        /// </summary>
        [Test]
        public async Task AKindOfWorkTheAccountMayNotRead_IsToldApartFromOneItCan()
        {
            var subject = CreateSubject();

            var readable = await subject.ValidateTeamSettings(
                ATeamCovering(["incident"], "active=true", RestrictedUser));
            var hidden = await subject.ValidateTeamSettings(
                ATeamCovering(["incident", "problem"], "active=true", RestrictedUser));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readable.IsValid, Is.True, readable.Message);
                Assert.That(hidden.IsValid, Is.False,
                    "problem holds records this account cannot see, and a team told nothing would quietly sync half its work.");
                Assert.That(hidden.Message, Does.Contain("problem"));
            }
        }

        /// <summary>
        /// Story #5611, AC-B6 / ADR-124 decision 2 as re-ordered 2026-07-31. A name can be a real,
        /// readable, populated table on this instance and still contribute nothing to the read, and
        /// the two answers diverge only against a real instance — a fixture can be made to say
        /// either. Measured, same account, same second: <c>/sys_user</c> reports 641 while
        /// <c>/task?sysparm_query=sys_class_name=sys_user</c> reports 0. If this ever passes,
        /// ServiceNow has started resolving <c>sys_class_name</c> outside the work hierarchy and the
        /// refusal has quietly become wrong.
        /// </summary>
        [Test]
        public async Task AKindOfWorkThatIsNotWork_IsToldApartFromOneThatIs()
        {
            var subject = CreateSubject();

            var realWork = await subject.ValidateTeamSettings(
                ATeamCovering([ChangeTable], "active=true", AdminUser));

            var result = await subject.ValidateTeamSettings(
                ATeamCovering(["incident", NotWorkTable], "active=true", AdminUser));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(realWork.IsValid, Is.True, realWork.Message);
                Assert.That(result.IsValid, Is.False,
                    "sys_user is a readable table holding 641 records, so a ladder that only asked whether the name resolves would accept this team — and it would then sync no user at all, silently.");
                Assert.That(result.Code, Is.EqualTo("class_is_not_a_kind_of_work"));
                Assert.That(result.Message, Does.Contain(NotWorkTable).And.Contain(HierarchyRootTable));
            }
        }

        /// <summary>
        /// The case the second probe has to ACCEPT rather than refuse: a genuine kind of work this
        /// instance holds none of yet. OQ-8 settled that as a legitimate configuration, and only a
        /// live instance has a class in that state to prove it with.
        /// </summary>
        [Test]
        public async Task AKindOfWorkTheInstanceHoldsNothingOfAnywhere_IsAcceptedRatherThanRefused()
        {
            var team = ATeamCovering(["incident", EmptyKindOfWorkTable], "active=true", AdminUser);

            var result = await CreateSubject().ValidateTeamSettings(team);

            Assert.That(result.IsValid, Is.True,
                $"'{EmptyKindOfWorkTable}' is a task descendant this instance has no records of. If it has since gained some, pick another empty descendant rather than weakening the assertion.");
        }

        /// <summary>
        /// S4. metric_definition rows attach to concrete classes and never to the base table — measured
        /// 0 for <c>table=task</c>, 6 for <c>tableINincident,change_request</c>. Shipping the class
        /// filter without scoping the definition read takes every started date and state span away from
        /// exactly the configuration this feature recommends.
        /// </summary>
        [Test]
        public async Task ATeamCoveringSeveralKindsOfWork_StillLearnsWhenItsWorkChangedState()
        {
            var team = ATeamCovering(["incident"], "active=true", AdminUser);

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty);
                Assert.That(workItems.SelectMany(item => item.SyncedTransitions), Is.Not.Empty,
                    "Read through the whole hierarchy, the definitions have to be looked for on the kinds of work the team named.");
            }
        }

        // Story #5610 slice 02 — the Earned Trust assertions ADR-125 asks for. vtb_board is a
        // substrate that has already been measured lying twice (an ACL-blind counter, and a denial
        // dressed as a 200), so each of the four below exercises one specific lie. They are
        // instance behaviour a ServiceNow release can change underneath us, and getting any of them
        // wrong ships the exact bug this epic's validation exists to catch.

        // The board's stored filter is a verbatim encoded query in COLUMN form. If it ever stops
        // being one, pre-filling it verbatim stops being safe.
        [Test]
        public async Task ABoardsOwnFilter_SelectsLessWorkThanTheWholeTableItRunsAgainst()
        {
            var board = await ABoardThisAccountCanUse();

            var selected = await AskTheInstance(AdminUser, TableOf(board), FilterOf(board));
            var wholeTable = await AskTheInstance(AdminUser, TableOf(board), string.Empty);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selected.Holds, Is.GreaterThan(0), "A board whose filter selects nothing is not a board anyone runs a stand-up from.");
                Assert.That(selected.Holds, Is.LessThan(wholeTable.Holds), "The filter is what makes the board a team rather than the whole table.");
            }
        }

        // The trap, kept as a standing guard: the filter as it reads on ServiceNow's own screen is
        // the label form, and running it matches every record in the table (105/105 and 118/118,
        // measured 2026-08-01). It is the legible string, which is why it is the one a careless
        // implementation reaches for.
        [Test]
        public async Task TheFilterAsItReadsOnScreen_SelectsTheWholeTable()
        {
            var board = await ABoardThisAccountCanUse();

            var selected = await AskTheInstance(AdminUser, TableOf(board), ReadableFilterOf(board));
            var wholeTable = await AskTheInstance(AdminUser, TableOf(board), string.Empty);

            Assert.That(selected.Holds, Is.EqualTo(wholeTable.Holds),
                "Still the whole-table widening. If this ever stops being true, the reason not to read readable_filter has changed and the decision deserves re-arguing rather than quiet inheritance.");
        }

        // Boards are shared, not roled: an account nobody has shared a board with is answered with
        // an empty success, never a refusal — so an empty picker must be worded as membership rather
        // than as a broken connection. And the instance still counts the boards it is hiding, which
        // is why the list is never counted from the header.
        [Test]
        public async Task AnAccountThatSharesNoBoard_IsAnsweredWithAnEmptySuccessWhoseCountStillNamesEveryBoard()
        {
            var answer = await AskTheInstance(NoRolesUser, BoardTable, string.Empty);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), "A denial here would be an honest one, and would move the empty-list copy from honest to false.");
                Assert.That(answer.Visible, Is.Zero);
                Assert.That(answer.Holds, Is.GreaterThan(0), "The counter is computed before the ACLs run. Counting boards from it offers an administrator boards that are not there.");
            }
        }

        // AC-B6. The whole promise of the picker, against a real instance: the work a pre-filled team
        // reads is the work the board's own filter selects — not the whole table, and not the board's
        // card set, which the SPIKE measured drifting behind its own filter (7 cards, 13 matches).
        [Test]
        public async Task ABoardPickedOnTheInstance_PreFillsTheWorkItsOwnFilterSelects()
        {
            IBoardInformationProvider picker = CreateSubject();
            var connection = CreateConnection(AdminUser);

            var boards = (await picker.GetBoards(connection)).ToList();

            Assert.That(boards, Is.Not.Empty, "The Lighthouse account has to be a member of at least one board for this to measure anything.");

            var preFill = await picker.GetBoardInformation(connection, boards[0].Id);
            var kindOfWork = preFill.WorkItemTypes.Single();

            var selected = await AskTheInstance(AdminUser, kindOfWork, preFill.DataRetrievalValue);
            var wholeTable = await AskTheInstance(AdminUser, kindOfWork, string.Empty);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selected.Holds, Is.GreaterThan(0));
                Assert.That(selected.Holds, Is.LessThan(wholeTable.Holds));
            }
        }

        private static async Task<JsonElement> ABoardThisAccountCanUse()
        {
            var boards = await AskTheInstance(AdminUser, BoardTable, UsableBoards);

            Assert.That(boards.Records, Is.Not.Empty, "No board on this instance carries both a table and a filter, so there is nothing to measure.");

            return boards.Records[0];
        }

        private static string TableOf(JsonElement board)
        {
            return board.GetProperty("table").GetString() ?? string.Empty;
        }

        private static string FilterOf(JsonElement board)
        {
            return board.GetProperty("filter").GetString() ?? string.Empty;
        }

        private static string ReadableFilterOf(JsonElement board)
        {
            return board.GetProperty("readable_filter").GetString() ?? string.Empty;
        }

        // A raw Table API read. The four assertions above are about the instance rather than about
        // Lighthouse, so they ask it directly instead of through a connector method that would
        // decide something on the way.
        private static async Task<InstanceAnswer> AskTheInstance(string username, string table, string query)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{Password()}")));

            var uri = $"{InstanceUrl().TrimEnd('/')}/api/now/table/{table}?sysparm_limit=1&sysparm_query={Uri.EscapeDataString(query)}";

            using var response = await client.GetAsync(new Uri(uri));
            var body = await response.Content.ReadAsStringAsync();

            var records = new List<JsonElement>();
            var holds = ReportedCount(response);

            if (response.IsSuccessStatusCode)
            {
                using var parsed = JsonDocument.Parse(body);

                if (parsed.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
                {
                    records.AddRange(result.EnumerateArray().Select(record => record.Clone()));
                }
            }

            return new InstanceAnswer(response.StatusCode, records.Count, holds, records);
        }

        private static int ReportedCount(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("X-Total-Count", out var values))
            {
                return 0;
            }

            return int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0;
        }

        private sealed record InstanceAnswer(HttpStatusCode Status, int Visible, int Holds, List<JsonElement> Records);

        // A team naming the kinds of work that are its own. Every read is rooted at the hierarchy,
        // so the classes are the only thing that varies.
        private static Team ATeamCovering(List<string> kindsOfWork, string query, string username)
        {
            var team = ATeamReading(
                query,
                kindsOfWork,
                ["New"],
                ["In Progress", "On Hold", "Assess", "Authorize", "Scheduled", "Implement", "Review"],
                ["Resolved", "Closed", "Canceled"]);

            team.WorkTrackingSystemConnection = CreateConnection(username);

            return team;
        }

        private static Team ATeamReadingIncidents(string query)
        {
            return ATeamReading(query, ["incident"], ["New"], ["In Progress", "On Hold"], ["Resolved", "Closed"]);
        }

        // Every label change_request uses, so nothing is filtered out and the count is purely about
        // how many pages were read.
        private static Team ATeamReadingEveryChange(string query)
        {
            return ATeamReading(
                query,
                [ChangeTable],
                ["New", "Assess"],
                ["Authorize", "Scheduled", "Implement", "Review"],
                ["Closed", "Canceled"]);
        }

        private static Team ATeamReading(
            string query,
            List<string> kindsOfWork,
            List<string> toDoStates,
            List<string> doingStates,
            List<string> doneStates)
        {
            return new Team
            {
                Name = "ServiceNow Integration Test Team",
                DataRetrievalValue = query,
                // Every ServiceNow team names the kinds of work it handles (#5611). Team's own
                // default is the Jira-shaped ["User Story", "Bug"], which no ServiceNow team persists.
                WorkItemTypes = kindsOfWork,
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                WorkTrackingSystemConnection = CreateConnection(AdminUser),
            };
        }

        private static WorkTrackingSystemConnection CreateConnection(
            string username, string? password = null, string? instanceUrl = null)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "ServiceNow Integration Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = instanceUrl ?? InstanceUrl() },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = username },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = password ?? Password(), IsSecret = true },
            ]);

            return connection;
        }

        // Both accessors treat an empty value as absent. A GitHub secret that is not set is still
        // exported as an environment variable holding the empty string, so `??` never fires and the
        // empty value reaches the connector — which is how CI reported `invalid_url` against a
        // perfectly good instance.
        private static string FromEnvironment(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);

            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        // The probe accounts created during the SPIKE share the admin password.
        private static string Password()
        {
            var password = FromEnvironment("ServiceNowLighthouseIntegrationTestToken");

            if (password.Length < 1)
            {
                throw new NotSupportedException("Can run test only if Environment Variable 'ServiceNowLighthouseIntegrationTestToken' is set!");
            }

            return password;
        }

        private static string InstanceUrl()
        {
            var instanceUrl = FromEnvironment("ServiceNowLighthouseIntegrationTestInstance");

            return instanceUrl.Length < 1 ? DefaultInstanceUrl : instanceUrl;
        }

        private static ServiceNowWorkTrackingConnector CreateSubject()
        {
            var cryptoService = new FakeCryptoService();

            return new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                TestAuthStrategyFactory.CreateRealFactory(cryptoService));
        }
    }
}
