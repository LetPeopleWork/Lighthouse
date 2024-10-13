namespace Lighthouse.Backend.API.DTO.LighthouseChart
{
    public class LighthouseChartDto
    {
        public List<LighthouseChartFeatureDto> Features { get; set; } = new List<LighthouseChartFeatureDto>();

        public List<MilestoneDto> Milestones { get; set; } = new List<MilestoneDto>();
    }
}
