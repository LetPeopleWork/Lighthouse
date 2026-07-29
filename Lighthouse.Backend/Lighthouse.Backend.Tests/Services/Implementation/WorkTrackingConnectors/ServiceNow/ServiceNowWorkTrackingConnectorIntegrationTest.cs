using System.Globalization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
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

        private const string MetricsTable = "metric_definition";

        // Held 105 records when slice 02 was written, which is what makes it the table that can
        // prove paging on an instance whose incident table fits in a single page.
        private const string ChangeTable = "change_request";

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
        /// SPIKE Q8: metric_definition returns 403 for every read-only role and opens only at
        /// itil-grade. This is the rung that proves ServiceNow does sometimes deny honestly —
        /// without it, "everything is a silent 200" would be indistinguishable from a bug in the ladder.
        /// </summary>
        [Test]
        public async Task ATableTheCredentialMayNotTouch_IsReportedAsInsufficientPermissions()
        {
            var connection = CreateConnection(NoRolesUser, table: MetricsTable);

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("insufficient_permissions"));
            }
        }

        [Test]
        public async Task ATableTheInstanceDoesNotHave_IsReportedAsAnUnknownTable()
        {
            var connection = CreateConnection(AdminUser, table: "lighthouse_no_such_table");

            var result = await CreateSubject().ValidateConnection(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("unknown_table"));
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
                Assert.That(
                    workItems.SelectMany(item => item.SyncedTransitions),
                    Is.Empty,
                    "AC5: ServiceNow supplies no history to a read-only account, and a fabricated transition would look like measured time-in-state.");
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
        /// ADR-117's headline, live. State 6 (Resolved) leaves <c>closed_at</c> empty — measured, and
        /// re-measured here on every run — so a mapper keying on it alone drops every
        /// resolved-but-not-closed record out of Throughput. Many ITSM shops never move a record past
        /// Resolved, so for them that is the whole chart.
        /// </summary>
        [Test]
        public async Task WorkThatWasResolvedButNeverClosed_ArrivesWithTheDayItFinished()
        {
            var team = ATeamReading("state=6", ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable, [], [], ["Resolved"]);

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty,
                    "This instance holds no resolved-but-not-closed incident any more, so it can no longer prove the rule ADR-117 exists for.");
                Assert.That(workItems.Select(item => item.ClosedDate), Is.All.Not.Null);
                Assert.That(workItems.Select(item => item.ClosedDate?.Kind), Is.All.EqualTo(DateTimeKind.Utc));
            }
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

        private static Team ATeamReadingIncidents(string query)
        {
            return ATeamReading(
                query,
                ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable,
                ["New"],
                ["In Progress", "On Hold"],
                ["Resolved", "Closed"]);
        }

        // Every label change_request uses, so nothing is filtered out and the count is purely about
        // how many pages were read.
        private static Team ATeamReadingEveryChange(string query)
        {
            return ATeamReading(
                query,
                ChangeTable,
                ["New", "Assess"],
                ["Authorize", "Scheduled", "Implement", "Review"],
                ["Closed", "Canceled"]);
        }

        private static Team ATeamReading(
            string query, string table, List<string> toDoStates, List<string> doingStates, List<string> doneStates)
        {
            return new Team
            {
                Name = "ServiceNow Integration Test Team",
                DataRetrievalValue = query,
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                WorkTrackingSystemConnection = CreateConnection(AdminUser, table: table),
            };
        }

        private static WorkTrackingSystemConnection CreateConnection(
            string username,
            string? password = null,
            string? instanceUrl = null,
            string table = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable)
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
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = table, IsOptional = true },
            ]);

            return connection;
        }

        // The probe accounts created during the SPIKE share the admin password.
        private static string Password()
        {
            return Environment.GetEnvironmentVariable("ServiceNowLighthouseIntegrationTestToken")
                ?? throw new NotSupportedException("Can run test only if Environment Variable 'ServiceNowLighthouseIntegrationTestToken' is set!");
        }

        private static string InstanceUrl()
        {
            return Environment.GetEnvironmentVariable("ServiceNowLighthouseIntegrationTestInstance") ?? DefaultInstanceUrl;
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
