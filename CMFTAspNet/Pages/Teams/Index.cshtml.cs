using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Data;
using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Pages.Teams
{
    public class IndexModel : PageModel
    {
        private readonly CMFTAspNet.Data.CMFTAspNetContext _context;

        public IndexModel(CMFTAspNet.Data.CMFTAspNetContext context)
        {
            _context = context;
        }

        public IList<Team> Team { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Team = await _context.Team.ToListAsync();
        }
    }
}
