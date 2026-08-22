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
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
        public void A_release_with_no_description_gets_one_that_is_only_the_forecast()
        {
            var subject = CreateSubject();
            var block = ABlock("2026-08-22");

            var result = subject.MergeInto(null, block);

            Assert.That(result, Is.EqualTo(block));
        }

        // @AC-05.4 - coexistence with the text the team wrote is the whole point of the markers.
        [Test]
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
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
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
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
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
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
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
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
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
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
        [Ignore("DISTILL scaffold - slice 04 not yet implemented")]
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

        private static int CountOpeningLines(string description) => description
            .Split(Environment.NewLine)
            .Count(line => line.StartsWith(DeliveryForecastBlockRenderer.OpeningLinePrefix, StringComparison.Ordinal));
    }
}
