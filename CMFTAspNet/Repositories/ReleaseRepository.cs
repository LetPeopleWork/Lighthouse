using CMFTAspNet.Models;

namespace CMFTAspNet.Repositories
{
    public class ReleaseRepository
    {
        private readonly List<ReleaseConfiguration> releases = new List<ReleaseConfiguration>();

        public void AddRelease(ReleaseConfiguration release)
        {
            releases.Add(release);
        }

        public IEnumerable<ReleaseConfiguration> GetReleases()
        {
            return releases;
        }

        public void RemoveRelease(ReleaseConfiguration release)
        {
            var releaseToRemove = releases.SingleOrDefault(t => t.Id == release.Id);
            releases.Remove(releaseToRemove);
        }

        public void UpdateRelease(ReleaseConfiguration release)
        {
            RemoveRelease(release);
            AddRelease(release);
        }
    }
}
