using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    public class FeatureComparerTests
    {
        [Test]
        public void Compare_WhenBothOrdersAreNumbers_ShouldCompareNumerically()
        {
            // Arrange
            var comparer = new FeatureComparer();
            var feature1 = new Feature { Order = "10" };
            var feature2 = new Feature { Order = "2" };

            // Act
            int result = comparer.Compare(feature1, feature2);

            // Assert
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void Compare_WhenOneOrderIsNumber_ShouldPutNumberFirst()
        {
            // Arrange
            var comparer = new FeatureComparer();
            var feature1 = new Feature { Order = "10" };
            var feature2 = new Feature { Order = "apple" };

            // Act
            int result = comparer.Compare(feature1, feature2);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void Compare_WhenBothOrdersAreNotNumbers_ShouldCompareAlphabetically()
        {
            // Arrange
            var comparer = new FeatureComparer();
            var feature1 = new Feature { Order = "apple" };
            var feature2 = new Feature { Order = "banana" };

            // Act
            int result = comparer.Compare(feature1, feature2);

            // Assert
            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void Compare_WhenOrdersAreEqual_ShouldReturnZero()
        {
            // Arrange
            var comparer = new FeatureComparer();
            var feature1 = new Feature { Order = "10" };
            var feature2 = new Feature { Order = "10" };

            // Act
            int result = comparer.Compare(feature1, feature2);

            // Assert
            Assert.That(result, Is.Zero);
        }

        [Test]
        [TestCase("10.5", "2.3", -1)]
        [TestCase("2.3", "10.5", 1)]
        [TestCase("-10.5", "-2.3", 1)]
        [TestCase("-2.3", "-10.5", -1)]
        [TestCase("2.3", "2.3", 0)]
        public void Compare_BothOrdersAreDouble_ReturnsCorrectComparison(double orderOne, double orderTwo, int expectedResult)
        {
            var comparer = new FeatureComparer();
            var feature1 = new Feature { Order = $"{orderOne}" };
            var feature2 = new Feature { Order = $"{orderTwo}" };

            var result = comparer.Compare(feature1, feature2);
            
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        // IComparer<T> declares both operands nullable and the contract is total, so a null element must
        // sort rather than throw. Nulls sort first, matching Comparer<T>.Default.
        [Test]
        public void Compare_BothFeaturesAreNull_TreatsThemAsEqual()
        {
            var comparer = new FeatureComparer();

            var result = comparer.Compare(null, null);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Compare_LeftFeatureIsNull_SortsItFirst()
        {
            var comparer = new FeatureComparer();
            var feature = new Feature { Order = "10" };

            var result = comparer.Compare(null, feature);

            Assert.That(result, Is.LessThan(0));
        }

        [Test]
        public void Compare_RightFeatureIsNull_SortsItFirst()
        {
            var comparer = new FeatureComparer();
            var feature = new Feature { Order = "10" };

            var result = comparer.Compare(feature, null);

            Assert.That(result, Is.GreaterThan(0));
        }

        [Test]
        public void Sorting_ACollectionContainingANull_DoesNotThrow()
        {
            var features = new List<Feature> { new() { Order = "10" }, null!, new() { Order = "2" } };

            Assert.That(() => features.OrderBy(f => f, new FeatureComparer()).ToList(), Throws.Nothing);
        }
    }
}
