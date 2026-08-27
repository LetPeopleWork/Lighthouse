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
    /// <typeparam name="TWorkItem">
    /// The item type this chart's buckets hold. It is a type parameter rather than a common base
    /// because a JSON writer emits a collection element by the type the collection declares, so a
    /// bucket declared as the base type would drop everything a richer item adds.
    /// </typeparam>
    public class RunChartDataDto<TWorkItem> where TWorkItem : RunChartWorkItemDto
    {
        public RunChartDataDto(Dictionary<int, List<TWorkItem>> workItemsPerUnitOfTime, int[] blackoutDayIndices)
        {
            WorkItemsPerUnitOfTime = workItemsPerUnitOfTime;
            BlackoutDayIndices = blackoutDayIndices;
        }

        public Dictionary<int, List<TWorkItem>> WorkItemsPerUnitOfTime { get; }

        public int History => WorkItemsPerUnitOfTime.Count;

        public int Total => WorkItemsPerUnitOfTime.Values.Sum(items => items.Count);

        public int[] BlackoutDayIndices { get; }

        public static RunChartDataDto<TWorkItem> From(RunChartData runChartData, Func<WorkItemBase, bool> isBlocked, Func<WorkItemBase, bool, TWorkItem> toDto)
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
                bucket => bucket.Value.Select(item => toDto(item, IsItemBlocked(item))).ToList());

            return new RunChartDataDto<TWorkItem>(itemsPerUnitOfTime, runChartData.BlackoutDayIndices);
        }
    }

    /// <summary>
    /// Carries the two fields the premium client-side throughput filter evaluates its rules against.
    /// They are not on <see cref="WorkItemDto"/> because the run chart is the only endpoint that
    /// hands the browser raw items to filter, and without them the filter has nothing to match on.
    /// </summary>
    public class RunChartWorkItemDto : WorkItemDto
    {
        /// <param name="asOf">
        /// Anchors Work Item Age on that day rather than today. Work in progress over time passes the
        /// end of the range, matching what the aging chart plots; the throughput and arrivals charts
        /// omit it and stay anchored on today.
        /// </param>
        public RunChartWorkItemDto(WorkItemBase workItem, ILighthouseClock clock, bool isBlocked, DateTime? asOf = null)
            : base(workItem, clock, isBlocked, [], null, asOf)
        {
            Tags = workItem.Tags;
            AdditionalFieldValues = workItem.AdditionalFieldValues;
        }

        public IReadOnlyList<string> Tags { get; }

        public IReadOnlyDictionary<int, string?> AdditionalFieldValues { get; }
    }

    /// <summary>
    /// A portfolio run chart holds Features, which know their size and the team that owns them. Those
    /// two are restated here rather than inherited because the Work Item dialog only draws its "Owned
    /// by" column when the payload actually carries owningTeam, and a chart whose items were written
    /// as the plainer base type loses the column with no error anywhere to explain it.
    /// </summary>
    public class PortfolioRunChartWorkItemDto : RunChartWorkItemDto
    {
        public PortfolioRunChartWorkItemDto(WorkItemBase workItem, ILighthouseClock clock, bool isBlocked, DateTime? asOf = null)
            : base(workItem, clock, isBlocked, asOf)
        {
            var feature = workItem as Feature;
            Size = feature?.Size ?? 0;
            OwningTeam = feature?.OwningTeam ?? string.Empty;
        }

        public int Size { get; }

        public string OwningTeam { get; }
    }
}
