using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// The real draw source, started from a number the test chose instead of a fresh one. Production picks a
    /// new starting number every run on purpose, so two unpinned runs over identical data return slightly
    /// different dates - any test comparing two runs has to pin it or it is comparing sampling noise.
    /// </summary>
    public sealed class DrawsFromAPinnedStartingNumber(long seed) : IDrawStreamFactory
    {
        public IDrawStream ForOneRun() => new AddressableDrawStream(seed);
    }
}
