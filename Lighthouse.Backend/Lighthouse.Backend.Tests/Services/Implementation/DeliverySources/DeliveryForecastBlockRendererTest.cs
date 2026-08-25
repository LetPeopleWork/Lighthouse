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
