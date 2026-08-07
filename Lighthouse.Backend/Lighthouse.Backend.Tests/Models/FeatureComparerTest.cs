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

            Assert.That(result, Is.Zero);
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

        // The int rung must win outright: falling through to the double rung would invert this pair,
        // because that rung ranks the lower number higher.
        [Test]
        public void Compare_WhenOnlyTheRightOrderIsAnInt_RanksTheIntAheadOfTheDecimal()
        {
            var comparer = new FeatureComparer();
            var decimalRanked = new Feature { Order = $"{9.5}" };
            var intRanked = new Feature { Order = "5" };

            var result = comparer.Compare(decimalRanked, intRanked);

            Assert.That(result, Is.GreaterThan(0));
        }

        // Mirrors the int rung: a rank the comparer can read as a number outranks one it cannot.
        [Test]
        public void Compare_WhenOnlyTheLeftOrderIsADouble_RanksTheDoubleAheadOfTheNonNumeric()
        {
            var comparer = new FeatureComparer();
            var doubleRanked = new Feature { Order = $"{9.5}" };
            var nonNumeric = new Feature { Order = "1abc" };

            var result = comparer.Compare(doubleRanked, nonNumeric);

            Assert.That(result, Is.LessThan(0));
        }

        // A multi-connector instance carries all three rank shapes at once — ADO integers, Linear
        // decimals and Jira LexoRanks. Without a rung of its own, a decimal compares against a
        // LexoRank as text, and the three-way relation stops being an order at all.
        [Test]
        public void Compare_AcrossEveryRankShape_IsTransitive()
        {
            var comparer = new FeatureComparer();
            var orders = new[] { "5", "-45661", $"{9.5}", $"{8.75}", "9-high", "0|i0007c:", "" };

            var features = orders.Select(order => new Feature { Order = order }).ToList();

            foreach (var first in features)
            {
                foreach (var second in features)
                {
                    foreach (var third in features)
                    {
                        if (comparer.Compare(first, second) >= 0 || comparer.Compare(second, third) >= 0)
                        {
                            continue;
                        }

                        Assert.That(
                            comparer.Compare(first, third),
                            Is.LessThan(0),
                            $"'{first.Order}' < '{second.Order}' < '{third.Order}', so '{first.Order}' must sort before '{third.Order}'");
                    }
                }
            }
        }

        // The forecast draws throughput from Features in Order sequence, so a comparer whose result
        // depends on the input sequence hands two callers two different forecasts over one backlog.
        [Test]
        public void Sorting_TheSameRanksInADifferentInputOrder_ProducesTheSameSequence()
        {
            var orders = new[] { "5", "-45661", $"{9.5}", $"{8.75}", "9-high", "0|i0007c:", "" };

            var sorted = orders
                .Select(order => new Feature { Order = order })
                .OrderBy(f => f, new FeatureComparer())
                .Select(f => f.Order)
                .ToList();

            var sortedFromReversedInput = orders
                .Reverse()
                .Select(order => new Feature { Order = order })
                .OrderBy(f => f, new FeatureComparer())
                .Select(f => f.Order)
                .ToList();

            Assert.That(sortedFromReversedInput, Is.EqualTo(sorted));
        }
    }
}
