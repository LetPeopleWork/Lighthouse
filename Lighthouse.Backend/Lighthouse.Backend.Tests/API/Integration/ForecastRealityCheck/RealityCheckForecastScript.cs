using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>How far one scripted check held: from none of its four levels up to all four.</summary>
    public enum HeldUpTo
    {
        NoLevel,
        NinetyFive,
        EightyFive,
        Seventy,
        EveryLevel,
    }

    /// <summary>
    /// Which forecast each check produces, chosen by the scenario. A check is recognised by the length
    /// of the history it was handed - exactly its sampling window - and by the number of days it forecast -
    /// its horizon. Nothing else about how the check asks is assumed. Every history length handed over is
    /// kept, the shipped engine's included, so a scenario can tell a check that read a day too many.
    /// </summary>
    public sealed class RealityCheckForecastScript
    {
        /// <summary>The sampling windows every Team is checked against, whatever its own setting is.</summary>
        internal static readonly int[] StandardWindowDays = [14, 30, 60, 90];

        /// <summary>One, two, four and eight weeks, each ending today.</summary>
        internal static readonly int[] HorizonDays = [7, 14, 28, 56];

        /// <summary>
        /// The day the instance believes it is, for every scenario. Arbitrary; that it never moves is the
        /// point, since every check this feature makes is measured back from it.
        /// </summary>
        internal static readonly DateTimeOffset Today = new(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

        internal static DateOnly TodayDay => DateOnly.FromDateTime(Today.UtcDateTime);

        /// <summary>
        /// The forecast value a level is given when the scenario wants it NOT to hold. No scenario seeds
        /// anywhere near this many finished Work Items in one period, so the Team can never reach it.
        /// </summary>
        private const int MoreThanAnyTeamHereDelivers = 500;

        private readonly Dictionary<(int Window, int Horizon), HeldUpTo?> checks = [];

        private readonly SortedSet<int> windows = [.. StandardWindowDays];

        private readonly List<int> historyLengthsHandedOver = [];

        private readonly Lock handingOver = new();

        private bool shippedEngine;

        /// <summary>How every check the scenario did not script behaves: the 95% and 85% levels hold.</summary>
        public HeldUpTo Otherwise { get; set; } = HeldUpTo.EightyFive;

        public List<int> HistoriesNoSweptWindowIsAsLongAs
        {
            get
            {
                lock (handingOver)
                {
                    return [.. historyLengthsHandedOver.Where(length => !windows.Contains(length))];
                }
            }
        }

        public void Reset()
        {
            lock (handingOver)
            {
                checks.Clear();
                windows.Clear();
                windows.UnionWith(StandardWindowDays);
                historyLengthsHandedOver.Clear();
            }

            shippedEngine = false;
            Otherwise = HeldUpTo.EightyFive;
        }

        public void AlsoSweeps(int samplingWindowDays)
        {
            if (samplingWindowDays > 0)
            {
                windows.Add(samplingWindowDays);
            }
        }

        public void Check(int samplingWindowDays, int horizonDays, HeldUpTo held) => checks[(samplingWindowDays, horizonDays)] = held;

        public void EveryCheckOf(int samplingWindowDays, HeldUpTo held)
        {
            foreach (var horizon in HorizonDays)
            {
                Check(samplingWindowDays, horizon, held);
            }
        }

        /// <summary>The engine comes back with nothing it can read a level from, for this check only.</summary>
        public void CannotBeWorkedOutFor(int samplingWindowDays, int horizonDays) => checks[(samplingWindowDays, horizonDays)] = null;

        public void RunAsShipped() => shippedEngine = true;

        /// <summary>
        /// The shipped forecast engine with one change: a check the scenario scripted gets the scripted
        /// forecast instead of a simulated one. Every other forecast the application makes runs as shipped.
        /// </summary>
        public IForecastService Over(IForecastService shipped) => new ForecastWithScriptedChecks(shipped, this);

        private HowManyForecast? ForecastFor(RunChartData history, int days)
        {
            lock (handingOver)
            {
                historyLengthsHandedOver.Add(history.History);
            }

            if (shippedEngine)
            {
                return null;
            }

            var horizon = HorizonDays.MinBy(candidate => Math.Abs(candidate - days));

            if (!checks.TryGetValue((history.History, horizon), out var held))
            {
                held = Otherwise;
            }

            return held is null
                ? new HowManyForecast([], days, DateTime.UtcNow)
                : AForecastWhere(held.Value, days);
        }

        /// <summary>
        /// A simulation whose four readings are exactly the values this check was scripted to have. A
        /// level meant to hold is read at nothing, which any Team reaches; a level meant not to hold is
        /// read at more than any Team here ever delivers. Readings stay ordered from the cautious 95%
        /// up to the optimistic 50%, as the engine's own are.
        /// </summary>
        private static HowManyForecast AForecastWhere(HeldUpTo held, int days)
        {
            var at95 = held >= HeldUpTo.NinetyFive ? 0 : MoreThanAnyTeamHereDelivers;
            var at85 = held >= HeldUpTo.EightyFive ? 0 : MoreThanAnyTeamHereDelivers + 1;
            var at70 = held >= HeldUpTo.Seventy ? 0 : MoreThanAnyTeamHereDelivers + 2;
            var at50 = held >= HeldUpTo.EveryLevel ? 0 : MoreThanAnyTeamHereDelivers + 3;

            var trials = new Dictionary<int, int>();
            AddTrials(trials, at50, 50);
            AddTrials(trials, at70, 20);
            AddTrials(trials, at85, 15);
            AddTrials(trials, at95, 10);
            AddTrials(trials, Math.Max(0, at95 - 1), 5);

            return new HowManyForecast(trials, days, DateTime.UtcNow);
        }

        private static void AddTrials(Dictionary<int, int> trials, int itemsCompleted, int count)
            => trials[itemsCompleted] = trials.GetValueOrDefault(itemsCompleted) + count;

        private sealed class ForecastWithScriptedChecks(IForecastService shipped, RealityCheckForecastScript script) : IForecastService
        {
            public Task<ForecastRunOutcome> UpdateForecastsForPortfolio(Portfolio portfolio, DateOnly instanceDay, CancellationToken cancellationToken = default)
                => shipped.UpdateForecastsForPortfolio(portfolio, instanceDay, cancellationToken);

            public HowManyForecast HowMany(RunChartData throughput, int days)
                => script.ForecastFor(throughput, days) ?? shipped.HowMany(throughput, days);

            public HowManyForecast PredictWorkItemCreation(Team team, string[] workItemTypes, DateTime startDate, DateTime endDate, int daysToForecast)
                => shipped.PredictWorkItemCreation(team, workItemTypes, startDate, endDate, daysToForecast);

            public Task<WhenForecast> When(Team team, int remainingItems, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
                => shipped.When(team, remainingItems, mode);
        }
    }
}
