using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvDemoJourneyWithStateMappingTest() : IntegrationTestBase
    {
        private const string Pending = "pending: category lists hold raw states";

        [Test]
        [Ignore(Pending)]
        public async Task DemoJourney_TeamGroupsTwoOfItsDoingStates_NeverMovesAnItemFromTheGroupToItself()
        {
            var team = ADemoTeamWithAReviewGroup();

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team, CancellationToken.None)).ToList();

            var transitions = workItems.Single(w => w.ReferenceId == "DONE-1").SyncedTransitions;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(2), "In Progress to Review, then Review to Done.");
                Assert.That(transitions.Where(t => t.FromState == t.ToState), Is.Empty);
                Assert.That(transitions[0].FromState, Is.EqualTo("In Progress"));
                Assert.That(transitions[0].ToState, Is.EqualTo("Review"));
                Assert.That(transitions[1].FromState, Is.EqualTo("Review"));
                Assert.That(transitions[1].ToState, Is.EqualTo("Done"));
                Assert.That(transitions[1].TransitionedAt, Is.EqualTo(new DateTime(2025, 01, 22, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task DemoJourney_GroupSitsFirstInDoing_StartsTheJourneyInTheGroup()
        {
            var team = ADemoTeamWithAReviewGroup();
            team.DoingStates = ["Code Review", "Testing", "In Progress"];

            var workItems = (await CreateSubject().GetWorkItemsForTeam(team, CancellationToken.None)).ToList();

            var transitions = workItems.Single(w => w.ReferenceId == "DONE-1").SyncedTransitions;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(2));
                Assert.That(transitions[0].FromState, Is.EqualTo("Review"));
                Assert.That(transitions[0].ToState, Is.EqualTo("In Progress"));
            }
        }

        private Team ADemoTeamWithAReviewGroup()
        {
            var connection = ServiceProvider.GetRequiredService<IWorkTrackingSystemFactory>().CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            SetOption(connection, CsvWorkTrackingOptionNames.DateTimeFormat, "yyyy-MM-dd");
            SetOption(connection, CsvWorkTrackingOptionNames.TagSeparator, "|");
            SetOption(connection, CsvWorkTrackingOptionNames.StateEnteredDateHeader, "StateSince");
            SetOption(connection, CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo, bool.TrueString);

            var team = new Team
            {
                Id = 1886,
                DataRetrievalValue = File.ReadAllText("Services/Implementation/WorkTrackingConnectors/Csv/team-valid-demo-journey-synthesis.csv"),
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress", "Code Review", "Testing"],
                DoneStates = ["Done"],
                WorkItemTypes = ["User Story", "bug", "Task"],
                WorkTrackingSystemConnection = connection,
            };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        private static void SetOption(WorkTrackingSystemConnection connection, string key, string value)
            => connection.Options.Single(option => option.Key == key).Value = value;

        private CsvWorkTrackingConnector CreateSubject()
            => ServiceProvider.GetRequiredService<CsvWorkTrackingConnector>();
    }
}
