using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [TestFixture]
    public class JiraSiteAddressTest
    {
        [TestCase("https://x.atlassian.net/jira/", ExpectedResult = "https://x.atlassian.net")]
        [TestCase("https://X.Atlassian.net/jira/software/projects", ExpectedResult = "https://X.Atlassian.net")]
        [TestCase("https://x.atlassian.net", ExpectedResult = "https://x.atlassian.net")]
        [TestCase("https://x.atlassian.net/", ExpectedResult = "https://x.atlassian.net")]
        [TestCase("https://x.atlassian.net?selectedProject=PROJ", ExpectedResult = "https://x.atlassian.net")]
        [TestCase("https://x.atlassian.net:8443/jira/", ExpectedResult = "https://x.atlassian.net:8443")]
        [TestCase("  https://x.atlassian.net/jira/  ", ExpectedResult = "https://x.atlassian.net")]
        [TestCase("https://host/jira", ExpectedResult = "https://host/jira")]
        [TestCase("https://host/jira/", ExpectedResult = "https://host/jira")]
        [TestCase("https://host/", ExpectedResult = "https://host")]
        [TestCase("https://host", ExpectedResult = "https://host")]
        [TestCase("https://notatlassian.net/jira/", ExpectedResult = "https://notatlassian.net/jira")]
        [TestCase("https://x.atlassian.net.example.invalid/jira/", ExpectedResult = "https://x.atlassian.net.example.invalid/jira")]
        [TestCase("not a url", ExpectedResult = "not a url")]
        [TestCase("", ExpectedResult = "")]
        public string Of_ReducesACloudAddressToItsSiteRootAndKeepsAnyOtherPath(string entered)
            => JiraSiteAddress.Of(entered);
    }
}
