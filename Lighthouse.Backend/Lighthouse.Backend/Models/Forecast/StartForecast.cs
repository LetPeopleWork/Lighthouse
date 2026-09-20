namespace Lighthouse.Backend.Models.Forecast
{
    /// <summary>
    /// How often the simulation began work on a Feature on each day, counted across every run. One of
    /// these per contributing Team, and one for the Feature itself.
    ///
    /// It is a sibling of <see cref="WhenForecast"/> rather than another of them, and that is the whole
    /// point. <see cref="Feature.Forecast"/> aggregates every entry of <see cref="Feature.Forecasts"/>
    /// without asking what it is, so a start distribution landing in that collection would be folded
    /// into the completion forecast on every Feature, silently, on the most load-bearing number the
    /// product emits. A separate type cannot land there at all: the collection is typed, and no filter
    /// has to stay correct for that to keep being true.
    ///
    /// Both share <see cref="ForecastBase"/>, which is where the percentile reads and the day histogram
    /// live. Nothing on <see cref="WhenForecast"/> is inherited - including NumberOfItems, which has no
    /// meaning for a start.
    /// </summary>
    public class StartForecast : ForecastBase
    {
        // The day a Feature starts reads the same way a completion day does: earliest first, so the
        // percentile is "by this day". Held once rather than built per row, because a forecast makes one
        // of these per Team per Feature.
        private static readonly IComparer<int> EarliestDayFirst = Comparer<int>.Create((left, right) => left.CompareTo(right));

        public StartForecast() : base(EarliestDayFirst)
        {
        }

        public StartForecast(Dictionary<int, int> daysWorkBeganOn) : base(daysWorkBeganOn, EarliestDayFirst)
        {
        }

        /// <param name="team">
        /// Null for the Feature's own start. ADR-111's shape: no single Team owns the Feature-grain
        /// value, and null is the honest answer rather than a sentinel Team.
        /// </param>
        public StartForecast(Dictionary<int, int> daysWorkBeganOn, Team? team) : this(daysWorkBeganOn)
        {
            TeamId = team?.Id;
            Team = team;
        }

        public int FeatureId { get; set; }

        public Feature Feature { get; set; }

        public int? TeamId { get; set; }

        public Team? Team { get; set; }
    }
}
