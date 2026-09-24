namespace Lighthouse.Backend.Models.OptionalFeatures
{
    public static class OptionalFeatureKeys
    {
        public const string LighthouseChartKey = "LighthouseChart";

        public const string CycleTimeScatterPlotKey = "CycleTimeScatterPlot";
        
        public const string McpServerKey = "McpServer";

        public const string LinearIntegrationKey = "LinearIntegration";

        /// <summary>
        /// The row older releases stored the Faster Updates switch under. Every refresh that can fetch only
        /// what changed now does so, and nothing reads this row; the key is kept only so start-up can find
        /// the row an upgraded instance still carries and remove it.
        /// </summary>
        public const string DeltaSyncKey = "DeltaSync";

        /// <summary>
        /// Who decides the order Features are forecast in: on means this instance keeps the order an
        /// administrator arranged, off means the work tracking system's own ranking wins on every refresh.
        /// </summary>
        public const string FeatureOrderingKey = "FeatureOrdering";

        /// <summary>
        /// An administrator's veto over usage data, and the one setting in this list that reads
        /// backwards: on means nothing is asked and nothing is sent, for everybody on this instance,
        /// whatever each person answered for themselves. It ships off, so an instance behaves as it
        /// always has until somebody deliberately stops it.
        /// </summary>
        public const string UsageDataKey = "UsageData";
    }
}
