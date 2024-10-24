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
                timespanInDays = (DateTime.Today - input.StartDate.Value).Days;
            }

            var featureDtos = CreateFeatureDtos(project.Features, input.SampleRate, timespanInDays - 1);

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
                                .ToList();

            var daysAgo = timespanInDays;
            while (daysAgo > -1)
            {
                var snapshotTime = DateOnly.FromDateTime(DateTime.Now.AddDays(-daysAgo));
                var featureHistoryEntry = featureHistory.LastOrDefault(fhe => fhe.Snapshot == snapshotTime);

                AddRemainingItemDtoFromFeatureHistoryEntry(remainingItems, snapshotTime, featureHistoryEntry);

                daysAgo -= samplingFrequency;
            }

            return remainingItems;
        }

        private static void AddRemainingItemDtoFromFeatureHistoryEntry(List<RemainingItemsDto> remainingItems, DateOnly snapshotTime, FeatureHistoryEntry? featureHistoryEntry)
        {
            if (featureHistoryEntry != null)
            {
                var remainingWork = featureHistoryEntry.FeatureWork.Sum(fw => fw.RemainingWorkItems);
                remainingItems.Add(new RemainingItemsDto(featureHistoryEntry.Snapshot, remainingWork));
            }
            else if (remainingItems.Any())
            {
                var lastEntry = remainingItems.Last();
                remainingItems.Add(new RemainingItemsDto(snapshotTime, lastEntry.RemainingItems));
            }
        }

        private LighthouseChartDto CreateLighthouseChartDtoWithMilestones(Project? project)
        {
            var milestones = project.Milestones.Where(m => m.Date > DateTime.Today).Select(ms => new MilestoneDto(ms));
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

        public class LighthouseChartDataInput
        {
            public DateTime? StartDate { get; set; }

            public int SampleRate { get; set; } = 1;
        }
    }
}
