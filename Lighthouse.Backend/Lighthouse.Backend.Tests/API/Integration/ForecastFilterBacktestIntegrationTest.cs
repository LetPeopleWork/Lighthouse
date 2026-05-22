// SCAFFOLD: true
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Wave: DISTILL — RED scaffold for filter-forecast-throughput Slice 04.
    /// Drives US-06 (per-run override on Backtest).
    /// </summary>
    [TestFixture]
    public class ForecastFilterBacktestIntegrationTest
    {
        [Test]
        public void Backtest_PremiumTenantTeamWithFilterApplyOverrideFalse_RunsAgainstUnfilteredHistoricalThroughput()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-06). DELIVER wave: BacktestInputDto.ApplyFilterOverride=false → ThroughputFilterMode.SkipFilter on the GetBlackoutAwareThroughputForTeam seam.");
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithFilterApplyOverrideTrue_RunsAgainstFilteredHistoricalThroughput()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-06). DELIVER wave: ApplyFilterOverride=true → ApplyFilter mode.");
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithFilterApplyOverrideOmitted_DefaultsToFilteredViaTeamSetting()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-06 default semantics).");
        }

        [Test]
        public void Backtest_ResultDto_IncludesFilterAppliedAndExcludedSummaryForChip()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-03 chip data on backtest result). DELIVER wave: BacktestResultDto gains FilterApplied + ExcludedSummary.");
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithoutFilter_IgnoresOverrideAndReturnsUnfilteredResult()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-06 toggle hidden).");
        }
    }
}
