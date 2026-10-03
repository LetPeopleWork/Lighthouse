using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Every voter answers the same question against the same number. When the Team has an SLE, the
    /// Refinement tab says so: its days and its probability. When it has none, the tab falls back to the
    /// 85th percentile of the Team's default cycle time over the Team's Throughput history window, and says
    /// that it is a fallback. When the Team has no SLE and nothing finished in that window, the tab says
    /// there is no number. The server answers facts; the browser writes the sentence.
    ///
    /// Driving port: the Refinement tab's read. Step definitions live in Slice10SleYardstickSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-10")]
    public partial class Slice10SleYardstickTest : SizingVotesAcceptanceTest
    {
        // @driving_port @real-io @us-10 @slice-10 @contract-shape:pure-function
        [Test]
        public async Task The_Teams_SLE_is_the_yardstick_every_voter_answers_against()
        {
            var gravity = await GivenGravityExpects85PercentWithinSevenDays();

            var tab = await WhenJonasOpensTheRefinementTab(gravity);

            ThenTheYardstickIs(tab, TheSle, days: 7, probability: 85);
        }

        // @driving_port @real-io @us-10 @slice-10 @contract-shape:pure-function
        [Test]
        public async Task Without_an_SLE_the_yardstick_is_the_85th_percentile_of_the_Teams_cycle_time()
        {
            var meridian = await GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();

            var tab = await WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        // @driving_port @real-io @us-10 @slice-10 @boundary @contract-shape:pure-function
        // The fallback samples the same window the Team's Throughput does, so old slow work does not
        // inflate the question.
        [Test]
        public async Task Work_Items_finished_before_the_Throughput_window_do_not_move_the_fallback()
        {
            var meridian = await GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();
            AndFiveVerySlowWorkItemsFinishedFortyDaysAgo(meridian);

            var tab = await WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        // @driving_port @real-io @us-10 @slice-10 @error @contract-shape:pure-function
        [Test]
        public async Task No_SLE_and_no_finished_Work_Items_leaves_the_question_without_a_number()
        {
            var equinox = await GivenEquinoxHasNoSleAndHasFinishedNothing();

            var tab = await WhenJonasOpensTheRefinementTab(equinox);

            ThenTheYardstickIs(tab, Unavailable, days: null, probability: null);
        }

        // @driving_port @real-io @us-10 @slice-10 @boundary @contract-shape:pure-function
        [Test]
        public async Task Work_finished_only_before_the_Throughput_window_counts_as_nothing_finished()
        {
            var equinox = await GivenEquinoxHasNoSleAndHasFinishedNothing();
            AndFiveVerySlowWorkItemsFinishedFortyDaysAgo(equinox);

            var tab = await WhenJonasOpensTheRefinementTab(equinox);

            ThenTheYardstickIs(tab, Unavailable, days: null, probability: null);
        }

        // @driving_port @real-io @us-10 @slice-10 @boundary @contract-shape:pure-function
        // An SLE needs both a probability and a number of days; either one alone is not a promise.
        [TestCase(85, 0)]
        [TestCase(0, 7)]
        public async Task Half_an_SLE_is_no_SLE_and_the_fallback_is_used(int probability, int days)
        {
            var meridian = await GivenMeridianWithAHalfSetSleAndCycleTimes85thPercentileOfTwelveDays(probability, days);

            var tab = await WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        // @driving_port @real-io @us-10 @slice-10 @contract-shape:bounded-change
        [Test]
        public async Task Setting_an_SLE_replaces_the_fallback_on_the_next_read()
        {
            var meridian = await GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();

            var withSle = await WhenTheAdminSetsTheSleTo75PercentWithinTenDays(meridian);
            var tab = await WhenJonasOpensTheRefinementTab(withSle);

            ThenTheYardstickIs(tab, TheSle, days: 10, probability: 75);
        }

        // @driving_port @real-io @us-10 @slice-10 @boundary @contract-shape:pure-function
        // The question is the same whoever asks: the yardstick is the Team's, not the reader's.
        [Test]
        public async Task Every_voter_is_shown_the_same_yardstick()
        {
            var meridian = await GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();

            var jonasSees = await WhenJonasOpensTheRefinementTab(meridian);
            var anaSees = await WhenAnaOpensTheRefinementTab(meridian);

            ThenBothAreShownTheSameYardstick(jonasSees, anaSees);
        }
    }
}
