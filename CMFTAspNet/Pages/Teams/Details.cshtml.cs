using Microsoft.AspNetCore.Mvc;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;

namespace CMFTAspNet.Pages.Teams
{
    public class DetailsModel : PageModelBase<Team>
    {
        private readonly IThroughputService throughputService;
        private readonly IRepository<Feature> featureRepository;
        private readonly IMonteCarloService monteCarloService;

        public DetailsModel(IRepository<Team> teamRepository, IThroughputService throughputService, IRepository<Feature> featureRepository, IMonteCarloService monteCarloService) : base(teamRepository) 
        {
            this.throughputService = throughputService;
            this.featureRepository = featureRepository;
            this.monteCarloService = monteCarloService;
        }

        public List<Feature> Features { get; set; } = new List<Feature>();

        protected override void OnGet(int id)
        {
            Features.Clear();
            IEnumerable<Feature> featuresForTeam = GetFeaturesForTeam(id);

            Features.AddRange(featuresForTeam);
        }

        public async Task<IActionResult> OnPostUpdateThroughput(int? id)
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

        public async Task<IActionResult> OnPostUpdateForecast(int? id)
        {
            var team = GetById(id);
            if (!id.HasValue || team == null)
            {
                return NotFound();
            }

            monteCarloService.ForecastFeatures(GetFeaturesForTeam(id.Value));
            await featureRepository.Save();

            return OnGet(id);
        }

        private IEnumerable<Feature> GetFeaturesForTeam(int id)
        {
            var allFeatures = featureRepository.GetAll().OrderBy(f => f.Order);
            var featuresForTeam = allFeatures.Where(f => f.RemainingWork.Any(rw => rw.Team.Id == id));
            return featuresForTeam;
        }
    }
}
