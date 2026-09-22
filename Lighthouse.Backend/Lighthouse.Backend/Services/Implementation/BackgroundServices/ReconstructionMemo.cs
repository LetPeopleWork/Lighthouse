using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// What earlier passes already found out about an owner, so that a chart load stops asking for
    /// days no pass will ever be able to write. There are two such days: one that falls before the
    /// owner's first finished item, and one a pass worked out and left with no row because nothing
    /// finished in the window behind it. Neither can be produced by trying again, and with nowhere to
    /// write that down both are found missing on every single chart load - so looking at a settled
    /// period keeps costing a pass, forever.
    ///
    /// A day's reading is worked out from the owner's stored items, and those change only when the
    /// owner is refreshed. That is why everything here can be thrown away the moment it is, and why
    /// none of it is worth keeping across a restart: losing the lot costs one wasted pass and nothing
    /// else. Nothing may be asserted from what this holds - a scenario that only passes because the
    /// memo is warm is a scenario whose real predicate is wrong.
    /// </summary>
    public sealed class ReconstructionMemo :
        IDomainEventHandler<TeamDataRefreshed>,
        IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        /// <summary>
        /// How much is kept. Past either bound what is held is dropped rather than grown, because an
        /// unbounded cache on a path a browser can drive is a way to run the instance out of memory,
        /// and dropping it costs one pass that would otherwise have been skipped.
        /// </summary>
        private const int MostOwnersRemembered = 256;

        private const int MostDaysRememberedPerOwner = 512;

        private readonly Dictionary<(int OwnerId, OwnerType OwnerType), OwnerNotes> byOwner = [];

        /// <summary>
        /// Whether a pass has already established that this day cannot be written. Answered from a
        /// dictionary and nothing else: the caller is a read somebody is waiting on.
        /// </summary>
        public bool NoPassCanWrite(int ownerId, OwnerType ownerType, DateOnly day)
        {
            lock (byOwner)
            {
                return byOwner.TryGetValue((ownerId, ownerType), out var notes) && notes.RulesOut(day);
            }
        }

        /// <summary>
        /// Where the owner's stored items begin. A null day means nothing has ever finished, which
        /// rules out every day rather than none.
        /// </summary>
        public void TheWalkReachesBackNoFurtherThan(
            int ownerId, OwnerType ownerType, DateOnly? earliestDayTheItemsSupport)
        {
            lock (byOwner)
            {
                NotesFor((ownerId, ownerType)).ReachesBackNoFurtherThan(earliestDayTheItemsSupport);
            }
        }

        /// <summary>
        /// A day nothing more can be got out of. Only a day every one of the owner's charts has been
        /// through counts: written down after some of them, the rest are never asked for again and the
        /// families that never got their turn become permanently unfillable.
        /// </summary>
        public void TheWalkHasAlreadyWorkedOut(int ownerId, OwnerType ownerType, DateOnly day)
        {
            lock (byOwner)
            {
                NotesFor((ownerId, ownerType)).AlreadyWorkedOut(day);
            }
        }

        public Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            Forget(domainEvent.TeamId, OwnerType.Team);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            Forget(domainEvent.PortfolioId, OwnerType.Portfolio);
            return Task.CompletedTask;
        }

        private void Forget(int ownerId, OwnerType ownerType)
        {
            lock (byOwner)
            {
                byOwner.Remove((ownerId, ownerType));
            }
        }

        private OwnerNotes NotesFor((int OwnerId, OwnerType OwnerType) key)
        {
            if (byOwner.TryGetValue(key, out var notes))
            {
                return notes;
            }

            if (byOwner.Count >= MostOwnersRemembered)
            {
                byOwner.Clear();
            }

            var fresh = new OwnerNotes();
            byOwner.Add(key, fresh);

            return fresh;
        }

        /// <summary>
        /// One owner's two findings. The floor is held as a day rather than as a day and a flag: not
        /// yet resolved is the smallest day there is, so nothing falls below it, and nothing finished
        /// at all is the largest, so everything does.
        /// </summary>
        private sealed class OwnerNotes
        {
            private readonly HashSet<DateOnly> alreadyWorkedOut = [];

            private DateOnly earliestDayTheItemsSupport = DateOnly.MinValue;

            public bool RulesOut(DateOnly day) => day < earliestDayTheItemsSupport || alreadyWorkedOut.Contains(day);

            public void ReachesBackNoFurtherThan(DateOnly? earliestFinishedDay)
                => earliestDayTheItemsSupport = earliestFinishedDay ?? DateOnly.MaxValue;

            public void AlreadyWorkedOut(DateOnly day)
            {
                if (alreadyWorkedOut.Count >= MostDaysRememberedPerOwner)
                {
                    alreadyWorkedOut.Clear();
                }

                alreadyWorkedOut.Add(day);
            }
        }
    }
}
