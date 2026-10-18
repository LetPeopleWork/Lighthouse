using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates
{
    /// <summary>
    /// Tomás reads "Billing Migration" in the Feature list. Some of its simulated runs never finish, so its
    /// 50% has a date and the percentiles above it say the Feature is beyond the horizon. What reaches him
    /// is the wire every client reads: each percentile keeps its date, for clients that read only dates,
    /// and gains a flag and the horizon's years, for clients that can say "More than 5 years".
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6311-responsive-during-updates")]
    [Category("us-03")]
    public class Slice03BeyondTheHorizonScenarios : ForecastWhileItUpdatesAcceptanceTest
    {
        private const string BillingMigration = "Billing Migration";

        private static readonly int[] AboveTheFinishingShare = [70, 85, 95];

        private static readonly int[] TheFourPercentiles = [50, 70, 85, 95];

        private static readonly DateOnly FiveYearsFromToday = new(2031, 10, 18);

        [Test]
        [Ignore(PendingEpic6311.Slice03)]
        [Category("slice-03")]
        [Category("ac-03.1")]
        [Category("ac-03.4")]
        [Category("driving_port")]
        [Category("kpi-OUT-6311-KPI-4")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_whose_runs_do_not_all_finish_reads_a_date_at_50_and_beyond_the_horizon_above_it()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(BillingMigration);
            GivenRunsThatFinish(inEveryTen: 6);

            await WhenThePortfolioIsForecast(portfolio);
            var billing = await TheFeatureAsTheClientSeesIt(BillingMigration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(BeyondTheHorizon(ThePercentile(billing, 50)), Is.False,
                    "Six in ten runs finished, so the 50% percentile has a day.");

                foreach (var percentile in AboveTheFinishingShare)
                {
                    Assert.That(BeyondTheHorizon(ThePercentile(billing, percentile)), Is.True,
                        $"Only six in ten runs finished, and the {percentile}% percentile was given a date anyway.");
                }

                foreach (var percentile in TheFourPercentiles)
                {
                    Assert.That(ThePercentile(billing, percentile).TryGetProperty("horizonYears", out _), Is.True,
                        $"The {percentile}% percentile does not say how long the horizon is, so no client can word it.");
                    Assert.That(ThePercentile(billing, percentile).TryGetProperty("expectedDate", out _), Is.True,
                        "Clients that read only dates still need one on every percentile.");
                }
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice03)]
        [Category("slice-03")]
        [Category("ac-03.2")]
        [Category("error")]
        [Category("kpi-OUT-6311-KPI-4")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_no_run_can_finish_is_never_shown_as_due_today()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems("Legacy Ledger Retirement");
            GivenRunsThatFinish(inEveryTen: 0);

            await WhenThePortfolioIsForecast(portfolio);
            var ledger = await TheFeatureAsTheClientSeesIt("Legacy Ledger Retirement");

            using (Assert.EnterMultipleScope())
            {
                foreach (var percentile in TheFourPercentiles)
                {
                    Assert.That(BeyondTheHorizon(ThePercentile(ledger, percentile)), Is.True);
                    Assert.That(TheDateOf(ThePercentile(ledger, percentile)), Is.Not.EqualTo(DateOnly.FromDateTime(Now.UtcDateTime)),
                        $"No run finished, and the {percentile}% percentile still read today.");
                }
            }
        }

        /// <summary>
        /// Every run finishes, so nothing is beyond the horizon and the flag says so rather than being absent.
        /// </summary>
        [Test]
        [Ignore(PendingEpic6311.Slice03)]
        [Category("slice-03")]
        [Category("ac-03.4")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_whose_every_run_finishes_says_none_of_its_percentiles_is_beyond_the_horizon()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems("Invoice PDF");
            GivenRunsThatFinish(inEveryTen: 10);

            await WhenThePortfolioIsForecast(portfolio);
            var invoice = await TheFeatureAsTheClientSeesIt("Invoice PDF");

            using (Assert.EnterMultipleScope())
            {
                foreach (var percentile in TheFourPercentiles)
                {
                    Assert.That(BeyondTheHorizon(ThePercentile(invoice, percentile)), Is.False);
                }
            }
        }

        /// <summary>
        /// The stand-in date released clients read for a percentile beyond the horizon: today plus five years
        /// of days, counted on the calendar. The years come from the product's horizon, not from the limits
        /// this host was given, so a test that shrinks the runs still sees five.
        /// </summary>
        [Test]
        [Ignore(PendingEpic6311.Slice04)]
        [Category("slice-04")]
        [Category("ac-04.2")]
        [Category("ac-03.4")]
        [Category("contract-shape:bounded-change")]
        public async Task A_percentile_beyond_the_horizon_carries_five_years_and_a_stand_in_date_five_years_out()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems(BillingMigration);
            GivenRunsThatFinish(inEveryTen: 6);

            await WhenThePortfolioIsForecast(portfolio);
            var at85 = ThePercentile(await TheFeatureAsTheClientSeesIt(BillingMigration), 85);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(at85.TryGetProperty("horizonYears", out var years) ? years.GetInt32() : (int?)null, Is.EqualTo(5));
                Assert.That(TheDateOf(at85), Is.EqualTo(FiveYearsFromToday));
            }
        }

        private async Task WhenThePortfolioIsForecast(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            var stored = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolio.Id);
            Assert.That(stored, Is.Not.Null);

            await scope.ServiceProvider.GetRequiredService<IForecastService>().UpdateForecastsForPortfolio(stored!);
        }

        private static bool? BeyondTheHorizon(JsonElement percentile)
            => percentile.TryGetProperty("beyondHorizon", out var flag) && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? flag.GetBoolean()
                : null;

        private static DateOnly TheDateOf(JsonElement percentile)
            => DateOnly.FromDateTime(percentile.GetProperty("expectedDate").GetDateTime());
    }
}
