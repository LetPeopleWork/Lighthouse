using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Where a Team's Work Item hangs once the Team's own choice and the connection's Parent default have met:
    /// the connector reads one place per entry of the Team's effective list, and the first place that holds a
    /// parent wins. The item here holds a value in both fields and under its tracker parent, so the place it
    /// ends up under says which list the Team read.
    /// </summary>
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    public class WhereATeamsWorkItemHangsTest
    {
        private const int TheTeamsOwnField = 5;
        private const int TheConnectionsDefault = 9;

        private const string UnderTheTeamsOwnField = "OWN-1";
        private const string UnderTheDefaultsField = "DEFAULT-1";
        private const string UnderTheTrackersOwnParent = "TRACKER-1";

        [Test]
        public void A_Team_with_its_own_field_hangs_its_item_under_that_field_whatever_the_default_names()
        {
            var team = ATeamOnAConnectionWhoseDefaultNames(TheConnectionsDefault);
            team.OwnParentOverride(TheTeamsOwnField);

            Assert.That(WhereTheItemHangs(team), Is.EqualTo(UnderTheTeamsOwnField));
        }

        [Test]
        public void A_Team_that_chose_None_hangs_its_item_under_the_trackers_own_parent_whatever_the_default_names()
        {
            var team = ATeamOnAConnectionWhoseDefaultNames(TheConnectionsDefault);
            team.OwnParentOverride(null);

            Assert.That(WhereTheItemHangs(team), Is.EqualTo(UnderTheTrackersOwnParent));
        }

        [Test]
        public void A_Team_that_follows_the_default_hangs_its_item_under_the_field_the_default_names()
        {
            var team = ATeamOnAConnectionWhoseDefaultNames(TheConnectionsDefault);
            team.OwnParentOverride(TheTeamsOwnField);
            team.FollowParentOverrideDefault();

            Assert.That(WhereTheItemHangs(team), Is.EqualTo(UnderTheDefaultsField));
        }

        [Test]
        public void A_Team_that_follows_a_default_nobody_set_hangs_its_item_under_the_trackers_own_parent()
        {
            var team = ATeamOnAConnectionWhoseDefaultNames(null);
            team.FollowParentOverrideDefault();

            Assert.That(WhereTheItemHangs(team), Is.EqualTo(UnderTheTrackersOwnParent));
        }

        [Test]
        public void A_Team_that_followed_the_default_hangs_its_item_under_the_trackers_own_parent_once_the_default_is_cleared()
        {
            var connection = new WorkTrackingSystemConnection { ParentOverrideForTeams = TheConnectionsDefault };
            var team = new Team { WorkTrackingSystemConnection = connection };
            team.FollowParentOverrideDefault();
            var whileTheDefaultIsSet = WhereTheItemHangs(team);

            connection.ParentOverrideForTeams = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileTheDefaultIsSet, Is.EqualTo(UnderTheDefaultsField));
                Assert.That(WhereTheItemHangs(team), Is.EqualTo(UnderTheTrackersOwnParent));
            }
        }

        private static Team ATeamOnAConnectionWhoseDefaultNames(int? defaultField)
            => new() { WorkTrackingSystemConnection = new WorkTrackingSystemConnection { ParentOverrideForTeams = defaultField } };

        /// <summary>Reads each entry of the Team's effective list the way a connector does, then lets the first place with a parent win.</summary>
        private static string WhereTheItemHangs(Team team)
        {
            var readings = ParentReadings.Of([.. team.TheEffectiveParentSources().Entries.Select(entry => new ParentReadingAt(entry, WhatItHoldsAt(entry)))]);
            var item = new WorkItemBase { ParentReferenceId = string.Empty };

            ParentSourceSelector.HangUnder(item, readings, ParentSourceSelector.Choose(readings, new StoredFeatures(new Dictionary<string, DateTime?>()), ParentNarrowing.Narrow));

            return item.ParentReferenceId;
        }

        private static ParentReading WhatItHoldsAt(ParentSourceEntry entry)
            => entry.AdditionalFieldDefinitionId switch
            {
                TheTeamsOwnField => ParentReading.FieldValue(UnderTheTeamsOwnField),
                TheConnectionsDefault => ParentReading.FieldValue(UnderTheDefaultsField),
                _ => ParentReading.TrackersOwn(UnderTheTrackersOwnParent),
            };
    }
}
