using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// How the scenarios of "every report in one place, created and removed in bulk" reach the system: the real
    /// host over a real SQLite database, through the Reports addresses that belong to no owner - the list of every
    /// report, the creation options, one bulk creation and one bulk removal - and the per-owner addresses the
    /// single wizard uses, which tell what a run left behind.
    ///
    /// Today in every example is 27 Oct 2026. The quarter's comparison has Then 1 May - 29 Jul 2026 and Now the 90
    /// days to today; the month's has Then 29 Aug - 27 Sep 2026 and Now the 30 days to today. Team Equinox keeps
    /// finished work only since 30 Jul 2026, so it cannot hold the quarter's Then.
    ///
    /// Only the licence, the clock and, where a scenario arms one, a change landing between two owners of one run
    /// are stood in; everything else is wired as in production.
    /// </summary>
    public abstract class BulkReportsAcceptanceTest : ReportsAcceptanceTest
    {
        protected const string TheQuarter = "Then & Now — 90 days to 29 Jul 2026";

        protected const string TheMonth = "Then & Now — 30 days to 27 Sep 2026";

        protected const int Ceiling = 50;

        protected const string Created = "created";
        protected const string NotCreated = "notCreated";
        protected const string Removed = "removed";
        protected const string NotRemoved = "notRemoved";

        protected static class RunReason
        {
            public const string OwnerNotFound = "owner-not-found";
            public const string NotAdmin = "not-admin";
            public const string NotSaved = "not-saved";
            public const string AlreadyGone = "already-gone";
        }

        protected static readonly DateOnly TodayInTheExamples = new(2026, 10, 27);

        protected static readonly DateOnly EquinoxKeepsFinishedWorkSince = new(2026, 7, 30);

        protected static readonly NewReport TheQuartersComparison = new()
        {
            Name = TheQuarter,
            ThenEnd = new DateOnly(2026, 7, 29),
            ThenLengthDays = 90,
            NowLengthDays = 90,
            Shown = [CycleTime, Throughput, Wip, WorkItemAge],
        };

        protected static readonly NewReport TheMonthsComparison = new()
        {
            Name = TheMonth,
            ThenEnd = new DateOnly(2026, 9, 27),
            ThenLengthDays = 30,
            NowLengthDays = 30,
            Shown = [CycleTime, Throughput, Wip, WorkItemAge],
        };

        protected readonly ChangesDuringABulkRun DuringTheRun = new();

        private readonly List<string> callersGrants = [];

        protected override WebApplicationFactory<Program> WithAuthentication(TestWebApplicationFactory<Program> root)
            => WithTheRunsStandIns(base.WithAuthentication(root));

        protected WebApplicationFactory<Program> WithTheRunsStandIns(WebApplicationFactory<Program> host)
        {
            var changes = DuringTheRun;
            return host.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                Decorate<IRbacAdministrationService>(services, inner => RbacSeenByABulkRun.Around(inner, changes));
                Decorate<IReportOwnerLookup>(services, inner => OwnerLookupSeenByABulkRun.Around(inner, changes));
            }));
        }

        [SetUp]
        public void SetTheExamplesDay()
        {
            DuringTheRun.Forget();
            callersGrants.Clear();
            reportsBeforeTheRequest = 0;
            TodayIs(TodayInTheExamples);
            TheCallerAdministersTheInstance();
        }

        // --- Owners ---

        /// <summary>A Team that keeps finished Work Items back to <paramref name="since"/>, counted from today in the examples.</summary>
        protected OwnerUnderTest ATeamKeepingFinishedWorkSince(string name, DateOnly since)
            => ATeam(name, keepsFinishedItemsForDays: TodayInTheExamples.DayNumber - since.DayNumber);

        protected OwnerUnderTest APortfolioKeepingFinishedWorkSince(string name, DateOnly since)
        {
            var portfolio = APortfolio(name);
            TheKeptHistoryNowStartsOn(portfolio, since);
            return portfolio;
        }

        /// <summary>The owner's Closed Items Cutoff is changed so that its kept history starts on <paramref name="since"/>.</summary>
        protected void TheKeptHistoryNowStartsOn(OwnerUnderTest owner, DateOnly since)
        {
            var days = TodayInTheExamples.DayNumber - since.DayNumber;
            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database;
            _ = owner.Kind == OwnerKind.Team
                ? database.ExecuteSql($"UPDATE \"Teams\" SET \"DoneItemsCutoffDays\" = {days} WHERE \"Id\" = {owner.Id}")
                : database.ExecuteSql($"UPDATE \"Portfolios\" SET \"DoneItemsCutoffDays\" = {days} WHERE \"Id\" = {owner.Id}");
        }

        /// <summary>Gone from the database, with every report it held, the way deleting it in Lighthouse removes it.</summary>
        protected void TheOwnerIsDeleted(OwnerUnderTest owner)
        {
            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database;
            _ = owner.Kind == OwnerKind.Team
                ? database.ExecuteSql($"DELETE FROM \"Teams\" WHERE \"Id\" = {owner.Id}")
                : database.ExecuteSql($"DELETE FROM \"Portfolios\" WHERE \"Id\" = {owner.Id}");
        }

        // --- Who is asking ---

        /// <summary>The caller, signed in as one person who holds every grant given so far.</summary>
        protected void TheCallerIsAdminOf(params OwnerUnderTest[] owners)
        {
            callersGrants.AddRange(owners.Select(owner => owner.Kind == OwnerKind.Team
                ? $"{ClaimsDrivenRbacAdministrationService.TeamAdminGrantPrefix}{owner.Id}"
                : $"{ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix}{owner.Id}"));
            SignInWithTheGrants();
        }

        protected void TheCallerOnlyReadsEach(params OwnerUnderTest[] owners)
        {
            callersGrants.AddRange(owners.Select(owner => owner.Kind == OwnerKind.Team
                ? $"{ClaimsDrivenRbacAdministrationService.ViewerTeamGrantPrefix}{owner.Id}"
                : $"{ClaimsDrivenRbacAdministrationService.ViewerPortfolioGrantPrefix}{owner.Id}"));
            SignInWithTheGrants();
        }

        private void SignInWithTheGrants()
        {
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            Client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "elena");
            Client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', callersGrants));
        }

        /// <summary>A report the scenario starts from, created through the owner's own address by somebody who may.</summary>
        protected async Task<int> GivenTheOwnerHolds(OwnerUnderTest owner, NewReport report)
        {
            TheCallerAdministersTheInstance();
            using var created = await CreateAReport(owner, report);
            var id = await ThenItWasCreated(created);
            RestoreTheCaller();
            return id;
        }

        private void RestoreTheCaller()
        {
            if (callersGrants.Count == 0)
            {
                TheCallerAdministersTheInstance();
            }
            else
            {
                SignInWithTheGrants();
            }
        }

        // --- What changes while a run is under way ---

        protected void WhenTheRunReaches(OwnerUnderTest owner, Func<bool?> change)
            => DuringTheRun.WhenTheRunReaches(KindOf(owner), owner.Id, change);

        protected void TheOwnerIsDeletedWhenTheRunReaches(OwnerUnderTest owner)
            => WhenTheRunReaches(owner, () =>
            {
                TheOwnerIsDeleted(owner);
                return null;
            });

        protected void TheCallerStopsBeingAdminWhenTheRunReaches(OwnerUnderTest owner)
            => WhenTheRunReaches(owner, () => false);

        protected void ThePremiumLicenceLapsesWhenTheRunReaches(OwnerUnderTest owner)
            => WhenTheRunReaches(owner, () =>
            {
                ThePremiumLicenceLapses();
                return null;
            });

        protected void TheOwnerIsDeletedBetweenLookupAndSave(OwnerUnderTest owner)
            => DuringTheRun.RightAfterTheRunLooksUp(KindOf(owner), owner.Id, () => TheOwnerIsDeleted(owner));

        // --- Driving ports ---

        protected const string EveryReport = "/api/latest/reports";
        protected const string CreationOptions = "/api/latest/reports/creation-options";
        protected const string BulkCreation = "/api/latest/reports/bulk";
        protected const string BulkRemoval = "/api/latest/reports/bulk-removal";

        protected async Task<HttpResponseMessage> AskForEveryReport() => await Client.GetAsync(EveryReport);

        protected async Task<JsonElement> EveryReportTheCallerCanRead() => await AnswerOf(await AskForEveryReport());

        protected async Task<HttpResponseMessage> AskForTheCreationOptions() => await Client.GetAsync(CreationOptions);

        protected async Task<JsonElement> TheCreationOptions() => await AnswerOf(await AskForTheCreationOptions());

        protected async Task<HttpResponseMessage> CreateForEach(NewReport report, params OwnerUnderTest[] owners)
            => await SendTo(BulkCreation, ABulkRun(report, owners));

        protected async Task<HttpResponseMessage> CreateForEachFromRawRequest(JsonObject request)
            => await SendTo(BulkCreation, request);

        protected async Task<HttpResponseMessage> RemoveEach(params int[] reportIds)
            => await SendTo(BulkRemoval, new JsonObject
            {
                ["reportIds"] = new JsonArray([.. reportIds.Select(id => (JsonNode)JsonValue.Create(id))]),
            });

        protected static JsonObject ABulkRun(NewReport report, IEnumerable<OwnerUnderTest> owners)
        {
            var request = report.ToJson();
            request["owners"] = new JsonArray([.. owners.Select(AnOwnerOnTheWire)]);
            return request;
        }

        protected static JsonNode AnOwnerOnTheWire(OwnerUnderTest owner) => new JsonObject
        {
            ["ownerKind"] = owner.Kind == OwnerKind.Team ? "Team" : "Portfolio",
            ["ownerId"] = owner.Id,
        };

        private async Task<HttpResponseMessage> SendTo(string address, JsonObject body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, address)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            return await Client.SendAsync(request);
        }

        protected static async Task<JsonElement> AnswerOf(HttpResponseMessage response)
        {
            using (response)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{response.RequestMessage?.RequestUri} answered {(int)response.StatusCode}: {body}");

                using var document = JsonDocument.Parse(body);
                return document.RootElement.Clone();
            }
        }

        protected async Task ThenTheWholeRequestIsRefusedAndNothingIsWritten(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(StoredReports(), Is.EqualTo(reportsBeforeTheRequest), "a refused request writes and removes nothing");
            }
        }

        private int reportsBeforeTheRequest;

        /// <summary>How many reports are stored before the scenario's request, so a refusal can be shown to have changed nothing.</summary>
        protected void CountTheStoredReports() => reportsBeforeTheRequest = StoredReports();

        // --- Reading the answers ---

        protected sealed record Outcome(string? OwnerKind, int? OwnerId, int? ReportId, string? Status, string? Code, string? EarliestStart, string? OwnerName);

        protected static async Task<List<Outcome>> TheOutcomesOf(HttpResponseMessage response)
        {
            var answer = await AnswerOf(response);
            return [.. answer.GetProperty("outcomes").EnumerateArray().Select(outcome => new Outcome(
                TextOf(outcome, "ownerKind")?.ToLowerInvariant(),
                NumberOf(outcome, "ownerId"),
                NumberOf(outcome, "reportId"),
                TextOf(outcome, "status"),
                TextOf(outcome, "code"),
                TextOf(outcome, "earliestStart"),
                TextOf(outcome, "ownerName")))];
        }

        protected static int? NumberOf(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

        protected static string WireKindOf(OwnerUnderTest owner) => owner.Kind == OwnerKind.Team ? "team" : "portfolio";

        /// <summary>Who each listed report belongs to, as "kind:id", in the order listed.</summary>
        protected static List<string> OwnersIn(JsonElement list)
            => [.. ReportsIn(list).Select(report => $"{TextOf(report, "ownerKind")?.ToLowerInvariant()}:{NumberOf(report, "ownerId")}")];

        protected static string OwnerKey(OwnerUnderTest owner) => $"{WireKindOf(owner)}:{owner.Id}";

        protected async Task<List<string?>> ReportNamesOn(OwnerUnderTest owner)
        {
            TheCallerAdministersTheInstance();
            var names = ReportNamesIn(await TheReportsListOf(owner));
            RestoreTheCaller();
            return names;
        }

        protected async Task<List<JsonElement>> ReportsOn(OwnerUnderTest owner)
        {
            TheCallerAdministersTheInstance();
            var reports = ReportsIn(await TheReportsListOf(owner));
            var read = new List<JsonElement>();
            foreach (var id in reports.Select(report => report.GetProperty("id").GetInt32()))
            {
                read.Add(await TheReport(owner, id));
            }

            RestoreTheCaller();
            return read;
        }

        /// <summary>The earliest Then start the owner's own single create names when it refuses the given report.</summary>
        protected async Task<string?> TheHistoryStartTheSingleCreateNames(OwnerUnderTest owner, NewReport report)
        {
            TheCallerAdministersTheInstance();
            using var refused = await CreateAReport(owner, report);
            var body = await refused.Content.ReadAsStringAsync();
            RestoreTheCaller();

            using var document = JsonDocument.Parse(body);
            return TextOf(document.RootElement, "earliestStart");
        }

        protected static string Iso(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static ReportOwnerKind KindOf(OwnerUnderTest owner)
            => owner.Kind == OwnerKind.Team ? ReportOwnerKind.Team : ReportOwnerKind.Portfolio;

        private static void Decorate<TService>(IServiceCollection services, Func<TService, TService> wrap)
            where TService : class
        {
            var registered = services.Last(descriptor => descriptor.ServiceType == typeof(TService));
            services.RemoveAll<TService>();
            services.Add(new ServiceDescriptor(
                typeof(TService),
                provider => wrap(TheRegistered<TService>(provider, registered)),
                registered.Lifetime));
        }

        private static TService TheRegistered<TService>(IServiceProvider provider, ServiceDescriptor registered)
            => (TService)(registered.ImplementationInstance
                ?? registered.ImplementationFactory?.Invoke(provider)
                ?? ActivatorUtilities.CreateInstance(provider, registered.ImplementationType!));
    }
}
