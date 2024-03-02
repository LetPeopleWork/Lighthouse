using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CMFTAspNet.Pages;

public class IndexModel : PageModel
{
    private readonly IRepository<Feature> featureRepository;
    private readonly IMonteCarloService monteCarloService;

    public IndexModel(IRepository<Feature> featureRepository, IMonteCarloService monteCarloService)
    {
        this.featureRepository = featureRepository;
        this.monteCarloService = monteCarloService;
    }

    [BindProperty]
    public List<Feature> Features { get; set; } = new List<Feature>();

    public IActionResult OnGet()
    {
        Features = new List<Feature>(featureRepository.GetAll());

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        await monteCarloService.ForecastAllFeatures();

        return OnGet();
    }
}
