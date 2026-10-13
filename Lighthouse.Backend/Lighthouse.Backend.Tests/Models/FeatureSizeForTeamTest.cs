using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// A Feature's size on one Team is that Team's own share: the Team's child Work Items under it, in
    /// any state. The scatter, the percentiles and the status on a Team all read this one number, so a
    /// second definition anywhere would let them disagree about the same Feature.
    ///
    /// Shaped after the demo Feature OE-002, which four Teams work on: Equinox has 8 Work Items under
    /// it, Gravity 4, Lightspeed 4 and Zenith 3, and the Portfolio sees all 19. Meridian works in the
    /// same Portfolio but has nothing under OE-002.
    ///
    /// The adding-up test is an invariant the pending share must satisfy: whatever the share is worked
    /// out from, the shares of every Team that has work under a Feature must come to the size the
    /// Portfolio shows for it.
    ///
    /// These are example-based on purpose. The rule has two branches (default-sized or not) over a
    /// handful of rows, the project has no property-testing library, and the examples are the demo
    /// numbers a reader can check by hand.
    /// </summary>
    [TestFixture]
    [Category("story-6251-team-feature-size")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("us-05")]
    public class FeatureSizeForTeamTest
    {
        private const int EquinoxId = 1;
        private const int GravityId = 2;
        private const int LightspeedId = 3;
        private const int ZenithId = 4;
        private const int MeridianId = 5;

        private static readonly Team Equinox = ATeam(EquinoxId, "Equinox");
        private static readonly Team Gravity = ATeam(GravityId, "Gravity");
        private static readonly Team Lightspeed = ATeam(LightspeedId, "Lightspeed");
        private static readonly Team Zenith = ATeam(ZenithId, "Zenith");

        [TestCase(EquinoxId, 8)]
        [TestCase(GravityId, 4)]
        [TestCase(LightspeedId, 4)]
        [TestCase(ZenithId, 3)]
        [Category("contract-shape:pure-function")]
        public void A_Feature_several_Teams_work_on_is_as_big_on_each_Team_as_that_Team_s_own_part(int teamId, int expectedShare)
        {
            var feature = CoralReefRestoration();

            Assert.That(feature.SizeFor(teamId), Is.EqualTo(expectedShare));
        }

        [Test]
        [Category("kpi")]
        [Category("contract-shape:pure-function")]
        public void The_parts_of_every_Team_add_up_to_the_Feature_s_size()
        {
            var feature = CoralReefRestoration();

            var everyTeamsPart = feature.FeatureWork.Select(work => work.TeamId).Distinct().Sum(feature.SizeFor);

            Assert.That(everyTeamsPart, Is.EqualTo(feature.Size).And.EqualTo(19));
        }

        [Test]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Team_with_no_Work_Item_under_the_Feature_has_no_part_of_it()
        {
            var feature = CoralReefRestoration();

            Assert.That(feature.SizeFor(MeridianId), Is.Zero);
        }

        // The share counts the Team's Work Items in any state, finished ones included, not only what
        // is left to do.
        [Test]
        [Category("contract-shape:pure-function")]
        public void A_Team_s_part_counts_its_finished_Work_Items_as_well_as_its_open_ones()
        {
            var feature = new Feature();
            feature.FeatureWork.Add(new FeatureWork(Gravity, remainingWork: 1, totalWorkItems: 4, feature));

            Assert.That(feature.SizeFor(GravityId), Is.EqualTo(4));
        }

        // A Feature sized by its Portfolio's default carries made-up work for Teams that have no child
        // under it at all, so a share above zero alone is not enough to count it.
        [Test]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Feature_sized_by_its_Portfolio_s_default_has_no_part_for_any_Team_even_with_work_made_up_for_it()
        {
            var feature = new Feature { IsUsingDefaultFeatureSize = true };
            feature.FeatureWork.Add(new FeatureWork(Gravity, remainingWork: 10, totalWorkItems: 10, feature));

            Assert.That(feature.SizeFor(GravityId), Is.Zero);
        }

        private static Feature CoralReefRestoration()
        {
            var feature = new Feature { ReferenceId = "OE-002", Name = "Coral Reef Restoration Program" };
            feature.FeatureWork.Add(new FeatureWork(Equinox, remainingWork: 8, totalWorkItems: 8, feature));
            feature.FeatureWork.Add(new FeatureWork(Gravity, remainingWork: 4, totalWorkItems: 4, feature));
            feature.FeatureWork.Add(new FeatureWork(Lightspeed, remainingWork: 4, totalWorkItems: 4, feature));
            feature.FeatureWork.Add(new FeatureWork(Zenith, remainingWork: 3, totalWorkItems: 3, feature));
            return feature;
        }

        private static Team ATeam(int id, string name) => new() { Id = id, Name = name };
    }
}
