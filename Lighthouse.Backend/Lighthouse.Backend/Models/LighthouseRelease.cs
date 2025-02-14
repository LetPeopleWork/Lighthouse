namespace Lighthouse.Backend.Models
{
    public class LighthouseRelease
    {
        public string Name { get; set; }

        public string Link { get; set; }

        public string Highlights { get; set; }

        public string Version { get; set; }

        public List<LighthouseReleaseAsset> Assets { get; } = [];
    }
}
