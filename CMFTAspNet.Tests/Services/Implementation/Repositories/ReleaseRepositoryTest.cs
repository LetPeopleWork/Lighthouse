using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
{
    public class ReleaseRepositoryTest
    {
        [Test]
        public void GetReleases_NoRelease_ReturnsEmptyList()
        {
            var subject = new ReleaseRepository();

            var releases = subject.GetReleases();

            Assert.That(releases, Is.Empty);
        }

        [Test]
        public void AddRelease_StoresRelease()
        {
            var subject = new ReleaseRepository();
            var release = new ReleaseConfiguration();

            subject.AddRelease(release);

            var releases = subject.GetReleases();

            Assert.That(releases, Contains.Item(release));
        }

        [Test]
        public void GivenExistingRelease_RemoveRelease_RemovesFromList()
        {
            var subject = new ReleaseRepository();
            var release = new ReleaseConfiguration();

            subject.AddRelease(release);

            // Act
            subject.RemoveRelease(release);

            var releases = subject.GetReleases();
            CollectionAssert.DoesNotContain(releases, release);
        }

        [Test]
        public void UpdateRelease_GivenExistingRelease_PersistsChange()
        {
            var subject = new ReleaseRepository();
            var release = new ReleaseConfiguration();

            subject.AddRelease(release);

            // Act
            release.Name = "New Release";
            subject.UpdateRelease(release);

            // Assert
            var releases = subject.GetReleases();
            Assert.That(releases.Single().Name, Is.EqualTo("New Release"));
        }
    }
}
