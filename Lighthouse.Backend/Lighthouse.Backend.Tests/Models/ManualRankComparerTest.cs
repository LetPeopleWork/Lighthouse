using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// The comparison this instance's own order is built on (INV-O1): places ascending, never-placed
    /// last, and total over nulls the way <see cref="Comparer{T}.Default"/> is - a collection carrying a
    /// null Feature must sort rather than throw, which is the bug the source-order ladder already had.
    /// </summary>
    public class ManualRankComparerTest
    {
        [Test]
        public void Compare_LowerPlaceFirst()
        {
            var comparer = new ManualRankComparer();

            var result = comparer.Compare(FeaturePlacedAt(1), FeaturePlacedAt(2));

            Assert.That(result, Is.LessThan(0));
        }

        [Test]
        public void Compare_TheSamePlaceTwice_TreatsThemAsEqual()
        {
            var comparer = new ManualRankComparer();

            var result = comparer.Compare(FeaturePlacedAt(7), FeaturePlacedAt(7));

            Assert.That(result, Is.Zero);
        }

        [Test]
        public void Compare_AFeatureNobodyPlaced_SortsItAfterEveryPlacedOne()
        {
            var comparer = new ManualRankComparer();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparer.Compare(FeaturePlacedAt(null), FeaturePlacedAt(1)), Is.GreaterThan(0));
                Assert.That(comparer.Compare(FeaturePlacedAt(1), FeaturePlacedAt(null)), Is.LessThan(0));
            }
        }

        [Test]
        public void Compare_TwoFeaturesNobodyPlaced_TreatsThemAsEqual()
        {
            var comparer = new ManualRankComparer();

            var result = comparer.Compare(FeaturePlacedAt(null), FeaturePlacedAt(null));

            Assert.That(result, Is.Zero);
        }

        [Test]
        public void Compare_ANullFeature_SortsItFirstAndStaysAntisymmetric()
        {
            var comparer = new ManualRankComparer();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(comparer.Compare(null, FeaturePlacedAt(1)), Is.LessThan(0));
                Assert.That(comparer.Compare(FeaturePlacedAt(1), null), Is.GreaterThan(0));
                Assert.That(comparer.Compare(null, null), Is.Zero);
            }
        }

        [Test]
        public void Sorting_ACollectionContainingANull_DoesNotThrow()
        {
            var features = new List<Feature> { FeaturePlacedAt(2), null!, FeaturePlacedAt(1) };

            Assert.That(() => features.OrderBy(f => f, new ManualRankComparer()).ToList(), Throws.Nothing);
        }

        [Test]
        public void CompareRanks_OrdersPlacesAscendingAndNeverPlacedLast()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ManualRankComparer.CompareRanks(1, 2), Is.LessThan(0));
                Assert.That(ManualRankComparer.CompareRanks(2, 1), Is.GreaterThan(0));
                Assert.That(ManualRankComparer.CompareRanks(3, 3), Is.Zero);
                Assert.That(ManualRankComparer.CompareRanks(null, 1), Is.GreaterThan(0));
                Assert.That(ManualRankComparer.CompareRanks(1, null), Is.LessThan(0));
                Assert.That(ManualRankComparer.CompareRanks(null, null), Is.Zero);
            }
        }

        private static Feature FeaturePlacedAt(int? place) => new() { ManualRank = place };
    }
}
