using Microsoft.AspNetCore.Mvc.RazorPages;
using Lighthouse.Models;
using Lighthouse.Services.Interfaces;

namespace Lighthouse.Pages.Teams
{
    public class IndexModel : PageModel
    {
        private readonly IRepository<Team> teamRepository;

        public IndexModel(IRepository<Team> teamRepository)
        {
            this.teamRepository = teamRepository;
        }

        public IList<Team> Teams { get;set; } = default!;

        public void OnGet()
        {
            var teams = teamRepository.GetAll();
            Teams = new List<Team>(teams);
        }
    }
}
