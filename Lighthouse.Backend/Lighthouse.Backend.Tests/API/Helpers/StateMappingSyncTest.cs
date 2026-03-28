using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class StateMappingSyncTest
    {
        [Test]
        public void SyncTeamWithTeamSettings_SyncsStateMappings()
        {
            var team = new Team();
            var dto = new TeamSettingDto
            {
                WorkTrackingSystemConnectionId = 1,
                StateMappings =
                [
                    new StateMappingDto { Name = "In Progress", States = ["Active", "Resolved"] },
                    new StateMappingDto { Name = "Waiting", States = ["Blocked"] }
                ]
            };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.StateMappings, Has.Count.EqualTo(2));
            Assert.That(team.StateMappings[0].Name, Is.EqualTo("In Progress"));
            Assert.That(team.StateMappings[0].States, Is.EquivalentTo(new[] { "Active", "Resolved" }));
            Assert.That(team.StateMappings[1].Name, Is.EqualTo("Waiting"));
            Assert.That(team.StateMappings[1].States, Is.EquivalentTo(new[] { "Blocked" }));
        }

        [Test]
        public void SyncTeamWithTeamSettings_EmptyStateMappings_ClearsExisting()
        {
            var team = new Team();
            team.StateMappings.Add(new StateMapping { Name = "Old", States = ["X"] });

            var dto = new TeamSettingDto
            {
                WorkTrackingSystemConnectionId = 1,
                StateMappings = []
            };

            team.SyncTeamWithTeamSettings(dto);

            Assert.That(team.StateMappings, Is.Empty);
        }

        [Test]
        public void WorkItemRelatedSettingsChanged_StateMappingsChanged_ReturnsTrue()
        {
            var team = new Team
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"]
            };
            team.StateMappings.Add(new StateMapping { Name = "A", States = ["X"] });

            var dto = new TeamSettingDto
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"],
                StateMappings = [new StateMappingDto { Name = "A", States = ["Y"] }]
            };

            var result = team.WorkItemRelatedSettingsChanged(dto);

            Assert.That(result, Is.True);
        }

        [Test]
        public void WorkItemRelatedSettingsChanged_StateMappingsUnchanged_ReturnsFalse()
        {
            var team = new Team
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"]
            };
            team.StateMappings.Add(new StateMapping { Name = "A", States = ["X"] });

            var dto = new TeamSettingDto
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"],
                StateMappings = [new StateMappingDto { Name = "A", States = ["X"] }]
            };

            var result = team.WorkItemRelatedSettingsChanged(dto);

            Assert.That(result, Is.False);
        }

        [Test]
        public void WorkItemRelatedSettingsChanged_StateMappingsAddedWhenNone_ReturnsTrue()
        {
            var team = new Team
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"]
            };

            var dto = new TeamSettingDto
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"],
                StateMappings = [new StateMappingDto { Name = "A", States = ["X"] }]
            };

            var result = team.WorkItemRelatedSettingsChanged(dto);

            Assert.That(result, Is.True);
        }
    }
}
