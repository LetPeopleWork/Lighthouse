using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// When a piece of work was admitted and when it started running, and how that pair is written down.
    ///
    /// Either half may be missing, and neither absence is a fault. A replica still on an older build
    /// records nothing at all, and work that is only waiting has not started yet, so every reader has to
    /// cope with a moment that is simply not there.
    ///
    /// The format lives here rather than in the store because it is the part of ADR-182 left open: the ADR
    /// fixes which hash the pair goes in and that it is written outside the Lua scripts, and says nothing
    /// about how the two halves are spelled.
    /// </summary>
    internal readonly record struct UpdateMoments(DateTimeOffset? QueuedAt, DateTimeOffset? StartedAt)
    {
        private const char Separator = '|';

        public static UpdateMoments NothingRecorded => default;

        public string ToStorageValue() => $"{Written(QueuedAt)}{Separator}{Written(StartedAt)}";

        /// <summary>
        /// Anything unreadable reads as nothing recorded, which is the same answer a rolling upgrade already
        /// produces and which every caller therefore already handles. Throwing would cost an operator the
        /// whole list over one malformed field - the habit the ordinal hash beside this one already keeps.
        /// </summary>
        public static UpdateMoments Parse(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return NothingRecorded;
            }

            var separator = text.IndexOf(Separator, StringComparison.Ordinal);
            if (separator < 0)
            {
                return NothingRecorded;
            }

            return new UpdateMoments(Read(text[..separator]), Read(text[(separator + 1)..]));
        }

        private static readonly long EarliestExpressible = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

        private static readonly long LatestExpressible = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();

        private static string Written(DateTimeOffset? moment)
            => moment?.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        /// <summary>
        /// The range check is not paranoia: a great many numbers parse as a <c>long</c> and then throw on the
        /// way to an instant, and the likeliest source is the very case this format is meant to survive - a
        /// later build writing a finer unit. Today in microseconds, or in ticks, is an ordinary-looking
        /// number that is nowhere near a representable millisecond.
        /// </summary>
        private static DateTimeOffset? Read(string written)
        {
            if (!long.TryParse(written, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixMs))
            {
                return null;
            }

            return unixMs >= EarliestExpressible && unixMs <= LatestExpressible
                ? DateTimeOffset.FromUnixTimeMilliseconds(unixMs)
                : null;
        }
    }
}
