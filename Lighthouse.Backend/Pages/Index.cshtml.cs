using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lighthouse.Backend.Pages;

public class IndexModel : PageModel
{
    private readonly IRepository<Project> projectRepository;
    private readonly IMonteCarloService monteCarloService;
    private readonly IWorkItemCollectorService workItemCollectorService;

    public IndexModel(IRepository<Project> projectRepository, IMonteCarloService monteCarloService, IWorkItemCollectorService workItemCollectorService)
    {
        this.projectRepository = projectRepository;
        this.monteCarloService = monteCarloService;
        this.workItemCollectorService = workItemCollectorService;
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
        foreach (var project in projectRepository.GetAll())
        {
            await workItemCollectorService.UpdateFeaturesForProject(project);
        }

        await projectRepository.Save();

        await monteCarloService.ForecastAllFeatures();

        return OnGet();
    }
}
