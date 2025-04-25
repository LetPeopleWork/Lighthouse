﻿using Lighthouse.Backend.Models;

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
            Assert.That(result, Is.EqualTo(0));
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
    }
}
