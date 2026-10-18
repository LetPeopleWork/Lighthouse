using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// A simulated run that does not finish within the horizon still counts. It counts as beyond the
    /// horizon, so a percentile is beyond exactly when fewer than that share of runs finished, and a
    /// likelihood is the share of every run that finished by the date - never the share of the lucky ones.
    ///
    /// The share of runs that finish is chosen by the draw source: a Team whose history is one item and
    /// then nothing delivers every day in the runs that draw the first day and never moves in the others.
    /// Five items therefore finish on day five in those runs and never in the rest.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("us-03")]
    public class ForecastBeyondTheHorizonTest
    {
        private const int Trials = 100;

        private const int TheDaysARunMayCover = 30;

        private const int FiveItems = 5;

        private const int TheDayTheFinishingRunsFinish = 5;

        private const int FiveYearsOfSimulatedDays = 5 * 365 + 2;

        private static readonly int[] OneItemThenNothing = [1, 0];

        private static readonly int[] OneItemEveryTenDays = [1, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        private static readonly int[] OneADay = [1];

        private static readonly int[] AnUnevenWeek = [3, 0, 1, 2, 0, 4, 1];

        private static readonly int[] TheFourPercentiles = [50, 70, 85, 95];

        private static readonly DateOnly Today = new(2026, 10, 18);

        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web);

        private static readonly int[] EveryShareOfRunsThatCanFallShort = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        private static readonly int[] EveryShareOfRuns = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        [Test]
        [Category("slice-03")]
        [Category("ac-03.1")]
        [Category("property")]
        [Category("contract-shape:bounded-change")]
        public async Task Every_run_counts_whether_or_not_it_finished_and_the_ones_that_did_not_are_beyond_the_horizon()
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var tenthsThatFinish in EveryShareOfRunsThatCanFallShort)
                {
                    var forecast = await TheForecastOfFiveItemsWhen(tenthsThatFinish);

                    Assert.That(forecast.TotalTrials, Is.EqualTo(Trials),
                        $"{tenthsThatFinish * 10}% of runs finished, and the runs that did not were left out of the count.");
                    Assert.That(TheRunsThatFinished(forecast), Is.EqualTo(tenthsThatFinish * Trials / 10),
                        "Only the runs that finished have a day to be recorded on.");
                }
            }
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.1")]
        [Category("ac-03.4")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_forecast_whose_every_run_finishes_has_nothing_beyond_the_horizon()
        {
            var forecast = await TheForecastOfFiveItemsWhen(tenthsThatFinish: 10);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.TotalTrials, Is.EqualTo(Trials));
                Assert.That(TheRunsThatFinished(forecast), Is.EqualTo(Trials));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(TheDayTheFinishingRunsFinish));

                foreach (var percentile in TheFourPercentiles)
                {
                    Assert.That(TheBeyondFlagOnTheWire(forecast, percentile), Is.False,
                        $"Every run finished, and the {percentile}% percentile did not say it has a day.");
                }
            }
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.1")]
        [Category("ac-03.4")]
        [Category("property")]
        [Category("contract-shape:bounded-change")]
        public async Task A_percentile_is_beyond_the_horizon_exactly_when_fewer_than_that_share_of_runs_finished()
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var tenthsThatFinish in EveryShareOfRuns)
                {
                    var forecast = await TheForecastOfFiveItemsWhen(tenthsThatFinish);

                    foreach (var percentile in TheFourPercentiles)
                    {
                        Assert.That(TheBeyondFlagOnTheWire(forecast, percentile), Is.EqualTo(tenthsThatFinish * 10 < percentile),
                            $"{tenthsThatFinish * 10}% of runs finished; the {percentile}% percentile.");
                    }
                }
            }
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.2")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_no_run_can_finish_is_never_shown_as_due_today()
        {
            var forecast = await TheForecastOfFiveItemsWhen(tenthsThatFinish: 0);

            using (Assert.EnterMultipleScope())
            {
                foreach (var percentile in TheFourPercentiles)
                {
                    var onTheWire = new WhenForecastDto(forecast, percentile, Today, []);

                    Assert.That(DateOnly.FromDateTime(onTheWire.ExpectedDate), Is.EqualTo(Today.AddYears(5)),
                        $"No run finished, and the {percentile}% percentile did not read the horizon's stand-in date, five years from today.");
                }
            }
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_likelihood_counts_the_runs_that_did_not_finish_as_late()
        {
            var forecast = await TheForecastOfFiveItemsWhen(tenthsThatFinish: 6);

            Assert.That(forecast.GetLikelihood(TheDaysARunMayCover), Is.EqualTo(60).Within(0.001),
                "Six in ten runs finished by the date; the other four never did, so the date is 60% likely, not certain.");
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.1")]
        [Category("ac-03.4")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_that_waits_behind_one_that_never_finishes_counts_every_run_in_its_start_forecast()
        {
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, OneItemThenNothing);
            var first = OnePortfolioForecast.AFeature(1, "Billing Migration", "10", (team, FiveItems));
            var second = OnePortfolioForecast.AFeature(2, "Invoice PDF", "20", (team, FiveItems));

            using var draws = new CountedDrawStream(runsInTenThatDrawTheFirstDay: 6);
            await forecast.RunsOver(draws, OnePortfolioForecast.Limits(Trials, TheDaysARunMayCover), first, second);

            var whenWorkOnTheSecondBegins = second.StartForecasts.Single(start => start.TeamId is null);

            var startOnTheWire = JsonSerializer.SerializeToNode(
                new FeatureStartDto(second.WhenWorkBegins, Today, [], 50, 85), AsTheWireSendsIt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whenWorkOnTheSecondBegins.TotalTrials, Is.EqualTo(Trials),
                    "Work on the second Feature never begins in the runs where the first never finishes, and those runs were dropped.");
                Assert.That(startOnTheWire?["percentiles"]?[0]?["beyondHorizon"]?.GetValue<bool>(), Is.False,
                    "Work on the second Feature begins in six of every ten runs, so its 50% start has a day.");
                Assert.That(startOnTheWire?["percentiles"]?[1]?["beyondHorizon"]?.GetValue<bool>(), Is.True,
                    "Work on the second Feature begins in only six of every ten runs, so its 85% start is beyond the horizon.");
                Assert.That(startOnTheWire?["percentiles"]?[1]?["horizonYears"]?.GetValue<int>(), Is.EqualTo(5));
            }
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.1")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Runs_that_end_because_two_Features_wait_on_each_other_count_as_beyond_the_horizon()
        {
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, OneADay);
            var billing = OnePortfolioForecast.AFeature(1, "Billing Migration", "10", (team, FiveItems));
            var ledger = OnePortfolioForecast.AFeature(2, "Legacy Ledger Retirement", "20", (team, FiveItems));
            var aCircle = new WaitsHandedStraightToTheForecast(
                Waits.On(billing.ReferenceId, ledger.ReferenceId).And(ledger.ReferenceId, billing.ReferenceId));

            using var draws = new CountedDrawStream();
            await forecast.RunsOver(draws, OnePortfolioForecast.Limits(Trials, TheDaysARunMayCover), aCircle, billing, ledger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(billing.Forecast.TotalTrials, Is.EqualTo(Trials),
                    "No run could start either Feature, and the runs were dropped instead of counted as beyond.");
                Assert.That(TheBeyondFlagOnTheWire(billing.Forecast, 50), Is.True);
            }
        }

        [Test]
        [Category("slice-03")]
        [Category("ac-03.6")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_two_Teams_share_is_beyond_the_horizon_wherever_either_Team_falls_short()
        {
            var forecast = new OnePortfolioForecast();
            var payments = forecast.ATeamThatDelivers(1, OneItemThenNothing);
            var platform = forecast.ATeamThatDelivers(2, OneADay);
            var billing = OnePortfolioForecast.AFeature(1, "Billing Migration", "10", (payments, FiveItems), (platform, FiveItems));

            using var draws = new CountedDrawStream(runsInTenThatDrawTheFirstDay: 6);
            await forecast.RunsOver(draws, OnePortfolioForecast.Limits(Trials, TheDaysARunMayCover), billing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(billing.Forecast.TotalTrials, Is.EqualTo(Trials),
                    "The forecast combined across the two Teams dropped the runs Payments never finished.");
                Assert.That(TheRunsThatFinished(billing.Forecast), Is.EqualTo(Trials * 6 / 10),
                    "Platform finished every run and Payments six in ten, so the Feature finished six in ten.");
                Assert.That(TheBeyondFlagOnTheWire(billing.Forecast, 50), Is.False,
                    "Payments finished six of every ten runs and Platform all of them, so the 50% percentile has a day.");
                Assert.That(TheBeyondFlagOnTheWire(billing.Forecast, 70), Is.True,
                    "Payments never finished in four of every ten runs, so the Feature did not either.");
            }
        }

        /// <summary>
        /// A Team that has never delivered anything is not simulated at all. It keeps reading "Cannot
        /// forecast", which says something different from "more than five years". Already true; kept running.
        /// </summary>
        [Test]
        [Category("slice-03")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Team_with_no_delivery_history_still_cannot_be_forecast_rather_than_being_beyond_the_horizon()
        {
            var forecast = new OnePortfolioForecast();
            var silent = forecast.ATeamThatHasNeverDeliveredAnything(1);
            var feature = OnePortfolioForecast.AFeature(1, "Invoice PDF", "10", (silent, FiveItems));

            using var draws = new CountedDrawStream();
            await forecast.RunsOver(draws, OnePortfolioForecast.Limits(Trials, TheDaysARunMayCover), feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecast.TotalTrials, Is.Zero);
                Assert.That(draws.RunsBegun, Is.Zero, "A Team with nothing to draw from was simulated anyway.");
            }
        }

        [Test]
        [Category("slice-04")]
        [Category("ac-04.1")]
        [Category("contract-shape:pure-function")]
        public void The_product_lets_one_run_cover_five_years_of_simulated_days()
        {
            Assert.That(ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover, Is.EqualTo(FiveYearsOfSimulatedDays));
        }

        [Test]
        [Category("slice-04")]
        [Category("ac-04.1")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_that_would_take_centuries_stops_being_simulated_after_five_years()
        {
            var forecast = new OnePortfolioForecast();
            var payments = forecast.ATeamThatDelivers(1, OneItemEveryTenDays);
            var billing = OnePortfolioForecast.AFeature(1, "Billing Migration", "10", (payments, 480));

            // Every run draws the empty days of that history, so no run ever finishes the 480 items.
            using var draws = new CountedDrawStream(runsInTenThatDrawTheFirstDay: 0);
            await forecast.RunsOver(draws, ForecastSimulationLimits.Default with { Trials = 20, CoresSeen = 4 }, billing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(draws.FurthestDayDrawn, Is.LessThanOrEqualTo(FiveYearsOfSimulatedDays),
                    "A run went on simulating past five years.");
                Assert.That(TheBeyondFlagOnTheWire(billing.Forecast, 85), Is.True);
            }
        }

        /// <summary>
        /// A Feature whose every run finishes well inside five years gives the same dates under the shorter
        /// horizon. Already true; kept running as the guard that lowering the horizon moves no near-term date.
        /// </summary>
        [Test]
        [Category("slice-04")]
        [Category("ac-04.3")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task Near_term_Features_keep_their_dates_under_the_product_horizon()
        {
            var underTheProductHorizon = await TheSeededPercentilesWith(ForecastSimulationLimits.Default with { Trials = 1_000, CoresSeen = 4 });
            var underTheOldHorizon = await TheSeededPercentilesWith(OnePortfolioForecast.Limits(1_000, 100_000));

            Assert.That(underTheProductHorizon, Is.EqualTo(underTheOldHorizon));
        }

        private static async Task<WhenForecast> TheForecastOfFiveItemsWhen(int tenthsThatFinish)
        {
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, OneItemThenNothing);
            var feature = OnePortfolioForecast.AFeature(1, "Billing Migration", "10", (team, FiveItems));

            using var draws = new CountedDrawStream(tenthsThatFinish);
            await forecast.RunsOver(draws, OnePortfolioForecast.Limits(Trials, TheDaysARunMayCover), feature);

            return feature.Forecast;
        }

        private static async Task<List<int>> TheSeededPercentilesWith(ForecastSimulationLimits limits)
        {
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, AnUnevenWeek);
            var feature = OnePortfolioForecast.AFeature(1, "Invoice PDF", "10", (team, 40));

            await forecast.RunsOver(new DrawsFromAPinnedStartingNumber(6311), limits, feature);

            return TheFourPercentiles.Select(feature.Forecast.GetProbability).ToList();
        }

        private static int TheRunsThatFinished(WhenForecast forecast)
            => forecast.SimulationResults.Sum(day => day.Value);

        /// <summary>The flag as a client reads it: the JSON property, so a missing one reads as no answer.</summary>
        private static bool? TheBeyondFlagOnTheWire(ForecastBase forecast, int percentile)
        {
            var onTheWire = JsonSerializer.SerializeToNode(new WhenForecastDto(forecast, percentile, Today, []), AsTheWireSendsIt);

            return onTheWire?["beyondHorizon"]?.GetValue<bool>();
        }
    }
}
