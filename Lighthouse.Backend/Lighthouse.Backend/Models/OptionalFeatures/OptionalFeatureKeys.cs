namespace Lighthouse.Backend.Models.OptionalFeatures
{
    public static class OptionalFeatureKeys
    {
        public const string LighthouseChartKey = "LighthouseChart";

        public const string CycleTimeScatterPlotKey = "CycleTimeScatterPlot";
        
        public const string McpServerKey = "McpServer";

        public const string LinearIntegrationKey = "LinearIntegration";

        /// <summary>
        /// Epic #5687 A1: delta ships dark. Off by default, preview, read per update in the update's own
        /// scope so a toggle takes effect on the next cycle without a restart. Removed once KPI-3 holds.
        /// </summary>
        public const string DeltaSyncKey = "DeltaSync";
    }
}
