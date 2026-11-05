using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Two behaviours of the model that need no host and no database: a record keeps what each source read when
    /// it is wrapped for its owner, and an owner's fetch fingerprint moves with its list and only its list.
    /// Each is something a customer sees the effect of: a parent that goes missing after the wrap, and Teams
    /// that are re-read when they should not be.
    ///
    /// What is missing until the slice builds it: the copy constructor carrying the record's readings.
    /// </summary>
    [TestFixture]
    public class ParentSourcesOnTheRecordAndOwnerTest
    {
        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        /// <summary>
        /// Given a record carrying what each source of a list read, when it is wrapped as a Work Item of a Team and
        /// as a Feature of a Portfolio, then the wrapped record carries the same readings, so the second pass still
        /// knows where to look. A record wrapped without them would be placed as if nothing had been read.
        /// </summary>
        [Test]
        [Ignore("Story 6501: needs the record carrying its readings across the wrap.")]
        public void The_readings_survive_being_wrapped_as_a_Work_Item_and_as_a_Feature()
        {
            var readings = ParentReadings.Of(new ParentReadingAt(
                ParentSourceEntry.Field(11),
                ParentReading.LinkCandidates(ParentResolution.From(new[] { new ParentCandidate("PAY-7", null) }))));
            var record = new WorkItemBase { ReferenceId = "PAY-142", ParentReadings = readings };

            var asAWorkItem = new WorkItem(record, JiraConnectorTestSetup.ATeamOnJiraCloud());
            var asAFeature = new Feature(record);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asAWorkItem.ParentReadings, Is.EqualTo(readings));
                Assert.That(asAFeature.ParentReadings, Is.EqualTo(readings));
            }
        }

        /// <summary>
        /// Given a Team that owns "Is caused by" then the own parent, and a second Team that follows the
        /// connection's default naming the same two, when the first Team swaps its two entries, then its
        /// fingerprint moves and the follower's does not; and the follower's fingerprint is the one a Team owning
        /// the default's list has, not the one of a Team with no list.
        /// </summary>
        [Test]
        public void Saving_a_different_list_moves_the_owners_fingerprint_and_a_follower_renders_the_defaults_list()
        {
            var owner = JiraConnectorTestSetup.ATeamOnJiraCloud();
            var follower = JiraConnectorTestSetup.ATeamOnJiraCloud();
            var twin = JiraConnectorTestSetup.ATeamOnJiraCloud();
            var connection = owner.WorkTrackingSystemConnection;
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 11, DisplayName = "Is caused by", Reference = "is caused by" });
            foreach (var team in new[] { follower, twin })
            {
                team.WorkTrackingSystemConnection = connection;
                team.WorkTrackingSystemConnectionId = connection.Id;
            }

            var causedByThenOwnParent = ParentSources.Of([ParentSourceEntry.Field(11), OwnParent]).List!;
            var ownParentThenCausedBy = ParentSources.Of([OwnParent, ParentSourceEntry.Field(11)]).List!;
            connection.SetParentSourcesDefaultForTeams(causedByThenOwnParent);
            owner.OwnParentSources(causedByThenOwnParent);
            follower.FollowParentSourcesDefault();
            twin.OwnParentSources(causedByThenOwnParent);
            var ownerBefore = FetchFingerprint.For(owner);
            var followerBefore = FetchFingerprint.For(follower);

            owner.OwnParentSources(ownParentThenCausedBy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FetchFingerprint.For(owner), Is.Not.EqualTo(ownerBefore), "A different order names a different parent, so the Team must be re-read.");
                Assert.That(FetchFingerprint.For(follower), Is.EqualTo(followerBefore), "The default did not change, so its followers are not re-read.");
                Assert.That(followerBefore, Is.EqualTo(FetchFingerprint.For(twin)), "A follower renders what the default renders.");
            }
        }
    }
}
