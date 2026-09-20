namespace Lighthouse.Backend.Models.Forecast
{
    public enum StartDateSource
    {
        /// <summary>Nothing can be said. A contributing team has no measured delivery to forecast from.</summary>
        Unknown,

        /// <summary>The simulation's answer, read at whichever percentile the caller asks for.</summary>
        Forecast,

        /// <summary>The day work actually began. Not a forecast, and not to be presented as one.</summary>
        Observed,
    }

    /// <summary>
    /// When work on a Feature begins, and where that answer came from.
    ///
    /// Provenance is carried rather than inferred. A reader that worked it out from the absence of
    /// percentiles would be deciding, separately in every client, a question this already answers - and
    /// "no percentiles because the work has started" and "no percentiles because nothing can be
    /// forecast" are opposite situations that look identical from the outside.
    ///
    /// It lives on the Feature rather than on the DTO because the DTO is not the only reader: the
    /// write-back resolver goes to the domain directly. Two implementations of one verdict is how a
    /// Jira plan ends up starting on a forecast date while the table beside it shows the real one.
    /// </summary>
    public sealed record FeatureStart(StartDateSource Source, DateTime? ObservedDate, StartForecast? Forecast)
    {
        public static FeatureStart NotKnown { get; } = new(StartDateSource.Unknown, null, null);

        public static FeatureStart On(DateTime observedDate) => new(StartDateSource.Observed, observedDate, null);

        public static FeatureStart ExpectedFrom(StartForecast forecast) => new(StartDateSource.Forecast, null, forecast);
    }
}
