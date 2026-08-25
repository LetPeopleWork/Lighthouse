using System.Globalization;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliverySources
{
    /// <summary>
    /// DISTILL acceptance specifications for Epic 5565 slice 04 - composing the published block and
    /// merging it into a description a human also writes in. Pure, so every marker rule is a unit test.
    ///
    /// The rules under test are the ones the slice 00 probe made safe to rely on: a hand edit through
    /// the Jira UI preserves the delimiters and every newline, so replacement in place is sound. What
    /// the probe could not make safe is a description whose markers have been broken, and the choice
    /// recorded there is deliberate - append a fresh block rather than infer a range to delete, because
    /// a visible duplicate is recoverable and deleted prose is not.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-04")]
    public class DeliveryForecastBlockRendererTest
    {
        private static readonly string Ball = char.ConvertFromUtf32(0x1F52E);

        private static string ABlock(string writtenOn) =>
            string.Join(Environment.NewLine,
                Ball + " Lighthouse forecast - updated " + writtenOn,
                "70%: 2026-09-15",
                "85%: 2026-09-29",
                "95%: 2026-10-13",
                "Target 2026-10-01: 88% likely",
                Ball);

        private DeliveryForecastBlockRenderer CreateSubject() => new();

        // @AC-05.3 - the observed default on the demo instance is a Release with no description at all.
        [Test]
        public void A_release_with_no_description_gets_one_that_is_only_the_forecast()
        {
            var subject = CreateSubject();
            var block = ABlock("2026-08-22");

            var result = subject.MergeInto(null, block);

            Assert.That(result, Is.EqualTo(block));
        }

        // @AC-05.4 - coexistence with the text the team wrote is the whole point of the markers.
        [Test]
        public void A_description_the_team_wrote_keeps_every_word_when_the_forecast_is_added()
        {
            var subject = CreateSubject();
            var theirs = "Ships with the autumn campaign. Ask Dana before moving this.";

            var result = subject.MergeInto(theirs, ABlock("2026-08-22"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Does.StartWith(theirs));
                Assert.That(result, Does.Contain(Ball));
            }
        }

        // @AC-05.4b - the criterion that carries the slice: rewriting replaces, it never accumulates.
        [Test]
        public void Writing_the_forecast_twice_leaves_one_forecast_not_two()
        {
            var subject = CreateSubject();
            var theirs = "Ships with the autumn campaign.";

            var once = subject.MergeInto(theirs, ABlock("2026-08-22"));
            var twice = subject.MergeInto(once, ABlock("2026-08-23"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CountOpeningLines(twice), Is.EqualTo(1));
                Assert.That(twice, Does.Contain("2026-08-23"));
                Assert.That(twice, Does.Not.Contain("2026-08-22"));
            }
        }

        // @AC-05.4c - text outside the markers survives a rewrite, on both sides of the block.
        [Test]
        public void Rewriting_the_forecast_disturbs_nothing_around_it()
        {
            var subject = CreateSubject();
            var before = "Ships with the autumn campaign.";
            var after = "Owner: Dana.";
            var existing = before + Environment.NewLine + ABlock("2026-08-22") + Environment.NewLine + after;

            var result = subject.MergeInto(existing, ABlock("2026-08-23"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Does.StartWith(before));
                Assert.That(result, Does.EndWith(after));
            }
        }

        // @AC-05.4d - a human who edited inside the block does not get a merge, they get it rewritten.
        [Test]
        public void A_hand_edited_forecast_is_replaced_wholesale_rather_than_merged()
        {
            var subject = CreateSubject();
            var tampered = ABlock("2026-08-22").Replace("88% likely", "definitely fine");

            var result = subject.MergeInto(tampered, ABlock("2026-08-23"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Does.Not.Contain("definitely fine"));
                Assert.That(CountOpeningLines(result), Is.EqualTo(1));
            }
        }

        // @error - the rule that protects prose Lighthouse did not write. Losing the closing marker must
        // never let the replace run to the end of the description.
        [Test]
        public void A_forecast_whose_closing_marker_was_deleted_costs_the_reader_nothing()
        {
            var subject = CreateSubject();
            var theirs = "Ask Dana before moving this.";
            var broken = ABlock("2026-08-22").Replace(Environment.NewLine + Ball, string.Empty);
            var existing = broken + Environment.NewLine + theirs;

            var result = subject.MergeInto(existing, ABlock("2026-08-23"));

            Assert.That(result, Does.Contain(theirs));
        }

        // @error - detection anchors on the opening line, so the emoji alone is just a character.
        [Test]
        public void An_emoji_someone_typed_themselves_is_not_treated_as_a_marker()
        {
            var subject = CreateSubject();
            var theirs = "Crystal ball " + Ball + " says we ship on time.";

            var result = subject.MergeInto(theirs, ABlock("2026-08-22"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Does.Contain(theirs));
                Assert.That(CountOpeningLines(result), Is.EqualTo(1));
            }
        }

        // @AC-05.3b - the first of the four required things, and the one the truncating Releases-list
        // column has to still show: whoever reads it must be able to tell where the number came from.
        [Test]
        public void The_block_says_Lighthouse_wrote_it_and_when()
        {
            var subject = CreateSubject();

            var block = subject.Render(AForecast());

            Assert.That(FirstLineOf(block), Is.EqualTo(Ball + " Lighthouse forecast - updated 2026-08-22"));
        }

        // @AC-05.3b - the same three percentiles the product renders on its own screen, so a Release
        // and the Lighthouse page can never disagree about which ones are on show.
        [Test]
        public void The_block_carries_all_three_forecasts_the_product_itself_shows()
        {
            var subject = CreateSubject();

            var block = subject.Render(AForecast());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(block, Does.Contain("70%: 2026-09-15"));
                Assert.That(block, Does.Contain("85%: 2026-09-29"));
                Assert.That(block, Does.Contain("95%: 2026-10-13"));
            }
        }

        // @AC-05.3b - the fourth required thing. The target is named rather than implied, because a
        // likelihood with no date attached is a number about nothing.
        [Test]
        public void The_block_names_the_target_and_the_chance_of_hitting_it()
        {
            var subject = CreateSubject();

            var block = subject.Render(AForecast());

            Assert.That(block, Does.Contain("Target 2026-10-01: 88% likely"));
        }

        /// <summary>
        /// A Jira Release is read by whoever opens it, from wherever they are. A date rendered in the
        /// server's culture would be 09/15/2026 for one reader and 15/09/2026 for another, and the two
        /// are indistinguishable for the first twelve days of any month.
        /// </summary>
        [Test]
        public void Dates_read_the_same_way_wherever_the_reader_is()
        {
            var subject = CreateSubject();
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            try
            {
                var block = subject.Render(AForecast());

                Assert.That(block, Does.Contain("70%: 2026-09-15"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        // A forecast is not precise enough for a decimal to mean anything, and the product's own screen
        // rounds the same number to a whole percent.
        [Test]
        public void The_chance_of_hitting_the_target_is_a_whole_number()
        {
            var subject = CreateSubject();

            var block = subject.Render(AForecast() with { LikelihoodPercentage = 87.6 });

            Assert.That(block, Does.Contain("Target 2026-10-01: 88% likely"));
        }

        /// <summary>
        /// What the renderer writes is what the merge later has to find. Composed and matched apart,
        /// the two can drift into a state where every write appends and the description fills up with
        /// forecasts - which is the failure the markers exist to prevent.
        /// </summary>
        [Test]
        public void What_the_block_is_written_as_is_what_a_later_write_finds()
        {
            var subject = CreateSubject();
            var theirs = "Ships with the autumn campaign.";

            var once = subject.MergeInto(theirs, subject.Render(AForecast()));
            var twice = subject.MergeInto(once, subject.Render(AForecast() with { WrittenOn = new DateOnly(2026, 8, 23) }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CountOpeningLines(twice), Is.EqualTo(1));
                Assert.That(twice, Does.StartWith(theirs));
                Assert.That(twice, Does.Contain("updated 2026-08-23"));
                Assert.That(twice, Does.Not.Contain("updated 2026-08-22"));
            }
        }

        /// <summary>
        /// The failure adversarial review found, and the one ADR-179 exists to prevent. A user deletes
        /// the closing marker of a block, types a note where it stood, and the next write appends a
        /// fresh block below - so the description now holds an orphaned opening line, a person's own
        /// words, and one whole block. Pairing that first opening line with the whole block's closing
        /// marker deletes everything between them, which is the note.
        /// </summary>
        [Test]
        public void An_unclosed_block_never_pairs_with_a_later_blocks_closing_marker()
        {
            var subject = CreateSubject();
            var theirNote = "DO NOT SHIP BEFORE LEGAL SIGN-OFF. Ask Dana. Ticket LEG-4412.";
            var orphaned = ABlock("2026-08-22").Replace(Environment.NewLine + Ball, string.Empty);
            var existing = string.Join(Environment.NewLine, orphaned, theirNote, ABlock("2026-08-23"));

            var result = subject.MergeInto(existing, ABlock("2026-08-24"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Does.Contain(theirNote));
                Assert.That(result, Does.Contain("2026-08-24"));
                Assert.That(result, Does.Not.Contain("2026-08-23"), "the whole block is the one that gets replaced.");
            }
        }

        /// <summary>
        /// Quoting the line Lighthouse wrote and typing underneath it is the obvious way to argue with
        /// a forecast. Matched on the phrase alone, that sentence opens a span that runs to the real
        /// block's closing marker and takes everything the person wrote with it.
        /// </summary>
        [Test]
        public void A_sentence_that_merely_starts_like_the_block_does_not_open_one()
        {
            var subject = CreateSubject();
            var theirArgument = Ball + " Lighthouse forecasts look wrong to me - checking with the team.";
            var theirNote = "Owner: Dana. Budget code 88123. Do not delete.";
            var existing = string.Join(Environment.NewLine, theirArgument, theirNote, ABlock("2026-08-22"));

            var result = subject.MergeInto(existing, ABlock("2026-08-23"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Does.Contain(theirArgument));
                Assert.That(result, Does.Contain(theirNote));
                Assert.That(result, Does.Not.Contain("2026-08-22"));
            }
        }

        /// <summary>
        /// A crystal ball typed inside the block would otherwise close it early, and the replace would
        /// cut the block in half: the lines below the stray marker would be left standing outside every
        /// marker, showing dates a reader takes for current, where no later write could ever reach them.
        /// Appending instead costs the reader a visible duplicate, which they can delete.
        /// </summary>
        [Test]
        public void A_stray_marker_inside_the_block_costs_the_reader_nothing_and_still_settles()
        {
            var subject = CreateSubject();
            var tampered = ABlock("2026-08-22").Replace(
                "70%: 2026-09-15", "70%: 2026-09-15" + Environment.NewLine + Ball);

            var once = subject.MergeInto(tampered, ABlock("2026-08-23"));
            var twice = subject.MergeInto(once, ABlock("2026-08-24"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(once, Does.Contain(tampered), "nothing a hand left behind is cut out.");
                Assert.That(twice, Does.Contain("2026-08-24"));
                Assert.That(twice, Does.Not.Contain("2026-08-23"), "the block Lighthouse does own is still replaced in place.");
                Assert.That(CountOpeningLines(twice), Is.EqualTo(CountOpeningLines(once)),
                    "so a description with a broken block in it settles rather than gaining one on every refresh.");
            }
        }

        /// <summary>
        /// A block that picks up a single space - an indent, a paste through an editor - has to stay
        /// findable. Left unfindable while still being written, every publish appends and the indented
        /// one lingers for good.
        /// </summary>
        [Test]
        public void A_block_that_picked_up_an_indent_is_still_the_block_it_was()
        {
            var subject = CreateSubject();
            var indented = "  " + ABlock("2026-08-22").Replace(Environment.NewLine, Environment.NewLine + "  ");

            var result = subject.MergeInto(indented, ABlock("2026-08-23"));

            Assert.That(CountOpeningLines(result), Is.EqualTo(1));
        }

        // A forecast has to carry all three percentiles, so no percentiles at all is a failure rather
        // than a shorter block.
        [Test]
        public void A_forecast_with_nothing_to_forecast_is_refused_rather_than_written_short()
        {
            var subject = CreateSubject();

            Assert.Throws<ArgumentException>(() => subject.Render(AForecast() with { Percentiles = [] }));
        }

        /// <summary>
        /// A description written on Windows, or pasted out of one, separates its lines with a carriage
        /// return and a line feed. Read as two separators rather than one, every line of it lands one
        /// apart from where it is, and a block Lighthouse wrote stops being findable in its own text -
        /// so every publish appends and the Release fills up with forecasts. Nothing on a Linux build
        /// exercises this by accident: the test fixtures' own separator is a bare line feed.
        /// </summary>
        [Test]
        public void A_description_written_with_carriage_returns_is_read_the_same_way()
        {
            var subject = CreateSubject();
            var before = "Ships with the autumn campaign.";
            var after = "Owner: Dana.";
            var existing = string.Join("\r\n", before, ABlock("2026-08-22").Replace(Environment.NewLine, "\r\n"), after);

            var result = subject.MergeInto(existing, ABlock("2026-08-23"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CountOpeningLines(result), Is.EqualTo(1));
                Assert.That(result, Does.StartWith(before));
                Assert.That(result, Does.EndWith(after));
                Assert.That(result, Does.Not.Contain("2026-08-22"));
            }
        }

        /// <summary>
        /// An empty line is one separator followed by another, not one long one. Read as a single
        /// separator, everything after the first blank line in a description shifts and the block stops
        /// being found - and a person leaving a blank line above their notes is the ordinary case.
        /// </summary>
        [Test]
        public void A_blank_line_in_the_description_does_not_hide_the_block_below_it()
        {
            var subject = CreateSubject();
            var theirs = "Ships with the autumn campaign.";
            var existing = theirs + Environment.NewLine + Environment.NewLine + ABlock("2026-08-22");

            var result = subject.MergeInto(existing, ABlock("2026-08-23"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CountOpeningLines(result), Is.EqualTo(1));
                Assert.That(result, Does.StartWith(theirs));
                Assert.That(result, Does.Not.Contain("2026-08-22"));
            }
        }

        // A carriage return with nothing after it is the last character of the description. Looking past
        // it for the line feed that usually follows reads off the end of the text.
        [Test]
        public void A_description_ending_in_a_bare_carriage_return_is_read_without_running_off_the_end()
        {
            var subject = CreateSubject();
            var theirs = "Ask Dana before moving this.\r";

            var result = subject.MergeInto(theirs, ABlock("2026-08-22"));

            Assert.That(result, Does.StartWith(theirs));
        }

        [Test]
        public void A_forecast_that_is_nothing_at_all_is_refused()
        {
            var subject = CreateSubject();

            Assert.Throws<ArgumentNullException>(() => subject.Render(null!));
        }

        private static DeliveryForecastBlock AForecast() => new(
            new DateOnly(2026, 8, 22),
            [
                new DeliveryForecastBlockPercentile(70, new DateOnly(2026, 9, 15)),
                new DeliveryForecastBlockPercentile(85, new DateOnly(2026, 9, 29)),
                new DeliveryForecastBlockPercentile(95, new DateOnly(2026, 10, 13)),
            ],
            new DateOnly(2026, 10, 1),
            88.0);

        private static string FirstLineOf(string block) => block.Split('\n')[0];

        // Split on the line feed alone and the carriage return trimmed off, so a fixture built with the
        // host's separator and a block written with the one Jira stores are counted the same way.
        private static int CountOpeningLines(string description) => description
            .Split('\n')
            .Count(line => line.TrimStart('\r').StartsWith(DeliveryForecastBlockRenderer.OpeningLinePrefix, StringComparison.Ordinal));
    }
}
