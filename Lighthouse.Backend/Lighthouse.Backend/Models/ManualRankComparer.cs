namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// This instance's own places, ascending, with the never-placed sorting last (INV-O1). Gaps,
    /// repeats and nulls are all legal - the sequence stays total, because <see cref="FeatureOrdering"/>
    /// always breaks the remaining ties on Id.
    /// </summary>
    public class ManualRankComparer : IComparer<Feature>
    {
        public int Compare(Feature? x, Feature? y)
        {
            if (x is null)
            {
                return y is null ? 0 : -1;
            }

            if (y is null)
            {
                return 1;
            }

            return CompareRanks(x.ManualRank, y.ManualRank);
        }

        /// <summary>
        /// The same comparison without a <see cref="Feature"/>, so ADR-135's position map numbers its
        /// projection by this rule rather than by a second copy of it.
        /// </summary>
        public static int CompareRanks(int? x, int? y)
        {
            if (x is null)
            {
                return y is null ? 0 : 1;
            }

            if (y is null)
            {
                return -1;
            }

            return x.Value.CompareTo(y.Value);
        }
    }
}
