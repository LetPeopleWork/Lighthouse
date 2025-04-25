namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Needed because the order might be an int (in Azure DevOps) or an alphanumeric value (in Jira). To handle both cases, a special comparer is needed.
    /// </summary>
    public class FeatureComparer : IComparer<Feature>
    {
        public int Compare(Feature? x, Feature? y)
        {
            // Convert order strings to integers for comparison
            var xIsInt = int.TryParse(x.Order, out int xNum);
            var yIsInt = int.TryParse(y.Order, out int yNum);

            if (xIsInt && yIsInt)
            {
                // Both are numbers, compare them numerically
                return xNum.CompareTo(yNum);
            }
            else if (xIsInt)
            {
                // x is a number, it should come first
                return -1;
            }
            else if (yIsInt)
            {
                // y is a number, it should come first
                return 1;
            }
            else
            {
                var xIsDouble = double.TryParse(x.Order, out double xDouble);
                var yIsDouble = double.TryParse(y.Order, out double yDouble);

                if (xIsDouble && yIsDouble)
                {
                    // Linear is using double values, but the lower the number, the higher the index.
                    return xDouble.CompareTo(yDouble) * -1;
                }

                // Both are strings, compare them alphabetically
                return string.Compare(x.Order, y.Order);
            }
        }
    }
}
