using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// The same number wherever it is asked. A Team drawing zero takes the first day of its measured history
    /// and works on the Feature nearest the top of its order, every day of every run, which makes a forecast
    /// arithmetic: a Team delivering one a day finishes forty items on day forty. Fixtures that are about
    /// something other than randomness use it so the number they assert is one that can be worked out by
    /// hand.
    /// </summary>
    public sealed class DrawsTheSameNumberEveryTime(int number = 0) : IDrawStreamFactory, IDrawStream
    {
        public long StartingNumber => number;

        public IDrawStream ForOneRun() => this;

        public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
            => Math.Min(number, Math.Max(maxExclusive - 1, 0));
    }
}
