using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// A draw worked out from its coordinates by hashing them, so nothing is stored, nothing is allocated
    /// and nothing is shared.
    ///
    /// It is written out here rather than taken from .NET because <see cref="Random"/> documents its
    /// algorithm as free to change between releases. The forecast's safety net is a test that compares two
    /// runs number for number, and that net would tear on a runtime upgrade with nothing wrong with
    /// Lighthouse at all.
    /// </summary>
    public sealed class AddressableDrawStream(long seed) : IDrawStream
    {
        private const ulong TrialPrime = 0xD6E8FEB86659FD93UL;
        private const ulong TeamPrime = 0xA24BAED4963EE407UL;
        private const ulong DayPrime = 0x9FB21C651E98DF25UL;
        private const ulong OrdinalPrime = 0xC2B2AE3D27D4EB4FUL;
        private const ulong RangePrime = 0x165667B19E3779F9UL;

        private readonly ulong runSeed = unchecked((ulong)seed);

        public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
        {
            if (maxExclusive <= 1)
            {
                return 0;
            }

            var word = HashOf(trial, team, day, ordinal, maxExclusive);

            // The top bits of a 64-bit number scaled onto the range asked for. There is no re-draw when the
            // scaling lands unevenly: a re-draw would make the answer depend on how many times it retried,
            // and a draw that depends on anything but its coordinates is exactly what this class exists to
            // avoid. What that leaves behind is a lean of about one part in eighteen quintillion, which no
            // number of simulated runs could ever show.
            return (int)(((UInt128)word * (ulong)maxExclusive) >> 64);
        }

        /// <summary>
        /// The range is mixed in alongside the coordinates so that two draws asked for at the same place
        /// over different ranges are unrelated. Scaling one number onto two ranges would make the smaller
        /// draw readable from the larger one.
        /// </summary>
        private ulong HashOf(int trial, int team, int day, int ordinal, int maxExclusive)
        {
            var word = Mix(runSeed ^ ((ulong)(uint)trial * TrialPrime));
            word = Mix(word ^ ((ulong)(uint)team * TeamPrime));
            word = Mix(word ^ ((ulong)(uint)day * DayPrime));
            word = Mix(word ^ ((ulong)(uint)ordinal * OrdinalPrime));

            return Mix(word ^ ((ulong)(uint)maxExclusive * RangePrime));
        }

        private static ulong Mix(ulong word)
        {
            unchecked
            {
                word += 0x9E3779B97F4A7C15UL;
                word = (word ^ (word >> 30)) * 0xBF58476D1CE4E5B9UL;
                word = (word ^ (word >> 27)) * 0x94D049BB133111EBUL;

                return word ^ (word >> 31);
            }
        }
    }
}
