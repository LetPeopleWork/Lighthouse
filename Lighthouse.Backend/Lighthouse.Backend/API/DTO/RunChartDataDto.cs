using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// Bug #5857: the run chart endpoints used to serialise the persistence entity straight onto the
    /// wire, so the day Cycle Time and Work Item Age became methods taking a time zone, both fields
    /// silently disappeared from every response and the Work Item dialog rendered blank cells. This
    /// is the boundary type; its shape mirrors <see cref="RunChartData"/> so the frontend keeps
    /// deserialising it unchanged.
    /// </summary>
    public class RunChartDataDto
    {
        public RunChartDataDto(Dictionary<int, List<RunChartWorkItemDto>> workItemsPerUnitOfTime, int[] blackoutDayIndices)
        {
            WorkItemsPerUnitOfTime = workItemsPerUnitOfTime;
            BlackoutDayIndices = blackoutDayIndices;
        }

        public Dictionary<int, List<RunChartWorkItemDto>> WorkItemsPerUnitOfTime { get; }

        public int History => WorkItemsPerUnitOfTime.Count;

        public int Total => WorkItemsPerUnitOfTime.Values.Sum(items => items.Count);

        public int[] BlackoutDayIndices { get; }

        /// <param name="asOf">
        /// Anchors Work Item Age on that day rather than today. Work in progress over time passes the
        /// end of the range, matching what the aging chart plots; the throughput and arrivals charts
        /// omit it and stay anchored on today.
        /// </param>
        public static RunChartDataDto From(RunChartData runChartData, ILighthouseClock clock, Func<WorkItemBase, bool> isBlocked, DateTime? asOf = null)
        {
            // The same item occupies one bucket per day it was in progress, and answering "is it
            // blocked" means evaluating the owner's whole rule set, so a wide range would ask the
            // same question hundreds of times over.
            var blockedByItemId = new Dictionary<int, bool>();

            bool IsItemBlocked(WorkItemBase item)
            {
                if (!blockedByItemId.TryGetValue(item.Id, out var blocked))
                {
                    blocked = isBlocked(item);
                    blockedByItemId[item.Id] = blocked;
                }

                return blocked;
            }

            var itemsPerUnitOfTime = runChartData.WorkItemsPerUnitOfTime.ToDictionary(
                bucket => bucket.Key,
                bucket => bucket.Value.Select(item => new RunChartWorkItemDto(item, clock, IsItemBlocked(item), asOf)).ToList());

            return new RunChartDataDto(itemsPerUnitOfTime, runChartData.BlackoutDayIndices);
        }
    }

    /// <summary>
    /// Carries the two fields the premium client-side throughput filter evaluates its rules against.
    /// They are not on <see cref="WorkItemDto"/> because the run chart is the only endpoint that
    /// hands the browser raw items to filter, and without them the filter has nothing to match on.
    /// </summary>
    public class RunChartWorkItemDto : WorkItemDto
    {
        public RunChartWorkItemDto(WorkItemBase workItem, ILighthouseClock clock, bool isBlocked, DateTime? asOf = null)
            : base(workItem, clock, isBlocked, [], null, asOf)
        {
            Tags = workItem.Tags;
            AdditionalFieldValues = workItem.AdditionalFieldValues;
        }

        public IReadOnlyList<string> Tags { get; }

        public IReadOnlyDictionary<int, string?> AdditionalFieldValues { get; }
    }
}
