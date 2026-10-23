namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// After a quiet round skipped the forecast, each Feature that forecast covers reads as last updated at
    /// that round, with its dates as they were: the forecast was confirmed current then. A round that
    /// failed or was stopped confirmed nothing and changes nothing a reader sees.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-02")]
    [Category("us-02")]
    public class Slice02ConfirmedNowScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        private static readonly string[] TheFeaturesOfPaymentsPlatform = ["PAY-88", "PAY-90", "PAY-97"];

        /// <summary>
        /// Onboarding shares no Team with Payments Platform, so the round that confirmed Payments Platform
        /// read none of its Features and must leave their time alone.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_skipped_forecast_reads_as_confirmed_at_that_round_with_its_dates_unchanged()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenOnboardingWasForecastAt(TenOClock);
            var atTen = await TheFeaturesAsTheClientsReadThem([.. TheFeaturesOfPaymentsPlatform, "ONB-1"]);

            var roundStarted = DateTime.UtcNow;
            var quietRound = await TheTeamRoundAt(TenFifteen, FssId);
            var roundEnded = DateTime.UtcNow;

            var afterTheRound = await TheFeaturesAsTheClientsReadThem([.. TheFeaturesOfPaymentsPlatform, "ONB-1"]);

            ThenNothingWasSimulated(quietRound, "Nothing the forecast reads changed since 10:00");
            using (Assert.EnterMultipleScope())
            {
                foreach (var feature in TheFeaturesOfPaymentsPlatform)
                {
                    Assert.That(afterTheRound[feature].LastUpdated, Is.InRange(roundStarted, roundEnded),
                        $"{feature}'s forecast still reads as last updated at {afterTheRound[feature].LastUpdated:O}, not at the round that confirmed it.");
                    Assert.That(afterTheRound[feature].Forecasts, Is.EqualTo(atTen[feature].Forecasts),
                        $"Confirming {feature}'s forecast changed its dates.");
                }

                Assert.That(TheStoredForecastsOf(PaymentsPlatformId).Select(row => row.CreationTime), Is.All.InRange(roundStarted, roundEnded),
                    "A completion or start forecast of Payments Platform was left with the time it was computed.");
                Assert.That(afterTheRound["ONB-1"], Is.EqualTo(atTen["ONB-1"]),
                    "A Feature no Team of Payments Platform works on was stamped by its round.");
            }
        }

        /// <summary>
        /// Payments Platform's forecast covers every Feature its Teams work on, Loyalty's included, so its
        /// confirmation confirms that Feature too. Atlas's round causes no forecast of Loyalty itself.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_skipped_forecast_also_confirms_a_Feature_of_another_Portfolio_its_Teams_work_on()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenLoyaltyWhichFssAlsoWorksOn();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            var atTen = await TheFeaturesAsTheClientsReadThem("LOY-1");

            var roundStarted = DateTime.UtcNow;
            var quietRound = await TheTeamRoundAt(TenFifteen, AtlasId);
            var roundEnded = DateTime.UtcNow;

            var afterTheRound = await TheFeaturesAsTheClientsReadThem("LOY-1");

            ThenNothingWasSimulated(quietRound, "Nothing the forecast reads changed since 10:00");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheRound["LOY-1"].LastUpdated, Is.InRange(roundStarted, roundEnded),
                    "Loyalty's Feature, forecast and confirmed with Payments Platform, still reads the time it was computed.");
                Assert.That(afterTheRound["LOY-1"].Forecasts, Is.EqualTo(atTen["LOY-1"].Forecasts),
                    "Confirming Loyalty's Feature changed its dates.");
            }
        }

        /// <summary>A round whose forecast failed confirmed nothing, so nothing may read as confirmed by it.</summary>
        [Test]
        [Category("guard")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_round_whose_forecast_failed_changes_no_Features_time_or_dates()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            var atTen = await TheFeaturesAsTheClientsReadThem(TheFeaturesOfPaymentsPlatform);

            ReadingTheThroughputOfTheTeamFails(FssId);
            var failedRound = await TheTeamRoundAt(TenFifteen, FssId);

            ThenTheForecastOfTheRoundFailed(failedRound);
            Assert.That(await TheFeaturesAsTheClientsReadThem(TheFeaturesOfPaymentsPlatform), Is.EqualTo(atTen));
        }

        /// <summary>A round the operator stopped confirmed nothing either.</summary>
        [Test]
        [Category("guard")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_round_whose_forecast_was_stopped_changes_no_Features_time_or_dates()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            var atTen = await TheFeaturesAsTheClientsReadThem(TheFeaturesOfPaymentsPlatform);

            Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
            AForecastIsUnderWayAfterTheRoundAt(TenFifteen);
            await WhenTheOperatorStopsTheRunningForecast();
            await TheRunningForecastIsLetGo();

            Assert.That(await TheFeaturesAsTheClientsReadThem(TheFeaturesOfPaymentsPlatform), Is.EqualTo(atTen));
        }

        [Test]
        [Category("guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_computed_forecast_reads_as_last_updated_at_its_round_with_its_new_dates()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            var atTen = await TheFeaturesAsTheClientsReadThem(TheFeaturesOfPaymentsPlatform);

            Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
            var roundStarted = DateTime.UtcNow;
            await TheTeamRoundAt(TenFifteen, FssId);
            var roundEnded = DateTime.UtcNow;

            var afterTheRound = await TheFeaturesAsTheClientsReadThem(TheFeaturesOfPaymentsPlatform);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheFeaturesOfPaymentsPlatform.Select(feature => afterTheRound[feature].LastUpdated), Is.All.InRange(roundStarted, roundEnded));
                Assert.That(afterTheRound["PAY-88"].Forecasts, Is.Not.EqualTo(atTen["PAY-88"].Forecasts),
                    "Card Vault lost an item, yet its forecast dates did not move.");
            }
        }
    }
}
