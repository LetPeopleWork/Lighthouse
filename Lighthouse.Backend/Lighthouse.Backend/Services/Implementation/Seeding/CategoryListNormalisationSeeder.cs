using System.Data.Common;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    /// <summary>
    /// Rewrites every Team's and Portfolio's To Do, Doing and Done lists as raw tracker states, once per start. Lists
    /// saved by older versions can hold a mapping's name in place of its states. An owner that cannot be rewritten is
    /// skipped with a Warning and retried on the next start; reads normalise the lists anyway, so nothing is lost.
    /// Every line carries ids, counts and a fixed rule only: these logs leave customers' instances.
    /// </summary>
    public class CategoryListNormalisationSeeder(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        ILogger<CategoryListNormalisationSeeder> logger) : ISeeder
    {
        private const string Shadowing = "Shadowing";
        private const string Chained = "Chained";
        private const string EmptyGroup = "EmptyGroup";
        private const string SaveFailed = "SaveFailed";
        private const string Unreadable = "Unreadable";

        public async Task Seed()
        {
            var tally = new Tally();

            await NormaliseAll(teamRepository, nameof(Team), tally);
            await NormaliseAll(portfolioRepository, nameof(Portfolio), tally);

            logger.LogInformation(
                "Category lists normalised: {Scanned} scanned, {Rewritten} rewritten, {LegacyExpanded} legacy-expanded, {Skipped} skipped, {UnreadOwnerTypes} owner types not read",
                tally.Scanned,
                tally.Rewritten,
                tally.LegacyExpanded,
                tally.Skipped,
                tally.UnreadOwnerTypes);
        }

        private async Task NormaliseAll<TOwner>(IRepository<TOwner> repository, string ownerType, Tally tally)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            var owners = TryRead(repository);
            if (owners == null)
            {
                tally.UnreadOwnerTypes++;
                logger.LogWarning("Could not read the {OwnerType} owners; Rule {Rule}", ownerType, Unreadable);
                return;
            }

            foreach (var owner in owners)
            {
                tally.Scanned++;
                await Normalise(repository, owner, ownerType, tally);
            }
        }

        private async Task Normalise<TOwner>(IRepository<TOwner> repository, TOwner owner, string ownerType, Tally tally)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            var plan = TryPlan(owner);
            if (plan == null)
            {
                tally.Skipped++;
                logger.LogWarning("{OwnerType} {OwnerId} could not be read and is left as it is; Rule {Rule}", ownerType, owner.Id, Unreadable);
                return;
            }

            var (legacyShape, stored, normalised) = plan;
            if (legacyShape == LegacyListShape.EmptyGroup)
            {
                tally.Skipped++;
                logger.LogWarning("{OwnerType} {OwnerId} left as it is; Rule {Rule}", ownerType, owner.Id, EmptyGroup);
                return;
            }

            if (normalised.SameAs(stored))
            {
                return;
            }

            normalised.WriteTo(owner);

            if (!await TrySave(repository))
            {
                stored.WriteTo(owner);
                tally.Skipped++;
                logger.LogWarning("{OwnerType} {OwnerId} could not be saved and is retried on the next start; Rule {Rule}", ownerType, owner.Id, SaveFailed);
                return;
            }

            tally.Rewritten++;
            logger.LogInformation("{OwnerType} {OwnerId} category lists rewritten as raw states", ownerType, owner.Id);

            if (legacyShape != null)
            {
                tally.LegacyExpanded++;
                logger.LogWarning(
                    "{OwnerType} {OwnerId} had a group's name listed beside one of its own states, or a group holding another group's name, and was rewritten as older versions read it; Rule {Rule}",
                    ownerType,
                    owner.Id,
                    legacyShape == LegacyListShape.Shadowing ? Shadowing : Chained);
            }
        }

        // Startup has no handler around the seeders, so a throw would refuse the start. The failure itself is not
        // logged: its text can carry an owner's or a state's name, and these logs leave customers' instances.
        private static List<TOwner>? TryRead<TOwner>(IRepository<TOwner> repository)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            try
            {
                return [.. repository.GetAll()];
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Stored data no rule expects, such as a missing list or a missing mapping, must not refuse the start
        // either. The failure is not logged, for the same reason as above.
        private static NormalisationPlan? TryPlan(WorkTrackingSystemOptionsOwner owner)
        {
            try
            {
                var lists = owner.CategoryLists();
                var stored = ListsSnapshot.Of(owner);
                return new NormalisationPlan(lists.LegacyShape(), stored, stored.NormalisedBy(lists));
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                return null;
            }
        }

        private static async Task<bool> TrySave<TOwner>(IRepository<TOwner> repository)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            try
            {
                await repository.Save();
                return true;
            }
            catch (DbUpdateException refused)
            {
                // Every owner shares one context, so a refused row left tracked would be retried, and refused again,
                // with the next owner's save.
                foreach (var entry in refused.Entries)
                {
                    entry.State = EntityState.Detached;
                }

                return false;
            }
            catch (Exception failure) when (failure is InvalidOperationException or DbException)
            {
                return false;
            }
        }

        private sealed record NormalisationPlan(LegacyListShape? LegacyShape, ListsSnapshot Stored, ListsSnapshot Normalised);

        private sealed record ListsSnapshot(List<string> ToDo, List<string> Doing, List<string> Done)
        {
            public static ListsSnapshot Of(WorkTrackingSystemOptionsOwner owner)
                => new([.. owner.ToDoStates], [.. owner.DoingStates], [.. owner.DoneStates]);

            public ListsSnapshot NormalisedBy(StateCategoryLists lists)
                => new(lists.NormalisedStatesFor(ToDo), lists.NormalisedStatesFor(Doing), lists.NormalisedStatesFor(Done));

            public bool SameAs(ListsSnapshot other)
                => ToDo.SequenceEqual(other.ToDo, StringComparer.Ordinal)
                    && Doing.SequenceEqual(other.Doing, StringComparer.Ordinal)
                    && Done.SequenceEqual(other.Done, StringComparer.Ordinal);

            public void WriteTo(WorkTrackingSystemOptionsOwner owner)
            {
                owner.ToDoStates = [.. ToDo];
                owner.DoingStates = [.. Doing];
                owner.DoneStates = [.. Done];
            }
        }

        private sealed class Tally
        {
            public int Scanned { get; set; }

            public int Rewritten { get; set; }

            public int LegacyExpanded { get; set; }

            public int Skipped { get; set; }

            public int UnreadOwnerTypes { get; set; }
        }
    }
}
