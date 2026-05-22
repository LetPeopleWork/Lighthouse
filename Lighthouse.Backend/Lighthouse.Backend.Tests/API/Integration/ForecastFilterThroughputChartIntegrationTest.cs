// SCAFFOLD: true
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Wave: DISTILL — RED scaffold for filter-forecast-throughput Slice 03.
    /// Drives US-05 (PBC chart ?view=raw|filtered query param per DDD-5),
    /// invariant #1 (default Raw behaviour preserved).
    /// </summary>
    [TestFixture]
    public class ForecastFilterThroughputChartIntegrationTest
    {
        [Test]
        public void GetThroughputPbc_PremiumTenantTeamWithFilterAndViewFiltered_ReturnsFilteredCounts()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-05 / DDD-5 PBC half). DELIVER wave: ?view=filtered triggers ThroughputFilterMode.ApplyFilter on the GetBlackoutAwareThroughputForTeam seam.");
        }

        [Test]
        public void GetThroughputPbc_PremiumTenantTeamWithFilterAndViewRaw_ReturnsUnfilteredCounts()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-05). DELIVER wave: ?view=raw skips the filter regardless of persisted rule set.");
        }

        [Test]
        public void GetThroughputPbc_PremiumTenantTeamWithFilterAndQueryParamOmitted_DefaultsToRaw()
        {
            Assert.Fail("Not yet implemented — RED scaffold (invariant #1 default chart Raw). DELIVER wave: missing ?view defaults to raw — preserves today's behaviour.");
        }

        [Test]
        public void GetThroughputPbc_NonPremiumTenantTeamWithViewFiltered_SilentlyReturnsRaw()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-07 license downgrade). DELIVER wave: license read-path gate suppresses the filter regardless of query param.");
        }

        [Test]
        public void GetThroughput_PremiumTenantTeamWithFilter_ReturnsPerItemGranularPayloadForClientSideFilter()
        {
            Assert.Fail("Not yet implemented — RED scaffold (US-05 / DDD-5 Run Chart half). DELIVER wave: GET /api/teamMetrics/{teamId}/throughput is unchanged; payload remains per-item granular (RunChartData.WorkItemsPerUnitOfTime carries WorkItemBase per day) so the FE filters client-side.");
        }
    }
}
