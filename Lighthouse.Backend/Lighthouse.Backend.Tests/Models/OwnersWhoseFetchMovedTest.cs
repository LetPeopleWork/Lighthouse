using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    public class OwnersWhoseFetchMovedTest
    {
        private const int TheTeam = 4;
        private const int CausedBy = 40;
        private const int ParentLink = 41;
        private const int StoryPoints = 42;

        private static readonly int[] JustTheTeam = [TheTeam];
        private static readonly int[] InIdOrder = [2, 5, 9];

        public enum ItsParentOverride
        {
            Own,
            FollowsTheDefault,
        }

        public enum TheSave
        {
            ChangesTheDefault,
            ChangesAFieldDefinition,
            ChangesTheEstimation,
            ChangesNothingFetched,
        }

        // The Estimation is read when a page loads, not asked of the tracker, so changing it moves nobody's fetch.
        [TestCase(ItsParentOverride.Own, TheSave.ChangesTheDefault, false)]
        [TestCase(ItsParentOverride.Own, TheSave.ChangesAFieldDefinition, true)]
        [TestCase(ItsParentOverride.Own, TheSave.ChangesTheEstimation, false)]
        [TestCase(ItsParentOverride.Own, TheSave.ChangesNothingFetched, false)]
        [TestCase(ItsParentOverride.FollowsTheDefault, TheSave.ChangesTheDefault, true)]
        [TestCase(ItsParentOverride.FollowsTheDefault, TheSave.ChangesAFieldDefinition, true)]
        [TestCase(ItsParentOverride.FollowsTheDefault, TheSave.ChangesTheEstimation, false)]
        [TestCase(ItsParentOverride.FollowsTheDefault, TheSave.ChangesNothingFetched, false)]
        [Category("contract-shape:bounded-change")]
        public void A_Team_moved_exactly_when_what_it_asks_the_tracker_for_changed(ItsParentOverride parentOverride, TheSave save, bool moved)
        {
            var connection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC – Payments", ParentOverrideForTeams = CausedBy };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedBy, DisplayName = "Caused by", Reference = "customfield_10500" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = ParentLink, DisplayName = "Parent Link", Reference = "customfield_10014" });
            var team = new Team { Id = TheTeam, Name = "Fraud Signals", WorkTrackingSystemConnectionId = connection.Id, WorkTrackingSystemConnection = connection };
            if (parentOverride == ItsParentOverride.Own)
            {
                team.OwningParentOverride(CausedBy);
            }

            var before = WhatItFetches(team);
            Apply(save, team);
            var after = WhatItFetches(team);

            Assert.That(OwnersWhoseFetchMoved.Between(before, after), moved ? Is.EqualTo(JustTheTeam) : Is.Empty);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void An_owner_that_appeared_during_the_save_is_updated()
        {
            var moved = OwnersWhoseFetchMoved.Between(new Dictionary<int, string>(), new Dictionary<int, string> { [TheTeam] = "a" });

            Assert.That(moved, Is.EqualTo(JustTheTeam));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void An_owner_that_disappeared_during_the_save_is_not_updated()
        {
            var moved = OwnersWhoseFetchMoved.Between(new Dictionary<int, string> { [TheTeam] = "a" }, new Dictionary<int, string>());

            Assert.That(moved, Is.Empty);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public void The_owners_that_moved_come_in_id_order_and_the_unmoved_are_left_out()
        {
            var before = new Dictionary<int, string> { [9] = "a", [2] = "a", [7] = "same", [5] = "a" };
            var after = new Dictionary<int, string> { [9] = "b", [7] = "same", [5] = "b", [2] = "b" };

            Assert.That(OwnersWhoseFetchMoved.Between(before, after), Is.EqualTo(InIdOrder));
        }

        private static Dictionary<int, string> WhatItFetches(Team team) => new() { [team.Id] = FetchFingerprint.For(team) };

        private static void Apply(TheSave save, Team team)
        {
            var connection = team.WorkTrackingSystemConnection;
            switch (save)
            {
                case TheSave.ChangesTheDefault:
                    connection.ParentOverrideForTeams = ParentLink;
                    break;
                case TheSave.ChangesAFieldDefinition:
                    connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = StoryPoints, DisplayName = "Story Points", Reference = "customfield_10016" });
                    break;
                case TheSave.ChangesTheEstimation:
                    team.OwningEstimationField(StoryPoints);
                    break;
                case TheSave.ChangesNothingFetched:
                    connection.Name = "Jira DC – Payments (renamed)";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(save), save, "A save this table does not know");
            }
        }
    }
}
