using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.LighthouseChart;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<FeatureHistoryEntry> featureHistoryRepository;

        public ChartsController(IRepository<Project> projectRepository, IRepository<FeatureHistoryEntry> featureHistoryRepository)
        {
            this.projectRepository = projectRepository;
            this.featureHistoryRepository = featureHistoryRepository;
        }

        [HttpGet("lighthouse/{projectId}")]
        public ActionResult<LighthouseChartDto> GetLighthouseChartData(int projectId)
        {
            var project = projectRepository.GetById(projectId);
            var timespanInDays = 30;

            if (project == null)
            {
                return NotFound();
            }

            var milestones = project.Milestones.Select(ms => new MilestoneDto(ms));

            var lighthouseChartDto = new LighthouseChartDto
            {
                Milestones = new List<MilestoneDto>(milestones)
            };

            foreach (var feature in project.Features)
            {
                var featureDto = new LighthouseChartFeatureDto(feature);

                var featureHistory = featureHistoryRepository
                    .GetAllByPredicate(fhe => fhe.FeatureReferenceId == feature.ReferenceId && fhe.Snapshot >= DateOnly.FromDateTime(DateTime.Now.AddDays(-timespanInDays)))
                    .GroupBy(item => item.Snapshot)
                    .Select(group => group.Last())
                    .ToList();

                foreach (var featureHistoryEntry in featureHistory)
                {
                    var remainingWork = featureHistoryEntry.FeatureWork.Sum(fw => fw.RemainingWorkItems);
                    featureDto.RemainingItemsTrend.Add(new RemainingItemsDto(featureHistoryEntry.Snapshot, remainingWork));
                }

                lighthouseChartDto.Features.Add(featureDto);
            }

            return Ok(lighthouseChartDto);
        }
    }
}
