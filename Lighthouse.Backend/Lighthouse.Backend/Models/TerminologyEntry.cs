using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    public class TerminologyEntry : IEntity
    {
        [JsonRequired]
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string DefaultValue { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
