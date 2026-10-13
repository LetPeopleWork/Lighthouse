using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// How the Then &amp; Now scenarios reach the system: the real ASP.NET host over a real database, through the
    /// Reports addresses of a Team or a Portfolio. Only the licence and the instance clock are replaced - the
    /// licence because it is external, the clock so that "today", and with it every window, is a day the
    /// scenario names.
    ///
    /// Everything a scenario seeds is a precondition: an owner and the Work Items or Features its tracker holds.
    /// What a report froze, and what it shows now, is only ever put there by the scenario's own action through
    /// those addresses. The few scenarios about rows a later version wrote, or rows an older version wrote,
    /// reshape a stored row directly, because no address of this version can write one.
    /// </summary>
    public abstract class ReportsAcceptanceTest
    {
        protected const string PendingSlice01 = "Epic #5878 slice 01 (#6159) - pending DELIVER";
        protected const string PendingSlice03 = "Epic #5878 slice 03 (#6161) - pending DELIVER";
        protected const string PendingSlice04 = "Epic #5878 slice 04 (#6162) - pending DELIVER";
        protected const string PendingSlice05 = "Epic #5878 slice 05 (#6163) - pending DELIVER";
        protected const string PendingSlice06 = "Epic #5878 slice 06 (#6164) - pending DELIVER";
        protected const string PendingSlice07 = "Epic #5878 slice 07 (#6165) - pending DELIVER";
        protected const string PendingSlice08 = "Epic #5878 slice 08 (#6166) - pending DELIVER";
        protected const string PendingSlice10 = "Epic #5878 slice 10 (#6168) - pending DELIVER";

        protected const string ThenAndNow = "then-and-now";

        protected const string CycleTime = "cycle-time";
        protected const string Throughput = "throughput";
        protected const string Wip = "wip";
        protected const string WorkItemAge = "work-item-age";

        protected const string Lightspeed = "Team Lightspeed";
        protected const string Zenith = "Team Zenith";
        protected const string Apollo = "Project Apollo";

        protected const string EngagementStart = "Then & Now — engagement start";

        protected const string P85 = "p85";
        protected const string P70 = "p70";
        protected const string P50 = "p50";
        protected const string Total = "total";
        protected const string PerDay = "perDay";
        protected const string Average = "average";
        protected const string Min = "min";
        protected const string Max = "max";
        protected const string TotalWindowAverage = "totalWindowAverage";
        protected const string TotalLastDay = "totalLastDay";
        protected const string AverageWindowAverage = "averageWindowAverage";
        protected const string AverageLastDay = "averageLastDay";

        /// <summary>A Then of 90 days ending on the day every scenario is set, 2 Oct 2026: 5 Jul to 2 Oct 2026, both days counted.</summary>
        protected static readonly ReportWindow NinetyDaysToToday = new("2026-07-05", "2026-10-02", 90);

        protected const string TodayOnTheWire = "2026-10-02";

        /// <summary>
        /// The closed set of reasons a value is shown as "—" or a request is refused. The frontend turns each
        /// into words; the server only ever names one of these.
        /// </summary>
        protected static class Reason
        {
            public const string NoFinishedItems = "no-finished-items";
            public const string TooFewPoints = "too-few-points";
            public const string NoProcess = "no-process";
            public const string NoWip = "no-wip";
            public const string LowerLimitZero = "lower-limit-zero";

            public const string ThenTooShort = "then-too-short";
            public const string ThenTooLong = "then-too-long";
            public const string ThenEndsInTheFuture = "then-ends-in-the-future";
            public const string ThenBeforeCutoff = "then-before-cutoff";
            public const string NowTooShort = "now-too-short";
            public const string NowBeforeCutoff = "now-before-cutoff";
            public const string NameMissing = "name-missing";
            public const string NoMetricShown = "no-metric-shown";
            public const string UnknownMetric = "unknown-metric";
            public const string MetricNotCaptured = "metric-not-captured";
            public const string TemplateNotSupported = "template-not-supported";
            public const string ReportCapReached = "report-cap-reached";
        }

        protected static readonly TimeZoneInfo InstanceZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        /// <summary>Friday 2 Oct 2026, a morning in the instance's zone: today, unless a scenario moves the clock.</summary>
        protected static readonly DateOnly TodayOnTheInstance = new(2026, 10, 2);

        private bool premiumLicence;

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected FakeLighthouseClock Clock = null!;

        [SetUp]
        public void Init()
        {
            premiumLicence = false;
            Clock = new FakeLighthouseClock(MorningOf(TodayOnTheInstance), InstanceZone);
            RootFactory = new TestWebApplicationFactory<Program>();

            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(() => premiumLicence);

            Factory = WithAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseService.Object);
                        services.RemoveAll<ILighthouseClock>();
                        services.AddSingleton<ILighthouseClock>(Clock);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var context = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        /// <summary>Sign-in on and roles enforced, as on every instance that has switched authentication on.</summary>
        protected virtual WebApplicationFactory<Program> WithAuthentication(TestWebApplicationFactory<Program> root)
            => TestWebApplicationFactory<Program>.WithTestAuthentication(root);

        protected static DateTimeOffset MorningOf(DateOnly day)
            => new(day.ToDateTime(new TimeOnly(9, 0)), InstanceZone.GetUtcOffset(day.ToDateTime(new TimeOnly(9, 0))));

        /// <summary>Mid-morning UTC, which is the same calendar day in the instance's zone all year round.</summary>
        private static DateTime During(DateOnly day) => day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);

        protected static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // --- The instance ---

        protected void TheInstanceHasNoPremiumLicence() => premiumLicence = false;

        protected void TheInstanceHasAPremiumLicence() => premiumLicence = true;

        protected void ThePremiumLicenceLapses() => premiumLicence = false;

        protected void TodayIs(DateOnly day) => Clock.SetInstant(MorningOf(day));

        // --- Owners ---

        protected OwnerUnderTest ATeam(string name, int keepsFinishedItemsForDays = 365)
        {
            using var scope = Factory.Services.CreateScope();

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = AConnection(),
                DataRetrievalValue = "project = LIGHTSPEED",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = keepsFinishedItemsForDays,
            };

            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();

            return new OwnerUnderTest(OwnerKind.Team, team.Id, name);
        }

        protected OwnerUnderTest APortfolio(string name)
        {
            using var scope = Factory.Services.CreateScope();

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = AConnection(),
                ToDoStates = ["Funnel"],
                DoingStates = ["Implementing"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = 365,
            };

            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolios.Add(portfolio);
            portfolios.Save().GetAwaiter().GetResult();

            return new OwnerUnderTest(OwnerKind.Portfolio, portfolio.Id, name);
        }

        private static WorkTrackingSystemConnection AConnection() => new()
        {
            Name = $"Connection {Guid.NewGuid():N}",
            WorkTrackingSystem = WorkTrackingSystems.Jira,
        };

        // --- What the tracker holds ---

        /// <summary>
        /// Finished Work Items, one per entry: it finished on <c>FinishedOn</c> and its Cycle Time is
        /// <c>TookDays</c> - started that many calendar days before, counting both ends.
        /// </summary>
        protected void TheTeamFinished(OwnerUnderTest team, params Finished[] items)
        {
            using var scope = Factory.Services.CreateScope();
            var owner = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(team.Id)
                ?? throw new InvalidOperationException($"{team.Name} was not seeded");
            var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var item in items)
            {
                var started = item.FinishedOn.AddDays(-(item.TookDays - 1));
                workItems.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = item.ReferenceId ?? $"WI-{Guid.NewGuid():N}",
                    Name = "Finished Work Item",
                    Type = "User Story",
                    State = "Done",
                    StateCategory = StateCategories.Done,
                    CreatedDate = During(started.AddDays(-1)),
                    StartedDate = During(started),
                    ClosedDate = During(item.FinishedOn),
                    Order = "1",
                });
            }

            workItems.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// One day after another from <paramref name="firstDay"/>, the Team finished as many Work Items as the
        /// entry for that day says, each started and finished on that day.
        /// </summary>
        protected void TheTeamFinishedPerDay(OwnerUnderTest team, DateOnly firstDay, params int[] finishedPerDay)
        {
            var items = finishedPerDay
                .SelectMany((count, offset) => Enumerable.Repeat(new Finished(firstDay.AddDays(offset), 1), count))
                .ToArray();

            TheTeamFinished(team, items);
        }

        /// <summary>
        /// <paramref name="count"/> Work Items started on <paramref name="startedOn"/>; they finish on
        /// <paramref name="finishedOn"/>, or are still in progress today when it is not given.
        /// </summary>
        protected void TheTeamWorksOn(OwnerUnderTest team, int count, DateOnly startedOn, DateOnly? finishedOn = null)
        {
            using var scope = Factory.Services.CreateScope();
            var owner = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(team.Id)
                ?? throw new InvalidOperationException($"{team.Name} was not seeded");
            var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            for (var i = 0; i < count; i++)
            {
                workItems.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = $"WIP-{Guid.NewGuid():N}",
                    Name = "Work Item in progress",
                    Type = "User Story",
                    State = finishedOn is null ? "In Progress" : "Done",
                    StateCategory = finishedOn is null ? StateCategories.Doing : StateCategories.Done,
                    CreatedDate = During(startedOn.AddDays(-1)),
                    StartedDate = During(startedOn),
                    ClosedDate = finishedOn is null ? null : During(finishedOn.Value),
                    Order = "1",
                });
            }

            workItems.Save().GetAwaiter().GetResult();
        }

        /// <summary>Finished Features of a Portfolio, one per entry, as <see cref="TheTeamFinished"/> does for Work Items.</summary>
        protected void ThePortfolioFinished(OwnerUnderTest portfolio, params Finished[] features)
        {
            using var scope = Factory.Services.CreateScope();
            var owner = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolio.Id)
                ?? throw new InvalidOperationException($"{portfolio.Name} was not seeded");
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            foreach (var item in features)
            {
                var started = item.FinishedOn.AddDays(-(item.TookDays - 1));
                var feature = new Feature
                {
                    ReferenceId = item.ReferenceId ?? $"FT-{Guid.NewGuid():N}",
                    Name = "Finished Feature",
                    Type = "Epic",
                    State = "Done",
                    StateCategory = StateCategories.Done,
                    CreatedDate = During(started.AddDays(-1)),
                    StartedDate = During(started),
                    ClosedDate = During(item.FinishedOn),
                    Order = "1",
                };
                feature.Portfolios.Add(owner);
                repository.Add(feature);
            }

            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>The tracker stops returning a Work Item, the way a changed query or a deleted item does.</summary>
        protected void TheTrackerNoLongerHolds(string referenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
            var item = workItems.GetByPredicate(candidate => candidate.ReferenceId == referenceId)
                ?? throw new InvalidOperationException($"{referenceId} was not seeded");

            workItems.Remove(item.Id);
            workItems.Save().GetAwaiter().GetResult();
        }

        protected void TheTeamsProcessBehaviourBaselineBecomes(OwnerUnderTest team, DateOnly start, DateOnly end)
        {
            using var scope = Factory.Services.CreateScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var owner = teams.GetById(team.Id) ?? throw new InvalidOperationException($"{team.Name} was not seeded");

            owner.ProcessBehaviourChartBaselineStartDate = During(start);
            owner.ProcessBehaviourChartBaselineEndDate = During(end);
            teams.Update(owner);
            teams.Save().GetAwaiter().GetResult();

            using var readBack = Factory.Services.CreateScope();
            var stored = readBack.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(team.Id)!;
            Assert.That(
                (stored.ProcessBehaviourChartBaselineStartDate, stored.ProcessBehaviourChartBaselineEndDate),
                Is.EqualTo((During(start), During(end))),
                "the Team's process behaviour baseline did not change, so nothing could have moved");
        }

        // --- Rows another version of Lighthouse wrote ---
        // These name the Reports table's columns and its payload's members as the production mapping stores them.

        protected int StoredReports()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = context.Database.GetDbConnection();
            connection.Open();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM \"Reports\"";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>Every column of one stored report, as text, keyed by the column's name.</summary>
        protected Dictionary<string, string?> TheStoredRowOf(int reportId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = context.Database.GetDbConnection();
            connection.Open();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM \"Reports\" WHERE \"Id\" = @id";
                var id = command.CreateParameter();
                id.ParameterName = "@id";
                id.Value = reportId;
                command.Parameters.Add(id);

                using var reader = command.ExecuteReader();
                Assert.That(reader.Read(), Is.True, $"Report {reportId} is not stored");
                return Enumerable.Range(0, reader.FieldCount).ToDictionary(
                    reader.GetName,
                    column => reader.IsDBNull(column) ? null : Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture));
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>The stored report becomes one of a type a later version adds, the way a rollback leaves it.</summary>
        protected void TheStoredReportBecomesOfType(int reportId, string templateKey)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.Database.ExecuteSqlRaw("UPDATE \"Reports\" SET \"TemplateKey\" = {0} WHERE \"Id\" = {1}", templateKey, reportId);
        }

        /// <summary>The stored report's frozen values are reshaped, the way an older or a later version left them.</summary>
        protected void TheStoredPayloadIsReshaped(int reportId, Action<JsonObject> reshape)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = context.Database.GetDbConnection();
            connection.Open();

            string stored;
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT \"TemplatePayloadJson\" FROM \"Reports\" WHERE \"Id\" = @id";
                var id = command.CreateParameter();
                id.ParameterName = "@id";
                id.Value = reportId;
                command.Parameters.Add(id);
                stored = Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)
                    ?? throw new InvalidOperationException($"Report {reportId} has no stored payload");
            }
            finally
            {
                connection.Close();
            }

            var payload = JsonNode.Parse(stored)?.AsObject()
                ?? throw new InvalidOperationException($"Report {reportId}'s payload is not an object");
            reshape(payload);

            context.Database.ExecuteSqlRaw(
                "UPDATE \"Reports\" SET \"TemplatePayloadJson\" = {0} WHERE \"Id\" = {1}", payload.ToJsonString(), reportId);
        }

        /// <summary>
        /// Removes what the report captured for one metric, as if the report was created before that metric
        /// was in the catalog. The captures sit under <c>captures</c>, keyed by metric, whatever their casing.
        /// </summary>
        protected static void WithoutTheCaptureOf(JsonObject payload, string metric, string? member = null)
        {
            var captures = payload.FirstOrDefault(entry => entry.Key.Equals("captures", StringComparison.OrdinalIgnoreCase)).Value?.AsObject()
                ?? throw new InvalidOperationException($"The stored payload holds no captures: {payload}");

            if (member is null)
            {
                captures.Remove(metric);
                return;
            }

            var capture = captures[metric]?.AsObject()
                ?? throw new InvalidOperationException($"The stored payload holds no capture of {metric}: {payload}");
            var key = capture.FirstOrDefault(entry => entry.Key.Equals(member, StringComparison.OrdinalIgnoreCase)).Key
                ?? throw new InvalidOperationException($"The capture of {metric} holds no {member}: {capture}");
            capture.Remove(key);
        }

        // --- Who is asking ---

        protected void TheCallerAdministers(OwnerUnderTest owner)
        {
            if (owner.Kind == OwnerKind.Team)
            {
                Client.AsTeamAdmin(owner.Id);
            }
            else
            {
                Client.AsPortfolioAdmin(owner.Id);
            }
        }

        protected void TheCallerOnlyReads(OwnerUnderTest owner)
        {
            if (owner.Kind == OwnerKind.Team)
            {
                Client.AsTeamViewer(owner.Id);
            }
            else
            {
                Client.AsPortfolioViewer(owner.Id);
            }
        }

        /// <summary>A second admin of the same owner, signed in as somebody else.</summary>
        protected void AnotherAdminOf(OwnerUnderTest owner)
        {
            if (owner.Kind == OwnerKind.Team)
            {
                Client.AsTeamAdmin(owner.Id, "another-team-admin");
            }
            else
            {
                Client.AsPortfolioAdmin(owner.Id, "another-portfolio-admin");
            }
        }

        protected void TheCallerHasNoRoleOnIt() => Client.AsViewer();

        protected void TheCallerAdministersTheInstance() => Client.AsSystemAdmin();

        // --- Driving ports ---

        protected static string ReportsOf(OwnerUnderTest owner)
            => owner.Kind == OwnerKind.Team
                ? $"/api/latest/teams/{owner.Id}/reports"
                : $"/api/latest/portfolios/{owner.Id}/reports";

        protected async Task<JsonElement> TheTypesOfReportFor(OwnerUnderTest owner) => await ReadOk($"{ReportsOf(owner)}/templates");

        protected async Task<JsonElement> TheReportsListOf(OwnerUnderTest owner) => await ReadOk(ReportsOf(owner));

        protected async Task<HttpResponseMessage> AskForTheReportsListOf(OwnerUnderTest owner) => await Client.GetAsync(ReportsOf(owner));

        protected async Task<HttpResponseMessage> CreateAReport(OwnerUnderTest owner, NewReport report)
            => await Send(HttpMethod.Post, ReportsOf(owner), report.ToJson());

        protected async Task<HttpResponseMessage> CreateAReportFromRawRequest(OwnerUnderTest owner, JsonObject request)
            => await Send(HttpMethod.Post, ReportsOf(owner), request);

        protected async Task<HttpResponseMessage> AskForTheReport(OwnerUnderTest owner, int reportId)
            => await Client.GetAsync($"{ReportsOf(owner)}/{reportId}");

        protected async Task<JsonElement> TheReport(OwnerUnderTest owner, int reportId) => await ReadOk($"{ReportsOf(owner)}/{reportId}");

        protected async Task<HttpResponseMessage> EditTheReport(OwnerUnderTest owner, int reportId, JsonObject edit)
            => await Send(HttpMethod.Put, $"{ReportsOf(owner)}/{reportId}", edit);

        protected async Task<HttpResponseMessage> DeleteTheReport(OwnerUnderTest owner, int reportId)
            => await Client.DeleteAsync($"{ReportsOf(owner)}/{reportId}");

        protected async Task<HttpResponseMessage> DeleteTheOwner(OwnerUnderTest owner)
            => await Client.DeleteAsync(owner.Kind == OwnerKind.Team
                ? $"/api/latest/teams/{owner.Id}"
                : $"/api/latest/portfolios/{owner.Id}");

        /// <summary>The Metrics tab's own percentiles and finished items for the same inclusive days.</summary>
        protected async Task<JsonElement> TheMetricsTabCycleTimePercentiles(OwnerUnderTest team, DateOnly start, DateOnly end)
            => await ReadOk($"/api/latest/teams/{team.Id}/metrics/cycleTimePercentiles?startDate={Day(start)}&endDate={Day(end)}");

        protected async Task<JsonElement> TheMetricsTabFinishedItems(OwnerUnderTest team, DateOnly start, DateOnly end)
            => await ReadOk($"/api/latest/teams/{team.Id}/metrics/cycleTimeData?startDate={Day(start)}&endDate={Day(end)}");

        protected async Task<JsonElement> TheMetricsTabThroughput(OwnerUnderTest team, DateOnly start, DateOnly end)
            => await ReadOk($"/api/latest/teams/{team.Id}/metrics/throughput?startDate={Day(start)}&endDate={Day(end)}");

        protected async Task<JsonElement> TheMetricsTabWipOverTime(OwnerUnderTest team, DateOnly start, DateOnly end)
            => await ReadOk($"/api/latest/teams/{team.Id}/metrics/wipOverTime?startDate={Day(start)}&endDate={Day(end)}");

        protected async Task<JsonElement> TheMetricsTabTotalWorkItemAge(OwnerUnderTest team, DateOnly asOf)
            => await ReadOk($"/api/latest/teams/{team.Id}/metrics/totalWorkItemAge?asOfDate={Day(asOf)}");

        protected async Task<JsonElement> ThePlaceOperatorsReadAboutProblems() => await ReadOk("/api/latest/logs/problems");

        private async Task<HttpResponseMessage> Send(HttpMethod method, string address, JsonObject body)
        {
            using var request = new HttpRequestMessage(method, address)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            return await Client.SendAsync(request);
        }

        private async Task<JsonElement> ReadOk(string address)
        {
            using var response = await Client.GetAsync(address);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{address} answered {(int)response.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }

        // --- Shared steps ---

        /// <summary>A report the owner's admin created; the scenario starts from it existing.</summary>
        protected async Task<int> GivenTheAdminCreated(OwnerUnderTest owner, NewReport report)
        {
            TheCallerAdministers(owner);
            using var response = await CreateAReport(owner, report);
            return await ThenItWasCreated(response);
        }

        protected static async Task<int> ThenItWasCreated(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"The report was not created: {body}");

            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("id").GetInt32();
        }

        protected static Task ThenItIsRefusedFor(HttpResponseMessage response, string reason)
            => ThenItIsRefusedWith(response, HttpStatusCode.BadRequest, reason);

        protected static async Task ThenItIsRefusedWith(HttpResponseMessage response, HttpStatusCode status, string reason)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(status), body);
                Assert.That(body, Does.Contain(reason), "the refusal must name its reason from the closed list");
            }
        }

        protected async Task ThenTheOwnerHoldsNoReport(OwnerUnderTest owner)
        {
            TheCallerAdministers(owner);
            Assert.That(ReportNamesIn(await TheReportsListOf(owner)), Is.Empty);
        }

        protected static async Task ThenItIsNotFound(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), await response.Content.ReadAsStringAsync());

        protected static async Task ThenItIsForbidden(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), await response.Content.ReadAsStringAsync());

        // --- Reading the answers ---

        protected static List<string?> ReportNamesIn(JsonElement list)
            => [.. ReportsIn(list).Select(report => TextOf(report, "name"))];

        protected static List<JsonElement> ReportsIn(JsonElement list)
        {
            Assert.That(list.TryGetProperty("reports", out var reports), Is.True, $"The list carries no reports: {list}");
            return [.. reports.EnumerateArray()];
        }

        protected static bool CreationBlockedByCapIn(JsonElement list)
        {
            Assert.That(list.TryGetProperty("creationBlockedByCap", out var blocked), Is.True, $"The list does not say whether the cap is reached: {list}");
            return blocked.GetBoolean();
        }

        protected static ReportWindow ThenWindowIn(JsonElement report) => WindowIn(report, "thenWindow");

        protected static ReportWindow NowWindowIn(JsonElement report) => WindowIn(report, "nowWindow");

        /// <summary>A value Then froze; <paramref name="member"/> is the value's name (p85, total, perDay, average, …).</summary>
        protected static CapturedValue ThenValueIn(JsonElement report, string metric, string member) => ValueIn(report, metric, "atThen", member);

        /// <summary>A value Now shows, computed over the Now window on the day of reading.</summary>
        protected static CapturedValue NowValueIn(JsonElement report, string metric, string member) => ValueIn(report, metric, "atNow", member);

        protected static CapturedValue AboveThenLimitsIn(JsonElement report, string metric) => BeyondThenLimitsIn(report, metric, "above");

        protected static CapturedValue BelowThenLimitsIn(JsonElement report, string metric) => BeyondThenLimitsIn(report, metric, "below");

        protected static CapturedValue Shown(double value, int sampleSize) => new(true, value, sampleSize, null);

        /// <summary>A count of Now's points beyond Then's limits, which carries no sample size of its own in the scenarios.</summary>
        protected static void ThenTheCountIs(CapturedValue count, int expected)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(count.Value, Is.EqualTo(expected));
                Assert.That(count.AbsentReason, Is.Null);
            }
        }

        protected static void ThenItIsADashFor(CapturedValue value, string reason)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.IsPresent, Is.True, "the value must be stated as absent, not left out");
                Assert.That(value.Value, Is.Null, "a value with nothing to compute is never a number, least of all 0");
                Assert.That(value.AbsentReason, Is.EqualTo(reason));
            }
        }

        private static ReportWindow WindowIn(JsonElement report, string which)
        {
            var window = TheThenAndNowOf(report).GetProperty(which);
            return new ReportWindow(
                TextOf(window, "start"),
                TextOf(window, "end"),
                window.TryGetProperty("lengthDays", out var length) && length.ValueKind == JsonValueKind.Number ? length.GetInt32() : null);
        }

        protected static int? DaysSinceThenEndedIn(JsonElement report)
            => TheThenAndNowOf(report).TryGetProperty("daysSinceThenEnded", out var days) && days.ValueKind == JsonValueKind.Number
                ? days.GetInt32()
                : null;

        protected static bool? ThenRebuildableIn(JsonElement report)
            => TheThenAndNowOf(report).TryGetProperty("thenRebuildable", out var rebuildable)
                && rebuildable.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? rebuildable.GetBoolean()
                : null;

        /// <summary>
        /// One value of a metric's panel - <paramref name="side"/> is "atThen" or "atNow", <paramref name="member"/>
        /// the value's name (p85, total, perDay, average, …). A value that is not there at all reads as absent
        /// with no reason, which no scenario accepts as a "—".
        /// </summary>
        private static CapturedValue ValueIn(JsonElement report, string metric, string side, string member)
        {
            var capture = MetricIn(report, metric);
            if (capture is null
                || !capture.Value.TryGetProperty(side, out var sideValues)
                || sideValues.ValueKind != JsonValueKind.Object
                || !sideValues.TryGetProperty(member, out var value)
                || value.ValueKind != JsonValueKind.Object)
            {
                return CapturedValue.Missing;
            }

            return CapturedValue.From(value);
        }

        /// <summary>How many of Now's points lie beyond Then's limits, on one side ("above" or "below").</summary>
        private static CapturedValue BeyondThenLimitsIn(JsonElement report, string metric, string side)
        {
            var capture = MetricIn(report, metric);
            if (capture is null
                || !capture.Value.TryGetProperty("beyondThenLimits", out var beyond)
                || beyond.ValueKind != JsonValueKind.Object
                || !beyond.TryGetProperty(side, out var count)
                || count.ValueKind != JsonValueKind.Object)
            {
                return CapturedValue.Missing;
            }

            return CapturedValue.From(count);
        }

        protected static bool LimitsAreCapturedIn(JsonElement report, string metric)
        {
            var capture = MetricIn(report, metric);
            return capture is not null
                && capture.Value.TryGetProperty("beyondThenLimits", out var beyond)
                && beyond.ValueKind == JsonValueKind.Object;
        }

        /// <summary>A metric the report captured, or null when it is not captured for this report.</summary>
        protected static JsonElement? MetricIn(JsonElement report, string metric)
            => TheThenAndNowOf(report).TryGetProperty("metrics", out var metrics)
                && metrics.ValueKind == JsonValueKind.Object
                && metrics.TryGetProperty(metric, out var capture)
                && capture.ValueKind == JsonValueKind.Object
                ? capture
                : null;

        protected static List<string?> ShownMetricsIn(JsonElement report)
            => report.TryGetProperty("shownMetricKeys", out var shown) && shown.ValueKind == JsonValueKind.Array
                ? [.. shown.EnumerateArray().Select(key => key.GetString())]
                : [];

        protected static List<string?> MetricKeysOfThenAndNowIn(JsonElement templates)
        {
            var thenAndNow = templates.GetProperty("templates").EnumerateArray()
                .FirstOrDefault(template => TextOf(template, "templateKey") == ThenAndNow);

            return thenAndNow.ValueKind == JsonValueKind.Object && thenAndNow.TryGetProperty("metrics", out var metrics)
                ? [.. metrics.EnumerateArray().Select(metric => TextOf(metric, "key"))]
                : [];
        }

        protected static string? TextOf(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static JsonElement TheThenAndNowOf(JsonElement report)
        {
            Assert.That(report.TryGetProperty("thenAndNow", out var thenAndNow) && thenAndNow.ValueKind == JsonValueKind.Object, Is.True,
                $"The report carries no Then & Now part: {report}");
            return thenAndNow;
        }

        /// <summary>The edit an editor saves: name, shown metrics and Now length, echoing the token it read.</summary>
        protected static JsonObject AnEdit(JsonElement asRead, string? name = null, string[]? shown = null, int? nowLengthDays = null)
        {
            var currentNow = NowWindowIn(asRead).LengthDays;
            return new JsonObject
            {
                ["name"] = name ?? TextOf(asRead, "name"),
                ["shownMetricKeys"] = new JsonArray([.. (shown ?? [.. ShownMetricsIn(asRead).Select(key => key ?? string.Empty)]).Select(key => (JsonNode)JsonValue.Create(key))]),
                ["concurrencyToken"] = TextOf(asRead, "concurrencyToken"),
                ["thenAndNow"] = new JsonObject { ["nowLengthDays"] = nowLengthDays ?? currentNow },
            };
        }

        protected enum OwnerKind
        {
            Team,
            Portfolio,
        }

        protected sealed record OwnerUnderTest(OwnerKind Kind, int Id, string Name);

        protected sealed record Finished(DateOnly FinishedOn, int TookDays, string? ReferenceId = null);

        protected sealed record ReportWindow(string? Start, string? End, int? LengthDays);

        /// <summary>
        /// A value as the report states it. A value that is shown carries a number and its sample size; one
        /// that cannot be computed carries no number and one reason from the closed list - never a 0.
        /// </summary>
        protected sealed record CapturedValue(bool IsPresent, double? Value, int? SampleSize, string? AbsentReason)
        {
            public static readonly CapturedValue Missing = new(false, null, null, null);

            public static CapturedValue From(JsonElement value)
            {
                double? number = value.TryGetProperty("value", out var n) && n.ValueKind == JsonValueKind.Number ? n.GetDouble() : null;
                int? sampleSize = value.TryGetProperty("sampleSize", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : null;
                return new CapturedValue(true, number, sampleSize, TextOf(value, "absentReason"));
            }
        }

        /// <summary>What the create step of the wizard sends. Defaults: Then of 90 days ending 2 Oct 2026, the day every scenario is set, Now as long as Then, {Cycle Time} shown.</summary>
        protected sealed record NewReport
        {
            public string TemplateKey { get; init; } = ThenAndNow;
            public string Name { get; init; } = EngagementStart;
            public DateOnly ThenEnd { get; init; } = TodayOnTheInstance;
            public int ThenLengthDays { get; init; } = 90;
            public int? NowLengthDays { get; init; } = 90;
            public IReadOnlyList<string> Shown { get; init; } = [CycleTime];

            public JsonObject ToJson()
            {
                var thenAndNow = new JsonObject
                {
                    ["thenEndDate"] = Day(ThenEnd),
                    ["thenLengthDays"] = ThenLengthDays,
                };

                if (NowLengthDays is not null)
                {
                    thenAndNow["nowLengthDays"] = NowLengthDays;
                }

                return new JsonObject
                {
                    ["templateKey"] = TemplateKey,
                    ["name"] = Name,
                    ["shownMetricKeys"] = new JsonArray([.. Shown.Select(key => (JsonNode)JsonValue.Create(key))]),
                    ["thenAndNow"] = thenAndNow,
                };
            }
        }

    }
}
