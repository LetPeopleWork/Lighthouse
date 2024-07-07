namespace Lighthouse.Backend.API.DTO
{
    public class ProjectOverviewDto
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public int RemainingWork { get; set; }
        
        public List<TeamDto> InvolvedTeams { get; } = new List<TeamDto>();

        public List<ForecastDto> Forecasts { get; } = new List<ForecastDto>();
        
        public DateTime LastUpdated { get; set; }
    }
}
