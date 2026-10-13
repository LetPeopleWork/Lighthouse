using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Reports
{
    public class Report : IConcurrencyTokenEntity
    {
        public const int LongestTemplateKey = 64;

        public const int LongestName = 200;

        public int Id { get; set; }

        public int? TeamId { get; set; }

        public int? PortfolioId { get; set; }

        public string TemplateKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string ShownMetricKeysJson { get; set; } = "[]";

        public string TemplatePayloadJson { get; set; } = "{}";

        public int PayloadSchemaVersion { get; set; }

        public Guid ConcurrencyToken { get; set; }
    }
}
