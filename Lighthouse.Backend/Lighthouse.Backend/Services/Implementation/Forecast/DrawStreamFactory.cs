using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// A fresh starting number for every run, which is what the forecast has always done - two refreshes
    /// over unchanged data already return slightly different dates, because each one is its own sample of
    /// the same distribution. Pinning it in production would stop that wobble and is a change worth making
    /// on its own merits one day; made here it would quietly bake one draw of sampling error into every
    /// date the product shows.
    /// </summary>
    public sealed class DrawStreamFactory : IDrawStreamFactory
    {
        public IDrawStream ForOneRun() => new AddressableDrawStream(Random.Shared.NextInt64());
    }
}
