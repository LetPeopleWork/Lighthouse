namespace CMFTAspNet.Models
{
    public class TeamInProject
    {
        public TeamInProject()
        {            
        }

        public TeamInProject(Team team, Project project)
        {
            Project = project;
            ProjectId = project.Id;
            Team = team;
            TeamId = team.Id;
        }

        public int Id { get; set; }

        public Project Project { get; set; }

        public int ProjectId { get; set; }

        public Team Team { get; set; }

        public int TeamId { get; set; }
    }
}
