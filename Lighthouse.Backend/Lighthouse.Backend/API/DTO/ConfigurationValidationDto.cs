namespace Lighthouse.Backend.API.DTO
{
    public class ConfigurationValidationDto
    {
        public ConfigurationValidationDto()
        {
        }

        public ConfigurationValidationDto(ConfigurationExport configurationExport)
        {
            InitializeWorkTrackingSystems(configurationExport.WorkTrackingSystems);
            InitializeTeams(configurationExport.Teams);
            InitializeProjects(configurationExport.Projects);
        }

        public List<ConfigurationValidationItem> WorkTrackingSystems { get; } = [];

        public List<ConfigurationValidationItem> Teams { get; } = [];

        public List<ConfigurationValidationItem> Projects { get; } = [];

        public void UpdateTeam(int teamId, ValidationStatus status, string errorMessage = "", int? updatedId = null)
        {
            Update(Teams, teamId, status, errorMessage, updatedId);
        }

        public void UpdateProject(int projectId, ValidationStatus status, string errorMessage = "", int? updatedId = null)
        {
            Update(Projects, projectId, status, errorMessage, updatedId);
        }

        private void Update(List<ConfigurationValidationItem> items, int id, ValidationStatus status, string errorMessage = "", int? updatedId = null)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                item.Status = status;
                item.ErrorMessage = errorMessage;
                item.Id = updatedId ?? item.Id;
            }
        }

        private void InitializeWorkTrackingSystems(IEnumerable<WorkTrackingSystemConnectionDto> workTrackingSystems)
        {
            foreach (var workTrackingSystem in workTrackingSystems)
            {
                WorkTrackingSystems.Add(new ConfigurationValidationItem
                {
                    Id = workTrackingSystem.Id,
                    Name = workTrackingSystem.Name,
                });
            }
        }

        private void InitializeTeams(IEnumerable<TeamSettingDto> teams)
        {
            foreach (var team in teams)
            {
                Teams.Add(new ConfigurationValidationItem
                {
                    Id = team.Id,
                    Name = team.Name,
                });
            }
        }

        private void InitializeProjects(IEnumerable<ProjectSettingDto> projects)
        {
            foreach (var project in projects)
            {
                Projects.Add(new ConfigurationValidationItem
                {
                    Id = project.Id,
                    Name = project.Name,
                });
            }
        }
    }

    public class ConfigurationValidationItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ValidationStatus Status { get; set; } = ValidationStatus.New;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public enum ValidationStatus
    {
        New,
        Update,
        Error,
    };
}
