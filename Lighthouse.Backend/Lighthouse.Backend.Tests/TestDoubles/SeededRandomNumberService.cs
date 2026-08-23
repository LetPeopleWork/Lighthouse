using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// Draws from a pinned starting number, so two runs over the same data see the same sequence. Production
    /// deliberately does not do this - a fresh source per run is a decision, not an oversight - which is why
    /// any test comparing two runs has to pin it or it compares sampling noise.
    ///
    /// One sequence, drawn from wherever the run asks next: a test that has more than one Team drawing at
    /// once is not comparing anything reproducible, and should have one.
    /// </summary>
    public sealed class SeededRandomNumberService(int seed) : IRandomNumberService
    {
        private readonly Random random = new(seed);

        public int GetRandomNumber(int maxValue) => random.Next(maxValue);
    }
}
