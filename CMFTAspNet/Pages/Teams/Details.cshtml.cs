using Microsoft.AspNetCore.Mvc;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Models;

namespace CMFTAspNet.Pages.Teams
{
    public class DetailsModel : PageModelBase<Team>
    {
        private readonly IThroughputService throughputService;

        public DetailsModel(IRepository<Team> teamRepository, IThroughputService throughputService) : base(teamRepository) 
        {
            this.throughputService = throughputService;
        }

        public async Task<IActionResult> OnPost(int? id)
        {
            var team = GetById(id);
            if (team == null)
            {
                return NotFound();
            }

            await throughputService.UpdateThroughput(team);
            Repository.Update(team);
            
            await Repository.Save();

            return OnGet(id);
        }
    }
}
