using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class TeamListSyncRawStatesTest
    {
        private const string Pending = "pending: category lists hold raw states";

        private static string[] Listed(params string[] states) => states;

        private static TeamSettingDto Settings(string[] doing, params StateMappingDto[] mappings)
            => new()
            {
                WorkTrackingSystemConnectionId = 1,
                ToDoStates = ["Backlog"],
                DoingStates = [.. doing],
                DoneStates = ["Done"],
                StateMappings = [.. mappings],
            };

        [Test]
        [Ignore(Pending)]
        public void SyncTeamWithTeamSettings_GroupNamedAfterItsOwnState_StoresTheOtherStatesRightAfterIt()
        {
            var team = new Team();

            team.SyncTeamWithTeamSettings(Settings(["Analysis", "Active", "Blocked"], new StateMappingDto { Name = "Active", States = ["Active", "Resolved"] }));

            Assert.That(team.DoingStates, Is.EqualTo(Listed("Analysis", "Active", "Resolved", "Blocked")));
        }

        [Test]
        [Ignore(Pending)]
        public void SyncTeamWithTeamSettings_CollapsedGroupFromTheCurrentEditor_StoresItsStatesInPlace()
        {
            var team = new Team();

            team.SyncTeamWithTeamSettings(Settings(["Analysis", "Review", "Waiting"], new StateMappingDto { Name = "Review", States = ["Code Review", "Testing"] }));

            Assert.That(team.DoingStates, Is.EqualTo(Listed("Analysis", "Code Review", "Testing", "Waiting")));
        }

        [Test]
        [Ignore(Pending)]
        public void SyncTeamWithTeamSettings_GroupChangedInTheSameSave_ReadsTheListAgainstTheGroupJustSent()
        {
            var team = new Team();
            team.StateMappings.Add(new StateMapping { Name = "Active", States = ["Active", "Resolved"] });

            team.SyncTeamWithTeamSettings(Settings(["Active"], new StateMappingDto { Name = "Active", States = ["Active", "Blocked"] }));

            Assert.That(team.DoingStates, Is.EqualTo(Listed("Active", "Blocked")));
        }

        [Test]
        public void SyncTeamWithTeamSettings_GroupDeletedInTheSameSave_ReadsTheListWithoutIt()
        {
            var team = new Team();
            team.StateMappings.Add(new StateMapping { Name = "Active", States = ["Active", "Resolved"] });

            team.SyncTeamWithTeamSettings(Settings(["Analysis", "Active"]));

            Assert.That(team.DoingStates, Is.EqualTo(Listed("Analysis", "Active")));
        }

        [Test]
        public void SyncTeamWithTeamSettings_RawStatesWithAGroup_KeepsTheListAsSent()
        {
            var team = new Team();

            team.SyncTeamWithTeamSettings(Settings(["Code Review", "Analysis", "Testing"], new StateMappingDto { Name = "Review", States = ["Code Review", "Testing"] }));

            Assert.That(team.DoingStates, Is.EqualTo(Listed("Code Review", "Analysis", "Testing")));
        }

        [Test]
        [Ignore(Pending)]
        public void SyncTeamWithTeamSettings_EntriesThatAreNullOrOnlyWhitespace_AreLeftOut()
        {
            var team = new Team();

            team.SyncTeamWithTeamSettings(Settings(["Analysis", null!, "  ", "\t", "Testing"]));

            Assert.That(team.DoingStates, Is.EqualTo(Listed("Analysis", "Testing")));
        }

        [Test]
        [Ignore(Pending)]
        public void SyncTeamWithTeamSettings_RefinementChosenOnAGroup_KeepsTheGroup()
        {
            var team = new Team();
            var settings = Settings(["Analysis", "Code Review", "Testing"], new StateMappingDto { Name = "Review", States = ["Code Review", "Testing"] });
            settings.Refinement = new RefinementSettingsDto { States = [new RefinementStateSettingDto { State = "Review" }] };

            team.SyncTeamWithTeamSettings(settings);

            Assert.That(team.RefinementSettings?.States.Select(state => state.State), Is.EqualTo(Listed("Review")));
        }
    }
}
