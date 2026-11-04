using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
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
    /// The one thin pass through the running application for the parent sources a Team keeps: a Team admin
    /// saves an ordered list and reads it back in the same order, and a list that cannot be kept is refused with
    /// a plain message and leaves the saved list as it was. The rules themselves are pinned without a host in
    /// <c>ParentSourcesTest</c>; the host is built once for the fixture.
    /// </summary>
    [TestFixture]
    [Ignore("Story 6501: enable when the Team settings carry the parent source list.")]
    public class ParentSourcesSettingsApiIntegrationTest
    {
        // The text the browser sends for a saved list, kinds as words, an own-parent entry carrying no field.
        // The frontend's save test pins the same two texts; change them together or not at all.
        private const string FieldThenOwnParent = """[{"kind":"AdditionalField","additionalFieldDefinitionId":2},{"kind":"TrackerParent"}]""";

        private const string OwnParentThenField = """[{"kind":"TrackerParent"},{"kind":"AdditionalField","additionalFieldDefinitionId":2}]""";

        private const string OwnParentAlone = """[{"kind":"TrackerParent"}]""";

        private const int TheFieldTheListsNameInTheirText = 2;

        private static readonly string[] FieldThenOwnParentKinds = ["AdditionalField", "TrackerParent"];

        private static readonly string[] OwnParentThenFieldKinds = ["TrackerParent", "AdditionalField"];

        private static readonly string[] TheListATeamStartsWithKinds = ["TrackerParent"];

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
                Assert.That(reordered.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(afterReordering.Select(entry => (string?)entry!["kind"]), Is.EqualTo(OwnParentThenFieldKinds).AsCollection);
            }
        }

        [TestCase(6, "A list holds at most 5 sources", Description = "A sixth source")]
        [TestCase(2, "Caused by is already in the list", Description = "The same source twice")]
        [TestCase(0, "A list needs at least one source", Description = "No source at all")]
        public async Task A_list_that_cannot_be_kept_is_refused_with_a_plain_message_and_nothing_is_saved(int howMany, string message)
        {
            client.AsTeamAdmin(teamId);
            var theListTheTeamStartedWith = await PutTheTeamListing(OwnParentAlone);
            Assert.That(theListTheTeamStartedWith.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var sixDifferentFields = string.Join(",", fieldIds.Select(id => $$"""{"kind":"AdditionalField","additionalFieldDefinitionId":{{id}}}"""));
            var theSameFieldTwice = $$"""{"kind":"AdditionalField","additionalFieldDefinitionId":{{fieldIds[0]}}}""";
            var list = howMany switch
            {
                6 => $"[{sixDifferentFields}]",
                2 => $"[{theSameFieldTwice},{theSameFieldTwice}]",
                _ => "[]",
            };

            var response = await PutTheTeamListing(list);
            var body = await response.Content.ReadAsStringAsync();
            var afterwards = await TheSavedList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain(message));
                Assert.That(afterwards.Select(entry => (string?)entry!["kind"]), Is.EqualTo(TheListATeamStartsWithKinds).AsCollection,
                    "A refused save must leave the list the Team had.");
            }
        }

        private async Task<HttpResponseMessage> PutTheTeamListing(string parentSourcesAsTheBrowserSendsThem)
        {
            var payload = BuildTeamSettingJson();
            payload["ParentOverrideSource"] = "Own";
            payload["ParentSources"] = JsonNode.Parse(parentSourcesAsTheBrowserSendsThem);

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/teams/{teamId}", content);
        }

        private async Task<JsonArray> TheSavedList()
        {
            var response = await client.GetAsync($"/api/latest/teams/{teamId}/settings");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var settings = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            return settings["parentSources"]?.AsArray() ?? throw new AssertionException("The Team's settings carry no parentSources.");
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
    }
}
