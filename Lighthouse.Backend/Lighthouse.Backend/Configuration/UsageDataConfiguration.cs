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
    }
}
