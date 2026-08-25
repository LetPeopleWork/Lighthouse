namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// One percentile of a Delivery's forecast, as the published block states it.
    /// </summary>
    public sealed record DeliveryForecastBlockPercentile(int Percentile, DateOnly ExpectedDate);

    /// <summary>
    /// The four things a published block has to carry: that Lighthouse wrote it, when it was written,
    /// the Delivery's forecasts, and the likelihood of hitting the target with the target named.
    /// Dropping any one of them is a failed feature rather than a trim - a reader who cannot tell where
    /// a date came from, or how old it is, has a number they cannot act on.
    ///
    /// Days rather than instants throughout. The block is read by a person planning around dates, and
    /// an instant would put a time of day on something that has none.
    /// </summary>
    public sealed record DeliveryForecastBlock(
        DateOnly WrittenOn,
        IReadOnlyList<DeliveryForecastBlockPercentile> Percentiles,
        DateOnly TargetDate,
        double LikelihoodPercentage);
}
