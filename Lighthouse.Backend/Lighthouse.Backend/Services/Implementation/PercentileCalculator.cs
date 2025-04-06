namespace Lighthouse.Backend.Services.Implementation
{
    public static class PercentileCalculator
    {
        public static int CalculatePercentile(List<int> items, int percentile)
        {
            items.Sort();
            var index = (int)Math.Floor(percentile / 100.0 * items.Count) - 1;

            index = Math.Min(Math.Max(index, 0), items.Count - 1);

            return items[index];
        }
    }
}
