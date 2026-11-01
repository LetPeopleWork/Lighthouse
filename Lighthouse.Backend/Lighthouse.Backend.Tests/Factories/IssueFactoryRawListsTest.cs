using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    [TestFixture]
    public class IssueFactoryRawListsTest
    {
        private static JsonDocument AnIssueNowIn(string state, string movedFrom, string movedAt)
            => JsonDocument.Parse($$"""
                {
                  "key": "TEST-1",
                  "changelog": {
                    "total": 1,
                    "histories": [
                      { "created": "{{movedAt}}", "items": [ { "field": "status", "fromString": "{{movedFrom}}", "toString": "{{state}}" } ] }
                    ]
                  },
                  "fields": {
                    "summary": "A story",
                    "created": "2024-09-01T00:00:00.000+0000",
                    "updated": "2024-09-28T00:00:00.000+0000",
                    "labels": [],
                    "issuetype": { "name": "Story" },
                    "status": { "name": "{{state}}" }
                  }
                }
                """);

        [Test]
        [Ignore("pending: category lists hold raw states")]
        public void CreateIssueFromJson_DoingStateNamedLikeAGroup_DatesTheStartByTheArrivalIntoThatState()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["In Arbeit"], DoneStates = ["Closed"] };
            team.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });
            using var json = AnIssueNowIn("In Arbeit", "Backlog", "2024-09-27T00:00:00.000+0000");

            var issue = new IssueFactory(Mock.Of<ILogger<IssueFactory>>()).CreateIssueFromJson(json.RootElement, team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27)));
                Assert.That(issue.ClosedDate, Is.Null);
            }
        }

        [Test]
        [Ignore("pending: category lists hold raw states")]
        public void CreateIssueFromJson_DoneStateNamedLikeAGroup_DatesTheCloseByTheArrivalIntoThatState()
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = ["Active"], DoneStates = ["Erledigt"] };
            team.StateMappings.Add(new StateMapping { Name = "Erledigt", States = ["Resolved"] });
            using var json = AnIssueNowIn("Erledigt", "Active", "2024-09-28T00:00:00.000+0000");

            var issue = new IssueFactory(Mock.Of<ILogger<IssueFactory>>()).CreateIssueFromJson(json.RootElement, team);

            Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 28)));
        }
    }
}
