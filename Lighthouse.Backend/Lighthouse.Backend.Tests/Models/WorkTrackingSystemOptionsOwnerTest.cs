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
        [TestCase("New", StateCategories.ToDo)]
        [TestCase("NEW", StateCategories.ToDo)]
        [TestCase("new", StateCategories.ToDo)]
        [TestCase("nEw", StateCategories.ToDo)]
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

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_InitializesSLE()
        {
            var subject = CreateSubject();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.ServiceLevelExpectationProbability, Is.Zero);
                Assert.That(subject.ServiceLevelExpectationRange, Is.Zero);
            }
        }

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_InitializesWIPLimit()
        {
            var subject = CreateSubject();
            Assert.That(subject.SystemWIPLimit, Is.Zero);
        }

        [Test]
        public void NewWorkTrackingSystemOptionsOwner_InitializesEmptyStateMappings()
        {
            var subject = CreateSubject();
            Assert.That(subject.StateMappings, Is.Empty);
        }

        [Test]
        public void StateMappings_CanAddMapping()
        {
            var subject = CreateSubject();
            var mapping = new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] };

            subject.StateMappings.Add(mapping);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.StateMappings, Has.Count.EqualTo(1));
                Assert.That(subject.StateMappings[0].Name, Is.EqualTo("In Progress"));
                Assert.That(subject.StateMappings[0].States, Is.EquivalentTo(["Active", "Resolved"]));
            }
        }

        [Test]
        public void GetRawStatesForCategory_NoMappings_ReturnsSameStates()
        {
            var subject = CreateSubject();
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active", "Resolved"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active", "Resolved"]));
        }

        [Test]
        public void GetRawStatesForCategory_WithMappedLabel_ExpandsToRawStates()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active", "Resolved"]));
        }

        [Test]
        public void GetRawStatesForCategory_MixedRawAndMapped_ExpandsOnlyMappedLabels()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress", "Development"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active", "Resolved", "Development"]));
        }

        [Test]
        public void GetRawStatesForCategory_MappedLabelCaseInsensitive_ExpandsCorrectly()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active"] });
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["in progress"]);

            var rawStates = subject.GetRawStatesForCategory(subject.DoingStates);

            Assert.That(rawStates, Is.EquivalentTo(["Active"]));
        }

        [Test]
        [TestCase("Active", StateCategories.Doing)]
        [TestCase("Resolved", StateCategories.Doing)]
        [TestCase("Development", StateCategories.Doing)]
        [TestCase("New", StateCategories.ToDo)]
        [TestCase("Closed", StateCategories.Done)]
        [TestCase("Unknown", StateCategories.Unknown)]
        public void MapStateToStateCategory_WithMappings_ExpandsAndMatchesRawState(string state, StateCategories expected)
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "In Progress", States = ["Active", "Resolved"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["In Progress", "Development"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Closed"]);

            var result = subject.MapStateToStateCategory(state);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void MapStateToStateCategory_WithMappingInToDoStates_ExpandsCorrectly()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "Backlog", States = ["New", "Prioritized"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["Backlog"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Closed"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.MapStateToStateCategory("New"), Is.EqualTo(StateCategories.ToDo));
                Assert.That(subject.MapStateToStateCategory("Prioritized"), Is.EqualTo(StateCategories.ToDo));
                Assert.That(subject.MapStateToStateCategory("Active"), Is.EqualTo(StateCategories.Doing));
            }
        }

        [Test]
        public void MapStateToStateCategory_WithMappingInDoneStates_ExpandsCorrectly()
        {
            var subject = CreateSubject();
            subject.StateMappings.Add(new StateMapping { Name = "Completed", States = ["Closed", "Delivered"] });
            subject.ToDoStates.Clear();
            subject.ToDoStates.AddRange(["New"]);
            subject.DoingStates.Clear();
            subject.DoingStates.AddRange(["Active"]);
            subject.DoneStates.Clear();
            subject.DoneStates.AddRange(["Completed"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.MapStateToStateCategory("Closed"), Is.EqualTo(StateCategories.Done));
                Assert.That(subject.MapStateToStateCategory("Delivered"), Is.EqualTo(StateCategories.Done));
            }
        }

        private static WorkTrackingSystemOptionsOwnerTestClass CreateSubject()
        {
            return new WorkTrackingSystemOptionsOwnerTestClass();
        }

    }

    internal class WorkTrackingSystemOptionsOwnerTestClass : WorkTrackingSystemOptionsOwner
    {
        public override List<string> WorkItemTypes { get; set; } = new List<string>();
        public override int DoneItemsCutoffDays { get; set; } = 180;
    }
}
