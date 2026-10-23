namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// After a quiet round skipped the forecast of "Payments Platform", any change to what that forecast
    /// reads makes the next background round forecast it in full again, including a change that a refresh
    /// downloading nothing still brings in, and a change to a Feature of another Portfolio that one of its
    /// Teams works on.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("us-01")]
    public class Slice01InputChangeScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        private static readonly DateTimeOffset TenThirty = TenFifteen + AQuarterOfAnHour;

        public enum InputChange
        {
            TheDayHasTurned,
            ATeamsFeatureWipSettingMovedFromTwoToThree,
            ATeamsThroughputWindowGrew,
            ABlackoutTookADayOutOfATeamsThroughput,
            TheForecastFilterNowExcludesItems,
            ATeamNoLongerHasEnoughData,
            ThePortfoliosDefaultFeatureSizeChanged,
            AFeatureGainedADependency,
            TwoFeaturesSwappedOrder,
            AFeatureJoinedThePortfolio,
            AFeatureLeftThePortfolio,
            ATeamJoinedThePortfolio,
            ATeamLeftThePortfolio,
        }

        [TestCase(InputChange.TheDayHasTurned)]
        [TestCase(InputChange.ATeamsFeatureWipSettingMovedFromTwoToThree)]
        [TestCase(InputChange.ATeamsThroughputWindowGrew)]
        [TestCase(InputChange.ABlackoutTookADayOutOfATeamsThroughput)]
        [TestCase(InputChange.TheForecastFilterNowExcludesItems)]
        [TestCase(InputChange.ATeamNoLongerHasEnoughData)]
        [TestCase(InputChange.ThePortfoliosDefaultFeatureSizeChanged)]
        [TestCase(InputChange.AFeatureGainedADependency)]
        [TestCase(InputChange.TwoFeaturesSwappedOrder)]
        [TestCase(InputChange.AFeatureJoinedThePortfolio)]
        [TestCase(InputChange.AFeatureLeftThePortfolio)]
        [TestCase(InputChange.ATeamJoinedThePortfolio)]
        [TestCase(InputChange.ATeamLeftThePortfolio)]
        [Category("driving_port")]
        [Category("kpi-OUT-6354-no-stale-forecast")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_background_round_after_an_input_changed_forecasts_in_full(InputChange change)
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            var nextRound = await TheNextBackgroundRoundAfter(change);

            ThenPaymentsPlatformWasForecastInFull(nextRound, $"{change} since the last forecast");
        }

        /// <summary>
        /// The reporter's rounds scan the Team's query and download nothing. An item that left the query is
        /// still taken out by such a round, and the remaining work of its Feature drops, so "nothing was
        /// downloaded" cannot be read as "nothing changed".
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6354-no-stale-forecast")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_background_round_after_an_item_left_a_Teams_query_forecasts_in_full_though_it_downloaded_nothing()
        {
            GivenTheTrackerCanBeScanned();
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
            var nextRound = await TheTeamRoundAt(TenThirty, FssId);

            var line = TheLineOfTheTeamRound(nextRound, Fss);
            Assert.That(line, Does.Contain(" | mode=Delta | ").And.Contain(" | fetched=0 | "),
                $"The round that took the item out downloaded something, so it is not the case this scenario is about. It read: {line ?? "(no line)"}");
            ThenPaymentsPlatformWasForecastInFull(nextRound, "An item left FSS's query");
        }

        /// <summary>
        /// A forecast of Payments Platform forecasts every Feature its Teams work on, Loyalty's included, so a
        /// change to Loyalty's Feature is a change to what Payments Platform's forecast reads.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6354-no-stale-forecast")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_background_round_after_a_Feature_of_another_Portfolio_its_Teams_work_on_changed_forecasts_in_full()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenLoyaltyWhichFssAlsoWorksOn();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenLoyaltyWasForecastOverTheFeaturesFssNowWorksOnAt(TenOClock + TimeSpan.FromMinutes(5));
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            Tracker.NoLongerShowsTheTeam(Fss, "FSS-21");
            var nextRound = await TheTeamRoundAt(TenThirty, FssId);

            ThenPaymentsPlatformWasForecastInFull(nextRound, "An item of Loyalty's Feature, worked on by FSS, was finished");
        }

        // --- The change, and the background round that brings it in ---

        private Task<ObservedRounds> TheNextBackgroundRoundAfter(InputChange change)
        {
            switch (change)
            {
                case InputChange.TheDayHasTurned:
                    return TheTeamRoundAt(new DateTimeOffset(2026, 10, 24, 0, 10, 0, TimeSpan.Zero), FssId);
                case InputChange.ATeamsFeatureWipSettingMovedFromTwoToThree:
                    ChangeTheTeam(FssId, team => team.FeatureWIP = 3);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ATeamsThroughputWindowGrew:
                    TheTeamDelivers(FssId, [.. FssDelivers, .. FssDelivers]);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ABlackoutTookADayOutOfATeamsThroughput:
                    TheTeamDelivers(FssId, FssDelivers[..^1]);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.TheForecastFilterNowExcludesItems:
                    TheTeamDelivers(FssId, [.. FssDelivers.Select(day => Math.Max(0, day - 1))], filterApplied: true, excludedSummary: "4 Items excluded: Bug");
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ATeamNoLongerHasEnoughData:
                    TheTeamDelivers(FssId, FssDelivers, hasSufficientData: false);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ThePortfoliosDefaultFeatureSizeChanged:
                    ChangeThePortfolio(PaymentsPlatformId, portfolio => portfolio.DefaultAmountOfWorkItemsPerFeature = 30);
                    return ThePortfolioRoundAt(TenThirty);
                case InputChange.AFeatureGainedADependency:
                    TheFeatureNowWaitsFor("PAY-90", "PAY-88");
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.TwoFeaturesSwappedOrder:
                    Tracker.Reorders(PaymentsPlatform, "PAY-88", "2");
                    Tracker.Reorders(PaymentsPlatform, "PAY-90", "1");
                    return ThePortfolioRoundAt(TenThirty);
                case InputChange.AFeatureJoinedThePortfolio:
                    Tracker.HoldsFeature(PaymentsPlatform, "PAY-99", "Chargebacks", order: "4");
                    Tracker.HoldsItems(Fss, "PAY-99", "FSS-9");
                    return TheBackgroundCycleAt(TenThirty, FssId);
                case InputChange.AFeatureLeftThePortfolio:
                    Tracker.NoLongerHoldsFeature(PaymentsPlatform, "PAY-97");
                    return ThePortfolioRoundAt(TenThirty);
                case InputChange.ATeamJoinedThePortfolio:
                    Tracker.HoldsItems(Orion, "PAY-88", "ORN-1", "ORN-2");
                    return TheBackgroundCycleAt(TenThirty, OrionId);
                case InputChange.ATeamLeftThePortfolio:
                    Tracker.NoLongerShowsTheTeam(Atlas);
                    return TheBackgroundCycleAt(TenThirty, AtlasId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change, null);
            }
        }
    }
}
