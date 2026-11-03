using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// A stored list that still holds a group's name is read through the owner's classifier, so a record in one of
    /// the group's states is still dated. These stay green through the change: they fail if reads ever take the
    /// stored list as it stands.
    /// </summary>
    [TestFixture]
    public class ServiceNowStateSpanMapperRawListsTest
    {
        private const string Record = "7f10b53a83da4310ad56c670ceaad387";

        private static DateTime At(string time) => DateTime.Parse($"2026-07-29 {time}", null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

        private static ServiceNowStateSpan ASpan(string label, string time) => new(Record, label, At(time));

        private static Team ADeskWhoseListsHoldTheGroupName(string[] toDo, string[] doing, string[] done)
        {
            var team = new Team { ToDoStates = [.. toDo], DoingStates = [.. doing], DoneStates = [.. done] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        [Test]
        public void WhenWorkStarted_StoredDoingListHoldsTheGroupName_IsTheArrivalIntoOneOfItsStates()
        {
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("New", "06:00:00"), ASpan("Code Review", "09:00:00"), ASpan("Closed", "17:00:00")];

            var started = ServiceNowStateSpanMapper.WhenWorkStarted(spans, ADeskWhoseListsHoldTheGroupName(["New"], ["Review"], ["Closed"]));

            Assert.That(started, Is.EqualTo(At("09:00:00")));
        }

        [Test]
        public void WhenWorkFinished_StoredDoneListHoldsTheGroupName_IsTheArrivalIntoOneOfItsStates()
        {
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("New", "06:00:00"), ASpan("Active", "09:00:00"), ASpan("Testing", "17:00:00")];

            var finished = ServiceNowStateSpanMapper.WhenWorkFinished(spans, ADeskWhoseListsHoldTheGroupName(["New"], ["Active"], ["Review"]));

            Assert.That(finished, Is.EqualTo(At("17:00:00")));
        }

        [Test]
        public void WhenWorkWasQueued_StoredToDoListHoldsTheGroupName_IsTheLastReturnIntoOneOfItsStates()
        {
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("Code Review", "06:00:00"), ASpan("Active", "09:00:00"), ASpan("Testing", "11:00:00")];

            var queued = ServiceNowStateSpanMapper.WhenWorkWasQueued(spans, ADeskWhoseListsHoldTheGroupName(["Review"], ["Active"], ["Closed"]));

            Assert.That(queued, Is.EqualTo(At("11:00:00")));
        }
    }
}
