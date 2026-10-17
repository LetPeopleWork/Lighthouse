using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    public class TeamExtensionsTest
    {
        private const int TheConnectionsDefault = 9;
        private const int AnotherField = 7;

        [Test]
        public void A_new_Team_copied_from_a_follower_without_its_source_follows_the_default()
        {
            var team = ANewTeam();

            team.SyncTeamWithTeamSettings(SettingsNaming(TheConnectionsDefault));

            Assert.That(
                (team.ParentOverrideSource, team.OwnParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.ConnectionDefault, (int?)null)));
        }

        [Test]
        public void A_new_Team_sent_another_field_without_a_source_owns_it()
        {
            var team = ANewTeam();

            team.SyncTeamWithTeamSettings(SettingsNaming(AnotherField));

            Assert.That(
                (team.ParentOverrideSource, team.OwnParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)AnotherField)));
        }

        [Test]
        public void A_new_Team_sent_its_default_as_its_own_owns_it()
        {
            var team = ANewTeam();
            var settings = SettingsNaming(TheConnectionsDefault);
            settings.ParentOverrideSource = SettingSource.Own;

            team.SyncTeamWithTeamSettings(settings);

            Assert.That(
                (team.ParentOverrideSource, team.OwnParentOverrideAdditionalFieldDefinitionId),
                Is.EqualTo((SettingSource.Own, (int?)TheConnectionsDefault)));
        }

        private static Team ANewTeam() => new()
        {
            WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = TheConnectionsDefault },
        };

        private static TeamSettingDto SettingsNaming(int field) => new()
        {
            ParentOverrideAdditionalFieldDefinitionId = field,
            WorkTrackingSystemConnectionId = 1,
        };
    }
}
