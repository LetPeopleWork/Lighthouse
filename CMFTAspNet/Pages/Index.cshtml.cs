using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMFTAspNet.Pages;

public class IndexModel : PageModel
{
    private readonly Data.AppContext _context;
    private readonly IMonteCarloService monteCarloService;

    public IndexModel(Data.AppContext context, IMonteCarloService monteCarloService)
    {
        _context = context;
        this.monteCarloService = monteCarloService;
    }

    [BindProperty]
    public List<Project> Projects { get; set; } = new List<Project>();

    public IActionResult OnGet()
    {
        Projects = new List<Project>(
            _context.Projects
            .Include(r => r.Features)
            .ThenInclude(f => f.RemainingWork)
            .Include(f => f.Features)
            .ThenInclude(f => f.Forecast)
            .ToList());

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var projects = _context.Projects.Include(r => r.Features).ThenInclude(x => x.RemainingWork).ThenInclude(x => x.Team);
        foreach (var project in projects)
        {
            monteCarloService.ForecastFeatures(project.Features);

            await _context.SaveChangesAsync();
        }

        return OnGet();
    }
}
