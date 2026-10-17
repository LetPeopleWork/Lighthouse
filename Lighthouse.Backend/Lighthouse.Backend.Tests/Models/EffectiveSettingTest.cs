using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    public class EffectiveSettingTest
    {
        private const int TheTeamsOwnField = 5;
        private const int TheConnectionsDefault = 9;

        [TestCase(SettingSource.ConnectionDefault, null, null, null)]
        [TestCase(SettingSource.ConnectionDefault, null, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, TheTeamsOwnField, null, null)]
        [TestCase(SettingSource.ConnectionDefault, TheTeamsOwnField, TheConnectionsDefault, TheConnectionsDefault)]
        [TestCase(SettingSource.Own, null, null, null)]
        [TestCase(SettingSource.Own, null, TheConnectionsDefault, null)]
        [TestCase(SettingSource.Own, TheTeamsOwnField, null, TheTeamsOwnField)]
        [TestCase(SettingSource.Own, TheTeamsOwnField, TheConnectionsDefault, TheTeamsOwnField)]
        public void A_Team_reads_its_own_Parent_Override_Field_or_the_connections_default_by_its_source(
            SettingSource source, int? ownValueBeforeTheSourceWasSet, int? connectionDefault, int? expected)
        {
            var team = new Team
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = connectionDefault },
            };

            team.OwnParentOverride(ownValueBeforeTheSourceWasSet);
            if (source == SettingSource.ConnectionDefault)
            {
                team.FollowParentOverrideDefault();
            }

            Assert.That(team.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(expected));
        }

        [Test]
        public void A_following_Team_built_without_its_connection_reads_no_field()
        {
            var team = new Team();

            team.FollowParentOverrideDefault();

            Assert.That(team.ParentOverrideAdditionalFieldDefinitionId, Is.Null);
        }

        [Test]
        public void A_Portfolio_has_no_Teams_default_to_follow()
        {
            var portfolio = new Portfolio
            {
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = TheConnectionsDefault },
            };

            portfolio.FollowParentOverrideDefault();

            Assert.That(portfolio.ParentOverrideAdditionalFieldDefinitionId, Is.Null);
        }
    }
}
