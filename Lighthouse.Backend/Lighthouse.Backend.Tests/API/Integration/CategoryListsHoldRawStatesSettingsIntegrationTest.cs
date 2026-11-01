using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// What a Team or a Portfolio admin can do with the To Do, Doing and Done lists and the state mappings over the
    /// settings endpoints: the lists hold the tracker's own states, a mapping only groups them on display.
    /// </summary>
    [TestFixture(OwnerKind.Team)]
    [TestFixture(OwnerKind.Portfolio)]
    public class CategoryListsHoldRawStatesSettingsIntegrationTest(OwnerKind kind)
    {
        private static string[] Listed(params string[] states) => states;

        private const string Pending = "pending: category lists hold raw states";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int ownerId;
        private int connectionId;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                }));

            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingRawStatesWithAGroup_ReadsBackTheSameStatesAndTheGroupOnDisplay()
        {
            SeedOwner();

            var saved = await Save(Settings(doing: ["Analysis", "Code Review", "Testing", "Waiting"], mappings: [Review()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
                Assert.That(Strings(settings, "toDoDisplayStates"), Is.EqualTo(Listed("Backlog")));
                Assert.That(Strings(settings, "doneDisplayStates"), Is.EqualTo(Listed("Done")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingGroupStatesThatAreApart_ShowsTheGroupWhereItsFirstStateIs()
        {
            SeedOwner();

            await Save(Settings(doing: ["Code Review", "Analysis", "Testing", "Waiting"], mappings: [Review()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Code Review", "Analysis", "Testing", "Waiting")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Review", "Analysis", "Waiting")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupInToDoAndDone_ShowsItThereToo()
        {
            SeedOwner();

            await Save(Settings(
                toDo: ["Intake", "Spec", "Estimate"],
                doing: ["Analysis"],
                done: ["Estimate", "Released"],
                mappings: [new StateMappingDto { Name = "Prep", States = ["Spec", "Estimate"] }]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "toDoDisplayStates"), Is.EqualTo(Listed("Intake", "Prep")));
                Assert.That(Strings(settings, "doneDisplayStates"), Is.EqualTo(Listed("Prep", "Released")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupNamedAfterOneOfItsStatesWithoutTheOthers_AddsTheOthersRightAfterIt()
        {
            SeedOwner();

            await Save(Settings(doing: ["Analysis", "Active", "Blocked"], mappings: [ActiveGroup()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Active", "Blocked")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingTheGroupAndTheListInOneSave_ReadsTheListAgainstTheGroupJustSent()
        {
            SeedOwner(mappings: [ActiveGroup()], doing: ["Analysis", "Active", "Resolved"]);

            var saved = await Save(Settings(doing: ["Active"], mappings: [new StateMappingDto { Name = "Active", States = ["Active", "Blocked"] }]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Active", "Blocked")),
                    "The group sent with the list is the one that counts, not the one stored before.");
            }
        }

        [Test]
        public async Task SavingTheListWithoutTheGroupThatStoodBehindAState_DoesNotBringTheGroupsOtherStatesBack()
        {
            SeedOwner(mappings: [ActiveGroup()], doing: ["Analysis", "Active", "Resolved"]);

            var saved = await Save(Settings(doing: ["Analysis", "Active"], mappings: []));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Active")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingTheSameSettingsTwice_ChangesNothingTheSecondTime()
        {
            SeedOwner();
            var legacy = Settings(doing: ["Analysis", "Active", "Blocked"], mappings: [ActiveGroup()]);

            await Save(legacy);
            var afterFirst = Strings(await ReadSettings(), "doingStates");
            await Save(Settings(doing: afterFirst, mappings: [ActiveGroup()]));
            var afterResendingWhatWasRead = Strings(await ReadSettings(), "doingStates");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterFirst, Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
                Assert.That(afterResendingWhatWasRead, Is.EqualTo(afterFirst));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAListWhoseEntryIsAGroupNameAndStillListsTheGroupsState_IsRefusedWithTheFix()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["In Arbeit", "In Progress"], mappings: [new StateMappingDto { Name = "In Arbeit", States = ["In Progress"] }]));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("In Arbeit"));
                Assert.That(body, Does.Contain("Rename the mapping"));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAListWhoseOnlyEntryIsTheGroupName_IsRefusedInsteadOfLosingTheTrackersState()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["In Arbeit"], mappings: [new StateMappingDto { Name = "In Arbeit", States = ["In Progress"] }]));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("Rename the mapping"));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupOverAState_WhoseEarlierGroupWasDeleted_LeavesNoLeftoverName()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["Y", "X"], mappings: [new StateMappingDto { Name = "B", States = ["X"] }]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Y", "X")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Y", "B")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupThatListsAnotherGroupsName_IsRefusedNamingBothGroups()
        {
            SeedOwner();

            var response = await Save(Settings(
                doing: ["X", "Analysis"],
                mappings: [new StateMappingDto { Name = "A", States = ["X"] }, new StateMappingDto { Name = "B", States = ["A"] }]));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("\"A\"").Or.Contain("'A'"));
                Assert.That(body, Does.Contain("\"B\"").Or.Contain("'B'"));
                Assert.That(body, Does.Contain("another mapping"));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupWithAStateListedNowhere_IsAcceptedWithoutAWarning()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["Code Review"], mappings: [Review()]));
            var body = await response.Content.ReadAsStringAsync();
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(body, Does.Not.Contain("warning").IgnoreCase);
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Review")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingANewGroupWhoseListedStatesSitInDifferentCategories_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(toDo: ["Code Review"], doing: ["Testing"], mappings: [Review()]));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task SavingAnUnrelatedChange_WhileAnOlderGroupSpansCategories_StillSaves()
        {
            SeedOwner(mappings: [Review()], toDo: ["Backlog", "Code Review"], doing: ["Testing"]);

            var response = await Save(Settings(toDo: ["Backlog", "Code Review"], doing: ["Testing"], mappings: [Review()], name: "Renamed"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        [Test]
        [Ignore(Pending)]
        public async Task ReadingAnOwnerSavedBeforeTheUpgrade_ShowsRawStatesAndTheGroupOnDisplay()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Code Review", "Testing"]);

            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Review")));
            }
        }

        private static StateMappingDto Review() => new() { Name = "Review", States = ["Code Review", "Testing"] };

        private static StateMappingDto ActiveGroup() => new() { Name = "Active", States = ["Active", "Resolved"] };

        private SettingsOwnerDtoBase Settings(
            string[]? toDo = null,
            string[]? doing = null,
            string[]? done = null,
            StateMappingDto[]? mappings = null,
            string? name = null)
        {
            SettingsOwnerDtoBase dto = kind == OwnerKind.Team
                ? new TeamSettingDto { ThroughputHistory = 30, FeatureWIP = 1, DoneItemsCutoffDays = 365 }
                : new PortfolioSettingDto
                {
                    UsePercentileToCalculateDefaultAmountOfWorkItems = false,
                    DefaultWorkItemPercentile = 85,
                    PercentileHistoryInDays = 90,
                    DefaultAmountOfWorkItemsPerFeature = 25,
                    DoneItemsCutoffDays = 365,
                };

            dto.Id = ownerId;
            dto.Name = name ?? $"Owner {ownerId}";
            dto.DataRetrievalValue = "project = TEST";
            dto.WorkTrackingSystemConnectionId = connectionId;
            dto.WorkItemTypes = ["User Story"];
            dto.ToDoStates = [.. toDo ?? ["Backlog"]];
            dto.DoingStates = [.. doing ?? ["Active"]];
            dto.DoneStates = [.. done ?? ["Done"]];
            dto.StateMappings = [.. mappings ?? []];
            return dto;
        }

        private async Task<HttpResponseMessage> Save(SettingsOwnerDtoBase dto)
        {
            client.AsSystemAdmin();
            var content = new StringContent(JsonSerializer.Serialize(dto, dto.GetType()), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/{Route}/{ownerId}", content);
        }

        private async Task<JsonElement> ReadSettings()
        {
            client.AsSystemAdmin();
            var response = await client.GetAsync($"/api/latest/{Route}/{ownerId}/settings");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonDocument.Parse(body).RootElement.Clone();
        }

        private string Route => kind == OwnerKind.Team ? "teams" : "portfolios";

        private static string[] Strings(JsonElement settings, string property)
        {
            Assert.That(settings.TryGetProperty(property, out var list), Is.True, $"The settings carry no {property}: {settings}");
            return [.. list.EnumerateArray().Select(item => item.GetString()!)];
        }

        private void SeedOwner(StateMappingDto[]? mappings = null, string[]? toDo = null, string[]? doing = null)
        {
            using var scope = factory.Services.CreateScope();
            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var stored = (mappings ?? []).Select(m => new StateMapping { Name = m.Name, States = [.. m.States] }).ToList();

            if (kind == OwnerKind.Team)
            {
                var team = new Team { Name = $"Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection, StateMappings = stored };
                ApplyLists(team, toDo, doing);
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                repository.Add(team);
                repository.Save().GetAwaiter().GetResult();
                ownerId = team.Id;
            }
            else
            {
                var portfolio = new Portfolio { Name = $"Portfolio {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection, StateMappings = stored };
                ApplyLists(portfolio, toDo, doing);
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                repository.Add(portfolio);
                repository.Save().GetAwaiter().GetResult();
                ownerId = portfolio.Id;
            }

            connectionId = connection.Id;
        }

        private static void ApplyLists(WorkTrackingSystemOptionsOwner owner, string[]? toDo, string[]? doing)
        {
            owner.ToDoStates = [.. toDo ?? ["Backlog"]];
            owner.DoingStates = [.. doing ?? ["Active"]];
            owner.DoneStates = ["Done"];
        }
    }

    public enum OwnerKind
    {
        Team,
        Portfolio,
    }
}
