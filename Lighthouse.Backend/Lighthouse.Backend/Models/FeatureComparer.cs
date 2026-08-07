namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Needed because the order might be an int (in Azure DevOps) or an alphanumeric value (in Jira). To handle both cases, a special comparer is needed.
    /// </summary>
    public class FeatureComparer : IComparer<Feature>
    {
        public int Compare(Feature? x, Feature? y)
        {
            // IComparer<T>'s contract is total over nulls; nulls sort first, as Comparer<T>.Default does.
            if (x is null)
            {
                return y is null ? 0 : -1;
            }

            if (y is null)
            {
                return 1;
            }

            return CompareOrderValues(x.Order, y.Order);
        }

        /// <summary>
        /// The same ladder without a <see cref="Feature"/>, so ADR-135's position map orders its projection
        /// by this comparison rather than by a second copy of it.
        /// </summary>
        public static int CompareOrderValues(string x, string y)
        {
            var xIsInt = int.TryParse(x, out int xNum);
            var yIsInt = int.TryParse(y, out int yNum);

            if (xIsInt && yIsInt)
            {
                return xNum.CompareTo(yNum);
            }

            if (xIsInt)
            {
                return -1;
            }

            if (yIsInt)
            {
                return 1;
            }

            var xIsDouble = double.TryParse(x, out double xDouble);
            var yIsDouble = double.TryParse(y, out double yDouble);

            if (xIsDouble && yIsDouble)
            {
                // Linear ranks with doubles, and the lower the number the higher the place - hence the inversion.
                return xDouble.CompareTo(yDouble) * -1;
            }

            // A decimal needs a rung of its own for the same reason an int does: comparing it against a
            // LexoRank as text makes the relation intransitive, and the whole ladder stops being an order.
            if (xIsDouble)
            {
                return -1;
            }

            if (yIsDouble)
            {
                return 1;
            }

            return string.Compare(x, y, StringComparison.CurrentCulture);
        }
    }
}
