namespace Lighthouse.Backend.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsItemInList(this IEnumerable<string> enumerable, string? item)
        {
            if (string.IsNullOrEmpty(item))
            {
                return false;
            }

            return enumerable.Any(s => string.Equals(s, item, StringComparison.OrdinalIgnoreCase));
        }
    }
}
