namespace Lighthouse.Backend.Models.Metrics
{
    /// <summary>
    /// What a request's optional filter choice means for the throughput a forecast reads: yes applies the
    /// Team's forecast filter, no skips it, and leaving it out keeps whatever the Team is set to.
    /// </summary>
    public static class ThroughputFilterOverride
    {
        public static ThroughputFilterMode ToFilterMode(bool? applyFilterOverride)
        {
            return applyFilterOverride switch
            {
                true => ThroughputFilterMode.ApplyFilter,
                false => ThroughputFilterMode.SkipFilter,
                null => ThroughputFilterMode.RespectTeamSetting,
            };
        }
    }
}
