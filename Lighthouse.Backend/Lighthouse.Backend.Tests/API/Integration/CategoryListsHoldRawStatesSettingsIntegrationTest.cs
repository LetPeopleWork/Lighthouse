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
        public async Task SavingAGroupNamedAfterOneOfItsStatesWithoutTheOthers_AddsTheOthersRightAfterIt()
        {
            SeedOwner();

            await SaveExpectingOk(Settings(doing: ["Analysis", "Active", "Blocked"], mappings: [ActiveGroup()]));
            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Active", "Blocked")));
            }
        }

        [Test]
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
        public async Task SavingAnOwnersUnchangedSettings_WhileItsStoredListHoldsACollapsedGroup_WritesRawStates()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Review", "Waiting"]);
            var asStored = StoredDoingStates();

            var saved = await Save(Settings(doing: asStored, mappings: [Review()]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asStored, Is.EqualTo(Listed("Analysis", "Review", "Waiting")), "The stored list is the collapsed one.");
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
                Assert.That(StoredDoingStates(), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
            }
        }

        [Test]
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
        public async Task SavingTheSameSettingsTwice_ChangesNothingTheSecondTime()
        {
            SeedOwner();
            var legacy = Settings(doing: ["Analysis", "Active", "Blocked"], mappings: [ActiveGroup()]);

            await SaveExpectingOk(legacy);
            var afterFirst = Strings(await ReadSettings(), "doingStates");
            await SaveExpectingOk(Settings(doing: afterFirst, mappings: [ActiveGroup()]));
            var afterResendingWhatWasRead = Strings(await ReadSettings(), "doingStates");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterFirst, Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
                Assert.That(afterResendingWhatWasRead, Is.EqualTo(afterFirst));
            }
        }

        [Test]
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

            await AssertNothingWasStored();
        }

        [Test]
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

            await AssertNothingWasStored();
        }

        [Test]
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

            await AssertNothingWasStored();
        }

        [Test]
        public async Task SavingAGroupWhoseOnlyStateIsListedInTwoCategories_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(toDo: ["Testing"], done: ["Testing"], mappings: [new StateMappingDto { Name = "Review", States = ["Testing"] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await Errors(response), Has.Some.EqualTo("Mapping \"Review\" groups \"Testing\", which is in both To Do and Done. Keep a mapping's states in one category."));
            }

            await AssertNothingWasStored();
        }

        [Test]
        public async Task SavingAnUnrelatedChange_WhileAnOlderGroupSpansCategories_StillSaves()
        {
            SeedOwner(mappings: [Review()], toDo: ["Backlog", "Code Review"], doing: ["Testing"]);

            var response = await Save(Settings(toDo: ["Backlog", "Code Review"], doing: ["Testing"], mappings: [Review()], name: "Renamed"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        [Test]
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

        // The settings page sends the stored cycle time definitions back on every save. One stored before a group
        // covered both its states would be refused if it were judged again, and then no setting could be changed.
        [Test]
        public async Task SavingAnOwnersUnchangedSettings_WhoseStoredCycleTimeRunsInsideOneGroup_Saves()
        {
            if (kind != OwnerKind.Team)
            {
                Assert.Ignore("A Portfolio save does not validate its cycle time definitions, so this cannot fail for a Portfolio.");
            }

            SeedOwner(mappings: [Review()], doing: ["Code Review", "Testing"], cycleTime: new CycleTimeDefinition { Name = "Review Time", StartState = "Code Review", EndState = "Testing" });
            var storedCycleTimes = (await ReadSettings()).GetProperty("cycleTimeDefinitions").EnumerateArray()
                .Select(definition => new CycleTimeDefinitionDto
                {
                    Id = definition.GetProperty("id").GetInt32(),
                    Name = definition.GetProperty("name").GetString()!,
                    StartState = definition.GetProperty("startState").GetString()!,
                    EndState = definition.GetProperty("endState").GetString()!,
                })
                .ToList();
            var unchanged = Settings(doing: ["Code Review", "Testing"], mappings: [Review()]);
            unchanged.CycleTimeDefinitions = storedCycleTimes;

            var saved = await Save(unchanged);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedCycleTimes, Has.Count.EqualTo(1), "The cycle time definition was stored.");
                Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
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

            await AssertNothingWasStored();
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

            await AssertNothingWasStored();
        }

        [Test]
        public async Task SavingAGroupWithANullName_IsRefusedAndStoresNothing()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["Code Review"], mappings: [new StateMappingDto { Name = null!, States = ["Code Review"] }]));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await response.Content.ReadAsStringAsync());

            await AssertNothingWasStored();
        }

        [Test]
        public async Task SavingAGroupWithNullStates_IsRefusedAndStoresNothing()
        {
            SeedOwner();

            var response = await Save(Settings(doing: ["Code Review"], mappings: [new StateMappingDto { Name = "Review", States = null! }]));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await response.Content.ReadAsStringAsync());

            await AssertNothingWasStored();
        }

        [Test]
        public async Task SavingTwoGroupsOverTheSameStatePaddedDifferently_IsRefused()
        {
            SeedOwner();

            var response = await Save(Settings(
                doing: ["A"],
                mappings: [new StateMappingDto { Name = "G1", States = ["A"] }, new StateMappingDto { Name = "G2", States = ["A "] }]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await response.Content.ReadAsStringAsync());
                Assert.That(await Errors(response), Has.Some.EqualTo("Source state 'A' appears in multiple mappings: 'G1', 'G2'."));
            }

            await AssertNothingWasStored();
        }

        [Test]
        public async Task SavingAnUnrelatedChange_WhileAStoredNameShadowsAState_StillSaves()
        {
            SeedOwner(mappings: [InArbeit()], doing: ["In Arbeit", "In Progress"]);

            var response = await Save(Settings(doing: ["In Arbeit", "In Progress"], mappings: [InArbeit()], name: "Renamed"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task SavingAnUnrelatedChange_WhileAStoredGroupListsAnotherGroupsName_StillSaves()
        {
            StateMappingDto[] chained = [new() { Name = "A", States = ["X"] }, new() { Name = "B", States = ["A"] }];
            SeedOwner(mappings: chained, doing: ["X", "Analysis"]);

            var response = await Save(Settings(doing: ["X", "Analysis"], mappings: chained, name: "Renamed"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task SavingANameBesideItsGroupsState_IntoAStoredGroupsList_IsRefused()
        {
            SeedOwner(mappings: [InArbeit()], doing: ["In Progress"]);

            var response = await Save(Settings(doing: ["In Arbeit", "In Progress"], mappings: [InArbeit()]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await response.Content.ReadAsStringAsync());
                Assert.That(
                    await Errors(response),
                    Has.Some.EqualTo("Mapping \"In Arbeit\" has the same name as the Doing state \"In Arbeit\". Add \"In Arbeit\" to this mapping to group it, or choose another name."));
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

            await AssertNothingWasStored();
        }

        [Test]
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

        // An owner the start-up pass has not rewritten (skipped, not yet reached, or saved by an older instance)
        // reads through the classifier, so its pages and suggestions already show the states.
        [Test]
        public async Task ReadingAnOwnerWhoseStoredListStillHoldsAGroupName_ShowsTheGroupsStatesAndTheGroupOnDisplay()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Review", "Waiting"]);

            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")), "The states of the group are in the list.");
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
            }
        }

        // The group has no states (none stored, or an empty list), so it is not a group: its name stays a state, and
        // reading the owner does not fail.
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReadingAnOwnerWhoseGroupHasNoStates_KeepsTheEntryAsAStateAndDoesNotFail(bool statesAreNull)
        {
            SeedOwner(mappings: [new StateMappingDto { Name = "Review", States = statesAreNull ? null! : [] }], doing: ["Analysis", "Review", "Waiting"]);

            var settings = await ReadSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
                Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("Analysis", "Review", "Waiting")));
            }
        }

        [Test]
        public async Task ReadingAnOwnerWhoseStoredListHoldsAGroupNameBesideOneOfItsStates_ShowsTheGroupOnceAsTheOldExpansionDid()
        {
            SeedOwner(mappings: [new StateMappingDto { Name = "In Arbeit", States = ["In Progress"] }], doing: ["In Arbeit", "In Progress"]);

            var settings = await ReadSettings();

            Assert.That(Strings(settings, "doingDisplayStates"), Is.EqualTo(Listed("In Arbeit")));
        }

        [Test]
        [Ignore(Pending)]
        public async Task SuggestingStatesWhileAnOwnersStoredListStillHoldsAGroupName_OffersTheStatesAndNotTheName()
        {
            SeedOwner(mappings: [Review()], doing: ["Analysis", "Review"]);

            client.AsSystemAdmin();
            var response = await client.GetAsync($"/api/latest/suggestions/states/{(kind == OwnerKind.Team ? "teams" : "projects")}");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(Strings(JsonDocument.Parse(body).RootElement, "doingStates"), Is.EqualTo(Listed("Analysis", "Code Review", "Testing")));
            }
        }

        private async Task SaveExpectingOk(SettingsOwnerDtoBase dto)
        {
            var response = await Save(dto);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        // A refused save stores nothing: the lists and the mappings read back as they were seeded.
        private async Task AssertNothingWasStored()
        {
            var settings = await ReadSettings();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Strings(settings, "toDoStates"), Is.EqualTo(Listed("Backlog")));
                Assert.That(Strings(settings, "doingStates"), Is.EqualTo(Listed("Active")));
                Assert.That(Strings(settings, "doneStates"), Is.EqualTo(Listed("Done")));
                Assert.That(settings.GetProperty("stateMappings").GetArrayLength(), Is.Zero);
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

        private static StateMappingDto InArbeit() => new() { Name = "In Arbeit", States = ["In Progress"] };

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

        // The list as it sits in the database, before the settings read turns any group name back into its states.
        private string[] StoredDoingStates()
        {
            using var scope = factory.Services.CreateScope();
            WorkTrackingSystemOptionsOwner? owner = kind == OwnerKind.Team
                ? scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(ownerId)
                : scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(ownerId);
            Assert.That(owner, Is.Not.Null, $"No owner {ownerId} is stored.");
            return [.. owner!.DoingStates];
        }

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

        private void SeedOwner(StateMappingDto[]? mappings = null, string[]? toDo = null, string[]? doing = null, CycleTimeDefinition? cycleTime = null)
        {
            using var scope = factory.Services.CreateScope();
            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var stored = (mappings ?? []).Select(m => new StateMapping { Name = m.Name, States = m.States is null ? null! : [.. m.States] }).ToList();

            if (kind == OwnerKind.Team)
            {
                var team = new Team { Name = $"Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection, StateMappings = stored };
                ApplyLists(team, toDo, doing, cycleTime);
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                repository.Add(team);
                repository.Save().GetAwaiter().GetResult();
                ownerId = team.Id;
            }
            else
            {
                var portfolio = new Portfolio { Name = $"Portfolio {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection, StateMappings = stored };
                ApplyLists(portfolio, toDo, doing, cycleTime);
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                repository.Add(portfolio);
                repository.Save().GetAwaiter().GetResult();
                ownerId = portfolio.Id;
            }

            connectionId = connection.Id;
        }

        private static void ApplyLists(WorkTrackingSystemOptionsOwner owner, string[]? toDo, string[]? doing, CycleTimeDefinition? cycleTime)
        {
            owner.ToDoStates = [.. toDo ?? ["Backlog"]];
            owner.DoingStates = [.. doing ?? ["Active"]];
            owner.DoneStates = ["Done"];
            owner.CycleTimeDefinitions = cycleTime is null ? [] : [cycleTime];
        }
    }

    public enum OwnerKind
    {
        Team,
        Portfolio,
    }
}
