using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.StateMappings, Has.Count.EqualTo(2));
                Assert.That(team.StateMappings[0].Name, Is.EqualTo("In Progress"));
                Assert.That(team.StateMappings[0].States, Is.EquivalentTo(["Active", "Resolved"]));
                Assert.That(team.StateMappings[1].Name, Is.EqualTo("Waiting"));
                Assert.That(team.StateMappings[1].States, Is.EquivalentTo(["Blocked"]));
            }
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

        /// <summary>A2: re-reading a raw state under a different name costs no purge - the next full cycle re-derives it.</summary>
        [Test]
        public void WorkItemRelatedSettingsChanged_StateMappingsChanged_ReturnsFalse()
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

            Assert.That(result, Is.False);
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

        /// <summary>A2: adding the first mapping costs no purge either - the shape of the edit does not change the answer.</summary>
        [Test]
        public void WorkItemRelatedSettingsChanged_StateMappingsAddedWhenNone_ReturnsFalse()
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

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Naming refinement states says nothing new about the work the Team fetches, so it must never
        /// cost the Team the Work Items it holds - a true answer here deletes them.
        /// </summary>
        [TestCase(new string[0], new[] { "Backlog", "Analysing" })]
        [TestCase(new[] { "Backlog" }, new[] { "Backlog", "Next" })]
        [TestCase(new[] { "Backlog", "Analysing" }, new string[0])]
        public void WorkItemRelatedSettingsChanged_RefinementStatesChanged_ReturnsFalse(string[] storedStates, string[] savedStates)
        {
            var team = new Team
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysing", "Next"],
                DoneStates = ["Closed"],
                RefinementSettings = new RefinementSettings
                {
                    States = [.. storedStates.Select(state => new RefinementStateSetting { State = state })],
                },
            };

            var dto = new TeamSettingDto
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysing", "Next"],
                DoneStates = ["Closed"],
                Refinement = new RefinementSettingsDto
                {
                    States = [.. savedStates.Select(state => new RefinementStateSettingDto { State = state })],
                },
            };

            var result = team.WorkItemRelatedSettingsChanged(dto);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// How many votes make a Work Item Ready says nothing about which Work Items the Team fetches either.
        /// </summary>
        [TestCase(3, 3, 1, 2)]
        [TestCase(1, 1, null, 2)]
        [TestCase(2, 5, 4, null)]
        public void WorkItemRelatedSettingsChanged_ReadinessChanged_ReturnsFalse(int minYes, int minVoters, int? discussWhenNo, int? discussWhenYesIf)
        {
            var team = new Team
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysing"],
                DoneStates = ["Closed"],
                RefinementSettings = new RefinementSettings
                {
                    States = [new RefinementStateSetting { State = "Backlog" }],
                    Readiness = new ReadinessSetting { MinYes = 2, MinVoters = 2 },
                },
            };

            var dto = new TeamSettingDto
            {
                DataRetrievalValue = "project = X",
                WorkTrackingSystemConnectionId = 1,
                WorkItemTypes = ["Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["Analysing"],
                DoneStates = ["Closed"],
                Refinement = new RefinementSettingsDto
                {
                    States = [new RefinementStateSettingDto { State = "Backlog" }],
                    Readiness = new ReadinessSettingDto
                    {
                        MinYes = minYes,
                        MinVoters = minVoters,
                        DiscussWhen = new DiscussionRulesDto { No = discussWhenNo, YesIf = discussWhenYesIf },
                    },
                },
            };

            var result = team.WorkItemRelatedSettingsChanged(dto);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// The positive control the cases above need: without it they agree only because the method
        /// can no longer say true at all, and an accidentally-empty registry would read as green.
        /// </summary>
        [Test]
        public void WorkItemRelatedSettingsChanged_ConnectionChanged_ReturnsTrue()
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
                WorkTrackingSystemConnectionId = 2,
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
