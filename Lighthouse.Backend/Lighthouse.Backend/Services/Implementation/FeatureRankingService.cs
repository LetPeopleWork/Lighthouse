using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureRankingService(
        LighthouseAppContext context,
        IFeatureOrdering featureOrdering,
        IDomainEventDispatcher domainEventDispatcher) : IFeatureRankingService
    {
        public async Task<FeatureMovePlacement> PlaceAsync(int featureId, int? targetFeatureId, bool placeBefore, CancellationToken cancellationToken = default)
        {
            // Postgres runs with a retrying execution strategy, which refuses a user-initiated transaction
            // unless the whole unit of work is handed to it. SQLite's default strategy just invokes it.
            var strategy = context.Database.CreateExecutionStrategy();

            var placement = await strategy.ExecuteAsync(() => Place(featureId, targetFeatureId, placeBefore, cancellationToken));

            if (placement == FeatureMovePlacement.Placed)
            {
                // After the commit, never inside it: the forecast reacts to a fact, and a run triggered by a
                // move that then rolled back would forecast an order nobody chose (ADR-133).
                await domainEventDispatcher.PublishAsync(new FeatureRankChanged(featureId), cancellationToken);
            }

            return placement;
        }

        private async Task<FeatureMovePlacement> Place(int featureId, int? targetFeatureId, bool placeBefore, CancellationToken cancellationToken)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            // The same narrow projection the position map reads (ADR-135), re-read inside the boundary
            // because the place the move is defined against is the target's place *now* (DDD-6).
            var orderKeys = await context.Features
                .AsNoTracking()
                .Select(feature => new FeatureOrderKey(feature.Id, feature.Order, feature.ManualRank))
                .ToListAsync(cancellationToken);

            if (!orderKeys.Exists(orderKey => orderKey.Id == featureId))
            {
                return FeatureMovePlacement.FeatureNotFound;
            }

            if (targetFeatureId is { } target && !orderKeys.Exists(orderKey => orderKey.Id == target))
            {
                return FeatureMovePlacement.TargetNotFound;
            }

            var sequence = featureOrdering.Order(orderKeys).Select(orderKey => orderKey.Id).ToList();
            var indexItHeld = sequence.IndexOf(featureId);

            sequence.Remove(featureId);
            sequence.Insert(InsertionIndex(sequence, indexItHeld, targetFeatureId, placeBefore), featureId);

            await WriteThePlaces(sequence, orderKeys.ToDictionary(orderKey => orderKey.Id, orderKey => orderKey.ManualRank), cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return FeatureMovePlacement.Placed;
        }

        private static int InsertionIndex(List<int> sequence, int indexItHeld, int? targetFeatureId, bool placeBefore)
        {
            // No target at all is the end of the order, which is what Move to Bottom means.
            if (targetFeatureId is not { } target)
            {
                return sequence.Count;
            }

            var targetIndex = sequence.IndexOf(target);

            // A Feature placed against itself has nowhere to go, so it goes back where it was rather than
            // to an index the caller never named.
            if (targetIndex < 0)
            {
                return Math.Min(indexItHeld, sequence.Count);
            }

            return placeBefore ? targetIndex : targetIndex + 1;
        }

        /// <summary>
        /// Renumbers the whole sequence rather than the block between the two positions. Gaps, repeats and
        /// Features nobody has placed are all legal (INV-O2), and over such a set a partial renumber is not
        /// sound — a row left untouched ahead of the block can hold a larger place than the ones just
        /// written. Whole-table is also what makes Move to Bottom mean the bottom: an unplaced Feature sorts
        /// last, so the tail it jumps has to be given places too (OQ-4).
        /// </summary>
        private async Task WriteThePlaces(List<int> sequence, Dictionary<int, int?> placesHeld, CancellationToken cancellationToken)
        {
            // A set-based UPDATE per row, deliberately, rather than loading Features and saving them back.
            // Loading drags the navigation graph into the change tracker, and saving over that graph
            // re-inserts the join rows between a Portfolio and its Teams - the bug 5f055dc30 fixed for
            // seeding, in the same shape. It also cannot collide with whatever the request already tracks.
            for (var index = 0; index < sequence.Count; index++)
            {
                var place = index + 1;
                var featureId = sequence[index];

                if (placesHeld[featureId] == place)
                {
                    continue;
                }

                await context.Features
                    .Where(feature => feature.Id == featureId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(feature => feature.ManualRank, place), cancellationToken);
            }
        }
    }
}
