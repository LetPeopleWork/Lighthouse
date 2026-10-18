using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    /// <summary>
    /// A Feature worked by two Teams, and a Delivery over two Teams, read a forecast combined from each
    /// Team's own. When a Team finished only some of its runs, the runs it did not finish are later than
    /// any day, so the combination carries them too: it is beyond the horizon wherever either Team is, and
    /// its count of runs is the whole count, not just the ones that finished.
    ///
    /// Payments finished eight runs in ten by day 5 and Platform three in four by day 8, so together they
    /// finished 0.8 × 0.75 = six runs in ten by day 8. The 50% has a day; the 70% and above are beyond.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-03")]
    [Category("us-03")]
    [Category("ac-03.6")]
    public class AggregatedBeyondTheHorizonTest
    {
        private const int RunsEachTeamSimulated = 100;

        private const int PaymentsFinishOn = 5;

        private const int PlatformFinishOn = 8;

        private const double TheShareTheTwoTeamsLeaveUnfinished = 0.4;

        private static readonly int[] AboveTheFinishingShare = [70, 85, 95];

        private static readonly DateOnly Today = new(2026, 10, 18);

        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web);

        [Test]
        [Ignore(PendingEpic6311.Slice03)]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Feature_two_Teams_work_is_beyond_the_horizon_where_the_runs_they_finished_together_fall_short()
        {
            var payments = TeamNamed(1, "Payments");
            var platform = TeamNamed(2, "Platform");
            var billing = AFeatureWorkedBy(1, (payments, PaymentsFinishOn, 80), (platform, PlatformFinishOn, 75));

            TheCombinationCarriesTheUnfinishedRuns(billing.Forecast);
        }

        [Test]
        [Ignore(PendingEpic6311.Slice03)]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Delivery_over_two_Teams_is_beyond_the_horizon_where_the_runs_they_finished_together_fall_short()
        {
            var payments = TeamNamed(1, "Payments");
            var platform = TeamNamed(2, "Platform");
            var billing = AFeatureWorkedBy(1, (payments, PaymentsFinishOn, 80));
            var invoices = AFeatureWorkedBy(2, (platform, PlatformFinishOn, 75));

            var delivery = DeliveryCompletionForecast.Build([billing, invoices]);

            Assert.That(delivery, Is.Not.Null, "Both Teams have a forecast, so the Delivery has one too.");
            TheCombinationCarriesTheUnfinishedRuns(delivery!);
        }

        /// <summary>
        /// Already true, and kept running: with every run of both Teams finished there is nothing beyond the
        /// horizon to carry, and the combination reads the later Team's day.
        /// </summary>
        [Test]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        public void A_Feature_whose_two_Teams_finish_every_run_reads_the_later_Teams_day()
        {
            var billing = AFeatureWorkedBy(1,
                (TeamNamed(1, "Payments"), PaymentsFinishOn, RunsEachTeamSimulated),
                (TeamNamed(2, "Platform"), PlatformFinishOn, RunsEachTeamSimulated));

            var forecast = billing.Forecast;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.TotalTrials, Is.GreaterThan(0));
                Assert.That(forecast.SimulationResults.Sum(day => day.Value), Is.EqualTo(forecast.TotalTrials));
                Assert.That(forecast.GetProbability(95), Is.EqualTo(PlatformFinishOn));
            }
        }

        private static void TheCombinationCarriesTheUnfinishedRuns(WhenForecast combined)
        {
            var finished = combined.SimulationResults.Sum(day => day.Value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(combined.TotalTrials, Is.GreaterThan(0), "The combination counted no runs at all.");
                Assert.That((combined.TotalTrials - finished) / (double)combined.TotalTrials,
                    Is.EqualTo(TheShareTheTwoTeamsLeaveUnfinished).Within(0.01),
                    "The runs either Team left unfinished were dropped when the two were combined.");
                Assert.That(combined.SimulationResults.Select(day => day.Key), Has.None.EqualTo(int.MaxValue),
                    "The bucket that carries the unfinished runs leaked into the days a reader projects.");
                Assert.That(TheBeyondFlagOnTheWire(combined, 50), Is.False,
                    "Six runs in ten finished, so the 50% percentile has a day.");
                Assert.That(combined.GetProbability(50), Is.EqualTo(PlatformFinishOn));

                foreach (var percentile in AboveTheFinishingShare)
                {
                    Assert.That(TheBeyondFlagOnTheWire(combined, percentile), Is.True,
                        $"Only six runs in ten finished, and the {percentile}% percentile was given a date anyway.");
                }
            }
        }

        private static bool? TheBeyondFlagOnTheWire(WhenForecast forecast, int percentile)
        {
            var onTheWire = JsonSerializer.SerializeToNode(new WhenForecastDto(forecast, percentile, Today, []), AsTheWireSendsIt);

            return onTheWire?["beyondHorizon"]?.GetValue<bool>();
        }

        private static Team TeamNamed(int id, string name) => new() { Id = id, Name = name };

        private static Feature AFeatureWorkedBy(int id, params (Team Team, int Day, int Finished)[] work)
        {
            var feature = new Feature(work.Select(entry => (entry.Team, 10, 12))) { Id = id, Name = $"Feature {id}" };

            feature.SetFeatureForecasts(work.Select(entry => new WhenForecast(new Dictionary<int, int> { [entry.Day] = entry.Finished })
            {
                Team = entry.Team,
                TeamId = entry.Team.Id,
                NumberOfItems = 10,
                HasSufficientData = true,
                TotalTrials = RunsEachTeamSimulated,
            }));

            return feature;
        }
    }
}
