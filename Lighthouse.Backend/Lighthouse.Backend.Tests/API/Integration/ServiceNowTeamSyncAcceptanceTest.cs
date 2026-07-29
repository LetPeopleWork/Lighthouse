using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // Story #5575, US-02 — the walking skeleton for a team's ServiceNow work becoming flow metrics.
    //
    // Everything the flow coach's click traverses is real: the HTTP endpoint, the connector factory,
    // the DI container, the ServiceNow connector, the auth strategy, a real HttpClient making a real
    // request over loopback, the record mapper, the work item service and the persisted work items.
    // The only thing that is not a customer's ServiceNow is the instance itself, which is a local
    // listener answering exactly the way the measured PDI answers — short pages, X-Total-Count, a
    // Link header, and sysparm_display_value=all with universal time in `value` and instance-local
    // time in `display_value`.
    //
    // Layer 5 (real stack): a small number of representative examples, traditional assertions.
    [Category("epic-5513-servicenow")]
    public class ServiceNowTeamSyncAcceptanceTest : IntegrationTestBase
    {
        private const string TeamsOwnQuery = "assignment_group.name=Service Desk^active=true";

        private LocalServiceNowInstance instance;

        [SetUp]
        public void StartInstance()
        {
            instance = LocalServiceNowInstance.Start();
        }

        [TearDown]
        public void StopInstance()
        {
            instance?.Dispose();
        }

        // The walking skeleton, driven the way the flow coach drives it: paste a query into the team
        // settings page, press Validate, and be told the settings are good.
        [Test]
        public async Task AFlowCoachPointingATeamAtTheirOwnServiceNowQuery_IsToldTheirSettingsAreGood()
        {
            instance.MatchesForAQuery = 2;
            var connectionId = await GivenAServiceNowConnection();

            var response = await Client.PostAsJsonAsync("/api/latest/teams/validate", TeamSettings(connectionId, TeamsOwnQuery));
            var verdict = await ReadVerdict(response);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), verdict.Message);
            Assert.That(verdict.IsValid, Is.True);
        }

        // The other half of AC6, and the failure this slice exists to make visible. ServiceNow drops
        // a query term naming a field the table does not have and answers with the whole table. The
        // flow coach finds out on the settings page instead of by trusting a Throughput chart drawn
        // over every incident in the instance.
        [Test]
        public async Task AFlowCoachWhoseQueryTheInstanceSilentlyIgnored_IsStoppedOnTheSettingsPage()
        {
            instance.IgnoresTheQuery = true;
            var connectionId = await GivenAServiceNowConnection();

            var response = await Client.PostAsJsonAsync(
                "/api/latest/teams/validate", TeamSettings(connectionId, "not_a_real_field=whatever"));
            var verdict = await ReadVerdict(response);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(verdict.Code, Is.EqualTo("query_matches_whole_table"));
            Assert.That(verdict.Message, Does.Contain("incident"));
        }

        // AC1, AC2, AC7 together over real HTTP. The day assertion is the load-bearing one: this
        // record's resolution instant falls on the 29th in the instance's own timezone and on the
        // 30th in universal time, and Throughput buckets by day.
        [Test]
        public async Task ATeamsServiceNowWork_ArrivesAsWorkItemsOnTheDaysThroughputCountsBy()
        {
            var team = await GivenATeamReadingItsOwnServiceNowQuery();
            var connector = ConnectorFor(team);

            var workItems = (await connector.GetWorkItemsForTeam(team)).ToList();
            var resolvedItem = workItems.SingleOrDefault(item => item.ReferenceId == "INC0000001");

            Assert.That(workItems, Has.Count.EqualTo(4),
                "Five records exist over three pages; the fifth sits in a label this team never mapped.");
            Assert.That(resolvedItem, Is.Not.Null);
            Assert.That(resolvedItem?.ClosedDate?.Date, Is.EqualTo(new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc)),
                "The record was resolved and never closed, and its universal-time resolution falls on the 30th.");
            Assert.That(resolvedItem?.State, Is.EqualTo("Resolved"),
                "The label the service desk uses, not the choice value.");
        }

        // AC5. ServiceNow cannot supply transition history to a read-only account, and Lighthouse
        // says so rather than guessing: SupportsTransitionHistory is false, the connector brings no
        // fabricated history, and the sync-delta fallback in WorkItemService is what fills the gap —
        // so a time-in-state widget shows something honest rather than a blank chart or a
        // confidently wrong number.
        [Test]
        public async Task TimeInStateOnServiceNowWork_IsDerivedFromObservedChangesRatherThanInventedOrLeftBlank()
        {
            await SeedDatabase();

            var team = await GivenATeamReadingItsOwnServiceNowQuery();
            var workItemService = ServiceProvider.GetRequiredService<IWorkItemService>();

            instance.StateOfTheFirstRecord = ("In Progress", "2");
            await workItemService.UpdateWorkItemsForTeam(team);

            instance.StateOfTheFirstRecord = ("Resolved", "6");
            await workItemService.UpdateWorkItemsForTeam(team);

            var persisted = DatabaseContext.WorkItems.FirstOrDefault(item => item.ReferenceId == "INC0000001");

            Assert.That(ConnectorFor(team).SupportsTransitionHistory(team.WorkTrackingSystemConnection), Is.False);
            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted?.State, Is.EqualTo("Resolved"));
            Assert.That(persisted?.CurrentStateEnteredAt, Is.Not.Null,
                "ServiceNow supplies no history, so the change Lighthouse observed between two syncs is the only honest signal — and it must be captured rather than left blank.");
        }

        private IWorkTrackingConnector ConnectorFor(Team team)
        {
            return ServiceProvider
                .GetRequiredService<IWorkTrackingConnectorFactory>()
                .GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);
        }

        private async Task<int> GivenAServiceNowConnection()
        {
            var connectionRepository = ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            var connection = AServiceNowConnection();

            connectionRepository.Add(connection);
            await connectionRepository.Save();

            return connection.Id;
        }

        private async Task<Team> GivenATeamReadingItsOwnServiceNowQuery()
        {
            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = new Team
            {
                Name = "Service Desk",
                DataRetrievalValue = TeamsOwnQuery,
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                WorkTrackingSystemConnection = AServiceNowConnection(),
            };

            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private WorkTrackingSystemConnection AServiceNowConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = instance.BaseAddress },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Username, Value = "lighthouse.integration" },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.Password, Value = "the-platform-teams-password", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = ServiceNowWorkTrackingOptionNames.WorkItemTable, Value = "incident", IsOptional = true },
            ]);

            return connection;
        }

        private static TeamSettingDto TeamSettings(int connectionId, string query)
        {
            return new TeamSettingDto
            {
                Name = "Service Desk",
                DataRetrievalValue = query,
                WorkTrackingSystemConnectionId = connectionId,
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Resolved", "Closed"],
                ThroughputHistory = 30,
                DoneItemsCutoffDays = 365,
            };
        }

        private static async Task<ConnectionValidationResult> ReadVerdict(HttpResponseMessage response)
        {
            return await response.Content.ReadFromJsonAsync<ConnectionValidationResult>()
                   ?? new ConnectionValidationResult();
        }

        // A ServiceNow instance small enough to run inside the test, faithful enough to be worth
        // running: it honours sysparm_offset, caps its pages at two rows regardless of the requested
        // sysparm_limit the way a real instance does, and reports the true total in X-Total-Count.
        private sealed class LocalServiceNowInstance : IDisposable
        {
            private const int PageSize = 2;

            private readonly HttpListener listener;
            private readonly CancellationTokenSource shutdown = new();

            private LocalServiceNowInstance(HttpListener listener, string baseAddress)
            {
                this.listener = listener;
                BaseAddress = baseAddress;
            }

            public string BaseAddress { get; }

            public bool IgnoresTheQuery { get; set; }

            public int? MatchesForAQuery { get; set; }

            public (string Label, string Value) StateOfTheFirstRecord { get; set; } = ("Resolved", "6");

            public static LocalServiceNowInstance Start()
            {
                var port = AFreePort();
                var baseAddress = $"http://127.0.0.1:{port.ToString(CultureInfo.InvariantCulture)}/";

                var listener = new HttpListener();
                listener.Prefixes.Add(baseAddress);
                listener.Start();

                var instance = new LocalServiceNowInstance(listener, baseAddress);
                _ = Task.Run(instance.Serve);

                return instance;
            }

            public void Dispose()
            {
                shutdown.Cancel();
                listener.Close();
                shutdown.Dispose();
            }

            private async Task Serve()
            {
                while (!shutdown.IsCancellationRequested)
                {
                    HttpListenerContext context;

                    try
                    {
                        context = await listener.GetContextAsync();
                    }
                    catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                    {
                        return;
                    }

                    Answer(context);
                }
            }

            private void Answer(HttpListenerContext context)
            {
                var query = context.Request.Url?.Query ?? string.Empty;

                var isFiltered = query.Contains("sysparm_query=", StringComparison.Ordinal)
                    && !query.Contains("sysparm_query=&", StringComparison.Ordinal);

                var visible = (isFiltered && !IgnoresTheQuery && MatchesForAQuery.HasValue)
                    ? Records().Take(MatchesForAQuery.Value).ToList()
                    : Records();

                var offset = NumberFromQuery(query, "sysparm_offset");
                var page = visible.Skip(offset).Take(PageSize).ToList();
                var body = Encoding.UTF8.GetBytes($"{{\"result\":[{string.Join(",", page)}]}}");

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Add("X-Total-Count", visible.Count.ToString(CultureInfo.InvariantCulture));
                context.Response.Headers.Add("Link", LinkHeader(offset, visible.Count));
                context.Response.ContentLength64 = body.Length;
                context.Response.OutputStream.Write(body, 0, body.Length);
                context.Response.OutputStream.Close();
            }

            private string LinkHeader(int offset, int total)
            {
                var links = new List<string> { $"<{BaseAddress}api/now/table/incident?sysparm_offset=0>;rel=\"first\"" };
                var next = offset + PageSize;

                if (next < total)
                {
                    links.Add($"<{BaseAddress}api/now/table/incident?sysparm_offset={next}>;rel=\"next\"");
                }

                links.Add($"<{BaseAddress}api/now/table/incident?sysparm_offset={Math.Max(0, total - PageSize)}>;rel=\"last\"");

                return string.Join(",", links);
            }

            private List<string> Records()
            {
                return
                [
                    ARecord("INC0000001", StateOfTheFirstRecord.Label, StateOfTheFirstRecord.Value,
                        resolvedDisplay: "2026-07-29 17:25:29", resolvedValue: "2026-07-30 00:25:29"),
                    ARecord("INC0000002", "Resolved", "6", "2026-07-28 09:00:00", "2026-07-28 16:00:00"),
                    ARecord("INC0000003", "In Progress", "2"),
                    ARecord("INC0000004", "New", "1"),
                    ARecord("INC0000005", "Awaiting Vendor", "18"),
                ];
            }

            private static string ARecord(
                string number, string stateLabel, string stateValue, string resolvedDisplay = "", string resolvedValue = "")
            {
                return $$"""
                    {
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

            private static int AFreePort()
            {
                var probe = new TcpListener(System.Net.IPAddress.Loopback, 0);
                probe.Start();
                var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();

                return port;
            }
        }
    }
}
