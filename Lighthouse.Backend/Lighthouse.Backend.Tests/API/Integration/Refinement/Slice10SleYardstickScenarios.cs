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
    /// Driven at the application-service port: the Refinement tab's query, read as the browser receives it,
    /// with the Team set up through the port's one builder. That an SLE saved through the Team settings
    /// changes the next read is a matter of the settings route, so it is pinned on the wire contract of the
    /// Refinement tab. Step definitions live in Slice10SleYardstickSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-10")]
    public partial class Slice10SleYardstickTest : RefinementAtThePortTest
    {
        [Test]
        public void The_Teams_SLE_is_the_yardstick_every_voter_answers_against()
        {
            var gravity = GivenGravityExpects85PercentWithinSevenDays();

            var tab = WhenJonasOpensTheRefinementTab(gravity);

            ThenTheYardstickIs(tab, TheSle, days: 7, probability: 85);
        }

        [Test]
        public void Without_an_SLE_the_yardstick_is_the_85th_percentile_of_the_Teams_cycle_time()
        {
            var meridian = GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();

            var tab = WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        // The fallback samples the same window the Team's Throughput does, so old slow work does not
        // inflate the question.
        [Test]
        public void Work_Items_finished_before_the_Throughput_window_do_not_move_the_fallback()
        {
            var meridian = GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();
            AndFiveVerySlowWorkItemsFinishedFortyDaysAgo(meridian);

            var tab = WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        // The window counts today, so its first day is one day short of the Throughput history ago.
        [Test]
        public void Work_Items_finished_on_the_first_day_of_the_Throughput_window_move_the_fallback()
        {
            var meridian = GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();
            AndFiveVerySlowWorkItemsFinishedOnTheFirstDayOfTheThroughputWindow(meridian);

            var tab = WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 60, probability: 85);
        }

        [Test]
        public void Work_Items_finished_the_day_before_the_Throughput_window_opens_do_not_move_the_fallback()
        {
            var meridian = GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();
            AndFiveVerySlowWorkItemsFinishedTheDayBeforeTheThroughputWindowOpens(meridian);

            var tab = WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        [Test]
        public void No_SLE_and_no_finished_Work_Items_leaves_the_question_without_a_number()
        {
            var equinox = GivenEquinoxHasNoSleAndHasFinishedNothing();

            var tab = WhenJonasOpensTheRefinementTab(equinox);

            ThenTheYardstickIs(tab, Unavailable, days: null, probability: null);
        }

        [Test]
        public void Work_finished_only_before_the_Throughput_window_counts_as_nothing_finished()
        {
            var equinox = GivenEquinoxHasNoSleAndHasFinishedNothing();
            AndFiveVerySlowWorkItemsFinishedFortyDaysAgo(equinox);

            var tab = WhenJonasOpensTheRefinementTab(equinox);

            ThenTheYardstickIs(tab, Unavailable, days: null, probability: null);
        }

        // An SLE needs both a probability and a number of days; either one alone is not a promise.
        [TestCase(85, 0)]
        [TestCase(0, 7)]
        public void Half_an_SLE_is_no_SLE_and_the_fallback_is_used(int probability, int days)
        {
            var meridian = GivenMeridianWithAHalfSetSleAndCycleTimes85thPercentileOfTwelveDays(probability, days);

            var tab = WhenJonasOpensTheRefinementTab(meridian);

            ThenTheYardstickIs(tab, TheCycleTimeFallback, days: 12, probability: 85);
        }

        // The question is the same whoever asks: the yardstick is the Team's, not the reader's.
        [Test]
        public void Every_voter_is_shown_the_same_yardstick()
        {
            var meridian = GivenMeridianHasNoSleAndItsCycleTimes85thPercentileIsTwelveDays();

            var jonasSees = WhenJonasOpensTheRefinementTab(meridian);
            var anaSees = WhenAnaOpensTheRefinementTab(meridian);

            ThenBothAreShownTheSameYardstick(jonasSees, anaSees);
        }
    }
}
