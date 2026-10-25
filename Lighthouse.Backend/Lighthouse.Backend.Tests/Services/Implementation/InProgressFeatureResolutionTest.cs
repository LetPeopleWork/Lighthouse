using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-03")]
    public class InProgressFeatureResolutionTest
    {
        private const string NotTracked = " (Item not tracked by Lighthouse)";

        private static readonly string?[] SearchThenCheckout = ["SEARCH-1", "CHECKOUT-1"];
        private static readonly string?[] CheckoutAndAnIdNobodyHolds = ["CHECKOUT-1", "GONE-1"];
        private static readonly string?[] NothingAndAnEmptyReference = [null, string.Empty];
        private static readonly string?[] AMixOfRepeatedAndEmptyReferences = ["CHECKOUT-1", string.Empty, "CHECKOUT-1", "SEARCH-1", null];
        private static readonly string?[] OnlyCheckout = ["CHECKOUT-1"];
        private static readonly string?[] NoReferences = [];
        private static readonly string?[] CheckoutThenSearchThenNothing = ["CHECKOUT-1", "SEARCH-1", null];

        private static readonly string[] SearchThenCheckoutNames = ["Search", "Checkout"];
        private static readonly string[] CheckoutThenSearchIds = ["CHECKOUT-1", "SEARCH-1"];
        private static readonly string[] FirstCopyOfCheckout = ["Checkout, first"];
        private static readonly string[] TwoUnknowns = ["Unknown" + NotTracked, "Unknown" + NotTracked];

        private Mock<IFeatureRepository> repository = null!;

        [SetUp]
        public void SetUp()
        {
            repository = new Mock<IFeatureRepository>();
            repository.Holds([AFeature("CHECKOUT-1", "Checkout"), AFeature("SEARCH-1", "Search")]);
        }

        [Test]
        public void Each_reference_is_answered_in_the_order_it_was_first_met()
        {
            var resolved = Resolve(SearchThenCheckout);

            Assert.That(resolved.Select(feature => feature.Name), Is.EqualTo(SearchThenCheckoutNames));
        }

        [Test]
        public void A_reference_nothing_holds_is_answered_by_a_placeholder_saying_so()
        {
            var resolved = Resolve(CheckoutAndAnIdNobodyHolds);

            var placeholder = resolved[1];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved[0].Name, Is.EqualTo("Checkout"));
                Assert.That(placeholder.Id, Is.EqualTo(-1));
                Assert.That(placeholder.ReferenceId, Is.EqualTo("GONE-1"));
                Assert.That(placeholder.Name, Is.EqualTo("GONE-1" + NotTracked));
                Assert.That(placeholder.StateCategory, Is.EqualTo(StateCategories.Unknown));
                Assert.That(placeholder.Type, Is.Empty);
                Assert.That(placeholder.State, Is.Empty);
                Assert.That(placeholder.Url, Is.Empty);
                Assert.That(placeholder.Order, Is.Empty);
            }
        }

        [Test]
        public void Work_without_a_parent_is_answered_by_an_Unknown_placeholder_each()
        {
            var resolved = Resolve(NothingAndAnEmptyReference);

            Assert.That(resolved.Select(feature => feature.Name), Is.EqualTo(TwoUnknowns));
        }

        [Test]
        public void One_read_asks_for_each_reference_once_and_never_for_an_empty_one()
        {
            Resolve(AMixOfRepeatedAndEmptyReferences);

            repository.Verify(r => r.FeaturesWithReferenceIds(It.Is<IReadOnlyCollection<string>>(asked => asked.SequenceEqual(CheckoutThenSearchIds))), Times.Once);
            repository.Verify(r => r.FeaturesWithReferenceIds(It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
        }

        [Test]
        public void A_reference_stored_twice_answers_with_the_Feature_the_read_gave_first()
        {
            repository.Holds([AFeature("CHECKOUT-1", "Checkout, first"), AFeature("CHECKOUT-1", "Checkout, second")]);

            var resolved = Resolve(OnlyCheckout);

            Assert.That(resolved.Select(feature => feature.Name), Is.EqualTo(FirstCopyOfCheckout));
        }

        [Test]
        public void No_references_answer_nothing()
        {
            Assert.That(Resolve(NoReferences), Is.Empty);
        }

        [Test]
        public void The_references_of_work_in_progress_are_its_parents_each_once_in_the_order_met()
        {
            var work = new[] { AnItemOf("CHECKOUT-1"), AnItemOf("SEARCH-1"), AnItemOf("CHECKOUT-1"), AnItemOf(null) };

            var references = InProgressFeatureResolution.ReferencesOf(work);

            Assert.That(references, Is.EqualTo(CheckoutThenSearchThenNothing));
        }

        private IReadOnlyList<Feature> Resolve(IReadOnlyList<string?> references)
            => new InProgressFeatureResolution(repository.Object).Resolve(references);

        private static Feature AFeature(string referenceId, string name)
            => new() { ReferenceId = referenceId, Name = name };

        private static WorkItem AnItemOf(string? parentReferenceId)
            => new() { ParentReferenceId = parentReferenceId!, ReferenceId = Guid.NewGuid().ToString(), Name = "item" };
    }
}
