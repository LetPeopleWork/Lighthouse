using Microsoft.AspNetCore.Mvc;
using Lighthouse.Services.Interfaces;
using Lighthouse.Models;
using Lighthouse.Services.Implementation;
using Lighthouse.Models.Forecast;

namespace Lighthouse.Pages.Teams
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

        [BindProperty]
        public HowManyForecast HowManyForecast { get; set; }

        [BindProperty]
        public WhenForecast WhenForecast { get; set; }

        protected override void OnGet(int id)
        {
            Features.Clear();
            IEnumerable<Feature> featuresForTeam = GetFeaturesForTeam(id);

            Features.AddRange(featuresForTeam);
        }

        public async Task<IActionResult> OnPostUpdateThroughput(int? id)
        {
            return await GetTeamAndReloadPage(id, async (Team team) =>
            {
                await throughputService.UpdateThroughput(team);
                Repository.Update(team);

                await Repository.Save();
            });
        }

        public async Task<IActionResult> OnPostUpdateForecast(int? id)
        {
            return await GetTeamAndReloadPage(id, async (Team team) =>
            {
                await monteCarloService.ForecastFeaturesForTeam(team);
            });
        }

        public async Task<IActionResult> OnPostWhenForecast(int? id, int? itemsRemaining)
        {
            return await GetTeamAndReloadPage(id, (Team team) =>
            {
                if (itemsRemaining.HasValue)
                {
                    WhenForecast = monteCarloService.When(team, itemsRemaining.Value);
                }

                return Task.CompletedTask;
            });
        }

        public async Task<IActionResult> OnPostHowManyForecast(int? id, DateTime? targetDate)
        {
            return await GetTeamAndReloadPage(id, (Team team) =>
            {
                if (targetDate.HasValue)
                {
                    var daysTillTargetDate = (targetDate.Value - DateTime.Today).Days;

                    if (daysTillTargetDate > 0)
                    {
                        HowManyForecast = monteCarloService.HowMany(team.Throughput, daysTillTargetDate);
                    }
                }

                return Task.CompletedTask;
            });
        }

        private async Task<IActionResult> GetTeamAndReloadPage(int? id, Func<Team, Task> postAction)
        {
            var team = GetById(id);
            if (!id.HasValue || team == null)
            {
                return NotFound();
            }

            await postAction(team);

            return OnGet(id);
        }

        private IEnumerable<Feature> GetFeaturesForTeam(int id)
        {
            var allFeatures = featureRepository.GetAll().OrderBy(f => f, new FeatureComparer());
            var featuresForTeam = allFeatures.Where(f => f.RemainingWork.Exists(rw => rw.Team.Id == id));
            return featuresForTeam;
        }
    }
}
