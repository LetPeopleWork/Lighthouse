using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
{
    /// <summary>
    /// The rule Jira and Azure DevOps have always applied, pinned in one place so a third connector
    /// cannot quietly diverge from it again (Bug #5621 F2).
    /// </summary>
    [TestFixture]
    public class WorkItemCategoryCrossingTest
    {
        private static readonly string[] DoneStates = ["Resolved", "Closed"];

        private static readonly string[] DoingStates = ["In Progress"];

        private static readonly string[] NothingToIgnore = [];

        private static readonly DateTime OnTheFirst = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime OnTheTenth = new(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime OnTheSeventeenth = new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);

        [Test]
        public void WorkThatNeverReachedTheCategory_HasNoEntry()
        {
            var moves = new[] { AMove("New", "In Progress", OnTheFirst) };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoneStates, NothingToIgnore);

            Assert.That(entry, Is.Null);
        }

        [Test]
        public void AnArrivalFromOutsideTheCategory_IsTheEntry()
        {
            var moves = new[]
            {
                AMove("New", "In Progress", OnTheFirst),
                AMove("In Progress", "Resolved", OnTheTenth),
            };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoneStates, NothingToIgnore);

            Assert.That(entry, Is.EqualTo(OnTheTenth));
        }

        // The half Bug #5621 F2 was filed for. A desk that maps both Resolved and Closed to Done
        // finishes the work when it is resolved; the instance's own close-out job moving it on a week
        // later has undone nothing, so it must not re-date the finish and inflate every Cycle Time.
        [Test]
        public void MovingBetweenTwoStatesOfTheSameCategory_DoesNotReDateTheEntry()
        {
            var moves = new[]
            {
                AMove("In Progress", "Resolved", OnTheTenth),
                AMove("Resolved", "Closed", OnTheSeventeenth),
            };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoneStates, NothingToIgnore);

            Assert.That(entry, Is.EqualTo(OnTheTenth),
                "Nothing was started or finished by a move inside the category the team already counted the work in.");
        }

        // The distinction the rule exists to draw: here something WAS undone, so the second arrival is
        // the real one.
        [Test]
        public void LeavingTheCategoryAndComingBack_ReDatesTheEntry()
        {
            var moves = new[]
            {
                AMove("In Progress", "Resolved", OnTheFirst),
                AMove("Resolved", "In Progress", OnTheTenth),
                AMove("In Progress", "Closed", OnTheSeventeenth),
            };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoneStates, NothingToIgnore);

            Assert.That(entry, Is.EqualTo(OnTheSeventeenth));
        }

        // Why the ignore list exists: a reopen puts the item back into Doing, and rework must not
        // restart the clock on work that had already begun.
        [Test]
        public void ArrivingFromAnIgnoredState_IsNotAnEntry()
        {
            var moves = new[]
            {
                AMove("New", "In Progress", OnTheFirst),
                AMove("In Progress", "Resolved", OnTheTenth),
                AMove("Resolved", "In Progress", OnTheSeventeenth),
            };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoingStates, DoneStates);

            Assert.That(entry, Is.EqualTo(OnTheFirst));
        }

        // An item whose earliest observed move has no predecessor — Azure DevOps reads the first
        // revision this way, and a record whose history begins mid-life reads the same. Coming from
        // nowhere is coming from outside the category.
        [Test]
        public void ArrivingFromNoPreviousState_IsAnEntry()
        {
            var moves = new[] { AMove(string.Empty, "In Progress", OnTheFirst) };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoingStates, DoneStates);

            Assert.That(entry, Is.EqualTo(OnTheFirst));
        }

        [Test]
        public void AnItemThatMadeNoMoves_HasNoEntry()
        {
            var entry = WorkItemCategoryCrossing.LastEntryInto([], DoneStates, NothingToIgnore);

            Assert.That(entry, Is.Null);
        }

        // Callers compare these against other instants, and a date that does not say it is universal
        // shifts by the host's offset the moment anything formats it.
        [Test]
        public void TheEntryItReports_IsUniversal()
        {
            var moves = new[] { AMove("New", "In Progress", DateTime.SpecifyKind(OnTheFirst, DateTimeKind.Unspecified)) };

            var entry = WorkItemCategoryCrossing.LastEntryInto(moves, DoingStates, NothingToIgnore);

            Assert.That(entry?.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        private static WorkItemStateTransition AMove(string from, string to, DateTime at)
        {
            return new WorkItemStateTransition { FromState = from, ToState = to, TransitionedAt = at };
        }
    }
}
