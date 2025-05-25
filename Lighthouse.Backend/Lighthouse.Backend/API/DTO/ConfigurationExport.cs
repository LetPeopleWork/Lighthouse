namespace Lighthouse.Backend.API.DTO
{
    public class ConfigurationExport
    {
        public List<WorkTrackingSystemConnectionDto> WorkTrackingSystems { get; set; } = [];

        public List<TeamSettingDto> Teams { get; set; } = [];

        public List<ProjectSettingDto> Projects { get; set; } = [];
    }
}
