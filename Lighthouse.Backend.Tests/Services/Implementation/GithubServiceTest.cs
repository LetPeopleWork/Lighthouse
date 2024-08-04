using Lighthouse.Backend.Services.Implementation;
using Octokit;

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

        [Test]
        public async Task GetReleaseByTag_FetchesInformationFromGitHub()
        {
            var subject = new GitHubService();

            var release = await subject.GetReleaseByTag("v24.8.3.1040");

            Assert.Multiple(() =>
            {
                Assert.That(release.Name, Is.EqualTo("Lighthouse v24.8.3.1040"));
                Assert.That(release.Link, Is.EqualTo("https://github.com/LetPeopleWork/Lighthouse/releases/tag/v24.8.3.1040"));
                Assert.That(release.Version, Is.EqualTo("v24.8.3.1040"));
                Assert.That(release.Highlights, Is.EqualTo("# Highlights\r\n- This release adds interactive tutorials for various pages\r\n- Possibility to adjust milestones via the project view\r\n- Possibility to adjust Feature WIP of involved teams via the project detail view\r\n\r\n**Full Changelog**: https://github.com/LetPeopleWork/Lighthouse/compare/v24.7.28.937...v24.8.3.1040"));
                
                Assert.That(release.Assets, Has.Count.EqualTo(3));
                Assert.That(release.Assets[0].Name, Is.EqualTo("Lighthouse_v24.8.3.1040_linux-x64.zip"));
                Assert.That(release.Assets[0].Link, Is.EqualTo("https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_linux-x64.zip"));
                Assert.That(release.Assets[1].Name, Is.EqualTo("Lighthouse_v24.8.3.1040_osx-x64.zip"));
                Assert.That(release.Assets[1].Link, Is.EqualTo("https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_osx-x64.zip"));
                Assert.That(release.Assets[2].Name, Is.EqualTo("Lighthouse_v24.8.3.1040_win-x64.zip"));
                Assert.That(release.Assets[2].Link, Is.EqualTo("https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_win-x64.zip"));
            });
        }

        [Test]
        public async Task GetReleaseByTag_TagDoesNotExist_ReturnsNull()
        {
            var subject = new GitHubService();

            var release = await subject.GetReleaseByTag("A non existing Tag");

            Assert.That(release, Is.Null);
        }

        [Test]
        public async Task GetAllReleases_ReturnsReleasesInOrder()
        {
            var subject = new GitHubService();

            var allReleases = (await subject.GetAllReleases()).ToList();

            Assert.Multiple(() =>
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
            });
        }
    }
}
