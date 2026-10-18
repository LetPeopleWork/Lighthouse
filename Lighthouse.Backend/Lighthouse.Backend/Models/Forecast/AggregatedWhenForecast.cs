namespace Lighthouse.Backend.Models.Forecast
{
    public class AggregatedWhenForecast : WhenForecast
    {
        public AggregatedWhenForecast()
        {
        }

        public AggregatedWhenForecast(IEnumerable<WhenForecast> forecasts) : base()
        {
            var materialized = forecasts.ToList();

            if (materialized.Count > 0)
            {
                var joint = JointCompletionDistribution.Combine(materialized.Select(EveryRunOf));

                if (joint.Count > 0)
                {
                    // The runs nobody finished are counted, but have no day a reader could be shown.
                    var everyRun = joint.Values.Sum();
                    joint.Remove(NeverFinished);
                    SetSimulationResult(joint);
                    TotalTrials = everyRun;
                }
                else
                {
                    // No contributor carries trials: keep the sentinel days so the percentiles still read
                    // as done today. Team/TeamId stay null - the aggregate has no team.
                    SetSimulationResult(DaysWithoutTrials(materialized));
                }

                NumberOfItems = materialized.Sum(f => f.NumberOfItems);
                CreationTime = materialized.Min(f => f.CreationTime);
            }

            FilterApplied = materialized.Any(f => f.FilterApplied);
            HasSufficientData = materialized.Count == 0 || materialized.All(f => f.HasSufficientData);
            var summaries = materialized
                .Where(f => !string.IsNullOrWhiteSpace(f.ExcludedSummary))
                .Select(f => f.ExcludedSummary!)
                .Distinct()
                .ToList();
            ExcludedSummary = summaries.Count == 0 ? null : string.Join("; ", summaries);
        }

        private static Dictionary<int, int> DaysWithoutTrials(IEnumerable<WhenForecast> forecasts)
        {
            return forecasts
                .SelectMany(f => f.SimulationResult.Keys)
                .Distinct()
                .ToDictionary(day => day, _ => 0);
        }
    }
}
