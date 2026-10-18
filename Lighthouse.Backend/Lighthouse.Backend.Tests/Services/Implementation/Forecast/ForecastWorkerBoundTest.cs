using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// A forecast runs its simulated runs on at most half the cores the instance was given, and never on
    /// fewer than one, so the pages a person is reading keep a core to answer on while it runs. The bound is
    /// per simulation: a manual forecast asked for during a background one gets its own.
    ///
    /// The core count is handed in. A test that read it off the machine would assert a different bound on
    /// a laptop than on the CI runner. How many runs are under way at once is seen by holding every run as
    /// it begins: each held run is a worker, so the count of held runs is the number of workers the forecast
    /// had, and it is read once the expected number are parked rather than after any wait.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-01")]
    [Category("us-01")]
    public class ForecastWorkerBoundTest
    {
        private const int Trials = 200;

        private const int AFewDaysOfWork = 3;

        private const int EnoughThreadsThatThePoolIsNeverTheLimit = 64;

        private static readonly TimeSpan LongEnoughForTheWorkersToStart = TimeSpan.FromSeconds(20);

        private static readonly int[] OneADay = [1];

        private static readonly int[] AnUnevenWeek = [3, 0, 1, 2, 0, 4, 1];

        private static readonly int[] TheFourPercentiles = [50, 70, 85, 95];

        private int threadsThePoolKeptReady;

        private int ioThreadsThePoolKeptReady;

        [SetUp]
        public void SetUp()
        {
            ThreadPool.GetMinThreads(out threadsThePoolKeptReady, out ioThreadsThePoolKeptReady);
            ThreadPool.SetMinThreads(
                Math.Max(threadsThePoolKeptReady, EnoughThreadsThatThePoolIsNeverTheLimit),
                ioThreadsThePoolKeptReady);
        }

        [TearDown]
        public void TearDown()
        {
            ThreadPool.SetMinThreads(threadsThePoolKeptReady, ioThreadsThePoolKeptReady);
        }

        /// <summary>
        /// The bound for the core counts an instance plausibly reports, as a table of examples: half, rounded
        /// down, and at least one, so a one-core host still forecasts. What a forecast actually does with it is
        /// the next scenario.
        /// </summary>
        [Category("ac-01.2")]
        [Category("ac-01.3")]
        [Category("edge")]
        [Category("contract-shape:pure-function")]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(5, 2)]
        [TestCase(8, 4)]
        [TestCase(16, 8)]
        [TestCase(31, 15)]
        [TestCase(32, 16)]
        [TestCase(64, 32)]
        public void The_runs_allowed_at_once_are_half_the_cores_and_never_fewer_than_one(int cores, int runsAtOnce)
        {
            var limits = OnePortfolioForecast.Limits(Trials, 100, cores);

            Assert.That(limits.MostRunsAtOnce, Is.EqualTo(runsAtOnce), $"An instance given {cores} cores.");
        }

        /// <summary>
        /// Every run is held as it begins, so the forecast has as many runs under way as it has workers. It
        /// must reach the bound, which proves the runs really were spread over that many, and never pass it.
        /// </summary>
        [Test]
        [Category("ac-01.2")]
        [Category("ac-01.3")]
        [Category("contract-shape:bounded-change")]
        [TestCase(1, 1, TestName = "A_one_core_host_runs_one_run_at_a_time")]
        [TestCase(2, 1, TestName = "A_two_core_host_runs_one_run_at_a_time")]
        [TestCase(4, 2, TestName = "A_four_core_host_runs_two_runs_at_once")]
        public async Task A_forecast_runs_exactly_as_many_runs_at_once_as_half_the_cores(int cores, int workers)
        {
            using var draws = new CountedDrawStream();
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, OneADay);
            var feature = OnePortfolioForecast.AFeature(1, "Invoice PDF", "10", (team, AFewDaysOfWork));

            draws.HoldOnceRunsHaveBegun(0);
            var running = Task.Run(() => forecast.RunsOver(draws, OnePortfolioForecast.Limits(Trials, 100, cores), feature));

            var reachedTheBound = draws.IsHoldingWithin(workers, LongEnoughForTheWorkersToStart);
            draws.LetItCarryOn();
            await running;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reachedTheBound, Is.True,
                    $"An instance given {cores} cores never had {workers} runs under way at once.");
                Assert.That(draws.MostRunsHeldAtOnce, Is.EqualTo(workers),
                    $"An instance given {cores} cores ran {draws.MostRunsHeldAtOnce} runs at once, not {workers}, which leaves the pages nothing to answer on.");
                Assert.That(draws.RunsBegun, Is.EqualTo(Trials),
                    "Bounding how many run at once must not drop any of the runs.");
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(AFewDaysOfWork),
                    "A bounded forecast still finishes with the answer the work implies.");
            }
        }

        /// <summary>
        /// The answer does not depend on how many workers carried the runs: one worker against the widest the
        /// bound allows. No forecast runs unbounded, so this is the comparison there is to make. It holds
        /// because each draw is addressed by its run, Team and day, and it guards that bounding the runs
        /// keeps it so.
        /// </summary>
        [Test]
        [Category("ac-01.4")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task Bounding_the_runs_does_not_change_any_percentile_of_a_seeded_forecast()
        {
            var oneCore = await TheSeededPercentilesOnAHostWith(cores: 1);
            var manyCores = await TheSeededPercentilesOnAHostWith(cores: 64);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(oneCore, Has.Count.EqualTo(TheFourPercentiles.Length));
                Assert.That(oneCore, Has.All.GreaterThan(0), "The seeded forecast gave no dates to compare.");
                Assert.That(oneCore, Is.EqualTo(manyCores),
                    "Two forecasts over the same data and the same starting number gave different dates depending on the cores.");
            }
        }

        /// <summary>
        /// The manual forecast a Team page asks for goes through the same simulation, so it is held to the
        /// same bound, of its own.
        /// </summary>
        [Test]
        [Category("ac-01.2")]
        [Category("contract-shape:bounded-change")]
        public async Task A_manual_forecast_is_held_to_the_same_bound()
        {
            using var draws = new CountedDrawStream();
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, OneADay);

            draws.HoldOnceRunsHaveBegun(0);
            var running = Task.Run(() => forecast.AManualForecast(draws, OnePortfolioForecast.Limits(Trials, 100, cores: 2), team, AFewDaysOfWork));

            var oneIsUnderWay = draws.IsHoldingWithin(1, LongEnoughForTheWorkersToStart);
            draws.LetItCarryOn();
            WhenForecast answer = await running;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(oneIsUnderWay, Is.True, "The manual forecast never began a run.");
                Assert.That(draws.MostRunsHeldAtOnce, Is.EqualTo(1),
                    "A manual forecast on a two-core host ran several runs at once.");
                Assert.That(answer.GetProbability(85), Is.EqualTo(AFewDaysOfWork));
            }
        }

        private static async Task<List<int>> TheSeededPercentilesOnAHostWith(int cores)
        {
            var forecast = new OnePortfolioForecast();
            var team = forecast.ATeamThatDelivers(1, AnUnevenWeek);
            Feature feature = OnePortfolioForecast.AFeature(1, "Billing Migration", "10", (team, 40));

            await forecast.RunsOver(new DrawsFromAPinnedStartingNumber(6311), OnePortfolioForecast.Limits(1_000, 1_000, cores), feature);

            return TheFourPercentiles.Select(feature.Forecast.GetProbability).ToList();
        }
    }
}
