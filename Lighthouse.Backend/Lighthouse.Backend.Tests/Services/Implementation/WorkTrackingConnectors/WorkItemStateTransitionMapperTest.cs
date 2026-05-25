using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
{
    public class WorkItemStateTransitionMapperTest
    {
        [Test]
        public void MapToMappedStates_RawStatesHaveMappings_TranslatesFromAndToStates()
        {
            var owner = CreateOwner(
                ("In Progress", ["Active", "Doing"]),
                ("Done", ["Closed", "Resolved"]));

            var transitionedAt = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc);
            var raw = new List<WorkItemStateTransition>
            {
                new() { FromState = "Active", ToState = "Resolved", TransitionedAt = transitionedAt },
            };

            var mapped = WorkItemStateTransitionMapper.MapToMappedStates(raw, owner);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mapped, Has.Count.EqualTo(1));
                Assert.That(mapped[0].FromState, Is.EqualTo("In Progress"));
                Assert.That(mapped[0].ToState, Is.EqualTo("Done"));
                Assert.That(mapped[0].TransitionedAt, Is.EqualTo(transitionedAt));
            }
        }

        [Test]
        public void MapToMappedStates_ConsecutiveRawStatesMapToSameMappedName_SuppressesNoOpSelfTransition()
        {
            var owner = CreateOwner(("In Progress", ["Active", "Doing"]));

            var raw = new List<WorkItemStateTransition>
            {
                new() { FromState = "Active", ToState = "Doing", TransitionedAt = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc) },
            };

            var mapped = WorkItemStateTransitionMapper.MapToMappedStates(raw, owner);

            Assert.That(mapped, Is.Empty);
        }

        [Test]
        public void MapToMappedStates_RawStateHasNoMapping_KeepsRawNameAsMappedName()
        {
            var owner = CreateOwner(("Done", ["Closed"]));

            var raw = new List<WorkItemStateTransition>
            {
                new() { FromState = "New", ToState = "Closed", TransitionedAt = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc) },
            };

            var mapped = WorkItemStateTransitionMapper.MapToMappedStates(raw, owner);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mapped, Has.Count.EqualTo(1));
                Assert.That(mapped[0].FromState, Is.EqualTo("New"));
                Assert.That(mapped[0].ToState, Is.EqualTo("Done"));
            }
        }

        private static IWorkItemQueryOwner CreateOwner(params (string mappedName, string[] rawStates)[] mappings)
        {
            var owner = new TestQueryOwner
            {
                StateMappings = mappings
                    .Select(m => new StateMapping { Name = m.mappedName, States = [.. m.rawStates] })
                    .ToList(),
            };

            return owner;
        }

        private sealed class TestQueryOwner : WorkTrackingSystemOptionsOwner
        {
            public override List<string> WorkItemTypes { get; set; } = [];
            public override int DoneItemsCutoffDays { get; set; }
        }
    }
}
