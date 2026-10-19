using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// A save that sends no source takes the Estimation bundle as one value: it becomes the Team's own only when it
    /// names a field and differs in any part from what the Team read before; anything else leaves the Team following.
    /// </summary>
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    public class EstimationBundleTest
    {
        private const int StoryPoints = 11;

        private const int TShirtSize = 12;

        private const string Points = "Points";

        private const string Sizes = "Sizes";

        private static readonly EstimationBundle TheDefault = new(StoryPoints, Points, true, ["S", "M", "L"]);

        private static IEnumerable<TestCaseData> OnePartDiffers()
        {
            yield return new TestCaseData(new EstimationBundle(TShirtSize, Points, true, ["S", "M", "L"])).SetArgDisplayNames("field");
            yield return new TestCaseData(new EstimationBundle(StoryPoints, Sizes, true, ["S", "M", "L"])).SetArgDisplayNames("unit");
            yield return new TestCaseData(new EstimationBundle(StoryPoints, "points", true, ["S", "M", "L"])).SetArgDisplayNames("unit in another case");
            yield return new TestCaseData(new EstimationBundle(StoryPoints, Points, false, ["S", "M", "L"])).SetArgDisplayNames("non-numeric flag");
            yield return new TestCaseData(new EstimationBundle(StoryPoints, Points, true, ["S", "M", "L", "XL"])).SetArgDisplayNames("categories");
            yield return new TestCaseData(new EstimationBundle(StoryPoints, Points, true, ["L", "M", "S"])).SetArgDisplayNames("categories reordered");
        }

        [TestCaseSource(nameof(OnePartDiffers))]
        public void A_bundle_differing_in_one_part_becomes_the_Teams_own_whole(EstimationBundle sent)
        {
            var team = AFollowingTeam();

            team.TakeEstimationAsSent(null, sent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.EstimationSource, Is.EqualTo(SettingSource.Own));
                Assert.That(team.EffectiveEstimation, Is.EqualTo(sent));
            }
        }

        [Test]
        public void A_bundle_equal_to_what_the_Team_reads_leaves_it_following()
        {
            var team = AFollowingTeam();

            team.TakeEstimationAsSent(null, new EstimationBundle(StoryPoints, Points, true, ["S", "M", "L"]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.EstimationSource, Is.EqualTo(SettingSource.ConnectionDefault));
                Assert.That(team.EffectiveEstimation, Is.EqualTo(TheDefault));
            }
        }

        [Test]
        public void A_bundle_without_a_field_leaves_the_Team_following()
        {
            var team = AFollowingTeam();

            team.TakeEstimationAsSent(null, new EstimationBundle(null, Sizes, false, ["XS"]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.EstimationSource, Is.EqualTo(SettingSource.ConnectionDefault));
                Assert.That(team.EffectiveEstimation, Is.EqualTo(TheDefault));
            }
        }

        [Test]
        public void A_bundle_without_a_field_leaves_an_owning_Team_its_own_bundle()
        {
            var team = AFollowingTeam();
            var owned = new EstimationBundle(TShirtSize, Sizes, true, ["S", "M"]);
            team.OwnEstimation(owned);

            team.TakeEstimationAsSent(null, EstimationBundle.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.EstimationSource, Is.EqualTo(SettingSource.Own));
                Assert.That(team.EffectiveEstimation, Is.EqualTo(owned));
            }
        }

        [Test]
        public void Following_the_default_empties_the_Teams_own_bundle()
        {
            var team = AFollowingTeam();
            team.OwnEstimation(new EstimationBundle(TShirtSize, Sizes, true, ["S", "M"]));

            team.FollowEstimationDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.EffectiveEstimation, Is.EqualTo(TheDefault));
                Assert.That(team.OwnEstimationAdditionalFieldDefinitionId, Is.Null);
                Assert.That(team.OwnEstimationUnit, Is.Null);
                Assert.That(team.OwnUseNonNumericEstimation, Is.False);
                Assert.That(team.OwnEstimationCategoryValues, Is.Empty);
            }
        }

        [TestCase(Points, "Story Points, Points")]
        [TestCase(null, "Story Points")]
        [TestCase("", "Story Points")]
        public void The_default_is_named_by_its_field_and_its_unit(string? unit, string named)
        {
            var team = AFollowingTeam();
            team.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = StoryPoints, DisplayName = "Story Points", Reference = "customfield_10016" });
            team.WorkTrackingSystemConnection.EstimationForTeams = new EstimationBundle(StoryPoints, unit, false, []);

            Assert.That(team.EstimationDefaultName, Is.EqualTo(named));
        }

        [Test]
        public void Equal_bundles_hash_alike()
        {
            Assert.That(new EstimationBundle(StoryPoints, Points, true, ["S", "M", "L"]).GetHashCode(), Is.EqualTo(TheDefault.GetHashCode()));
        }

        [TestCaseSource(nameof(OnePartDiffers))]
        public void A_bundle_differing_in_one_part_hashes_apart(EstimationBundle other)
        {
            Assert.That(other.GetHashCode(), Is.Not.EqualTo(TheDefault.GetHashCode()));
        }

        [Test]
        public void A_following_Portfolio_reads_the_Portfolios_default_and_not_the_Teams()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Jira DC – Payments", WorkTrackingSystem = WorkTrackingSystems.Jira };
            connection.EstimationForTeams = new EstimationBundle(TShirtSize, Sizes, false, []);
            connection.EstimationForPortfolios = TheDefault;
            var portfolio = new Portfolio { Name = "Card Issuing", WorkTrackingSystemConnection = connection };

            Assert.That(portfolio.EffectiveEstimation, Is.EqualTo(TheDefault));
        }

        private static Team AFollowingTeam()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Jira DC – Payments", WorkTrackingSystem = WorkTrackingSystems.Jira };
            connection.EstimationForTeams = TheDefault;
            return new Team { Name = "Fraud Signals", WorkTrackingSystemConnection = connection };
        }
    }
}
