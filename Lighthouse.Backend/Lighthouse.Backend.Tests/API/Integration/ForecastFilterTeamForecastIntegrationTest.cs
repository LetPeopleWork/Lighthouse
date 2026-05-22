// SCAFFOLD: true
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Wave: DISTILL — RED scaffold for filter-forecast-throughput Slice 02.
    /// Drives US-04 (per-run override on Team Forecast How Many / When).
    /// </summary>
    [TestFixture]
    public class ForecastFilterTeamForecastIntegrationTest
    {
        [Test]
        public void HowMany_PremiumTenantTeamWithFilterApplyOverrideFalse_ReturnsUnfilteredForecast()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-04). DELIVER wave: ManualForecastInputDto.ApplyFilterOverride=false → ThroughputFilterMode.SkipFilter on the GetCurrentThroughputForTeamForecast call.");
        }

        [Test]
        public void HowMany_PremiumTenantTeamWithFilterApplyOverrideTrue_ReturnsFilteredForecast()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-04). DELIVER wave: ApplyFilterOverride=true → ApplyFilter mode.");
        }

        [Test]
        public void HowMany_PremiumTenantTeamWithFilterApplyOverrideOmitted_DefaultsToFilteredViaTeamSetting()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-04 default semantics). DELIVER wave: omitted override on premium tenant with filter → default mode applies the filter.");
        }

        [Test]
        public void HowMany_PremiumTenantTeamWithoutFilter_IgnoresOverrideAndReturnsUnfilteredForecast()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-04 toggle hidden). DELIVER wave: team without filter → override field has no effect.");
        }

        [Test]
        public void HowMany_ResponsePayload_IncludesFilterAppliedAndExcludedSummary()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-03 chip data on team forecast). DELIVER wave: ManualForecastDto extension covers this surface.");
        }

        [Test]
        public void When_PremiumTenantTeamWithFilterApplyOverrideFalse_ReturnsUnfilteredForecast()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-04). DELIVER wave: parity with HowMany on the /when endpoint.");
        }
    }
}
