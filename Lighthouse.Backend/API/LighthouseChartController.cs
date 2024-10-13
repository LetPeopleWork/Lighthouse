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
    public class LighthouseChartController : ControllerBase
    {
        private readonly IRepository<Project> projectRepository;
        private readonly IRepository<FeatureHistoryEntry> featureHistoryRepository;
        private const int MaxEntries = 30;

        public LighthouseChartController(IRepository<Project> projectRepository, IRepository<FeatureHistoryEntry> featureHistoryRepository)
        {
            this.projectRepository = projectRepository;
            this.featureHistoryRepository = featureHistoryRepository;
        }

        [HttpPost("{projectId}")]
        public ActionResult<LighthouseChartDto> GetLighthouseChartData(int projectId, [FromBody] LighthouseChartDataInput input)
        {
            var project = projectRepository.GetById(projectId);

            if (project == null)
            {
                return NotFound();
            }

            var lighthouseChartDto = CreateLighthouseChartDtoWithMilestones(project);

            var timespanInDays = 30;

            if (input.StartDate.HasValue)
            {
                timespanInDays = (DateTime.Today - input.StartDate.Value).Days - 1;
            }

            var featureDtos = CreateFeatureDtos(project.Features, input.SampleRate, timespanInDays);

            lighthouseChartDto.Features.AddRange(featureDtos);

            return Ok(lighthouseChartDto);
        }

        private List<LighthouseChartFeatureDto> CreateFeatureDtos(IEnumerable<Feature> features, int sampleRate, int timespanInDays)
        {
            var featureDtos = new List<LighthouseChartFeatureDto>();

            var samplingFrequency = CalculateSamplingFrequency(timespanInDays, sampleRate);

            foreach (var feature in features)
            {
                var featureDto = new LighthouseChartFeatureDto(feature);
                var remainingItems = CreateFeatureHistoryTrend(samplingFrequency, timespanInDays, feature.ReferenceId);
                featureDto.RemainingItemsTrend.AddRange(remainingItems);

                featureDtos.Add(featureDto);
            }

            return featureDtos;
        }

        private List<RemainingItemsDto> CreateFeatureHistoryTrend(int samplingFrequency, int timespanInDays, string featureReferenceId)
        {
            var remainingItems = new List<RemainingItemsDto>();

            var featureHistory = featureHistoryRepository
                                .GetAllByPredicate(fhe => fhe.FeatureReferenceId == featureReferenceId && fhe.Snapshot >= DateOnly.FromDateTime(DateTime.Now.AddDays(-timespanInDays)))
                                .GroupBy(item => item.Snapshot)
                                .Select(group => group.Last())
                                .ToList();

            featureHistory = FilterHistoryByFrequency(featureHistory, samplingFrequency);

            foreach (var featureHistoryEntry in featureHistory)
            {
                var remainingWork = featureHistoryEntry.FeatureWork.Sum(fw => fw.RemainingWorkItems);
                remainingItems.Add(new RemainingItemsDto(featureHistoryEntry.Snapshot, remainingWork));
            }

            return remainingItems;
        }

        private LighthouseChartDto CreateLighthouseChartDtoWithMilestones(Project? project)
        {
            var milestones = project.Milestones.Select(ms => new MilestoneDto(ms));
            var lighthouseChartDto = new LighthouseChartDto
            {
                Milestones = new List<MilestoneDto>(milestones)
            };

            return lighthouseChartDto;
        }

        private static int CalculateSamplingFrequency(int timespanInDays, int userSampleEveryNthDay)
        {
            int maxAllowedSampleFrequency = Math.Max(1, timespanInDays / MaxEntries);
            return Math.Max(userSampleEveryNthDay, maxAllowedSampleFrequency);
        }

        private static List<FeatureHistoryEntry> FilterHistoryByFrequency(List<FeatureHistoryEntry> history, int sampleEveryNthDay)
        {
            return history.Where((entry, index) => index % sampleEveryNthDay == 0).ToList();
        }

        public class LighthouseChartDataInput
        {
            public DateTime? StartDate { get; set; }

            public int SampleRate { get; set; } = 1;
        }
    }
}
