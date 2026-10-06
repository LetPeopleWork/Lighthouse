using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// How the sizing-vote scenarios reach the system: the same real host and database as the rest of the
    /// Refinement scenarios, through the Refinement tab's read, the vote, comment and take-back writes and
    /// the per-Work Item log. Every voter is a browser, a client or a signed-in person with a connection of
    /// their own, so what one of them is told can never leak into what another is told.
    ///
    /// Without sign-in a voter is a self-declared name plus a random key the browser keeps; the key travels
    /// in a header on every read and write. With sign-in the voter is the account, and both the name and the
    /// key are ignored.
    ///
    /// Votes are only ever put there by the scenario's own voters through the vote write; nothing seeds a
    /// vote directly.
    /// </summary>
    public abstract class SizingVotesAcceptanceTest : RefinementAcceptanceTest
    {
        protected const string PendingSlice13 = "Epic #5510 slice 13 (#6151) - pending DELIVER";

        protected const string PendingSlice15 = "Epic #5510 slice 15 (#6153) - pending DELIVER";

        protected const string PendingSlice16 = "Epic #5510 slice 16 (#6154) - pending DELIVER";

        protected const string PendingSlice17a = "Epic #5510 slice 17a (#6155) - pending DELIVER";

        protected const string PendingSlice17b = "Epic #5510 slice 17b (#6156) - pending DELIVER";

        protected const string VoterKeyHeader = "X-Lighthouse-Voter-Key";

        protected const string VoterNameRequired = "voter-name-required";

        protected const string VoterKeyRequired = "voter-key-required";

        protected const string VoteNeedsAPerson = "vote-needs-a-person";

        protected const string WorkItemNotInRefinement = "work-item-not-in-refinement";

        // --- Gravity's Work Items ---

        protected const string BillingExport = "GR-040";

        protected const string AdvancedReporting = "GR-051";

        protected const string ApiVersioning = "GR-054";

        protected const string UserActivityTracking = "GR-058";

        protected const string ConfigurationManagement = "GR-073";

        protected const string LoadTesting = "GR-074";

        // --- The people in the scenarios ---

        protected const string JonasWeber = "Jonas Weber";

        protected const string AnaLima = "Ana Lima";

        protected const string MoOkafor = "Mo Okafor";

        protected const string PriyaSharma = "Priya Sharma";

        private const string ApiKeyHeader = "X-Api-Key";

        private const string SignInWithoutRolesScheme = "SizingVotesSignInWithoutRoles";

        private readonly Dictionary<Voter, HttpClient> connections = [];

        /// <summary>The instance the fixture's scenarios run on. Most sizing votes happen without sign-in.</summary>
        protected virtual InstanceUnderTest Instance => InstanceUnderTest.WithoutSignIn;

        protected override WebApplicationFactory<Program> WithAuthentication(TestWebApplicationFactory<Program> root)
            => Instance switch
            {
                InstanceUnderTest.WithoutSignIn => root,
                InstanceUnderTest.WithSignInAndRoles => TestWebApplicationFactory<Program>.WithTestAuthentication(root),
                InstanceUnderTest.WithSignInWithoutRoles => WithSignInButNoRoles(root),
                _ => throw new ArgumentOutOfRangeException(nameof(root), Instance, "No such instance."),
            };

        [TearDown]
        public void CloseEveryVotersConnection()
        {
            foreach (var connection in connections.Values)
            {
                connection.Dispose();
            }

            connections.Clear();
        }

        // --- Voters ---

        /// <summary>A browser on an instance without sign-in: the name its owner declared and the key it keeps.</summary>
        protected static Voter ABrowserOf(string name) => new(name, NewVoterKey());

        /// <summary>A browser whose owner has not declared a name yet.</summary>
        protected static Voter ABrowserWithoutAName() => new(null, NewVoterKey());

        /// <summary>A client - the command line or an assistant - with the name and key its user configured.</summary>
        protected static Voter AClientOf(string name) => new(name, NewVoterKey());

        protected static Voter ASignedInReaderOf(TeamUnderTest team, string subject, string displayName)
            => new(displayName, null, subject, $"{ClaimsDrivenRbacAdministrationService.ViewerTeamGrantPrefix}{team.TeamId}");

        protected static Voter ASignedInAdminOf(TeamUnderTest team, string subject, string displayName)
            => new(displayName, null, subject, $"{ClaimsDrivenRbacAdministrationService.TeamAdminGrantPrefix}{team.TeamId}");

        protected static Voter ASignedInPersonWithoutARoleOn(string subject, string displayName)
            => new(displayName, null, subject, null);

        /// <summary>A voter key long enough to be accepted: sixty-four characters nobody else holds.</summary>
        protected static string NewVoterKey() => $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

        /// <summary>
        /// An API key nobody owns: it authenticates, but no person stands behind it. Created through the
        /// product's own key service with no owner named, which is how such keys come about.
        /// </summary>
        protected async Task<Voter> AnApiKeyThatBelongsToNoPerson()
        {
            using var scope = Factory.Services.CreateScope();
            var keys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var created = await keys.CreateApiKeyAsync("Team bridge", "Shared by a script", string.Empty, string.Empty);

            return new Voter(null, null, ApiKey: created.PlainTextKey);
        }

        /// <summary>An API key a person created for themselves; votes cast with it are theirs.</summary>
        protected async Task<Voter> APersonalApiKeyOf(string subject, string displayName)
        {
            using (var scope = Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                context.UserProfiles.Add(new UserProfile
                {
                    Subject = subject,
                    SubjectClaimType = "sub",
                    DisplayName = displayName,
                    Email = $"{subject}@example.test",
                });
                await context.SaveChangesAsync();
            }

            using var keyScope = Factory.Services.CreateScope();
            var keys = keyScope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var created = await keys.CreateApiKeyAsync($"{displayName}'s terminal", string.Empty, displayName, subject);

            return new Voter(displayName, null, ApiKey: created.PlainTextKey);
        }

        // --- Preconditions ---

        /// <summary>
        /// Team Gravity refines in Backlog, Analysing and Next, and its tracker holds, in backlog order:
        /// GR-040 being implemented (not in refinement), GR-058 in Next, GR-051 and GR-054 in Analysing, and
        /// GR-073 and GR-074 in Backlog. The admin has chosen the refinement states; nobody has voted.
        /// </summary>
        protected async Task<TeamUnderTest> GravityRefinesSixWorkItemsNobodyHasVotedOn(int sleProbability = 85, int sleRange = 7, string name = "Team Gravity")
        {
            var gravity = SeedTeam(name, [Backlog], [Next, Analysing, Implementation], [Done], [], sleProbability, sleRange);

            SeedWorkItems(gravity,
            [
                new TrackerWorkItem(BillingExport, "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
                new TrackerWorkItem(UserActivityTracking, "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedReporting, "Advanced reporting module", Analysing, StateCategories.Doing, "3", StartedDaysAgo: 3),
                new TrackerWorkItem(ApiVersioning, "Public API versioning", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 2),
                new TrackerWorkItem(ConfigurationManagement, "Configuration management", Backlog, StateCategories.ToDo, "5"),
                new TrackerWorkItem(LoadTesting, "Load testing framework", Backlog, StateCategories.ToDo, "6"),
            ]);

            return await TheAdminHasChosen(gravity, Backlog, Analysing, Next);
        }

        /// <summary>
        /// Finished Work Items, each with the cycle time it took and how many days before today it finished.
        /// A cycle time of N days means it started N - 1 days before the day it finished.
        /// </summary>
        protected void SeedFinishedWorkItems(TeamUnderTest team, params FinishedWorkItem[] finished)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var owner = context.Teams.Single(candidate => candidate.Id == team.TeamId);

            foreach (var item in finished)
            {
                var closed = Today.UtcDateTime.Date.AddDays(-item.FinishedDaysAgo).AddHours(12);

                context.WorkItems.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = item.ReferenceId,
                    Name = $"Finished {item.ReferenceId}",
                    Type = "User Story",
                    State = Done,
                    StateCategory = StateCategories.Done,
                    CreatedDate = closed.AddDays(-item.CycleTimeDays - 10),
                    StartedDate = closed.AddDays(-(item.CycleTimeDays - 1)),
                    ClosedDate = closed,
                    Order = "99",
                    Url = TrackerAddressOf(item.ReferenceId),
                });
            }

            context.SaveChanges();
        }

        /// <summary>The voter's vote was accepted; a refused one stops the scenario here, naming why.</summary>
        protected async Task HasVoted(Voter voter, TeamUnderTest team, string workItem, Answer answer, string? comment = null, Channel channel = Channel.Web)
        {
            using var response = await Votes(voter, team, workItem, answer, comment, channel);
            Assert.That(response.IsSuccessStatusCode, Is.True,
                $"{voter.Name ?? "The voter"}'s {answer} on {workItem} was not accepted, so nothing that follows can build on it: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }

        /// <summary>The voter's comment was accepted.</summary>
        protected async Task HasCommented(Voter voter, TeamUnderTest team, string workItem, string comment, Channel channel = Channel.Web)
        {
            using var response = await Comments(voter, team, workItem, comment, channel);
            Assert.That(response.IsSuccessStatusCode, Is.True,
                $"{voter.Name ?? "The voter"}'s comment on {workItem} was not accepted: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }

        /// <summary>The admin has saved this readiness alongside the refinement states the Team already has.</summary>
        protected async Task TheAdminHasSetReadiness(TeamUnderTest team, int minYes, int minVoters, DiscussWhen? discussWhen = null)
        {
            TheCallerAdministersTheTeam(team);

            using var save = await SaveTheReadiness(team, minYes, minVoters, discussWhen);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's readiness was not saved, so nothing that follows can build on it. {await save.Content.ReadAsStringAsync()}");
        }

        // --- Driving ports ---

        protected async Task<HttpResponseMessage> Votes(Voter voter, TeamUnderTest team, string workItem, Answer answer, string? comment = null, Channel channel = Channel.Web)
        {
            var body = new JsonObject
            {
                ["answer"] = answer.ToString(),
                ["channel"] = channel.ToString(),
            };

            if (comment is not null)
            {
                body["comment"] = comment;
            }

            if (voter.DeclaresItsName)
            {
                body["voterName"] = voter.Name;
            }

            return await SendsAVote(voter, team.TeamId, workItem, body);
        }

        /// <summary>A vote exactly as the caller wrote it, for the refusals that are about what it carries.</summary>
        protected async Task<HttpResponseMessage> SendsAVote(Voter voter, int teamId, string workItem, JsonObject body)
            => await ConnectionOf(voter).PostAsync(
                $"{RefinementOf(teamId)}/work-items/{Uri.EscapeDataString(workItem)}/votes",
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));

        protected async Task<HttpResponseMessage> Comments(Voter voter, TeamUnderTest team, string workItem, string comment, Channel channel = Channel.Web)
        {
            var body = new JsonObject
            {
                ["comment"] = comment,
                ["channel"] = channel.ToString(),
            };

            if (voter.DeclaresItsName)
            {
                body["voterName"] = voter.Name;
            }

            return await ConnectionOf(voter).PostAsync(
                $"{RefinementOf(team.TeamId)}/work-items/{Uri.EscapeDataString(workItem)}/comments",
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
        }

        protected async Task<HttpResponseMessage> TakesBackTheirVote(Voter voter, TeamUnderTest team, string workItem)
            => await ConnectionOf(voter).DeleteAsync(
                $"{RefinementOf(team.TeamId)}/work-items/{Uri.EscapeDataString(workItem)}/votes/mine");

        protected async Task<HttpResponseMessage> OpensTheRefinementTab(Voter voter, int teamId)
            => await ConnectionOf(voter).GetAsync(RefinementOf(teamId));

        protected async Task<HttpResponseMessage> OpensTheLog(Voter voter, TeamUnderTest team, string workItem)
            => await ConnectionOf(voter).GetAsync(
                $"{RefinementOf(team.TeamId)}/work-items/{Uri.EscapeDataString(workItem)}/log");

        /// <summary>The Refinement tab as this voter is shown it.</summary>
        protected async Task<JsonElement> TheTabAsSeenBy(Voter voter, TeamUnderTest team)
            => await ReadOkAs(voter, RefinementOf(team.TeamId));

        /// <summary>One Work Item's log as this voter is shown it.</summary>
        protected async Task<JsonElement> TheLogAsSeenBy(Voter voter, TeamUnderTest team, string workItem)
            => await ReadOkAs(voter, $"{RefinementOf(team.TeamId)}/work-items/{Uri.EscapeDataString(workItem)}/log");

        /// <summary>The readiness section with both voter numbers, and the discussion rules only when given.</summary>
        protected async Task<HttpResponseMessage> SaveTheReadiness(TeamUnderTest team, int minYes, int minVoters, DiscussWhen? discussWhen = null)
        {
            var readiness = new JsonObject
            {
                ["minYes"] = minYes,
                ["minVoters"] = minVoters,
            };

            if (discussWhen is not null)
            {
                readiness["discussWhen"] = new JsonObject { ["no"] = discussWhen.No, ["yesIf"] = discussWhen.YesIf };
            }

            return await SaveTheRefinementSection(team, readiness);
        }

        /// <summary>The settings form saved with the Team's current refinement states and the given readiness section, or none.</summary>
        protected async Task<HttpResponseMessage> SaveTheRefinementSection(TeamUnderTest team, JsonObject? readiness)
        {
            var settings = await ReadTheTeamSettings(team);
            var states = RefinementStatesIn(settings).Select(state => (JsonNode)new JsonObject { ["state"] = state.State }).ToArray();

            var refinement = new JsonObject { ["states"] = new JsonArray(states) };
            if (readiness is not null)
            {
                refinement["readiness"] = readiness;
            }

            var payload = TheTeamSettingsFormFor(team);
            payload["refinement"] = refinement;

            return await PutTheTeamSettings(team, payload);
        }

        /// <summary>The settings form saved with the Team's SLE changed to this probability and number of days.</summary>
        protected async Task<TeamUnderTest> TheAdminHasSetTheSle(TeamUnderTest team, int probability, int days)
        {
            TheCallerAdministersTheTeam(team);
            var changed = team with { SleProbability = probability, SleRange = days };

            var settings = await ReadTheTeamSettings(team);
            var payload = TheTeamSettingsFormFor(changed);
            payload["refinement"] = new JsonObject
            {
                ["states"] = new JsonArray([.. RefinementStatesIn(settings).Select(state => (JsonNode)new JsonObject { ["state"] = state.State })]),
            };

            using var save = await PutTheTeamSettings(changed, payload);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin could not set the SLE. {await save.Content.ReadAsStringAsync()}");

            return changed;
        }

        // --- Reading the answers ---

        /// <summary>One row of the Refinement tab, as far as votes are concerned. Absent facts read as null.</summary>
        protected static VotedRowReading RowOf(JsonElement tab, string referenceId)
        {
            Assert.That(tab.TryGetProperty("workItems", out var rows) && rows.ValueKind == JsonValueKind.Array, Is.True,
                $"The Refinement tab's answer carries no list of Work Items. Body: {tab}");

            var row = rows.EnumerateArray().FirstOrDefault(candidate => TextOf(candidate, "referenceId") == referenceId);
            Assert.That(row.ValueKind, Is.EqualTo(JsonValueKind.Object), $"{referenceId} is not listed on the Refinement tab. Body: {tab}");

            return VotedRowReading.From(row);
        }

        protected static bool IsListed(JsonElement tab, string referenceId)
            => tab.TryGetProperty("workItems", out var rows)
                && rows.ValueKind == JsonValueKind.Array
                && rows.EnumerateArray().Any(candidate => TextOf(candidate, "referenceId") == referenceId);

        protected static YardstickReading YardstickIn(JsonElement tab)
        {
            Assert.That(tab.TryGetProperty("yardstick", out var yardstick) && yardstick.ValueKind == JsonValueKind.Object, Is.True,
                $"The Refinement tab's answer does not say what the votes are cast against. Body: {tab}");

            return new YardstickReading(TextOf(yardstick, "source"), NumberOf(yardstick, "days"), NumberOf(yardstick, "probability"));
        }

        protected static int? ReadyByVotesIn(JsonElement tab) => NumberOf(tab, "readyByVotesCount");

        protected static string? VoterIdentityIn(JsonElement tab) => TextOf(tab, "voterIdentity");

        protected static List<LogEntryReading> EntriesIn(JsonElement log)
        {
            if (!log.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return [.. entries.EnumerateArray().Select(LogEntryReading.From)];
        }

        protected static ReadinessReading ReadinessIn(JsonElement settings)
        {
            Assert.That(
                settings.TryGetProperty("refinement", out var refinement)
                    && refinement.ValueKind == JsonValueKind.Object
                    && refinement.TryGetProperty("readiness", out var found)
                    && found.ValueKind == JsonValueKind.Object,
                Is.True,
                $"The Team's settings carry no readiness. Body: {settings}");

            var readiness = settings.GetProperty("refinement").GetProperty("readiness");
            return new ReadinessReading(
                NumberOf(readiness, "minYes"),
                NumberOf(readiness, "minVoters"),
                DiscussWhenIn(readiness));
        }

        // A rule that is off reads as an explicit null, so the reader can tell it apart from a field it never got.
        private static DiscussWhen? DiscussWhenIn(JsonElement readiness)
        {
            if (!readiness.TryGetProperty("discussWhen", out var discussWhen) || discussWhen.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            Assert.That(
                discussWhen.TryGetProperty("no", out _) && discussWhen.TryGetProperty("yesIf", out _),
                Is.True,
                $"The discussion rules must name both thresholds, null for a rule that is off. Body: {discussWhen}");

            return new DiscussWhen(NumberOf(discussWhen, "no"), NumberOf(discussWhen, "yesIf"));
        }

        /// <summary>The refusal code a problem answer names, or null when it names none.</summary>
        protected static async Task<string?> RefusalCodeOf(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                return document.RootElement.ValueKind == JsonValueKind.Object ? TextOf(document.RootElement, "code") : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        protected static int? NumberOf(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : null;

        protected static bool? FlagOf(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : null;

        private static string RefinementOf(int teamId) => $"/api/latest/teams/{teamId}/refinement";

        private async Task<JsonElement> ReadOkAs(Voter voter, string address)
        {
            using var response = await ConnectionOf(voter).GetAsync(address);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{address} answered {(int)response.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }

        private HttpClient ConnectionOf(Voter voter)
        {
            if (connections.TryGetValue(voter, out var existing))
            {
                return existing;
            }

            var connection = Factory.CreateClient();

            if (voter.Key is not null)
            {
                connection.DefaultRequestHeaders.Add(VoterKeyHeader, voter.Key);
            }

            if (voter.ApiKey is not null)
            {
                connection.DefaultRequestHeaders.Add(ApiKeyHeader, voter.ApiKey);
            }

            if (voter.Subject is not null)
            {
                connection.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, voter.Subject);
                connection.DefaultRequestHeaders.Add(TestAuthHandler.DisplayNameHeader, voter.Name ?? voter.Subject);
                if (voter.Roles is not null)
                {
                    connection.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, voter.Roles);
                }
            }

            connections[voter] = connection;
            return connection;
        }

        /// <summary>
        /// Sign-in on, roles not enforced: every signed-in person may do everything, and an API key is
        /// authenticated by the product's own key handler, so a key with no owner arrives as no person.
        /// </summary>
        private static WebApplicationFactory<Program> WithSignInButNoRoles(TestWebApplicationFactory<Program> root)
            => root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "true",
                        ["Authentication:Authority"] = "https://example.test/oidc",
                        ["Authentication:ClientId"] = "lighthouse-test",
                        ["Authentication:ClientSecret"] = "test-secret",
                        ["Authentication:MetadataAddress"] = "https://example.test/oidc/.well-known/openid-configuration",
                        ["Authentication:RequireHttpsMetadata"] = "false",
                        ["Authorization:Enabled"] = "false",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = SignInWithoutRolesScheme;
                        options.DefaultAuthenticateScheme = SignInWithoutRolesScheme;
                        options.DefaultChallengeScheme = SignInWithoutRolesScheme;
                        options.DefaultForbidScheme = SignInWithoutRolesScheme;
                    })
                    .AddPolicyScheme(SignInWithoutRolesScheme, "Sizing votes - sign-in without roles", options =>
                    {
                        options.ForwardDefaultSelector = context =>
                            context.Request.Headers.ContainsKey(ApiKeyHeader)
                                ? SmartAuthSchemeSelector.ApiKeyScheme
                                : TestAuthHandler.SchemeName;
                        options.ForwardChallenge = TestAuthHandler.SchemeName;
                        options.ForwardForbid = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(SmartAuthSchemeSelector.ApiKeyScheme, _ => { });
                });
            });

        // --- Domain types ---

        protected enum InstanceUnderTest
        {
            WithoutSignIn,
            WithSignInAndRoles,
            WithSignInWithoutRoles,
        }

        /// <summary>The three answers to "doable within our SLE?". Their names are what travels.</summary>
        public enum Answer
        {
            Yes,
            YesBut,
            No,
        }

        /// <summary>Where a vote was cast from, as the caller declares it.</summary>
        public enum Channel
        {
            Web,
            LiveSession,
            Cli,
            Assistant,
        }

        /// <summary>
        /// Somebody who votes. Without sign-in: a declared name and the key their browser or client keeps.
        /// With sign-in: an account (subject, display name, grants), or an API key.
        /// </summary>
        protected sealed record Voter(string? Name, string? Key, string? Subject = null, string? Roles = null, string? ApiKey = null)
        {
            /// <summary>Only a voter without an account says who they are; an account or an API key already does.</summary>
            public bool DeclaresItsName => Name is not null && Subject is null && ApiKey is null;
        }

        protected sealed record FinishedWorkItem(string ReferenceId, int CycleTimeDays, int FinishedDaysAgo);

        protected sealed record YardstickReading(string? Source, int? Days, int? Probability);

        protected sealed record ReadinessReading(int? MinYes, int? MinVoters, DiscussWhen? Discussion);

        /// <summary>How many No and how many "Yes, if…" votes send a Work Item to discussion; null turns that rule off.</summary>
        protected sealed record DiscussWhen(int? No, int? YesIf);

        protected sealed record SplitReading(int? Yes, int? YesBut, int? No);

        /// <summary>One row's votes: how many, the reader's own, how they split, and readiness.</summary>
        protected sealed record VotedRowReading(
            string? ReferenceId,
            int? VoteCount,
            string? MyVote,
            SplitReading? Split,
            string? Readiness,
            int? MissingVotes,
            bool? HasComments,
            bool? HasOpenQuestion)
        {
            public static VotedRowReading From(JsonElement row)
            {
                var split = row.TryGetProperty("split", out var splitElement) && splitElement.ValueKind == JsonValueKind.Object
                    ? new SplitReading(NumberOf(splitElement, "yes"), NumberOf(splitElement, "yesBut"), NumberOf(splitElement, "no"))
                    : null;

                return new VotedRowReading(
                    TextOf(row, "referenceId"),
                    NumberOf(row, "voteCount"),
                    TextOf(row, "myVote"),
                    split,
                    TextOf(row, "readiness"),
                    NumberOf(row, "missingVotes"),
                    FlagOf(row, "hasComments"),
                    FlagOf(row, "hasOpenQuestion"));
            }
        }

        /// <summary>One entry of a Work Item's log, oldest first.</summary>
        protected sealed record LogEntryReading(
            string? Kind,
            string? Answer,
            string? Comment,
            string? VoterName,
            string? Channel,
            string? RecordedAt,
            bool? IsMine)
        {
            public static LogEntryReading From(JsonElement entry) => new(
                TextOf(entry, "kind"),
                TextOf(entry, "answer"),
                TextOf(entry, "comment"),
                TextOf(entry, "voterName"),
                TextOf(entry, "channel"),
                TextOf(entry, "recordedAt"),
                FlagOf(entry, "isMine"));
        }
    }
}
