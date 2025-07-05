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

            Assert.That(latestVersion, Is.Not.Null);
        }

        [Test]
        public async Task GetAllReleases_ReturnsReleasesInOrder()
        {
            var subject = new GitHubService();

            var allReleases = (await subject.GetAllReleases()).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(allReleases, Has.Count.GreaterThan(2));

                var firstRelease = allReleases[allReleases.Count - 1];
                Assert.That(firstRelease.Name, Is.EqualTo("Lighthouse v24.7.26.1246"));
                Assert.That(firstRelease.Link, Is.EqualTo("https://github.com/LetPeopleWork/Lighthouse/releases/tag/v24.7.26.1246"));
                Assert.That(firstRelease.Version, Is.EqualTo("v24.7.26.1246"));
                Assert.That(firstRelease.Highlights, Is.EqualTo("## What's Changed\r\n* Complete rewrite of Frontend, it's now using a react-based one\r\n* Add the possibility to see and download logs via UI\r\n* Manage WorkTrackingSystem Connections in one place - reuse in various Teams/Projects\r\n* Overall UI/UX Improvements\r\n\r\n\r\n**Full Changelog**: https://github.com/LetPeopleWork/Lighthouse/compare/v24.6.22.730...v24.7.26.1246"));

                Assert.That(firstRelease.Assets, Has.Count.EqualTo(1));
                Assert.That(firstRelease.Assets[0].Name, Is.EqualTo("Lighthouse.v24.7.26.1246.zip"));
                Assert.That(firstRelease.Assets[0].Link, Is.EqualTo("https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.7.26.1246/Lighthouse.v24.7.26.1246.zip"));
            };
        }
    }
}
