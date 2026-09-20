using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates
{
    /// <summary>
    /// A Feature's forecast says when work on it is expected to begin, read off the same simulated runs
    /// that already say when it is expected to end.
    ///
    /// Every scenario here runs over a Team that delivers the same number every day, because the two that
    /// matter most - that a Feature starts the day the one above it finishes, and that a started Feature
    /// reports the day it actually started - are claims about which day, and a sampled forecast can only
    /// make claims about a spread. The sampled half of the slice lives next door.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public partial class Slice01StartForecastTest
    {
        /// <summary>
        /// AC-1.1, and the one scenario that has to hold before any of the others mean anything: the day
        /// the run starts a Feature reaches a client, as a date, at the confidence levels the completion
        /// date already uses.
        /// </summary>
        // @walking_skeleton @driving_port @us-01 @real-io @contract-shape:bounded-change (AC-1.1)
        [Test]
        [Category("walking_skeleton")]
        public async Task A_Feature_nobody_has_started_says_when_work_on_it_is_expected_to_begin()
        {
            var portfolio = await GivenAQueueOfTwoFeaturesOnOneTeam();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var alpha = TheFeatureNamed(features, Alpha);

            Assert.That(TheStartSource(alpha), Is.EqualTo("Forecast"),
                "nothing has been pulled yet, so the date can only come from the simulation.");

            foreach (var percentile in TheFourPercentiles)
            {
                Assert.That(TheForecastedStart(alpha, percentile), Is.Not.Null,
                    $"no forecasted start at the {percentile}th percentile.");
            }

            var start = ADateThatMustBeThere(TheForecastedStart(alpha, 85), "Alpha's forecasted start");
            var completion = ADateThatMustBeThere(TheForecastedCompletion(alpha, 85), "Alpha's forecasted completion");

            Assert.That(start, Is.LessThan(completion),
                "a Feature that takes more than a day to deliver cannot start and finish on the same one.");
        }

        /// <summary>
        /// AC-1.2 and D7. The Epic was framed on the assumption that the next Feature begins the day after
        /// the one above it ends. It does not: a Team re-reads what it may work on after every item it
        /// delivers, so a Team that closes a Feature with capacity left in the day starts the next one
        /// immediately. Written from that reasoning before it was run, because it is the assertion most
        /// likely to be quietly rewritten to match whatever came out.
        ///
        /// The one-day gap is real, but it is a gap between Teams, and there is only one Team here.
        /// </summary>
        // @driving_port @us-01 @real-io @contract-shape:bounded-change (AC-1.2, D7)
        [Test]
        public async Task At_Feature_WIP_one_the_next_Feature_starts_the_day_the_one_above_it_finishes()
        {
            var portfolio = await GivenAQueueOfTwoFeaturesOnOneTeam();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var alpha = TheFeatureNamed(features, Alpha);
            var bravo = TheFeatureNamed(features, Bravo);

            Assert.That(TheForecastedStart(bravo, 85), Is.EqualTo(TheForecastedCompletion(alpha, 85)),
                "the same day the Feature above it finished, not the day after.");
        }

        /// <summary>
        /// AC-1.6 and D5. A Feature already in flight has a start date that is a fact, and a forecast of
        /// it would be a worse answer to a question already answered. The simulation is not told what is
        /// in flight - it still runs, still records a day - and this is decided on the way out.
        /// </summary>
        // @driving_port @us-01 @edge @real-io @contract-shape:bounded-change (AC-1.6, D5)
        [Test]
        public async Task A_Feature_already_in_flight_reports_the_day_it_actually_started()
        {
            var startedOn = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

            var portfolio = await GivenAQueueOfTwoFeaturesOnOneTeam(alphaStartedOn: startedOn);

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var alpha = TheFeatureNamed(features, Alpha);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStartSource(alpha), Is.EqualTo("Observed"));
                Assert.That(TheObservedStart(alpha), Is.EqualTo(startedOn));
                Assert.That(TheForecastedStart(alpha, 85), Is.Null,
                    "a percentile beside an observed date reads as a second opinion about a settled fact.");
            }
        }

        /// <summary>
        /// AC-1.9 and AC-1.10. Both grains are served whatever the screens currently draw, so the
        /// question "which Team is the late one" stays answerable and the decision about where it surfaces
        /// stays reversible without touching the backend.
        ///
        /// The per-Team completion half closes a gap that predates this Epic: those forecasts have always
        /// existed in the domain and have always died at the boundary.
        /// </summary>
        // @driving_port @us-01 @real-io @contract-shape:bounded-change (AC-1.9, AC-1.10, D16)
        [Test]
        public async Task A_Feature_two_Teams_share_reports_a_start_and_a_completion_for_each_of_them()
        {
            var (portfolio, firstTeamId, secondTeamId) = await GivenAFeatureTwoTeamsShare();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var shared = TheFeatureNamed(features, Shared);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheForecastedStartFor(shared, firstTeamId, 85), Is.Not.Null);
                Assert.That(TheForecastedStartFor(shared, secondTeamId, 85), Is.Not.Null);
                Assert.That(TheForecastedCompletionFor(shared, firstTeamId, 85), Is.Not.Null,
                    "a lane spanning nothing but a start is not a bar - the timeline needs both ends.");
                Assert.That(TheForecastedCompletionFor(shared, secondTeamId, 85), Is.Not.Null);
            }
        }

        /// <summary>
        /// AC-1.9's other half. A Feature one Team works has one row, so its own start and that Team's
        /// start are the same observation seen at two grains and must be the same number. If they ever
        /// differ, the roll-up is doing arithmetic instead of reading.
        /// </summary>
        // @driving_port @us-01 @invariant @real-io @contract-shape:pure-function (AC-1.9, D4)
        [Test]
        public async Task A_Feature_one_Team_works_reports_the_same_number_at_both_grains()
        {
            var portfolio = await GivenAQueueOfTwoFeaturesOnOneTeam();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var bravo = TheFeatureNamed(features, Bravo);

            // Both sides have to be present before they are compared. Absent, they are both null and
            // null equals null - the scenario would pass against an implementation that served neither
            // grain, which is exactly the shape of test this slice cannot afford.
            var perTeam = ADateThatMustBeThere(
                TheForecastedStartFor(bravo, TheOnlyTeamId, 85), "Bravo's forecasted start for its one Team");
            var perFeature = ADateThatMustBeThere(
                TheForecastedStart(bravo, 85), "Bravo's own forecasted start");

            Assert.That(perTeam, Is.EqualTo(perFeature));
        }

        /// <summary>
        /// AC-1.5. The completion forecast is the most load-bearing number in the product and this Epic
        /// must not move it. The guard is structural rather than numeric - start distributions are not in
        /// the collection the completion aggregate reads - and this is the scenario that would notice if
        /// that ever stopped being true, because a start row folded into the aggregate would drag every
        /// completion percentile earlier.
        /// </summary>
        // @regression @us-01 @real-io @contract-shape:unbounded-preservation (AC-1.5, D3)
        // Passes on unmodified main. It is a regression guard rather than a RED scaffold: it asserts
        // only against the completion contract, which this slice must leave exactly where it is.
        [Test]
        public async Task Nothing_about_the_completion_forecast_moves()
        {
            var portfolio = await GivenAQueueOfTwoFeaturesOnOneTeam();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var alpha = TheFeatureNamed(features, Alpha);
            var bravo = TheFeatureNamed(features, Bravo);

            var alphaFinishes = ADateThatMustBeThere(TheForecastedCompletion(alpha, 85), "Alpha's forecasted completion");
            var bravoFinishes = ADateThatMustBeThere(TheForecastedCompletion(bravo, 85), "Bravo's forecasted completion");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheCompletionPercentilesOf(alpha), Is.EqualTo(TheFourPercentiles),
                    "the completion list keeps its shape - four percentiles, no start rows folded in.");

                Assert.That(TheForecastedCompletion(alpha, 50), Is.EqualTo(TheForecastedCompletion(alpha, 95)),
                    "this Team delivers the same number every day, so every trial finishes on the same day.");

                Assert.That(bravoFinishes, Is.GreaterThan(alphaFinishes),
                    "the second Feature in the order still finishes after the first.");
            }
        }

        /// <summary>
        /// AC-1.7. A Team with nothing measured takes no part in the run, so there is nothing to record
        /// for it and nothing to roll up. The Feature reports no forecast at all - not a partial one built
        /// from the Teams that could be forecast, which reads as a whole one - exactly as it has since
        /// long before start dates existed. No new rule was written for this.
        /// </summary>
        // @driving_port @us-01 @error @real-io @contract-shape:bounded-change (AC-1.7, DDD-4)
        [Test]
        public async Task A_Feature_a_Team_cannot_be_forecast_for_carries_no_start_date_either()
        {
            var (portfolio, silentTeamName, silentTeamId) = await GivenAFeatureOneOfWhoseTeamsHasNeverDelivered();

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var shared = TheFeatureNamed(features, Shared);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheTeamsWithoutForecastOf(shared), Does.Contain(silentTeamName));
                Assert.That(TheStartSource(shared), Is.EqualTo("Unknown"));
                Assert.That(TheForecastedStart(shared, 85), Is.Null);
                Assert.That(TheForecastedCompletion(shared, 85), Is.Null,
                    "the completion side already behaves this way and the start side inherits it.");

                // The per-team rows are new here, and they are where this could still go wrong: a team
                // with nothing measured has a completion row with no runs behind it, and every percentile
                // off that reads as day zero, which projects to today. Served as-is it would say this
                // team finishes today, in the same shape a real answer arrives in.
                Assert.That(TheForecastedStartFor(shared, silentTeamId, 85), Is.Null);
                Assert.That(TheForecastedCompletionFor(shared, silentTeamId, 85), Is.Null,
                    "a team that has never delivered does not get a date, least of all today's.");
            }
        }

        /// <summary>
        /// The same silent Team, on a Feature somebody has already started. The two facts arrive together
        /// and only one of them is a forecast: the Team with nothing measured stops us saying when work
        /// will begin, and says nothing about when work did begin. So the observed day is reported even
        /// though the Feature as a whole cannot be forecast.
        ///
        /// The client relies on this order. Its Forecasted Start column asks which source the server named
        /// before it decides whether to draw the cannot-forecast state, so flipping these two here would
        /// hide a date the client is holding.
        /// </summary>
        // @driving_port @us-01 @real-io @contract-shape:bounded-change (AC-1.7, DDD-4)
        [Test]
        public async Task A_started_Feature_reports_the_day_it_began_even_when_a_Team_cannot_be_forecast()
        {
            var startedOn = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
            var (portfolio, silentTeamName, _) = await GivenAFeatureOneOfWhoseTeamsHasNeverDelivered(startedOn);

            await WhenTheForecastRuns(portfolio);

            var features = await TheFeaturesAsTheClientSeesThem(portfolio.Id);
            var shared = TheFeatureNamed(features, Shared);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheTeamsWithoutForecastOf(shared), Does.Contain(silentTeamName),
                    "the Team still cannot be forecast - that has not changed.");
                Assert.That(TheStartSource(shared), Is.EqualTo("Observed"));
                Assert.That(TheObservedStart(shared), Is.EqualTo(startedOn));
                Assert.That(TheForecastedCompletion(shared, 85), Is.Null,
                    "the completion side is a forecast and still has nothing to say.");
            }
        }
    }
}
