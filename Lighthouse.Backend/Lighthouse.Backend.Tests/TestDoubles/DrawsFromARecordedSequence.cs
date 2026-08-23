using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// Hands out numbers written down in advance, one after the next, wrapping round at the end and paying
    /// no attention to where the draw was asked for. That is how the forecast's numbers behaved before they
    /// were addressed by coordinate, so a fixture using this reproduces a run of the released product draw
    /// for draw.
    ///
    /// It is therefore the only double here that a change to the order draws are taken in can break, which
    /// is exactly what makes it worth having while that order is being changed - and exactly why it cannot
    /// survive simulated runs being carried out side by side, where there is no one order at all.
    /// </summary>
    internal sealed class DrawsFromARecordedSequence : IDrawStreamFactory, IDrawStream
    {
        private readonly int[] numbers;

        private int next;

        internal DrawsFromARecordedSequence() : this([0])
        {
        }

        internal DrawsFromARecordedSequence(int[] numbers)
        {
            this.numbers = numbers;
        }

        public long StartingNumber => 0;

        public IDrawStream ForOneRun() => this;

        public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
        {
            var number = numbers[next];
            next = (next + 1) % numbers.Length;

            return number;
        }
    }
}
