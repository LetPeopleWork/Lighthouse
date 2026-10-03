using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class RefinementViewQuery(IRepository<Team> teamRepository, RefinementList refinementList) : IRefinementViewQuery
    {
        public RefinementView? ForTeam(int teamId)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null)
            {
                return null;
            }

            return new RefinementView(team.HasRefinementStates, refinementList.For(team));
        }
    }
}
