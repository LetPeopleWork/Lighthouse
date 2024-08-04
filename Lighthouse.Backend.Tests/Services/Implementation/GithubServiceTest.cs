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
    }
}
