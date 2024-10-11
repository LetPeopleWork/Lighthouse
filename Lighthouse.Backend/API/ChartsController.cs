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
        private const int MaxEntries = 30;

        public ChartsController(IRepository<Project> projectRepository, IRepository<FeatureHistoryEntry> featureHistoryRepository)
        {
            this.projectRepository = projectRepository;
            this.featureHistoryRepository = featureHistoryRepository;
        }

        [HttpPost("lighthouse/{projectId}")]
        public ActionResult<LighthouseChartDto> GetLighthouseChartData(int projectId, [FromBody] LighthouseChartDataInput input)
        {
            var project = projectRepository.GetById(projectId);
            var timespanInDays = (DateTime.Today - input.StartDate).Days;

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

                int samplingFrequency = CalculateSamplingFrequency(timespanInDays, input.SampleRate);

                featureHistory = FilterHistoryByFrequency(featureHistory, samplingFrequency);

                featureHistory = featureHistory.Take(MaxEntries).ToList();

                foreach (var featureHistoryEntry in featureHistory)
                {
                    var remainingWork = featureHistoryEntry.FeatureWork.Sum(fw => fw.RemainingWorkItems);
                    featureDto.RemainingItemsTrend.Add(new RemainingItemsDto(featureHistoryEntry.Snapshot, remainingWork));
                }

                lighthouseChartDto.Features.Add(featureDto);
            }

            return Ok(lighthouseChartDto);
        }

        private int CalculateSamplingFrequency(int timespanInDays, int userSampleEveryNthDay)
        {
            int maxAllowedSampleFrequency = Math.Max(1, timespanInDays / MaxEntries); // Ensure at least 1 day
            return Math.Max(userSampleEveryNthDay, maxAllowedSampleFrequency); // Auto-correct if needed
        }

        private List<FeatureHistoryEntry> FilterHistoryByFrequency(List<FeatureHistoryEntry> history, int sampleEveryNthDay)
        {
            return history.Where((entry, index) => index % sampleEveryNthDay == 0).ToList();
        }

        public class LighthouseChartDataInput
        {
            public DateTime StartDate { get; set; }

            public int SampleRate { get; set; }
        }
    }
}
