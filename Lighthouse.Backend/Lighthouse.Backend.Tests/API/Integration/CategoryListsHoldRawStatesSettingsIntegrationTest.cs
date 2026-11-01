using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
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
        private const string Pending = "pending: category lists hold raw states";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int ownerId;
        private int connectionId;

        private static string[] Listed(params string[] states) => states;

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

            var saved = await Save(Settings(doing: ["Code Review", "Analysis", "Testing", "Waiting"], mappings: [Review()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Code Review", "Analysis", "Testing", "Waiting")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Review", "Analysis", "Waiting")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupOverToDoStates_ShowsItInToDo()
        {
            SeedOwner();

            var saved = await Save(Settings(toDo: ["Intake", "Spec", "Estimate"], doing: ["Analysis"], mappings: [new StateMappingDto { Name = "Prep", States = ["Spec", "Estimate"] }]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "toDoDisplayStates"), Is.EqualTo(Listed("Intake", "Prep")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupOverDoneStates_ShowsItInDone()
        {
            SeedOwner();

            var saved = await Save(Settings(done: ["Released", "Shipped", "Archived"], mappings: [new StateMappingDto { Name = "Finished", States = ["Shipped", "Archived"] }]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doneDisplayStates"), Is.EqualTo(Listed("Released", "Finished")));
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
        public async Task SavingTheGroupsNameInPlaceOfItsStates_StoresTheStatesInPlace()
        {
            SeedOwner();

            var saved = await Save(Settings(doing: ["Analysis", "Review", "Waiting"], mappings: [Review()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingTheOnlyEntryAsTheGroupsName_StoresTheGroupsStateInstead()
        {
            SeedOwner();

            var saved = await Save(Settings(doing: ["In Arbeit"], mappings: [new StateMappingDto { Name = "In Arbeit", States = ["In Progress"] }]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("In Progress")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAnOwnersUnchangedSettings_WhileItsStoredListHoldsACollapsedGroup_WritesRawStates()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Review", "Waiting"]);
            var asStored = await ReadSettings();

            var saved = await Save(Settings(doing: Strings(asStored, "doingStates"), mappings: [Review()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(asStored, "doingStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")), "The stored list is the collapsed one.");
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
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
        public async Task SavingANameBesideOneOfItsGroupsStates_IsRefusedWithTheSentenceTheSettingsPageShows()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["In Arbeit", "In Progress"], mappings: [new StateMappingDto { Name = "In Arbeit", States = ["In Progress"] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(
                    await Errors(response),
                    Has.Some.EqualTo("Mapping \"In Arbeit\" has the same name as the Doing state \"In Arbeit\". Add \"In Arbeit\" to this mapping to group it, or choose another name."));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupOverAState_WhoseEarlierGroupWasDeleted_LeavesNoLeftoverName()
        {
            SeedOwner(mappings: [new StateMappingDto { Name = "A", States = ["X"] }], doing: ["Y", "X"]);

            var saved = await Save(Settings(doing: ["Y", "X"], mappings: []));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Y", "X")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Y", "X")));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupThatListsAnotherGroupsName_IsRefusedWithTheChainSentence()
        {
            SeedOwner();

            var response = await Save(Settings(
                doing: ["X", "Analysis"],
                mappings: [new StateMappingDto { Name = "A", States = ["X"] }, new StateMappingDto { Name = "B", States = ["A"] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await Errors(response), Has.Some.EqualTo("Mapping \"A\" has the same name as a state grouped by mapping \"B\". Choose another name."));
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
        public async Task SavingANewGroupWhoseListedStatesSitInDifferentCategories_IsRefusedNamingTheTwoStates()
        {
            SeedOwner();

            var response = await Save(Settings(toDo: ["Code Review"], doing: ["Testing"], mappings: [Review()]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(
                    await Errors(response),
                    Has.Some.EqualTo("Mapping \"Review\" groups states from different categories: \"Code Review\" is in To Do and \"Testing\" is in Doing. Keep a mapping's states in one category."));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task SavingAGroupWhoseOnlyStateIsListedInTwoCategories_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(toDo: ["Testing"], done: ["Testing"], mappings: [new StateMappingDto { Name = "Review", States = ["Testing"] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await Errors(response), Has.Some.Contains("groups states from different categories"));
            }
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
        public async Task SavingListsWithNullAndBlankEntries_LeavesThemOut()
        {
            SeedOwner();

            var response = await Save(Settings(toDo: ["Backlog", " "], doing: ["Analysis", null!, "\t", "Testing"]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Testing")));
                Assert.That(Strings(settings, "toDoStates"), Is.EqualTo(Listed("Backlog")));
            }
        }

        [Test]
        public async Task SavingAGroupWithABlankNameAlongsideLists_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["Code Review"], mappings: [new StateMappingDto { Name = " ", States = ["Code Review"] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await Errors(response), Has.Some.EqualTo("A state mapping name is required."));
            }
        }

        [Test]
        public async Task SavingAGroupWithNoStatesAlongsideLists_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["Code Review"], mappings: [new StateMappingDto { Name = "Review", States = [] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await Errors(response), Has.Some.EqualTo("State mapping 'Review' must contain at least one state."));
            }
        }

        [Test]
        public async Task SavingTwoGroupsWithTheSameName_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(
                doing: ["Code Review", "Testing"],
                mappings: [new StateMappingDto { Name = "Review", States = ["Code Review"] }, new StateMappingDto { Name = "review", States = ["Testing"] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await Errors(response), Has.Some.EqualTo("Duplicate state mapping name 'Review'."));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task ReadingAnOwnerSavedBeforeTheUpgrade_AfterTheUpgradeRan_ShowsRawStatesAndTheGroupOnDisplay()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Review", "Waiting"]);

            await RunEveryRegisteredSeeder();
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
            }
        }

        [Test]
        public async Task SuggestingStatesAfterSavingRawStatesWithAGroup_OffersTheStatesAndNotTheGroupsName()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Code Review", "Testing"]);

            client.AsSystemAdmin();
            var response = await client.GetAsync($"/api/latest/suggestions/states/{(kind == OwnerKind.Team ? "teams" : "projects")}");
            var body = await response.Content.ReadAsStringAsync();
            var doing = Strings(JsonDocument.Parse(body).RootElement, "doingStates");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(doing, Is.EqualTo(Listed("Analysis", "Code Review", "Testing")));
            }
        }

        private async Task RunEveryRegisteredSeeder()
        {
            using var scope = factory.Services.CreateScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
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

        // A refusal comes back as a JSON array of sentences.
        private static async Task<List<string>> Errors(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(body) ?? [];
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
