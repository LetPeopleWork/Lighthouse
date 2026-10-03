using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class RefinementViewQuery(IRepository<Team> teamRepository, RefinementList refinementList, SleYardstickResolver yardstickResolver) : IRefinementViewQuery
    {
        public RefinementView? ForTeam(int teamId)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null)
            {
                return null;
            }

            if (!team.HasRefinementStates)
            {
                return new RefinementView(false, [], Yardstick.None);
            }

            return new RefinementView(true, refinementList.For(team), yardstickResolver.For(team));
        }
    }
}
