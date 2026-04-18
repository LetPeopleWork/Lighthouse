namespace Lighthouse.Backend.API.DTO
{
    public class ForecastInputCandidatesDto
    {
        public int CurrentWipCount { get; init; }

        public int BacklogCount { get; init; }

        public List<FeatureCandidateDto> Features { get; init; } = [];
    }

    public class FeatureCandidateDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int RemainingWork { get; init; }
    }
}
