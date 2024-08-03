using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [Category("Integration")]
    public class GithubServiceTest
    {
        [Test]
        public async Task GetLatestVersion_DoesGetLatestTag()
        {
            var subject = new GitHubService();

            var latestVersion = await subject.GetLatestReleaseVersion();

            Assert.IsNotNull(latestVersion);
        }
    }
}
