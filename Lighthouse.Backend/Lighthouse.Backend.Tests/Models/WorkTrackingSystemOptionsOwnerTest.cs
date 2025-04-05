using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkTrackingSystemOptionsOwnerTest
    {
        [Test]
        [TestCase("Prioritized", StateCategories.ToDo)]
        [TestCase("Analysis In Progress", StateCategories.Doing)]
        [TestCase("Delivered", StateCategories.Done)]
        [TestCase("Jellybean", StateCategories.Unknown)]
        public void MapStateToStateCategory_MapsCorrect(string state, StateCategories expectedStateCategory)
        {
            var subject = CreateSubject();
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New", "Prioritized"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Analysis In Progress", "Implementation"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Delivered"]);

            var stateCategory = subject.MapStateToStateCategory(state);

            Assert.That(stateCategory, Is.EqualTo(expectedStateCategory));
        }

        private WorkTrackingSystemOptionsOwnerTestClass CreateSubject()
        {
            return new WorkTrackingSystemOptionsOwnerTestClass();
        }

    }

    internal class WorkTrackingSystemOptionsOwnerTestClass : WorkTrackingSystemOptionsOwner
    {
        public override List<string> WorkItemTypes { get; set; } = new List<string>();
    }
}
