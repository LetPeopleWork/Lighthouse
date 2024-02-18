using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Repositories
{
    public class TeamRepository
    {
        private readonly List<Team> teams = new List<Team>();

        public void AddTeam(Team team)
        {
            teams.Add(team);
        }

        public IEnumerable<Team> GetTeams()
        {
            return teams;
        }

        public void RemoveTeam(Team team)
        {
            var teamToRemove = teams.SingleOrDefault(t => t.Id == team.Id);
            teams.Remove(teamToRemove);
        }

        public void UpdateTeam(Team team)
        {
            RemoveTeam(team);
            AddTeam(team);
        }
    }
}
