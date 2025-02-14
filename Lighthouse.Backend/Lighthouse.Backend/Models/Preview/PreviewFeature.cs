using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Preview
{
    public class PreviewFeature : IEntity
    {
        public required int Id { get; set; }

        public string Key { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public required bool Enabled { get; set; }
    }
}
