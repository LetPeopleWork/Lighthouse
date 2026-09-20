using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// When work on a Feature begins, as a client reads it. The source is always stated: a screen that
    /// guessed it from an empty percentile list would be re-deciding the domain's rule, and would have no
    /// way to tell "already started" from "cannot be forecast".
    /// </summary>
    public class FeatureStartDto
    {
        public FeatureStartDto()
        {
        }

        public FeatureStartDto(FeatureStart start, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods, params int[] probabilities)
        {
            Source = start.Source.ToString();
            ObservedDate = start.ObservedDate;

            if (start.Forecast is { } forecast)
            {
                Percentiles.AddRange(forecast.CreateForecastDtos(today, blackoutPeriods, probabilities));
            }
        }

        public string Source { get; set; } = nameof(StartDateSource.Unknown);

        /// <summary>Set only when the source is Observed. A date here is a fact, not a percentile.</summary>
        public DateTime? ObservedDate { get; set; }

        /// <summary>Filled only when the source is Forecast. Empty otherwise, deliberately.</summary>
        public List<WhenForecastDto> Percentiles { get; } = [];
    }

    /// <summary>
    /// One contributing team's share of a Feature, at both ends. The table shows a Feature's own
    /// forecast; this is what a timeline needs to draw a lane per team, and what answers "which team is
    /// the late one" - a question the API could not answer at all before this, because the per-team
    /// completion forecasts existed in the domain and were dropped at this boundary.
    /// </summary>
    public class FeatureTeamForecastDto
    {
        public FeatureTeamForecastDto()
        {
        }

        public FeatureTeamForecastDto(
            int teamId,
            StartForecast? start,
            WhenForecast? completion,
            DateOnly today,
            IReadOnlyList<BlackoutPeriod> blackoutPeriods,
            params int[] probabilities)
        {
            TeamId = teamId;

            if (start is not null)
            {
                StartPercentiles.AddRange(start.CreateForecastDtos(today, blackoutPeriods, probabilities));
            }

            if (completion is not null)
            {
                CompletionPercentiles.AddRange(completion.CreateForecastDtos(today, blackoutPeriods, probabilities));
            }
        }

        public int TeamId { get; set; }

        public List<WhenForecastDto> StartPercentiles { get; } = [];

        public List<WhenForecastDto> CompletionPercentiles { get; } = [];
    }
}
