using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// Everything the Features grid of one Portfolio or Team draws, in one answer: the rows, the parents they hang under,
    /// and which Teams are working on which Features today.
    /// </summary>
    public class FeatureListDto
    {
        public List<FeatureListRowDto> Features { get; } = [];

        public List<FeatureListParentDto> Parents { get; } = [];

        public List<FeatureListActiveWorkDto> ActiveWork { get; } = [];
    }

    public class FeatureListParentDto(string referenceId, string name, string url)
    {
        public string ReferenceId { get; } = referenceId;

        public string Name { get; } = name;

        public string Url { get; } = url;
    }

    public class FeatureListActiveWorkDto(int teamId, IReadOnlyList<string> featureReferenceIds)
    {
        public int TeamId { get; } = teamId;

        public IReadOnlyList<string> FeatureReferenceIds { get; } = featureReferenceIds;
    }

    /// <summary>
    /// A row of the list: the same fields as <see cref="FeatureDto"/>, with the same names and shapes, except the
    /// forecast of each contributing Team, which the grid never reads and which only the Delivery timeline draws.
    /// </summary>
    public class FeatureListRowDto : WorkItemDto
    {
        // The four the completion forecast has always reported, and the start is read at the same levels.
        private static readonly int[] TheUsualPercentiles = [50, 70, 85, 95];

        public FeatureListRowDto(
            Feature feature,
            ILighthouseClock clock,
            IReadOnlyList<BlackoutPeriod> blackoutPeriods,
            bool isBlocked,
            ISet<int> readablePortfolioIds)
            : base(feature, clock, isBlocked, [], null)
        {
            LastUpdated = DateTime.SpecifyKind(feature.Forecast?.CreationTime ?? DateTime.MinValue, DateTimeKind.Utc);
            IsUsingDefaultFeatureSize = feature.IsUsingDefaultFeatureSize;
            Size = feature.Size;
            OwningTeam = feature.OwningTeam;

            TeamsWithoutForecast.AddRange(feature.TeamsWithoutForecast.Select(team => team.Name).Distinct().Order());

            if (feature.CanBeForecast)
            {
                Forecasts.AddRange(feature.Forecast?.CreateForecastDtos(clock.Today, blackoutPeriods, TheUsualPercentiles) ?? []);
            }

            StartForecast = new FeatureStartDto(feature.WhenWorkBegins, clock.Today, blackoutPeriods, TheUsualPercentiles);

            foreach (var work in feature.FeatureWork)
            {
                if (RemainingWork.TryAdd(work.TeamId, 0))
                {
                    TotalWork.Add(work.TeamId, 0);
                }

                RemainingWork[work.TeamId] += work.RemainingWorkItems;
                TotalWork[work.TeamId] += work.TotalWorkItems;
            }

            Projects.AddRange(feature.Portfolios
                .Where(portfolio => readablePortfolioIds.Contains(portfolio.Id))
                .Select(portfolio => new EntityReferenceDto(portfolio.Id, portfolio.Name)));
        }

        public bool IsUsingDefaultFeatureSize { get; }

        public int Size { get; }

        public string OwningTeam { get; }

        public List<EntityReferenceDto> Projects { get; } = [];

        public DateTime LastUpdated { get; }

        public Dictionary<int, int> RemainingWork { get; } = [];

        public Dictionary<int, int> TotalWork { get; } = [];

        public List<WhenForecastDto> Forecasts { get; } = [];

        public FeatureStartDto StartForecast { get; }

        public List<string> TeamsWithoutForecast { get; } = [];

        public int? Position { get; set; }

        public bool? CanMove { get; set; }

        public string? MoveBlockReason { get; set; }

        public List<EntityReferenceDto> BlockingPortfolios { get; } = [];

        public List<FeatureDependsOnDto> DependsOn { get; } = [];
    }
}
