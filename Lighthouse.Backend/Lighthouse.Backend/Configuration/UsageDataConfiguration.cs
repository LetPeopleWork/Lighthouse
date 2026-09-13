namespace Lighthouse.Backend.Configuration
{
    public class UsageDataConfiguration
    {
        public const string SectionName = "UsageData";

        /// <summary>
        /// How long a browser's consent keeps counting after it was last seen. Clearing browser
        /// storage sends nothing, so a browser that has gone away can only be noticed by its
        /// silence - this is how long that silence has to last.
        /// </summary>
        public int ConsentLivenessWindowDays { get; set; } = 30;

        /// <summary>
        /// Where usage data is sent. Deliberately empty until somebody says otherwise: an instance
        /// that was never told where to send does not send at all. Every real deployment supplies
        /// this - the chart renders it, Docker and standalone set it - and nothing that starts this
        /// application in a test does, which is what keeps runs of the test suite from adding
        /// invented events to the numbers real usage is counted in.
        /// </summary>
        public string? CollectorBaseUrl { get; set; }

        /// <summary>
        /// What the collector recognises this product's own project by. It travels in each message
        /// rather than in a header, because that is the shape the collector reads it in.
        /// </summary>
        public string? ProjectApiKey { get; set; }
    }
}
