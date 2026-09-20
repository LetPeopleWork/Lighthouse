using System.Collections.Frozen;

namespace Lighthouse.Backend.Models.WriteBack
{
    /// <summary>
    /// Which sources answer for which end of a Feature.
    ///
    /// Written here, beside the enum, because more than one place needs to know and they do not all need
    /// to know it for the same reason - the resolver asks which pass a mapping belongs to, the validator
    /// asks whether a mapping writes a date. Those are two questions with the same answer today and no
    /// guarantee of the same answer later, so each caller still composes and names the set it means. What
    /// is shared is only the membership, which is one fact and was starting to be written twice.
    /// </summary>
    public static class WriteBackValueSources
    {
        // Frozen rather than a HashSet behind an interface: these are process-wide and a caller holding the
        // interface can cast back to the concrete set and add to it, which would edit routing for everyone.
        public static readonly FrozenSet<WriteBackValueSource> Completion = new[]
        {
            WriteBackValueSource.ForecastPercentile50,
            WriteBackValueSource.ForecastPercentile70,
            WriteBackValueSource.ForecastPercentile85,
            WriteBackValueSource.ForecastPercentile95,
        }.ToFrozenSet();

        public static readonly FrozenSet<WriteBackValueSource> Start = new[]
        {
            WriteBackValueSource.ForecastedStartPercentile50,
            WriteBackValueSource.ForecastedStartPercentile70,
            WriteBackValueSource.ForecastedStartPercentile85,
            WriteBackValueSource.ForecastedStartPercentile95,
        }.ToFrozenSet();
    }
}
