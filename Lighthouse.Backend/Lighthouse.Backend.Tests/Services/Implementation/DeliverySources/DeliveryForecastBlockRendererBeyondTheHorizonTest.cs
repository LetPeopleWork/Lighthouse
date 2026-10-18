using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.DeliverySources;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliverySources
{
    /// <summary>
    /// A Delivery that publishes its forecast into a Jira Release writes one line per percentile. A
    /// percentile beyond the horizon says so in words, the way the Lighthouse screen does, because the
    /// date it carries is a stand-in nobody forecast and the Release is something stakeholders read.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-03")]
    [Category("us-03")]
    [Category("ac-03.9")]
    public class DeliveryForecastBlockRendererBeyondTheHorizonTest
    {
        private static readonly string Ball = char.ConvertFromUtf32(0x1F52E);

        private static readonly DateOnly WrittenOn = new(2026, 10, 18);

        private static readonly DateOnly Target = new(2026, 12, 15);

        private static readonly DateOnly TheStandInDate = new(2031, 10, 18);

        [Test]
        [Category("contract-shape:pure-function")]
        public void A_percentile_beyond_the_horizon_is_written_as_more_than_five_years_and_the_others_keep_their_dates()
        {
            var text = new DeliveryForecastBlockRenderer().Render(ABlock(
                new DeliveryForecastBlockPercentile(70, new DateOnly(2027, 3, 14)),
                new DeliveryForecastBlockPercentile(85, TheStandInDate, BeyondHorizon: true),
                new DeliveryForecastBlockPercentile(95, TheStandInDate, BeyondHorizon: true)));

            var expected = string.Join('\n',
                Ball + " Lighthouse forecast - updated 2026-10-18",
                "70%: 2027-03-14",
                "85%: More than 5 years",
                "95%: More than 5 years",
                "Target 2026-12-15: 12% likely",
                Ball);

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Delivery_whose_every_percentile_is_beyond_the_horizon_still_publishes_a_block_that_says_so()
        {
            var text = new DeliveryForecastBlockRenderer().Render(ABlock(
                new DeliveryForecastBlockPercentile(70, TheStandInDate, BeyondHorizon: true)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Does.Contain("70%: More than 5 years"));
                Assert.That(text, Does.Not.Contain("2031-10-18"), "The stand-in date reached the Release as though it were a forecast.");
            }
        }

        [Test]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public void A_block_with_a_percentile_beyond_the_horizon_replaces_an_older_block_and_leaves_the_teams_words_alone()
        {
            var renderer = new DeliveryForecastBlockRenderer();
            var older = renderer.Render(ABlock(new DeliveryForecastBlockPercentile(85, new DateOnly(2027, 1, 4))));
            var theirs = "Ships with the spring campaign.";

            var merged = renderer.MergeInto(theirs + "\n" + older,
                renderer.Render(ABlock(new DeliveryForecastBlockPercentile(85, TheStandInDate, BeyondHorizon: true))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(merged, Does.StartWith(theirs));
                Assert.That(merged, Does.Contain("85%: More than 5 years"));
                Assert.That(merged, Does.Not.Contain("2027-01-04"), "The older forecast was left standing beside the new one.");
            }
        }

        private static DeliveryForecastBlock ABlock(params DeliveryForecastBlockPercentile[] percentiles)
            => new(WrittenOn, percentiles, Target, 12);
    }
}
