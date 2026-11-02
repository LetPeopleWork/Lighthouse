using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    /// <summary>
    /// A stored list that still holds a group's name (not yet rewritten, skipped by the start-up pass, or saved by an
    /// older instance) is read through the owner's classifier, so an item in one of the group's states is still
    /// dated. These stay green through the change: they fail if reads ever take the stored list as it stands.
    /// </summary>
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

        private static Team ATeamWhoseListsHoldTheGroupName(string[] doing, string[] done)
        {
            var team = new Team { ToDoStates = ["Backlog"], DoingStates = [.. doing], DoneStates = [.. done] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        [Test]
        public void CreateIssueFromJson_StoredDoingListHoldsTheGroupName_DatesTheStartByTheArrivalIntoOneOfItsStates()
        {
            var team = ATeamWhoseListsHoldTheGroupName(["Analysis", "Review"], ["Closed"]);
            using var json = AnIssueNowIn("Code Review", "Backlog", "2024-09-27T00:00:00.000+0000");

            var issue = new IssueFactory(Mock.Of<ILogger<IssueFactory>>()).CreateIssueFromJson(json.RootElement, team, JiraOriginalNames.AsReported);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27)));
                Assert.That(issue.ClosedDate, Is.Null);
            }
        }

        [Test]
        public void CreateIssueFromJson_StoredDoneListHoldsTheGroupName_DatesTheCloseByTheArrivalIntoOneOfItsStates()
        {
            var team = ATeamWhoseListsHoldTheGroupName(["Analysis"], ["Review"]);
            using var json = AnIssueNowIn("Testing", "Analysis", "2024-09-28T00:00:00.000+0000");

            var issue = new IssueFactory(Mock.Of<ILogger<IssueFactory>>()).CreateIssueFromJson(json.RootElement, team, JiraOriginalNames.AsReported);

            Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 28)));
        }
    }
}
