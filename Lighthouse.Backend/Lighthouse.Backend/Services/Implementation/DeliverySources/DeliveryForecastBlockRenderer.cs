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

        /// <summary>
        /// Everything of the opening line that never varies, the trailing separator included. Matching
        /// the shorter phrase alone would also match a sentence that merely begins with it - somebody
        /// quoting the line to comment on it, which is the obvious way to argue with a forecast - and
        /// everything they then typed would be inside the span Lighthouse rewrites.
        /// </summary>
        public static readonly string OpeningLinePrefix = Marker + " Lighthouse forecast - updated ";

        /// <summary>
        /// The last thing the block says before it closes. Written and looked for in one place, because
        /// the closing marker on its own cannot say which lone crystal ball belongs to Lighthouse: a
        /// stray one typed inside the block would otherwise end the span early and leave the rest of an
        /// old forecast standing outside the markers, reading as current and never cleaned up again.
        /// </summary>
        private const string TargetLinePrefix = "Target ";

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

            // The three forecasts are one of the four things the block is required to carry, so a block
            // with none of them is a failure rather than a shorter block. Unreachable from the only
            // caller, which skips a Delivery that has no forecast - it is here so the next caller
            // written finds out loudly instead of publishing a statement with a hole in it.
            if (block.Percentiles is not { Count: > 0 })
            {
                throw new ArgumentException(
                    "A published forecast has to carry at least one percentile.", nameof(block));
            }

            var text = new StringBuilder();

            // Attribution first, and the forecasts before the target line, because the Releases list
            // shows this field as a column that truncates. What survives the truncation has to read as
            // something true and attributable rather than as a dangling fragment.
            text.Append(OpeningLinePrefix)
                .Append(Day(block.WrittenOn))
                .Append(LineSeparator);

            foreach (var percentile in block.Percentiles)
            {
                text.Append(percentile.Percentile.ToString(CultureInfo.InvariantCulture))
                    .Append("%: ")
                    .Append(Day(percentile.ExpectedDate))
                    .Append(LineSeparator);
            }

            text.Append(TargetLinePrefix)
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
        /// Where a block Lighthouse wrote begins and ends, or nothing when no whole one can be found.
        ///
        /// Every opening line is tried in turn and each is matched only against a closing marker that
        /// comes before the NEXT opening line, so an opening whose own closer was deleted is passed over
        /// rather than paired with a later block's - pairing across two blocks would delete everything
        /// between them, which is a person's own prose in the one case ADR-179 exists to survive.
        ///
        /// Nothing at all is what a description with no whole block has to produce: a range inferred
        /// from half a pair is a guess, and the text it would delete belongs to whoever typed it. A
        /// duplicate block is something a person can see and remove; prose that has been eaten is not.
        /// </summary>
        private static Line? FindPreviousBlock(string description)
        {
            var lines = LinesOf(description);
            var openings = OpeningLinesIn(description, lines);

            for (var index = 0; index < openings.Count; index++)
            {
                var opening = openings[index];
                var nextOpening = index + 1 < openings.Count ? openings[index + 1] : lines.Count;

                if (ClosingLineFor(description, lines, opening, nextOpening) is not { } closing)
                {
                    continue;
                }

                var start = lines[opening].Start;

                return new Line(start, lines[closing].Start + lines[closing].Length - start);
            }

            return null;
        }

        private static List<int> OpeningLinesIn(string description, List<Line> lines)
        {
            var openings = new List<int>();

            for (var index = 0; index < lines.Count; index++)
            {
                if (StartsTheBlock(description, lines[index]))
                {
                    openings.Add(index);
                }
            }

            return openings;
        }

        /// <summary>
        /// The line that closes this block, or nothing when what stands between reads as anything other
        /// than a forecast. The check is the line just above the marker: Lighthouse always writes the
        /// target line last, so a marker sitting anywhere else was typed by somebody, and honouring it
        /// would end the span early and leave the tail of an old forecast outside the markers where no
        /// later write can ever reach it.
        /// </summary>
        private static int? ClosingLineFor(string description, List<Line> lines, int opening, int nextOpening)
        {
            for (var index = opening + 1; index < nextOpening; index++)
            {
                if (!EndsTheBlock(description, lines[index]))
                {
                    continue;
                }

                return IsTheLastThingTheBlockSays(description, lines[index - 1]) ? index : null;
            }

            return null;
        }

        // Leading whitespace is ignored on both markers. Ignored on one and not the other, a block that
        // picks up a single space - an indent, a paste - stops being findable while still being written,
        // so every later publish appends and the indented one lingers for good.
        private static bool StartsTheBlock(string description, Line line)
        {
            return description.AsSpan(line.Start, line.Length).TrimStart().StartsWith(OpeningLinePrefix, StringComparison.Ordinal);
        }

        // The closing marker is a line carrying nothing else, so the emoji inside somebody's own
        // sentence cannot close a block.
        private static bool EndsTheBlock(string description, Line line)
        {
            return description.AsSpan(line.Start, line.Length).Trim().SequenceEqual(Marker);
        }

        private static bool IsTheLastThingTheBlockSays(string description, Line line)
        {
            return description.AsSpan(line.Start, line.Length).TrimStart().StartsWith(TargetLinePrefix, StringComparison.Ordinal);
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
