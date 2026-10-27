using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Models.Reports
{
    public class Report : IConcurrencyTokenEntity
    {
        public const int LongestTemplateKey = 64;

        public const int LongestName = 200;

        public const int CurrentPayloadSchemaVersion = 1;

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

        /// <summary>A new report of exactly one owner: the owner's kind decides which of the two owner columns is set.</summary>
        public static Report For(ReportOwner owner, string templateKey, string name, DateTime createdAt)
        {
            return new Report
            {
                TeamId = owner.Kind == ReportOwnerKind.Team ? owner.Id : null,
                PortfolioId = owner.Kind == ReportOwnerKind.Portfolio ? owner.Id : null,
                TemplateKey = templateKey,
                Name = name,
                CreatedAt = createdAt,
                PayloadSchemaVersion = CurrentPayloadSchemaVersion,
                ConcurrencyToken = Guid.NewGuid(),
            };
        }

        public ReportOwner OwnedBy() => TeamId is int teamId ? ReportOwner.Team(teamId) : ReportOwner.Portfolio(PortfolioId ?? 0);
    }
}
