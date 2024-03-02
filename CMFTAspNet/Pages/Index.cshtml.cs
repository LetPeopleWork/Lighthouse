using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CMFTAspNet.Pages;

public class IndexModel : PageModel
{
    private readonly IRepository<Project> projectRepository;
    private readonly IMonteCarloService monteCarloService;

    public IndexModel(IRepository<Project> projectRepository, IMonteCarloService monteCarloService)
    {
        this.projectRepository = projectRepository;
        this.monteCarloService = monteCarloService;
    }

    [BindProperty]
    public List<Project> Projects { get; set; } = new List<Project>();

    public IActionResult OnGet()
    {
        Projects = new List<Project>(projectRepository.GetAll());

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        await monteCarloService.ForecastAllFeatures();

        return OnGet();
    }
}
