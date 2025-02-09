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
            var xIsNumeric = int.TryParse(x.Order, out int xNum);
            var yIsNumeric = int.TryParse(y.Order, out int yNum);

            if (xIsNumeric && yIsNumeric)
            {
                // Both are numbers, compare them numerically
                return xNum.CompareTo(yNum);
            }
            else if (xIsNumeric)
            {
                // x is a number, it should come first
                return -1;
            }
            else if (yIsNumeric)
            {
                // y is a number, it should come first
                return 1;
            }
            else
            {
                // Both are strings, compare them alphabetically
                return string.Compare(x.Order, y.Order);
            }
        }
    }
}
