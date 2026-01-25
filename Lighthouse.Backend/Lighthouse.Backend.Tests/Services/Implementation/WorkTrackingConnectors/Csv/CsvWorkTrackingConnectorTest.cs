using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnectorTest() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        [TestCase("empty-file.txt", false)]
        [TestCase("invalid-missing-required.csv", false)]
        [TestCase("invalid-not-csv.csv", false)]
        [TestCase("invalid-wrong-csv.csv", false)]
        [TestCase("team-valid-all-optional.csv", true)]
        [TestCase("team-valid-required-only.csv", true)]
        [TestCase("team-valid-with-optional.csv", true)]
        public async Task ValidateTeam_ChecksIfValidCsv(string csvFileName, bool expectedResult)
        {
            var subject = CreateSubject();
            var team = new Team
            {
                DataRetrievalValue = LoadCsvFile(csvFileName),
                WorkTrackingSystemConnection = CreateCsvWorkTrackingConnection(),
            };

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task ValidateConnection_ReturnsTrueIfAllOptionsAreFilledIn()
        {
            var subject = CreateSubject();

            var connection = GetWorkTrackingSystemFactory().CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);

            var isValid = await subject.ValidateConnection(connection);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase(CsvWorkTrackingOptionNames.Delimiter)]
        [TestCase(CsvWorkTrackingOptionNames.DateTimeFormat)]
        [TestCase(CsvWorkTrackingOptionNames.IdHeader)]
        [TestCase(CsvWorkTrackingOptionNames.NameHeader)]
        [TestCase(CsvWorkTrackingOptionNames.StateHeader)]
        [TestCase(CsvWorkTrackingOptionNames.TypeHeader)]
        [TestCase(CsvWorkTrackingOptionNames.StartedDateHeader)]
        [TestCase(CsvWorkTrackingOptionNames.ClosedDateHeader)]
        public async Task ValidateConnection_ReturnsFalseIfSingleRequiredOptionIsNotFilledIn(string optionKey)
        {
            var subject = CreateSubject();

            var connection = GetWorkTrackingSystemFactory().CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            var option = connection.Options.Single(o => o.Key == optionKey);
            option.Value = string.Empty;

            var isValid = await subject.ValidateConnection(connection);

            Assert.That(isValid, Is.False);
        }

        [TestCase(CsvWorkTrackingOptionNames.CreatedDateHeader)]
        [TestCase(CsvWorkTrackingOptionNames.ParentReferenceIdHeader)]
        [TestCase(CsvWorkTrackingOptionNames.TagsHeader)]
        [TestCase(CsvWorkTrackingOptionNames.UrlHeader)]
        [TestCase(CsvWorkTrackingOptionNames.EstimatedSizeHeader)]
        [TestCase(CsvWorkTrackingOptionNames.OwningTeamHeader)]
        public async Task ValidateConnection_ReturnsTrueEvenIfOptionalOptionIsNotFilledIn(string optionKey)
        {
            var subject = CreateSubject();

            var connection = GetWorkTrackingSystemFactory().CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            var option = connection.Options.Single(o => o.Key == optionKey);
            option.Value = string.Empty;

            var isValid = await subject.ValidateConnection(connection);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public async Task GetWorkItemsForTeam_NoOptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("team-valid-required-only.csv");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyRequiredWorkItemFields(workItems[0], "ITEM-001", "User Story Title", "In Progress", StateCategories.Doing, "User Story", new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 01, 25, 0, 0, 0, DateTimeKind.Utc));
                VerifyRequiredWorkItemFields(workItems[1], "ITEM-002", "Bug Title", "Done", StateCategories.Done, "Bug", new DateTime(2025, 01, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 01, 18, 0, 0, 0, DateTimeKind.Utc));
                VerifyRequiredWorkItemFields(workItems[2], "ITEM-003", "Database cleanup task", "To Do", StateCategories.ToDo, "Task", new DateTime(2025, 01, 25, 0, 0, 0, DateTimeKind.Utc), null);
                VerifyRequiredWorkItemFields(workItems[3], "ITEM-004", "Refactor auth module", "In Progress", StateCategories.Doing, "Task", new DateTime(2025, 01, 30, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 02, 05, 0, 0, 0, DateTimeKind.Utc));
                VerifyRequiredWorkItemFields(workItems[4], "ITEM-005", "Improve logging", "Done", StateCategories.Done, "Task", new DateTime(2025, 01, 22, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 01, 28, 0, 0, 0, DateTimeKind.Utc));

                var workItem = workItems[0];
                Assert.That(workItem.TeamId, Is.EqualTo(team.Id));
                Assert.That(workItem.Team, Is.EqualTo(team));
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_OptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("team-valid-all-optional.csv");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalWorkItemFields(workItems[0], new DateTime(2025, 01, 15, 0, 0, 0, DateTimeKind.Utc), "EPIC-001", ["frontend", "feature"], "https://system.com/item/1");
                VerifyOptionalWorkItemFields(workItems[1], new DateTime(2025, 01, 10, 0, 0, 0, DateTimeKind.Utc), string.Empty, ["critical"], string.Empty);
                VerifyOptionalWorkItemFields(workItems[2], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), "EPIC-001", ["maintenance", "blocked"], string.Empty);
                VerifyOptionalWorkItemFields(workItems[3], new DateTime(2025, 01, 28, 0, 0, 0, DateTimeKind.Utc), "EPIC-003", ["backend", "security"], "https://system.com/item/4");
                VerifyOptionalWorkItemFields(workItems[4], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), string.Empty, [], string.Empty);
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_OptionalRows_OptionalColumnHeaderMissing_IgnoresMissing()
        {
            var subject = CreateSubject();
            var team = CreateTeam("team-valid-all-optional.csv");

            AdjustWorkTrackingSystemOption(team.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.CreatedDateHeader, string.Empty);
            AdjustWorkTrackingSystemOption(team.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.TagsHeader, string.Empty);
            AdjustWorkTrackingSystemOption(team.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.ParentReferenceIdHeader, string.Empty);

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalWorkItemFields(workItems[0], null, string.Empty, [], "https://system.com/item/1");
                VerifyOptionalWorkItemFields(workItems[1], null, string.Empty, [], string.Empty);
                VerifyOptionalWorkItemFields(workItems[2], null, string.Empty, [], string.Empty);
                VerifyOptionalWorkItemFields(workItems[3], null, string.Empty, [], "https://system.com/item/4");
                VerifyOptionalWorkItemFields(workItems[4], null, string.Empty, [], string.Empty);
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_SomeOptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("team-valid-with-optional.csv");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalWorkItemFields(workItems[0], new DateTime(2025, 01, 15, 0, 0, 0, DateTimeKind.Utc), string.Empty, ["frontend", "feature"], "https://system.com/item/1");
                VerifyOptionalWorkItemFields(workItems[1], new DateTime(2025, 01, 10, 0, 0, 0, DateTimeKind.Utc), string.Empty, ["critical", "bug"], "https://system.com/item/2");
                VerifyOptionalWorkItemFields(workItems[2], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), string.Empty, ["maintenance", "blocked"], string.Empty);
                VerifyOptionalWorkItemFields(workItems[3], new DateTime(2025, 01, 28, 0, 0, 0, DateTimeKind.Utc), string.Empty, ["backend", "security"], "https://system.com/item/4");
                VerifyOptionalWorkItemFields(workItems[4], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), string.Empty, [], "https://system.com/item/5");
            }
        }

        [Test]
        [TestCase("empty-file.txt", false)]
        [TestCase("invalid-missing-required.csv", false)]
        [TestCase("invalid-not-csv.csv", false)]
        [TestCase("invalid-wrong-csv.csv", false)]
        [TestCase("project-valid-required-only.csv", true)]
        [TestCase("project-valid-all-optional.csv", true)]
        [TestCase("project-valid-with-optional.csv", true)]
        public async Task ValidateProject_ChecksIfValidCsv(string csvFileName, bool expectedResult)
        {
            var subject = CreateSubject();
            var project = new Portfolio
            {
                DataRetrievalValue = LoadCsvFile(csvFileName),
                WorkTrackingSystemConnection = CreateCsvWorkTrackingConnection(),
            };

            var isValid = await subject.ValidatePortfolioSettings(project);

            Assert.That(isValid, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task GetFeaturesForProject_NoOptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var project = CreateProject("project-valid-required-only.csv");

            var features = (await subject.GetFeaturesForProject(project)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyRequiredWorkItemFields(features[0], "EPIC-001", "Epic Title", "In Progress", StateCategories.Doing, "Epic", new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 01, 25, 0, 0, 0, DateTimeKind.Utc));
                VerifyRequiredWorkItemFields(features[1], "EPIC-002", "Feature Title", "Done", StateCategories.Done, "Feature", new DateTime(2025, 01, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 01, 18, 0, 0, 0, DateTimeKind.Utc));
                VerifyRequiredWorkItemFields(features[2], "EPIC-003", "Database cleanup Container", "To Do", StateCategories.ToDo, "Epic", new DateTime(2025, 01, 25, 0, 0, 0, DateTimeKind.Utc), null);
                VerifyRequiredWorkItemFields(features[3], "EPIC-004", "Refactor auth module", "In Progress", StateCategories.Doing, "Epic", new DateTime(2025, 01, 30, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 02, 05, 0, 0, 0, DateTimeKind.Utc));
                VerifyRequiredWorkItemFields(features[4], "EPIC-005", "Improve logging", "Done", StateCategories.Done, "Epic", new DateTime(2025, 01, 22, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 01, 28, 0, 0, 0, DateTimeKind.Utc));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_OptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var project = CreateProject("project-valid-all-optional.csv");

            var features = (await subject.GetFeaturesForProject(project)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalFeatureFields(features[0], new DateTime(2025, 01, 15, 0, 0, 0, DateTimeKind.Utc), ["frontend", "feature"], "https://system.com/item/1", 8, "Frontend Team");
                VerifyOptionalFeatureFields(features[1], new DateTime(2025, 01, 10, 0, 0, 0, DateTimeKind.Utc), ["critical"], string.Empty, 3, string.Empty);
                VerifyOptionalFeatureFields(features[2], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), ["maintenance", "blocked"], string.Empty, 1, "Frontend Team");
                VerifyOptionalFeatureFields(features[3], new DateTime(2025, 01, 28, 0, 0, 0, DateTimeKind.Utc), ["backend", "security"], "https://system.com/item/4", 5, "Backend Team");
                VerifyOptionalFeatureFields(features[4], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), [], string.Empty, 2, string.Empty);
            }
        }

        [Test]
        public async Task GetFeaturesForProject_SomeOptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var project = CreateProject("project-valid-with-optional.csv");

            var features = (await subject.GetFeaturesForProject(project)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalFeatureFields(features[0], new DateTime(2025, 01, 15, 0, 0, 0, DateTimeKind.Utc), ["frontend", "feature"], "https://system.com/item/1", 8, "Frontend Team");
                VerifyOptionalFeatureFields(features[1], new DateTime(2025, 01, 10, 0, 0, 0, DateTimeKind.Utc), ["critical", "bug"], "https://system.com/item/2", 3, "Platform Team");
                VerifyOptionalFeatureFields(features[2], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), ["maintenance", "blocked"], string.Empty, 1, "Operations");
                VerifyOptionalFeatureFields(features[3], new DateTime(2025, 01, 28, 0, 0, 0, DateTimeKind.Utc), ["backend", "security"], "https://system.com/item/4", 5, "Backend Team");
                VerifyOptionalFeatureFields(features[4], new DateTime(2025, 01, 20, 0, 0, 0, DateTimeKind.Utc), [], "https://system.com/item/5", 2, "Backend Team");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_WrongType_Ignores()
        {
            var subject = CreateSubject();
            var team = CreateTeam("team-valid-required-only.csv");

            team.WorkItemTypes.Remove("Task");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            Assert.That(workItems, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetWorkItemsForTeam_WrongState_Ignores()
        {
            var subject = CreateSubject();
            var team = CreateTeam("team-valid-required-only.csv");

            team.ToDoStates = ["New"];

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            Assert.That(workItems, Has.Count.EqualTo(4));
        }

        [Test]
        public async Task GetFeaturesForProject_WrongType_Ignores()
        {
            var subject = CreateSubject();
            var project = CreateProject("project-valid-required-only.csv");

            project.WorkItemTypes.Remove("Feature");

            var features = (await subject.GetFeaturesForProject(project)).ToList();

            Assert.That(features, Has.Count.EqualTo(4));
        }

        [Test]
        public async Task TaskGetFeaturesForProject_WrongState_Ignores()
        {
            var subject = CreateSubject();
            var project = CreateProject("project-valid-required-only.csv");

            project.ToDoStates = ["New"];

            var features = (await subject.GetFeaturesForProject(project)).ToList();

            Assert.That(features, Has.Count.EqualTo(4));
        }

        [Test]
        public async Task DefaultAzureDevOpsConnection_CanLoadAdoFile()
        {
            await SeedDatabase();
            var subject = CreateSubject();
            var adoCsvConnection = GetWorkTrackingSystemRepository().GetByPredicate(x => x is { WorkTrackingSystem: WorkTrackingSystems.Csv, Name: "CSV Azure DevOps" }) ?? throw new InvalidOperationException("Azure DevOps CSV Connection not found");

            var toDoStates = new List<string>{ "New" };
            var doingStates = new List<string>{ "Active", "Resolved" };
            var doneStates = new List<string>{ "Closed" };

            var team = CreateTeam("adodata.csv");
            team.WorkTrackingSystemConnection = adoCsvConnection;
            team.ToDoStates = toDoStates;
            team.DoingStates = doingStates;
            team.DoneStates = doneStates;

            var workItems = await subject.GetWorkItemsForTeam(team);
            Assert.That(workItems.ToList(), Has.Count.EqualTo(95));

            var project = CreateProject("adodata.csv");
            project.ToDoStates = toDoStates;
            project.DoingStates = doingStates;
            project.DoneStates = doneStates;

            project.WorkTrackingSystemConnection = adoCsvConnection;
            var features = await subject.GetFeaturesForProject(project);
            Assert.That(features.ToList(), Has.Count.EqualTo(14));
        }

        private static void VerifyRequiredWorkItemFields(WorkItemBase workItem, string referenceId, string name, string state, StateCategories stateCategory, string type, DateTime startedDate, DateTime? closedDate)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.ReferenceId, Is.EqualTo(referenceId));
                Assert.That(workItem.Name, Is.EqualTo(name));
                Assert.That(workItem.Type, Is.EqualTo(type));
                Assert.That(workItem.State, Is.EqualTo(state));
                
                Assert.That(workItem.StartedDate, Is.EqualTo(startedDate));
                Assert.That(workItem.StartedDate?.Kind ?? DateTimeKind.Utc, Is.EqualTo(DateTimeKind.Utc));
                
                Assert.That(workItem.ClosedDate, Is.EqualTo(closedDate));
                Assert.That(workItem.ClosedDate?.Kind ?? DateTimeKind.Utc, Is.EqualTo(DateTimeKind.Utc));

                Assert.That(workItem.StateCategory, Is.EqualTo(stateCategory));
            }
        }

        private static void VerifyOptionalWorkItemFields(WorkItemBase workItem, DateTime? createdDate, string? parentReferenceId, string[] tags, string? url)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.CreatedDate, Is.EqualTo(createdDate));
                Assert.That(workItem.CreatedDate?.Kind ?? DateTimeKind.Utc, Is.EqualTo(DateTimeKind.Utc));

                Assert.That(workItem.ParentReferenceId, Is.EqualTo(parentReferenceId));
                Assert.That(workItem.Url, Is.EqualTo(url));

                foreach (var tag in tags)
                {
                    Assert.That(workItem.Tags, Does.Contain(tag));
                }
            }
        }

        private static void VerifyOptionalFeatureFields(Feature feature, DateTime? createdDate, string[] tags, string? url, int estimatedSize, string owningTeam)
        {
            VerifyOptionalWorkItemFields(feature, createdDate, string.Empty, tags, url);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.EstimatedSize, Is.EqualTo(estimatedSize));
                Assert.That(feature.OwningTeam, Is.EqualTo(owningTeam));
            }
        }

        private static string LoadCsvFile(string csvFileName)
        {
            var csvFileContent = File.ReadAllText($"Services/Implementation/WorkTrackingConnectors/Csv/{csvFileName}");
            return csvFileContent;
        }

        private Team CreateTeam(string csvFile)
        {
            var team = new Team
            {
                Id = 1886,
                DataRetrievalValue = LoadCsvFile(csvFile),
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress"],
                DoneStates = ["done"],
                WorkItemTypes = ["User Story", "bug", "Task"],
                WorkTrackingSystemConnection = CreateCsvWorkTrackingConnection(),
            };

            return team;
        }

        private Portfolio CreateProject(string csvFile)
        {
            var project = new Portfolio
            {
                Id = 1886,
                DataRetrievalValue = LoadCsvFile(csvFile),
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                WorkItemTypes = ["Feature", "Epic"],
                WorkTrackingSystemConnection = CreateCsvWorkTrackingConnection(),
            };

            return project;
        }

        private static void AdjustWorkTrackingSystemOption(WorkTrackingSystemConnection connection, string key, string value)
        {
            connection.Options.Single(o => o.Key == key).Value = value;
        }

        private WorkTrackingSystemConnection CreateCsvWorkTrackingConnection()
        {
            var connection = GetWorkTrackingSystemFactory().CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);

            AdjustWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.DateTimeFormat, "yyyy-MM-dd");
            AdjustWorkTrackingSystemOption(connection, CsvWorkTrackingOptionNames.TagSeparator, "|");

            return connection;
        }

        private CsvWorkTrackingConnector CreateSubject()
        {
            return ServiceProvider.GetService<CsvWorkTrackingConnector>() ?? throw new InvalidOperationException("Could not resolve Work Tracking Connector");
        }

        private IWorkTrackingSystemFactory GetWorkTrackingSystemFactory()
        {
            return ServiceProvider.GetService<IWorkTrackingSystemFactory>() ?? throw new InvalidOperationException("Could not resolve Work Tracking System Factory");
        }

        private WorkTrackingSystemConnectionRepository GetWorkTrackingSystemRepository()
        {
            var repo = (WorkTrackingSystemConnectionRepository)ServiceProvider.GetService<IRepository<WorkTrackingSystemConnection>>() ?? throw new InvalidOperationException("Could not resolve Work Tracking System Connection Repo");
            return repo;
        }
    }
}
