using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates
{
    /// <summary>
    /// The half of the slice that needs a forecast to actually vary.
    ///
    /// How many Features a Team may have on the go at once bounds how many can begin on the first day, and
    /// a Feature worked by two Teams begins when the first of them begins. Neither claim survives a Team
    /// that delivers the same number every day: with no variation every percentile is the same day and the
    /// two ways of arriving at a Feature-level start agree by accident. So these draw for real, from a
    /// starting number the test pinned - unpinned they would be asserting sampling noise.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public partial class Slice01SampledStartTest
    {
        /// <summary>
        /// AC-1.3 and S2. Feature WIP bounds the range the run draws from, so at three a Team begins the
        /// top three on the first day and cannot reach the fourth until one of them is out of the way.
        /// None of the five is small enough to finish inside a single day, which is what keeps the fourth
        /// out - a Feature that emptied would let the one below it start the same day.
        /// </summary>
        // @driving_port @us-01 @edge @real-io @contract-shape:bounded-change (AC-1.3, S2)
        [Test]
        [Ignore(PendingDeliver)]
        public async Task Feature_WIP_bounds_how_many_Features_can_begin_on_the_first_day()
        {
            var portfolio = await GivenFiveFeaturesOnOneTeamAt(featureWip: 3);

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);

            var theTeamBegins = ADateThatMustBeThere(TheForecastedStart(features, "Rank 1", 85), "Rank 1's forecasted start");
            var fourth = ADateThatMustBeThere(TheForecastedStart(features, "Rank 4", 85), "Rank 4's forecasted start");
            var fifth = ADateThatMustBeThere(TheForecastedStart(features, "Rank 5", 85), "Rank 5's forecasted start");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheForecastedStart(features, "Rank 2", 85), Is.EqualTo(theTeamBegins),
                    "second in the order, and within the WIP, so it begins the day the Team does.");
                Assert.That(TheForecastedStart(features, "Rank 3", 85), Is.EqualTo(theTeamBegins));

                Assert.That(fourth, Is.GreaterThan(theTeamBegins),
                    "fourth in the order and outside the WIP - it waits for one of the three above it.");
                Assert.That(fifth, Is.GreaterThan(theTeamBegins));
            }
        }

        /// <summary>
        /// AC-1.3's other half, and the reason the scenario above is about the setting rather than about
        /// the number four. Raise what the Team may carry and the same five Features all begin at once:
        /// the wait was the policy, not the work.
        /// </summary>
        // @driving_port @us-01 @real-io @contract-shape:bounded-change (AC-1.3)
        [Test]
        [Ignore(PendingDeliver)]
        public async Task Raising_Feature_WIP_lets_them_all_begin_on_the_first_day()
        {
            var portfolio = await GivenFiveFeaturesOnOneTeamAt(featureWip: 5);

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);

            var theTeamBegins = ADateThatMustBeThere(TheForecastedStart(features, "Rank 1", 85), "Rank 1's forecasted start");

            using (Assert.EnterMultipleScope())
            {
                foreach (var rank in TheRanksBelowTheFirst)
                {
                    Assert.That(TheForecastedStart(features, rank, 85), Is.EqualTo(theTeamBegins),
                        $"{rank} is inside the WIP now, so nothing holds it back.");
                }
            }
        }

        /// <summary>
        /// AC-1.4 and D4 - the decisive scenario of the slice.
        ///
        /// A Feature two Teams share begins when the first of them begins, and there are two ways to say
        /// when that is. The one not taken reads it off the Teams' own distributions afterwards, as
        /// 1 - the product of their chances of not having started, which is the mirror of what the
        /// completion forecast does across Teams. That formula assumes the Teams are independent.
        ///
        /// Here they are not: both wait on the same upstream Feature, so they begin on the same day in
        /// every single run. Observed inside the run, the Feature's start is that day - exactly what each
        /// Team reports. Derived from the marginals it would be strictly earlier, because the formula
        /// credits the Feature with two independent chances of having started when it only ever had one.
        /// Equality here is therefore not a coincidence; it is the derivation being ruled out.
        /// </summary>
        // @driving_port @us-01 @edge @invariant @real-io @contract-shape:bounded-change (AC-1.4, D4, ADR-199)
        [Test]
        [Ignore(PendingDeliver)]
        public async Task The_start_a_Feature_reports_is_the_one_the_run_saw_not_the_one_its_Teams_marginals_imply()
        {
            var (portfolio, firstTeamId, secondTeamId) = await GivenTwoTeamsWaitingOnTheSameUpstreamFeature();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var downstream = TheFeatureNamed(features, Downstream);

            var atHalf = ADateThatMustBeThere(TheForecastedStart(downstream, 50), "the Feature's forecasted start at the 50th percentile");
            var atNinetyFive = ADateThatMustBeThere(TheForecastedStart(downstream, 95), "the Feature's forecasted start at the 95th percentile");
            var atEightyFive = ADateThatMustBeThere(TheForecastedStart(downstream, 85), "the Feature's forecasted start at the 85th percentile");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(atNinetyFive, Is.GreaterThan(atHalf),
                    "a start that lands on one day in every run would make the two ways of computing it agree, " +
                    "and this scenario would prove nothing.");

                Assert.That(TheForecastedStartFor(downstream, firstTeamId, 85), Is.EqualTo(atEightyFive),
                    "the Teams move together, so the earliest of them is either of them.");
                Assert.That(TheForecastedStartFor(downstream, secondTeamId, 85), Is.EqualTo(atEightyFive));
            }
        }
    }
}
