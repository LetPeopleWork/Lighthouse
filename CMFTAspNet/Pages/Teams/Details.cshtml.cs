using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Data;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Pages.Teams
{
    public class DetailsModel : PageModel
    {
        private readonly CMFTAspNetContext _context;
        private readonly IThroughputService throughputService;

        public DetailsModel(CMFTAspNetContext context, IThroughputService throughputService)
        {
            _context = context;
            this.throughputService = throughputService;
        }

        public Team Team { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var team = await _context.Team.FirstOrDefaultAsync(m => m.Id == id);
            if (team == null)
            {
                return NotFound();
            }
            else
            {
                Team = team;
            }
            return Page();
        }

        public async Task<IActionResult> OnPost(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var team = await _context.Team.Include(t => t.WorkTrackingSystemOptions).FirstOrDefaultAsync(m => m.Id == id);
            if (team == null)
            {
                return NotFound();
            }            

            await throughputService.UpdateThroughput(team);
            _context.Attach(team).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return await OnGetAsync(id);
        }
    }
}
