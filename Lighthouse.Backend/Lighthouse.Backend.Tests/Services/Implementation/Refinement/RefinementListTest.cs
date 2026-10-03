using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class RefinementListTest
    {
        private const string Backlog = "Backlog";

        private static readonly string[] OnlyA = ["A"];

        [TestCase("7,7,7", "GR-3,GR-1,GR-2", "GR-1,GR-2,GR-3", TestName = "Equal ranks are listed by id")]
        [TestCase("7,7", "GR-10,GR-9", "GR-10,GR-9", TestName = "Equal ranks compare ids as text, not as numbers")]
        [TestCase("10,9,100", "A,B,C", "B,A,C", TestName = "Numeric ranks are compared as numbers")]
        [TestCase("0|hzzzzz:,3", "A,B", "B,A", TestName = "A numeric rank comes before a text rank")]
        [TestCase("0|i0000:,0|hzzzzz:", "A,B", "B,A", TestName = "Text ranks are compared as text")]
        [TestCase(",5", "A,B", "B,A", TestName = "A missing rank comes after a numeric rank")]
        [TestCase(",,", "C,A,B", "A,B,C", TestName = "Missing ranks are listed by id")]
        public void ListsWorkItemsInBacklogOrder(string ranks, string ids, string expectedOrder)
        {
            var team = ATeamRefiningInBacklog();
            var items = ranks.Split(',')
                .Zip(ids.Split(','), (rank, id) => new WorkItem { ReferenceId = id, Order = rank, State = Backlog, TeamId = team.Id })
                .ToList();

            var listed = new RefinementList(ARepositoryHolding(items)).For(team);

            Assert.That(listed.Select(item => item.ReferenceId), Is.EqualTo(expectedOrder.Split(',')));
        }

        [TestCase("backlog", "Backlog", TestName = "A state typed in other case than the tracker's is still listed")]
        [TestCase("Backlog", "BACKLOG", TestName = "A tracker state in other case than the Team's is still listed")]
        public void MatchesTheTrackersStateWhateverItsCase(string teamState, string trackerState)
        {
            var team = ATeamRefiningIn(teamState, []);
            var items = new List<WorkItem> { new() { ReferenceId = "A", Order = "1", State = trackerState, TeamId = team.Id } };

            var listed = new RefinementList(ARepositoryHolding(items)).For(team);

            Assert.That(listed.Select(item => item.ReferenceId), Is.EqualTo(OnlyA));
        }

        [Test]
        public void MatchesAStateGatheredByAMappingWhateverItsCase()
        {
            var team = ATeamRefiningIn("Refining", [new StateMapping { Name = "refining", States = ["analysing"] }]);
            var items = new List<WorkItem> { new() { ReferenceId = "A", Order = "1", State = "Analysing", TeamId = team.Id } };

            var listed = new RefinementList(ARepositoryHolding(items)).For(team);

            Assert.That(listed.Select(item => item.ReferenceId), Is.EqualTo(OnlyA));
        }

        // Every Team page asks for the refinement list, and most Teams have not set refinement up, so those
        // must not cost a read of every Work Item the Team holds.
        [Test]
        public void ATeamWithoutRefinementStatesListsNothingWithoutReadingItsWorkItems()
        {
            var team = new Team { Id = 12, ToDoStates = [Backlog], DoingStates = ["Doing"] };
            var repository = new Mock<IWorkItemRepository>();

            var listed = new RefinementList(repository.Object).For(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed, Is.Empty);
                repository.Verify(repo => repo.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()), Times.Never);
            }
        }

        private static Team ATeamRefiningIn(string state, List<StateMapping> mappings)
        {
            return new Team
            {
                Id = 12,
                ToDoStates = [state],
                DoingStates = ["Doing"],
                StateMappings = mappings,
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = state }] },
            };
        }

        private static Team ATeamRefiningInBacklog()
        {
            return new Team
            {
                Id = 12,
                ToDoStates = [Backlog],
                DoingStates = ["Doing"],
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = Backlog }] },
            };
        }

        private static IWorkItemRepository ARepositoryHolding(List<WorkItem> items)
        {
            var repository = new Mock<IWorkItemRepository>();
            repository
                .Setup(repo => repo.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => items.AsQueryable().Where(predicate));
            return repository.Object;
        }
    }
}
