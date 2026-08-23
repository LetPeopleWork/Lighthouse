using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// A fresh random number for every draw, with no regard for where it was asked for. This is what the
    /// released product's forecast drew from, so it is what the new source has to be compared against to
    /// show that replacing the source left the dates where they were.
    ///
    /// Fixtures that only want real randomness rather than the old behaviour specifically use it too.
    /// </summary>
    public sealed class DrawsAfreshEveryTime : IDrawStreamFactory, IDrawStream
    {
        public IDrawStream ForOneRun() => this;

        public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
            => maxExclusive <= 0 ? 0 : Random.Shared.Next(maxExclusive);
    }
}
