using Lighthouse.Backend.Configuration;

namespace Lighthouse.Backend.Tests.Configuration
{
    [Category("epic-5305-k8s-readiness")]
    public class TelemetryConfigurationTest
    {
        [TestCase("json", true)]
        [TestCase("JSON", true)]
        [TestCase("Json", true)]
        [TestCase("text", false)]
        [TestCase("", false)]
        public void IsJson_MatchesJsonFormatCaseInsensitively(string format, bool expected)
        {
            var logging = new TelemetryLoggingConfiguration { Format = format };

            Assert.That(logging.IsJson, Is.EqualTo(expected));
        }
    }
}
