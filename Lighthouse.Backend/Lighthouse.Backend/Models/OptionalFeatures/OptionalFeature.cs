using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.OptionalFeatures
{
    public class OptionalFeature : IEntity
    {
        public required int Id { get; set; }

        public string Key { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public required bool Enabled { get; set; }

        public bool IsPreview { get; set; } = false;
    }
}
