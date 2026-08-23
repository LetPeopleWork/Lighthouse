namespace Lighthouse.Backend.Models.Dependencies
{
    /// <summary>
    /// What a forecast has to wait for, as the answer a single decision already gave. It carries reference
    /// ids and nothing else - no reason, no licence, no order - because a simulation that could read any of
    /// those could act on one of them, and then two things in this product would be deciding what a
    /// dependency does.
    /// </summary>
    public sealed class ForecastWaits
    {
        private static readonly string[] NothingToWaitFor = [];

        private readonly Dictionary<string, string[]> whatMustFinishFirst;

        private ForecastWaits(Dictionary<string, string[]> whatMustFinishFirst)
        {
            this.whatMustFinishFirst = whatMustFinishFirst;
        }

        /// <summary>Nothing waits on anything, which is what every forecast did before this existed.</summary>
        public static ForecastWaits Nothing { get; } = new([]);

        public static ForecastWaits From(HonouredDependencies decided)
        {
            var byDependent = decided.Verdicts
                .Where(verdict => verdict.IsHonoured)
                .GroupBy(verdict => verdict.DependentReferenceId, StringComparer.Ordinal)
                .ToDictionary(
                    byReferenceId => byReferenceId.Key,
                    byReferenceId => byReferenceId
                        .Select(verdict => verdict.BlockerReferenceId)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    StringComparer.Ordinal);

            return new ForecastWaits(byDependent);
        }

        public bool NobodyWaitsForAnything => whatMustFinishFirst.Count == 0;

        public IReadOnlyList<string> Of(string featureReferenceId)
            => whatMustFinishFirst.GetValueOrDefault(featureReferenceId, NothingToWaitFor);
    }
}
