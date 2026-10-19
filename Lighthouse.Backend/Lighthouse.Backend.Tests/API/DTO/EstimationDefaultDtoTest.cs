using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6313")]
    public class EstimationDefaultDtoTest
    {
        private const int StoryPoints = 11;

        private const string Sizes = "Sizes";

        private static readonly string[] SmallMediumLarge = ["S", "M", "L"];

        private static readonly List<string> SmallMedium = ["S", "M"];

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_connection_without_an_Estimation_default_sends_none()
        {
            Assert.That(EstimationDefaultDto.From(EstimationBundle.None), Is.Null);
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_connections_Estimation_default_is_sent_with_all_four_parts()
        {
            var sent = EstimationDefaultDto.From(new EstimationBundle(StoryPoints, Sizes, true, SmallMediumLarge));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sent?.AdditionalFieldDefinitionId, Is.EqualTo(StoryPoints));
                Assert.That(sent?.Unit, Is.EqualTo(Sizes));
                Assert.That(sent?.UseNonNumericEstimation, Is.True);
                Assert.That(sent?.EstimationCategoryValues, Is.EqualTo(SmallMediumLarge));
            }
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_saved_bundle_is_read_with_all_four_parts()
        {
            var saved = new EstimationDefaultDto { AdditionalFieldDefinitionId = StoryPoints, Unit = Sizes, UseNonNumericEstimation = true, EstimationCategoryValues = SmallMedium };

            Assert.That(saved.ToBundle(), Is.EqualTo(new EstimationBundle(StoryPoints, Sizes, true, SmallMedium)));
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_saved_bundle_that_leaves_out_the_switch_and_the_categories_is_numeric_without_categories()
        {
            var saved = new EstimationDefaultDto { AdditionalFieldDefinitionId = StoryPoints, Unit = "Points" };

            Assert.That(saved.ToBundle(), Is.EqualTo(new EstimationBundle(StoryPoints, "Points", false, [])));
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_saved_bundle_without_a_field_is_none_whatever_else_it_says()
        {
            var saved = new EstimationDefaultDto { Unit = Sizes, UseNonNumericEstimation = true, EstimationCategoryValues = ["S"] };

            Assert.That(saved.ToBundle(), Is.SameAs(EstimationBundle.None));
        }
    }
}
