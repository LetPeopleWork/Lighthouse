using System.Globalization;
using System.Text;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;

namespace Lighthouse.Backend.Services.Implementation.DeliverySources
{
    public class DeliveryForecastBlockRenderer : IDeliveryForecastBlockRenderer
    {
        /// <summary>
        /// The crystal ball opens and closes the block. Detection anchors on the whole opening line
        /// rather than this character alone, so a stray emoji in a human sentence is never mistaken for
        /// a marker.
        /// </summary>
        public static readonly string Marker = char.ConvertFromUtf32(0x1F52E);

        public static readonly string OpeningLinePrefix = Marker + " Lighthouse forecast";

        // The separator a Jira description is stored with, measured rather than assumed. Writing the
        // host's separator instead would put carriage returns into the field on a Windows instance and
        // not on a Linux one, so the same Release would read differently depending on who published it.
        private const string LineSeparator = "\n";

        // A day the same reader reads the same way from anywhere. Rendered in the server's culture,
        // 09/15/2026 and 15/09/2026 are the same string for the first twelve days of every month.
        private const string DayFormat = "yyyy-MM-dd";

        private readonly record struct Line(int Start, int Length);

        public string Render(DeliveryForecastBlock block)
        {
            ArgumentNullException.ThrowIfNull(block);

            var text = new StringBuilder();

            // Attribution first, and the forecasts before the target line, because the Releases list
            // shows this field as a column that truncates. What survives the truncation has to read as
            // something true and attributable rather than as a dangling fragment.
            text.Append(OpeningLinePrefix)
                .Append(" - updated ")
                .Append(Day(block.WrittenOn))
                .Append(LineSeparator);

            foreach (var percentile in block.Percentiles)
            {
                text.Append(percentile.Percentile.ToString(CultureInfo.InvariantCulture))
                    .Append("%: ")
                    .Append(Day(percentile.ExpectedDate))
                    .Append(LineSeparator);
            }

            text.Append("Target ")
                .Append(Day(block.TargetDate))
                .Append(": ")
                .Append(Math.Round(block.LikelihoodPercentage, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture))
                .Append("% likely")
                .Append(LineSeparator)
                .Append(Marker);

            return text.ToString();
        }

        public string MergeInto(string? existingDescription, string blockText)
        {
            if (string.IsNullOrEmpty(existingDescription))
            {
                return blockText;
            }

            if (FindPreviousBlock(existingDescription) is not { } previous)
            {
                return existingDescription + LineSeparator + blockText;
            }

            return string.Concat(
                existingDescription.AsSpan(0, previous.Start),
                blockText,
                existingDescription.AsSpan(previous.Start + previous.Length));
        }

        private static string Day(DateOnly day) => day.ToString(DayFormat, CultureInfo.InvariantCulture);

        /// <summary>
        /// Where a block Lighthouse wrote begins and ends, or nothing when the pair cannot be found
        /// whole. Nothing is what a lost delimiter has to produce: a range inferred from half a pair is
        /// a guess, and the text it would delete belongs to whoever typed it. A duplicate block is
        /// something a person can see and remove; prose that has been eaten is not.
        /// </summary>
        private static Line? FindPreviousBlock(string description)
        {
            var lines = LinesOf(description);

            var opening = lines.FindIndex(line => StartsTheBlock(description, line));
            if (opening < 0)
            {
                return null;
            }

            var closing = lines.FindIndex(opening + 1, line => EndsTheBlock(description, line));
            if (closing < 0)
            {
                return null;
            }

            var start = lines[opening].Start;

            return new Line(start, lines[closing].Start + lines[closing].Length - start);
        }

        private static bool StartsTheBlock(string description, Line line)
        {
            return description.AsSpan(line.Start, line.Length).StartsWith(OpeningLinePrefix, StringComparison.Ordinal);
        }

        // The closing marker is a line carrying nothing else, so the emoji inside somebody's own
        // sentence cannot close a block.
        private static bool EndsTheBlock(string description, Line line)
        {
            return description.AsSpan(line.Start, line.Length).Trim().SequenceEqual(Marker);
        }

        /// <summary>
        /// The lines of the text with their positions in it, so the merge can replace a span of the
        /// original rather than rebuild it from parts - which is what leaves every character outside
        /// the block, separators included, exactly as its author left it.
        ///
        /// All three separators are recognised, because the description may have been written by Jira,
        /// by a person on any platform, or by an earlier Lighthouse write.
        /// </summary>
        private static List<Line> LinesOf(string description)
        {
            var lines = new List<Line>();
            var start = 0;
            var index = 0;

            while (index < description.Length)
            {
                if (description[index] is not ('\n' or '\r'))
                {
                    index++;
                    continue;
                }

                lines.Add(new Line(start, index - start));

                var separatorLength = description[index] == '\r' && index + 1 < description.Length && description[index + 1] == '\n'
                    ? 2
                    : 1;

                index += separatorLength;
                start = index;
            }

            lines.Add(new Line(start, description.Length - start));

            return lines;
        }
    }
}
