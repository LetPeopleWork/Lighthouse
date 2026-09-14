using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// What to call a piece of work on a screen.
    ///
    /// Resolved as the list is read rather than stored alongside the status, so a rename shows up
    /// immediately and the update path stays ignorant of anything a screen needs. An entity that has gone -
    /// deleted while its own refresh was in flight - still has a type and an id, and saying those is more
    /// use to an operator than a blank row.
    /// </summary>
    public class UpdateTaskNaming : IUpdateTaskNaming
    {
        private readonly IRepository<Team> teamRepository;

        private readonly IPortfolioRepository portfolioRepository;

        public UpdateTaskNaming(IRepository<Team> teamRepository, IPortfolioRepository portfolioRepository)
        {
            this.teamRepository = teamRepository;
            this.portfolioRepository = portfolioRepository;
        }

        public string NameOf(UpdateStatus work)
        {
            var name = work.UpdateType switch
            {
                UpdateType.Team or UpdateType.TeamDelete => teamRepository.GetById(work.Id)?.Name,
                _ => portfolioRepository.GetById(work.Id)?.Name,
            };

            return string.IsNullOrWhiteSpace(name) ? $"{work.UpdateType} {work.Id}" : name;
        }
    }
}
