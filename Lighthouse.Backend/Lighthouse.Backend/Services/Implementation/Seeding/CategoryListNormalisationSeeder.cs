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
                "Category lists normalised: {Scanned} scanned, {Rewritten} rewritten, {LegacyExpanded} legacy-expanded, {Skipped} skipped",
                tally.Scanned,
                tally.Rewritten,
                tally.LegacyExpanded,
                tally.Skipped);
        }

        private async Task NormaliseAll<TOwner>(IRepository<TOwner> repository, string ownerType, Tally tally)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            var owners = TryRead(repository);
            if (owners == null)
            {
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
            if (owner.StateMappings.Exists(mapping => StateCategoryLists.MembersOf(mapping).Count == 0))
            {
                tally.Skipped++;
                logger.LogWarning("{OwnerType} {OwnerId} left as it is; Rule {Rule}", ownerType, owner.Id, EmptyGroup);
                return;
            }

            var stored = ListsSnapshot.Of(owner);
            var normalised = stored.NormalisedBy(owner.CategoryLists());
            if (normalised.SameAs(stored))
            {
                return;
            }

            var legacyRule = LegacyRuleOf(owner);
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

            if (legacyRule != null)
            {
                tally.LegacyExpanded++;
                logger.LogWarning("{OwnerType} {OwnerId} rewritten the old way; Rule {Rule}", ownerType, owner.Id, legacyRule);
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
            catch (Exception)
            {
                return false;
            }
        }

        private static string? LegacyRuleOf(WorkTrackingSystemOptionsOwner owner)
        {
            var lists = owner.CategoryLists();
            var entries = lists.StoredLists()
                .SelectMany(stored => stored.States.Select(entry => (Entry: entry, Kind: lists.ClassifyListEntry(stored.States, entry))))
                .ToList();

            if (entries.Exists(classified => classified.Kind == ListEntryKind.Shadowing))
            {
                return Shadowing;
            }

            return entries.Exists(classified => classified.Kind != ListEntryKind.NotAGroupName && GroupsAnotherGroup(lists, classified.Entry))
                ? Chained
                : null;
        }

        private static bool GroupsAnotherGroup(StateCategoryLists lists, string groupName)
            => StateCategoryLists.MembersOf(lists.GroupNamed(groupName)!)
                .Exists(member => !StateCategoryLists.IsSameState(member, groupName) && lists.GroupNamed(member) != null);

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
        }
    }
}
