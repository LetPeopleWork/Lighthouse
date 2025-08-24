using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnectorTest
    {
        [Test]
        [TestCase("empty-file.txt", false)]
        [TestCase("invalid-missing-required.csv", false)]
        [TestCase("invalid-not-csv.csv", false)]
        [TestCase("invalid-wrong-csv.csv", false)]
        [TestCase("valid-all-optional.csv", true)]
        [TestCase("valid-required-only.csv", true)]
        [TestCase("valid-with-optional.csv", true)]
        public async Task Validate_ChecksIfValidCsv(string csvFileName, bool expectedResult)
        {
            var subject = CreateSubject();
            var team = new Team
            {
                WorkItemQuery = LoadCsvFile(csvFileName),
            };

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task GetWorkItemsForTeam_NoOptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("valid-required-only.csv");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyRequiredWorkItemFields(workItems[0], "ITEM-001", "User Story Title", "In Progress", StateCategories.Doing, "User Story", new DateTime(2025, 01, 20, 9, 15, 0), new DateTime(2025, 01, 25));
                VerifyRequiredWorkItemFields(workItems[1], "ITEM-002", "Bug Title", "Done", StateCategories.Done, "Bug", new DateTime(2025, 01, 12), new DateTime(2025, 01, 18, 14, 45, 0));
                VerifyRequiredWorkItemFields(workItems[2], "ITEM-003", "Database cleanup task", "To Do", StateCategories.ToDo, "Task", new DateTime(2025, 01, 25), null);
                VerifyRequiredWorkItemFields(workItems[3], "ITEM-004", "Refactor auth module", "In Progress", StateCategories.Doing, "Task", new DateTime(2025, 01, 30), new DateTime(2025, 02, 05));
                VerifyRequiredWorkItemFields(workItems[4], "ITEM-005", "Improve logging", "Done", StateCategories.Done, "Task", new DateTime(2025, 01, 22), new DateTime(2025, 01, 28));

                var workItem = workItems[0];
                Assert.That(workItem.TeamId, Is.EqualTo(team.Id));
                Assert.That(workItem.Team, Is.EqualTo(team));
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_OptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("valid-all-optional.csv");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalWorkItemFields(workItems[0], new DateTime(2025, 01, 15), "EPIC-001", ["frontend", "feature"], "https://system.com/item/1");
                VerifyOptionalWorkItemFields(workItems[1], new DateTime(2025, 01, 10), string.Empty, ["critical"], string.Empty);
                VerifyOptionalWorkItemFields(workItems[2], new DateTime(2025, 01, 20), "EPIC-001", ["maintenance", "blocked"], string.Empty);
                VerifyOptionalWorkItemFields(workItems[3], new DateTime(2025, 01, 28), "EPIC-003", ["backend", "security"], "https://system.com/item/4");
                VerifyOptionalWorkItemFields(workItems[4], new DateTime(2025, 01, 20), string.Empty, [], string.Empty);
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_SomeOptionalRows_LoadsCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("valid-with-optional.csv");

            var workItems = (await subject.GetWorkItemsForTeam(team)).ToList();

            using (Assert.EnterMultipleScope())
            {
                VerifyOptionalWorkItemFields(workItems[0], new DateTime(2025, 01, 15), string.Empty, ["frontend", "feature"], "https://system.com/item/1");
                VerifyOptionalWorkItemFields(workItems[1], new DateTime(2025, 01, 10), string.Empty, ["critical", "bug"], "https://system.com/item/2");
                VerifyOptionalWorkItemFields(workItems[2], new DateTime(2025, 01, 20), string.Empty, ["maintenance", "blocked"], string.Empty);
                VerifyOptionalWorkItemFields(workItems[3], new DateTime(2025, 01, 28), string.Empty, ["backend", "security"], "https://system.com/item/4");
                VerifyOptionalWorkItemFields(workItems[4], new DateTime(2025, 01, 20), string.Empty, [], "https://system.com/item/5");
            }
        }

        private void VerifyRequiredWorkItemFields(WorkItem workItem, string referenceId, string name, string state, StateCategories stateCategory, string type, DateTime startedDate, DateTime? closedDate)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.ReferenceId, Is.EqualTo(referenceId));
                Assert.That(workItem.Name, Is.EqualTo(name));
                Assert.That(workItem.Type, Is.EqualTo(type));
                Assert.That(workItem.State, Is.EqualTo(state));
                Assert.That(workItem.StartedDate, Is.EqualTo(startedDate));
                Assert.That(workItem.ClosedDate, Is.EqualTo(closedDate));
                Assert.That(workItem.StateCategory, Is.EqualTo(stateCategory));
            }
        }

        private void VerifyOptionalWorkItemFields(WorkItem workItem, DateTime? createdDate, string? parentReferenceId, string[] tags, string? url)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.CreatedDate, Is.EqualTo(createdDate));
                Assert.That(workItem.ParentReferenceId, Is.EqualTo(parentReferenceId));
                Assert.That(workItem.Url, Is.EqualTo(url));

                foreach (var tag in tags)
                {
                    Assert.That(workItem.Tags, Does.Contain(tag));
                }
            }
        }

        private string LoadCsvFile(string csvFileName)
        {
            var csvFileContent = File.ReadAllText($"Services/Implementation/WorkTrackingConnectors/Csv/{csvFileName}");
            return csvFileContent;
        }

        private Team CreateTeam(string csvFile)
        {
            var team = new Team
            {
                Id = 1886,
                WorkItemQuery = LoadCsvFile(csvFile),
                ToDoStates = new List<string> { "To Do" },
                DoingStates = new List<string> { "In Progress" },
                DoneStates = new List<string> { "Done" },
            };

            return team;
        }

        private CsvWorkTrackingConnector CreateSubject()
        {
            return new CsvWorkTrackingConnector(Mock.Of<ILogger<CsvWorkTrackingConnector>>());
        }
    }
}
