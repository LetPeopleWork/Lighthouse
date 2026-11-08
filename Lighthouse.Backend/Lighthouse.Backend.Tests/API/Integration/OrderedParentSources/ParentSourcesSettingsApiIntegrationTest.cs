using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OrderedParentSources
{
    /// <summary>
    /// The one thin pass through the running application for the parent sources a Team or a Portfolio keeps: an
    /// admin saves an ordered list and reads it back in the same order, a list that cannot be kept is refused with
    /// a plain message and leaves the saved list as it was, and a client that only knows the old single field
    /// cannot overwrite a list it never saw. The rules themselves are pinned without a host in
    /// <c>ParentSourcesTest</c>; the host is built once for the fixture.
    /// </summary>
    [TestFixture]
    public class ParentSourcesSettingsApiIntegrationTest : IntegrationFixture
    {
        // The text the browser sends for a saved list, kinds as words, an own-parent entry carrying no field.
        // The frontend's save test pins the same two texts; change them together or not at all.
        private const string FieldThenOwnParent = """[{"kind":"AdditionalField","additionalFieldDefinitionId":2},{"kind":"TrackerParent"}]""";

        private const string OwnParentThenField = """[{"kind":"TrackerParent"},{"kind":"AdditionalField","additionalFieldDefinitionId":2}]""";

        private const string OwnParentAlone = """[{"kind":"TrackerParent"}]""";

        private const int TheFieldTheListsNameInTheirText = 2;

        private static readonly string[] FieldThenOwnParentKinds = ["AdditionalField", "TrackerParent"];

        private static readonly string[] OwnParentThenFieldKinds = ["TrackerParent", "AdditionalField"];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int teamId;
        private int connectionId;
        private List<int> fieldIds = [];

        private readonly List<(int OwnerId, OwnerType OwnerType)> ownersSeeded = [];

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program>();

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });
        }

        [SetUp]
        public void Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            ownersSeeded.Clear();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();
            factory.ForgetTheCachedMetrics();

            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedATeamOnAJiraConnectionWithSixFields();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            try
            {
                await SharedHostCleanup.DrainTheHistoryFillerOrFail(
                    factory.Services.GetRequiredService<OverTimeHistoryFiller>(), TimeSpan.FromSeconds(60));
                await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(factory.Services, TimeSpan.FromSeconds(30));
            }
            finally
            {
                await SharedHostCleanup.ForgetOwners(factory.Services, ownersSeeded);

                using (var teardownScope = factory.Services.CreateScope())
                {
                    var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                    await dbContext.Database.EnsureDeletedAsync();
                }

                client.Dispose();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            factory?.Dispose();
            rootFactory?.Dispose();
        }

        [Test]
        public async Task A_Team_admin_saves_two_parent_sources_and_reads_them_back_in_the_same_order()
        {
            client.AsTeamAdmin(teamId);
            Assert.That(fieldIds[1], Is.EqualTo(TheFieldTheListsNameInTheirText),
                "The literal lists below name this field by its number, so the fresh database has to hand it out.");

            var saved = await PutTheTeamListing(FieldThenOwnParent);
            var afterSaving = await TheSavedList();

            var reordered = await PutTheTeamListing(OwnParentThenField);
            var afterReordering = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterSaving.Select(entry => (string?)entry!["kind"]), Is.EqualTo(FieldThenOwnParentKinds).AsCollection,
                    "The order is what decides which parent wins, so it must come back as it was saved.");
                Assert.That((int?)afterSaving[0]!["additionalFieldDefinitionId"], Is.EqualTo(TheFieldTheListsNameInTheirText));
                Assert.That(afterSaving.ToJsonString(), Is.EqualTo(FieldThenOwnParent),
                    "Kinds travel as words and an own-parent entry carries no field, in the very text the browser sends.");
                Assert.That(reordered.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterReordering.Select(entry => (string?)entry!["kind"]), Is.EqualTo(OwnParentThenFieldKinds).AsCollection);
            }
        }

        [TestCase(6, "A list holds at most 5 sources", Description = "A sixth source")]
        [TestCase(2, "Caused by is already in the list", Description = "The same source twice")]
        [TestCase(0, "A list needs at least one source", Description = "No source at all")]
        public async Task A_list_that_cannot_be_kept_is_refused_with_a_plain_message_and_nothing_is_saved(int howMany, string message)
        {
            GivenTheTeamKeeps(ParentSourceEntry.Field(fieldIds[1]), ParentSourceEntry.TrackerParent);
            client.AsTeamAdmin(teamId);

            var response = await PutTheTeamListing(AListOf(howMany));
            var body = await response.Content.ReadAsStringAsync();
            var afterwards = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain(message));
                Assert.That(afterwards.ToJsonString(), Is.EqualTo(FieldThenOwnParent),
                    "A refused save must leave the list the Team had.");
            }
        }

        [TestCase(6, "A list holds at most 5 sources", Description = "A sixth source")]
        [TestCase(2, "Caused by is already in the list", Description = "The same source twice")]
        [TestCase(0, "A list needs at least one source", Description = "No source at all")]
        public async Task A_Portfolios_list_that_cannot_be_kept_is_refused_with_a_plain_message_and_nothing_is_saved(int howMany, string message)
        {
            var portfolioId = GivenAPortfolioThatKeeps(ParentSourceEntry.Field(fieldIds[1]), ParentSourceEntry.TrackerParent);
            client.AsPortfolioAdmin(portfolioId);

            var response = await PutThePortfolioListing(portfolioId, AListOf(howMany));
            var body = await response.Content.ReadAsStringAsync();
            var afterwards = await SettingsAt($"/api/latest/portfolios/{portfolioId}/settings");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain(message));
                Assert.That(afterwards["parentSources"]?.ToJsonString(), Is.EqualTo(FieldThenOwnParent),
                    "A refused save must leave the list the Portfolio had.");
            }
        }

        [Test]
        public async Task An_entry_naming_a_field_the_connection_lacks_is_refused_and_nothing_is_saved()
        {
            GivenTheTeamKeeps(ParentSourceEntry.Field(fieldIds[1]), ParentSourceEntry.TrackerParent);
            client.AsTeamAdmin(teamId);
            var aFieldNoConnectionHolds = fieldIds.Max() + 1;

            var response = await PutTheTeamListing(
                $$"""[{"kind":"AdditionalField","additionalFieldDefinitionId":{{fieldIds[0]}}},{"kind":"AdditionalField","additionalFieldDefinitionId":{{aFieldNoConnectionHolds}}}]""");
            var body = await response.Content.ReadAsStringAsync();
            var afterwards = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("is not an Additional Field of this"));
                Assert.That(afterwards.ToJsonString(), Is.EqualTo(FieldThenOwnParent),
                    "Every entry is judged, not only the first, so a later one can refuse the save too.");
            }
        }

        [TestCase("""[{"kind":"AdditionalField"}]""", Description = "A field entry that names no field")]
        [TestCase("""[{"kind":"TrackerParent","additionalFieldDefinitionId":2}]""", Description = "An own-parent entry that names a field")]
        [TestCase("""[{"additionalFieldDefinitionId":2}]""", Description = "An entry of no kind")]
        [TestCase("""[null]""", Description = "No entry where one belongs")]
        public async Task An_entry_that_is_neither_the_own_parent_nor_a_field_is_refused_and_nothing_is_saved(string list)
        {
            GivenTheTeamKeeps(ParentSourceEntry.Field(fieldIds[1]), ParentSourceEntry.TrackerParent);
            client.AsTeamAdmin(teamId);

            var response = await PutTheTeamListing(list);
            var body = await response.Content.ReadAsStringAsync();
            var afterwards = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("A source is the own Parent or one Additional Field"));
                Assert.That(afterwards.ToJsonString(), Is.EqualTo(FieldThenOwnParent));
            }
        }

        [TestCase("""[{"kind":"TrackerParent"},{"kind":"TrackerParent"}]""", "Parent is already in the list", Description = "The own parent twice")]
        [TestCase("""[{"kind":"AdditionalField","additionalFieldDefinitionId":99},{"kind":"AdditionalField","additionalFieldDefinitionId":99}]""",
            "Unknown field (id 99) is already in the list", Description = "A field the connection does not hold has no name to quote")]
        public async Task A_repeated_entry_that_is_not_a_named_field_is_still_refused_by_what_can_be_said_of_it(string list, string message)
        {
            client.AsTeamAdmin(teamId);

            var response = await PutTheTeamListing(list);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain(message));
            }
        }

        [Test]
        public async Task An_old_clients_echo_leaves_a_richer_list_alone_and_a_changed_old_field_becomes_that_field_then_the_own_parent()
        {
            GivenTheTeamKeeps(ParentSourceEntry.TrackerParent, ParentSourceEntry.Field(fieldIds[0]), ParentSourceEntry.Field(fieldIds[1]));
            client.AsTeamAdmin(teamId);
            var theRicherList = (await TheSavedList()).ToJsonString();

            var echoed = await PutWhatAnOldClientReadBack();
            var afterTheEcho = await TheSavedList();

            var changed = await PutWhatAnOldClientReadBack(fieldIds[2]);
            var afterTheChange = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(echoed.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterTheEcho.ToJsonString(), Is.EqualTo(theRicherList),
                    "The old client sent back the field it read, which is no reason to throw away what it cannot see.");
                Assert.That(changed.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterTheChange.ToJsonString(), Is.EqualTo(
                    $$"""[{"kind":"AdditionalField","additionalFieldDefinitionId":{{fieldIds[2]}}},{"kind":"TrackerParent"}]"""));
            }
        }

        [Test]
        public async Task A_client_that_sends_back_the_list_and_changes_only_the_old_field_gets_that_field_then_the_own_parent()
        {
            GivenTheTeamKeeps(ParentSourceEntry.TrackerParent, ParentSourceEntry.Field(fieldIds[0]), ParentSourceEntry.Field(fieldIds[1]));
            client.AsTeamAdmin(teamId);
            var payload = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            payload["parentOverrideAdditionalFieldDefinitionId"] = fieldIds[2];

            var response = await Put($"/api/latest/teams/{teamId}", payload);
            var afterwards = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterwards.ToJsonString(), Is.EqualTo(
                    $$"""[{"kind":"AdditionalField","additionalFieldDefinitionId":{{fieldIds[2]}}},{"kind":"TrackerParent"}]"""));
            }
        }

        [Test]
        public async Task A_client_that_sends_only_the_list_keeps_it_when_it_equals_what_the_owner_reads()
        {
            GivenTheTeamKeeps(ParentSourceEntry.TrackerParent, ParentSourceEntry.Field(fieldIds[0]), ParentSourceEntry.Field(fieldIds[1]));
            client.AsTeamAdmin(teamId);
            var payload = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            var theList = payload["parentSources"]!.ToJsonString();
            payload.Remove("parentOverrideAdditionalFieldDefinitionId");

            var response = await Put($"/api/latest/teams/{teamId}", payload);
            var afterwards = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterwards.ToJsonString(), Is.EqualTo(theList),
                    "An old field that was never sent is no choice of None.");
            }
        }

        [Test]
        public async Task A_follower_reads_the_list_it_follows_and_a_list_sent_while_following_is_ignored()
        {
            GivenTheConnectionDefaultForTeamsIs(ParentSourceEntry.Field(TheFieldTheListsNameInTheirText), ParentSourceEntry.TrackerParent);
            client.AsTeamAdmin(teamId);

            var whatTheFollowerReads = await SettingsAt($"/api/latest/teams/{teamId}/settings");

            var sentWhileFollowing = await PutTheTeamListing(OwnParentAlone, source: "ConnectionDefault");
            var afterwards = await SettingsAt($"/api/latest/teams/{teamId}/settings");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatTheFollowerReads["parentSources"]?.ToJsonString(), Is.EqualTo(FieldThenOwnParent));
                Assert.That(whatTheFollowerReads["parentSourcesDefault"]?.ToJsonString(), Is.EqualTo(FieldThenOwnParent));
                Assert.That((int?)whatTheFollowerReads["parentOverrideAdditionalFieldDefinitionId"], Is.EqualTo(TheFieldTheListsNameInTheirText),
                    "A client that only knows the old field reads the first field of the list.");
                Assert.That(sentWhileFollowing.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That((string?)afterwards["parentOverrideSource"], Is.EqualTo("ConnectionDefault"));
                Assert.That(afterwards["parentSources"]?.ToJsonString(), Is.EqualTo(FieldThenOwnParent),
                    "A page echoes the list it shows even while following, and that must not turn into an own list.");
            }
        }

        [Test]
        public async Task An_old_client_that_read_the_owner_before_its_list_changed_is_refused_and_one_that_read_it_afterwards_may_choose_None()
        {
            client.AsTeamAdmin(teamId);
            var readByTheStaleClient = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            var savedByAnotherAdmin = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            savedByAnotherAdmin["parentOverrideSource"] = "Own";
            savedByAnotherAdmin["parentSources"] = JsonNode.Parse(FieldThenOwnParent);
            Assert.That((await Put($"/api/latest/teams/{teamId}", savedByAnotherAdmin)).StatusCode, Is.EqualTo(HttpStatusCode.OK));

            readByTheStaleClient.Remove("parentSources");
            readByTheStaleClient["parentOverrideSource"] = "Own";
            readByTheStaleClient["parentOverrideAdditionalFieldDefinitionId"] = null;
            var refused = await Put($"/api/latest/teams/{teamId}", readByTheStaleClient);
            var afterTheRefusal = await TheSavedList();

            var readAfterwards = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            readAfterwards.Remove("parentSources");
            readAfterwards["parentOverrideSource"] = "Own";
            readAfterwards["parentOverrideAdditionalFieldDefinitionId"] = null;
            var chosen = await Put($"/api/latest/teams/{teamId}", readAfterwards);
            var afterTheChoice = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(afterTheRefusal.ToJsonString(), Is.EqualTo(FieldThenOwnParent),
                    "An old client that never saw the list must not wipe it by sending None.");
                Assert.That(chosen.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterTheChoice.ToJsonString(), Is.EqualTo(OwnParentAlone));
            }
        }

        [Test]
        public async Task A_new_Team_that_sends_no_source_follows_the_connections_list()
        {
            GivenTheConnectionDefaultForTeamsIs(ParentSourceEntry.Field(TheFieldTheListsNameInTheirText), ParentSourceEntry.TrackerParent);
            client.AsSystemAdmin();
            var payload = BuildTeamSettingJson();
            payload["Id"] = 0;

            var response = await Put("/api/latest/teams", payload, HttpMethod.Post);
            var created = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            ownersSeeded.Add(((int)created["id"]!, OwnerType.Team));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That((string?)created["parentOverrideSource"], Is.EqualTo("ConnectionDefault"));
                Assert.That(created["parentSources"]?.ToJsonString(), Is.EqualTo(FieldThenOwnParent));
            }
        }

        private string AListOf(int howMany)
        {
            var sixDifferentFields = string.Join(",", fieldIds.Select(id => $$"""{"kind":"AdditionalField","additionalFieldDefinitionId":{{id}}}"""));
            var theSameFieldTwice = $$"""{"kind":"AdditionalField","additionalFieldDefinitionId":{{fieldIds[0]}}}""";

            return howMany switch
            {
                6 => $"[{sixDifferentFields}]",
                2 => $"[{theSameFieldTwice},{theSameFieldTwice}]",
                _ => "[]",
            };
        }

        private async Task<HttpResponseMessage> PutTheTeamListing(string parentSourcesAsTheBrowserSendsThem, string source = "Own")
        {
            var payload = BuildTeamSettingJson();
            payload["ParentOverrideSource"] = source;
            payload["ParentSources"] = JsonNode.Parse(parentSourcesAsTheBrowserSendsThem);

            return await Put($"/api/latest/teams/{teamId}", payload);
        }

        private async Task<HttpResponseMessage> PutThePortfolioListing(int portfolioId, string parentSourcesAsTheBrowserSendsThem)
        {
            var payload = BuildPortfolioSettingJson(portfolioId);
            payload["ParentOverrideSource"] = "Own";
            payload["ParentSources"] = JsonNode.Parse(parentSourcesAsTheBrowserSendsThem);

            return await Put($"/api/latest/portfolios/{portfolioId}", payload);
        }

        // An old client knows nothing of the list, so what it read comes back without it.
        private async Task<HttpResponseMessage> PutWhatAnOldClientReadBack(int? theFieldItChanged = null)
        {
            var payload = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            payload.Remove("parentSources");
            payload.Remove("parentSourcesDefault");
            if (theFieldItChanged is { } field)
            {
                payload["parentOverrideAdditionalFieldDefinitionId"] = field;
            }

            return await Put($"/api/latest/teams/{teamId}", payload);
        }

        private async Task<HttpResponseMessage> Put(string route, JsonObject payload, HttpMethod? method = null)
        {
            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return method == HttpMethod.Post ? await client.PostAsync(route, content) : await client.PutAsync(route, content);
        }

        private async Task<JsonArray> TheSavedList()
        {
            var settings = await SettingsAt($"/api/latest/teams/{teamId}/settings");
            return settings["parentSources"]?.AsArray() ?? throw new AssertionException("The Team's settings carry no parentSources.");
        }

        private async Task<JsonObject> SettingsAt(string route)
        {
            var response = await client.GetAsync(route);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        }

        private void GivenTheTeamKeeps(params ParentSourceEntry[] entries)
        {
            using var scope = factory.Services.CreateScope();
            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = teamRepository.GetById(teamId)!;
            team.OwnParentSources(ParentSources.Of(entries).List!);
            teamRepository.Update(team);
            teamRepository.Save().GetAwaiter().GetResult();
        }

        private void GivenTheConnectionDefaultForTeamsIs(params ParentSourceEntry[] entries)
        {
            using var scope = factory.Services.CreateScope();
            var connectionRepository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = connectionRepository.GetById(connectionId)!;
            connection.SetParentSourcesDefaultForTeams(ParentSources.Of(entries).List);
            connectionRepository.Update(connection);
            connectionRepository.Save().GetAwaiter().GetResult();
        }

        private int GivenAPortfolioThatKeeps(params ParentSourceEntry[] entries)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
            };
            portfolio.OwnParentSources(ParentSources.Of(entries).List!);

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            ownersSeeded.Add((portfolio.Id, OwnerType.Portfolio));
            return portfolio.Id;
        }

        private void SeedATeamOnAJiraConnectionWithSixFields()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = "Caused by", Reference = "customfield_10500" });
            foreach (var number in Enumerable.Range(2, 5))
            {
                connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = $"Field {number}", Reference = $"customfield_1050{number}" });
            }

            var team = new Team { Name = $"Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            teamId = team.Id;
            connectionId = connection.Id;
            fieldIds = [.. connection.AdditionalFieldDefinitions.Select(field => field.Id)];
            ownersSeeded.Add((team.Id, OwnerType.Team));
        }

        private JsonObject BuildTeamSettingJson()
        {
            var dto = new TeamSettingDto
            {
                Id = teamId,
                Name = $"Team {teamId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = connectionId,
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false,
                FeatureWIP = 1,
                AutomaticallyAdjustFeatureWIP = false,
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };

            return JsonNode.Parse(JsonSerializer.Serialize(dto))!.AsObject();
        }

        private JsonObject BuildPortfolioSettingJson(int portfolioId)
        {
            var dto = new PortfolioSettingDto
            {
                Id = portfolioId,
                Name = $"Portfolio {portfolioId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = connectionId,
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };

            return JsonNode.Parse(JsonSerializer.Serialize(dto))!.AsObject();
        }
    }
}
