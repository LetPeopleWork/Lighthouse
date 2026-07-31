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

        // Created during the SPIKE with sn_incident_read but no sn_problem_read. The asymmetry is what
        // makes AC-B6 provable: incident and problem are the same response shape to this account, and
        // only the ACL-blind X-Total-Count tells them apart (ADR-124).
        private const string RestrictedUser = "lh_probe_snc_read";

        // ServiceNow's work hierarchy. Everything the ITSM applications file lives under it, which is
        // why a team rooted here has to name the kinds of work that are its own (ADR-123 decision 5).
        private const string HierarchyRootTable = "task";

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

        // A team reading the whole task hierarchy and naming the kinds of work that are its own.
        private static Team ATeamCovering(List<string> kindsOfWork, string query, string username)
        {
            var team = ATeamReading(
                query,
                HierarchyRootTable,
                ["New"],
                ["In Progress", "On Hold", "Assess", "Authorize", "Scheduled", "Implement", "Review"],
                ["Resolved", "Closed", "Canceled"]);

            team.WorkItemTypes = kindsOfWork;
            team.WorkTrackingSystemConnection = CreateConnection(username, table: HierarchyRootTable);

            return team;
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
                // A team on a single kind of work names none. Team's own default is the Jira-shaped
                // ["User Story", "Bug"], which no ServiceNow team ever persists (#5611).
                WorkItemTypes = [],
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
