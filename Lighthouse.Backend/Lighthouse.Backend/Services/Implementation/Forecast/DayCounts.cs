namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// How often something happened on each simulated day, one count per row or per Feature.
    ///
    /// The shape every recorder in a forecast writes into and every fold reads back out of. Kept in one
    /// place because a worker's own share and the total it is added into have to be built the same way:
    /// a total one entry short throws on the first worker that recorded something there, and one entry
    /// long is a row nothing will ever write to.
    /// </summary>
    internal static class DayCounts
    {
        public static Dictionary<int, int>[] EmptyPer(int howMany)
        {
            var counts = new Dictionary<int, int>[howMany];

            for (var index = 0; index < howMany; index++)
            {
                counts[index] = [];
            }

            return counts;
        }
    }
}
