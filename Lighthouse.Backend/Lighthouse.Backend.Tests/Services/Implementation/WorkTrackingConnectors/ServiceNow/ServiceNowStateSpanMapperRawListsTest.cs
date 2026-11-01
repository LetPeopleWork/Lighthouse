using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    [TestFixture]
    public class ServiceNowStateSpanMapperRawListsTest
    {
        private const string Record = "7f10b53a83da4310ad56c670ceaad387";

        private static DateTime At(string time) => DateTime.Parse($"2026-07-29 {time}", null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

        private static ServiceNowStateSpan ASpan(string label, string time) => new(Record, label, At(time));

        // A desk whose tracker really has a state called "In Arbeit", next to a group of the same name over another state.
        private static Team ADeskWithAStateNamedLikeAGroup()
        {
            var team = new Team { ToDoStates = ["New"], DoingStates = ["In Arbeit"], DoneStates = ["Closed"] };
            team.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });
            return team;
        }

        [Test]
        [Ignore("pending: category lists hold raw states")]
        public void WhenWorkStarted_StateNamedLikeAGroup_IsTheArrivalIntoThatState()
        {
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("New", "06:00:00"), ASpan("In Arbeit", "09:00:00"), ASpan("Closed", "17:00:00")];

            var started = ServiceNowStateSpanMapper.WhenWorkStarted(spans, ADeskWithAStateNamedLikeAGroup());

            Assert.That(started, Is.EqualTo(At("09:00:00")));
        }

        [Test]
        [Ignore("pending: category lists hold raw states")]
        public void WhenWorkFinished_DoneStateNamedLikeAGroup_IsTheArrivalIntoThatState()
        {
            var desk = new Team { ToDoStates = ["New"], DoingStates = ["Active"], DoneStates = ["Erledigt"] };
            desk.StateMappings.Add(new StateMapping { Name = "Erledigt", States = ["Resolved"] });
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("New", "06:00:00"), ASpan("Active", "09:00:00"), ASpan("Erledigt", "17:00:00")];

            var finished = ServiceNowStateSpanMapper.WhenWorkFinished(spans, desk);

            Assert.That(finished, Is.EqualTo(At("17:00:00")));
        }

        [Test]
        [Ignore("pending: category lists hold raw states")]
        public void WhenWorkWasQueued_ToDoStateNamedLikeAGroup_IsTheLastReturnIntoThatState()
        {
            var desk = new Team { ToDoStates = ["Eingang"], DoingStates = ["Active"], DoneStates = ["Closed"] };
            desk.StateMappings.Add(new StateMapping { Name = "Eingang", States = ["New"] });
            IReadOnlyList<ServiceNowStateSpan> spans = [ASpan("Eingang", "06:00:00"), ASpan("Active", "09:00:00"), ASpan("Eingang", "11:00:00")];

            var queued = ServiceNowStateSpanMapper.WhenWorkWasQueued(spans, desk);

            Assert.That(queued, Is.EqualTo(At("11:00:00")));
        }
    }
}
