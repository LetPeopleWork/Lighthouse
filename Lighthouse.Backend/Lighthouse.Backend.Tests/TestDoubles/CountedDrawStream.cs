using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// A draw source that watches the forecast drawing from it: how many simulated runs began, how many runs
    /// were drawing at the same moment, and how far into a run the furthest draw reached. It can also hold
    /// the forecast open part-way through, the way a slow real forecast is open, so a test can ask the
    /// instance something while it runs or stop it without timing anything.
    ///
    /// Every run of a Team draws either the first day of its history or the second, decided by the run's
    /// number: of every ten runs, <paramref name="runsInTenThatDrawTheFirstDay"/> take the first. A history
    /// of one item and then nothing therefore makes that share of the runs finish and the rest never move,
    /// which is how a test chooses the share of runs that finish.
    /// </summary>
    public sealed class CountedDrawStream(int runsInTenThatDrawTheFirstDay = 10) : IDrawStreamFactory, IDrawStream, IDisposable
    {
        private int runsInTen = runsInTenThatDrawTheFirstDay;

        private int heldNow;

        private int mostHeldAtOnce;

        private const int TheDrawForHowMuchTheTeamDelivers = 0;

        private static readonly TimeSpan TheLongestAnyRunIsHeld = TimeSpan.FromSeconds(30);

        private readonly ConcurrentDictionary<int, byte> runsBegun = new();

        private readonly ManualResetEventSlim mayCarryOn = new(initialState: true);

        private readonly ManualResetEventSlim aRunIsBeingHeld = new(initialState: false);

        private int holdEveryRunAfter = int.MaxValue;

        private int drawingNow;

        private int mostDrawingAtOnce;

        private int furthestDay;

        /// <summary>
        /// How long each draw takes. A draw is ordinarily far too quick for two workers to be caught in one
        /// at the same moment, so a test about how many run at once makes each draw take a little time.
        /// </summary>
        public TimeSpan EachDrawTakes { get; init; } = TimeSpan.Zero;

        public long StartingNumber => TheStartingNumber;

        private const long TheStartingNumber = 6311;

        /// <summary>Changed between forecasts by a scenario that needs a different share of runs to finish.</summary>
        public int RunsInTenThatDrawTheFirstDay
        {
            get => Volatile.Read(ref runsInTen);
            set => Volatile.Write(ref runsInTen, value);
        }

        /// <summary>The runs the latest forecast began. Counted afresh for each forecast.</summary>
        public int RunsBegun => runsBegun.Count;

        /// <summary>How many runs were parked at the same moment: each one is a worker the forecast was using.</summary>
        public int MostRunsHeldAtOnce => Volatile.Read(ref mostHeldAtOnce);

        public int MostDrawingAtOnce => Volatile.Read(ref mostDrawingAtOnce);

        public int FurthestDayDrawn => Volatile.Read(ref furthestDay);

        public IDrawStream ForOneRun()
        {
            runsBegun.Clear();
            return this;
        }

        /// <summary>
        /// Lets <paramref name="runs"/> runs begin, then parks every run that begins after them until
        /// <see cref="LetItCarryOn"/>. Each parked run is a worker the forecast cannot use.
        /// </summary>
        public void HoldOnceRunsHaveBegun(int runs)
        {
            holdEveryRunAfter = runs;
            mayCarryOn.Reset();
        }

        public bool IsHeldWithin(TimeSpan wait) => aRunIsBeingHeld.Wait(wait);

        /// <summary>
        /// Whether the given number of runs are parked at the same moment within the wait. While every run is
        /// held, each parked run is one worker, so this is how a test sees that the forecast really had that
        /// many workers at once, rather than merely never more than that.
        /// </summary>
        public bool IsHoldingWithin(int runs, TimeSpan wait)
            => SpinWait.SpinUntil(() => Volatile.Read(ref heldNow) >= runs, wait);

        public void LetItCarryOn() => mayCarryOn.Set();

        public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
        {
            if (runsBegun.TryAdd(trial, 0) && runsBegun.Count > Volatile.Read(ref holdEveryRunAfter))
            {
                RecordTheMostHeld(Interlocked.Increment(ref heldNow));
                aRunIsBeingHeld.Set();
                mayCarryOn.Wait(TheLongestAnyRunIsHeld);
                Interlocked.Decrement(ref heldNow);
            }

            RecordTheFurthest(day);

            var now = Interlocked.Increment(ref drawingNow);
            RecordTheMost(now);

            if (EachDrawTakes > TimeSpan.Zero)
            {
                Thread.Sleep(EachDrawTakes);
            }

            Interlocked.Decrement(ref drawingNow);

            var drawn = ordinal == TheDrawForHowMuchTheTeamDelivers && trial % 10 >= RunsInTenThatDrawTheFirstDay ? 1 : 0;

            return Math.Min(drawn, Math.Max(maxExclusive - 1, 0));
        }

        public void Dispose()
        {
            mayCarryOn.Set();
            mayCarryOn.Dispose();
            aRunIsBeingHeld.Dispose();
        }

        private void RecordTheMost(int now)
        {
            var seen = Volatile.Read(ref mostDrawingAtOnce);

            while (now > seen && Interlocked.CompareExchange(ref mostDrawingAtOnce, now, seen) != seen)
            {
                seen = Volatile.Read(ref mostDrawingAtOnce);
            }
        }

        private void RecordTheMostHeld(int now)
        {
            var seen = Volatile.Read(ref mostHeldAtOnce);

            while (now > seen && Interlocked.CompareExchange(ref mostHeldAtOnce, now, seen) != seen)
            {
                seen = Volatile.Read(ref mostHeldAtOnce);
            }
        }

        private void RecordTheFurthest(int day)
        {
            var seen = Volatile.Read(ref furthestDay);

            while (day > seen && Interlocked.CompareExchange(ref furthestDay, day, seen) != seen)
            {
                seen = Volatile.Read(ref furthestDay);
            }
        }
    }
}
